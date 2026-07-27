# PathSearch — Hybrid A* CLI 프로젝트

1024×768 맵 이미지를 입력받아 하이브리드 A*(Hybrid A*)로 경로를 탐색하고, 원본 이미지에 경로를 그려 저장하는 C# .NET 8.0 콘솔 프로그램.

## 기술 스택
- C# .NET 8.0 콘솔 애플리케이션
- OpenCvSharp4 (이미지 로드, 이진화, 시각화 오버레이, PNG 저장)
- Microsoft.Extensions.Configuration / Configuration.Json (appsettings.json 로드)
- 입력: `maps/` 폴더의 1024×768 맵 이미지 (초록 점=시작, 빨강 점=도착, 검정=장애물/벽, 밝은 회색=이동 가능 영역)
- 출력: `results/result_{원본 파일명}.png`

## 핵심 설계 원칙

하이브리드 A*는 일반 A*와 달리 로봇의 방향(heading, θ)과 최소 회전 반경 같은 운동학 제약을 반영하는 **연속 공간(Continuous Space) 탐색**이다. 상태는 `(x, y)`가 아니라 `(x, y, θ)`이며, 이웃 노드는 격자의 상하좌우가 아니라 로봇이 실제로 움직일 수 있는 모션 프리미티브(조향각 후보 × 전/후진)로 생성된다. 이 특성 때문에 아래처럼 계층을 분리했다.

| 계층 | 책임 | 비고 |
|---|---|---|
| `IO/` | 이미지 ↔ 데이터 변환 (PNG 로드/파싱/저장) | 입력 형식이 바뀌어도 `Planning/`은 영향받지 않음 |
| `Map/` | 환경(점유 격자) 표현 | `ObstacleInflator`는 휴리스틱 전용(아래 참고) |
| `Parameter/` | 로봇/알고리즘 파라미터 로드 | 경로 설정(appsettings.json)과는 별도 관리 |
| `Planning/` | 하이브리드 A* 알고리즘 본체 | 이미지/콘솔 I/O를 모르는 순수 로직 |
| `Visualization/` | 결과 오버레이 렌더링 | 알고리즘 코드와 분리 |
| `App/` | 위 계층을 순서대로 호출하는 파이프라인/CLI | `Program.cs`는 최대한 얇게 유지 |

## 폴더 구조

```
AI_CodeTest/
├── data/
│   └── parameter.json                  # 로봇/탐색 알고리즘 전용 파라미터
├── maps/                                # 입력 맵 이미지
├── results/                              # result_{원본파일명}.png 저장 위치
├── prompts/                              # LOG_PROMPT 명령으로 기록되는 프롬프트 히스토리
├── src/
│   ├── PathSearch.csproj
│   ├── Program.cs
│   ├── appsettings.json                # 경로 설정 전용: MapDirectory, DataDirectory, ResultDirectory
│   │
│   ├── Parameter/
│   │   ├── Parameters.cs               # Robot/Search 파라미터 POCO
│   │   └── ParameterLoader.cs          # data/parameter.json → Parameters 역직렬화
│   │
│   ├── IO/
│   │   ├── MapImageLoader.cs           # PNG 로드 (OpenCvSharp Mat)
│   │   ├── MapImageParser.cs           # Mat → OccupancyGrid + 시작/도착 좌표 (색상 기반)
│   │   └── ResultImageWriter.cs        # 원본 위에 경로 오버레이 후 PNG 저장
│   │
│   ├── Map/
│   │   ├── OccupancyGrid.cs            # 2차원 점유 격자
│   │   └── ObstacleInflator.cs         # 휴리스틱 전용 point-robot 근사 (장애물 팽창)
│   │
│   ├── Planning/                        # ★ 하이브리드 A* 핵심 로직
│   │   ├── Kinematics/
│   │   │   ├── VehicleKinematics.cs    # 자전거 모델: (x,y,θ)+조향각+StepSize → 다음 연속 좌표
│   │   │   ├── Footprint.cs            # 차체 형상(사각형) 정의, pose별 꼭짓점 계산
│   │   │   └── MotionPrimitiveGenerator.cs  # 조향각 후보 × 전/후진 → 다음 상태 후보 생성
│   │   ├── Collision/
│   │   │   └── FootprintCollisionChecker.cs # Footprint 기반 정밀 충돌 검사 (실제 탐색용)
│   │   ├── HybridState.cs              # 탐색 노드 (x, y, θ, g, h, 부모, 전진/후진)
│   │   ├── StateDiscretizer.cs         # 연속 상태 → CLOSED 판정용 격자·각도 인덱스
│   │   ├── Heuristics/
│   │   │   ├── IHeuristic.cs
│   │   │   ├── HolonomicObstacleHeuristic.cs  # 장애물 고려 2D A* 기반 (사전계산, point-robot 근사)
│   │   │   └── NonHolonomicHeuristic.cs       # 회전 제약만 고려한 곡선 거리(Dubins/Reeds-Shepp)
│   │   ├── AnalyticExpansion.cs        # 목표까지 곡선으로 한번에 연결 시도
│   │   ├── PriorityOpenSet.cs          # OPEN(우선순위 큐) / CLOSED 집합 관리
│   │   └── HybridAStarPlanner.cs       # 전체 탐색 루프 오케스트레이터
│   │
│   ├── Visualization/
│   │   └── PathOverlayRenderer.cs      # 경로 선/노드를 이미지 위에 그리는 순수 렌더링 로직
│   │
│   └── App/
│       ├── AppConfig.cs                # appsettings.json → IConfigurationRoot 정적 접근 + 필수값 검증
│       ├── PlanningPipeline.cs         # 맵 1개: 로드→파싱→탐색→렌더→저장 전체 흐름
│       └── CliRunner.cs                # maps/ 폴더 전체 순회, 콘솔 진행상황/결과 출력
```

