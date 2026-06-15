import { useNavigate } from 'react-router-dom'

export default function LandingPage() {
  const navigate = useNavigate()

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col items-center justify-center p-8">

      {/* Logo */}
      <div className="flex items-center gap-3 mb-3">
        <div className="w-9 h-9 bg-blue-600 rounded-lg flex items-center justify-center">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d="M9.5 2A2.5 2.5 0 0 1 12 4.5v15a2.5 2.5 0 0 1-4.96-.46 2.5 2.5 0 0 1-1.04-4.79A2.5 2.5 0 0 1 7 9.5a2.5 2.5 0 0 1 2.5-7.5z"/>
            <path d="M14.5 2A2.5 2.5 0 0 0 12 4.5v15a2.5 2.5 0 0 0 4.96-.46 2.5 2.5 0 0 0 1.04-4.79A2.5 2.5 0 0 0 17 9.5a2.5 2.5 0 0 0-2.5-7.5z"/>
          </svg>
        </div>
        <span className="text-xl font-semibold text-gray-900">TeacherAid</span>
      </div>

      {/* Tagline */}
      <p className="text-sm text-gray-500 mb-10">AI-stöd för feedback och kursfrågor</p>

      {/* Cards */}
      <div className="grid grid-cols-2 gap-4 w-full max-w-md">
        <button
          onClick={() => navigate('/login')}
          className="bg-white border border-gray-200 rounded-xl p-6 flex flex-col items-center gap-3 hover:border-blue-300 hover:shadow-sm transition-all text-left"
        >
          <div className="w-11 h-11 rounded-full bg-blue-50 flex items-center justify-center">
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="#1d4ed8" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
              <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/>
              <circle cx="12" cy="7" r="4"/>
              <polyline points="16 11 18 13 22 9"/>
            </svg>
          </div>
          <div className="text-center">
            <p className="font-medium text-gray-900 text-sm">Jag är lärare</p>
            <p className="text-xs text-gray-400 mt-1 leading-relaxed">Hantera inlämningar och feedback</p>
          </div>
          <span className="text-xs text-blue-600 font-medium mt-1">Logga in →</span>
        </button>

        <button
          onClick={() => navigate('/student')}
          className="bg-white border border-gray-200 rounded-xl p-6 flex flex-col items-center gap-3 hover:border-green-300 hover:shadow-sm transition-all text-left"
        >
          <div className="w-11 h-11 rounded-full bg-green-50 flex items-center justify-center">
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="#15803d" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
              <path d="M22 10v6M2 10l10-5 10 5-10 5z"/>
              <path d="M6 12v5c3 3 9 3 12 0v-5"/>
            </svg>
          </div>
          <div className="text-center">
            <p className="font-medium text-gray-900 text-sm">Jag är elev</p>
            <p className="text-xs text-gray-400 mt-1 leading-relaxed">Ställ frågor om kursmaterial</p>
          </div>
          <span className="text-xs text-green-700 font-medium mt-1">Till kursassistenten →</span>
        </button>
      </div>

      <p className="text-xs text-gray-300 mt-12">Yrkesakademin</p>
    </div>
  )
}
