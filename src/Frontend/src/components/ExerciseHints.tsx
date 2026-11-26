import { useState } from 'react'
import { ExerciseError, unlockHint } from '../api/exercises'
import { Exercise, ExerciseHint, HINT_TITLES, SOLUTION_LEVEL } from '../types/exercise'
import { MathMarkdown } from './MathMarkdown'

interface Props {
  exercise: Exercise
  /** Called with each newly unlocked hint; the parent reloads the exercise to refresh the rules. */
  onUnlocked: (hint: ExerciseHint) => void
}

/**
 * The four hints, unlocked one by one. Unlocked hints stay displayed in order. The page only mirrors the
 * rules: the server decides (order, solution after three hints or three wrong answers).
 */
export function ExerciseHints({ exercise, onUnlocked }: Props) {
  const [loadingLevel, setLoadingLevel] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [confirmSolution, setConfirmSolution] = useState(false)

  const unlocked = new Set(exercise.hints.map((h) => h.level))
  const next = exercise.nextHintLevel
  const solutionUnlocked = unlocked.has(SOLUTION_LEVEL)

  const unlock = async (level: number) => {
    setLoadingLevel(level)
    setError(null)
    try {
      onUnlocked(await unlockHint(exercise.id, level))
      setConfirmSolution(false)
    } catch (err) {
      setError(err instanceof ExerciseError ? err.message : "L'indice n'a pas pu être chargé.")
    } finally {
      setLoadingLevel(null)
    }
  }

  const busy = loadingLevel !== null

  return (
    <section className="bg-white rounded-lg border border-gray-200 shadow-sm p-4" aria-labelledby="hints-title">
      <h2 id="hints-title" className="text-sm font-semibold uppercase tracking-wide text-gray-500 mb-1">
        Indices
      </h2>
      <p className="text-sm text-gray-600 mb-3">
        Bloqué ? Débloque les indices un par un : chacun t'en dit un peu plus que le précédent.
      </p>

      {exercise.hints.length > 0 && (
        <ol className="space-y-3 mb-4">
          {exercise.hints.map((hint) => (
            <li
              key={hint.level}
              className={`rounded-lg border px-4 py-3 ${
                hint.level === SOLUTION_LEVEL ? 'border-green-200 bg-green-50' : 'border-blue-100 bg-blue-50'
              }`}
            >
              <p className="text-xs font-semibold uppercase tracking-wide text-gray-600 mb-1">
                {hint.level === SOLUTION_LEVEL ? hint.title : `Indice ${hint.level} · ${hint.title}`}
              </p>
              <div className="relative text-gray-900">
                <MathMarkdown content={hint.content} />
              </div>
            </li>
          ))}
        </ol>
      )}

      {loadingLevel !== null && (
        <p className="text-sm text-gray-600 mb-3" role="status">
          {loadingLevel === SOLUTION_LEVEL ? 'Rédaction de la solution…' : `Préparation de l'indice ${loadingLevel}…`}
        </p>
      )}

      <div className="flex flex-wrap gap-2">
        {next !== null && next < SOLUTION_LEVEL && (
          <button
            onClick={() => unlock(next)}
            disabled={busy}
            className="h-10 px-4 rounded-full bg-blue-600 text-white text-sm font-medium hover:bg-blue-700 disabled:opacity-40"
          >
            Indice {next} : {HINT_TITLES[next].toLowerCase()}
          </button>
        )}

        {!solutionUnlocked && !confirmSolution && (
          <button
            onClick={() => setConfirmSolution(true)}
            disabled={busy || exercise.solutionLockedReason !== null}
            className="h-10 px-4 rounded-full border border-gray-300 text-gray-700 text-sm font-medium hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <span aria-hidden>{exercise.solutionLockedReason ? '🔒 ' : ''}</span>Voir la solution complète
          </button>
        )}
      </div>

      {!solutionUnlocked && exercise.solutionLockedReason && (
        <p className="text-xs text-gray-500 mt-2">{exercise.solutionLockedReason}</p>
      )}

      {confirmSolution && (
        <div className="mt-3 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3">
          <p className="text-sm text-amber-900 mb-2">
            Tu es sûr ? Tu apprendras davantage en trouvant toi-même. Tu peux aussi proposer une réponse pour savoir
            où tu en es.
          </p>
          <div className="flex flex-wrap gap-2">
            <button
              onClick={() => unlock(SOLUTION_LEVEL)}
              disabled={busy}
              className="h-9 px-4 rounded-full bg-amber-600 text-white text-sm font-medium hover:bg-amber-700 disabled:opacity-40"
            >
              Oui, montre la solution
            </button>
            <button
              onClick={() => setConfirmSolution(false)}
              disabled={busy}
              className="h-9 px-4 rounded-full border border-amber-300 text-amber-900 text-sm font-medium"
            >
              Je continue à chercher
            </button>
          </div>
        </div>
      )}

      {error && (
        <p role="alert" className="text-sm text-red-700 mt-3">
          {error}
        </p>
      )}
    </section>
  )
}
