# Step 5/6 Hybrid A* 탐색기·이중 휴리스틱 구현, 경로 시각화, 탐색 성능 최적화

## 사용 도구
Claude

## 목적
Step 0~4(설정/파라미터, 이미지 I/O, 맵 모델, 로봇 운동학·Footprint·모션 프리미티브)까지 완료된 상태에서, Hybrid A* 메인 탐색 루프(Step 5)와 Holonomic+Non-Holonomic 이중 휴리스틱 결합(Step 6)을 신규 구현하고, 결과 경로를 육안으로 확인할 수 있는 디버깅용 시각화 이미지 생성 기능을 추가한 뒤, 실제 지그재그 미로 맵에서 관측된 탐색 속도 병목(약 59만 노드 방문에 24초 소요)의 원인을 분석하고 정확성을 유지한 채 성능을 최적화하기 위함.

## 프롬프트 (원문 그대로)

### 1) Step 5/6 구현 요청

```text
너는 C# .NET 8.0 및 로봇 경로 탐색(하이브리드 A*) 알고리즘 전문가야.
우리는 1024*768 맵 이미지 기반 하이브리드 A* CLI 탐색 프로그램을 개발 중이야.

[프로젝트 현황 및 설정]
1. Step 0 ~ Step 4까지 구현이 완료된 상태야.
   - Step 3: Holonomic 2D A* Distance Map (Look-up Table 사전 계산) 정상 작동 확인됨.
   - Step 4: Kinematics (Ackermann/Unicycle 모션 프리미티브), Footprint, 정밀 충돌 검사(CollisionChecker) 완료.
2. 주석 규칙: XML Summary 주석은 한 줄(Inline)로 핵심과 단위(px, deg 등)만 컴팩트하게 작성.

[지금 진행할 작업: Step 5 & 6 - 하이브리드 A* 탐색기 및 휴리스틱 결합]
Step 3의 2D Distance Map과 Step 4의 운동학/충돌검사를 결합하여 **Hybrid A* 메인 탐색 알고리즘**과 **이중 휴리스틱(Holonomic + Non-Holonomic) 결합**을 구현해 줘.

[Step 5/6 핵심 요구사항]

1. **상태 공간 범주화 (StateDiscretizer & PriorityOpenSet)**
   - 연속 상태 $(x, y, \theta)$를 Closed 판정용 3D 격자 인덱스 $(i_x, i_y, i_\theta)$로 변환.
     - $i_x = \lfloor x / GridResolution \rfloor$
     - $i_y = \lfloor y / GridResolution \rfloor$
     - $i_\theta = \lfloor \theta / HeadingResolutionDeg \rfloor$
   - 동일한 $(i_x, i_y, i_\theta)$ 셀에 이미 방문한 노드가 있고 $g$ 코스트가 더 크다면 스킵(Closed 처리).
   - PriorityQueue를 활용해 $f = g + h$ 값이 가장 작은 노드를 Pop하는 OpenSet 관리.

2. **휴리스틱 결합 (Dual Heuristics: Step 6)**
   - $h(x, y, \theta) = \max(h_{\text{holonomic}}(x, y), h_{\text{non-holonomic}}(x, y, \theta))$
     - $h_{\text{holonomic}}$: Step 3에서 작성한 2D Distance Map lookup 값 ($O(1)$)
     - $h_{\text{non-holonomic}}$: 회전 제약만 고려한 곡선 거리 (Analytic Reeds-Shepp 또는 Dubins distance 근사/계산식)
   - 두 휴리스틱 중 더 높은(Admissible하면서 Tight한) 코스트를 최종 $h$로 선택.
   - 코스트 페널티 적용: 후진($ReversePenalty$), 방향 전환($DirectionChangePenalty$) 등 `data/parameter.json` 설정값 반영.

3. **HybridAStarPlanner 메인 루프 (Step 5)**
   - `Search(HybridState start, HybridState goal, OccupancyGrid grid, ObstacleInflator inflator)` 구현
   - Loop:
     1. OpenSet에서 $f$ 최소 노드 Pop
     2. Goal Tolerance(목표 위치/각도 오차 범위 내) 도달 시 경로 역추적(Backtracking) 후 반환
     3. `MotionPrimitiveGenerator`로 전진/후진 및 조향각 후보 노드들 생성
     4. `FootprintCollisionChecker`로 정밀 충돌 검사 수행
     5. 통과된 후보 노드의 $g, h, f$ 계산 후 OpenSet push 및 Discretizer에 기록
   - 최대 탐색 노드 수/시간 초과 시 안전하게 실패 처리.

4. **클래스 구조 및 위치**
   - `src/Planning/HybridState.cs` (탐색 노드 정의: x, y, theta, g, h, f, Parent, IsReverse 등)
   - `src/Planning/StateDiscretizer.cs`
   - `src/Planning/PriorityOpenSet.cs`
   - `src/Planning/Heuristics/NonHolonomicHeuristic.cs` 및 `IHeuristic.cs`
   - `src/Planning/HybridAStarPlanner.cs`

5. **자가 검증 코드 포함**
   - `Program.cs`에서 맵 1개에 대해 실제 Start부터 Goal까지 하이브리드 A* 탐색을 수행하고, 탐색 성공 여부, 걸린 시간, 탐색된 노드 수, 생성된 최종 경로 점 목록/코스트를 콘솔로 출력하는 검증용 예시 코드를 포함해 줘.
```

