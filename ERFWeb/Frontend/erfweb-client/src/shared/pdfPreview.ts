import type { PDFDocumentProxy } from 'pdfjs-dist/legacy/build/pdf.mjs';

// Draws PDF pages as images where the browser cannot frame a PDF (Android shows a blank frame, iOS
// only page 1). pdf.js (pinned to 4.x: 5.x needs newer browsers than some phones have) is loaded on
// first use only, in its own chunk with its worker, and never runs scripts embedded in a PDF.
type PdfJs = typeof import('pdfjs-dist/legacy/build/pdf.mjs');

let pdfjs: Promise<PdfJs> | null = null;

function loadPdfJs(): Promise<PdfJs> {
  // `?worker` makes Vite bundle the worker as a .js file: a static host may not serve .mjs as JavaScript.
  pdfjs ??= Promise.all([import('pdfjs-dist/legacy/build/pdf.mjs'), import('pdfjs-dist/legacy/build/pdf.worker.min.mjs?worker')]).then(
    ([lib, worker]) => {
      lib.GlobalWorkerOptions.workerPort = new worker.default();
      return lib;
    },
  );
  return pdfjs;
}

export async function openPdf(blob: Blob): Promise<PDFDocumentProxy> {
  const lib = await loadPdfJs();
  return lib.getDocument({ data: new Uint8Array(await blob.arrayBuffer()), isEvalSupported: false, enableXfa: false }).promise;
}

/** One page as a PNG about 2400 px wide, so small print stays sharp when the reviewer zooms in. */
export async function renderPdfPage(doc: PDFDocumentProxy, pageNumber: number): Promise<Blob> {
  const page = await doc.getPage(pageNumber);
  try {
    const scale = Math.min(4, Math.max(1, 2400 / page.getViewport({ scale: 1 }).width));
    const viewport = page.getViewport({ scale });
    const canvas = document.createElement('canvas');
    canvas.width = Math.ceil(viewport.width);
    canvas.height = Math.ceil(viewport.height);
    const context = canvas.getContext('2d');
    if (!context) throw new Error('Canvas is not available.');
    context.fillStyle = '#fff';
    context.fillRect(0, 0, canvas.width, canvas.height);
    await page.render({ canvasContext: context, viewport }).promise;
    return await new Promise<Blob>((resolve, reject) =>
      canvas.toBlob((blob) => (blob ? resolve(blob) : reject(new Error('The page could not be drawn.'))), 'image/png'),
    );
  } finally {
    page.cleanup();
  }
}
