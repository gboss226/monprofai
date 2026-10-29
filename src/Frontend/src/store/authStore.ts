import { create } from 'zustand'
import { User } from '../types/auth'
import { LearnerProfile } from '../types/learnerProfile'

interface AuthStore {
  user: User | null
  isAuthenticated: boolean
  profile: LearnerProfile | null
  login: (user: User) => void
  logout: () => void
  setUser: (user: User | null) => void
  setProfile: (profile: LearnerProfile | null) => void
}

export const useAuthStore = create<AuthStore>((set) => ({
  user: null,
  isAuthenticated: false,
  profile: null,

  login: (user) => {
    set({ user, isAuthenticated: true })
    // Tokens are in httpOnly cookies, handled by browser automatically
  },

  logout: () => {
    set({ user: null, isAuthenticated: false, profile: null })
  },

  setUser: (user) => set({ user }),
  setProfile: (profile) => set({ profile }),
}))
