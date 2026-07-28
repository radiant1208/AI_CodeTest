<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useMapStore } from '@/stores/mapStore'

const mapStore = useMapStore()

onMounted(() => {
  mapStore.refreshMapList()
})

const selected = computed({
  get: () => mapStore.currentFileName,
  set: (value: string | null) => {
    if (value) mapStore.selectMap(value)
  },
})
</script>

<template>
  <v-select
    v-model="selected"
    :items="mapStore.availableMaps"
    :loading="mapStore.isLoadingList"
    label="맵 선택"
    density="compact"
    variant="filled"
    hide-details
    class="map-select text-no-wrap"
  />
</template>

<style scoped>
.map-select {
  min-width: 200px;
  max-width: 260px;
}
</style>
