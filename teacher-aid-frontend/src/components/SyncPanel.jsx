import { useState, useEffect, useRef } from 'react'
import axios from 'axios'
import { useAuth } from '../context/AuthContext'

const API = 'http://localhost:5010/api'

function parseAssignment(fileName) {
  if (!fileName) return 'Okänd uppgift'
  const parts = fileName.replace(/\.[^.]+$/, '').split('_')
  return parts.length >= 4 ? parts.slice(3).join(' ') : 'Okänd uppgift'
}

function groupByAssignment(submissions) {
  const groups = {}
  for (const s of submissions) {
    const key = parseAssignment(s.sourceFileName)
    if (!groups[key]) groups[key] = []
    groups[key].push(s)
  }
  return groups
}

function StatusBadge({ s, pollingIds }) {
  if (pollingIds.has(s.id))
    return <span className="text-xs font-medium px-2.5 py-1 rounded-full bg-amber-50 text-amber-700">Genererar…</span>
  if (s.feedback?.approved)
    return <span className="text-xs font-medium px-2.5 py-1 rounded-full bg-green-50 text-green-700">✓ Godkänd</span>
  if (s.feedback)
    return <span className="text-xs font-medium px-2.5 py-1 rounded-full bg-yellow-50 text-yellow-700">Granskas</span>
  return <span className="text-xs font-medium px-2.5 py-1 rounded-full bg-blue-50 text-blue-700">Väntar</span>
}

