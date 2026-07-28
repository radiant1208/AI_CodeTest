import { apiClient } from './apiClient'
import type { PlanResult } from '@/models/PlanResult'

/** fileName에 대해 Hybrid A* 탐색을 시작한다. signal을 abort하면 stopPlan과 동일하게 백엔드 요청이 취소된다. */
export async function startPlan(fileName: string, signal: AbortSignal): Promise<PlanResult> {
  const response = await apiClient.post<PlanResult>(`/plan/${encodeURIComponent(fileName)}`, null, {
    signal,
  })
  return response.data
}

/** 진행 중인 탐색 요청을 취소한다(연결 종료 → 백엔드의 CancellationToken이 취소됨). */
export function stopPlan(controller: AbortController): void {
  controller.abort()
}

/** 탐색 성공 후 렌더링된 결과 이미지(원본 위 최단 경로 오버레이) URL. */
export function resultImageUrl(fileName: string): string {
  return `/api/results/${encodeURIComponent(fileName)}`
}
