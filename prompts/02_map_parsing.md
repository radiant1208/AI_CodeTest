# 맵 이미지 파싱(IO) 및 장애물 격자 모델(Map) 구현 — Step 1, 2

## 사용 도구
Claude

## 목적
Step 0(설정/파라미터 로더) 완료 후, 실제 맵 이미지를 OpenCvSharp4로 로드·이진화하여 점유 격자(OccupancyGrid)와 시작/목표 좌표를 추출하는 IO 파이프라인(Step 1)과, 휴리스틱 전용 장애물 팽창 전처리기(ObstacleInflator, Step 2)를 구현하기 위함. 구현 과정에서 발견된 예외 처리 누락·성능·메모리 누수 이슈를 함께 점검하고 개선함.

## 프롬프트 (원문 그대로)

### 1) Step 1 요청: 맵 이미지 로더/파서 구현

```text
너는 C# .NET 8.0 및 로봇 경로 탐색(하이브리드 A*) 알고리즘 전문가야.
우리는 1024*768 맵 이미지 기반 하이브리드 A* CLI 탐색 프로그램을 개발 중이야.

[프로젝트 현황 및 설정]
1. 이미 Step 0(프로젝트 아키텍처 및 설정 로더 구축)은 완료된 상태야.
2. 주요 구조:
   - App/AppConfig.cs (appsettings.json 정적 로드 및 경로 검증)
   - Parameter/Parameters.cs & ParameterLoader.cs (data/parameter.json 로드 및 default fallback)
   - IO/, Planning/, Visualization/ 폴더 구조 준비 완료
3. 주석 규칙: XML Summary 주석은 한 줄(Inline)로 핵심과 단위(px, deg 등)만 컴팩트하게 작성.

[지금 진행할 작업: Step 1]
이제 Step 1 개발을 진행하려고 해. 
OpenCvSharp4를 활용하여 ./maps 폴더 내의 맵 이미지를 로드하고, 이진화(Binary Grid Map) 처리 및 시작점/목표점/장애물 영역을 파싱하는 `IO/MapLoader.cs`를 구현해 줘.

[Step 1 요구사항]
1. OpenCvSharp4를 사용해 1024*768 이미지 파일 로드
2. 맵 이미지를 탐색 가능한 공간과 장애물 공간으로 이진화(Binarization) 처리
3. 시작점(Start)과 목표점(Goal)의 좌표 및 Heading(각도) 정보를 추출 또는 설정할 수 있는 구조 정의
4. 구현에 필요한 C# 코드와 간단한 사용 방법을 작성해 줘.
```

### 2) 코드 품질 검토 요청 (예외 처리 / 병목 / 메모리 누수)

```text
1. 방금 작성해 준 MapLoader.cs 코드에 예외 처리가 누락된 부분이 있어?
2. 이 코드가 하이브리드 A 스타 알고리즘의 연속 공간 탐색 연산 시 병목을 일으킬 만한 요소가 있어?
3. 메모리 누수(OpenCvSharp Mat 객체 해제 등) 위험이 있는 부분이 있다면 수정해 줘.
```

### 3) MapImageLoader 크기 값을 파라미터로 분리 요청

```text
MapImageLoader의 기대 이미지 너비 및 높이는 readonly 대신  Parameter 객체 하위에 Map을 추가하여 기본 값을 각각 1024, 768로 가지도록 파라미터로 분리해줘.
```

### 4) Step 2 진행 요청

```text
step 2 진행해줘.
```

## AI 응답 요약

