/** 로봇 차체 및 운동학 파라미터. 길이 단위는 px. */
export interface RobotConfig {
  footprintLength: number
  footprintWidth: number
  turningRadius: number
  maxSteeringAngleDeg: number
}

/** 하이브리드 A* 탐색 파라미터. */
export interface SearchConfig {
  stepSize: number
  gridResolution: number
  headingResolutionDeg: number
  steeringAngleSamples: number
  reverseEnabled: boolean
  reversePenalty: number
  directionChangePenalty: number
  analyticExpansionInterval: number
  goalToleranceXY: number
  goalToleranceThetaDeg: number
  maxSearchNodes: number
  maxSearchSeconds: number
}

/** 맵 이미지 관련 파라미터. */
export interface MapConfig {
  width: number
  height: number
}

/** data/parameter.json과 1:1 대응하는 전체 설정. GET/PUT /api/config로 조회/수정한다. */
export interface PlannerConfig {
  robot: RobotConfig
  search: SearchConfig
  map: MapConfig
}
