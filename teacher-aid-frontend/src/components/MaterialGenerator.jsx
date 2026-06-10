import { useState } from 'react'
import axios from 'axios'
import { useAuth } from '../context/AuthContext'

const API = 'http://localhost:5010/api'

export default function MaterialGenerator() {
  const [courseId, setCourseId] = useState('PRJ23A')
  const [instruction, setInstruction] = useState('')
  const [result, setResult] = useState(null)
  const [loading, setLoading] = useState(false)
  const { token } = useAuth()
  const headers = { Authorization: `Bearer ${token}` }

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
    <form onSubmit={handleGenerate} className="max-w-xl mx-auto bg-white rounded-xl shadow p-6 space-y-4">
      <h2 className="text-xl font-semibold">Generera kursmaterial</h2>
      <input
        className="w-full border rounded p-2"
        placeholder="Kurs-ID (t.ex. PRJ23A)"
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
  )
}
