/** 프론트엔드에서 관리하는 탐색 진행 상태(백엔드에는 별도 상태 API가 없음 — 요청의 pending/성공/실패/중단으로 파생). */
export type PlanStatus = 'idle' | 'running' | 'succeeded' | 'failed' | 'cancelled'
