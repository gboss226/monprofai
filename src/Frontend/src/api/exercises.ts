import { isAxiosError } from 'axios'
import client from './client'
import { CreatedExercise, Exercise } from '../types/exercise'

export class ExerciseError extends Error {
  constructor(
    message: string,
    public readonly code?: string
  ) {
    super(message)
    this.name = 'ExerciseError'
  }
}

/**
 * Saves an exercise. Without `notionId` the backend asks the model to detect the notion;
 * if it cannot, this throws an ExerciseError with code "NotionNotDetected" and nothing is saved.
 */
export async function createExercise(statement: string, notionId?: string): Promise<CreatedExercise> {
  try {
    const response = await client.post<CreatedExercise>('/exercises', { statement, notionId })
    return response.data
  } catch (error) {
    if (isAxiosError(error) && error.response?.data?.message) {
      throw new ExerciseError(error.response.data.message, error.response.data.code)
    }
    if (isAxiosError(error) && error.response?.status === 429) {
      throw new ExerciseError('Tu as envoyé beaucoup de demandes. Réessaie dans quelques minutes.', 'RateLimited')
    }
    throw new ExerciseError("L'exercice n'a pas pu être enregistré. Vérifie ta connexion et réessaie.")
  }
}

/** One of the student's exercises. Rejects with a 404 for anyone else's. */
export async function getExercise(id: string): Promise<Exercise> {
  const response = await client.get<Exercise>(`/exercises/${id}`)
  return response.data
}
