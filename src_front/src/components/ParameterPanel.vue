<script setup lang="ts">
import { onMounted, reactive, watch } from 'vue'
import { useConfigStore } from '@/stores/configStore'
import type { PlannerConfig } from '@/models/PlannerConfig'

const configStore = useConfigStore()

const form = reactive<PlannerConfig>({
  robot: { footprintLength: 0, footprintWidth: 0, turningRadius: 0, maxSteeringAngleDeg: 0 },
  search: {
    stepSize: 0,
    gridResolution: 0,
    headingResolutionDeg: 0,
    steeringAngleSamples: 0,
    reverseEnabled: true,
    reversePenalty: 0,
    directionChangePenalty: 0,
    steeringChangePenalty: 0,
    analyticExpansionInterval: 0,
    goalToleranceXY: 0,
    goalToleranceThetaDeg: 0,
    maxSearchNodes: 0,
    maxSearchSeconds: 0,
  },
  map: { width: 1024, height: 768 },
})

onMounted(async () => {
  await configStore.load()
})

watch(
  () => configStore.config,
  (config) => {
    if (!config) return
    Object.assign(form.robot, config.robot)
    Object.assign(form.search, config.search)
    Object.assign(form.map, config.map)
  },
  { immediate: true },
)

function onFieldChange() {
  configStore.save(JSON.parse(JSON.stringify(form)))
}
</script>

<template>
  <div class="parameter-panel">
    <v-progress-linear v-if="configStore.isLoading" indeterminate color="primary" />

    <!-- 라벨은 백엔드 Parameter/Parameters.cs의 XML 주석을 기반으로, 괄호 안에 원래 필드명/단위를 함께 표기한다. -->
    <v-expansion-panels v-else variant="accordion" multiple :model-value="['robot', 'search']">
      <v-expansion-panel value="robot" title="로봇 차체 설정">
        <v-expansion-panel-text>
          <v-row dense>
            <v-col cols="12">
              <v-text-field
                v-model.number="form.robot.footprintLength"
                label="차체 길이 (px)"
                type="number"
                density="compact"
                hide-details
                @blur="onFieldChange"
              />
            </v-col>
            <v-col cols="12">
              <v-text-field
                v-model.number="form.robot.footprintWidth"
                label="차체 폭 (px)"
                type="number"
                density="compact"
                hide-details
                @blur="onFieldChange"
              />
            </v-col>
            <v-col cols="12">
              <v-text-field
                v-model.number="form.robot.turningRadius"
                label="최소 회전 반경 (px)"
                type="number"
                density="compact"
                hide-details
                @blur="onFieldChange"
              />
            </v-col>
            <v-col cols="12">
              <v-text-field
                v-model.number="form.robot.maxSteeringAngleDeg"
                label="최대 조향각 (°)"
                type="number"
                density="compact"
                hide-details
                @blur="onFieldChange"
              />
            </v-col>
          </v-row>
        </v-expansion-panel-text>
      </v-expansion-panel>

      <v-expansion-panel value="search" title="탐색 알고리즘 설정">
        <v-expansion-panel-text>
          <v-row dense>
            <v-col cols="12">
              <v-text-field
                v-model.number="form.search.stepSize"
                label="한 걸음 이동 거리 (px)"
                type="number"
                density="compact"
                hide-details
                @blur="onFieldChange"
              />
            </v-col>
            <v-col cols="12">
              <v-text-field
                v-model.number="form.search.gridResolution"
                label="격자 칸 크기 (px)"
                type="number"
                density="compact"
                hide-details
                @blur="onFieldChange"
              />
            </v-col>
            <v-col cols="12">
              <v-text-field
                v-model.number="form.search.headingResolutionDeg"
                label="방향 각도 간격 (°)"
                type="number"
                density="compact"
                hide-details
                @blur="onFieldChange"
              />
            </v-col>
            <v-col cols="12">
              <v-text-field
                v-model.number="form.search.steeringAngleSamples"
                label="조향각 후보 개수"
                type="number"
                density="compact"
                hide-details
                @blur="onFieldChange"
              />
            </v-col>
            <v-col cols="12">
              <v-switch
                v-model="form.search.reverseEnabled"
                label="후진 이동 허용"
                density="compact"
                hide-details
                @update:model-value="onFieldChange"
              />
            </v-col>
            <v-col cols="12">
              <v-text-field
                v-model.number="form.search.reversePenalty"
                label="후진 이동 페널티 배율"
                type="number"
                density="compact"
                hide-details
                @blur="onFieldChange"
              />
            </v-col>
            <v-col cols="12">
              <v-text-field
                v-model.number="form.search.directionChangePenalty"
                label="전후진 전환 페널티 (DirectionChangePenalty, px)"
                type="number"
                density="compact"
                hide-details
                @blur="onFieldChange"
              />
            </v-col>
            <v-col cols="12">
              <v-text-field
                v-model.number="form.search.steeringChangePenalty"
                label="조향각 변화 페널티 (SteeringChangePenalty, px/rad) — 직진 중 좌우 헤딩 진동 억제"
                type="number"
                density="compact"
                hide-details
                @blur="onFieldChange"
              />
            </v-col>
            <v-col cols="12">
              <v-text-field
                v-model.number="form.search.analyticExpansionInterval"
                label="목표까지 직결 시도 간격"
                type="number"
                density="compact"
                hide-details
                @blur="onFieldChange"
              />
            </v-col>
            <v-col cols="12">
              <v-text-field
                v-model.number="form.search.goalToleranceXY"
                label="목표 도착 허용 오차 (px)"
                type="number"
                density="compact"
                hide-details
                @blur="onFieldChange"
              />
            </v-col>
            <v-col cols="12">
              <v-text-field
                v-model.number="form.search.goalToleranceThetaDeg"
                label="목표 방향 허용 오차 (°)"
                type="number"
                density="compact"
                hide-details
                @blur="onFieldChange"
              />
            </v-col>
            <v-col cols="12">
              <v-text-field
                v-model.number="form.search.maxSearchNodes"
                label="최대 탐색 시도 횟수"
                type="number"
                density="compact"
                hide-details
                @blur="onFieldChange"
              />
            </v-col>
            <v-col cols="12">
              <v-text-field
                v-model.number="form.search.maxSearchSeconds"
                label="최대 탐색 시간 (초)"
                type="number"
                density="compact"
                hide-details
                @blur="onFieldChange"
              />
            </v-col>
          </v-row>
        </v-expansion-panel-text>
      </v-expansion-panel>
    </v-expansion-panels>
  </div>
</template>

<style scoped>
.parameter-panel {
  padding: 4px;
}

:deep(.v-expansion-panel-text__wrapper) {
  padding: 8px 12px;
}

:deep(.v-row) {
  margin: 0 -4px;
}

:deep(.v-col) {
  padding: 4px;
}
</style>
