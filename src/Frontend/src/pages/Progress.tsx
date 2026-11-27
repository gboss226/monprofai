import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { getMyProfile } from '../api/learnerProfile'
import { getReferential } from '../api/notions'
import { getProgress } from '../api/progress'
import { formatWhen } from '../components/PastConversations'
import { useAuthStore } from '../store/authStore'
import { SCHOOL_LEVELS } from '../types/learnerProfile'
import { masteryLevel, percent, Progress as ProgressData } from '../types/progress'
import { SubjectDto } from '../types/tutor'

const levelLabel = (value: string) => SCHOOL_LEVELS.find((l) => l.value === value)?.label ?? value
const levelRank = (value: string) => SCHOOL_LEVELS.findIndex((l) => l.value === value)

/** The student's mastery of each notion, chapter by chapter, and his latest finished exercises. */
export function Progress() {
  const profile = useAuthStore((state) => state.profile)
  const setProfile = useAuthStore((state) => state.setProfile)
  const [progress, setProgress] = useState<ProgressData | null>(null)
  const [subjects, setSubjects] = useState<SubjectDto[] | null>(null)
  const [failed, setFailed] = useState(false)
  const [level, setLevel] = useState<string | null>(null)

  useEffect(() => {
    Promise.all([getProgress(), getReferential()])
      .then(([p, s]) => {
        setProgress(p)
        setSubjects(s)
      })
      .catch(() => setFailed(true))
    if (!profile) getMyProfile().then(setProfile).catch(() => {})
  }, [profile, setProfile])

  const scores = useMemo(() => new Map(progress?.masteries.map((m) => [m.notionId, m]) ?? []), [progress])
  const chapters = useMemo(() => subjects?.flatMap((s) => s.chapters) ?? [], [subjects])
  const levels = useMemo(
    () => [...new Set(chapters.map((c) => c.schoolLevel))].sort((a, b) => levelRank(a) - levelRank(b)),
    [chapters]
  )
  const activeLevel =
    level ?? (profile && levels.includes(profile.schoolLevel) ? profile.schoolLevel : levels[0] ?? null)

  const worked = progress?.masteries.length ?? 0
  const mastered = progress?.masteries.filter((m) => m.score >= 0.75).length ?? 0
  const finished = progress?.masteries.reduce((sum, m) => sum + m.exercisesCount, 0) ?? 0

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white shadow-sm">
        <div className="max-w-3xl mx-auto px-4 py-3 flex items-center gap-3">
          <Link to="/dashboard" className="text-gray-500 hover:text-gray-800 p-2 -ml-2 rounded" aria-label="Retour au tableau de bord">
            ←
          </Link>
          <h1 className="text-lg font-semibold text-gray-900">Ma progression</h1>
        </div>
      </header>

      <main className="max-w-3xl mx-auto px-4 py-6 space-y-6">
        {failed && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
            Impossible de charger ta progression. Recharge la page.
          </div>
        )}
        {!progress && !failed && <p className="text-gray-500">Chargement…</p>}

        {progress && (
          <>
            <section className="grid grid-cols-3 gap-3" aria-label="Résumé">
              {[
                { value: finished, label: finished > 1 ? 'exercices terminés' : 'exercice terminé' },
                { value: worked, label: worked > 1 ? 'notions travaillées' : 'notion travaillée' },
                { value: mastered, label: mastered > 1 ? 'notions maîtrisées' : 'notion maîtrisée' },
              ].map((stat) => (
                <div key={stat.label} className="bg-white rounded-lg border border-gray-200 px-3 py-3 text-center">
                  <p className="text-2xl font-semibold text-gray-900">{stat.value}</p>
                  <p className="text-xs text-gray-500">{stat.label}</p>
                </div>
              ))}
            </section>

            {progress.recentResults.length > 0 && (
              <section aria-labelledby="recent-title">
                <h2 id="recent-title" className="text-sm font-semibold uppercase tracking-wide text-gray-500 mb-2">
                  Derniers exercices
                </h2>
                <ul className="space-y-2">
                  {progress.recentResults.map((r) => {
                    const delta = Math.round((r.masteryAfter - r.masteryBefore) * 100)
                    return (
                      <li key={r.exerciseId}>
                        <Link
                          to={`/exercises/${r.exerciseId}`}
                          className="flex items-center gap-3 bg-white rounded-lg border border-gray-200 px-4 py-3 hover:border-blue-400"
                        >
                          <span aria-hidden className="text-lg">{r.succeeded ? '✅' : '📖'}</span>
                          <span className="min-w-0 flex-1">
                            <span className="block font-medium text-gray-900 truncate">{r.notionTitle}</span>
                            <span className="block text-xs text-gray-500">
                              {r.succeeded ? 'Réussi' : 'Solution consultée'} · {r.hintsUsed} indice{r.hintsUsed > 1 ? 's' : ''} ·{' '}
                              {r.attempts} essai{r.attempts > 1 ? 's' : ''} · {formatWhen(r.completedAt)}
                            </span>
                          </span>
                          <span
                            className={`text-sm font-semibold shrink-0 ${delta >= 0 ? 'text-green-700' : 'text-red-700'}`}
                            title="Évolution de ta maîtrise de la notion"
                          >
                            {delta >= 0 ? '+' : '−'}
                            {Math.abs(delta)} pts
                          </span>
                        </Link>
                      </li>
                    )
                  })}
                </ul>
              </section>
            )}

            <section aria-labelledby="mastery-title">
              <h2 id="mastery-title" className="text-sm font-semibold uppercase tracking-wide text-gray-500 mb-2">
                Maîtrise par notion
              </h2>
              <p className="text-sm text-gray-600 mb-3">
                Ta maîtrise monte quand tu réussis un exercice, d'autant plus que tu as utilisé peu d'indices, et
                baisse quand tu dois regarder la solution.
              </p>

              <div className="flex gap-2 overflow-x-auto pb-2 mb-3" role="tablist" aria-label="Classe">
                {levels.map((l) => (
                  <button
                    key={l}
                    role="tab"
                    aria-selected={l === activeLevel}
                    onClick={() => setLevel(l)}
                    className={`px-4 py-2 rounded-full text-sm font-medium whitespace-nowrap border ${
                      l === activeLevel ? 'bg-blue-600 text-white border-blue-600' : 'bg-white text-gray-700 border-gray-300'
                    }`}
                  >
                    {levelLabel(l)}
                  </button>
                ))}
              </div>

              <div className="space-y-4">
                {chapters
                  .filter((c) => c.schoolLevel === activeLevel)
                  .map((chapter) => (
                    <div key={chapter.id} className="bg-white rounded-lg border border-gray-200 shadow-sm">
                      <h3 className="px-4 pt-3 pb-2 font-medium text-gray-900">
                        <span className="text-xs uppercase tracking-wide text-gray-500 block">Chapitre {chapter.order}</span>
                        {chapter.title}
                      </h3>
                      <ul className="divide-y divide-gray-100">
                        {chapter.notions.map((notion) => {
                          const mastery = scores.get(notion.id)
                          const lvl = masteryLevel(mastery?.score)
                          return (
                            <li key={notion.id} className="px-4 py-3">
                              <div className="flex items-start justify-between gap-3 mb-1.5">
                                <span className="text-sm text-gray-900">{notion.title}</span>
                                <span className={`text-xs rounded-full px-2 py-0.5 shrink-0 ${lvl.badge}`}>{lvl.label}</span>
                              </div>
                              <div
                                className="h-2 rounded-full bg-gray-100 overflow-hidden"
                                role="meter"
                                aria-label={`Maîtrise de ${notion.title}`}
                                aria-valuemin={0}
                                aria-valuemax={100}
                                aria-valuenow={mastery ? Math.round(mastery.score * 100) : 0}
                              >
                                <div className={`h-full ${lvl.bar}`} style={{ width: mastery ? percent(mastery.score).replace(' ', '') : '0%' }} />
                              </div>
                              <p className="text-xs text-gray-500 mt-1">
                                {mastery
                                  ? `${percent(mastery.score)} · ${mastery.exercisesCount} exercice${mastery.exercisesCount > 1 ? 's' : ''}`
                                  : 'Pas encore travaillée'}
                              </p>
                            </li>
                          )
                        })}
                      </ul>
                    </div>
                  ))}
              </div>
            </section>
          </>
        )}
      </main>
    </div>
  )
}
