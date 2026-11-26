import { create } from 'zustand'
import { getCurrentUser } from '../api/auth'
import { User } from '../types/auth'
import { LearnerProfile } from '../types/learnerProfile'

// 'unknown' until the server has been asked whether the session cookie is still valid.
export type AuthStatus = 'unknown' | 'authenticated' | 'anonymous'

interface AuthStore {
  user: User | null
  isAuthenticated: boolean
  status: AuthStatus
  profile: LearnerProfile | null
  login: (user: User) => void
  logout: () => void
  setUser: (user: User | null) => void
  setProfile: (profile: LearnerProfile | null) => void
  restoreSession: () => Promise<void>
}

let pendingRestore: Promise<void> | null = null

export const useAuthStore = create<AuthStore>((set, get) => ({
  user: null,
  isAuthenticated: false,
  status: 'unknown',
  profile: null,

  login: (user) => {
    // Tokens are in httpOnly cookies, handled by browser automatically
    set({ user, isAuthenticated: true, status: 'authenticated' })
  },

  logout: () => {
    set({ user: null, isAuthenticated: false, status: 'anonymous', profile: null })
  },

  setUser: (user) => set({ user }),
  setProfile: (profile) => set({ profile }),

  // The store lives in memory only, so after a page reload we ask the backend
  // who the cookie belongs to. Concurrent callers share the same request.
  restoreSession: () => {
    if (get().status !== 'unknown') return Promise.resolve()
    if (!pendingRestore) {
      pendingRestore = getCurrentUser()
        .then((user) => {
          if (get().status === 'unknown') get().login(user)
        })
        .catch(() => {
          // A login that completed while we were waiting wins over a stale 401.
          if (get().status === 'unknown') set({ status: 'anonymous' })
        })
        .finally(() => {
          pendingRestore = null
        })
    }
    return pendingRestore
  },
}))
