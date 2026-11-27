export type ExerciseStatus = 'ToSolve' | 'InProgress' | 'Solved'

export interface CreatedExercise {
  id: string
  notionId: string
  /** True when the model picked the notion. */
  notionDetected: boolean
}

export interface ExerciseHint {
  /** 1 = notion reminder, 2 = method, 3 = first step, 4 = full solution. */
  level: number
  title: string
  content: string
}

export interface ExerciseAttempt {
  id: string
  answer: string
  isCorrect: boolean
  feedback: string
  createdAt: string
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
  hints: ExerciseHint[]
  attempts: ExerciseAttempt[]
  /** Level offered next, null when every hint is unlocked. */
  nextHintLevel: number | null
  /** Why the solution is still locked, null when it can be (or is) unlocked. */
  solutionLockedReason: string | null
}

export const SOLUTION_LEVEL = 4

export const HINT_TITLES: Record<number, string> = {
  1: 'Rappel de la notion',
  2: 'Méthode',
  3: 'Première étape',
  4: 'Solution complète',
}

/** Matches the backend limit (HintLevels.MaxAnswerLength). */
export const MAX_ANSWER_LENGTH = 2000

export const STATUS_LABELS: Record<ExerciseStatus, string> = {
  ToSolve: 'À résoudre',
  InProgress: 'En cours',
  Solved: 'Résolu',
}

/** Matches the backend limit (ExerciseLimits.MaxStatementLength). */
export const MAX_STATEMENT_LENGTH = 4000
