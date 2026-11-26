import { API_BASE_URL } from './client'

const GENERIC_ERROR = "Ton professeur IA n'est pas disponible pour le moment. Réessaie plus tard."

export class TutorError extends Error {
  constructor(
    message: string,
    public readonly code?: string,
    /** Set when the question was saved in a conversation before the failure. */
    public readonly conversationId?: string
  ) {
    super(message)
    this.name = 'TutorError'
  }
}

/** Continue `conversationId`, or start a new conversation about `notionId`. */
export interface AskTutorPayload {
  message: string
  conversationId?: string
  notionId?: string
}

export interface StreamHandlers {
  /** Called first, with the conversation the question was saved in. */
  onConversation?: (conversationId: string) => void
  onChunk: (text: string) => void
}

/**
 * Sends a question and calls `onChunk` with each piece of the answer as it arrives.
 * The backend keeps the conversation and replays its latest messages to the model.
 * The backend streams Server-Sent Events; axios cannot read a stream in the browser, so this uses fetch.
 * Throws a TutorError whose message can be shown to the student as is.
 */
export async function streamTutorAnswer(
  payload: AskTutorPayload,
  handlers: StreamHandlers,
  signal?: AbortSignal
): Promise<void> {
  let response: Response
  try {
    response = await fetch(`${API_BASE_URL}/tutor/ask`, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
      signal,
    })
  } catch (error) {
    if (signal?.aborted) throw error
    throw new TutorError('Impossible de joindre le serveur. Vérifie ta connexion internet.', 'Network')
  }

  if (response.status === 401) {
    window.location.href = '/login'
    throw new TutorError('Ta session a expiré. Reconnecte-toi.', 'Unauthorized')
  }

  if (!response.ok || !response.body) {
    const body = await response.json().catch(() => null)
    throw new TutorError(body?.message ?? GENERIC_ERROR, body?.code, body?.conversationId ?? undefined)
  }

  const reader = response.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''

  for (;;) {
    const { value, done } = await reader.read()
    if (done) break
    buffer += decoder.decode(value, { stream: true })

    let frameEnd: number
    while ((frameEnd = buffer.indexOf('\n\n')) !== -1) {
      const frame = buffer.slice(0, frameEnd)
      buffer = buffer.slice(frameEnd + 2)
      if (handleFrame(frame, handlers) === 'done') return
    }
  }

  throw new TutorError("La réponse a été interrompue. Repose ta question.", 'Interrupted')
}

function handleFrame(frame: string, handlers: StreamHandlers): 'done' | 'continue' {
  let event = 'message'
  const dataLines: string[] = []
  for (const line of frame.split('\n')) {
    if (line.startsWith('event:')) event = line.slice(6).trim()
    else if (line.startsWith('data:')) dataLines.push(line.slice(5).trimStart())
  }
  if (dataLines.length === 0) return 'continue'

  const data = JSON.parse(dataLines.join('\n'))
  if (event === 'done') return 'done'
  if (event === 'conversation') {
    handlers.onConversation?.(data.conversationId)
    return 'continue'
  }
  if (event === 'error') throw new TutorError(data.message ?? GENERIC_ERROR, data.code)
  if (typeof data.text === 'string') handlers.onChunk(data.text)
  return 'continue'
}
