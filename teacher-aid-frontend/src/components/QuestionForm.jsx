import { useState } from 'react'
import axios from 'axios'

const API = 'http://localhost:5010/api'

export default function QuestionForm() {
  const [courseId, setCourseId] = useState('SYS25D')
  const [question, setQuestion] = useState('')
  const [answer, setAnswer] = useState(null)
  const [loading, setLoading] = useState(false)

  const handleAsk = async (e) => {
    e.preventDefault()
    setLoading(true)
    setAnswer(null)
    try {
      const { data } = await axios.post(`${API}/qa/ask`, { courseId, question })
      // Strip optional "S:" answer prefix from the LLM response.
      setAnswer(data.answer.replace(/^S:\s*/i, ''))
    } catch (err) {
      setAnswer('Något gick fel: ' + err.message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <form onSubmit={handleAsk} className="max-w-xl mx-auto bg-white rounded-xl shadow p-6 space-y-4">
      <h2 className="text-xl font-semibold">Ställ en kursfråga</h2>
      <input
        className="w-full border rounded p-2"
        placeholder="Kurs-ID (t.ex. SYS25D)"
        value={courseId}
        onChange={e => setCourseId(e.target.value)}
        required
      />
      <div className="relative">
        <textarea
          className="w-full border rounded p-2 h-24"
          placeholder="Din fråga..."
          value={question}
          onChange={e => setQuestion(e.target.value)}
          maxLength={400}
          required
        />
        <span className={`text-xs absolute bottom-2 right-2 ${question.length > 360 ? 'text-red-500' : 'text-gray-400'}`}>
          {question.length}/400
        </span>
      </div>
      <button
        type="submit"
        disabled={loading}
        className="w-full bg-blue-600 text-white py-2 rounded hover:bg-blue-700 disabled:opacity-50"
      >
        {loading ? 'Söker svar...' : 'Fråga'}
      </button>
      {answer && (
        <div className="bg-gray-50 rounded p-3 text-sm text-gray-700">
          <strong>Svar:</strong> {answer}
        </div>
      )}
    </form>
  )
}
