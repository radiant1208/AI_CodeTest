# Step 7 Analytic Expansion(Reeds-Shepp/Dubins) 구현, Footprint 시각화 추가, 검증용 테스트 코드 정리

## 사용 도구
Claude

## 목적
Step 0~6(설정/파라미터, 이미지 I/O, 맵 모델, 로봇 운동학, Hybrid A* 본 탐색, 이중 휴리스틱)까지 완료되고 Step 5/6 성능 최적화까지 마친 상태에서, 남아있던 로드맵 7단계 — 목표 근처에서 매 스텝 모션 프리미티브 대신 Reeds-Shepp/Dubins 곡선으로 한 번에 연결을 시도해 탐색 시간을 줄이는 **Analytic Expansion** — 을 구현하기 위함. 이어서 결과 이미지에 경로선만으로는 보기 어려운 로봇 실제 크기(Footprint)의 통로 통과 여유를 육안으로 확인할 수 있도록 시각화를 보강하고, 두 기능을 검증하는 과정에서 임시로 추가했던 점검 전용 코드를 세션 마무리 시점에 정리하기 위함.

## 프롬프트 (원문 그대로)

### 1) Step 7 Analytic Expansion 구현 요청

```text
너는 C# .NET 8.0 및 로봇 경로 탐색(하이브리드 A*) 알고리즘 전문가야.
우리는 1024*768 맵 이미지 기반 하이브리드 A* CLI 탐색 프로그램을 개발 중이야.

[프로젝트 현황 및 설정]
1. Step 0 ~ Step 6까지 완료되었으며, 최적화(Direct Lookup, Bounding Circle Early-out 등)가 진행된 상태야.
2. 주석 규칙: XML Summary 주석은 한 줄(Inline)로 핵심과 단위(px, deg 등)만 컴팩트하게 작성.

[지금 진행할 작업: Step 7 - Analytic Expansion (Reeds-Shepp / Dubins Curve 연결)]
하이브리드 A* 탐색 중 목표점(Goal) 부근에서 정밀한 Pose $(x, y, \theta)$로 한 번에 도달하고, 탐색 시간을 획기적으로 줄이기 위한 **Analytic Expansion (해석적 확장)** 기법을 구현해 줘.

[Step 7 핵심 요구사항]

1. **Reeds-Shepp 또는 Dubins Curve Generator 작성**
   - 현재 설정(`data/parameter.json`)의 `ReverseEnabled` 옵션에 따라 곡선 선택:
     - `ReverseEnabled == true` : Reeds-Shepp Curve (전진/후진 조합 최단 곡선)
     - `ReverseEnabled == false`: Dubins Curve (전진 전용 최단 곡선)
   - 최소 회전 반경(`TurningRadius`) 및 현재 Pose $(x_1, y_1, \theta_1) \rightarrow$ Goal Pose $(x_2, y_2, \theta_2)$를 잇는 최적 수학적 곡선 경로와 총 길이(Cost) 계산.

2. **Analytic Expansion 시도 및 충돌 검사 (AnalyticExpansion.cs)**
   - 메인 탐색 루프에서 $N$번째 노드 확장마다(`AnalyticExpansionInterval` 간격), 또는 Goal과의 2D 휴리스틱 거리가 일정 범위 이내일 때 Analytic Expansion 시도.
   - 현재 노드에서 Goal까지 생성된 Analytic Curve를 일정 보간 간격(`StepSize` 이하)으로 샘플링.
   - 샘플링된 모든 보간 점에 대해 `FootprintCollisionChecker`로 정밀 충돌 검사 수행.
   - 중간에 장애물과 하나라도 충돌하면 즉시 중단(Fail)하고 일반 모션 프리미티브 탐색 계속 진행.

3. **탐색 조기 종료 및 경로 합치기 (HybridAStarPlanner.cs 연동)**
   - Analytic Curve 샘플링 지점 전체가 충돌 없이 통과된 경우:
     - 더 이상 OpenSet 탐색을 진행하지 않고 **즉시 탐색 성공(Goal Reached)** 처리.
     - Start부터 현재 노드까지의 백트래킹 경로 + Analytic Curve 보간 경로를 하나로 이어 붙여 최종 경로 구성.

4. **클래스 위치**
   - `src/Planning/AnalyticExpansion.cs` (또는 필요 시 `ReedsSheppPath.cs` / `DubinsPath.cs` Helper 분리)
   - `src/Planning/HybridAStarPlanner.cs` 탐색 루프 내에 Analytic Expansion 호출부 통합.

5. **자가 검증 코드 포함**
   - `Program.cs`에서 실행 시 Analytic Expansion이 발동하여 조기 종료되는지 콘솔 로그(예: `[Analytic Expansion Success] Goal connected!`) 및 총 탐색 시간/방문 노드 수의 감소량을 확인할 수 있는 코드를 제공해 줘.
```

