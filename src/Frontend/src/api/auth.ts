import client from './client'
import { User } from '../types/auth'

// Returns the user owning the session cookie; rejects (401) when there is none.
// skipAuthRedirect: an expected 401 here must not trigger the global redirect.
export async function getCurrentUser(): Promise<User> {
  const response = await client.get<User>('/auth/me', { skipAuthRedirect: true })
  return response.data
}
