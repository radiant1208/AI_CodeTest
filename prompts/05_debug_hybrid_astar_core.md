# HolonomicObstacleHeuristic Distance Map 시각화 버그 진단 및 코너컷 방어 로직 추가

## 사용 도구
Claude

## 목적
사용자가 Distance Map 히트맵 시각화 결과에서 Goal→Start 색상이 장애물(긴 파란색 장벽)을 무시하고 직선거리(Euclidean)처럼 통과해 칠해지는 것으로 보인다고 문제를 제기함. 이것이 `HolonomicObstacleHeuristic`의 역방향 Dijkstra 로직 자체의 버그인지, 아니면 시각화 단계의 문제인지를 자가검증(Self-Verification)하여 근본 원인을 규명하고, 실제로 필요한 수정을 반영하기 위함.

## 프롬프트 (원문 그대로)

### 1) Distance Map 시각화 버그 의심 및 수정·자가검산 요청

```text
첨부한 이미지는 현재 작성된 HolonomicObstacleHeuristic의 거리 지도(Distance Map) 시각화 결과야.

[문제 현상 분석]
1. 시각화 결과를 보면 우측 상단의 Goal부터 좌측 하단의 Start까지 색상이 장애물(긴 파란색 장벽)을 무시하고 **직선거리(Euclidean Distance) 형태로 통과해서** 칠해지고 있어.
2. 2D A*/Dijkstra 기반 역방향 확산(Goal -> All Nodes)이 아니라, 단순 유클리디안 거리 공식으로 배열을 채웠거나 OpenSet 확산 시 장애물 조건을 검사하지 않는 문제가 발생하는 것으로 보여.
3. 이대로 두면 하이브리드 A* 탐색 시 벽을 뚫는 코스트를 참조하게 되어 탐색 성능이 크게 저하되거나 실패하게 돼.

[수정 및 자가 검산 요청사항]
1. `HolonomicObstacleHeuristic.cs` 내의 Distance Map 생성 로직을 **Goal 지점부터 출발하는 역방향 Dijkstra (8방향 확산)** 방식으로 완전히 수정해 줘.
2. 노드 확산 시 반드시 `ObstacleInflator`(또는 `OccupancyGrid`)의 장애물 조건(`IsObstacle`)을 체크하여, **장애물 영역은 큐(PriorityQueue)에 넣지도 않고 통과도 못 하도록** 철저히 방어할 것.
3. 대각선 이동 비용($\sqrt{2} \approx 1.414$)과 상하좌우 이동 비용($1.0$)을 정확히 반영해 줘.
4. 작성한 코드가 위 문제를 해결하는지 **스스로 코드 흐름을 Step-by-Step으로 추적(Self-Verification)하여 검산**한 결과를 짧게 설명하고, 수정된 전체 코드를 제공해 줘.
```

## AI 응답 요약

1. **코드 리뷰(사실 확인)**: `ComputeDistanceMap`을 라인 단위로 재추적 — goal을 `distance=0`으로 seed, 이웃 확장 시 `!inflated.IsInside(nx, ny) || inflated.IsOccupied(nx, ny)`이면 즉시 `continue`(장애물은 큐에 들어가지도, 갱신되지도 않음), 비용은 직교 1.0/대각 √2로 이미 정확히 반영되어 있음을 확인. 즉 요청된 "장애물 검사 후 역방향 Dijkstra"는 이미 구현돼 있었고, 이 시점에서 로직 자체엔 버그가 없다고 잠정 결론.
2. **가설 검증**: 로직이 맞다면 실제 원인은 시각화일 가능성이 높다고 보고, `Program.cs`의 `RunHolonomicHeuristicDebugTest`를 3개 맵(`map1_corridor`, `map2_scattered`, `map3_rooms`) 전체를 순회하도록 임시 확장하고, `SaveDistanceMapHeatmap`에 원본 `OccupancyGrid`의 실제 장애물 픽셀을 순수 검정으로 별도 오버레이하는 로직을 추가해 재실행. `map3_rooms`처럼 방과 문(gap)이 뚜렷한 맵에서 색상이 벽 경계마다 완전히 끊기고 오직 문 구간으로만 이어지는 것을 육안으로 확인 — 유클리디안 거리라면 나올 수 없는 결과. 이로써 기존 히트맵이 "도달불가(`double.MaxValue`)"와 "그 맵의 최대 유한거리"를 동일하게 정규화값 0(파랑)으로 칠해, 장애물이 배경의 "먼 영역" 그라데이션과 같은 색으로 묻혀버리는 **시각화 버그**였음을 최종 확인(알고리즘 버그가 아니었음).
3. **방어 로직 보강**: 사용자가 요청한 "철저한 방어" 취지에 맞춰, 기존엔 없었던 **대각선 코너 컷팅(corner cutting) 방지**를 `HolonomicObstacleHeuristic.ComputeDistanceMap`에 추가 — 대각 이동 시 인접한 두 직교 셀(코너) 중 하나라도 장애물이면 그 대각 이동을 차단. 로직 버그는 아니었지만, 실제 로봇이 통과할 수 없는 대각선 모서리 스침을 막는 안전장치로 보강. 수정 후 `map1`(2414.00→2416.34px), `map3`(1399.69→1400.86px) 거리가 소폭 증가해, 이전에 허용되던 "불법 코너컷 지름길"이 실제로 막혔음을 수치로 확인.
4. **자동 검증 코드 추가**: `Program.cs`에 `VerifyNoDistanceLeaksIntoObstacles`를 신설 — 인플레이트된 장애물 셀 중 유한 거리값이 기록된 셀 수를 실행 시점에 직접 세어 콘솔에 출력(`0`이어야 정상). 3개 맵 전부 `"0 => 정상(누출 없음)"`으로 확인. 기존 18개 xUnit 단위테스트도 재실행해 전부 통과(회귀 없음) 확인.

## 사용자 피드백
- 버그로 의심되는 현상을 보고할 때 스크린샷(시각적 근거)과 함께 "왜 이런 현상이 생길 수 있는지에 대한 자체 가설"까지 구체적으로 제시하는 방식으로 소통함.
- 수정을 요청할 때 단순 코드 변경에 그치지 않고, 그 수정이 실제로 문제를 해결하는지 Step-by-Step 자가검증(Self-Verification) 결과를 함께 요구함 — 말로 된 설명보다 실행 결과(수치, 이미지)로 된 근거 제시를 선호.

## 참고
- 본 디버깅의 대상이 된 최초 구현(`HolonomicObstacleHeuristic.cs`, 2D A* Distance Map)은 `prompts/04_hybrid_astar_core.md`에 기록되어 있으며, 본 문서는 그 이후 시각화 버그 진단·수정 과정만을 다룸.
- 최종 결론은 "알고리즘이 아닌 시각화(`Program.cs`의 `SaveDistanceMapHeatmap` 정규화 방식)가 원인"이었다는 것이며, 그럼에도 사용자가 요청한 방어 로직(대각선 코너컷 방지) 및 자동 누출 검증은 실제로 코드에 반영됨.
