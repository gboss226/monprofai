import { isAxiosError } from 'axios'
import client from './client'
import { CreatedExercise, Exercise, ExerciseAttempt, ExerciseHint } from '../types/exercise'

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

/**
 * Sends a photo of an exercise and returns the statement read from it, for the student to check.
 * Nothing is saved on the server, the photo included.
 */
export async function transcribeExercise(photo: Blob): Promise<string> {
  const form = new FormData()
  form.append('image', photo, 'exercice.jpg')
  try {
    const response = await client.post<{ statement: string }>('/exercises/transcribe', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
      // Reading a photo takes the model a few seconds more than a text answer.
      timeout: 90_000,
    })
    return response.data.statement
  } catch (error) {
    if (isAxiosError(error) && error.response?.data?.message) {
      throw new ExerciseError(error.response.data.message, error.response.data.code)
    }
    if (isAxiosError(error) && error.response?.status === 429) {
      throw new ExerciseError('Tu as envoyé beaucoup de demandes. Réessaie dans quelques minutes.', 'RateLimited')
    }
    throw new ExerciseError("La photo n'a pas pu être envoyée. Vérifie ta connexion et réessaie.")
  }
}

function toExerciseError(error: unknown, fallback: string): ExerciseError {
  if (isAxiosError(error) && error.response?.data?.message) {
    return new ExerciseError(error.response.data.message, error.response.data.code)
  }
  if (isAxiosError(error) && error.response?.status === 429) {
    return new ExerciseError('Tu as envoyé beaucoup de demandes. Réessaie dans quelques minutes.', 'RateLimited')
  }
  return new ExerciseError(fallback)
}

/** Unlocks the hint of `level`. Rejects with code "HintLocked" when the rules forbid it for now. */
export async function unlockHint(exerciseId: string, level: number): Promise<ExerciseHint> {
  try {
    const response = await client.post<ExerciseHint>(`/exercises/${exerciseId}/hints`, { level }, { timeout: 90_000 })
    return response.data
  } catch (error) {
    throw toExerciseError(error, "L'indice n'a pas pu être chargé. Réessaie.")
  }
}

/** Has the model check an answer: right, or where the mistake is, without giving the solution. */
export async function submitAnswer(exerciseId: string, answer: string): Promise<ExerciseAttempt> {
  try {
    const response = await client.post<ExerciseAttempt>(`/exercises/${exerciseId}/attempts`, { answer }, { timeout: 90_000 })
    return response.data
  } catch (error) {
    throw toExerciseError(error, "Ta réponse n'a pas pu être vérifiée. Réessaie.")
  }
}

/** One of the student's exercises. Rejects with a 404 for anyone else's. */
export async function getExercise(id: string): Promise<Exercise> {
  const response = await client.get<Exercise>(`/exercises/${id}`)
  return response.data
}
