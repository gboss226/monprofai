import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { listConversations } from '../api/conversations'
import { ConversationSummary } from '../types/tutor'

const SHOWN_BY_DEFAULT = 3

/** "aujourd'hui à 14:05", "hier", "il y a 3 jours", then the date. */
export function formatWhen(iso: string, now = new Date()): string {
  const date = new Date(iso)
  const startOfDay = (d: Date) => new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime()
  const days = Math.round((startOfDay(now) - startOfDay(date)) / 86_400_000)
  if (days <= 0) return `aujourd'hui à ${date.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })}`
  if (days === 1) return 'hier'
  if (days < 7) return `il y a ${days} jours`
  return date.toLocaleDateString('fr-FR', { day: 'numeric', month: 'long' })
}

/** The student's previous conversations with the tutor, to pick one up where he left it. */
export function PastConversations() {
  const [conversations, setConversations] = useState<ConversationSummary[] | null>(null)
  const [failed, setFailed] = useState(false)
  const [showAll, setShowAll] = useState(false)

  useEffect(() => {
    listConversations()
      .then(setConversations)
      .catch(() => setFailed(true))
  }, [])

  if (failed) {
    return (
      <p className="text-sm text-red-700 mb-6">Impossible de charger tes conversations. Recharge la page.</p>
    )
  }
  if (!conversations || conversations.length === 0) return null

  const shown = showAll ? conversations : conversations.slice(0, SHOWN_BY_DEFAULT)

  return (
    <section className="mb-8" aria-labelledby="past-conversations">
      <h2 id="past-conversations" className="text-sm font-semibold uppercase tracking-wide text-gray-500 mb-3">
        Reprendre une conversation
      </h2>
      <ul className="space-y-2">
        {shown.map((c) => (
          <li key={c.id}>
            <Link
              to={`/tutor/c/${c.id}`}
              className="block bg-white rounded-lg border border-gray-200 shadow-sm px-4 py-3 hover:border-blue-400 focus:border-blue-400"
            >
              <span className="flex items-baseline justify-between gap-3">
                <span className="font-medium text-gray-900 truncate">{c.notionTitle ?? 'Discussion libre'}</span>
                <span className="text-xs text-gray-500 shrink-0">{formatWhen(c.updatedAt)}</span>
              </span>
              <span className="block text-sm text-gray-600 truncate mt-0.5">« {c.preview} »</span>
              <span className="block text-xs text-gray-400 mt-0.5">
                {c.chapterTitle ? `${c.chapterTitle} · ` : ''}
                {c.messageCount} message{c.messageCount > 1 ? 's' : ''}
              </span>
            </Link>
          </li>
        ))}
      </ul>
      {conversations.length > SHOWN_BY_DEFAULT && (
        <button
          onClick={() => setShowAll(!showAll)}
          className="mt-2 text-sm font-medium text-blue-700 hover:underline"
        >
          {showAll ? 'Afficher moins' : `Voir toutes mes conversations (${conversations.length})`}
        </button>
      )}
    </section>
  )
}
