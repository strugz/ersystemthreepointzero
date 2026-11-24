<script setup lang="ts">
import { computed, onBeforeUnmount, reactive, ref, watch } from 'vue';
import type { PDFDocumentProxy } from 'pdfjs-dist/legacy/build/pdf.mjs';
import { useLayoutMode } from '@/shared/composables/useLayoutMode';
import { openPdf, renderPdfPage } from '@/shared/pdfPreview';
import { expensesApi } from '../api';
import { documentTypeLabel, formCodeText, formCodeTitle, hasFormCode } from '../labels';
import type { Attachment } from '../types';

// One receipt at a time, fetched once with the session cookie and kept while the viewer is open.
// Desktop frames a PDF from a blob URL (the API's frame-ancestors header would block framing it
// straight from the API on the Render-hosted SPA). Phones and tablets cannot frame PDFs, so each
// page is drawn as an image with pdf.js; photos and PDF pages then share pinch zoom and swipe.
const props = defineProps<{ reportId: string; attachments: Attachment[] }>();
const index = defineModel<number>('index', { required: true });

const { compact } = useLayoutMode();
const blobs = new Map<number, Blob>();
const docs = new Map<number, Promise<PDFDocumentProxy>>();
const urls = new Map<string, string>();
const url = ref<string | null>(null);
const loading = ref(false);
const failed = ref(false);
const page = ref(1);
const pageCount = ref(1);
const view = reactive({ zoom: 1, x: 0, y: 0, rotation: 0 });
let showToken = 0;

const current = computed<Attachment | null>(() => props.attachments[index.value] ?? null);
const isPdf = computed(() => current.value?.contentType === 'application/pdf');
const framed = computed(() => isPdf.value && !compact.value);
const openUrl = computed(() => (current.value ? expensesApi.attachmentUrl(props.reportId, current.value.id) : undefined));
const downloadUrl = computed(() => (current.value ? expensesApi.attachmentUrl(props.reportId, current.value.id, true) : undefined));

async function fileBlob(a: Attachment): Promise<Blob> {
  let blob = blobs.get(a.id);
  if (!blob) {
    blob = (await expensesApi.fetchAttachment(props.reportId, a.id)).blob;
    blobs.set(a.id, blob);
  }
  return blob;
}

function pdfDoc(a: Attachment): Promise<PDFDocumentProxy> {
  let doc = docs.get(a.id);
  if (!doc) {
    doc = fileBlob(a).then(openPdf);
    doc.catch(() => docs.delete(a.id));
    docs.set(a.id, doc);
  }
  return doc;
}

/** What the stage shows: the file itself, or on phones and tablets one PDF page drawn as an image. */
async function source(a: Attachment, pageNumber: number): Promise<{ url: string; pages: number }> {
  if (a.contentType !== 'application/pdf' || !compact.value) {
    const key = String(a.id);
    let u = urls.get(key);
    if (!u) {
      u = URL.createObjectURL(await fileBlob(a));
      urls.set(key, u);
    }
    return { url: u, pages: 1 };
  }
  const doc = await pdfDoc(a);
  const key = `${a.id}:${pageNumber}`;
  let u = urls.get(key);
  if (!u) {
    u = URL.createObjectURL(await renderPdfPage(doc, pageNumber));
    urls.set(key, u);
  }
  return { url: u, pages: doc.numPages };
}

async function show() {
  const a = current.value;
  const token = ++showToken;
  Object.assign(view, { zoom: 1, x: 0, y: 0, rotation: 0 });
  failed.value = false;
  url.value = null;
  if (!a) return;
  loading.value = true;
  try {
    const s = await source(a, page.value);
    if (token !== showToken) return;
    url.value = s.url;
    pageCount.value = s.pages;
  } catch {
    if (token === showToken) failed.value = true;
  } finally {
    if (token === showToken) loading.value = false;
  }
}

