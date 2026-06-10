import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider, useAuth } from './context/AuthContext'
import LoginPage from './pages/LoginPage'
import StudentPage from './pages/StudentPage'
import TeacherPage from './pages/TeacherPage'

function ProtectedRoute({ children }) {
  const { token } = useAuth()
  return token ? children : <Navigate to="/login" replace />
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Navigate to="/student" replace />} />
          <Route path="/student" element={<StudentPage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/teacher" element={
            <ProtectedRoute><TeacherPage /></ProtectedRoute>
          } />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