### 2) 경로 시각화 요청

```text
해당 경로를 디버깅용으로 시각화하기 위한 이미지를 생성 및 출력해줘.
```

### 3) 탐색 성능 최적화 요청

```text
현재 Step 5/6 하이브리드 A* 탐색 속도가 너무 느려 (50만 노드 방문 시 약 24초 소요).
Analytic Expansion(Step 7)을 붙이기 전에, 탐색 루프의 연산 병목을 해결하고 탐색 성능을 최적화하고 싶어.

[주요 병목 원인 추정 및 최적화 지시사항]

1. Footprint 충돌 검사 2단계 캐싱/사전검사 (Early Out)
   - 매 노드 생성 시 정밀 차체 꼭짓점(Footprint) 충돌 검사를 수행하면 매우 느려집니다.
   - 1차 검사: 로봇을 포함하는 외접원(Bounding Circle) 반경으로 `ObstacleInflator` 점유 여부를 빠르게 체크(O(1))하고, 통과한 노드만 2차 정밀 Footprint SAT 충돌 검사로 넘기도록 개선해 줘.

2. StateDiscretizer 및 Closed Set 컬렉션 최적화
   - `(ix, iy, itheta)` 인덱스에 대한 Hash 연산 오버헤드를 줄여 줘.
   - C# `HashSet<ValueTuple>` 대신, 3D 구조를 단일 bit/int 키로 비트 연산(`(ix << 20) | (iy << 10) | itheta`)하거나, 1차원/3D 구조의 `bool[,,]` 또는 `int[,,]` 배열 기반 Direct Look-up Table로 Closed 처리를 수행하도록 변경해 줘.

3. 힙(PriorityQueue) 및 메모리 할당(GC) 최소화
   - 탐색 루프 내부에서 `new` 객체 생성(GC Allocations)을 최소화해 줘. (`HybridState` 생성 시 struct 고려 또는 풀링/값 전달)
   - `PriorityQueue`에 쓸데없이 중복되거나 비효율적인 노드가 인큐되지 않도록, Discretizer에서 더 높은 코스트의 동일 격자 방문 시 즉시 컷팅할 것.

4. 파라미터 및 해상도 가이드 점검
   - `HeadingResolutionDeg` (예: 15° → 30°), `GridResolution` (예: 4px → 8px), `StepSize` 등의 파라미터를 조절했을 때 성능 변화를 확인할 수 있도록 점검 로직을 추가해 줘.

위 1~3번 최적화 기법을 적용하여 코드 연산 속도를 대폭 개선하고, 수정된 관련 클래스 및 검산 로직을 제공해 줘.
```

### 4) 테스트 코드 정리 및 프롬프트 기록 요청