### 2) Footprint 시각화 보강 요청

```text
result에 footprint 크기를 고려해서 경로 위에 함께 시각화 해줘.
```

### 3) 테스트 코드 정리 및 프롬프트 기록 요청

```text
시각화 테스트 코드 제거 후, 해당 세션에서 나눈 대화를 /commands/LOG_PROMPT.md를 사용하여 "07_analytic_expansion.md"로 제작해줘.
```

## AI 응답 요약

1. **Reeds-Shepp/Dubins 곡선 생성기 (1번 프롬프트)**: `src/Planning/Curves/` 하위에 `CurvePathResult.cs`(세그먼트 1개를 `CurveSegment(Motion: 'L'/'R'/'S', SignedLengthPx: +전진/-후진)`로 표현하는 공통 타입), `ReedsSheppPath.cs`, `DubinsPath.cs`를 신규 작성.
   - `ReedsSheppPath`: 반경=1로 정규화한 로컬 좌표계에서 LSL/LSR/LRL 3종 기본해(polar 좌표 기반)를 구하고, timeflip(전진↔후진 부호 반전)·reflect(L↔R 문자 교체) 대칭 변환을 적용해 최대 12종 후보를 만든 뒤 총 길이가 최단인 경로를 선택. 전체 48-word Reeds-Shepp 중 CCCC/CCSC/CCSCC 등 희귀 word는 생략한 실용적 근사이며, 이는 [[06_path_search]]에서 구현한 `NonHolonomicHeuristic`의 LSL/RSR 근사와 동일한 설계 방향임을 주석으로 명시.
   - `DubinsPath`: `NonHolonomicHeuristic`이 이미 쓰던 alpha/beta/d 파라미터화(시작-목표 직선 기준 상대 heading)를 LSL/RSR 2종에서 LSR/RSL/RLR/LRL까지 6종 전체로 확장. Reeds-Shepp의 폴라 기반 공식은 CCC(LRL/RLR) word에서 부호가 항상 음수인 세그먼트가 나올 수 있어(진짜 후진이 아니라 공식상의 기하적 부호) Dubins처럼 순수 전진만 허용해야 하는 경우엔 그대로 재사용할 수 없음을 확인하고, alpha/beta/d 기반의 독립된 forward-only 공식을 별도로 유지하기로 결정. 부동소수점 경계오차로 아주 작은 음수가 나오는 경우까지 배제하는 허용오차 체크를 추가해 Dubins가 절대 후진 세그먼트를 반환하지 않도록 보장.
