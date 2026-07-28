import { defineStore } from 'pinia'
import { getMaps, mapImageUrl } from '@/services/mapService'
import { extractErrorMessage } from '@/services/apiClient'
import { useToastStore } from './toastStore'
import { usePlanStore } from './planStore'

/** MapDirectory에 있는 맵 목록과 현재 선택된 맵을 관리한다. */
export const useMapStore = defineStore('map', {
  state: () => ({
    availableMaps: [] as string[],
    currentFileName: null as string | null,
    isLoadingList: false,
  }),
  getters: {
    currentMapImageUrl(state): string | null {
      return state.currentFileName ? mapImageUrl(state.currentFileName) : null
    },
  },
  actions: {
    async refreshMapList() {
      this.isLoadingList = true
      try {
        this.availableMaps = await getMaps()
      } catch (err) {
        useToastStore().error(`맵 목록 조회 실패: ${extractErrorMessage(err)}`)
      } finally {
        this.isLoadingList = false
      }
    },

    // 맵이 바뀌면 이전 탐색 결과(FE에 그려진 경로/Footprint 애니메이션)는 더 이상 유효하지 않으므로 초기화한다.
    selectMap(fileName: string) {
      this.currentFileName = fileName
      usePlanStore().reset()
    },
  },
})
