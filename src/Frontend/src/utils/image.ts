/** Matches the backend limit (ImageFormat.MaxBytes). */
export const MAX_IMAGE_BYTES = 5 * 1024 * 1024

/**
 * Longest side kept after resizing. Phone photos are often 4000 px and several MB; 2000 px keeps a page of
 * text legible for the model while making the upload (and the model call) several times lighter.
 */
const MAX_SIDE = 2000

export class ImageError extends Error {}

/**
 * Decodes a picture chosen by the student, scales it down and re-encodes it as JPEG.
 * Re-encoding also turns formats the backend refuses (e.g. HEIC opened by Safari) into a JPEG.
 */
export async function prepareImage(file: File): Promise<Blob> {
  if (!file.type.startsWith('image/') && file.type !== '') {
    throw new ImageError("Ce fichier n'est pas une image.")
  }

  let bitmap: ImageBitmap
  try {
    bitmap = await createImageBitmap(file, { imageOrientation: 'from-image' })
  } catch {
    throw new ImageError("Cette image ne peut pas être ouverte ici. Essaie une photo en JPEG ou en PNG.")
  }

  const scale = Math.min(1, MAX_SIDE / Math.max(bitmap.width, bitmap.height))
  const canvas = document.createElement('canvas')
  canvas.width = Math.round(bitmap.width * scale)
  canvas.height = Math.round(bitmap.height * scale)
  const context = canvas.getContext('2d')
  if (!context) throw new ImageError("L'image n'a pas pu être préparée.")
  // White background: transparent PNGs would otherwise turn black in JPEG.
  context.fillStyle = '#fff'
  context.fillRect(0, 0, canvas.width, canvas.height)
  context.drawImage(bitmap, 0, 0, canvas.width, canvas.height)
  bitmap.close()

  for (const quality of [0.85, 0.7, 0.5]) {
    const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, 'image/jpeg', quality))
    if (blob && blob.size <= MAX_IMAGE_BYTES) return blob
  }
  throw new ImageError('La photo est trop lourde, même réduite. Prends-la de plus près.')
}
