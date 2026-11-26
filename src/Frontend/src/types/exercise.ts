export type ExerciseStatus = 'ToSolve' | 'InProgress' | 'Solved'

export interface CreatedExercise {
  id: string
  notionId: string
  /** True when the model picked the notion. */
  notionDetected: boolean
}

export interface Exercise {
  id: string
  statement: string
  status: ExerciseStatus
  notionDetected: boolean
  createdAt: string
  notion: {
    id: string
    title: string
    chapterTitle: string
    schoolLevel: string
    subjectName: string
  }
}

export const STATUS_LABELS: Record<ExerciseStatus, string> = {
  ToSolve: 'À résoudre',
  InProgress: 'En cours',
  Solved: 'Résolu',
}

/** Matches the backend limit (ExerciseLimits.MaxStatementLength). */
export const MAX_STATEMENT_LENGTH = 4000
