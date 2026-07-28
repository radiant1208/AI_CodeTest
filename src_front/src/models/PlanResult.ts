import type { PathNode } from './PathNode'

/** POST /api/plan/{fileName} 응답. */
export interface PlanResult {
  success: boolean
  failureReason: string
  elapsedSeconds: number
  expandedNodeCount: number
  totalCost: number
  analyticExpansionUsed: boolean
  path: PathNode[]
  resultImageUrl: string | null
}
