import client from './client'
import { NotionContext, SubjectDto } from '../types/tutor'

let referentialPromise: Promise<SubjectDto[]> | null = null

/** Subjects → chapters → notions. Fetched once per page load: the referential rarely changes. */
export function getReferential(): Promise<SubjectDto[]> {
  if (!referentialPromise) {
    referentialPromise = client
      .get<SubjectDto[]>('/notions')
      .then((response) => response.data)
      .catch((error) => {
        referentialPromise = null
        throw error
      })
  }
  return referentialPromise
}

export function findNotion(subjects: SubjectDto[], notionId: string): NotionContext | null {
  for (const subject of subjects) {
    for (const chapter of subject.chapters) {
      const notion = chapter.notions.find((n) => n.id === notionId)
      if (notion) return { notion, chapter, subject }
    }
  }
  return null
}
