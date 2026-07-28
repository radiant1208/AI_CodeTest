<script setup lang="ts">
import { computed } from 'vue'
import { useMapStore } from '@/stores/mapStore'
import { usePlanStore } from '@/stores/planStore'
import { resultImageUrl } from '@/services/planService'

const mapStore = useMapStore()
const planStore = usePlanStore()

const isRunning = computed(() => planStore.status === 'running')
const canStart = computed(() => Boolean(mapStore.currentFileName) && !isRunning.value)
const canDownload = computed(() => planStore.status === 'succeeded' && Boolean(planStore.result?.resultImageUrl))

const statusColor = computed(() => {
  switch (planStore.status) {
    case 'running':
      return 'info'
    case 'succeeded':
      return 'success'
    case 'failed':
      return 'error'
    case 'cancelled':
      return 'warning'
    default:
      return 'grey'
  }
})

function onStart() {
  if (!mapStore.currentFileName) return
  planStore.start(mapStore.currentFileName)
}

function onStop() {
  planStore.stop()
}

function onSpeedInput(value: number) {
  planStore.setAnimationSpeed(value)
}

function onDownload() {
  if (!mapStore.currentFileName) return
  const link = document.createElement('a')
  link.href = resultImageUrl(mapStore.currentFileName)
  link.download = `result_${mapStore.currentFileName}`
  link.click()
}
</script>

<template>
  <div class="control-panel">
    <!-- 시작 버튼 -->
      <v-btn
        color="primary"
        density="compact"
        variant="flat"
        class="text-no-wrap font-weight-bold px-3"
        elevation="1"
        prepend-icon="mdi-play"
        :loading="isRunning"
        :disabled="!canStart || isRunning"
        @click="onStart"
      >
        경로 탐색
      </v-btn>

      <!-- 중지 버튼 -->
      <v-btn
        color="error"
        density="compact"
        variant="tonal"
        class="text-no-wrap font-weight-bold px-3"
        prepend-icon="mdi-stop"
        :disabled="!isRunning"
        @click="onStop"
      >
        탐색 중지
      </v-btn>

    <div class="d-flex align-center ga-2 speed-slider-container">
      <span class="text-caption font-weight-medium text-grey-darken-1 text-no-wrap">속도</span>

      <v-slider
        class="speed-slider flex-grow-1"
        density="compact"
        hide-details
        :model-value="planStore.animationSpeed"
        :min="1"
        :max="60"
        :step="1"
        thumb-size="12"
        track-size="3"
        color="primary"
        track-color="grey-lighten-2"
        @update:model-value="onSpeedInput"
      />

      <span class="text-caption font-weight-bold text-primary speed-value">
        {{ planStore.animationSpeed }}
      </span>
    </div>

    <v-btn
      icon="mdi-download"
      variant="outlined"
      density="compact"
      :disabled="!canDownload"
      title="결과 이미지 다운로드"
      @click="onDownload"
    />

    <v-chip
      :color="statusColor"
      size="small"
      variant="tonal"
      class="status-chip text-no-wrap font-weight-semibold px-2.5"
    >
      <template #prepend>
        <span class="status-dot mr-1.5" :class="`bg-${statusColor}`" />
      </template>
      
      {{ planStore.status }}
    </v-chip>
  </div>
</template>

<style scoped>
.control-panel {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: nowrap;
}

.speed-slider {
  min-width: 160px;
  max-width: 200px;
}
</style>