watch(
  current,
  () => {
    pageCount.value = 1;
    if (page.value !== 1) page.value = 1;
    else void show();
  },
  { immediate: true },
);
watch([page, compact], () => void show());

onBeforeUnmount(() => {
  showToken++;
  for (const u of urls.values()) URL.revokeObjectURL(u);
  for (const d of docs.values()) d.then((doc) => doc.destroy()).catch(() => undefined);
  urls.clear();
  docs.clear();
  blobs.clear();
});

function step(delta: number) {
  const n = props.attachments.length;
  if (n > 1) index.value = (index.value + delta + n) % n;
}

// Zoom keeps the point under the fingers (or the tap, or the wheel) where it is; panning stops at
// the picture's edges so it cannot be lost off screen. 1 = fit, up to 5x.
const MAX_ZOOM = 5;
const photo = ref<HTMLElement | null>(null);

function clampPan() {
  const box = photo.value?.getBoundingClientRect();
  if (!box) return;
  const mx = ((view.zoom - 1) * box.width) / 2;
  const my = ((view.zoom - 1) * box.height) / 2;
  view.x = Math.min(mx, Math.max(-mx, view.x));
  view.y = Math.min(my, Math.max(-my, view.y));
}

/** Zoom to `z` around a point given in client coordinates (default: the middle of the picture). */
function setZoom(z: number, at?: { x: number; y: number }) {
  const next = Math.min(MAX_ZOOM, Math.max(1, z));
  const box = photo.value?.getBoundingClientRect();
  if (box && at) {
    const fx = at.x - (box.left + box.width / 2);
    const fy = at.y - (box.top + box.height / 2);
    view.x = fx - ((fx - view.x) * next) / view.zoom;
    view.y = fy - ((fy - view.y) * next) / view.zoom;
  } else {
    view.x = (view.x * next) / view.zoom;
    view.y = (view.y * next) / view.zoom;
  }
  view.zoom = next;
  if (next === 1) Object.assign(view, { x: 0, y: 0 });
  clampPan();
}

function zoomStep(direction: 1 | -1) {
  setZoom(direction > 0 ? view.zoom * 1.5 : view.zoom / 1.5);
}

function onWheel(e: WheelEvent) {
  setZoom(view.zoom * (e.deltaY < 0 ? 1.2 : 1 / 1.2), { x: e.clientX, y: e.clientY });
}

function rotate() {
  view.rotation = (view.rotation + 90) % 360;
}

// Touch and mouse on the picture: pinch to zoom, drag to pan when zoomed, double-tap to zoom in on
// that spot (again to fit), swipe left/right (not zoomed) for the next file.
const pointers = new Map<number, { x: number; y: number }>();
let gesture: { x: number; y: number; vx: number; vy: number; zoom: number; moved: boolean } | null = null;
let pinch: { dist: number; zoom: number; qx: number; qy: number } | null = null;
let lastTap = 0;

function pair(): { mid: { x: number; y: number }; dist: number } {
  const [a, b] = [...pointers.values()];
  return { mid: { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 }, dist: Math.max(1, Math.hypot(a.x - b.x, a.y - b.y)) };
}

function centre(): { x: number; y: number } {
  const box = photo.value?.getBoundingClientRect();
  return box ? { x: box.left + box.width / 2, y: box.top + box.height / 2 } : { x: 0, y: 0 };
}

function startPinch() {
  const { mid, dist } = pair();
  const c = centre();
  // The picture point under the fingers, in unzoomed picture units from the middle.
  pinch = { dist, zoom: view.zoom, qx: (mid.x - c.x - view.x) / view.zoom, qy: (mid.y - c.y - view.y) / view.zoom };
}

