import { useState, useEffect } from 'react'
import axios from 'axios'
import { useAuth } from '../context/AuthContext'

const API = 'http://localhost:5010/api'
const STORAGE_KEY = 'teacher_aid_last_material'

export default function MaterialGenerator() {
  const [courseId, setCourseId]           = useState('')
  const [instruction, setInstruction]     = useState('')
  const [result, setResult]               = useState(null)
  const [editedContent, setEditedContent] = useState('')
  const [loading, setLoading]             = useState(false)
  const [saving, setSaving]               = useState(false)
  const [saved, setSaved]                 = useState(false)
  const [syncing, setSyncing]             = useState(false)
  const [syncResult, setSyncResult]       = useState(null)
  const [history, setHistory]             = useState([])
  const { token } = useAuth()
  const headers = { Authorization: `Bearer ${token}` }

  useEffect(() => {
    const stored = localStorage.getItem(STORAGE_KEY)
    if (stored) {
      const parsed = JSON.parse(stored)
      setResult(parsed)
      setEditedContent(parsed.content)
    }
    fetchHistory()
  }, [])

  const fetchHistory = async () => {
    try {
      const { data } = await axios.get(`${API}/qa/generated`, { headers })
      setHistory(data.files ?? [])
    } catch {
      // Silent failure — history is non-critical.
    }
  }

  const handleSync = async () => {
    setSyncing(true)
    setSyncResult(null)
    try {
      const { data } = await axios.post(`${API}/sync/course-material`, {}, { headers })
      setSyncResult(data)
    } catch (err) {
      setSyncResult({ errors: [err.message], processed: [] })
    } finally {
      setSyncing(false)
    }
  }

  const handleGenerate = async (e) => {
    e.preventDefault()
    setLoading(true)
    setResult(null)
    setEditedContent('')
    setSaved(false)
    try {
      const { data } = await axios.post(
        `${API}/qa/generate-material`,
        { courseId, instruction },
        { headers }
      )
      const newResult = {
        content: data.content,
        savedAs: data.savedAs,
        courseId,
        generatedAt: new Date().toLocaleString('sv-SE'),
      }
      setResult(newResult)
      setEditedContent(data.content)
      localStorage.setItem(STORAGE_KEY, JSON.stringify(newResult))
      await fetchHistory()
    } catch (err) {
      const errResult = { content: 'Något gick fel: ' + err.message }
      setResult(errResult)
      setEditedContent(errResult.content)
    } finally {
      setLoading(false)
    }
  }

  const handleSave = async () => {
    if (!result?.savedAs) return
    setSaving(true)
    setSaved(false)
    try {
      await axios.put(
        `${API}/qa/generated/${result.savedAs}`,
        { content: editedContent },
        { headers }
      )
      const updated = { ...result, content: editedContent }
      setResult(updated)
      localStorage.setItem(STORAGE_KEY, JSON.stringify(updated))
      setSaved(true)
      setTimeout(() => setSaved(false), 3000)
    } catch (err) {
      alert('Kunde inte spara: ' + err.message)
    } finally {
      setSaving(false)
    }
  }

  const handleLoadHistory = async (fileName) => {
    try {
      const { data } = await axios.get(`${API}/qa/generated/${fileName}`, { headers })
      const loaded = {
        content: data.content,
        savedAs: fileName,
        generatedAt: history.find(f => f.fileName === fileName)?.generatedAt ?? '',
      }
      setResult(loaded)
      setEditedContent(data.content)
      localStorage.setItem(STORAGE_KEY, JSON.stringify(loaded))
      setSaved(false)
    } catch (err) {
      alert('Kunde inte läsa filen: ' + err.message)
    }
  }

  const isDirty = result && editedContent !== result.content

  return (
    <div className="space-y-4">

      {/* Sync bar */}
      <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-5">
        <div className="flex items-center justify-between gap-4">
          <div>
            <h2 className="text-sm font-semibold text-gray-900">Kursmaterial</h2>
            <p className="text-xs text-gray-400 mt-0.5">
              Lägg filer i <code className="bg-gray-100 px-1 rounded text-gray-600">kursmaterial/</code>
              {' · '}Indexeras för RAG-sökning
            </p>
          </div>
          <button
            onClick={handleSync}
            disabled={syncing}
            className="bg-blue-600 text-white text-sm font-medium px-4 py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50 whitespace-nowrap transition-colors"
          >
            {syncing ? 'Synkar…' : '↻ Synka nu'}
          </button>
        </div>

        {syncResult && (
          <div className="mt-3 pt-3 border-t border-gray-100 text-xs space-y-1">
            {syncResult.message && <p className="text-gray-500">{syncResult.message}</p>}
            {syncResult.processedCount > 0 && (
              <p className="text-green-700 font-medium">✓ {syncResult.processedCount} dokument indexerade</p>
            )}
            {syncResult.processed?.map(f => <p key={f} className="text-gray-400 pl-2">– {f}</p>)}
            {syncResult.errors?.map(e => <p key={e} className="text-red-500 pl-2">✗ {e}</p>)}
            {syncResult.processedCount === 0 && syncResult.errorCount === 0 && !syncResult.message && (
              <p className="text-gray-400">Inga nya dokument att indexera.</p>
            )}
          </div>
        )}
      </div>

      {/* Generate form */}
      <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-5">
        <h2 className="text-sm font-semibold text-gray-900 mb-4">Generera nytt material</h2>

        <form onSubmit={handleGenerate} className="space-y-3">
          <div>
            <label className="block text-xs font-medium text-gray-500 mb-1.5">Kurs-ID</label>
            <input
              className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm text-gray-900 placeholder-gray-300 focus:outline-none focus:border-blue-400"
              placeholder="t.ex. SYS25D"
              value={courseId}
              onChange={e => setCourseId(e.target.value)}
              required
            />
          </div>

          <div>
            <label className="block text-xs font-medium text-gray-500 mb-1.5">Instruktion</label>
            <textarea
              className="w-full border border-gray-200 rounded-lg px-3 py-2.5 text-sm text-gray-900 placeholder-gray-300 h-24 resize-none focus:outline-none focus:border-blue-400"
              placeholder="t.ex. Skapa 3 övningsuppgifter om riskanalys"
              value={instruction}
              onChange={e => setInstruction(e.target.value)}
              required
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            className="w-full bg-blue-600 text-white text-sm font-medium py-2.5 rounded-lg hover:bg-blue-700 disabled:opacity-50 transition-colors"
          >
            {loading ? 'Genererar…' : 'Generera material'}
          </button>
        </form>

        {result && (
          <div className="mt-4 pt-4 border-t border-gray-100">
            <div className="flex items-center justify-between mb-2">
              <p className="text-xs font-medium text-gray-500">Resultat</p>
              <div className="flex items-center gap-3">
                {result.generatedAt && (
                  <p className="text-xs text-gray-400">{result.generatedAt}</p>
                )}
                {result.savedAs && (
                  <button
                    onClick={handleSave}
                    disabled={saving || !isDirty}
                    className={`text-xs font-medium px-3 py-1.5 rounded-lg transition-colors ${
                      saved
                        ? 'bg-green-50 text-green-700'
                        : isDirty
                          ? 'bg-blue-600 text-white hover:bg-blue-700'
                          : 'bg-gray-100 text-gray-400 cursor-default'
                    } disabled:opacity-50`}
                  >
                    {saving ? 'Sparar…' : saved ? '✓ Sparat' : 'Spara'}
                  </button>
                )}
              </div>
            </div>
            <textarea
              className="w-full border border-gray-200 rounded-lg px-3 py-2.5 text-sm text-gray-700 leading-relaxed resize-y focus:outline-none focus:border-blue-400"
              style={{ minHeight: '200px' }}
              value={editedContent}
              onChange={e => { setEditedContent(e.target.value); setSaved(false) }}
            />
          </div>
        )}
      </div>

      {/* History */}
      {history.length > 0 && (
        <div className="bg-white rounded-xl border border-gray-100 shadow-sm p-5">
          <h2 className="text-sm font-semibold text-gray-900 mb-3">Tidigare genererat</h2>
          <div className="space-y-1">
            {history.map(file => (
              <button
                key={file.fileName}
                onClick={() => handleLoadHistory(file.fileName)}
                className={`w-full text-left px-3 py-2.5 rounded-lg text-sm transition-colors ${
                  result?.savedAs === file.fileName
                    ? 'bg-blue-50 text-blue-700'
                    : 'text-gray-700 hover:bg-gray-50'
                }`}
              >
                <span className="font-medium">{file.fileName}</span>
                <span className="ml-2 text-xs text-gray-400">{file.generatedAt}</span>
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