2. **AnalyticExpansion.cs (2번 요구사항)**: `ReverseEnabled`에 따라 `ReedsSheppPath`/`DubinsPath` 중 하나를 선택해 곡선을 구하고, 세그먼트별로 `StepSize` 이하 간격으로 걸으며 `VehicleKinematics.Move`로 좌표를 갱신하고 매 샘플을 `FootprintCollisionChecker.IsColliding`으로 검사. 하나라도 충돌하면 즉시 `false`를 반환(부분 체인 폐기)하고, 전부 통과하면 `current`를 부모로 잇는 `HybridState` 체인을 만들어 마지막(목표) 노드를 반환. 이동 코스트는 메인 루프와 동일한 모델(세그먼트 길이 × `ReversePenalty`(후진 시) + 방향 전환 시 `DirectionChangePenalty`)을 재사용해 두 경로 방식(모션 프리미티브 vs 곡선 연결)의 비용 산정 기준을 일치시킴.
3. **HybridAStarPlanner 연동 (3번 요구사항)**: Pop한 노드가 목표 허용오차 내가 아니면, `expanded % AnalyticExpansionInterval == 0`(간격 트리거) 또는 목표까지 직선거리가 `TurningRadius × 3` 이내(근접 트리거, 새 파라미터 추가 없이 기존 `TurningRadius`를 재사용)일 때 `AnalyticExpansion.TryExpand()`를 시도하도록 모션 프리미티브 생성 앞에 삽입. 성공 시 그 자리에서 `Succeed()`를 호출해 OpenSet 루프를 종료. `PlanResult`에 `AnalyticExpansionUsed` 플래그를 추가해 목표 도달이 일반 탐색인지 곡선 연결인지 구분 가능하게 함.
4. **자가 검증 (5번 요구사항, 1차)**: `Program.cs`에 Analytic Expansion On/Off 두 번 탐색을 수행해 확장노드수/소요시간 차이를 로그로 출력하는 임시 A/B 비교 코드와, 이를 끄기 위한 `AnalyticExpansion.Enabled` 토글을 추가해 실행. `map1_corridor.png` 기준 Off=590,192노드/2.529s, On=109,500노드/0.516s로 **확장노드수 -81.4%, 소요시간 -2.012s** 감소를 확인하고 `[Analytic Expansion Success] Goal connected!` 로그가 정상 출력됨을 검증.
5. **Footprint 시각화 (2번 프롬프트)**: `Visualization/PathOverlayRenderer.cs`의 `Render()`에 `Footprint` 매개변수를 추가하고, 경로점 8개(`FootprintDrawInterval`)마다 `Footprint.GetCorners(x,y,thetaRad)`로 구한 4꼭짓점을 `Cv2.Polylines`로 녹색 사각형 윤곽으로 그리도록 변경(마지막/목표 노드는 간격에 안 맞아도 항상 포함). 매 노드마다 그리면 사각형이 겹쳐 잘 안 보이므로 간격을 둠. `Program.cs` 호출부에 이미 생성돼 있던 `footprint` 인스턴스를 전달하도록 수정. 재실행 후 `results/result_map1_corridor.png`를 직접 읽어 회전된 녹색 사각형이 경로를 따라 heading에 맞게 그려지고, 통로 회전 구간에서 벽과의 여유가 시각적으로 확인됨을 검증(이때 재검증용 A/B 비교도 함께 재실행되어 Off=510,080노드/3.989s, On=32,130노드/0.336s, **-93.7%** 감소도 재확인 — `data/parameter.json`의 `TurningRadius`가 세션 중 30px→40px로 바뀌어 있어 절대 수치는 1차 검증과 다르지만 감소 추세는 동일하게 재현됨).
6. **테스트 코드 정리 (3번 프롬프트)**: [[06_path_search]]에서 확립된 패턴(기능 검증용으로 임시 추가한 코드만 제거하고, 요구사항에 명시된 정식 자가 검증 코드는 유지)을 그대로 적용. `Program.cs`에서 Step 7 검증 전용으로 추가했던 Analytic Expansion Off/On 2회 탐색·효과(%) 로그 블록을 제거하고 `AnalyticExpansion`을 항상 활성 상태로 한 번만 `Search()`하도록 단순화했고, 그 유일한 소비자였던 `AnalyticExpansion.Enabled` 토글 속성도 함께 제거(죽은 코드 방지). `[Analytic Expansion Success] Goal connected!` 로그와 경로/시각화 저장 로직은 정식 자가 검증 코드이므로 유지. 정리 후 재빌드·재실행해 Success=True, 소요시간=1.189s, 확장노드수=114,460, 경로 318점(비용 2552.91px)으로 정상 동작함을 재확인.

## 반영 여부 및 이유

### 1) Step 7 Analytic Expansion 구현 요청
반영 여부: 그대로 반영
이유: 목표까지 한 번에 연결하는 기능을 요청한 대로 구현함.

### 2) Footprint 시각화 보강 요청
반영 여부: 그대로 반영
이유: 결과 이미지에 로봇 크기도 같이 그려달라고 요청했고, 색상·간격 같은 세부 사항은 기존 그림 스타일(전진 주황/후진 빨강)과 구분되게 알아서 정하도록 맡겼는데 잘 반영됨.

### 3) 테스트 코드 정리 및 프롬프트 기록 요청
반영 여부: 그대로 반영
이유: 지난 세션(06번)처럼 확인용으로 넣은 On/Off 비교 코드는 정리하고 실제 기능에 필요한 코드만 남겨달라고 했고, 그대로 반영됨.