```text
테스트 코드 제거 후,
해당 세션에서 나눈 대화를 /commands/LOG_PROMPT.md를 사용하여 "06_path_search.md"로 제작해줘.
```

## AI 응답 요약

1. **Step 5 구현**: `Planning/Collision/FootprintCollisionChecker.cs`(Step 4에서 실제로는 누락돼 있던 파일)를 신규 작성 — Footprint 4개 꼭짓점의 바운딩 박스를 픽셀 단위로 순회하며 각 픽셀 중심을 로봇 로컬 좌표계로 역회전시켜 사각형 내부 여부와 `OccupancyGrid` 점유 여부를 판정. `HybridState`(pose+g/h/f+Parent+IsReverse), `StateDiscretizer`((ix,iy,itheta) 격자 인덱스로 변환하고 셀별 최소 g코스트를 관리해 Closed 판정), `PriorityOpenSet`(`PriorityQueue<HybridState,double>`의 decrease-key 미지원을 감안해 중복 push 허용 + Pop 시 discretizer 기준 stale 노드 skip)을 구현. `HybridAStarPlanner.Search()`가 Pop→목표 허용오차(`GoalToleranceXY`/`GoalToleranceThetaDeg`, 신규 파라미터) 판정→모션 프리미티브 생성→충돌검사→g/h/f 계산→push 루프를 수행하고, `MaxSearchNodes`/`MaxSearchSeconds`(둘 다 신규 파라미터) 초과 시 실패 처리.
2. **Step 6 구현**: `Planning/Heuristics/NonHolonomicHeuristic.cs` 구현 — 전체 Reeds-Shepp 48-word 대신 LSL/RSR(동일 방향 회전) 해석해만 계산하고, 해석 불가(반대 방향 S자 경로) 케이스는 유클리드 거리+회전 보정항으로 근사(요구사항의 "근사/계산식" 허용 범위 내 단순화, 검증 가능한 스트레이트 케이스로 공식 정확성 확인). 후진 허용 시 heading을 π 반전해 재계산 후 최솟값 채택. `h = max(holonomic, non-holonomic)` 결합과 ReversePenalty/DirectionChangePenalty 코스트 반영을 `HybridAStarPlanner`에 구현.
3. **자가 검증 및 디버깅**: `Program.cs`에 `map1_corridor.png`(지그재그 미로) 대상 자가 검증 코드 작성 후 실행했더니 기본 안전 한도(20만 노드/20초) 초과로 실패. 장애물 없는 합성 격자로 먼저 테스트(18노드/0.01초 성공)해 알고리즘 자체엔 문제가 없음을 확인한 뒤, 한도를 임시로 늘려 재실행 → 실제로는 59만 노드/24초 만에 경로(297점, 비용 2368px)를 정상적으로 찾아냄. 원인은 버그가 아니라 24개 heading bin × 256×192 격자라는 큰 상태공간이었으므로, `MaxSearchNodes`(20만→100만)/`MaxSearchSeconds`(20→60초) 기본값 조정으로 대응.
4. **경로 시각화(2번 프롬프트)**: 로드맵 8단계에 해당하는 `Visualization/PathOverlayRenderer.cs`(순수 렌더링: 원본 Mat 복제 후 경로 세그먼트를 전진=파랑/후진=빨강으로 그리고 각 노드에 heading 방향 틱 표시)와 `IO/ResultImageWriter.cs`(`results/result_{원본파일명}.png` 저장)를 신규 작성해 `RunHybridAStarSelfCheck`에 연결. `map1_corridor.png` 결과 이미지를 실제로 열어 시작(초록)→목표(빨강)까지 지그재그 통로의 틈을 통과하며 회전 반경 제약에 맞는 부드러운 곡선 경로가 그려졌음을 육안으로 확인.
5. **성능 최적화(3번 프롬프트)**: 요청받은 1~3번 기법을 그대로 적용.
   - (1) `FootprintCollisionChecker` 생성자에서 외접원(`sqrt((L/2)²+(W/2)²)`) 반경으로 `ObstacleInflator.Inflate()`를 1회만 실행해 캐싱하고, `IsColliding()`이 이 격자를 O(1) 조회해 외접원조차 장애물과 안 겹치면 정밀 래스터화(2차)를 생략하도록 변경. 오탐(false positive)만 발생 가능해 최종 정확도는 그대로 유지됨을 설계 근거로 확인. `ObstacleInflator`는 "휴리스틱 전용"에서 "휴리스틱 Distance Map + Collision Early-Out 공용 유틸리티"로 문서(주석/CLAUDE.md) 갱신.
   - (2) `StateDiscretizer`를 `Dictionary<(int,int,int),double>` → 맵 크기·해상도로 크기가 고정된 1차원 `double[]` Direct Look-up Table로 교체(해시 연산 완전 제거, flat index 산술 접근만 사용). 과도하게 작은 GridResolution/HeadingResolutionDeg로 배열이 5,000만 셀을 넘으면 명확한 예외로 안내하는 안전장치 추가. 이에 맞춰 `HybridAStarPlanner` 생성자가 `OccupancyGrid`를 받아 `_grid.Width/Height`로 LUT 크기를 계산하도록 변경.
   - (3) `MotionPrimitiveGenerator.Generate()`가 매 확장마다 `List<MotionPrimitive>`를 새로 할당하던 것을 내부 재사용 버퍼 + `ReadOnlySpan<MotionPrimitive>` 반환으로 변경(GC 압력 제거). 열등 후보를 `HybridState` 생성 전에 `discretizer.TryUpdate`로 컷팅하는 기존 로직은 이미 최적이었음을 확인하고 유지.
   - (4) `Program.cs`에 `RunResolutionSweep()`을 임시로 추가해 GridResolution/HeadingResolutionDeg 4가지 조합(4px/15°, 8px/15°, 4px/30°, 8px/30°)의 노드 수·시간·경로비용을 비교 측정.
   - **검증 결과**: 최적화 전후 동일 파라미터(4px/15°)에서 정확히 같은 노드 수(590,192)·같은 경로(297점, 2368px)가 나와 알고리즘 동작 불변을 확인했고, 소요시간만 **24.0초 → 2.7초(약 9배)** 로 단축. 해상도 스윕 결과 8px/15°는 79,757노드/0.36초(비용 2434px), 4px/30°는 274,608노드/1.14초(비용 2416px), 8px/30°는 74,953노드/0.29초(비용 2546px)로, 해상도를 낮출수록 빨라지지만 경로 비용(품질)이 소폭 나빠지는 트레이드오프를 수치로 확인.
