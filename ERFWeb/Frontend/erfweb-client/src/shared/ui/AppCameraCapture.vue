<script setup lang="ts">
// Full-screen in-app camera for receipts and documents. A file input with `capture` is not reliable (Android shows
// the file chooser for a list of MIME types, some browsers ignore it), so the page opens the camera itself through
// getUserMedia. When the camera cannot be opened (no permission, no camera, not HTTPS) the phone's own camera app
// is offered instead.
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue';

const props = withDefaults(defineProps<{ modelValue: boolean; title?: string }>(), { title: 'Take photo' });

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  capture: [file: File];
}>();

type Phase = 'starting' | 'live' | 'review' | 'error';

const phase = ref<Phase>('starting');
const errorText = ref('');
const video = ref<HTMLVideoElement | null>(null);
const fallback = ref<HTMLInputElement | null>(null);
const facing = ref<'environment' | 'user'>('environment');
const canFlip = ref(false);
const flash = ref(false);
const shot = ref<{ file: File; url: string } | null>(null);
// Light clean-up on the review screen: quarter turns and a "scan" look (grey, paper whitened, ink darkened).
const turns = ref(0);
const enhanced = ref(false);
const rendering = ref(false);
let frame: HTMLCanvasElement | null = null;
let stream: MediaStream | null = null;
// Longest side kept from the camera frame; the upload shrinks to 1600 px anyway, so this keeps clean-up fast.
const MAX_SIDE = 2000;

const busy = computed(() => phase.value === 'starting');

function stop() {
  stream?.getTracks().forEach((t) => t.stop());
  stream = null;
}

function clearShot() {
  if (shot.value) URL.revokeObjectURL(shot.value.url);
  shot.value = null;
}

async function start() {
  stop();
  phase.value = 'starting';
  if (!navigator.mediaDevices?.getUserMedia) {
    fail('This browser cannot open the camera here.');
    return;
  }
  try {
    stream = await navigator.mediaDevices.getUserMedia({
      audio: false,
      video: { facingMode: { ideal: facing.value }, width: { ideal: 1920 }, height: { ideal: 1080 } },
    });
    if (!props.modelValue) {
      stop();
      return;
    }
    const devices = await navigator.mediaDevices.enumerateDevices();
    canFlip.value = devices.filter((d) => d.kind === 'videoinput').length > 1;
    await nextTick();
    if (video.value) {
      video.value.srcObject = stream;
      await video.value.play();
    }
    phase.value = 'live';
  } catch (e) {
    const name = e instanceof DOMException ? e.name : '';
    fail(
      name === 'NotAllowedError'
        ? 'Camera access is blocked. Allow the camera for this site in the browser settings, or use the camera app.'
        : name === 'NotFoundError' || name === 'OverconstrainedError'
          ? 'No camera was found on this device.'
          : 'The camera could not be opened.',
    );
  }
}

function fail(text: string) {
  stop();
  errorText.value = text;
  phase.value = 'error';
}

function fileName(): string {
  const d = new Date();
  const p = (n: number) => String(n).padStart(2, '0');
  return `photo-${d.getFullYear()}${p(d.getMonth() + 1)}${p(d.getDate())}-${p(d.getHours())}${p(d.getMinutes())}${p(d.getSeconds())}.jpg`;
}

function snap() {
  const v = video.value;
  if (!v || !v.videoWidth) return;
  const scale = Math.min(1, MAX_SIDE / Math.max(v.videoWidth, v.videoHeight));
  const canvas = document.createElement('canvas');
  canvas.width = Math.round(v.videoWidth * scale);
  canvas.height = Math.round(v.videoHeight * scale);
  canvas.getContext('2d')?.drawImage(v, 0, 0, canvas.width, canvas.height);
  flash.value = true;
  window.setTimeout(() => (flash.value = false), 160);
  frame = canvas;
  turns.value = 0;
  enhanced.value = false;
  stop();
  phase.value = 'review';
  void render();
}

