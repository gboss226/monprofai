import client from './client'
import { ConversationDetail, ConversationSummary } from '../types/tutor'

/** The logged-in student's conversations, most recent first. */
export async function listConversations(): Promise<ConversationSummary[]> {
  const response = await client.get<ConversationSummary[]>('/conversations')
  return response.data
}

/** One of the student's conversations with its messages. Rejects with a 404 for anyone else's. */
export async function getConversation(id: string): Promise<ConversationDetail> {
  const response = await client.get<ConversationDetail>(`/conversations/${id}`)
  return response.data
}
