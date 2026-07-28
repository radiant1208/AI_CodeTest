<script setup lang="ts">
import { ref } from 'vue'
import MapSelectPanel from '@/components/MapSelectPanel.vue'
import ControlPanel from '@/components/ControlPanel.vue'
import ParameterPanel from '@/components/ParameterPanel.vue'
import MapCanvas from '@/components/MapCanvas.vue'
import LoadingModal from '@/components/LoadingModal.vue'
import ToastContainer from '@/components/ToastContainer.vue'
import { usePlanStore } from '@/stores/planStore'

const planStore = usePlanStore()
const drawerOpen = ref(false)
</script>

<template>
  <v-app>
    <v-app-bar density="compact" class="px-3" flat border style="background-color: #212121;">
      <v-app-bar-title class="text-no-wrap me-4" style="flex: 0 0 auto">
        <span class="text-subtitle-1 font-weight-bold">Hybrid A* PathSearch</span>
      </v-app-bar-title>

      <div class="d-flex align-center ga-2 flex-grow-1 overflow-hidden">
        <MapSelectPanel />
        <ControlPanel />
      </div>
      <v-spacer />
      <v-btn
        icon="mdi-cog"
        variant="text"
        density="compact"
        title="파라미터 패널 토글"
        @click="drawerOpen = !drawerOpen"
      />
    </v-app-bar>

    <v-navigation-drawer v-model="drawerOpen" location="right" width="300" temporary>
      <v-card flat>
        <ParameterPanel />
      </v-card>
    </v-navigation-drawer>

    <v-main>
      <v-container fluid class="d-flex align-center justify-center canvas-area pa-2">
        <v-sheet rounded="lg" class="canvas-sheet pa-1">
          <MapCanvas />
        </v-sheet>
      </v-container>
    </v-main>

    <LoadingModal :visible="planStore.status === 'running'" />
    <ToastContainer />
  </v-app>
</template>

<style scoped>
/* v-main의 실제 높이는 flex-basis:auto + flex-shrink:0이라 percentage 체인만으로는 정의되지 않는다.
   Vuetify가 v-main의 padding에 실제로 사용하는 --v-layout-top/-bottom 변수를 그대로 재사용해
   뷰포트 기준으로 명시적인 높이를 계산하면, 그 아래(v-sheet/canvas-wrapper/canvas)의
   height:100%/max-height:100% 체인이 비로소 유효한 기준을 갖게 되어 캔버스가 잘리지 않는다. */
.canvas-area {
  height: calc(100vh - var(--v-layout-top, 0px) - var(--v-layout-bottom, 0px));
  height: calc(100dvh - var(--v-layout-top, 0px) - var(--v-layout-bottom, 0px));
  background-color: #f2f2f2;
}

.canvas-sheet {
  width: 100%;
  height: 100%;
  max-width: 1060px;
  display: flex;
  background-color: #f2f2f2;
}
</style>
