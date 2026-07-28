import type { Pose } from './Pose'

/** 탐색된 경로 위 한 점. reverse=true면 이 노드로의 이동이 후진이었음을 의미한다. */
export interface PathNode extends Pose {
  reverse: boolean
}
