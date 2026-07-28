import { apiClient } from './apiClient'
import type { PlannerConfig } from '@/models/PlannerConfig'

/** 현재 로드된 parameter.json 값을 조회한다. */
export async function fetchConfig(): Promise<PlannerConfig> {
  const response = await apiClient.get<PlannerConfig>('/config')
  return response.data
}

/** 파라미터를 수정해 즉시 적용(다음 탐색부터)하고 data/parameter.json에 저장한다. */
export async function updateConfig(config: PlannerConfig): Promise<PlannerConfig> {
  const response = await apiClient.put<PlannerConfig>('/config', config)
  return response.data
}
