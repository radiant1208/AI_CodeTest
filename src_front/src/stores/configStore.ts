import { defineStore } from 'pinia'
import { fetchConfig, updateConfig } from '@/services/configService'
import type { PlannerConfig } from '@/models/PlannerConfig'
import { extractErrorMessage } from '@/services/apiClient'
import { useToastStore } from './toastStore'

/** parameter.json 값(Robot/Search/Map)을 조회/실시간 수정한다. */
export const useConfigStore = defineStore('config', {
  state: () => ({
    config: null as PlannerConfig | null,
    isLoading: false,
    isSaving: false,
  }),
  actions: {
    async load() {
      this.isLoading = true
      try {
        this.config = await fetchConfig()
      } catch (err) {
        useToastStore().error(`설정 조회 실패: ${extractErrorMessage(err)}`)
      } finally {
        this.isLoading = false
      }
    },

    async save(updated: PlannerConfig) {
      const toast = useToastStore()
      this.isSaving = true
      try {
        this.config = await updateConfig(updated)
        toast.success('파라미터 수정 완료')
      } catch (err) {
        toast.error(`파라미터 수정 실패: ${extractErrorMessage(err)}`)
        throw err
      } finally {
        this.isSaving = false
      }
    },
  },
})
