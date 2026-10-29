export interface LearnerProfile {
  id: string
  userId: string
  schoolLevel: string
  strugglingSubjects: string[]
  goal: string | null
  preferredLearningStyle: string | null
  learningObjectives: string | null
  onboardingCompleted: boolean
  updatedAt: string
}

export interface CompleteOnboardingRequest {
  schoolLevel: string
  strugglingSubjects: string[]
  goal: string
  preferredLearningStyle: string
}

export const SCHOOL_LEVELS: { value: string; label: string }[] = [
  { value: 'Sixieme', label: '6ème' },
  { value: 'Cinquieme', label: '5ème' },
  { value: 'Quatrieme', label: '4ème' },
  { value: 'Troisieme', label: '3ème' },
  { value: 'Seconde', label: '2nde' },
  { value: 'Premiere', label: '1ère' },
  { value: 'Terminale', label: 'Terminale' },
]

export const SUBJECTS: string[] = [
  'Mathématiques',
  'Français',
  'Histoire-Géographie',
  'Anglais',
  'SVT',
  'Physique-Chimie',
  'Technologie',
  'Autre',
]

export const GOALS: { value: string; label: string }[] = [
  { value: 'Brevet', label: 'Réussir le brevet' },
  { value: 'Bac', label: 'Réussir le bac' },
  { value: 'ImproveGrades', label: 'Remonter mes notes' },
  { value: 'Other', label: 'Autre' },
]

export const LEARNING_STYLES: { value: string; label: string }[] = [
  { value: 'ConcreteExamples', label: 'Avec des exemples concrets' },
  { value: 'Diagrams', label: 'Avec des schémas' },
  { value: 'ShortExplanations', label: 'Des explications courtes' },
  { value: 'DetailedExplanations', label: 'Des explications détaillées' },
]