// Whitens the paper and darkens the ink: grey levels stretched so the darkest 2 % become black and the
// brightest 10 % (the paper) become white, then a slight curve to lift greyish paper further.
function enhance(ctx: CanvasRenderingContext2D, width: number, height: number) {
  const img = ctx.getImageData(0, 0, width, height);
  const d = img.data;
  const histogram = new Uint32Array(256);
  for (let i = 0; i < d.length; i += 4) {
    const y = (d[i] * 299 + d[i + 1] * 587 + d[i + 2] * 114) / 1000;
    d[i] = y;
    histogram[d[i]]++;
  }
  const total = width * height;
  let low = 0;
  let high = 255;
  for (let n = 0, v = 0; v < 256; v++) {
    n += histogram[v];
    if (n >= total * 0.02) {
      low = v;
      break;
    }
  }
  for (let n = 0, v = 255; v >= 0; v--) {
    n += histogram[v];
    if (n >= total * 0.1) {
      high = v;
      break;
    }
  }
  const range = Math.max(1, high - low);
  const lut = new Uint8ClampedArray(256);
  for (let v = 0; v < 256; v++) {
    const t = Math.min(1, Math.max(0, (v - low) / range));
    lut[v] = Math.round(255 * Math.pow(t, 0.8));
  }
  for (let i = 0; i < d.length; i += 4) {
    const y = lut[d[i]];
    d[i] = y;
    d[i + 1] = y;
    d[i + 2] = y;
  }
  ctx.putImageData(img, 0, 0);
}

async function render() {
  if (!frame) return;
  rendering.value = true;
  const sideways = turns.value % 2 === 1;
  const canvas = document.createElement('canvas');
  canvas.width = sideways ? frame.height : frame.width;
  canvas.height = sideways ? frame.width : frame.height;
  const ctx = canvas.getContext('2d', { willReadFrequently: enhanced.value });
  if (!ctx) {
    rendering.value = false;
    return;
  }
  ctx.translate(canvas.width / 2, canvas.height / 2);
  ctx.rotate((turns.value * Math.PI) / 2);
  ctx.drawImage(frame, -frame.width / 2, -frame.height / 2);
  ctx.setTransform(1, 0, 0, 1, 0, 0);
  if (enhanced.value) enhance(ctx, canvas.width, canvas.height);
  const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, 'image/jpeg', 0.92));
  rendering.value = false;
  if (!blob || phase.value !== 'review') return;
  clearShot();
  const file = new File([blob], fileName(), { type: 'image/jpeg' });
  shot.value = { file, url: URL.createObjectURL(file) };
}

function rotate() {
  turns.value = (turns.value + 1) % 4;
  void render();
}

function toggleEnhance() {
  enhanced.value = !enhanced.value;
  void render();
}

function retake() {
  clearShot();
  frame = null;
  void start();
}

function use() {
  if (!shot.value) return;
  emit('capture', shot.value.file);
  close();
}

function flip() {
  facing.value = facing.value === 'environment' ? 'user' : 'environment';
  void start();
}

function onFallback(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  input.value = '';
  if (file) {
    emit('capture', file);
    close();
  }
}

function close() {
  emit('update:modelValue', false);
}

watch(
  () => props.modelValue,
  (open) => {
    if (open) {
      facing.value = 'environment';
      void start();
    } else {
      stop();
      clearShot();
      frame = null;
    }
  },
  { immediate: true },
);

onBeforeUnmount(() => {
  stop();
  clearShot();
});
</script>

