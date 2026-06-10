import { useState } from 'react'
import axios from 'axios'
import { useAuth } from '../context/AuthContext'

const API = 'http://localhost:5010/api'

export default function SubmitForm({ onSubmitted }) {
  const [form, setForm] = useState({ studentName: '', courseId: '', content: '' })
  const [loading, setLoading] = useState(false)
  const { token } = useAuth()
  const headers = { Authorization: `Bearer ${token}` }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setLoading(true)
    try {
      const { data } = await axios.post(`${API}/submissions`, form, { headers })
      await axios.post(`${API}/submissions/${data.id}/process`, {}, { headers })
      onSubmitted(data.id)
    } catch (err) {
      alert('Något gick fel: ' + err.message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="max-w-xl mx-auto bg-white rounded-xl shadow p-6 space-y-4">
      <h2 className="text-xl font-semibold">Ny inlämning</h2>
      <input
        className="w-full border rounded p-2"
        placeholder="Studentens namn"
        value={form.studentName}
        onChange={e => setForm({ ...form, studentName: e.target.value })}
        required
      />
      <input
        className="w-full border rounded p-2"
        placeholder="Kurs-ID (t.ex. SYS25D)"
        value={form.courseId}
        onChange={e => setForm({ ...form, courseId: e.target.value })}
        required
      />
      <textarea
        className="w-full border rounded p-2 h-40"
        placeholder="Klistra in inlämningens text här..."
        value={form.content}
        onChange={e => setForm({ ...form, content: e.target.value })}
        required
      />
      <button
        type="submit"
        disabled={loading}
        className="w-full bg-blue-600 text-white py-2 rounded hover:bg-blue-700 disabled:opacity-50"
      >
        {loading ? 'Bearbetar...' : 'Skicka för AI-granskning'}
      </button>
    </form>
  )
}
