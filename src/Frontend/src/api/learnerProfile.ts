import client from './client'
import { CompleteOnboardingRequest, LearnerProfile } from '../types/learnerProfile'

export async function getMyProfile(): Promise<LearnerProfile> {
  const response = await client.get<LearnerProfile>('/learnerprofile/me')
  return response.data
}

export async function completeOnboarding(
  payload: CompleteOnboardingRequest
): Promise<LearnerProfile> {
  const response = await client.post<LearnerProfile>('/learnerprofile/onboarding', payload)
  return response.data
}
