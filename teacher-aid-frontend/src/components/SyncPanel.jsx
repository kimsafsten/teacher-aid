import { useState, useEffect, useRef } from 'react'
import axios from 'axios'
import { useAuth } from '../context/AuthContext'

const API = 'http://localhost:5010/api'

export default function SyncPanel() {
  const [syncing, setSyncing] = useState(false)
  const [syncResult, setSyncResult] = useState(null)
  const [submissions, setSubmissions] = useState([])
  const [loading, setLoading] = useState(false)
  const [pollingIds, setPollingIds] = useState(new Set())
  const [reviewingId, setReviewingId] = useState(null)
  const [expandedId, setExpandedId] = useState(null)
  const [editState, setEditState] = useState({})
  const [saving, setSaving] = useState(false)
  const pollingRefs = useRef({})
  const { token } = useAuth()
  const headers = { Authorization: `Bearer ${token}` }

  useEffect(() => {
    fetchSubmissions()
    return () => Object.values(pollingRefs.current).forEach(clearInterval)
  }, [])

  const fetchSubmissions = async () => {
    setLoading(true)
    try {
      const { data } = await axios.get(`${API}/submissions/all`, { headers })
      setSubmissions(data)
    } catch {
      setSubmissions([])
    } finally {
      setLoading(false)
    }
  }

  const handleSync = async () => {
    setSyncing(true)
    setSyncResult(null)
    try {
      const { data } = await axios.post(`${API}/sync/inlamningar`, {}, { headers })
      setSyncResult(data)
      fetchSubmissions()
    } catch (err) {
      setSyncResult({ errors: [err.message], processed: [] })
    } finally {
      setSyncing(false)
    }
  }

  const startPolling = (id) => {
    setPollingIds(prev => new Set([...prev, id]))
    let attempts = 0
    pollingRefs.current[id] = setInterval(async () => {
      try {
        await axios.get(`${API}/submissions/${id}/feedback`, { headers })
        clearInterval(pollingRefs.current[id])
        delete pollingRefs.current[id]
        setPollingIds(prev => { const next = new Set(prev); next.delete(id); return next })
        setReviewingId(id)
        fetchSubmissions()
      } catch {
        attempts++
        if (attempts >= 30) {
          clearInterval(pollingRefs.current[id])
          setPollingIds(prev => { const next = new Set(prev); next.delete(id); return next })
          alert(`Timeout – kunde inte hämta feedback för inlämning #${id}`)
        }
      }
    }, 2000)
  }

  const handleProcess = async (s) => {
    try {
      await axios.post(`${API}/submissions/${s.id}/process`, {}, { headers })
      startPolling(s.id)
    } catch (err) {
      alert('Kunde inte starta AI-granskning: ' + err.message)
    }
  }

  const handleOpenReview = (s) => {
    setReviewingId(s.id)
    setEditState({
      feedback: s.feedback?.teacherFeedback ?? s.feedback?.aiFeedback ?? '',
      grade: s.feedback?.teacherGrade ?? ''
    })
  }

  const handleApprove = async (id) => {
    setSaving(true)
    try {
      await axios.put(
        `${API}/submissions/${id}/feedback`,
        { teacherFeedback: editState.feedback, teacherGrade: editState.grade },
        { headers }
      )
      setReviewingId(null)
      fetchSubmissions()
    } catch (err) {
      alert('Kunde inte spara: ' + err.message)
    } finally {
      setSaving(false)
    }
  }

  const pending = submissions.filter(s => !s.feedback && !pollingIds.has(s.id))
  const polling = submissions.filter(s => pollingIds.has(s.id))
  const reviewing = submissions.filter(s => s.feedback && !s.feedback.approved)
  const approved = submissions.filter(s => s.feedback?.approved)

  return (
    <div className="max-w-2xl mx-auto space-y-6">

      {/* Synksektion */}
      <div className="bg-white rounded-xl shadow p-6">
        <div className="flex items-center justify-between mb-2">
          <div>
            <h2 className="text-lg font-semibold">Synkronisera inlämningar</h2>
            <p className="text-sm text-gray-500 mt-1">
              Lägg filerna i <code className="bg-gray-100 px-1 rounded">inlamningar/</code>.
              Format: <code className="bg-gray-100 px-1 rounded">Förnamn_Efternamn_KursID_Uppgift.pdf</code>
            </p>
          </div>
          <button
            onClick={handleSync}
            disabled={syncing}
            className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 disabled:opacity-50 whitespace-nowrap ml-4"
          >
            {syncing ? 'Synkroniserar...' : '↻ Synka nu'}
          </button>
        </div>
        {syncResult && (
          <div className="mt-3 text-sm space-y-1">
            {syncResult.message && <p className="text-gray-600">{syncResult.message}</p>}
            {syncResult.processedCount > 0 && <p className="text-green-700">✓ {syncResult.processedCount} ny/nya inlämning(ar) importerade</p>}
            {syncResult.processed?.map(f => <p key={f} className="text-gray-500 pl-4">– {f}</p>)}
            {syncResult.errors?.map(e => <p key={e} className="text-red-600 pl-4">✗ {e}</p>)}
            {syncResult.processedCount === 0 && syncResult.errorCount === 0 && !syncResult.message && (
              <p className="text-gray-500">Inga nya filer att importera.</p>
            )}
          </div>
        )}
      </div>

      {/* Genererar feedback */}
      {polling.length > 0 && (
        <Section title="Genererar feedback" count={polling.length}>
          {polling.map(s => (
            <li key={s.id} className="py-3 flex items-center justify-between">
              <SubmissionInfo s={s} />
              <span className="text-sm text-gray-400 animate-pulse ml-4">Väntar på AI...</span>
            </li>
          ))}
        </Section>
      )}

      {/* Väntande */}
      <Section title="Väntande" count={pending.length} onRefresh={fetchSubmissions} loading={loading}
        emptyText="Inga väntande inlämningar.">
        {pending.map(s => (
          <li key={s.id} className="py-3 flex items-center justify-between">
            <SubmissionInfo s={s} />
            <button
              onClick={() => handleProcess(s)}
              className="bg-green-600 text-white text-sm px-3 py-1.5 rounded hover:bg-green-700 ml-4"
            >
              Ge AI-feedback
            </button>
          </li>
        ))}
      </Section>

      {/* Under granskning */}
      {reviewing.length > 0 && (
        <Section title="Under granskning" count={reviewing.length}>
          {reviewing.map(s => (
            <li key={s.id} className="py-2">
              {reviewingId === s.id ? (
                <div className="space-y-3">
                  <div className="flex items-center justify-between">
                    <SubmissionInfo s={s} />
                    <button onClick={() => setReviewingId(null)} className="text-gray-400 text-sm ml-4">✕ Stäng</button>
                  </div>
                  {s.feedback.summary && (
                    <p className="text-sm text-gray-500 italic bg-gray-50 rounded p-2">{s.feedback.summary}</p>
                  )}
                  <div>
                    <label className="block text-xs font-medium text-gray-600 mb-1">Feedback (redigera vid behov)</label>
                    <textarea
                      className="w-full border rounded p-2 h-36 text-sm"
                      value={editState.feedback}
                      onChange={e => setEditState(prev => ({ ...prev, feedback: e.target.value }))}
                    />
                  </div>
                  <div className="flex items-center gap-3">
                    <label className="text-xs font-medium text-gray-600">Betyg</label>
                    <select
                      className="border rounded p-1.5 text-sm"
                      value={editState.grade}
                      onChange={e => setEditState(prev => ({ ...prev, grade: e.target.value }))}
                    >
                      <option>IG</option>
                      <option>G</option>
                      <option>VG</option>
                    </select>
                    <button
                      onClick={() => handleApprove(s.id)}
                      disabled={saving}
                      className="flex-1 bg-green-600 text-white py-1.5 rounded hover:bg-green-700 disabled:opacity-50 text-sm"
                    >
                      {saving ? 'Sparar...' : 'Godkänn och spara'}
                    </button>
                  </div>
                </div>
              ) : (
                <div className="flex items-center justify-between">
                  <SubmissionInfo s={s} />
                  <button
                    onClick={() => handleOpenReview(s)}
                    className="bg-blue-600 text-white text-sm px-3 py-1.5 rounded hover:bg-blue-700 ml-4"
                  >
                    Granska
                  </button>
                </div>
              )}
            </li>
          ))}
        </Section>
      )}

      {/* Godkända */}
      {approved.length > 0 && (
        <Section title="Godkända" count={approved.length}>
          {approved.map(s => (
            <li key={s.id} className="py-2">
              <button
                onClick={() => setExpandedId(expandedId === s.id ? null : s.id)}
                className="w-full text-left flex items-center justify-between py-1"
              >
                <div className="flex items-center gap-2">
                  <span className="text-green-600 font-bold">✓</span>
                  <SubmissionInfo s={s} />
                </div>
                <div className="flex items-center gap-2 ml-4">
                  <span className="bg-gray-100 text-gray-700 text-xs px-2 py-0.5 rounded font-medium">
                    {s.feedback.teacherGrade}
                  </span>
                  <span className="text-gray-400 text-xs">{expandedId === s.id ? '▲' : '▼'}</span>
                </div>
              </button>
              {expandedId === s.id && (
                <div className="mt-2 ml-6 bg-gray-50 rounded p-3 text-sm text-gray-700 space-y-2">
                  {s.feedback.summary && <p className="text-gray-500 italic">{s.feedback.summary}</p>}
                  <p className="whitespace-pre-wrap">{s.feedback.teacherFeedback ?? s.feedback.aiFeedback}</p>
                </div>
              )}
            </li>
          ))}
        </Section>
      )}
    </div>
  )
}

function Section({ title, count, onRefresh, loading, emptyText, children }) {
  return (
    <div className="bg-white rounded-xl shadow p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold">
          {title}
          <span className="ml-2 text-sm font-normal text-gray-400">({count})</span>
        </h2>
        {onRefresh && (
          <button onClick={onRefresh} className="text-sm text-gray-500 hover:text-gray-800">Uppdatera</button>
        )}
      </div>
      {loading ? (
        <p className="text-gray-400 text-sm">Laddar...</p>
      ) : count === 0 ? (
        <p className="text-gray-400 text-sm">{emptyText}</p>
      ) : (
        <ul className="divide-y">{children}</ul>
      )}
    </div>
  )
}

function SubmissionInfo({ s }) {
  return (
    <div>
      <p className="font-medium text-sm">{s.studentName}</p>
      <p className="text-xs text-gray-500">{s.courseId} · {s.sourceFileName ?? `Inlämning #${s.id}`}</p>
      <p className="text-xs text-gray-400">{new Date(s.submittedAt).toLocaleString('sv-SE')}</p>
    </div>
  )
}
