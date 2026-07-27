# 2D A* 거리 지도(Holonomic Heuristic) 및 로봇 운동학(Kinematics) 구현 — Step 3, 4

## 사용 도구
Claude

## 목적
Step 2(`OccupancyGrid`/`ObstacleInflator`) 완료 후, Hybrid A*의 휴리스틱으로 재사용될 회전 무시 2D A*(`HolonomicObstacleHeuristic`)를 역방향 Dijkstra 기반 Distance Map으로 구현(Step 3)하고, 이어서 자전거 모델 기반 로봇 운동학(`VehicleKinematics`)과 차체 `Footprint`, 모션 프리미티브 생성기(`MotionPrimitiveGenerator`)를 구현하며 이를 검증하는 xUnit 테스트 프로젝트를 신설(Step 4)하기 위함.

## 프롬프트 (원문 그대로)

### 1) Step 3 요청: 기본 2D A*(Holonomic Heuristic) 구현

```text
너는 C# .NET 8.0 및 로봇 경로 탐색(하이브리드 A*) 알고리즘 전문가야.
우리는 1024*768 맵 이미지 기반 하이브리드 A* CLI 탐색 프로그램을 개발 중이야.

[프로젝트 현황 및 설정]
1. Step 0 (설정/파라미터 로더), Step 1 (OpenCvSharp4 기반 이미지 I/O), Step 2 (OccupancyGrid 및 ObstacleInflator) 구현이 모두 완료된 상태야.
2. 주요 구조:
   - IO/ : MapImageLoader, MapImageParser, ResultImageWriter
   - Map/ : OccupancyGrid, ObstacleInflator
   - Planning/ : 하이브리드 A* 및 경로 탐색 알고리즘
3. 주석 규칙: XML Summary 주석은 한 줄(Inline)로 핵심과 단위(px, deg 등)만 컴팩트하게 작성.

[지금 진행할 작업: Step 3 - 기본 2D A* (Holonomic 2D A*) 구현]
Step 2에서 만든 OccupancyGrid와 ObstacleInflator를 활용하여, 회전 제약을 고려하지 않는 기본 2D A* 알고리즘을 구현해줘.
이 2D A*는 단독 탐색용으로도 쓰이지만, 추후 Step 6에서 하이브리드 A*의 휴리스틱으로 100% 재사용될 목적이야.

[Step 3 요구사항]
1. 역방향 Dijkstra/A* 기반 Look-up Table (Distance Map) 생성:
   - 하이브리드 A* 탐색 중 임의의 연속 노드 (x, y)에서 Goal까지의 2D 장애물 고려 거리를 O(1)로 조회해야 함.
   - 따라서 Goal 지점부터 시작하여 맵 전체로 확산되는 2D Distance Map(double[,] 등)을 계산하는 클래스/메서드로 구현해 줘.
2. ObstacleInflator 적용:
   - 2D A* 탐색 시 원본 OccupancyGrid 대신 ObstacleInflator로 부풀려진 점유 격자를 사용해 point-robot 근사 충돌 검사를 수행.
3. 8방향 이동 및 이동 비용:
   - 상하좌우(비용 1.0), 대각선(비용 √2 ≈ 1.414) 8방향 grid 이동 지원.
4. 클래스 위치 및 주요 인터페이스:
   - 위치: `src/Planning/Heuristics/HolonomicObstacleHeuristic.cs` (또는 필요시 2D A* 계산용 Helper 분리 가능)
   - `Init(OccupancyGrid grid, Point goal, double robotRadius)` 또는 생성자에서 Goal 기준 전체 맵의 Distance Map 사전 계산.
   - `GetDistance(int x, int y)` : 특정 격자 좌표에서 Goal까지의 사전 계산된 2D A* 거리(float/double) 반환. Reachable 하지 않은 경우 무한대(double.MaxValue) 반환.
5. 구현 코드와 함께 Program.cs에서 간단히 호출하여 Start에서 Goal까지의 2D A* 코스트 및 거리 지도가 잘 생성되는지 검증할 수 있는 예시 코드도 작성해 줘.
```

### 2) Step 4 진행 요청

```text
step 4도 이어서 구현해줘.
```

## AI 응답 요약

