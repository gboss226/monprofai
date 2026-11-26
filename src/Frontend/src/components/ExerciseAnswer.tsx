import { FormEvent, useState } from 'react'
import { ExerciseError, submitAnswer } from '../api/exercises'
import { Exercise, ExerciseAttempt, MAX_ANSWER_LENGTH } from '../types/exercise'
import { MathMarkdown } from './MathMarkdown'

interface Props {
  exercise: Exercise
  onAnswered: (attempt: ExerciseAttempt) => void
}

/** The student proposes an answer at any time; the model says whether it is right, or where the mistake is. */
export function ExerciseAnswer({ exercise, onAnswered }: Props) {
  const [answer, setAnswer] = useState('')
  const [checking, setChecking] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const solved = exercise.status === 'Solved'

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!answer.trim() || checking) return
    setChecking(true)
    setError(null)
    try {
      onAnswered(await submitAnswer(exercise.id, answer))
      setAnswer('')
    } catch (err) {
      setError(err instanceof ExerciseError ? err.message : "Ta réponse n'a pas pu être vérifiée.")
    } finally {
      setChecking(false)
    }
  }

  return (
    <section className="bg-white rounded-lg border border-gray-200 shadow-sm p-4" aria-labelledby="answer-title">
      <h2 id="answer-title" className="text-sm font-semibold uppercase tracking-wide text-gray-500 mb-3">
        Ta réponse
      </h2>

      {solved && (
        <p className="rounded-lg bg-green-50 border border-green-200 text-green-900 px-4 py-3 mb-3 font-medium">
          🎉 Bravo, tu as résolu cet exercice !
        </p>
      )}

      {exercise.attempts.length > 0 && (
        <ol className="space-y-3 mb-4" aria-label="Tes réponses">
          {exercise.attempts.map((attempt, i) => (
            <li
              key={attempt.id}
              className={`rounded-lg border px-4 py-3 ${
                attempt.isCorrect ? 'border-green-200 bg-green-50' : 'border-red-100 bg-red-50'
              }`}
            >
              <p className="text-xs font-semibold uppercase tracking-wide text-gray-600 mb-1">
                Essai {i + 1} · {attempt.isCorrect ? '✅ Juste' : '❌ Pas encore'}
              </p>
              <p className="text-gray-900 whitespace-pre-wrap break-words mb-2">« {attempt.answer} »</p>
              <div className="relative text-gray-800 text-sm">
                <MathMarkdown content={attempt.feedback} />
              </div>
            </li>
          ))}
        </ol>
      )}

      {!solved && (
        <form onSubmit={onSubmit}>
          <label htmlFor="answer" className="sr-only">
            Ta réponse
          </label>
          <textarea
            id="answer"
            value={answer}
            onChange={(e) => setAnswer(e.target.value)}
            rows={3}
            maxLength={MAX_ANSWER_LENGTH}
            readOnly={checking}
            placeholder="Écris ta réponse, et ta démarche si tu veux savoir où est l'erreur."
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-base leading-6 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <div className="flex items-center justify-between gap-3 mt-2">
            <p className="text-xs text-gray-500" role="status">
              {checking ? 'Vérification de ta réponse…' : 'Tu peux proposer une réponse à tout moment.'}
            </p>
            <button
              type="submit"
              disabled={!answer.trim() || checking}
              className="h-10 px-5 rounded-full bg-blue-600 text-white text-sm font-medium hover:bg-blue-700 disabled:opacity-40 shrink-0"
            >
              Vérifier ma réponse
            </button>
          </div>
        </form>
      )}

      {error && (
        <p role="alert" className="text-sm text-red-700 mt-2">
          {error}
        </p>
      )}
    </section>
  )
}
