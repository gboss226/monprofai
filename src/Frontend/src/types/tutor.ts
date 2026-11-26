export interface NotionDto {
  id: string
  order: number
  title: string
  shortDescription: string | null
}

export interface ChapterDto {
  id: string
  /** Enum name, e.g. "Quatrieme" (same values as the learner profile). */
  schoolLevel: string
  order: number
  title: string
  notions: NotionDto[]
}

export interface SubjectDto {
  id: string
  code: string
  name: string
  chapters: ChapterDto[]
}

/** A notion with the chapter and subject it belongs to. */
export interface NotionContext {
  notion: NotionDto
  chapter: ChapterDto
  subject: SubjectDto
}

export interface ConversationSummary {
  id: string
  notionId: string | null
  notionTitle: string | null
  chapterTitle: string | null
  /** Start of the student's first message. */
  preview: string
  messageCount: number
  createdAt: string
  updatedAt: string
}

export interface ConversationDetail {
  id: string
  notionId: string | null
  createdAt: string
  messages: { id: string; role: ChatRole; content: string; createdAt: string }[]
}

export type ChatRole = 'user' | 'assistant'

export interface ChatTurn {
  role: ChatRole
  content: string
}

export interface ChatMessage extends ChatTurn {
  id: number
  status: 'streaming' | 'done' | 'error'
  /** Message for the student when the answer failed or was cut short. */
  error?: string
}
