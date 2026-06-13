import { useState } from 'react'
import axios from 'axios'
import { useAuth } from '../context/AuthContext'

const API = 'http://localhost:5010/api'

export default function MaterialGenerator() {
  const [courseId, setCourseId] = useState('')
  const [instruction, setInstruction] = useState('')
  const [result, setResult] = useState(null)
  const [loading, setLoading] = useState(false)
  const [syncing, setSyncing] = useState(false)
  const [syncResult, setSyncResult] = useState(null)
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
    <div className="max-w-xl mx-auto space-y-6">

      {/* Synka kursmaterial */}
      <div className="bg-white rounded-xl shadow p-6">
        <div className="flex items-center justify-between mb-2">
          <div>
            <h2 className="text-lg font-semibold">Synkronisera kursmaterial</h2>
            <p className="text-sm text-gray-500 mt-1">
              Lägg filer i mappen <code className="bg-gray-100 px-1 rounded">kursmaterial/</code> och synka för att indexera dem för RAG.
            </p>
          </div>
          <button
            onClick={handleSync}
            disabled={syncing}
            className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 disabled:opacity-50 whitespace-nowrap"
          >
            {syncing ? 'Synkroniserar...' : '↻ Synka nu'}
          </button>
        </div>

        {syncResult && (
          <div className="mt-3 text-sm space-y-1">
            {syncResult.message && <p className="text-gray-600">{syncResult.message}</p>}
            {syncResult.processedCount > 0 && (
              <p className="text-green-700">✓ {syncResult.processedCount} dokument indexerade</p>
            )}
            {syncResult.processed?.map(f => (
              <p key={f} className="text-gray-500 pl-4">– {f}</p>
            ))}
            {syncResult.errors?.map(e => (
              <p key={e} className="text-red-600 pl-4">✗ {e}</p>
            ))}
            {syncResult.processedCount === 0 && syncResult.errorCount === 0 && !syncResult.message && (
              <p className="text-gray-500">Inga nya dokument att indexera.</p>
            )}
          </div>
        )}
      </div>

      {/* Generera kursmaterial */}
      <form onSubmit={handleGenerate} className="bg-white rounded-xl shadow p-6 space-y-4">
        <h2 className="text-lg font-semibold">Generera nytt material</h2>
        <input
          className="w-full border rounded p-2"
          placeholder="Kurs-ID (t.ex. SYS25D)"
          value={courseId}
          onChange={e => setCourseId(e.target.value)}
          required
        />
        <textarea
          className="w-full border rounded p-2 h-24"
          placeholder="Instruktion, t.ex. 'Skapa 3 övningsuppgifter om riskanalys'"
          value={instruction}
          onChange={e => setInstruction(e.target.value)}
          required
        />
        <button
          type="submit"
          disabled={loading}
          className="w-full bg-purple-600 text-white py-2 rounded hover:bg-purple-700 disabled:opacity-50"
        >
          {loading ? 'Genererar...' : 'Generera'}
        </button>
        {result && (
          <div className="bg-gray-50 rounded p-3 text-sm text-gray-700 whitespace-pre-wrap">
            {result}
          </div>
        )}
      </form>
    </div>
  )
}