<template>
  <v-dialog :model-value="modelValue" fullscreen :scrim="false" transition="fade-transition" @update:model-value="(v: boolean) => !v && close()">
    <div class="cam" role="dialog" :aria-label="title">
      <header class="cam__top">
        <v-btn class="press" icon="mdi-close" variant="text" color="white" aria-label="Close camera" @click="close" />
        <span class="cam__title">{{ title }}</span>
        <span class="cam__spacer" />
      </header>

      <div class="cam__stage">
        <video v-show="phase === 'live' || phase === 'starting'" ref="video" class="cam__video" playsinline muted autoplay />
        <img v-if="phase === 'review' && shot" :key="shot.url" :src="shot.url" alt="Captured photo" class="cam__still" />
        <v-progress-circular v-if="phase === 'review' && rendering && !shot" indeterminate color="white" size="32" width="3" class="cam__center" />

        <div v-if="phase === 'live'" class="cam__frame" aria-hidden="true">
          <span class="cam__corner cam__corner--tl" /><span class="cam__corner cam__corner--tr" />
          <span class="cam__corner cam__corner--bl" /><span class="cam__corner cam__corner--br" />
        </div>
        <p v-if="phase === 'live'" class="cam__hint">Fit the whole document inside the frame</p>

        <v-progress-circular v-if="busy" indeterminate color="white" size="32" width="3" class="cam__center" />

        <div v-if="phase === 'error'" class="cam__error">
          <v-icon icon="mdi-camera-off-outline" size="40" />
          <p>{{ errorText }}</p>
          <v-btn class="press" color="white" variant="flat" prepend-icon="mdi-camera-outline" @click="fallback?.click()">Use the camera app</v-btn>
          <v-btn class="press" color="white" variant="text" @click="start">Try again</v-btn>
        </div>

        <div class="cam__flash" :class="{ 'cam__flash--on': flash }" aria-hidden="true" />
      </div>

      <div v-if="phase === 'review'" class="cam__tools">
        <button type="button" class="cam__tool press" :disabled="rendering" @click="rotate">
          <v-icon icon="mdi-rotate-right" size="22" />
          <span>Rotate</span>
        </button>
        <button
          type="button"
          class="cam__tool press"
          :class="{ 'cam__tool--on': enhanced }"
          :aria-pressed="enhanced"
          :disabled="rendering"
          @click="toggleEnhance"
        >
          <v-icon icon="mdi-file-document-outline" size="22" />
          <span>Scan look</span>
        </button>
      </div>

      <footer class="cam__bar">
        <template v-if="phase === 'review'">
          <v-btn class="press cam__wide" variant="outlined" color="white" size="large" prepend-icon="mdi-restore" @click="retake">Retake</v-btn>
          <v-btn
            class="press cam__wide"
            variant="flat"
            color="primary"
            size="large"
            prepend-icon="mdi-check"
            :disabled="rendering || !shot"
            @click="use"
          >
            Use photo
          </v-btn>
        </template>
        <template v-else>
          <span class="cam__side" />
          <button type="button" class="cam__shutter" :disabled="phase !== 'live'" aria-label="Take photo" @click="snap">
            <span class="cam__shutter-core" />
          </button>
          <span class="cam__side">
            <v-btn
              v-if="canFlip && phase === 'live'"
              class="press"
              icon="mdi-camera-flip-outline"
              variant="tonal"
              color="white"
              aria-label="Switch camera"
              @click="flip"
            />
          </span>
        </template>
      </footer>

      <input ref="fallback" type="file" accept="image/*" capture="environment" hidden @change="onFallback" />
    </div>
  </v-dialog>
</template>

