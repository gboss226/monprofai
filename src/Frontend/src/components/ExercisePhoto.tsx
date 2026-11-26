import { ChangeEvent, useEffect, useRef, useState } from 'react'
import { ExerciseError, transcribeExercise } from '../api/exercises'
import { ImageError, prepareImage } from '../utils/image'

interface Props {
  /** Called with the statement read from the photo. */
  onStatement: (statement: string) => void
  /** Lets the parent block the form while the photo is being read. */
  onBusyChange?: (busy: boolean) => void
  disabled?: boolean
}

/**
 * "Take a photo" / "Import a picture" buttons. The picture is resized in the browser, read by the model
 * on the server, and only the resulting text is kept: the photo is never stored.
 */
export function ExercisePhoto({ onStatement, onBusyChange, disabled }: Props) {
  const cameraRef = useRef<HTMLInputElement>(null)
  const fileRef = useRef<HTMLInputElement>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [thumbnail, setThumbnail] = useState<string | null>(null)
  const [transcribed, setTranscribed] = useState(false)

  useEffect(() => () => {
    if (thumbnail) URL.revokeObjectURL(thumbnail)
  }, [thumbnail])

  const onFile = async (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    e.target.value = '' // lets the student pick the same file again after an error
    if (!file) return

    setError(null)
    setTranscribed(false)
    setBusy(true)
    onBusyChange?.(true)
    try {
      const photo = await prepareImage(file)
      setThumbnail(URL.createObjectURL(photo))
      onStatement(await transcribeExercise(photo))
      setTranscribed(true)
    } catch (err) {
      setError(
        err instanceof ImageError || err instanceof ExerciseError
          ? err.message
          : "La photo n'a pas pu être lue. Réessaie ou tape l'énoncé."
      )
    } finally {
      setBusy(false)
      onBusyChange?.(false)
    }
  }

  const buttonClass =
    'inline-flex items-center gap-2 h-10 px-4 rounded-full border border-blue-600 text-blue-700 text-sm font-medium hover:bg-blue-50 disabled:opacity-40'

  return (
    <div className="mb-3">
      <div className="flex flex-wrap items-center gap-2">
        {/* capture opens the rear camera on phones; on a computer it behaves like a file picker. */}
        <input ref={cameraRef} type="file" accept="image/*" capture="environment" hidden onChange={onFile} />
        <input ref={fileRef} type="file" accept="image/*" hidden onChange={onFile} />
        <button type="button" onClick={() => cameraRef.current?.click()} disabled={disabled || busy} className={buttonClass}>
          <span aria-hidden>📷</span> Prendre une photo
        </button>
        <button type="button" onClick={() => fileRef.current?.click()} disabled={disabled || busy} className={buttonClass}>
          <span aria-hidden>🖼️</span> Importer une image
        </button>
      </div>

      {(busy || thumbnail) && (
        <div className="flex items-center gap-3 mt-3" aria-live="polite">
          {thumbnail && (
            <img src={thumbnail} alt="Photo de l'exercice" className="h-16 w-16 object-cover rounded border border-gray-200" />
          )}
          {(busy || transcribed) && (
            <p className="text-sm text-gray-600">
              {busy
                ? 'Lecture de l’énoncé sur la photo…'
                : 'Énoncé recopié depuis la photo. Vérifie le texte et corrige-le si besoin.'}
            </p>
          )}
        </div>
      )}

      {error && (
        <p role="alert" className="text-sm text-red-700 mt-2">
          {error}
        </p>
      )}
    </div>
  )
}
