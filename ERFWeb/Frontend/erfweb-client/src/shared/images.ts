// Browser-side receipt image compression. Phone photos arrive at 3–5 MB and 4000+ px; a receipt is
// perfectly legible at ~1600 px / JPEG 80 %, so we shrink before the bytes leave the device. The API
// keeps its own size and type limits as the safety net. PDFs and unknown types pass through untouched.

export interface CompressOptions {
  /** Longest side after resize, in CSS pixels. */
  maxSide?: number;
  /** JPEG quality 0–1. */
  quality?: number;
  /** Images already at or under this size are sent as-is. */
  skipUnderBytes?: number;
}

export interface CompressResult {
  file: File;
  originalBytes: number;
  /** True when the compressed output replaced the original. */
  compressed: boolean;
}

const DEFAULTS: Required<CompressOptions> = { maxSide: 1600, quality: 0.8, skipUnderBytes: 300 * 1024 };
const IMAGE_TYPES = new Set(['image/jpeg', 'image/png']);

function jpegName(name: string): string {
  const dot = name.lastIndexOf('.');
  return `${dot > 0 ? name.slice(0, dot) : name}.jpg`;
}

async function decode(file: File): Promise<ImageBitmap | HTMLImageElement> {
  if (typeof createImageBitmap === 'function') {
    // imageOrientation applies the EXIF rotation so sideways phone photos come out upright.
    try {
      return await createImageBitmap(file, { imageOrientation: 'from-image' });
    } catch {
      // Fall through to the <img> path (older Safari).
    }
  }
  const url = URL.createObjectURL(file);
  try {
    return await new Promise<HTMLImageElement>((resolve, reject) => {
      const img = new Image();
      img.onload = () => resolve(img);
      img.onerror = () => reject(new Error('The image could not be read.'));
      img.src = url;
    });
  } finally {
    URL.revokeObjectURL(url);
  }
}

function toBlob(canvas: HTMLCanvasElement, quality: number): Promise<Blob | null> {
  return new Promise((resolve) => canvas.toBlob(resolve, 'image/jpeg', quality));
}

/**
 * Resizes and re-encodes JPEG/PNG files as JPEG. Returns the original when the file is not an image,
 * is already small, cannot be decoded, or when re-encoding would not make it smaller.
 */
export async function compressImage(file: File, options: CompressOptions = {}): Promise<CompressResult> {
  const opts = { ...DEFAULTS, ...options };
  const passthrough: CompressResult = { file, originalBytes: file.size, compressed: false };

  if (!IMAGE_TYPES.has(file.type) || file.size <= opts.skipUnderBytes) {
    return passthrough;
  }

  let source: ImageBitmap | HTMLImageElement;
  try {
    source = await decode(file);
  } catch {
    return passthrough;
  }

  const width = 'naturalWidth' in source ? source.naturalWidth : source.width;
  const height = 'naturalHeight' in source ? source.naturalHeight : source.height;
  if (!width || !height) {
    return passthrough;
  }

  const scale = Math.min(1, opts.maxSide / Math.max(width, height));
  const canvas = document.createElement('canvas');
  canvas.width = Math.round(width * scale);
  canvas.height = Math.round(height * scale);
  const ctx = canvas.getContext('2d');
  if (!ctx) {
    return passthrough;
  }
  // PNG screenshots may have transparency; JPEG has none, so paint white behind the image.
  ctx.fillStyle = '#fff';
  ctx.fillRect(0, 0, canvas.width, canvas.height);
  ctx.drawImage(source, 0, 0, canvas.width, canvas.height);
  if ('close' in source) {
    source.close();
  }

  const blob = await toBlob(canvas, opts.quality);
  if (!blob || blob.size >= file.size) {
    return passthrough;
  }

  return {
    file: new File([blob], jpegName(file.name), { type: 'image/jpeg', lastModified: file.lastModified }),
    originalBytes: file.size,
    compressed: true,
  };
}
