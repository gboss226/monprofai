import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { completeOnboarding } from '../api/learnerProfile'
import { useAuthStore } from '../store/authStore'
import {
  GOALS,
  LEARNING_STYLES,
  SCHOOL_LEVELS,
  SUBJECTS,
} from '../types/learnerProfile'

const TOTAL_STEPS = 4

export function Onboarding() {
  const navigate = useNavigate()
  const setProfile = useAuthStore((state) => state.setProfile)

  const [step, setStep] = useState(0)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const [schoolLevel, setSchoolLevel] = useState('')
  const [strugglingSubjects, setStrugglingSubjects] = useState<string[]>([])
  const [goal, setGoal] = useState('')
  const [preferredLearningStyle, setPreferredLearningStyle] = useState('')

  const canGoNext = [
    schoolLevel !== '',
    true, // struggling subjects are optional
    goal !== '',
    preferredLearningStyle !== '',
  ][step]

  const toggleSubject = (subject: string) => {
    setStrugglingSubjects((prev) =>
      prev.includes(subject)
        ? prev.filter((s) => s !== subject)
        : [...prev, subject]
    )
  }

  const handleNext = () => {
    if (step < TOTAL_STEPS - 1) {
      setStep((s) => s + 1)
    } else {
      handleSubmit()
    }
  }

  const handleBack = () => {
    if (step > 0) setStep((s) => s - 1)
  }

  const handleSkip = () => {
    navigate('/dashboard')
  }

  const handleSubmit = async () => {
    setError('')
    setLoading(true)
    try {
      const profile = await completeOnboarding({
        schoolLevel,
        strugglingSubjects,
        goal,
        preferredLearningStyle,
      })
      setProfile(profile)
      navigate('/dashboard')
    } catch (err: any) {
      setError(err.response?.data?.message || "Une erreur est survenue")
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-600 to-blue-800 flex items-center justify-center">
      <div className="bg-white rounded-lg shadow-xl p-8 w-full max-w-lg">
        <div className="w-full bg-gray-200 rounded-full h-2 mb-8">
          <div
            className="bg-blue-600 h-2 rounded-full transition-all"
            style={{ width: `${((step + 1) / TOTAL_STEPS) * 100}%` }}
          />
        </div>

        {error && (
          <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
            {error}
          </div>
        )}

        {step === 0 && (
          <div>
            <h2 className="text-xl font-bold text-gray-900 mb-6">
              Dans quelle classe es-tu ?
            </h2>
            <div className="grid grid-cols-2 gap-3">
              {SCHOOL_LEVELS.map((level) => (
                <button
                  key={level.value}
                  type="button"
                  onClick={() => setSchoolLevel(level.value)}
                  className={`px-4 py-3 rounded-lg border-2 font-medium transition ${
                    schoolLevel === level.value
                      ? 'border-blue-600 bg-blue-50 text-blue-700'
                      : 'border-gray-300 text-gray-700 hover:border-gray-400'
                  }`}
                >
                  {level.label}
                </button>
              ))}
            </div>
          </div>
        )}

        {step === 1 && (
          <div>
            <h2 className="text-xl font-bold text-gray-900 mb-2">
              Dans quelles matières as-tu le plus de difficultés ?
            </h2>
            <p className="text-gray-500 mb-6">
              Facultatif — tu peux en choisir plusieurs
            </p>
            <div className="grid grid-cols-2 gap-3">
              {SUBJECTS.map((subject) => (
                <button
                  key={subject}
                  type="button"
                  onClick={() => toggleSubject(subject)}
                  className={`px-4 py-3 rounded-lg border-2 font-medium transition ${
                    strugglingSubjects.includes(subject)
                      ? 'border-blue-600 bg-blue-50 text-blue-700'
                      : 'border-gray-300 text-gray-700 hover:border-gray-400'
                  }`}
                >
                  {subject}
                </button>
              ))}
            </div>
          </div>
        )}

        {step === 2 && (
          <div>
            <h2 className="text-xl font-bold text-gray-900 mb-6">
              Quel est ton objectif ?
            </h2>
            <div className="flex flex-col gap-3">
              {GOALS.map((g) => (
                <button
                  key={g.value}
                  type="button"
                  onClick={() => setGoal(g.value)}
                  className={`px-4 py-3 rounded-lg border-2 font-medium text-left transition ${
                    goal === g.value
                      ? 'border-blue-600 bg-blue-50 text-blue-700'
                      : 'border-gray-300 text-gray-700 hover:border-gray-400'
                  }`}
                >
                  {g.label}
                </button>
              ))}
            </div>
          </div>
        )}

        {step === 3 && (
          <div>
            <h2 className="text-xl font-bold text-gray-900 mb-6">
              Comment préfères-tu apprendre ?
            </h2>
            <div className="flex flex-col gap-3">
              {LEARNING_STYLES.map((style) => (
                <button
                  key={style.value}
                  type="button"
                  onClick={() => setPreferredLearningStyle(style.value)}
                  className={`px-4 py-3 rounded-lg border-2 font-medium text-left transition ${
                    preferredLearningStyle === style.value
                      ? 'border-blue-600 bg-blue-50 text-blue-700'
                      : 'border-gray-300 text-gray-700 hover:border-gray-400'
                  }`}
                >
                  {style.label}
                </button>
              ))}
            </div>
          </div>
        )}

        <div className="flex items-center justify-between mt-8">
          <div className="flex gap-3">
            {step > 0 && (
              <button
                type="button"
                onClick={handleBack}
                className="px-4 py-2 text-gray-600 font-medium hover:text-gray-900"
              >
                Retour
              </button>
            )}
            <button
              type="button"
              onClick={handleSkip}
              className="px-4 py-2 text-gray-400 font-medium hover:text-gray-600"
            >
              Passer pour l'instant
            </button>
          </div>

          <button
            type="button"
            onClick={handleNext}
            disabled={!canGoNext || loading}
            className="bg-blue-600 text-white font-medium px-6 py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50 transition"
          >
            {loading
              ? 'Enregistrement...'
              : step === TOTAL_STEPS - 1
                ? 'Terminer'
                : 'Suivant'}
          </button>
        </div>
      </div>
    </div>
  )
}