## 파라미터 관리 규칙 (appsettings.json vs parameter.json)

두 파일 모두 "설정"이지만 성격과 변경 주기가 다르므로 **엄격히 분리**한다.

| 파일 | 위치 | 담당 클래스 | 내용 | 성격 |
|---|---|---|---|---|
| `src/appsettings.json` | 실행 경로 | `App/AppConfig.cs` | `MapDirectory`, `DataDirectory`, `ResultDirectory` | 인프라 설정 (파일 위치) |
| `data/parameter.json` | 데이터 경로 | `Parameter/Parameters.cs` | Footprint, TurningRadius, StepSize, GridResolution 등 | 알고리즘/로봇 도메인 값 |

- **새 로봇/알고리즘 파라미터를 추가할 때는 반드시 `data/parameter.json` + `Parameter/Parameters.cs`에만 추가**하고, `appsettings.json`에는 경로 외의 값을 넣지 않는다.
- `App/AppConfig.cs`는 별도 POCO를 만들지 않고 `ConfigurationBuilder`로 생성한 `IConfigurationRoot`를 정적으로 보관해 `MapDirectory`/`DataDirectory`/`ResultDirectory`를 인덱서로 직접 노출한다. `appsettings.json` 파일 자체, `MapDirectory`, `DataDirectory`는 필수 요소이며 하나라도 없으면 `Validate(out string error)`가 `false`를 반환 — `Program.cs`는 이때 에러 로그를 출력하고 `return`으로 즉시 종료한다 (예외 throw 없음).
- `Parameter/ParameterLoader.cs`는 `System.Text.Json`으로 역직렬화만 담당한다. `data/parameter.json`이 없으면 예외를 던지지 않고 `new Parameters()` 기본값으로 폴백한다 (파라미터는 appsettings.json과 달리 필수가 아님).
- 단위는 별도 스케일 설정이 없는 한 **픽셀** 기준이다 (맵이 실좌표계 없는 1024×768 이미지이므로). 실좌표 스케일이 필요해지면 `RobotParameters`에 `PixelsPerMeter` 같은 필드를 추가한다.

### `data/parameter.json` 구조

```json
{
  "Robot": {
    "FootprintLength": 30.0,
    "FootprintWidth": 20.0,
    "TurningRadius": 40.0,
    "MaxSteeringAngleDeg": 35.0
  },
  "Search": {
    "StepSize": 8.0,
    "GridResolution": 4.0,
    "HeadingResolutionDeg": 15.0,
    "SteeringAngleSamples": 5,
    "ReverseEnabled": true,
    "ReversePenalty": 2.0,
    "DirectionChangePenalty": 5.0,
    "AnalyticExpansionInterval": 10
  }
}
```

## Kinematics / Footprint / Collision 분리 이유 (`Planning/`)