1. **Step 3**: `Planning/Heuristics/IHeuristic.cs` 인터페이스(`Estimate(x, y, headingRad)`)를 신설해 추후 `NonHolonomicHeuristic`(Step 6)과 공통 계약을 공유하도록 설계. `HolonomicObstacleHeuristic.cs`를 구현: 생성자에서 `ObstacleInflator.Inflate(grid, robotRadius)`로 부풀린 격자를 만든 뒤, Goal을 단일 시작점으로 한 Dijkstra(`PriorityQueue<int,double>`, decrease-key 미지원을 감안해 중복 enqueue + stale 항목 skip 방식)로 8방향(직교 1.0, 대각 √2) 전체 Distance Map을 사전계산. `GetDistance(int x, int y)`로 O(1) 조회(도달불가 시 `double.MaxValue`), `Estimate(x, y, headingRad)`로 연속좌표 반올림 지원. `Program.cs`에 `#if DEBUG` `RunHolonomicHeuristicDebugTest` 추가: `map1_corridor.png`에서 Start→Goal 실제 2D A* 거리(2414px)와 직선거리(1112px)를 비교 출력하고, Jet 컬러맵 히트맵을 `test_output/`에 저장해 장애물을 우회하는 그라데이션을 육안 확인.
2. **Step 4**: `Planning/Kinematics/` 폴더에 세 파일 구현.
   - `VehicleKinematics.cs`: 정적 클래스. `Move(x, y, theta, curvature, arcLength)`가 상수 곡률 원호 운동 공식(`θ' = θ + κs`, `x' = x + (1/κ)(sin θ' - sin θ)`, `y' = y - (1/κ)(cos θ' - cos θ)`)을 적용하고 `curvature ≈ 0`일 때 직선 이동으로 근사(0-division 방지). `NormalizeAngle`로 각도를 `[-π, π)`로 정규화.
   - `Footprint.cs`: 사각형 차체를 나타내는 값 타입(`readonly record struct`). `GetCorners(centerX, centerY, headingRad)`가 pose 기준 4개 꼭짓점을 로컬→월드 회전 변환으로 계산.
   - `MotionPrimitiveGenerator.cs`: `SteeringAngleSamples`개의 조향각을 `[-MaxSteeringAngleDeg, +MaxSteeringAngleDeg]`에 균등분포(홀수 개일 때 정확히 0/직진 포함)시키고, 각 조향각을 `curvature = (steeringRad / maxSteeringRad) * (1 / TurningRadius)`로 정규화 변환(최대 조향각일 때 곡률이 로봇 최소회전반경의 역수가 되도록)한 뒤, 전진(및 `ReverseEnabled`일 때 후진)에 대해 `VehicleKinematics.Move`를 호출해 `MotionPrimitive`(다음 pose, 후진 여부, 조향각) 후보 목록을 생성.
   - 좌표 타입으로 `System.Drawing.Point`를 채택(별도 NuGet 없이 .NET 8 런타임에 포함된 `System.Drawing.Primitives`로 크로스플랫폼 사용 가능함을 빌드로 확인). `Program.cs`가 `OpenCvSharp`도 `using`하고 있어 `Point` 이름이 `System.Drawing.Point`/`OpenCvSharp.Point`로 모호해지는 컴파일 에러(CS0104)가 발생 → 디버그 코드의 관련 사용처를 모두 완전정규화(`System.Drawing.Point`/`OpenCvSharp.Point`)해 해결.
   - `tests/PathSearch.Tests/` xUnit 프로젝트(`net8.0`, `src/PathSearch.csproj`에 대한 `ProjectReference`)를 신규 스캐폴딩. `VehicleKinematicsTests`(직선/후진 이동, 1/4원·전체원 궤적의 해석적 검증, 각도 정규화), `FootprintTests`(원점/이동/90도 회전 시 코너 좌표), `MotionPrimitiveGeneratorTests`(프리미티브 개수, 0-조향 직진/후진 프리미티브 일치, 최대 조향각 곡률이 `1/TurningRadius`와 일치, 샘플 1개일 때 직진만 생성) 총 18개 단위테스트를 작성해 `dotnet test`로 전부 통과 확인.

## 사용자 피드백
- Step 단위 로드맵을 "step N도 이어서 구현해줘"처럼 짧게 요청해도 이전 세션의 세부 컨텍스트(파라미터 구조, 폴더 규칙, XML 주석 스타일 등)를 그대로 이어받아 진행하는 방식을 기대함(재설명 불필요).
- 로드맵 문구에 "단위테스트"가 명시되면, 콘솔 디버그 프린트 수준이 아니라 실제 xUnit 테스트 프로젝트를 신설하는 수준의 검증을 기대함.

## 참고
- 본 세션은 이전 대화(Step 0~2)를 이어받아 진행됨; Step 0~2 기록은 `prompts/01_initial_design.md`, `prompts/02_map_parsing.md` 참고.
- Step 3/4 구현 이후 발견된 Distance Map 시각화 관련 디버깅 과정은 `prompts/05_debug_hybrid_astar_core.md`에 별도 기록.
