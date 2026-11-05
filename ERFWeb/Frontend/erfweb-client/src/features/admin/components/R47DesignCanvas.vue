<script setup lang="ts">
// A scaled drawing of the R47 page in points (viewBox 792×612). Band edges and column dividers are
// handles: drag them, or focus one and use the arrow keys (Shift for 5 pt). Clicking a band selects its
// section in the panel. The drawing is a schematic; the PDF preview shows the real print.
import { computed, ref } from 'vue';
import type { R47Design } from '../types';
import {
  GRID_COLUMN_NAMES,
  PAGE_HEIGHT,
  PAGE_WIDTH,
  RANGES,
  bands,
  clamp,
  columnEdges,
  contentWidth,
  dateBoxWidth,
  moveColumnDivider,
  promiseLines,
  round,
  titleWidth,
  type R47Section,
  type R47SizeKey,
} from '../r47Design';

const props = defineProps<{ modelValue: R47Design; selected: R47Section | null }>();
const emit = defineEmits<{
  'update:modelValue': [value: R47Design];
  select: [section: R47Section];
  /** Before a drag or key move starts, so the parent can record an undo step. */
  checkpoint: [];
}>();

const d = computed(() => props.modelValue);
const b = computed(() => bands(d.value));
const x = computed(() => columnEdges(d.value));
const left = computed(() => d.value.marginLeft);
const width = computed(() => contentWidth(d.value));
const right = computed(() => left.value + width.value);
const dateX = computed(() => right.value - dateBoxWidth(d.value));

interface Handle {
  id: string;
  label: string;
  vertical: boolean;
  /** Fixed coordinate of the line (x when vertical, y when horizontal) and its extent. */
  at: number;
  from: number;
  to: number;
  apply: (design: R47Design, pos: number) => R47Design;
}

function size(key: R47SizeKey, value: number) {
  return (design: R47Design): R47Design => ({ ...design, [key]: clamp(round(value), RANGES[key]) });
}

const handles = computed<Handle[]>(() => {
  const v = d.value;
  const bb = b.value;
  const xs = x.value;
  const titleX = left.value + v.logoWidth;
  const codeX = titleX + titleWidth(v);
  const pageX = codeX + v.documentCodeWidth;
  const list: Handle[] = [
    { id: 'marginTop', label: 'Top margin', vertical: false, at: bb.header, from: left.value, to: right.value, apply: (s, p) => size('marginTop', p)(s) },
    { id: 'headerHeight', label: 'Header height', vertical: false, at: bb.dateStamp, from: left.value, to: dateX.value, apply: (s, p) => size('headerHeight', p - bb.header)(s) },
    { id: 'dateStampHeight', label: 'DATE stamp height', vertical: false, at: bb.employee, from: dateX.value, to: right.value, apply: (s, p) => size('dateStampHeight', p - bb.dateStamp)(s) },
    { id: 'employeeLineHeight', label: 'Employee line height', vertical: false, at: bb.heading, from: left.value, to: right.value, apply: (s, p) => size('employeeLineHeight', (p - bb.employee) / 2)(s) },
    { id: 'gridHeadingHeight', label: 'Grid heading height', vertical: false, at: bb.subHeading, from: xs[3], to: xs[9], apply: (s, p) => size('gridHeadingHeight', p - bb.heading)(s) },
    { id: 'gridSubHeadingHeight', label: 'Grid sub-heading height', vertical: false, at: bb.detail, from: left.value, to: right.value, apply: (s, p) => size('gridSubHeadingHeight', p - bb.subHeading)(s) },
    { id: 'detailHeight', label: 'Detail area height', vertical: false, at: bb.totals, from: left.value, to: right.value, apply: (s, p) => size('detailHeight', p - bb.detail)(s) },
    { id: 'totalsRowHeight', label: 'Page totals row height', vertical: false, at: bb.grandTotal, from: left.value, to: right.value, apply: (s, p) => size('totalsRowHeight', p - bb.totals)(s) },
    { id: 'grandTotalRowHeight', label: 'GRAND TOTAL row height', vertical: false, at: bb.grandTotal + v.grandTotalRowHeight, from: xs[9], to: right.value, apply: (s, p) => size('grandTotalRowHeight', p - bb.grandTotal)(s) },
    { id: 'footerHeight', label: 'Footer height', vertical: false, at: bb.reference, from: left.value, to: right.value, apply: (s, p) => size('footerHeight', p - bb.footer)(s) },
    { id: 'logoWidth', label: 'Logo box width', vertical: true, at: titleX, from: bb.header, to: bb.dateStamp, apply: (s, p) => size('logoWidth', p - left.value)(s) },
    {
      id: 'documentCodeWidth',
      label: 'Document Code box width',
      vertical: true,
      at: codeX,
      from: bb.header,
      to: bb.dateStamp,
      apply: (s, p) => size('documentCodeWidth', pageX - p)(s),
    },
    {
      id: 'pageNoWidth',
      label: 'Document Code and Page No. boxes',
      vertical: true,
      at: pageX,
      from: bb.header,
      to: bb.dateStamp,
      apply: (s, p) => {
        // Moves the divider between the two boxes: one grows as the other shrinks.
        const total = s.documentCodeWidth + s.pageNoWidth;
        const code = clamp(round(p - codeX), RANGES.documentCodeWidth);
        const page = clamp(total - code, RANGES.pageNoWidth);
        return { ...s, documentCodeWidth: total - page, pageNoWidth: page };
      },
    },
    { id: 'cashAdvanceWidth', label: 'Cash advance box width', vertical: true, at: left.value + v.cashAdvanceWidth, from: bb.cashAdvance, to: bb.footer, apply: (s, p) => size('cashAdvanceWidth', p - left.value)(s) },
    { id: 'footerLeftWidth', label: 'Promise box width', vertical: true, at: left.value + v.footerLeftWidth, from: bb.footer, to: bb.reference, apply: (s, p) => size('footerLeftWidth', p - left.value)(s) },
  ];
  for (let i = 0; i < v.gridColumns.length - 1; i++) {
    list.push({
      id: `column${i}`,
      label: `Divider between ${GRID_COLUMN_NAMES[i]} and ${GRID_COLUMN_NAMES[i + 1]}`,
      vertical: true,
      at: xs[i + 1],
      from: bb.heading,
      to: bb.grandTotal,
      apply: (s, p) => ({ ...s, gridColumns: moveColumnDivider(s, i, p - columnEdges(s)[i + 1]) }),
    });
  }
  return list;
});

