import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import SyncPanel from '../components/SyncPanel'
import MaterialGenerator from '../components/MaterialGenerator'

const tabs = [
  { id: 'inlamningar', label: 'Inlämningar' },
  { id: 'material',    label: 'Kursmaterial' },
]

export default function TeacherPage() {
  const [view, setView] = useState('inlamningar')
  const { logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = () => {
    logout()
    navigate('/')
  }

  return (
    <div className="min-h-screen bg-gray-50">

      {/* Top bar */}
      <header className="bg-white border-b border-gray-100">
        <div className="max-w-2xl mx-auto px-4 h-14 flex items-center justify-between">

          {/* Logo */}
          <div className="flex items-center gap-2">
            <div className="w-7 h-7 bg-blue-600 rounded-md flex items-center justify-center">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M9.5 2A2.5 2.5 0 0 1 12 4.5v15a2.5 2.5 0 0 1-4.96-.46 2.5 2.5 0 0 1-1.04-4.79A2.5 2.5 0 0 1 7 9.5a2.5 2.5 0 0 1 2.5-7.5z"/>
                <path d="M14.5 2A2.5 2.5 0 0 0 12 4.5v15a2.5 2.5 0 0 0 4.96-.46 2.5 2.5 0 0 0 1.04-4.79A2.5 2.5 0 0 0 17 9.5a2.5 2.5 0 0 0-2.5-7.5z"/>
              </svg>
            </div>
            <span className="text-sm font-semibold text-gray-900">TeacherAid</span>
          </div>

          {/* Right: badge + logout */}
          <div className="flex items-center gap-3">
            <span className="text-xs bg-blue-50 text-blue-700 font-medium px-2.5 py-1 rounded-full">
              Lärare
            </span>
            <button
              onClick={handleLogout}
              className="text-xs text-gray-400 hover:text-gray-600 transition-colors"
            >
              Logga ut
            </button>
          </div>
        </div>

        {/* Tab nav */}
        <div className="max-w-2xl mx-auto px-4 flex gap-1">
          {tabs.map(tab => (
            <button
              key={tab.id}
              onClick={() => setView(tab.id)}
              className={`px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
                view === tab.id
                  ? 'border-blue-600 text-blue-600'
                  : 'border-transparent text-gray-400 hover:text-gray-600'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </header>

      {/* Content */}
      <main className="max-w-2xl mx-auto px-4 py-6">
        {view === 'inlamningar' && <SyncPanel />}
        {view === 'material'    && <MaterialGenerator />}
      </main>
    </div>
  )
}