- `Kinematics/VehicleKinematics.cs`: 조향각과 `TurningRadius`로 다음 연속 좌표 `(x', y', θ')`를 계산. **탐색은 끝까지 연속 좌표로 진행**하며, `StateDiscretizer`는 CLOSED 판정에만 사용된다 (단순 2D 격자 A*가 아님).
- `Kinematics/Footprint.cs`: 로봇 차체를 사각형으로 정의하는 값 객체. pose가 주어지면 꼭짓점을 계산.
- `Collision/FootprintCollisionChecker.cs`: 실제 탐색 중 각 후보 상태에 대해 `Footprint`를 이용한 정밀 충돌 검사 (정확하지만 느림).
- `Map/ObstacleInflator.cs`: `HolonomicObstacleHeuristic`이 매번 정밀 검사를 하면 느리므로, 로봇을 원형 점으로 근사하고 장애물을 반경만큼 부풀린 격자로 빠른 2D A*를 돌리기 위한 전처리 (부정확하지만 빠름, 휴리스틱 전용).

두 충돌 검사 방식(정밀 Footprint vs 팽창 근사)이 공존하는 것은 중복이 아니라, 하나는 "탐색의 정확성", 하나는 "휴리스틱의 속도"를 위한 의도된 설계다.

## 단계별 구현 로드맵

| 단계 | 목표 |
|---|---|
| 0 | 골격 정리: `Parameter/`, `App/AppConfig.cs`, `appsettings.json` 확장 |
| 1 | 이미지 I/O 파이프라인 (`IO/`) — 경로 없이 원본 그대로 저장까지 end-to-end 확인 |
| 2 | 맵 모델 (`Map/OccupancyGrid`, `ObstacleInflator`) |
| 3 | 기본 2D A* (회전 무시) — 추후 `HolonomicObstacleHeuristic`으로 재사용 |
| 4 | 로봇 운동학 (`Kinematics/`) — 후보 상태 생성 단위테스트 |
| 5 | Hybrid A* 본 탐색 (`StateDiscretizer`, `FootprintCollisionChecker`, `PriorityOpenSet`, `HybridAStarPlanner`) |
| 6 | 휴리스틱 결합 (`HolonomicObstacleHeuristic` + `NonHolonomicHeuristic`) |
| 7 | Analytic Expansion (Reeds-Shepp/Dubins) |
| 8 | 시각화 (`PathOverlayRenderer`) 정식 구현 |
| 9 | CLI 통합 (`CliRunner`) — `maps/` 전체 일괄 처리 |
| 10 | 튜닝/마무리 — `parameter.json` 값 조정 |

## 진행 상황 (2026-07-27 기준)

