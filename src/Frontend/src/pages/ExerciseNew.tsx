import { FormEvent, useDeferredValue, useEffect, useMemo, useRef, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { createExercise, ExerciseError } from '../api/exercises'
import { getMyProfile } from '../api/learnerProfile'
import { getReferential } from '../api/notions'
import { MathMarkdown } from '../components/MathMarkdown'
import { useAuthStore } from '../store/authStore'
import { MAX_STATEMENT_LENGTH } from '../types/exercise'
import { SCHOOL_LEVELS } from '../types/learnerProfile'
import { SubjectDto } from '../types/tutor'

const levelLabel = (value: string) => SCHOOL_LEVELS.find((l) => l.value === value)?.label ?? value
const levelRank = (value: string) => SCHOOL_LEVELS.findIndex((l) => l.value === value)

const AUTO = ''

/** The student types or pastes an exercise statement, optionally names its notion, then goes on to solve it. */
export function ExerciseNew() {
  const navigate = useNavigate()
  const profile = useAuthStore((state) => state.profile)
  const setProfile = useAuthStore((state) => state.setProfile)

  const [statement, setStatement] = useState('')
  const preview = useDeferredValue(statement)
  const [subjects, setSubjects] = useState<SubjectDto[] | null>(null)
  const [level, setLevel] = useState<string | null>(null)
  const [notionId, setNotionId] = useState(AUTO)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [mustChooseNotion, setMustChooseNotion] = useState(false)
  const notionSelectRef = useRef<HTMLSelectElement>(null)

  useEffect(() => {
    getReferential()
      .then(setSubjects)
      .catch(() => setSubjects([]))
    if (!profile) getMyProfile().then(setProfile).catch(() => {})
  }, [profile, setProfile])

  const chapters = useMemo(() => subjects?.flatMap((s) => s.chapters) ?? [], [subjects])
  const levels = useMemo(
    () => [...new Set(chapters.map((c) => c.schoolLevel))].sort((a, b) => levelRank(a) - levelRank(b)),
    [chapters]
  )
  const activeLevel =
    level ?? (profile && levels.includes(profile.schoolLevel) ? profile.schoolLevel : levels[0] ?? null)

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!statement.trim() || submitting) return
    if (mustChooseNotion && notionId === AUTO) {
      notionSelectRef.current?.focus()
      return
    }

    setSubmitting(true)
    setError(null)
    try {
      const created = await createExercise(statement, notionId === AUTO ? undefined : notionId)
      navigate(`/exercises/${created.id}`)
    } catch (err) {
      const message = err instanceof ExerciseError ? err.message : "L'exercice n'a pas pu être enregistré."
      setError(message)
      if (err instanceof ExerciseError && err.code === 'NotionNotDetected') {
        setMustChooseNotion(true)
        notionSelectRef.current?.focus()
      }
      setSubmitting(false)
    }
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white shadow-sm">
        <div className="max-w-5xl mx-auto px-4 py-3 flex items-center gap-3">
          <Link
            to="/dashboard"
            className="text-gray-500 hover:text-gray-800 p-2 -ml-2 rounded"
            aria-label="Retour au tableau de bord"
          >
            ←
          </Link>
          <h1 className="text-lg font-semibold text-gray-900">J'ai un exercice</h1>
        </div>
      </header>

      <main className="max-w-5xl mx-auto px-4 py-6">
        <form onSubmit={onSubmit} className="space-y-6">
          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <label htmlFor="statement" className="block font-medium text-gray-900 mb-1">
                L'énoncé
              </label>
              <p id="statement-help" className="text-sm text-gray-500 mb-2">
                Colle ou tape ton exercice. Tu peux écrire les formules entre dollars, par exemple{' '}
                <code>{'$\\frac{2}{3}$'}</code> ou <code>{'$x^2$'}</code> : l'aperçu te montre le résultat.
              </p>
              <textarea
                id="statement"
                value={statement}
                onChange={(e) => setStatement(e.target.value)}
                aria-describedby="statement-help"
                maxLength={MAX_STATEMENT_LENGTH}
                rows={10}
                required
                placeholder="Exemple : Le triangle ABC est rectangle en A, avec AB = 3 cm et AC = 4 cm. Calcule BC."
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-base leading-6 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              <p className="text-xs text-gray-400 text-right mt-1">
                {statement.length} / {MAX_STATEMENT_LENGTH}
              </p>
            </div>

            <div>
              <p className="block font-medium text-gray-900 mb-1">Aperçu</p>
              <p className="text-sm text-gray-500 mb-2">Vérifie que les formules s'affichent bien.</p>
              <div
                className="relative min-h-[15rem] rounded-lg border border-dashed border-gray-300 bg-white px-4 py-3 text-gray-900"
                aria-live="polite"
              >
                {preview.trim() ? (
                  <MathMarkdown content={preview} />
                ) : (
                  <p className="text-gray-400">L'aperçu de ton énoncé apparaîtra ici.</p>
                )}
              </div>
            </div>
          </div>

          <fieldset className="bg-white rounded-lg border border-gray-200 p-4">
            <legend className="font-medium text-gray-900 px-1">La notion</legend>
            <p className="text-sm text-gray-500 mb-3">
              {mustChooseNotion
                ? 'Choisis la notion de cet exercice.'
                : "Si tu ne sais pas, laisse « Je ne sais pas » : ton prof IA la trouvera."}
            </p>
            <div className="flex flex-col sm:flex-row gap-3">
              <label className="sm:w-40">
                <span className="sr-only">Classe</span>
                <select
                  value={activeLevel ?? ''}
                  onChange={(e) => {
                    setLevel(e.target.value)
                    setNotionId(AUTO)
                  }}
                  className="w-full rounded-lg border border-gray-300 px-3 py-2 text-base bg-white"
                >
                  {levels.map((l) => (
                    <option key={l} value={l}>
                      {levelLabel(l)}
                    </option>
                  ))}
                </select>
              </label>
              <label className="flex-1 min-w-0">
                <span className="sr-only">Notion</span>
                <select
                  ref={notionSelectRef}
                  value={notionId}
                  onChange={(e) => setNotionId(e.target.value)}
                  aria-invalid={mustChooseNotion && notionId === AUTO}
                  className={`w-full rounded-lg border px-3 py-2 text-base bg-white ${
                    mustChooseNotion && notionId === AUTO ? 'border-red-500 ring-2 ring-red-200' : 'border-gray-300'
                  }`}
                >
                  <option value={AUTO}>
                    {mustChooseNotion ? '— Choisis une notion —' : 'Je ne sais pas (détection automatique)'}
                  </option>
                  {chapters
                    .filter((c) => c.schoolLevel === activeLevel)
                    .map((c) => (
                      <optgroup key={c.id} label={`${c.order}. ${c.title}`}>
                        {c.notions.map((n) => (
                          <option key={n.id} value={n.id}>
                            {n.title}
                          </option>
                        ))}
                      </optgroup>
                    ))}
                </select>
              </label>
            </div>
          </fieldset>

          {error && (
            <div role="alert" className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
              {error}
            </div>
          )}

          <div className="flex justify-end">
            <button
              type="submit"
              disabled={!statement.trim() || submitting}
              className="h-11 px-6 rounded-full bg-blue-600 text-white font-medium hover:bg-blue-700 disabled:opacity-40"
            >
              {submitting
                ? notionId === AUTO
                  ? 'Recherche de la notion…'
                  : 'Enregistrement…'
                : 'Passer à la résolution'}
            </button>
          </div>
        </form>
      </main>
    </div>
  )
}