const svg = ref<SVGSVGElement | null>(null);
const dragging = ref<{ handle: Handle; start: R47Design } | null>(null);

function toPage(event: PointerEvent): { x: number; y: number } | null {
  const ctm = svg.value?.getScreenCTM();
  if (!ctm) return null;
  const point = new DOMPoint(event.clientX, event.clientY).matrixTransform(ctm.inverse());
  return { x: point.x, y: point.y };
}

function startDrag(handle: Handle, event: PointerEvent) {
  emit('checkpoint');
  dragging.value = { handle, start: d.value };
  (event.target as Element).setPointerCapture(event.pointerId);
}

function drag(event: PointerEvent) {
  const current = dragging.value;
  const p = current && toPage(event);
  if (!current || !p) return;
  // Handles are rebuilt from the start design so the drag follows the pointer, not the last frame.
  emit('update:modelValue', current.handle.apply(current.start, current.handle.vertical ? p.x : p.y));
}

function endDrag() {
  dragging.value = null;
}

function nudge(handle: Handle, event: KeyboardEvent) {
  const step = event.shiftKey ? 5 : 0.5;
  const delta = handle.vertical
    ? { ArrowLeft: -step, ArrowRight: step }[event.key]
    : { ArrowUp: -step, ArrowDown: step }[event.key];
  if (delta === undefined) return;
  event.preventDefault();
  emit('checkpoint');
  emit('update:modelValue', handle.apply(d.value, handle.at + delta));
}

const sectionBands = computed(() => {
  const bb = b.value;
  const v = d.value;
  const rows: { section: R47Section; y: number; h: number }[] = [
    { section: 'header', y: bb.header, h: v.headerHeight },
    { section: 'employee', y: bb.dateStamp, h: bb.heading - bb.dateStamp },
    { section: 'grid', y: bb.heading, h: bb.totals - bb.heading },
    { section: 'totals', y: bb.totals, h: bb.cashAdvance - bb.totals },
    { section: 'cashAdvance', y: bb.cashAdvance, h: bb.footer - bb.cashAdvance },
    { section: 'footer', y: bb.footer, h: v.footerHeight },
  ];
  return rows;
});

