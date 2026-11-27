import { Link } from 'react-router-dom'
import { Exercise } from '../types/exercise'
import { percent } from '../types/progress'

/** Shown once the exercise is over: how it went and how it moved the notion's mastery. */
export function ExerciseResultBanner({ exercise }: { exercise: Exercise }) {
  const result = exercise.result
  if (!result) return null

  const rose = result.masteryAfter >= result.masteryBefore
  return (
    <section
      className={`rounded-lg border px-4 py-3 ${rose ? 'border-green-200 bg-green-50' : 'border-amber-200 bg-amber-50'}`}
      aria-label="Résultat de l'exercice"
    >
      <p className="font-medium text-gray-900">
        {result.succeeded
          ? result.hintsUsed === 0
            ? 'Réussi sans indice, bravo !'
            : `Réussi avec ${result.hintsUsed} indice${result.hintsUsed > 1 ? 's' : ''}.`
          : 'Tu as regardé la solution : relis-la bien, puis essaie un exercice du même type.'}
      </p>
      <p className="text-sm text-gray-700 mt-1">
        Ta maîtrise de « {exercise.notion.title} » : {percent(result.masteryBefore)} → <strong>{percent(result.masteryAfter)}</strong>
      </p>
      <Link to="/progress" className="inline-block text-sm font-medium text-blue-700 hover:underline mt-1">
        Voir ma progression
      </Link>
    </section>
  )
}
