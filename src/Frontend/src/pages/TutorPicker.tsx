import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { getReferential } from '../api/notions'
import { getMyProfile } from '../api/learnerProfile'
import { PastConversations } from '../components/PastConversations'
import { useAuthStore } from '../store/authStore'
import { SCHOOL_LEVELS } from '../types/learnerProfile'
import { ChapterDto, SubjectDto } from '../types/tutor'

const levelLabel = (value: string) => SCHOOL_LEVELS.find((l) => l.value === value)?.label ?? value
const levelRank = (value: string) => SCHOOL_LEVELS.findIndex((l) => l.value === value)

/** First step of a tutoring session: pick the notion to work on, chapter by chapter. */
export function TutorPicker() {
  const navigate = useNavigate()
  const profile = useAuthStore((state) => state.profile)
  const setProfile = useAuthStore((state) => state.setProfile)

  const [subjects, setSubjects] = useState<SubjectDto[] | null>(null)
  const [loadError, setLoadError] = useState(false)
  const [subjectId, setSubjectId] = useState<string | null>(null)
  const [level, setLevel] = useState<string | null>(null)
  const [openChapterId, setOpenChapterId] = useState<string | null>(null)

  useEffect(() => {
    getReferential()
      .then(setSubjects)
      .catch(() => setLoadError(true))
    if (!profile) getMyProfile().then(setProfile).catch(() => {})
  }, [profile, setProfile])

  const subject = subjects?.find((s) => s.id === subjectId) ?? subjects?.[0] ?? null

  const levels = useMemo(
    () =>
      [...new Set(subject?.chapters.map((c) => c.schoolLevel) ?? [])].sort(
        (a, b) => levelRank(a) - levelRank(b)
      ),
    [subject]
  )

  // Default to the student's own class when the referential covers it.
  const activeLevel =
    level && levels.includes(level)
      ? level
      : profile && levels.includes(profile.schoolLevel)
        ? profile.schoolLevel
        : levels[0] ?? null

  const chapters: ChapterDto[] = subject?.chapters.filter((c) => c.schoolLevel === activeLevel) ?? []

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
          <h1 className="text-lg font-semibold text-gray-900">Travailler avec mon prof IA</h1>
        </div>
      </header>

      <main className="max-w-3xl mx-auto px-4 py-6">
        <PastConversations />

        <h2 className="text-sm font-semibold uppercase tracking-wide text-gray-500 mb-1">Nouvelle séance</h2>
        <p className="text-gray-700 mb-5">Choisis la notion sur laquelle tu veux travailler.</p>

        {loadError && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
            Impossible de charger les notions. Recharge la page pour réessayer.
          </div>
        )}

        {!subjects && !loadError && <p className="text-gray-500">Chargement des notions…</p>}

        {subjects && subjects.length === 0 && (
          <p className="text-gray-600">Aucune notion n'est encore disponible.</p>
        )}

        {subject && (
          <>
            {subjects!.length > 1 && (
              <div className="flex gap-2 overflow-x-auto pb-2 mb-3" role="tablist" aria-label="Matière">
                {subjects!.map((s) => (
                  <button
                    key={s.id}
                    role="tab"
                    aria-selected={s.id === subject.id}
                    onClick={() => setSubjectId(s.id)}
                    className={`px-4 py-2 rounded-full text-sm font-medium whitespace-nowrap border ${
                      s.id === subject.id
                        ? 'bg-blue-600 text-white border-blue-600'
                        : 'bg-white text-gray-700 border-gray-300'
                    }`}
                  >
                    {s.name}
                  </button>
                ))}
              </div>
            )}

            <div className="flex gap-2 overflow-x-auto pb-2 mb-4" role="tablist" aria-label="Classe">
              {levels.map((l) => (
                <button
                  key={l}
                  role="tab"
                  aria-selected={l === activeLevel}
                  onClick={() => {
                    setLevel(l)
                    setOpenChapterId(null)
                  }}
                  className={`px-4 py-2 rounded-full text-sm font-medium whitespace-nowrap border ${
                    l === activeLevel
                      ? 'bg-blue-600 text-white border-blue-600'
                      : 'bg-white text-gray-700 border-gray-300 hover:border-blue-400'
                  }`}
                >
                  {levelLabel(l)}
                </button>
              ))}
            </div>

            <ul className="space-y-3">
              {chapters.map((chapter) => {
                const open = openChapterId === chapter.id
                return (
                  <li key={chapter.id} className="bg-white rounded-lg shadow-sm border border-gray-200">
                    <button
                      onClick={() => setOpenChapterId(open ? null : chapter.id)}
                      aria-expanded={open}
                      className="w-full flex items-center justify-between gap-3 px-4 py-4 text-left"
                    >
                      <span>
                        <span className="block text-xs uppercase tracking-wide text-gray-500">
                          Chapitre {chapter.order}
                        </span>
                        <span className="block font-medium text-gray-900">{chapter.title}</span>
                      </span>
                      <span className="flex items-center gap-2 text-sm text-gray-500 shrink-0">
                        {chapter.notions.length} notion{chapter.notions.length > 1 ? 's' : ''}
                        <span aria-hidden className={`transition-transform ${open ? 'rotate-180' : ''}`}>
                          ▾
                        </span>
                      </span>
                    </button>

                    {open && (
                      <ul className="border-t border-gray-100">
                        {chapter.notions.map((notion) => (
                          <li key={notion.id}>
                            <button
                              onClick={() => navigate(`/tutor/${notion.id}`)}
                              className="w-full text-left px-4 py-3 hover:bg-blue-50 focus:bg-blue-50 border-b border-gray-100 last:border-b-0 last:rounded-b-lg"
                            >
                              <span className="block text-gray-900">{notion.title}</span>
                              {notion.shortDescription && (
                                <span className="block text-sm text-gray-500 mt-0.5">
                                  {notion.shortDescription}
                                </span>
                              )}
                            </button>
                          </li>
                        ))}
                      </ul>
                    )}
                  </li>
                )
              })}
            </ul>
          </>
        )}
      </main>
    </div>
  )
}