function onDown(e: PointerEvent) {
  if (pointers.size >= 2) return;
  (e.currentTarget as HTMLElement).setPointerCapture(e.pointerId);
  pointers.set(e.pointerId, { x: e.clientX, y: e.clientY });
  if (pointers.size === 2) {
    startPinch();
    if (gesture) gesture.moved = true;
    return;
  }
  gesture = { x: e.clientX, y: e.clientY, vx: view.x, vy: view.y, zoom: view.zoom, moved: false };
}

function onMove(e: PointerEvent) {
  if (!gesture || !pointers.has(e.pointerId)) return;
  pointers.set(e.pointerId, { x: e.clientX, y: e.clientY });
  if (pinch && pointers.size === 2) {
    const { mid, dist } = pair();
    const c = centre();
    const z = Math.min(MAX_ZOOM, Math.max(1, (pinch.zoom * dist) / pinch.dist));
    view.zoom = z;
    view.x = mid.x - c.x - z * pinch.qx;
    view.y = mid.y - c.y - z * pinch.qy;
    if (z === 1) Object.assign(view, { x: 0, y: 0 });
    clampPan();
    return;
  }
  const dx = e.clientX - gesture.x;
  const dy = e.clientY - gesture.y;
  if (Math.abs(dx) > 6 || Math.abs(dy) > 6) gesture.moved = true;
  if (view.zoom > 1) {
    view.x = gesture.vx + dx;
    view.y = gesture.vy + dy;
    clampPan();
  }
}

function onUp(e: PointerEvent) {
  pointers.delete(e.pointerId);
  const g = gesture;
  if (pointers.size === 1 && g) {
    // One finger lifted from a pinch: keep panning with the other.
    pinch = null;
    const [p] = [...pointers.values()];
    gesture = { ...g, x: p.x, y: p.y, vx: view.x, vy: view.y, zoom: view.zoom };
    return;
  }
  pinch = null;
  gesture = null;
  if (!g || pointers.size > 0) return;
  const dx = e.clientX - g.x;
  const dy = e.clientY - g.y;
  if (!g.moved) {
    const now = Date.now();
    if (now - lastTap < 300) {
      if (view.zoom > 1) setZoom(1);
      else setZoom(2.5, { x: e.clientX, y: e.clientY });
      lastTap = 0;
    } else {
      lastTap = now;
    }
  } else if (g.zoom === 1 && view.zoom === 1 && Math.abs(dx) > 60 && Math.abs(dx) > Math.abs(dy) * 1.5) {
    step(dx < 0 ? 1 : -1);
  }
}

function onCancel(e: PointerEvent) {
  pointers.delete(e.pointerId);
  pinch = null;
  if (pointers.size === 0) gesture = null;
}
</script>

