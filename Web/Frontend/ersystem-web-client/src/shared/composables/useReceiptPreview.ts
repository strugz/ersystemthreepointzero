import { onBeforeUnmount, ref } from 'vue'
import type { ReceiptAttachment } from '@/shared/types/reportReview'
import { receiptDisplayName } from '@/shared/utils/reportReview'

export function useReceiptPreview(
  loadAttachment: (attachmentId: number) => Promise<Blob>,
  showError: (message: string) => void
) {
  const open = ref(false)
  const openingAttachmentId = ref<number | null>(null)
  const url = ref('')
  const contentType = ref('')
  const title = ref('')

  function release() {
    open.value = false
    if (url.value) URL.revokeObjectURL(url.value)
    url.value = ''
    contentType.value = ''
    title.value = ''
  }

  async function preview(attachment: ReceiptAttachment, index: number) {
    if (openingAttachmentId.value != null) return
    release()
    openingAttachmentId.value = attachment.id
    try {
      const blob = await loadAttachment(attachment.id)
      if (blob.size === 0) throw new Error('The receipt file is empty.')
      url.value = URL.createObjectURL(blob)
      contentType.value = blob.type || attachment.contentType || 'application/octet-stream'
      title.value = receiptDisplayName(attachment, index)
      open.value = true
    } catch (caught) {
      showError(caught instanceof Error ? caught.message : 'Unable to open the receipt.')
    } finally {
      openingAttachmentId.value = null
    }
  }

  function requestClose() { open.value = false }

  function openExternally() {
    if (url.value) window.open(url.value, '_blank', 'noopener,noreferrer')
  }

  function download() {
    if (!url.value) return
    const link = document.createElement('a')
    link.href = url.value
    link.download = title.value || 'scanned-receipt'
    link.rel = 'noopener'
    document.body.appendChild(link)
    link.click()
    link.remove()
  }

  onBeforeUnmount(release)

  return {
    open,
    openingAttachmentId,
    url,
    contentType,
    title,
    preview,
    requestClose,
    release,
    openExternally,
    download
  }
}
