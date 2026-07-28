import axios from 'axios'
import { defineStore } from 'pinia'
import { startPlan, stopPlan } from '@/services/planService'
import type { PlanResult } from '@/models/PlanResult'
import type { PlanStatus } from '@/models/PlanStatus'
import { extractErrorMessage } from '@/services/apiClient'
import { useToastStore } from './toastStore'

const SPEED_STORAGE_KEY = 'pathsearch:animationSpeed'
const DEFAULT_SPEED = 20
const MIN_SPEED = 1
const MAX_SPEED = 60

function loadStoredSpeed(): number {
  const raw = sessionStorage.getItem(SPEED_STORAGE_KEY)
  const parsed = raw === null ? NaN : Number(raw)
  if (Number.isFinite(parsed) && parsed >= MIN_SPEED && parsed <= MAX_SPEED) {
    return parsed
  }
  return DEFAULT_SPEED
}

/** 탐색 진행 상태, 결과, 취소용 AbortController, 세션 유지되는 애니메이션 속도를 관리한다. */
export const usePlanStore = defineStore('plan', {
  state: () => ({
    status: 'idle' as PlanStatus,
    result: null as PlanResult | null,
    abortController: null as AbortController | null,
    animationSpeed: loadStoredSpeed(),
  }),
  actions: {
    setAnimationSpeed(speed: number) {
      const clamped = Math.min(MAX_SPEED, Math.max(MIN_SPEED, speed))
      this.animationSpeed = clamped
      sessionStorage.setItem(SPEED_STORAGE_KEY, String(clamped))
    },

    async start(fileName: string) {
      const toast = useToastStore()
      this.status = 'running'
      this.result = null
      this.abortController = new AbortController()
      toast.info(`탐색 시작: ${fileName}`)

      try {
        const result = await startPlan(fileName, this.abortController.signal)
        this.result = result
        this.status = result.success ? 'succeeded' : 'failed'
        if (result.success) {
          toast.success(`탐색 성공: 경로 ${result.path.length}점, 비용 ${result.totalCost.toFixed(1)}px`)
        } else {
          toast.error(`탐색 실패: ${result.failureReason}`)
        }
      } catch (err) {
        if (axios.isCancel(err) || this.abortController.signal.aborted) {
          this.status = 'cancelled'
          toast.info('탐색이 취소되었습니다.')
        } else {
          this.status = 'failed'
          toast.error(`탐색 중 오류 발생: ${extractErrorMessage(err)}`)
        }
      } finally {
        this.abortController = null
      }
    },

    stop() {
      if (this.abortController) {
        stopPlan(this.abortController)
      }
    },

    reset() {
      this.status = 'idle'
      this.result = null
      this.abortController = null
    },
  },
})