<style scoped>
.cam {
  position: fixed;
  inset: 0;
  display: flex;
  flex-direction: column;
  background: #000;
  color: #fff;
}
.cam__top {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: max(8px, env(safe-area-inset-top)) 8px 8px;
}
.cam__title {
  flex: 1;
  text-align: center;
  font-size: 15px;
  font-weight: 600;
}
.cam__spacer {
  width: 48px;
}
.cam__stage {
  position: relative;
  flex: 1;
  min-height: 0;
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
}
.cam__video,
.cam__still {
  width: 100%;
  height: 100%;
  object-fit: contain;
}
.cam__still {
  animation: cam-still 220ms var(--ease-out) both;
}
@keyframes cam-still {
  from {
    opacity: 0;
    transform: scale(0.97);
  }
}
/* Frame guide: the area outside is dimmed so the eye goes to the document. */
.cam__frame {
  position: absolute;
  inset: 10% 8% 14%;
  border-radius: 14px;
  box-shadow: 0 0 0 100vmax rgba(0, 0, 0, 0.35);
  pointer-events: none;
}
.cam__corner {
  position: absolute;
  width: 28px;
  height: 28px;
  border: 3px solid #fff;
}
.cam__corner--tl {
  top: -2px;
  left: -2px;
  border-right: 0;
  border-bottom: 0;
  border-top-left-radius: 14px;
}
.cam__corner--tr {
  top: -2px;
  right: -2px;
  border-left: 0;
  border-bottom: 0;
  border-top-right-radius: 14px;
}
.cam__corner--bl {
  bottom: -2px;
  left: -2px;
  border-right: 0;
  border-top: 0;
  border-bottom-left-radius: 14px;
}
.cam__corner--br {
  bottom: -2px;
  right: -2px;
  border-left: 0;
  border-top: 0;
  border-bottom-right-radius: 14px;
}
.cam__hint {
  position: absolute;
  bottom: 4%;
  left: 0;
  right: 0;
  margin: 0;
  text-align: center;
  font-size: 13px;
  color: rgba(255, 255, 255, 0.85);
}
.cam__center {
  position: absolute;
}
.cam__error {
  position: absolute;
  inset: 0;
  padding: 24px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 12px;
  text-align: center;
  font-size: 14px;
  color: rgba(255, 255, 255, 0.9);
}
.cam__error p {
  margin: 0 0 4px;
  max-width: 320px;
}
/* Shutter flash: instant on, quick fade out, so the tap feels heard. */
.cam__flash {
  position: absolute;
  inset: 0;
  background: #fff;
  opacity: 0;
  pointer-events: none;
  transition: opacity 160ms var(--ease-out);
}
.cam__flash--on {
  opacity: 0.6;
  transition-duration: 0ms;
}
.cam__tools {
  display: flex;
  justify-content: center;
  gap: 12px;
  padding: 12px 24px 0;
}
.cam__tool {
  min-width: 96px;
  min-height: 56px;
  border-radius: 12px;
  border: 1px solid rgba(255, 255, 255, 0.25);
  background: rgba(255, 255, 255, 0.08);
  color: #fff;
  font: inherit;
  font-size: 12px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 4px;
  cursor: pointer;
  -webkit-tap-highlight-color: transparent;
  transition:
    background-color 150ms ease,
    color 150ms ease,
    transform var(--dur-press) var(--ease-out);
}
.cam__tool--on {
  background: #fff;
  color: #000;
  border-color: #fff;
}
.cam__tool:disabled {
  opacity: 0.5;
  cursor: default;
}
.cam__tool:focus-visible {
  outline: 2px solid #fff;
  outline-offset: 2px;
}
.cam__bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 16px 24px max(20px, env(safe-area-inset-bottom));
  min-height: 112px;
}
.cam__side {
  width: 56px;
  display: flex;
  justify-content: center;
}
.cam__wide {
  flex: 1;
}
.cam__shutter {
  width: 74px;
  height: 74px;
  border-radius: 50%;
  border: 4px solid #fff;
  background: transparent;
  padding: 4px;
  cursor: pointer;
  -webkit-tap-highlight-color: transparent;
}
.cam__shutter:disabled {
  opacity: 0.4;
  cursor: default;
}
.cam__shutter:focus-visible {
  outline: 2px solid #fff;
  outline-offset: 4px;
}
.cam__shutter-core {
  display: block;
  width: 100%;
  height: 100%;
  border-radius: 50%;
  background: #fff;
  transition: transform var(--dur-press) var(--ease-out);
}
.cam__shutter:not(:disabled):active .cam__shutter-core {
  transform: scale(0.88);
}
@media (prefers-reduced-motion: reduce) {
  .cam__still {
    animation: none;
  }
}
</style>