6. **정리(4번 프롬프트)**: 최적화 검증 목적으로 추가했던 임시 점검 코드 `RunResolutionSweep()`과 그 호출부를 `Program.cs`에서 제거(원래 Step 5/6 요구사항에 명시된 `RunHybridAStarSelfCheck`은 유지). CLAUDE.md의 관련 기록도 "검증 후 제거된 임시 점검 코드"로 갱신해 문서와 실제 코드 상태를 일치시킴. 이어서 `LOG_PROMPT` 스킬로 본 문서(`prompts/06_path_search.md`)를 생성.

## 반영 여부 및 이유

### 1) Step 5/6 구현 요청
반영 여부: 그대로 반영
이유: 상태 공간 범주화, 이중 휴리스틱 결합, 메인 탐색 루프까지 요청한 구조 그대로 구현됨.

### 2) 경로 시각화 요청
반영 여부: 그대로 반영
이유: 디버깅용으로 경로를 이미지에 그려서 보여달라고 했고, 그대로 반영됨.

### 3) 탐색 성능 최적화 요청
반영 여부: 그대로 반영
이유: 실제로 디버깅 시 탐색 속도가 개선됨을 확인하였으며, 개선 방안에 대한 내용도 합리적이라고 판단하여 그대로 반영됨.

### 4) 테스트 코드 정리 및 프롬프트 기록 요청
반영 여부: 그대로 반영
이유: 확인용으로 잠깐 넣었던 임시 코드(`RunResolutionSweep`)는 지우고 필요한 코드만 남겨달라고 했고, 그대로 반영됨.
