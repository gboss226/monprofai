import client from './client'
import { Progress } from '../types/progress'

/** The student's mastery per notion and latest finished exercises. */
export async function getProgress(): Promise<Progress> {
  const response = await client.get<Progress>('/progress')
  return response.data
}
