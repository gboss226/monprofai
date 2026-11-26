import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { isAxiosError } from 'axios'
import { getExercise } from '../api/exercises'
import { MathMarkdown } from '../components/MathMarkdown'
import { Exercise, STATUS_LABELS } from '../types/exercise'
import { SCHOOL_LEVELS } from '../types/learnerProfile'

/** Resolution screen of an exercise: the statement, its notion, and the way into the guided resolution. */
export function ExerciseSolve() {
  const { exerciseId } = useParams<{ exerciseId: string }>()
  const navigate = useNavigate()
  const [exercise, setExercise] = useState<Exercise | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!exerciseId) return
    getExercise(exerciseId)
      .then(setExercise)
      .catch((err) =>
        setError(
          isAxiosError(err) && err.response?.status === 404
            ? 'Cet exercice est introuvable.'
            : "Impossible de charger l'exercice. Recharge la page."
        )
      )
  }, [exerciseId])

  const startWithTutor = () => {
    if (!exercise) return
    // The tutor's prompt forbids doing the exercise for the student: it guides him step by step.
    navigate(`/tutor/${exercise.notion.id}`, {
      state: {
        initialMessage: `Voici mon exercice :\n\n${exercise.statement}\n\nAide-moi à le résoudre étape par étape, sans me donner la réponse.`,
      },
    })
  }

  const level = exercise ? SCHOOL_LEVELS.find((l) => l.value === exercise.notion.schoolLevel)?.label : null

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white shadow-sm">
        <div className="max-w-3xl mx-auto px-4 py-3 flex items-center gap-3">
          <Link
            to="/dashboard"
            className="text-gray-500 hover:text-gray-800 p-2 -ml-2 rounded"
            aria-label="Retour au tableau de bord"
          >
            ←
          </Link>
          <h1 className="text-lg font-semibold text-gray-900">Résolution de l'exercice</h1>
        </div>
      </header>

      <main className="max-w-3xl mx-auto px-4 py-6 space-y-5">
        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
            {error}{' '}
            <Link to="/exercises/new" className="underline font-medium">
              Saisir un exercice
            </Link>
          </div>
        )}

        {!exercise && !error && <p className="text-gray-500">Chargement de l'exercice…</p>}

        {exercise && (
          <>
            <section className="bg-white rounded-lg border border-gray-200 shadow-sm p-4" aria-labelledby="notion-title">
              <div className="flex flex-wrap items-center gap-2 mb-1">
                <span className="text-xs text-gray-500">
                  {exercise.notion.subjectName} · {level} · {exercise.notion.chapterTitle}
                </span>
                <span className="text-xs rounded-full bg-amber-50 text-amber-800 border border-amber-200 px-2 py-0.5">
                  {STATUS_LABELS[exercise.status]}
                </span>
              </div>
              <h2 id="notion-title" className="font-semibold text-gray-900">
                {exercise.notion.title}
              </h2>
              {exercise.notionDetected && (
                <p className="text-sm text-gray-500 mt-1">Notion trouvée par ton prof IA à partir de l'énoncé.</p>
              )}
            </section>

            <section className="bg-white rounded-lg border border-gray-200 shadow-sm p-4" aria-labelledby="statement-title">
              <h2 id="statement-title" className="text-sm font-semibold uppercase tracking-wide text-gray-500 mb-2">
                Énoncé
              </h2>
              <div className="relative text-gray-900">
                <MathMarkdown content={exercise.statement} />
              </div>
            </section>

            <section className="bg-blue-50 border border-blue-200 rounded-lg p-4">
              <h2 className="font-semibold text-blue-900 mb-1">Résoudre avec ton prof IA</h2>
              <p className="text-sm text-blue-900 mb-3">
                Il ne te donnera pas la réponse : il t'aide à trouver chaque étape toi-même.
              </p>
              <button
                onClick={startWithTutor}
                className="h-11 px-5 rounded-full bg-blue-600 text-white font-medium hover:bg-blue-700"
              >
                Commencer la résolution
              </button>
            </section>
          </>
        )}
      </main>
    </div>
  )
}