<template>
  <div class="viewer">
    <!-- Desktop: toolbar -->
    <div v-if="!compact" class="viewer__bar">
      <v-btn class="press" icon="mdi-chevron-left" size="small" variant="text" aria-label="Previous file" :disabled="attachments.length < 2" @click="step(-1)" />
      <span class="viewer__count mono">{{ attachments.length ? index + 1 : 0 }} / {{ attachments.length }}</span>
      <v-btn class="press" icon="mdi-chevron-right" size="small" variant="text" aria-label="Next file" :disabled="attachments.length < 2" @click="step(1)" />
      <div v-if="current" class="viewer__name">
        <span class="text-truncate" :title="current.fileName">{{ current.fileName }}</span>
        <span v-if="current.documentType && current.documentType !== 'Receipt'" class="pill pill--neutral">{{ documentTypeLabel[current.documentType] }}</span>
        <span v-if="hasFormCode(current.documentType)" class="pill" :class="current.formCodeCheck === 'Found' ? 'pill--success' : 'pill--warning'" :title="formCodeTitle[current.formCodeCheck ?? 'None']">
          {{ formCodeText[current.formCodeCheck ?? 'None'] }}
        </span>
      </div>
      <template v-if="current">
        <template v-if="!isPdf">
          <v-btn class="press" icon="mdi-magnify-minus-outline" size="small" variant="text" aria-label="Zoom out" :disabled="view.zoom <= 1" @click="zoomStep(-1)" />
          <v-btn class="press" size="small" variant="text" :aria-label="`Fit (now ${Math.round(view.zoom * 100)}%)`" @click="setZoom(1)">{{ Math.round(view.zoom * 100) }}%</v-btn>
          <v-btn class="press" icon="mdi-magnify-plus-outline" size="small" variant="text" aria-label="Zoom in" :disabled="view.zoom >= MAX_ZOOM" @click="zoomStep(1)" />
          <v-btn class="press" icon="mdi-rotate-right" size="small" variant="text" aria-label="Rotate" @click="rotate" />
        </template>
        <v-btn class="press" icon="mdi-open-in-new" size="small" variant="text" aria-label="Open in new tab" :href="openUrl" target="_blank" rel="noopener" />
        <v-btn class="press" icon="mdi-download-outline" size="small" variant="text" aria-label="Download" :href="downloadUrl" />
      </template>
    </div>

    <div class="viewer__stage">
      <div v-if="!current" class="viewer__note text-body-2 text-medium-emphasis">No receipts are attached to this report.</div>
      <v-progress-circular v-else-if="loading" indeterminate color="primary" class="viewer__note" />
      <div v-else-if="failed" class="viewer__note text-body-2">
        Could not load this file.
        <v-btn class="press" size="small" variant="text" color="primary" @click="show">Try again</v-btn>
      </div>
      <iframe v-else-if="url && framed" :key="url" :src="url" :title="current.fileName" class="viewer__pdf" />
      <div
        v-else-if="url"
        ref="photo"
        class="viewer__photo"
        @wheel.prevent="onWheel"
        :class="{ 'viewer__photo--zoomed': view.zoom > 1 }"
        @pointerdown="onDown"
        @pointermove="onMove"
        @pointerup="onUp"
        @pointercancel="onCancel"
      >
        <img
          :src="url"
          :alt="current.fileName"
          class="viewer__img"
          draggable="false"
          :style="{ transform: `translate(${view.x}px, ${view.y}px) scale(${view.zoom}) rotate(${view.rotation}deg)` }"
        />
      </div>

      <!-- Phone and tablet controls over the picture -->
      <template v-if="compact && current">
        <div class="float float--tr">
          <v-btn v-if="url" class="press" icon="mdi-rotate-right" size="small" variant="flat" aria-label="Rotate" @click="rotate" />
          <v-btn class="press" icon="mdi-open-in-new" size="small" variant="flat" aria-label="Open in new tab" :href="openUrl" target="_blank" rel="noopener" />
        </div>
        <div v-if="pageCount > 1" class="float float--tl pager">
          <button type="button" class="pager__btn press" aria-label="Previous page" :disabled="page <= 1" @click="page--"><v-icon icon="mdi-chevron-left" size="18" /></button>
          <span class="mono">Page {{ page }} / {{ pageCount }}</span>
          <button type="button" class="pager__btn press" aria-label="Next page" :disabled="page >= pageCount" @click="page++"><v-icon icon="mdi-chevron-right" size="18" /></button>
        </div>
        <div v-if="url" class="float float--left-bottom pager zoom" role="group" aria-label="Zoom">
          <button type="button" class="pager__btn press" aria-label="Zoom out" :disabled="view.zoom <= 1" @click="zoomStep(-1)"><v-icon icon="mdi-minus" size="20" /></button>
          <button type="button" class="zoom__level mono press" :aria-label="`Fit to screen (now ${Math.round(view.zoom * 100)}%)`" @click="setZoom(1)">{{ Math.round(view.zoom * 100) }}%</button>
          <button type="button" class="pager__btn press" aria-label="Zoom in" :disabled="view.zoom >= MAX_ZOOM" @click="zoomStep(1)"><v-icon icon="mdi-plus" size="20" /></button>
        </div>
        <div v-if="attachments.length > 1" class="float float--right-bottom pager">
          <button type="button" class="pager__btn press" aria-label="Previous receipt" @click="step(-1)"><v-icon icon="mdi-chevron-left" size="20" /></button>
          <span class="mono">{{ index + 1 }} / {{ attachments.length }}</span>
          <button type="button" class="pager__btn press" aria-label="Next receipt" @click="step(1)"><v-icon icon="mdi-chevron-right" size="20" /></button>
        </div>
      </template>
    </div>

    <div v-if="compact && current" class="caption">
      <v-icon :icon="isPdf ? 'mdi-file-pdf-box' : 'mdi-image-outline'" size="16" />
      <span class="caption__name text-truncate" :title="current.fileName">{{ current.fileName }}</span>
      <span v-if="current.documentType && current.documentType !== 'Receipt'" class="pill pill--neutral">{{ documentTypeLabel[current.documentType] }}</span>
      <span v-if="hasFormCode(current.documentType)" class="pill" :class="current.formCodeCheck === 'Found' ? 'pill--success' : 'pill--warning'">
        {{ formCodeText[current.formCodeCheck ?? 'None'] }}
      </span>
    </div>
  </div>