const sampleRows = computed(() => {
  const rows: number[] = [];
  for (let y = b.value.detail + d.value.singleRowHeight; y < b.value.totals - 0.1; y += d.value.singleRowHeight) rows.push(y);
  return rows;
});

const promise = computed(() => promiseLines(d.value));
const headerFont = 'Calibri, Carlito, sans-serif';
const bodyFont = '"Times New Roman", Times, serif';
const gridFont = 'Tahoma, Verdana, sans-serif';
</script>

<template>
  <svg
    ref="svg"
    class="r47-canvas"
    :viewBox="`-8 -8 ${PAGE_WIDTH + 16} ${PAGE_HEIGHT + 16}`"
    role="group"
    aria-label="R47 page layout"
    @pointermove="drag"
    @pointerup="endDrag"
    @pointercancel="endDrag"
  >
    <rect class="paper" x="0" y="0" :width="PAGE_WIDTH" :height="PAGE_HEIGHT" @click="emit('select', 'page')" />
    <rect
      class="margins"
      :class="{ selected: selected === 'page' }"
      :x="left"
      :y="d.marginTop"
      :width="width"
      :height="PAGE_HEIGHT - d.marginTop"
      pointer-events="none"
    />
    <rect v-if="b.end > PAGE_HEIGHT" class="overflow" x="0" :y="PAGE_HEIGHT" :width="PAGE_WIDTH" :height="b.end - PAGE_HEIGHT" pointer-events="none" />

    <!-- Section hit areas (behind the drawing). -->
    <rect
      v-for="band in sectionBands"
      :key="band.section"
      class="band"
      :class="{ selected: selected === band.section }"
      :x="left"
      :y="band.y"
      :width="width"
      :height="band.h"
      @click.stop="emit('select', band.section)"
    />

    <g class="ink" pointer-events="none">
      <!-- Header -->
      <rect :x="left" :y="b.header" :width="width" :height="d.headerHeight" />
      <line :x1="left + d.logoWidth" :y1="b.header + d.headerHeight / 2" :x2="right" :y2="b.header + d.headerHeight / 2" />
      <line :x1="left + d.logoWidth + titleWidth(d)" :y1="b.header" :x2="left + d.logoWidth + titleWidth(d)" :y2="b.dateStamp" />
      <line :x1="right - d.pageNoWidth" :y1="b.header" :x2="right - d.pageNoWidth" :y2="b.dateStamp" />
      <rect class="logo" :x="left + 6" :y="b.header + 6" :width="d.logoWidth - 12" :height="d.headerHeight - 12" />
      <text class="t-muted" :x="left + d.logoWidth / 2" :y="b.header + d.headerHeight / 2" text-anchor="middle" dominant-baseline="middle" font-size="9">Logo</text>
      <text :x="left + d.logoWidth + titleWidth(d) / 2" :y="b.header + d.headerHeight / 4" text-anchor="middle" dominant-baseline="middle" :font-size="d.companyNameSize" :font-family="headerFont" font-weight="700">Company name</text>
      <text :x="left + d.logoWidth + titleWidth(d) / 2" :y="b.header + (3 * d.headerHeight) / 4" text-anchor="middle" dominant-baseline="middle" :font-size="d.titleSize" :font-family="headerFont" font-weight="700">EXPENSE REPORT</text>
      <text :x="right - d.pageNoWidth - d.documentCodeWidth + 4" :y="b.header + 4 + d.infoLabelSize" :font-size="d.infoLabelSize" :font-family="headerFont" font-weight="700">Document Code:</text>
      <text :x="right - d.pageNoWidth + 4" :y="b.header + 4 + d.infoLabelSize" :font-size="d.infoLabelSize" :font-family="headerFont" font-weight="700">Page No.</text>
      <text :x="right - d.pageNoWidth - d.documentCodeWidth / 2" :y="b.header + 8 + d.infoLabelSize * 1.4 + d.infoValueSize" text-anchor="middle" :font-size="d.infoValueSize" :font-family="headerFont" font-weight="700">R47</text>
      <text :x="right - d.pageNoWidth / 2" :y="b.header + 8 + d.infoLabelSize * 1.4 + d.infoValueSize" text-anchor="middle" :font-size="d.infoValueSize" :font-family="headerFont" font-weight="700">1</text>

      <!-- DATE stamp and employee block -->
      <rect :x="dateX" :y="b.dateStamp" :width="dateBoxWidth(d)" :height="d.dateStampHeight" />
      <text :x="dateX + 4" :y="b.dateStamp + d.dateStampHeight / 2" dominant-baseline="middle" :font-size="d.bodySize" :font-family="bodyFont" font-weight="700">DATE</text>
      <rect :x="left" :y="b.employee" :width="width" :height="2 * d.employeeLineHeight" />
      <line :x1="dateX" :y1="b.employee" :x2="dateX" :y2="b.heading" />
      <line :x1="left" :y1="b.employee + d.employeeLineHeight" :x2="dateX" :y2="b.employee + d.employeeLineHeight" />
      <text :x="left + 5" :y="b.employee + d.employeeLineHeight / 2" dominant-baseline="middle" :font-size="d.bodySize" :font-family="bodyFont" font-weight="700">EMPLOYEE:</text>
      <text :x="left + 5" :y="b.employee + 1.5 * d.employeeLineHeight" dominant-baseline="middle" :font-size="d.bodySize" :font-family="bodyFont" font-weight="700">PURPOSE OF EXPENSE:</text>
      <line class="guide" :x1="left + 5 + d.employeeLabelWidth" :y1="b.employee + 2" :x2="left + 5 + d.employeeLabelWidth" :y2="b.heading - 2" />

      <!-- Grid headings, detail area and totals -->
      <rect :x="left" :y="b.heading" :width="width" :height="b.grandTotal - b.heading" />
      <line v-for="(cx, i) in x.slice(1, -1)" :key="`col${i}`" :x1="cx" :y1="i >= 3 && i < 8 ? b.subHeading : b.heading" :x2="cx" :y2="b.grandTotal" />
      <line :x1="x[3]" :y1="b.subHeading" :x2="x[9]" :y2="b.subHeading" />
      <line :x1="left" :y1="b.detail" :x2="right" :y2="b.detail" />
      <line :x1="left" :y1="b.totals" :x2="right" :y2="b.totals" />
      <line v-for="(ry, i) in sampleRows" :key="`row${i}`" class="guide" :x1="left" :y1="ry" :x2="right" :y2="ry" />
      <text :x="(x[3] + x[9]) / 2" :y="b.heading + d.gridHeadingHeight / 2" text-anchor="middle" dominant-baseline="middle" :font-size="d.bodySize" :font-family="bodyFont" :fill="d.labelColor">LOCAL TRAVEL</text>
      <template v-for="(name, i) in GRID_COLUMN_NAMES" :key="name">
        <text
          :x="(x[i] + x[i + 1]) / 2"
          :y="i >= 3 && i <= 8 ? b.subHeading + d.gridSubHeadingHeight / 2 : (b.heading + b.detail) / 2"
          text-anchor="middle"
          dominant-baseline="middle"
          :font-size="i >= 3 && i <= 10 ? d.smallSize : d.bodySize"
          :font-family="bodyFont"
          font-weight="700"
          :fill="i >= 3 && i <= 8 ? undefined : d.labelColor"
        >{{ name }}</text>
      </template>
      <text :x="x[1] + 6.5" :y="b.detail + d.singleRowHeight / 2" dominant-baseline="middle" :font-size="d.gridTextSize" :font-family="gridFont">Location Work w/: JCA</text>
      <text :x="x[1] + 8.5" :y="b.detail + 1.5 * d.singleRowHeight" dominant-baseline="middle" :font-size="d.gridTextSize" :font-family="gridFont">Particulars of an expense line</text>
      <text :x="x[13] - 1.5" :y="b.detail + 1.5 * d.singleRowHeight" text-anchor="end" dominant-baseline="middle" :font-size="d.amountSize" :font-family="gridFont">1,234.00</text>
      <text :x="x[13] - 1.5" :y="b.totals + d.totalsRowHeight / 2" text-anchor="end" dominant-baseline="middle" :font-size="d.amountSize" :font-family="gridFont" font-weight="700">1,234.00</text>

      <!-- Revolving fund and GRAND TOTAL row -->
      <text :x="left + 6" :y="b.grandTotal + d.grandTotalRowHeight / 2" dominant-baseline="middle" :font-size="d.smallSize" :font-family="bodyFont" :fill="d.noteColor">{{ d.revolvingFundText }}</text>
      <rect :x="x[3]" :y="b.grandTotal" :width="d.gridColumns[3]" :height="d.grandTotalRowHeight" />
      <rect :x="x[9]" :y="b.grandTotal" :width="right - x[9]" :height="d.grandTotalRowHeight" />
      <line :x1="x[12]" :y1="b.grandTotal" :x2="x[12]" :y2="b.grandTotal + d.grandTotalRowHeight" />
      <text :x="x[9] + 4" :y="b.grandTotal + d.grandTotalRowHeight / 2" dominant-baseline="middle" :font-size="d.bodySize" :font-family="bodyFont" font-weight="700">GRAND TOTAL</text>

      <!-- Cash advance and reconciliation -->
      <rect :x="left" :y="b.cashAdvance" :width="d.cashAdvanceWidth" :height="b.footer - b.cashAdvance" />
      <line :x1="left" :y1="b.cashAdvance + 12" :x2="left + d.cashAdvanceWidth" :y2="b.cashAdvance + 12" />
      <line :x1="left" :y1="b.cashAdvance + 24.5" :x2="left + d.cashAdvanceWidth" :y2="b.cashAdvance + 24.5" />
      <text :x="left + d.cashAdvanceWidth / 2" :y="b.cashAdvance + 6" text-anchor="middle" dominant-baseline="middle" :font-size="d.bodySize + 0.8" :font-family="bodyFont" font-weight="700" :fill="d.labelColor">CASH ADVANCE RECEIVED</text>
      <text :x="left + d.cashAdvanceWidth + 4" :y="b.cashAdvance + d.reconLineHeight / 2" dominant-baseline="middle" :font-size="d.bodySize" :font-family="bodyFont" font-weight="700" :fill="d.labelColor">CASH ADVANCE, if any</text>
      <text :x="left + d.cashAdvanceWidth + 4" :y="b.cashAdvance + 1.5 * d.reconLineHeight" dominant-baseline="middle" :font-size="d.bodySize" :font-family="bodyFont" font-weight="700" :fill="d.labelColor">LESS EXPENSE (THIS EXPENSE REPORT)</text>
      <text :x="left + d.cashAdvanceWidth + 4" :y="b.cashAdvance + 2.5 * d.reconLineHeight" dominant-baseline="middle" :font-size="d.bodySize" :font-family="bodyFont" font-weight="700">BAL DUE TO:</text>
      <rect v-for="k in 3" :key="`rec${k}`" :x="x[12]" :y="b.cashAdvance + (k - 1) * d.reconLineHeight" :width="d.gridColumns[12]" :height="d.reconLineHeight" />

      <!-- Footer -->
      <rect :x="left" :y="b.footer" :width="width" :height="d.footerHeight" />
      <line :x1="left + d.footerLeftWidth" :y1="b.footer" :x2="left + d.footerLeftWidth" :y2="b.reference" />
      <text
        v-for="(line, i) in promise"
        :key="`p${i}`"
        :x="d.promiseAlign === 'Center' ? left + d.footerLeftWidth / 2 : left + 6"
        :text-anchor="d.promiseAlign === 'Center' ? 'middle' : 'start'"
        :y="b.footer + 5 + (i + 0.5) * d.bodySize * 1.45"
        dominant-baseline="middle"
        :font-size="d.bodySize"
        :font-family="bodyFont"
        :fill="d.noteColor"
      >{{ line }}</text>
      <line
        :x1="left + (d.footerLeftWidth - 160) / 2"
        :y1="b.footer + 25 + promise.length * d.bodySize * 1.45"
        :x2="left + (d.footerLeftWidth + 160) / 2"
        :y2="b.footer + 25 + promise.length * d.bodySize * 1.45"
      />
      <text :x="left + d.footerLeftWidth / 2" :y="b.footer + 26 + promise.length * d.bodySize * 1.45 + d.bodySize" text-anchor="middle" :font-size="d.bodySize + 0.8" :font-family="bodyFont" font-weight="700">EMPLOYEE'S SIGNATURE</text>
      <text :x="left + d.footerLeftWidth + 20" :y="b.footer + 7.5" dominant-baseline="middle" :font-size="d.bodySize" :font-family="bodyFont" :fill="d.labelColor">(Signature Over Printed Name)</text>
      <text :x="left + d.footerLeftWidth + 20" :y="b.footer + 39" :font-size="d.bodySize" :font-family="bodyFont" font-weight="700">ENDORSED BY :</text>
      <line :x1="left + d.footerLeftWidth" :y1="b.footer + 42" :x2="right" :y2="b.footer + 42" />
      <text :x="left + d.footerLeftWidth + 20" :y="b.footer + 51" dominant-baseline="middle" :font-size="d.bodySize" :font-family="bodyFont" font-weight="700">REVIEWED BY :</text>
      <line :x1="left + d.footerLeftWidth" :y1="b.footer + 60" :x2="right" :y2="b.footer + 60" />
      <text :x="left + d.footerLeftWidth + 20" :y="b.footer + 87" :font-size="d.bodySize" :font-family="bodyFont" font-weight="700">APPROVED BY :</text>

      <text :x="left" :y="b.reference + 3 + d.smallSize" :font-size="d.smallSize" :font-family="bodyFont">ER Reference Number: <tspan :fill="d.labelColor">ERF-0000-000000</tspan></text>
    </g>

    <!-- Handles last, so they sit on top. -->
    <g v-for="h in handles" :key="h.id" class="handle" :class="{ active: dragging?.handle.id === h.id }">
      <title>{{ h.label }}</title>
      <line v-if="h.vertical" class="handle-line" :x1="h.at" :y1="h.from" :x2="h.at" :y2="h.to" />
      <line v-else class="handle-line" :x1="h.from" :y1="h.at" :x2="h.to" :y2="h.at" />
      <rect
        class="handle-hit"
        :class="h.vertical ? 'ew' : 'ns'"
        :x="h.vertical ? h.at - 3 : h.from"
        :y="h.vertical ? h.from : h.at - 3"
        :width="h.vertical ? 6 : h.to - h.from"
        :height="h.vertical ? h.to - h.from : 6"
        tabindex="0"
        role="slider"
        :aria-label="h.label"
        :aria-orientation="h.vertical ? 'horizontal' : 'vertical'"
        :aria-valuenow="Math.round(h.at * 10) / 10"
        @pointerdown.prevent="startDrag(h, $event)"
        @keydown="nudge(h, $event)"
      />
    </g>
  </svg>
