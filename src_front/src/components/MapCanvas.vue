<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useMapStore } from '@/stores/mapStore'
import { usePlanStore } from '@/stores/planStore'
import { useConfigStore } from '@/stores/configStore'
import type { PathNode } from '@/models/PathNode'

const CANVAS_WIDTH = 1024
const CANVAS_HEIGHT = 768
const FORWARD_COLOR = '#ff8000'
const REVERSE_COLOR = '#e00000'
const FOOTPRINT_COLOR = '#00c853'
const HEADING_COLOR = '#ffcc00'
const ARROW_LENGTH = 16
const ARROWHEAD_LENGTH = 8
const ARROWHEAD_WIDTH = 8

const mapStore = useMapStore()
const planStore = usePlanStore()
const configStore = useConfigStore()

const canvasRef = ref<HTMLCanvasElement | null>(null)
let ctx: CanvasRenderingContext2D | null = null
let mapImage: HTMLImageElement | null = null
let animationFrameId: number | null = null
let lastFrameTime: number | null = null
let progressIndex = 0

onMounted(() => {
  ctx = canvasRef.value?.getContext('2d') ?? null
  drawFrame()
})

onBeforeUnmount(() => {
  stopAnimation()
})

// 맵이 바뀌면 진행 중인 애니메이션을 멈추고 새 배경 이미지를 로드한다.
watch(
  () => mapStore.currentMapImageUrl,
  (url) => {
    stopAnimation()
    progressIndex = 0
    if (!url) {
      mapImage = null
      drawFrame()
      return
    }
    const img = new Image()
    img.onload = () => {
      mapImage = img
      drawFrame()
    }
    img.src = url
  },
  { immediate: true },
)

// 탐색이 시작되면 이전 경로/Footprint를 지우고, 성공하면 애니메이션을 재생한다.
watch(
  () => planStore.status,
  (status) => {
    if (status === 'running') {
      stopAnimation()
      progressIndex = 0
      drawFrame()
    } else if (status === 'succeeded' && planStore.result && planStore.result.path.length > 0) {
      progressIndex = 0
      startAnimation()
    } else {
      stopAnimation()
      drawFrame()
    }
  },
)

function stopAnimation() {
  if (animationFrameId !== null) {
    cancelAnimationFrame(animationFrameId)
    animationFrameId = null
  }
  lastFrameTime = null
}

function startAnimation() {
  stopAnimation()
  animationFrameId = requestAnimationFrame(tick)
}

function tick(timestamp: number) {
  const path = planStore.result?.path ?? []
  if (path.length === 0) {
    return
  }

  if (lastFrameTime !== null) {
    const deltaSeconds = (timestamp - lastFrameTime) / 1000
    progressIndex = Math.min(path.length - 1, progressIndex + deltaSeconds * planStore.animationSpeed)
  }
  lastFrameTime = timestamp

  drawFrame()

  if (progressIndex < path.length - 1) {
    animationFrameId = requestAnimationFrame(tick)
  } else {
    animationFrameId = null
  }
}

function drawFrame() {
  if (!ctx) return
  ctx.clearRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT)

  if (mapImage) {
    ctx.drawImage(mapImage, 0, 0, CANVAS_WIDTH, CANVAS_HEIGHT)
  } else {
    ctx.fillStyle = '#eef1f6'
    ctx.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT)
    ctx.fillStyle = '#999'
    ctx.font = '16px sans-serif'
    ctx.fillText('맵을 선택하세요', 24, 32)
  }

  const path = planStore.result?.path
  if (path && path.length > 0 && planStore.status !== 'running') {
    drawPathLine(path)
    drawFootprint(path[Math.floor(progressIndex)])
  }
}

function drawPathLine(path: PathNode[]) {
  if (!ctx) return
  ctx.lineWidth = 2
  for (let i = 1; i < path.length; i++) {
    const prev = path[i - 1]
    const curr = path[i]
    ctx.strokeStyle = curr.reverse ? REVERSE_COLOR : FORWARD_COLOR
    ctx.beginPath()
    ctx.moveTo(prev.x, prev.y)
    ctx.lineTo(curr.x, curr.y)
    ctx.stroke()
  }
}

function drawFootprint(node: PathNode) {
  if (!ctx) return
  const robot = configStore.config?.robot
  const length = robot?.footprintLength ?? 30
  const width = robot?.footprintWidth ?? 20

  ctx.save()
  ctx.translate(node.x, node.y)
  ctx.rotate(node.theta)

  ctx.strokeStyle = FOOTPRINT_COLOR
  ctx.lineWidth = 2
  ctx.strokeRect(-length / 2, -width / 2, length, width)

  drawHeadingArrow(length)

  ctx.restore()
}

// 로컬 좌표계(footprint 중심이 원점, +x가 heading 방향)에 화살표(직선 + 삼각형 화살촉)를 그린다.
function drawHeadingArrow(footprintLength: number) {
  if (!ctx) return
  const shaftLength = footprintLength / 2 + ARROW_LENGTH
  const shaftEnd = shaftLength - ARROWHEAD_LENGTH

  ctx.strokeStyle = HEADING_COLOR
  ctx.fillStyle = HEADING_COLOR
  ctx.lineWidth = 2

  ctx.beginPath()
  ctx.moveTo(0, 0)
  ctx.lineTo(shaftEnd, 0)
  ctx.stroke()

  ctx.beginPath()
  ctx.moveTo(shaftLength, 0)
  ctx.lineTo(shaftEnd, ARROWHEAD_WIDTH / 2)
  ctx.lineTo(shaftEnd, -ARROWHEAD_WIDTH / 2)
  ctx.closePath()
  ctx.fill()
}
</script>

<template>
  <div class="canvas-wrapper">
    <canvas ref="canvasRef" :width="CANVAS_WIDTH" :height="CANVAS_HEIGHT" />
  </div>
</template>

<style scoped>
.canvas-wrapper {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}

canvas {
  width: auto;
  height: auto;
  max-width: 100%;
  max-height: 100%;
  aspect-ratio: 1024 / 768;
  display: block;
  border: 1px solid #ccc;
  background: #eef1f6;
}
</style>
