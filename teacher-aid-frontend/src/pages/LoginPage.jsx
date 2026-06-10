import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import axios from 'axios'
import { useAuth } from '../context/AuthContext'

const API = 'http://localhost:5010/api'

export default function LoginPage() {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState(null)
  const { login } = useAuth()
  const navigate = useNavigate()

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)
    try {
      const { data } = await axios.post(`${API}/auth/login`, { username, password })
      login(data.token)
      navigate('/teacher')
    } catch {
      setError('Fel användarnamn eller lösenord')
    }
  }

  return (
    <div className="min-h-screen bg-gray-100 flex items-center justify-center">
      <form onSubmit={handleSubmit} className="bg-white rounded-xl shadow p-8 w-full max-w-sm space-y-4">
        <h1 className="text-2xl font-bold text-center">Lärarinloggning</h1>
        <input
          className="w-full border rounded p-2"
          placeholder="Användarnamn"
          value={username}
          onChange={e => setUsername(e.target.value)}
          required
        />
        <input
          type="password"
          className="w-full border rounded p-2"
          placeholder="Lösenord"
          value={password}
          onChange={e => setPassword(e.target.value)}
          required
        />
        {error && <p className="text-red-500 text-sm">{error}</p>}
        <button type="submit" className="w-full bg-blue-600 text-white py-2 rounded hover:bg-blue-700">
          Logga in
        </button>
      </form>
    </div>
  )
}