</template>

<style scoped>
.r47-canvas {
  display: block;
  width: 100%;
  height: auto;
  touch-action: none;
  user-select: none;
}
.paper {
  fill: #fff;
  stroke: rgba(0, 0, 0, 0.18);
  stroke-width: 1;
}
.margins {
  fill: none;
  stroke: rgba(0, 0, 0, 0.18);
  stroke-dasharray: 3 3;
  stroke-width: 0.6;
}
.margins.selected {
  stroke: rgb(var(--v-theme-primary));
  stroke-width: 1.2;
}
.overflow {
  fill: rgba(var(--v-theme-error), 0.25);
}
.band {
  fill: transparent;
  cursor: pointer;
}
.band:hover {
  fill: rgba(var(--v-theme-primary), 0.05);
}
.band.selected {
  fill: rgba(var(--v-theme-primary), 0.1);
}
.ink {
  fill: #000;
  stroke: none;
}
.ink rect,
.ink line {
  fill: none;
  stroke: #000;
  stroke-width: 0.6;
}
.ink .logo {
  fill: rgba(0, 0, 0, 0.05);
  stroke: rgba(0, 0, 0, 0.2);
}
.ink .guide {
  stroke: rgba(0, 0, 0, 0.15);
  stroke-dasharray: 2 2;
}
.ink .t-muted {
  fill: rgba(0, 0, 0, 0.45);
}
.handle-line {
  stroke: rgb(var(--v-theme-primary));
  stroke-width: 1.4;
  opacity: 0;
  transition: opacity 120ms ease;
}
.handle:hover .handle-line,
.handle.active .handle-line,
.handle:focus-within .handle-line {
  opacity: 1;
}
.handle-hit {
  fill: transparent;
  outline: none;
}
.handle-hit.ns {
  cursor: ns-resize;
}
.handle-hit.ew {
  cursor: ew-resize;
}
@media (prefers-reduced-motion: reduce) {
  .handle-line {
    transition: none;
  }
}
</style>
