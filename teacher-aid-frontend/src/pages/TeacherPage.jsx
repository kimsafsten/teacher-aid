import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import SyncPanel from '../components/SyncPanel'
import MaterialGenerator from '../components/MaterialGenerator'

export default function TeacherPage() {
  const [view, setView] = useState('inlamningar')
  const { logout } = useAuth()
  const navigate = useNavigate()

  const tabs = [
    { id: 'inlamningar', label: 'Inlämningar' },
    { id: 'material', label: 'Kursmaterial' },
  ]

  return (
    <div className="min-h-screen bg-gray-100 p-8">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">Lärarens AI-verktyg</h1>
        <button onClick={() => { logout(); navigate('/login') }} className="text-sm text-gray-500 hover:text-gray-800">
          Logga ut
        </button>
      </div>

      <div className="flex justify-center gap-4 mb-8">
        {tabs.map(tab => (
          <button
            key={tab.id}
            onClick={() => setView(tab.id)}
            className={`px-4 py-2 rounded ${view === tab.id ? 'bg-blue-600 text-white' : 'bg-white border'}`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {view === 'inlamningar' && <SyncPanel />}
      {view === 'material' && <MaterialGenerator />}
    </div>
  )
}