</template>

<style scoped>
.viewer {
  display: flex;
  flex-direction: column;
  min-height: 0;
  height: 100%;
  background: rgb(var(--v-theme-surface));
}
.viewer__bar {
  display: flex;
  align-items: center;
  gap: 2px;
  padding: 4px 8px;
  border-bottom: 1px solid var(--line);
  min-width: 0;
}
.viewer__count {
  font-size: 12px;
  color: var(--ink-2);
  min-width: 36px;
  text-align: center;
}
.viewer__name {
  flex: 1;
  min-width: 0;
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  padding: 0 6px;
  overflow: hidden;
}
.viewer__stage {
  position: relative;
  flex: 1;
  min-height: 0;
  background: var(--line);
  display: flex;
  overflow: hidden;
}
.viewer__note {
  margin: auto;
  text-align: center;
  padding: 16px;
}
.viewer__pdf {
  flex: 1;
  width: 100%;
  border: 0;
  background: #fff;
}
.viewer__photo {
  flex: 1;
  min-width: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 8px;
  touch-action: none;
  user-select: none;
  cursor: zoom-in;
}
.viewer__photo--zoomed {
  cursor: grab;
}
.viewer__img {
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
  transform-origin: center center;
  pointer-events: none;
}
.float {
  position: absolute;
  display: flex;
  align-items: center;
  gap: 6px;
}
.float--tr {
  top: 8px;
  right: 8px;
}
.float--tl {
  top: 8px;
  left: 8px;
}
.float--left-bottom {
  bottom: 10px;
  left: 8px;
}
.float--right-bottom {
  bottom: 10px;
  right: 8px;
}
.zoom__level {
  min-width: 48px;
  height: 36px;
  border-radius: 18px;
  background: none;
  border: 0;
  font: inherit;
  font-size: 12px;
  font-weight: 600;
  color: var(--ink);
  cursor: pointer;
}
.float :deep(.v-btn),
.pager {
  background: rgba(var(--v-theme-surface), 0.9);
  backdrop-filter: blur(6px);
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.18);
}
.pager {
  border-radius: 999px;
  padding: 2px;
  font-size: 12px;
  font-weight: 600;
  color: var(--ink-2);
}
.pager__btn {
  width: 36px;
  height: 36px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  background: none;
  border: 0;
  color: var(--ink);
  cursor: pointer;
}
.pager__btn:disabled {
  opacity: 0.35;
  cursor: default;
}
.caption {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 12px;
  font-size: 12px;
  color: var(--ink-3);
  border-bottom: 1px solid var(--line);
}
.caption__name {
  flex: 1;
  min-width: 0;
}
</style>
