import { useState, useEffect } from 'react'
import axios from 'axios'
import { useAuth } from '../context/AuthContext'

const API = 'http://localhost:5010/api'

export default function FeedbackView({ submissionId, onReset }) {
  const [draft, setDraft] = useState(null)
  const [feedback, setFeedback] = useState('')
  const [grade, setGrade] = useState('')
  const [saved, setSaved] = useState(false)
  const { token } = useAuth()
  const headers = { Authorization: `Bearer ${token}` }

  useEffect(() => {
    let attempts = 0
    const interval = setInterval(async () => {
      try {
        const { data } = await axios.get(`${API}/submissions/${submissionId}/feedback`, { headers })
        setDraft(data)
        setFeedback(data.aiFeedback)
        setGrade(data.aiGrade)
        clearInterval(interval)
      } catch {
        attempts++
        if (attempts >= 30) {
          clearInterval(interval)
          alert('Timeout – kunde inte hämta feedback efter 60 sekunder')
        }
      }
    }, 2000)
    return () => clearInterval(interval)
  }, [submissionId])

  const handleApprove = async () => {
    await axios.put(
      `${API}/submissions/${submissionId}/feedback`,
      { teacherFeedback: feedback, teacherGrade: grade },
      { headers }
    )
    setSaved(true)
  }

  if (!draft) return <p className="text-center mt-8">Laddar feedback...</p>

  return (
    <div className="max-w-xl mx-auto bg-white rounded-xl shadow p-6 space-y-4">
      <h2 className="text-xl font-semibold">AI-genererat feedbackutkast</h2>

      <div className="bg-gray-50 rounded p-3 text-sm text-gray-600">
        <strong>Sammanfattning:</strong> {draft.summary}
      </div>

      <div>
        <label className="block text-sm font-medium mb-1">Feedback (redigera vid behov)</label>
        <textarea
          className="w-full border rounded p-2 h-40"
          value={feedback}
          onChange={e => setFeedback(e.target.value)}
        />
      </div>

      <div>
        <label className="block text-sm font-medium mb-1">Betyg</label>
        <select
          className="w-full border rounded p-2"
          value={grade}
          onChange={e => setGrade(e.target.value)}
        >
          <option>IG</option>
          <option>G</option>
          <option>VG</option>
        </select>
      </div>

      {saved
        ? <p className="text-green-600 font-medium">✓ Feedback sparad!</p>
        : <button
            onClick={handleApprove}
            className="w-full bg-green-600 text-white py-2 rounded hover:bg-green-700"
          >
            Godkänn och spara
          </button>
      }

      <button onClick={onReset} className="w-full border py-2 rounded hover:bg-gray-50 text-sm">
        Ny inlämning
      </button>
    </div>
  )
}
