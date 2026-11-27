export interface NotionMastery {
  notionId: string
  /** From 0 to 1. */
  score: number
  exercisesCount: number
  lastActivity: string
}

export interface RecentResult {
  exerciseId: string
  notionId: string
  notionTitle: string
  succeeded: boolean
  hintsUsed: number
  attempts: number
  masteryBefore: number
  masteryAfter: number
  completedAt: string
}

export interface Progress {
  masteries: NotionMastery[]
  recentResults: RecentResult[]
}

export interface MasteryLevel {
  label: string
  /** Tailwind classes for the bar and the badge. */
  bar: string
  badge: string
}

/** Same thresholds as the tutor's prompt (TutorPromptBuilder.MasteryLabel). */
export function masteryLevel(score: number | undefined): MasteryLevel {
  if (score === undefined) return { label: 'À découvrir', bar: 'bg-gray-300', badge: 'bg-gray-100 text-gray-600' }
  if (score < 0.4) return { label: 'Fragile', bar: 'bg-red-400', badge: 'bg-red-50 text-red-800' }
  if (score < 0.75) return { label: 'En cours', bar: 'bg-amber-400', badge: 'bg-amber-50 text-amber-800' }
  return { label: 'Maîtrisée', bar: 'bg-green-500', badge: 'bg-green-50 text-green-800' }
}

export const percent = (score: number) => `${Math.round(score * 100)} %`
