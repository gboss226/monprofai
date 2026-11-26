import { FormEvent, KeyboardEvent, useEffect, useLayoutEffect, useRef, useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { isAxiosError } from 'axios'
import { getConversation } from '../api/conversations'
import { findNotion, getReferential } from '../api/notions'
import { streamTutorAnswer, TutorError } from '../api/tutor'
import { MathMarkdown } from '../components/MathMarkdown'
import { SCHOOL_LEVELS } from '../types/learnerProfile'
import { ChatMessage, NotionContext } from '../types/tutor'

/** Matches the backend limit (AskTutorQueryValidator). */
const MAX_MESSAGE_LENGTH = 2000

const STARTERS = [
  'Explique-moi cette notion simplement',
  'Donne-moi un exemple corrigé',
  'Propose-moi un exercice',
]

let nextId = 1

/**
 * Chat with the tutor. Two entry points:
 * - /tutor/:notionId starts a new conversation about a notion (the URL becomes /tutor/c/:id once it is saved);
 * - /tutor/c/:conversationId resumes one of the student's conversations.
 */
export function TutorChat() {
  const params = useParams<{ notionId?: string; conversationId?: string }>()
  const navigate = useNavigate()
  const location = useLocation()

  const [conversationId, setConversationId] = useState<string | null>(params.conversationId ?? null)
  // Avoids reloading the conversation this page just created when the URL switches to it.
  const conversationIdRef = useRef<string | null>(null)
  const [notionId, setNotionId] = useState<string | null>(params.notionId ?? null)
  const [loading, setLoading] = useState(Boolean(params.conversationId))

  const [context, setContext] = useState<NotionContext | null>(null)
  const [contextError, setContextError] = useState<string | null>(null)

  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [input, setInput] = useState('')
  const [streaming, setStreaming] = useState(false)

  const abortRef = useRef<AbortController | null>(null)
  const scrollRef = useRef<HTMLDivElement>(null)
  const stickToBottom = useRef(true)
  const inputRef = useRef<HTMLTextAreaElement>(null)

  // Resume a saved conversation.
  useEffect(() => {
    const id = params.conversationId
    if (!id || id === conversationIdRef.current) return
    conversationIdRef.current = id
    setLoading(true)
    getConversation(id)
      .then((conversation) => {
        setConversationId(conversation.id)
        setNotionId(conversation.notionId)
        setMessages(
          conversation.messages.map((m) => ({ id: nextId++, role: m.role, content: m.content, status: 'done' }))
        )
      })
      .catch((error) =>
        setContextError(
          isAxiosError(error) && error.response?.status === 404
            ? 'Cette conversation est introuvable.'
            : 'Impossible de charger la conversation. Recharge la page.'
        )
      )
      .finally(() => setLoading(false))
  }, [params.conversationId])

  useEffect(() => {
    if (!notionId) return
    getReferential()
      .then((subjects) => {
        const found = findNotion(subjects, notionId)
        if (found) setContext(found)
        else setContextError("Cette notion n'existe pas.")
      })
      .catch(() => setContextError('Impossible de charger la notion. Recharge la page.'))
  }, [notionId])

  const adoptConversation = (id: string) => {
    if (conversationIdRef.current === id) return
    conversationIdRef.current = id
    setConversationId(id)
    // A reload or a shared link now resumes this conversation instead of starting a new one.
    navigate(`/tutor/c/${id}`, { replace: true })
  }

  const ready = !loading && !contextError && (notionId ? Boolean(context) : Boolean(conversationId))

  // Stop a running answer when leaving the page.
  useEffect(() => () => abortRef.current?.abort(), [])

  // Follow the answer as it grows, unless the student scrolled up to reread something.
  useLayoutEffect(() => {
    const el = scrollRef.current
    if (el && stickToBottom.current) el.scrollTop = el.scrollHeight
  }, [messages])

  const onScroll = () => {
    const el = scrollRef.current
    if (el) stickToBottom.current = el.scrollHeight - el.scrollTop - el.clientHeight < 80
  }

  const updateMessage = (id: number, change: (m: ChatMessage) => ChatMessage) =>
    setMessages((all) => all.map((m) => (m.id === id ? change(m) : m)))

  const send = async (text: string) => {
    const message = text.trim()
    if (!message || streaming || !ready) return

    const userMessage: ChatMessage = { id: nextId++, role: 'user', content: message, status: 'done' }
    const answer: ChatMessage = { id: nextId++, role: 'assistant', content: '', status: 'streaming' }
    setMessages((all) => [...all, userMessage, answer])
    setInput('')
    setStreaming(true)
    stickToBottom.current = true

    const controller = new AbortController()
    abortRef.current = controller
    try {
      // The server keeps the conversation and gives the model its latest messages: no history to send.
      await streamTutorAnswer(
        conversationIdRef.current
          ? { message, conversationId: conversationIdRef.current }
          : { message, notionId: notionId ?? undefined },
        {
          onConversation: adoptConversation,
          onChunk: (chunk) => updateMessage(answer.id, (m) => ({ ...m, content: m.content + chunk })),
        },
        controller.signal
      )
      updateMessage(answer.id, (m) => ({ ...m, status: 'done' }))
    } catch (error) {
      if (error instanceof TutorError && error.conversationId) adoptConversation(error.conversationId)
      if (controller.signal.aborted) {
        updateMessage(answer.id, (m) => ({
          ...m,
          status: m.content ? 'done' : 'error',
          error: m.content ? undefined : 'Réponse arrêtée.',
        }))
      } else {
        const text =
          error instanceof TutorError ? error.message : "Une erreur inattendue s'est produite. Réessaie."
        updateMessage(answer.id, (m) => ({ ...m, status: 'error', error: text }))
      }
    } finally {
      abortRef.current = null
      setStreaming(false)
      inputRef.current?.focus()
    }
  }

  // Coming from an exercise: its statement is sent as the first question, once.
  const initialMessage = (location.state as { initialMessage?: string } | null)?.initialMessage
  const initialSent = useRef(false)
  useEffect(() => {
    if (!initialMessage || initialSent.current || !ready || messages.length > 0) return
    initialSent.current = true
    send(initialMessage)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [initialMessage, ready])

  const onSubmit = (e: FormEvent) => {
    e.preventDefault()
    send(input)
  }

  // Enter sends, Shift+Enter adds a line. On touch keyboards Enter always adds a line (there is a send button).
  const onKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    const touch = window.matchMedia('(pointer: coarse)').matches
    if (e.key === 'Enter' && !e.shiftKey && !touch && !e.nativeEvent.isComposing) {
      e.preventDefault()
      send(input)
    }
  }

  // Grow the textarea with its content, up to a few lines.
  useLayoutEffect(() => {
    const el = inputRef.current
    if (!el) return
    el.style.height = 'auto'
    // scrollHeight excludes the borders; without them the field always scrolls by 2px.
    const borders = el.offsetHeight - el.clientHeight
    el.style.height = `${Math.min(el.scrollHeight + borders, 160)}px`
    el.style.overflowY = el.scrollHeight + borders > 160 ? 'auto' : 'hidden'
  }, [input])

  const level = context ? SCHOOL_LEVELS.find((l) => l.value === context.chapter.schoolLevel)?.label : null

  return (
    <div className="h-[100dvh] flex flex-col bg-gray-50">
      <header className="bg-white shadow-sm shrink-0">
        <div className="max-w-3xl mx-auto px-4 py-3 flex items-center gap-3">
          <Link
            to="/tutor"
            className="text-gray-500 hover:text-gray-800 p-2 -ml-2 rounded shrink-0"
            aria-label="Mes conversations et notions"
          >
            ←
          </Link>
          <div className="min-w-0">
            <p className="text-xs text-gray-500 truncate">
              {context ? `${context.subject.name} · ${level} · ${context.chapter.title}` : 'Prof IA'}
            </p>
            <h1 className="font-semibold text-gray-900 leading-snug line-clamp-2">
              {context?.notion.title ??
                (contextError ? 'Introuvable' : loading || notionId ? 'Chargement…' : 'Discussion libre')}
            </h1>
          </div>
        </div>
      </header>

      {/* relative: KaTeX's hidden MathML is absolutely positioned and would otherwise stretch the page. */}
      <div ref={scrollRef} onScroll={onScroll} className="relative flex-1 overflow-y-auto" aria-live="polite">
        <div className="max-w-3xl mx-auto px-3 sm:px-4 py-4 space-y-4">
          {contextError && (
            <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
              {contextError}{' '}
              <Link to="/tutor" className="underline font-medium">
                Retour à mes conversations
              </Link>
            </div>
          )}

          {loading && <p className="text-center text-gray-500 py-6">Chargement de la conversation…</p>}

          {ready && messages.length === 0 && (
            <div className="text-center py-6">
              <p className="text-gray-700 mb-1">Pose ta question sur cette notion.</p>
              <p className="text-sm text-gray-500 mb-5">
                Ton prof IA t'aide à comprendre et à trouver la réponse par toi-même.
              </p>
              <div className="flex flex-wrap justify-center gap-2">
                {STARTERS.map((starter) => (
                  <button
                    key={starter}
                    onClick={() => send(starter)}
                    className="px-4 py-2 rounded-full bg-white border border-gray-300 text-sm text-gray-700 hover:border-blue-400 hover:text-blue-700"
                  >
                    {starter}
                  </button>
                ))}
              </div>
            </div>
          )}

          {messages.map((m) => (
            <MessageBubble key={m.id} message={m} />
          ))}
        </div>
      </div>

      <form
        onSubmit={onSubmit}
        className="bg-white border-t border-gray-200 shrink-0"
        style={{ paddingBottom: 'env(safe-area-inset-bottom)' }}
      >
        <div className="max-w-3xl mx-auto px-3 sm:px-4 py-3 flex items-end gap-2">
          <label htmlFor="tutor-input" className="sr-only">
            Ta question
          </label>
          <textarea
            id="tutor-input"
            ref={inputRef}
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={onKeyDown}
            rows={1}
            maxLength={MAX_MESSAGE_LENGTH}
            disabled={!ready}
            placeholder="Écris ta question…"
            // text-base (16px) stops iOS from zooming into the field.
            className="flex-1 resize-none rounded-2xl border border-gray-300 px-4 py-2.5 text-base leading-6 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100"
          />
          {streaming ? (
            <button
              type="button"
              onClick={() => abortRef.current?.abort()}
              className="h-11 px-4 rounded-full bg-gray-200 text-gray-800 font-medium hover:bg-gray-300 shrink-0"
            >
              Arrêter
            </button>
          ) : (
            <button
              type="submit"
              disabled={!input.trim() || !ready}
              className="h-11 px-5 rounded-full bg-blue-600 text-white font-medium hover:bg-blue-700 disabled:opacity-40 shrink-0"
            >
              Envoyer
            </button>
          )}
        </div>
      </form>
    </div>
  )
}

function MessageBubble({ message }: { message: ChatMessage }) {
  if (message.role === 'user') {
    return (
      <div className="flex justify-end">
        <div className="max-w-[85%] sm:max-w-[75%] bg-blue-600 text-white rounded-2xl rounded-br-md px-4 py-2.5 whitespace-pre-wrap break-words">
          {message.content}
        </div>
      </div>
    )
  }

  const waiting = message.status === 'streaming' && !message.content
  return (
    <div className="flex justify-start">
      <div className="max-w-[92%] sm:max-w-[85%] min-w-0 bg-white border border-gray-200 text-gray-900 rounded-2xl rounded-bl-md px-4 py-3 shadow-sm">
        {waiting ? (
          <span className="flex items-center gap-1.5 py-1" role="status" aria-label="Le prof IA écrit">
            <span className="typing-dot w-2 h-2 rounded-full bg-gray-500" />
            <span className="typing-dot w-2 h-2 rounded-full bg-gray-500" style={{ animationDelay: '0.15s' }} />
            <span className="typing-dot w-2 h-2 rounded-full bg-gray-500" style={{ animationDelay: '0.3s' }} />
          </span>
        ) : (
          message.content && <MathMarkdown content={message.content} />
        )}
        {message.error && (
          <p className={`text-sm text-red-700 ${message.content ? 'mt-2 pt-2 border-t border-gray-100' : ''}`}>
            {message.error}
          </p>
        )}
      </div>
    </div>
  )
}
