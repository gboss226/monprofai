import { useEffect } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import client from '../api/client'
import { getMyProfile } from '../api/learnerProfile'
import { useAuthStore } from '../store/authStore'

export function Dashboard() {
  const user = useAuthStore((state) => state.user)
  const profile = useAuthStore((state) => state.profile)
  const setProfile = useAuthStore((state) => state.setProfile)
  const logout = useAuthStore((state) => state.logout)
  const navigate = useNavigate()

  useEffect(() => {
    if (!profile) {
      getMyProfile()
        .then(setProfile)
        .catch(() => {
          // profile fetch failure is non-blocking for the dashboard
        })
    }
  }, [profile, setProfile])

  const handleLogout = async () => {
    try {
      await client.post('/auth/logout')
    } catch (err) {
      console.error('Logout error:', err)
    }
    logout()
    navigate('/login')
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 py-4 flex justify-between items-center">
          <h1 className="text-2xl font-bold text-blue-600">MonProfAI</h1>
          <div className="flex items-center gap-4">
            <span className="text-gray-700">Bienvenue, {user?.fullName}</span>
            <button
              onClick={handleLogout}
              className="bg-red-600 text-white px-4 py-2 rounded hover:bg-red-700 transition"
            >
              Déconnexion
            </button>
          </div>
        </div>
      </nav>

      <main className="max-w-7xl mx-auto px-4 py-8">
        {profile && !profile.onboardingCompleted && (
          <div className="bg-blue-50 border border-blue-200 text-blue-800 px-4 py-3 rounded mb-6 flex items-center justify-between">
            <span>Complète ton profil pour une expérience personnalisée.</span>
            <Link
              to="/onboarding"
              className="font-medium text-blue-700 hover:underline whitespace-nowrap ml-4"
            >
              Compléter mon profil
            </Link>
          </div>
        )}

        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-xl font-semibold text-gray-900 mb-4">Mes cours</h2>
            <p className="text-gray-600">
              Vos cours apparaîtront ici. Commencez à explorer nos contenus!
            </p>
          </div>

          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-xl font-semibold text-gray-900 mb-4">Progression</h2>
            <div className="w-full bg-gray-200 rounded-full h-2">
              <div className="bg-blue-600 h-2 rounded-full" style={{ width: '0%' }}></div>
            </div>
            <p className="text-gray-600 mt-2">0% complété</p>
          </div>

          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-xl font-semibold text-gray-900 mb-4">Notifications</h2>
            <p className="text-gray-600">Aucune notification pour le moment</p>
          </div>
        </div>
      </main>
    </div>
  )
}
