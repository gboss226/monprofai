import { lazy, Suspense } from 'react'
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom'
import { Login } from './pages/Login'
import { Register } from './pages/Register'
import { Dashboard } from './pages/Dashboard'
import { Onboarding } from './pages/Onboarding'
import { ProtectedRoute } from './components/ProtectedRoute'
import { TutorPicker } from './pages/TutorPicker'

// Loaded on demand: Markdown + KaTeX weigh more than the rest of the app.
const TutorChat = lazy(() => import('./pages/TutorChat').then((m) => ({ default: m.TutorChat })))

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route
          path="/onboarding"
          element={
            <ProtectedRoute>
              <Onboarding />
            </ProtectedRoute>
          }
        />
        <Route
          path="/dashboard"
          element={
            <ProtectedRoute>
              <Dashboard />
            </ProtectedRoute>
          }
        />
        <Route
          path="/tutor"
          element={
            <ProtectedRoute>
              <TutorPicker />
            </ProtectedRoute>
          }
        />
        <Route
          path="/tutor/:notionId"
          element={
            <ProtectedRoute>
              <Suspense fallback={<p className="p-6 text-gray-500">Chargement…</p>}>
                <TutorChat />
              </Suspense>
            </ProtectedRoute>
          }
        />
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </Router>
  )
}

export default App
