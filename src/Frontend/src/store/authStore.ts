import { create } from 'zustand'
import { User } from '../types/auth'

interface AuthStore {
  user: User | null
  isAuthenticated: boolean
  login: (user: User) => void
  logout: () => void
  setUser: (user: User | null) => void
}

export const useAuthStore = create<AuthStore>((set) => ({
  user: null,
  isAuthenticated: false,

  login: (user) => {
    set({ user, isAuthenticated: true })
    // Tokens are in httpOnly cookies, handled by browser automatically
  },

  logout: () => {
    set({ user: null, isAuthenticated: false })
  },

  setUser: (user) => set({ user }),
}))
