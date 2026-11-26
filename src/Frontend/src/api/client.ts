import axios, { AxiosInstance } from 'axios'

declare module 'axios' {
  interface AxiosRequestConfig {
    // Set on requests whose 401 is handled by the caller (e.g. the session check).
    skipAuthRedirect?: boolean
  }
}

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api'

const client: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true, // Send cookies with requests
})

client.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401 && !error.config?.skipAuthRedirect) {
      // Clear auth state and redirect to login
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

export default client
