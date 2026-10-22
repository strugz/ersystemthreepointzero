// Saves a Blob the SPA already holds (fetched with the session cookie) as a file download.

/** Export formats the API accepts (ExportFormat on the server). */
export type ExportFormat = 'Csv' | 'Pdf';

export function saveFile(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  link.rel = 'noopener';
  document.body.appendChild(link);
  link.click();
  link.remove();
  // Revoke after the click has been handled; revoking synchronously can cancel the download in some browsers.
  setTimeout(() => URL.revokeObjectURL(url), 0);
}