- [x] `Parameter/Parameters.cs`, `Parameter/ParameterLoader.cs`, `data/parameter.json` 스캐폴딩 완료 (빌드 확인됨)
- [x] `Parameters.cs` 모든 속성에 단위(px/deg/count/배율)·목적 XML 주석 추가
- [x] **0단계 완료**: `appsettings.json`에 `ResultDirectory` 추가, `Program.cs`에서 시작 시 설정/파라미터를 로드하도록 연결 (클래스명 충돌 회피 위해 `App` → `Program`으로 변경), 빌드·실행 확인 완료
- [x] `App/AppConfig.cs`로 교체: 별도 POCO 없이 `IConfigurationRoot`를 정적으로 노출하는 방식으로 변경. `Validate()`로 appsettings.json/`MapDirectory`/`DataDirectory` 누락 시 `Program.cs`가 에러 로그 후 `return`하도록 처리. `ParameterLoader`는 `data/parameter.json` 누락 시 예외 대신 `new Parameters()`로 폴백하도록 변경. `Microsoft.Extensions.Configuration`, `Configuration.Json` 패키지 추가. 정상/appsettings.json 누락/parameter.json 누락 3가지 시나리오 실행 확인 완료
- [x] **1~4단계 완료**: `IO/`(이미지 로드/파싱/저장), `Map/`(OccupancyGrid, ObstacleInflator), 2D A* 기반 `HolonomicObstacleHeuristic`, `Kinematics/`(VehicleKinematics, Footprint, MotionPrimitiveGenerator) 구현 완료
- [x] **5단계 완료**: `Planning/Collision/FootprintCollisionChecker.cs`(사각형 Footprint 래스터화 정밀 충돌검사, Step 4에서 누락되어 있던 파일) 신규 작성. `HybridState`, `StateDiscretizer`((ix,iy,itheta) 격자 인덱스 + 셀별 최소 g 기반 Closed 처리), `PriorityOpenSet`(PriorityQueue 기반, stale 노드는 Pop 시 discretizer로 검증) 구현. `HybridAStarPlanner.Search()`가 Pop→목표 허용오차 판정→모션 프리미티브 생성→충돌검사→g/h/f 계산→push 루프를 수행하고, `MaxSearchNodes`/`MaxSearchSeconds`(둘 다 `data/parameter.json` 신규 파라미터) 초과 시 실패 처리
- [x] **6단계 완료**: `Planning/Heuristics/NonHolonomicHeuristic.cs` 구현 — TurningRadius 제약만 고려한 Dubins 곡선 거리 근사(LSL/RSR 해석해, 해석 불가 케이스는 유클리드 거리+회전 보정항으로 근사; 전체 Reeds-Shepp 대신 근사식 채택). `h = max(holonomic, non-holonomic)` 결합, ReversePenalty/DirectionChangePenalty 코스트 반영. `Program.cs`에 맵 1개(`map1_corridor.png`) 대상 자가 검증 코드 추가 — 실행 확인 결과 지그재그 미로에서 약 59만 노드/24초 만에 경로(297점, 비용 2368px) 탐색 성공. 이 결과에 맞춰 `MaxSearchNodes` 기본값을 200,000→1,000,000, `MaxSearchSeconds`를 20→60으로 상향 조정
- [ ] 7단계: `AnalyticExpansion.cs`(Reeds-Shepp/Dubins 곡선으로 목표까지 한번에 연결 시도) — 아직 미착수, 현재는 매 스텝 모션 프리미티브만으로 탐색
- [x] **8단계 일부(디버깅용) 완료**: `Visualization/PathOverlayRenderer.cs`(경로 세그먼트 전진=파랑/후진=빨강 색상 구분 + 노드별 heading 틱, 순수 렌더링) + `IO/ResultImageWriter.cs`(`results/result_{원본파일명}.png` 저장) 신규 작성, `Program.cs` 자가 검증 코드에 연결해 `map1_corridor.png` 결과 이미지 생성 확인. 정식 CLI 통합(모든 맵 대상 자동 렌더링/스타일 확정)은 9~10단계에서 마무리 예정
- [x] **Step 5/6 성능 최적화 완료** (Step 7 Analytic Expansion 착수 전 병목 해소):
  1. `FootprintCollisionChecker`가 생성 시 `ObstacleInflator`로 외접원(Bounding Circle) 반경만큼 부풀린 격자를 1회 캐싱하고, 매 충돌검사마다 이 격자를 O(1) 조회해 "외접원조차 장애물과 안 겹침"이 확정되면 정밀 Footprint 래스터화(2차)를 생략(Early-Out)하도록 변경. `ObstacleInflator`는 이제 휴리스틱 전용이 아니라 heuristic Distance Map + collision Early-Out 양쪽에서 재사용되는 공용 유틸리티로 문서 갱신
  2. `StateDiscretizer`를 `Dictionary<(int,int,int),double>` → 맵 크기·해상도로 크기가 고정된 1차원 `double[]` Direct Look-up Table로 교체(해시 연산 제거, flat index 배열 접근만 사용). 배열 크기가 5,000만 셀을 넘으면(과도하게 작은 GridResolution/HeadingResolutionDeg) 예외로 안내
  3. `MotionPrimitiveGenerator.Generate()`가 매 호출마다 `List<MotionPrimitive>`를 새로 할당하던 것을 내부 재사용 버퍼 + `ReadOnlySpan<MotionPrimitive>` 반환으로 변경해 탐색 루프의 GC 압력 제거(discretizer의 `TryUpdate` 게이트로 열등한 후보는 이미 HybridState 생성 전에 컷팅되던 기존 로직은 유지)
  4. (검증 후 제거된 임시 점검 코드) `Program.cs`에 `RunResolutionSweep()`을 임시로 추가해 동일 맵(`map1_corridor.png`)에서 GridResolution/HeadingResolutionDeg 조합(4px/15°, 8px/15°, 4px/30°, 8px/30°)별 확장노드수·소요시간·경로비용을 비교 측정한 뒤 삭제

  **실측 결과(map1_corridor.png, 동일 파라미터 4px/15°)**: 최적화 전 590,192노드/24.0s → 최적화 후 590,192노드/2.7s (노드 수 동일 = 탐색 결과·정확성 불변, 노드당 처리 속도만 약 9배 개선). 해상도 스윕: 8px/15°는 79,757노드/0.36s(비용 2434px), 4px/30°는 274,608노드/1.14s(비용 2416px), 8px/30°는 74,953노드/0.29s(비용 2546px) — 해상도를 낮출수록 빨라지지만 경로 비용(품질)이 약간 나빠지는 트레이드오프 확인. 최종 기본값 튜닝은 10단계에서 결정
- [ ] 9~10단계: `CliRunner.cs`(maps/ 전체 순회), 파라미터 최종 튜닝