1. 실제 맵 이미지(map1~3) 픽셀을 직접 샘플링(Python/PIL)해 장애물(~RGB 40,40,40)/이동가능(~RGB 245,245,245)/시작 마커(초록 코어 BGR 48,176,17)/목표 마커(빨강 코어 BGR 27,27,197) 색상을 실측. CLAUDE.md 아키텍처(계층 분리 원칙)에 따라 사용자가 요청한 단일 `MapLoader.cs` 대신 `IO/MapImageLoader.cs`(로드+크기 검증) / `IO/MapImageParser.cs`(이진화+시작·목표 파싱) / `Map/OccupancyGrid.cs`(점유 격자)로 분리 구현.
2. `OpenCvSharp4`, `OpenCvSharp4.runtime.win` NuGet 패키지 추가. `MapImageParser` 최초 구현 시 시작/목표 마커를 단순 BGR box range(`InRange`)로 판별했으나, 실제 맵으로 검증하는 과정에서 장애물-배경 경계의 중립 회색(R=G=B≈120) 안티에일리어싱 픽셀까지 오검출돼 마스크가 이미지 전체로 번지는 버그를 발견 → 채널 우세(dominance, G-R/G-B 차이 임계값) 방식으로 교체해 수정. `Program.cs`에 임시 검증 코드를 넣어 3개 맵 전부에서 시작/목표 좌표가 육안 확인한 위치와 정확히 일치함을 확인 후 원복.
3. 코드 리뷰 요청에 따라 세 가지 이슈를 발견해 수정: (1) `MapImageParser.Parse`에 null/empty/3채널 검증이 없어 잘못된 Mat 입력 시 `IndexOutOfRangeException`이나 채널 누수가 발생할 수 있음 → `ArgumentNullException`/`ArgumentException` 가드 추가, `MapImageLoader.Load`도 경로 null/공백 검증 추가. (2) `OccupancyGrid`의 `bool[,]` 사각 배열이 향후 `FootprintCollisionChecker`/`HolonomicObstacleHeuristic`이 탐색 중 고빈도 호출할 hot path라는 점에서 인덱싱 오버헤드·캐시 지역성 문제 소지 → 내부 저장소를 `bool[]`(1차원, row-major)로 교체(공개 인덱서 시그니처는 유지). (3) `BuildDominanceMask` 내부에서 `Cv2.BitwiseAnd` 실패 시 결과 Mat이 해제되지 않고 새는 경로 발견 → try/catch로 감싸 예외 시 `Dispose` 후 재throw하도록 수정. 세 수정 모두 실제 맵 3종 + 경계 케이스(1채널 Mat, null Mat)로 재검증.
4. `MapImageLoader`의 기대 너비/높이(기존 `const` 1024/768)를 `Parameter/Parameters.cs`에 `MapParameters`(`Width`/`Height`, 기본값 1024/768) 클래스로 신설해 이동, `data/parameter.json`에도 `Map` 섹션 추가. `MapImageLoader.Load(string, MapParameters)` 시그니처로 변경해 하드코딩 제거. 실제 맵으로 재검증해 이전과 동일한 결과 확인.
5. Step 2(`Map/ObstacleInflator`) 구현: 모든 장애물 셀을 동시 시작점으로 하는 다중 시작 BFS(8방향, Chebyshev 거리 근사)로 지정 반경 내 셀을 장애물로 표시하는 `Inflate(OccupancyGrid, double radiusPx)` 구현. `Map/` 계층을 OpenCvSharp 의존성 없는 순수 자료구조로 유지하기 위해 OpenCV 거리 변환 대신 BFS 근사를 선택(CLAUDE.md의 "부정확하지만 빠름, 휴리스틱 전용" 설계 의도와 부합). `radiusPx=0`/음수/`grid==null` 등 경계값 처리 포함. 실제 맵으로 반경 10px/20px 검증: 반경 증가에 따라 점유 픽셀 수 단조 증가, `radius=0`은 원본과 완전 일치, 반경 10px에서도 start/goal은 이동 가능 영역으로 유지됨을 확인, 786,432셀 기준 처리시간 74~165ms.

## 사용자 피드백
- 아키텍처 문서(CLAUDE.md)에 이미 정의된 계층 분리 규칙이 사용자의 개별 요청(단일 파일명 등)보다 우선한다는 판단을 별도 확인 없이 적용해도 이견 없이 수용함.
- 코드를 작성만 하고 끝내는 것이 아니라, 실제 맵 이미지로 직접 실행·검증한 뒤 결과 수치(좌표, 비율, 시간 등)를 제시하는 방식을 선호. 검증용으로 `Program.cs`에 임시로 추가한 코드는 확인 후 원복하는 흐름에 이견 없음.
- "예외 처리/병목/메모리 누수가 있는지" 질문 후 실제로 발견된 이슈에 대해 별도 지시 없이 바로 수정까지 진행하는 방식을 자연스럽게 수용(질문이 곧 수정 요청으로 이어짐).

## 참고
- 확정된 아키텍처는 `.claude/CLAUDE.md`에 별도 유지되며, 본 세션에서 CLAUDE.md 자체는 수정하지 않음.
