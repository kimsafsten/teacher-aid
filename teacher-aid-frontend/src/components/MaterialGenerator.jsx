import { useState } from 'react'
import axios from 'axios'
import { useAuth } from '../context/AuthContext'

const API = 'http://localhost:5010/api'

export default function MaterialGenerator() {
  const [courseId, setCourseId]         = useState('')
  const [instruction, setInstruction]   = useState('')
  const [result, setResult]             = useState(null)
  const [loading, setLoading]           = useState(false)
  const [syncing, setSyncing]           = useState(false)
  const [syncResult, setSyncResult]     = useState(null)
  const { token } = useAuth()
  const headers = { Authorization: `Bearer ${token}` }

  const handleSync = async () => {
    setSyncing(true)
    setSyncResult(null)
    try {
      const { data } = await axios.post(`${API}/sync/kursmaterial`, {}, { headers })
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
    try {
      const { data } = await axios.post(
        `${API}/qa/generate-material`,
        { courseId, instruction },
        { headers }
      )
      setResult(data.content)
    } catch (err) {
      setResult('Något gick fel: ' + err.message)
    } finally {
      setLoading(false)
    }
  }

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
            <p className="text-xs font-medium text-gray-500 mb-2">Resultat</p>
            <div className="bg-gray-50 rounded-lg p-3 text-sm text-gray-700 whitespace-pre-wrap leading-relaxed">
              {result}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