export default function SyncPanel() {
  const [syncing, setSyncing] = useState(false)
  const [syncResult, setSyncResult] = useState(null)
  const [submissions, setSubmissions] = useState([])
  const [loading, setLoading] = useState(false)
  const [pollingIds, setPollingIds] = useState(new Set())
  const [openGroups, setOpenGroups] = useState({})
  const [reviewingId, setReviewingId] = useState(null)
  const [expandedId, setExpandedId] = useState(null)
  const [editStates, setEditStates] = useState({})
  const [saving, setSaving] = useState(false)
  const pollingRefs = useRef({})
  const { token } = useAuth()
  const headers = { Authorization: `Bearer ${token}` }

  async function downloadFile(submissionId, fileName) {
    const res = await fetch(`${API}/submissions/${submissionId}/file`, { headers })
    if (!res.ok) return alert('Kunde inte hämta filen')
    const blob = await res.blob()
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = fileName
    a.click()
    URL.revokeObjectURL(url)
  }

  useEffect(() => {
    fetchSubmissions()
    return () => Object.values(pollingRefs.current).forEach(clearInterval)
  }, [])

  const fetchSubmissions = async () => {
    setLoading(true)
    try {
      const { data } = await axios.get(`${API}/submissions/all`, { headers })
      setSubmissions(data)
      data.filter(s => !s.feedback).forEach(s => startPolling(s.id))
      // Open all groups by default
      const groups = groupByAssignment(data)
      setOpenGroups(prev => {
        const next = { ...prev }
        for (const key of Object.keys(groups)) {
          if (!(key in next)) next[key] = true
        }
        return next
      })
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
      const { data } = await axios.post(`${API}/sync/submissions`, {}, { headers })
      setSyncResult(data)
      fetchSubmissions()
    } catch (err) {
      setSyncResult({ errors: [err.message], processed: [] })
    } finally {
      setSyncing(false)
    }
  }

  const startPolling = (id) => {
    if (pollingRefs.current[id]) return
    setPollingIds(prev => new Set([...prev, id]))
    pollingRefs.current[id] = setInterval(async () => {
      try {
        await axios.get(`${API}/submissions/${id}/feedback`, { headers })
        clearInterval(pollingRefs.current[id])
        delete pollingRefs.current[id]
        setPollingIds(prev => { const next = new Set(prev); next.delete(id); return next })
        fetchSubmissions()
      } catch {
        // Keep polling until feedback arrives.
      }
    }, 15000)
  }

  const handleOpenReview = (s) => {
    if (reviewingId === s.id) {
      setReviewingId(null)
      return
    }
    setReviewingId(s.id)
    // Initialize state only when missing (preserve edits when collapsing).
    if (!editStates[s.id]) {
      setEditStates(prev => ({
        ...prev,
        [s.id]: {
          feedback: s.feedback?.teacherFeedback ?? s.feedback?.aiFeedback ?? '',
          grade: s.feedback?.teacherGrade ?? 'G'
        }
      }))
    }
  }

  const editState = editStates[reviewingId] ?? { feedback: '', grade: 'G' }
  const setEditState = (updater) => {
    setEditStates(prev => ({
      ...prev,
      [reviewingId]: typeof updater === 'function' ? updater(prev[reviewingId]) : updater
    }))
  }

  const handleApprove = async (id) => {
    setSaving(true)
    try {
      const state = editStates[id]
      await axios.put(
        `${API}/submissions/${id}/feedback`,
        { teacherFeedback: state.feedback, teacherGrade: state.grade },
        { headers }
      )
      setReviewingId(null)
      setEditStates(prev => { const next = { ...prev }; delete next[id]; return next })
      fetchSubmissions()
    } catch (err) {
      alert('Kunde inte spara: ' + err.message)
    } finally {
      setSaving(false)
    }
  }

  const toggleGroup = (key) =>
    setOpenGroups(prev => ({ ...prev, [key]: !prev[key] }))

  const groups = groupByAssignment(submissions)

  return (
    <div className="max-w-2xl mx-auto space-y-4">

      {/* Sync bar */}
      <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-5">
        <div className="flex items-center justify-between gap-4">
          <div>
            <h2 className="text-sm font-semibold text-gray-900">Inlämningar</h2>
            <p className="text-xs text-gray-400 mt-0.5">
              Lägg filer i <code className="bg-gray-100 px-1 rounded text-gray-600">inlamningar/</code>
              {' · '}Format: <code className="bg-gray-100 px-1 rounded text-gray-600">Förnamn_Efternamn_KursID.pdf/.docx</code>
            </p>
          </div>
          <div className="flex flex-col items-end gap-1">
            <button
              onClick={handleSync}
              disabled={syncing}
              className="bg-blue-600 text-white text-sm font-medium px-4 py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50 whitespace-nowrap transition-colors"
            >
              {syncing ? 'Synkar…' : '↻ Synka nu'}
            </button>
            <p className="text-xs text-gray-400 text-right">
              Kom ihåg: lägg in <code className="bg-gray-100 px-1 rounded text-gray-500">uppgiftsbeskrivning</code> och <code className="bg-gray-100 px-1 rounded text-gray-500">bedömningsmall</code> i uppgiftsmappen först.
            </p>
          </div>
        </div>

        {syncResult && (
          <div className="mt-3 pt-3 border-t border-gray-100 text-xs space-y-1">
            {syncResult.message && <p className="text-gray-500">{syncResult.message}</p>}
            {syncResult.processedCount > 0 && (
              <p className="text-green-700 font-medium">✓ {syncResult.processedCount} ny/nya inlämning(ar) importerade</p>
            )}
            {syncResult.processed?.map(f => <p key={f} className="text-gray-400 pl-2">– {f}</p>)}
            {syncResult.warnings?.map(w => (
              <p key={w} className="text-amber-600 pl-2">⚠ {w}</p>
            ))}
            {syncResult.errors?.map(e => <p key={e} className="text-red-500 pl-2">✗ {e}</p>)}
            {syncResult.processedCount === 0 && syncResult.errorCount === 0 && syncResult.warningCount === 0 && !syncResult.message && (
              <p className="text-gray-400">Inga nya filer att importera.</p>
            )}
          </div>
        )}
      </div>

      {/* Loading */}
      {loading && (
        <p className="text-sm text-gray-400 text-center py-6">Laddar…</p>
      )}

      {/* Empty state */}
      {!loading && submissions.length === 0 && (
        <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-8 text-center">
          <p className="text-sm text-gray-400">Inga inlämningar ännu. Synka för att importera filer.</p>
        </div>
      )}

      {/* Grouped submissions */}
      {Object.entries(groups).map(([assignment, subs]) => (
        <div key={assignment} className="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden">

          {/* Group header */}
          <button
            onClick={() => toggleGroup(assignment)}
            className="w-full flex items-center justify-between px-5 py-3.5 hover:bg-gray-50 transition-colors"
          >
            <div className="flex items-center gap-2.5">
              <span className="text-sm font-semibold text-gray-900">{assignment}</span>
              <span className="text-xs bg-gray-100 text-gray-500 px-2 py-0.5 rounded-full">
                {subs.length} {subs.length === 1 ? 'elev' : 'elever'}
              </span>
            </div>
            <span className={`text-gray-400 text-xs transition-transform duration-150 inline-block ${openGroups[assignment] ? 'rotate-180' : ''}`}>
              ▼
            </span>
          </button>

          {/* Rows */}
          {openGroups[assignment] && (
            <div className="border-t border-gray-100">
              {subs.map((s, i) => (
                <div key={s.id} className={i < subs.length - 1 ? 'border-b border-gray-100' : ''}>

                  {/* Submission row */}
                  <div className="flex items-center justify-between px-5 py-3 gap-3">
                    <div>
                      <p className="text-sm font-medium text-gray-900">{s.studentName}</p>
                      <p className="text-xs text-gray-400 mt-0.5">
                        {s.courseId}
                        {s.sourceFileName && (
                          <>
                            <span className="text-gray-300"> · </span>
                            <button
                              type="button"
                              onClick={() => downloadFile(s.id, s.sourceFileName)}
                              className="text-blue-400 hover:text-blue-600 hover:underline transition-colors"
                            >
                              {s.sourceFileName}
                            </button>
                          </>
                        )}
                        <span className="text-gray-300"> · </span>
                        {new Date(s.submittedAt).toLocaleString('sv-SE', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })}
                      </p>
                    </div>
                    <div className="flex items-center gap-2 flex-shrink-0">
                      <StatusBadge s={s} pollingIds={pollingIds} />

                      {s.feedback?.approved && (
                        <span className="text-xs font-medium bg-gray-100 text-gray-600 px-2 py-0.5 rounded">
                          {s.feedback.teacherGrade}
                        </span>
                      )}

                      {s.feedback && !s.feedback.approved && !pollingIds.has(s.id) && (
                        <button
                          onClick={() => handleOpenReview(s)}
                          className={`text-xs font-medium px-3 py-1.5 rounded-lg border transition-colors ${
                            reviewingId === s.id
                              ? 'border-blue-300 bg-blue-50 text-blue-700'
                              : 'border-blue-200 text-blue-700 hover:bg-blue-50'
                          }`}
                        >
                          {reviewingId === s.id ? '▲ Granska' : 'Granska'}
                        </button>
                      )}

                      {s.feedback?.approved && (
                        <button
                          onClick={() => setExpandedId(expandedId === s.id ? null : s.id)}
                          className="text-xs text-gray-300 hover:text-gray-500 transition-colors px-1"
                        >
                          {expandedId === s.id ? '▲' : '▼'}
                        </button>
                      )}
                    </div>
                  </div>

                  {/* Expanded approved feedback */}
                  {expandedId === s.id && s.feedback?.approved && (
                    <div className="px-5 pb-4 pt-3 bg-gray-50 border-t border-gray-100">
                      {s.feedback.summary && (
                        <p className="text-xs text-gray-400 italic mb-2">{s.feedback.summary}</p>
                      )}
                      <p className="text-sm text-gray-700 whitespace-pre-wrap leading-relaxed">
                        {s.feedback.teacherFeedback ?? s.feedback.aiFeedback}
                      </p>
                    </div>
                  )}

                  {/* Inline review form */}
                  {reviewingId === s.id && (
                    <div className="px-5 pb-4 pt-3 bg-gray-50 border-t border-gray-100 space-y-3">
                      {s.feedback.summary && (
                        <p className="text-xs text-gray-400 italic">{s.feedback.summary}</p>
                      )}
                      <div>
                        <label className="block text-xs font-medium text-gray-500 mb-1.5">Feedback</label>
                        <textarea
                          className="w-full border border-gray-200 rounded-lg p-2.5 text-sm text-gray-900 h-64 resize-y focus:outline-none focus:border-blue-400"
                          value={editState.feedback}
                          onChange={e => setEditState(prev => ({ ...prev, feedback: e.target.value }))}
                        />
                      </div>
                      <div className="flex items-center gap-3">
                        <div className="flex items-center gap-2">
                          <label className="text-xs font-medium text-gray-500">Betyg</label>
                          <select
                            className="border border-gray-200 rounded-lg px-2 py-1.5 text-sm text-gray-900 focus:outline-none focus:border-blue-400"
                            value={editState.grade}
                            onChange={e => setEditState(prev => ({ ...prev, grade: e.target.value }))}
                          >
                            <option>IG</option>
                            <option>G</option>
                            <option>VG</option>
                          </select>
                        </div>
                        <button
                          onClick={() => setReviewingId(null)}
                          className="text-xs text-gray-400 hover:text-gray-600 transition-colors"
                        >
                          Fäll ihop
                        </button>
                        <button
                          onClick={() => handleApprove(s.id)}
                          disabled={saving}
                          className="ml-auto bg-blue-600 text-white text-xs font-medium px-4 py-1.5 rounded-lg hover:bg-blue-700 disabled:opacity-50 transition-colors"
                        >
                          {saving ? 'Sparar…' : 'Godkänn och spara'}
                        </button>
                      </div>
                    </div>
                  )}

                </div>
              ))}
            </div>
          )}
        </div>
      ))}
    </div>
  )
}
