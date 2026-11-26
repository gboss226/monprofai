import { useEffect } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'

interface Props {
  children: React.ReactNode
}

export function ProtectedRoute({ children }: Props) {
  const status = useAuthStore((state) => state.status)
  const restoreSession = useAuthStore((state) => state.restoreSession)

  useEffect(() => {
    if (status === 'unknown') restoreSession()
  }, [status, restoreSession])

  if (status === 'unknown') {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50" role="status">
        <div className="flex items-center gap-3 text-gray-600">
          <span className="h-5 w-5 rounded-full border-2 border-blue-600 border-t-transparent animate-spin" />
          Chargement...
        </div>
      </div>
    )
  }

  if (status === 'anonymous') {
    return <Navigate to="/login" replace />
  }

  return <>{children}</>
}
