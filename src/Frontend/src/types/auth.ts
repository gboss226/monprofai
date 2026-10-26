export interface User {
  id: string
  email: string
  fullName: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  accessToken: string
  refreshToken: string
  user: User
}

export interface RegisterRequest {
  email: string
  fullName: string
  password: string
}

export interface RegisterResponse {
  userId: string
  email: string
}
