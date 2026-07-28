# Step 10 성능/메모리/알고리즘 정확도 전수 검증·최적화 및 직진 구간 헤딩 지그재그 수정

## 사용 도구
Claude

## 목적
로드맵 10단계(튜닝/마무리)에 진입하기 전에, Step 5~6에서 이미 1차 최적화(Footprint 충돌검사 Early-Out, StateDiscretizer 배열화, MotionPrimitiveGenerator 버퍼 재사용)를 마친 Hybrid A* 구현을 "정밀 프로파일링 수준의 시각"으로 재검증하기 위함. 사용자가 알고리즘 내부 구현 세부사항을 전부 파악하고 있지 않다고 명시했으므로, AI가 스스로 (1) GC/메모리, (2) CPU/알고리즘 탐색 성능, (3) Hybrid A* 정확도·도달성(Admissible/Consistent 여부), (4) .NET 8.0 하이레벨 최적화 기법의 4개 관점에서 결함을 찾아내고, 발견에 그치지 않고 즉시 반영 가능한 리팩토링 코드까지 완결해 제시해야 하는 요구였음. 이후 같은 세션 흐름에서 실제 탐색 결과를 사용자가 관찰하며 "직진 구간에서 헤딩이 좌우로 반복해서 꺾이는" 별도의 알고리즘적 결함을 추가로 보고받아, 원인 진단과 수정까지 이어서 진행함.

## 프롬프트 (원문 그대로)

### 1) Step 10 전수 검증 및 최적화 요청

```text
너는 C# .NET 8.0 성능 최적화 및 로봇 경로 탐색(하이브리드 A*) 알고리즘 최상위 전문가야.
1024*768 맵 이미지 기반의 하이브리드 A* 프로젝트에서 Step 10(튜닝 및 최적화/결함 검증)을 진행하려고 해.

나는 알고리즘 내부 구현 세부사항을 전부 파악하고 있지 않으므로, 네가 정밀 프로파일링 수준의 시각으로 코드를 "스스로 검증하고 최적화 패치"까지 완결된 형태로 제시해야 해.

---

[검증 및 최적화 목표]
다음 4가지 관점에서 현재 프로젝트의 결함을 전수 조사하고 성능 최적화를 진행해 줘.

1. 메모리 누수 및 GC(가비지 컬렉션) 부하 최적화
   - 탐색 루프(Loop) 내부에서 반복적인 `new` 객체 할당이 일어나는지 확인
   - `PriorityOpenSet`, ClosedSet(방문 배열/해시), `StateDiscretizer`에서 불필요한 힙 할당을 `struct`, `ArrayPool<T>`, `Span<T>` 또는 노드 재사용(Node Pool) 방식으로 전환
   - 메모리 누수 위험 요소(이벤트 핸들러 미해제, Unmanaged Resouce, static 참조 유지 등) 검증

2. CPU 및 알고리즘 탐색 성능 최적화
   - `PriorityOpenSet`의 삽입/추출 타임 컴플렉시티 최적화 (Binary Min-Heap 또는 Indexed Priority Queue 적용 여부)
   - `FootprintCollisionChecker`에서 SAT(분리축 정리) 또는 Bounding Box 프리필터링이 최적화되어 있는지 검증
   - `HolonomicObstacleHeuristic` 룩업 테이블 접근 시 배열 인덱싱/경계 검사 오버헤드가 최소화되어 있는지 확인

3. 하이브리드 A* 알고리즘 정확도 및 도달성 결함 검증
   - 연속 공간(Continuous) <-> 이산 공간(Discrete State: x, y, theta) 변환 시 해시 충돌이나 Floating Point 오차로 인한 무한 루프 / 노드 유실 가능성 검증
   - Non-Holonomic Heuristic(Reeds-Shepp/Dubins)과 Holonomic Heuristic 간의 `Math.Max` 상호작용이 Admissible(최적성 보장) 및 Consistent를 유지하는지 확인

4. .NET 8.0 최신 High-Performance C# 기법 적용
   - `[MethodImpl(MethodImplOptions.AggressiveInlining)]` 적용 대상 선별
   - 캐시 미스(Cache Miss)를 줄이기 위한 데이터 구조의 연속성(Data Locality) 확보

---

[요청 사항]
1. [결함 리스트 & 원인 분석]: 코드에서 발견된 성능 병목, 메모리 누수 위험, 알고리즘적 오류를 요약해 줘.
2. [최적화 전/후 비교]: 주요 병목 지점별로 개선 방향(예: Heap Node Pool 도입, Struct 전환 등)을 명확히 설명해 줘.
3. [리팩토링 코드 제공]: 수정된 최적화 코드를 곧바로 프로젝트에 반영할 수 있도록 완전한 코드 형태로 작성해 줘.
4. [주석 규칙]: XML Summary 주석은 기존 규칙대로 한 줄(Inline)로 핵심과 단위(px, deg, ns 등)만 컴팩트하게 작성해 줘.
```

### 2) 프롬프트 기록 요청 (1차)

```text
해당 세션에서 나눈 대화를 /commands/LOG_PROMPT.md를 사용하여 "09_optimization.md"로 제작해줘.
```

### 3) 직진 구간 헤딩 지그재그 결함 보고

```text
경로 탐색 시, 직진 구간에서 직진을 하지 못하고 불필요하게 헤딩을 반복해서 좌우로 꺾는 현상이 있어.
해당 현상에 대한 원인 파악과, 개선을 해줘.
```

### 4) 프롬프트 기록 요청 (2차, 병합 지시)

```text
위 세션의 내용까지 함께 포함해서 09_optimization.md를 수정해줘.
```

## AI 응답 요약

### A. Step 10 전수 검증 및 최적화 (프롬프트 1)

1. **전수 코드 리딩(패치 전 진단)**: 코드 수정 전에 `Planning/`, `Map/`, `IO/`, `App/`, `Visualization/`, `WebServer/` 전체를 직접 읽어(서브에이전트 위임 없이 본인이 직접 검증) 4개 관점별 실제 결함을 확인. 주요 발견:
   - **(GC, Critical)** `HybridAStarPlanner.Search()`의 모션 프리미티브 확장 루프에서 매 확장(최대 `MaxSearchNodes`=1,000,000)마다 `new HybridState(...)`(참조 타입, parent 포인터로 체인)를 힙에 할당 — 최대 수백만~수천만 건/탐색의 Gen0 GC 압박 주범.
   - **(정확도, Critical)** `PlanningPipeline.cs`에서 `HolonomicObstacleHeuristic`에 넘기는 point-robot 반경이 Footprint의 **외접원(대각선) 반경**이었음 — 실제로는 로봇이 정렬해서 통과 가능한 대각선 방향 좁은 통로까지 "도달 불가(h=`double.MaxValue`)"로 오판해 Admissible을 위반, 근접 유도가 실패한 구간에서 탐색이 사실상 무정보(비informed) 상태로 폭증하는 근본 원인으로 특정.
   - **(정확도, Medium)** `NonHolonomicHeuristic`이 Dubins 6-word 중 LSL/RSR **2종만** 계산하고, 나머지 케이스의 폴백식 `straightDistance + turningRadius*headingDiff`가 삼각부등식(어떤 경로든 직선거리보다 짧을 수 없음)을 보장하지 못해 드물게 Admissible 위반 가능.
   - **(GC, Medium)** `Footprint.GetCorners()`가 충돌검사(초당 수백만 회 호출되는 hot path)마다 4-tuple 배열을 힙에 할당.
   - **(결함 아님, 확인)** `PriorityQueue<TElement,TPriority>`는 이미 배열 기반 Binary Min-Heap이라 Enqueue/Dequeue가 이미 O(log n); `StateDiscretizer`는 Dictionary/해시 없이 direct flat-index 배열이라 해시 충돌 자체가 불가능; 탐색 루프는 `MaxSearchNodes`/`MaxSearchSeconds`/`CancellationToken`을 매 반복 체크해 무한 루프 불가능; IO 계층의 모든 `Mat`은 `using`으로 정확히 해제되어 네이티브 메모리 누수 없음; `OccupancyGrid`/`StateDiscretizer`의 배열 인덱싱 순서는 이미 히트 루프의 접근 패턴과 맞아떨어지는 지역성을 갖춤.
2. **(1) GC 패치 — Node Pool(Arena) 도입**: 신규 `Planning/NodePool.cs`에 `readonly struct HybridStateNode`(부모를 참조가 아닌 **int 인덱스**로 연결, `ParentIndex=-1`이 루트)와 이를 담는 `HybridStateNode[]` 기반 `NodePool`(용량 초과 시에만 더블링)을 추가. `HybridState.cs`는 탐색 종료 후 역추적된 경로(최대 수백 개)만 표현하는 경량 `readonly record struct`(X/Y/ThetaRad/IsReverse/SteeringAngleRad)로 축소. `PriorityOpenSet`을 `PriorityQueue<HybridState,double>` → `PriorityQueue<int,double>`(풀 인덱스만 보관)로 교체. `HybridAStarPlanner.Search()`와 `AnalyticExpansion.TryExpand()` 모두 동일 `NodePool`에 `pool.Add(...)`로 append하도록 재배선(둘 다 같은 풀을 공유해 경로 체인이 끊기지 않게 함). `Succeed()`에서만 `ParentIndex`를 따라가며 최종 `List<HybridState>`로 변환.
3. **(2) 정확도 패치 — Holonomic 반경 완화 + NonHolonomicHeuristic 완전한 6-word 이식**: `PlanningPipeline.cs`의 `robotRadius` 계산을 외접원(`sqrt((L/2)²+(W/2)²)`)에서 **내접(최소 축) 절반**(`Math.Min(L,W)/2`)으로 변경 — 정밀 충돌검사(`FootprintCollisionChecker`)가 안전성을 별도로 보장하므로 이 완화로 인한 실제 위험은 없음. `NonHolonomicHeuristic.cs`를 `Planning/Curves/DubinsPath.cs`에 이미 구현돼 있던 LSR/RSL/RLR/LRL 공식을 (배열/리스트 할당 없이 스칼라 합만 계산하도록) 이식해 6-word 전부를 평가하도록 재작성하고, 유효 후보가 하나도 없는 이론상 도달 불가능한 경계 케이스의 폴백을 (Admissible이 보장되지 않던) `straightDistance + turningRadius*headingDiff`에서 항상 성립하는 하한인 `straightDistance` 단독으로 교체.
4. **(1) GC 부가 패치 — Footprint 코너 배열 제거**: `Footprint.GetCorners()`를 `(double,double)[]` 반환에서 `Span<(double X,double Y)>` out-파라미터로 변경, 호출부 `FootprintCollisionChecker.IsColliding()`에서 `stackalloc`으로 받도록 수정해 충돌검사 hot path의 배열 힙 할당 제거.
5. **(4) AggressiveInlining — 선별 적용**: 분기 없는 자명한 산술 리프 함수에만 적용(`VehicleKinematics.Move/NormalizeAngle`, `StateDiscretizer.ToFlatIndex/TryUpdate/IsBest`, `OccupancyGrid`의 인덱서·`IsInside/IsOccupied/IsFree`, `NodePool` 인덱서). `NonHolonomicHeuristic`의 6개 Dubins word 함수나 `FootprintCollisionChecker.IsColliding`처럼 분기·루프가 있는 메서드는 강제 인라인 시 오히려 명령어 캐시를 해칠 수 있어 의도적으로 제외했음을 응답에 명시.
6. **빌드 및 자가 검증**: `dotnet build -c Release` 0경고/0오류 확인 후, `Program.cs`에 임시 `--selftest [맵파일명]` 분기(WebServer 기동 없이 `PlanningPipeline.Run`을 직접 호출해 결과를 콘솔 출력)를 추가해 `map1_corridor.png`/`map2_scattered.png`/`map3_rooms.png` 3개 맵을 `ReverseEnabled=false`(기존 기본값)와 `true`(Reeds-Shepp Analytic Expansion 경로까지 실제로 타는지 확인) 양쪽 조건에서 각각 실행 검증. 검증 도중 이전에 실행해 두었던 `PathSearch.exe` 프로세스가 WebServer 포트(8888)를 선점해 재실행이 걸리는 것을 프로세스 목록에서 발견해 종료 후 재시도. 최종 확인 후 `--selftest` 분기와 테스트용으로 토글했던 `data/parameter.json`의 `ReverseEnabled: true`를 모두 원상 복구(→ 실제로는 복구에 실패했음, 프롬프트 3에서 재발견·정정. 아래 B 및 참고 참조).
7. **실측 결과(map1_corridor.png, 기존과 동일 파라미터 4px/15°)**: 패치 전 590,192 노드 / 2.7s / 경로비용 2368px → 패치 후 **39,780 노드 / 0.20s / 경로비용 2429px** — 노드 수 약 15배, 소요 시간 약 13배 감소(경로 비용은 +2.6%로 사실상 동등한 트레이드오프). 개선폭의 대부분이 Node Pool(GC)이 아니라 Holonomic 휴리스틱 반경 완화(Admissible 위반 해소)에서 나왔음을 실측으로 확인·보고. `ReverseEnabled=true`로도 3개 맵 전부 성공, `AnalyticExpansionUsed=True`로 Reeds-Shepp 경로가 실제로 동작함을 확인.
8. **후속 권장(이번엔 미적용)**: `NonHolonomicHeuristic`을 `ReedsSheppPath`/`DubinsPath` 클래스 자체를 재사용하지 않고 별도로 alloc-free 이식한 이유를 명시 — 두 클래스는 `List<Candidate>`/`char[]`/`CurveSegment[]`를 매 호출마다 할당해 Node Pool 최적화를 무력화하기 때문. 두 곳의 수학이 이제 완전히 동일하므로, 향후 `ReedsSheppPath`/`DubinsPath` 자체를 alloc-free API로 통합하는 리팩토링을 권장 사항으로만 남기고 이번 패치 범위에서는 제외.
9. **프롬프트 기록 1차 (프롬프트 2)**: `LOG_PROMPT` 스킬을 실행해 `prompts/08_visualization.md`와 동일한 확장 양식으로 이 시점까지의 세션을 `prompts/09_optimization.md`로 저장.

### B. 직진 구간 헤딩 지그재그 원인 진단 및 수정 (프롬프트 3)

1. **원인 진단**: `HybridAStarPlanner.Search()`의 `moveCost` 계산(`StepSize` + 후진/방향전환 페널티)이 **조향각 자체와 완전히 무관**함을 코드에서 직접 확인 — 직진 프리미티브와 최대 조향 프리미티브의 `g` 비용이 정확히 동일. 여기에 `HolonomicObstacleHeuristic`이 heading을 전혀 고려하지 않는 point-robot 거리라는 점(A 세션의 검증 항목)까지 겹쳐, 직선 구간에서 `f=g+h`가 사실상 동률로 남는 것으로 특정. 동률은 `StateDiscretizer.TryUpdate`가 discretized 셀을 먼저 선점한 후보를 그대로 채택하는 구조라, 우연히 지그재그 프리미티브가 먼저 셀을 차지하면 그것이 그대로 경로에 남는다는 점을 근본 원인으로 지목.
2. **수정**: `Parameter/Parameters.cs`의 `SearchParameters`에 `SteeringChangePenalty`(단위: px/rad, 기본값 8.0)를 신규 추가. `HybridAStarPlanner.Search()`의 모션 프리미티브 루프에서 `steeringChange = Math.Abs(primitive.SteeringAngleRad - current.SteeringAngleRad)`를 계산해 `moveCost += steeringChange * SteeringChangePenalty`로 반영 — 직전 스텝과 같은 조향각을 유지(직진 또는 일정한 회전 유지)하면 추가 비용이 0이고, 좌우로 조향각이 바뀔 때만 비용이 붙어 동률을 깨도록 함. `g` 비용에만 항을 더하는 변경이라 A 세션에서 검증한 휴리스틱 Admissible/Consistent 성질에는 영향 없음(실제 비용이 늘거나 그대로인 방향으로만 바뀌므로 기존 h는 여전히 하한으로 유효)을 근거와 함께 명시. `data/parameter.json`, FE `src_front/src/models/PlannerConfig.ts`(`SearchConfig.steeringChangePenalty` 필드), `src_front/src/components/ParameterPanel.vue`(폼 기본값·입력 필드·라벨)에도 일관되게 반영해 웹 대시보드에서도 실시간 조정 가능하도록 함.
3. **검증(임시 계측 코드)**: `Program.cs`에 임시로 `--selftest` 분기를 다시 추가하되 이번엔 경로의 연속된 heading 변화량 부호가 몇 번 반전되는지("HeadingSignFlips", 지그재그의 직접적인 정량 지표)까지 계산해 콘솔에 출력하도록 확장. `SteeringChangePenalty=8`(적용 후)과 `0`(적용 전, `data/parameter.json`을 임시로 되돌려 비교)을 각각 3개 맵에 대해 실행 비교.
4. **실측 결과**: 헤딩 방향 전환 횟수가 map1 115→**5**, map2 63→**3**, map3 47→**3**로 약 95% 감소. 경로 비용은 2~4% 증가하는 선에서 그침(부드러운 경로를 위한 합리적 트레이드오프로 판단). 이 비교를 처음엔 실수로 `ReverseEnabled=true` 상태(직전 세션에서 복구되지 않은 채 남아있던 값)에서 수행했음을 뒤늦게 인지하고, 실제 배포 기본값인 `ReverseEnabled=false` 조건으로 별도 재검증까지 완료(map1: 45,030 노드/0.21s/비용 2467px/HeadingSignFlips=5, map2: 540 노드/0.01s/1177px/3, map3: 5,640 노드/0.04s/1400px/3).
5. **부수적으로 발견한 프로세스 오류와 정정**: 재검증 과정에서 `data/parameter.json`의 `ReverseEnabled`가 A 세션 종료 시점에 `true`로 남아있던 것을 발견 — A 세션에서 `mv`로 백업본을 복구한 뒤 `git status`에 해당 파일 변경이 나타나지 않는 것을 근거로 "복구 확인됨"이라 기록했으나, 이 파일이 `.gitignore`(9번째 줄 `parameter.json`) 대상이라 애초에 git이 추적하지 않으므로 그 근거 자체가 무효였음을 인지. 파일을 직접 Read해 `ReverseEnabled: false`로 재복구하고, A 섹션 하단의 잘못된 기록도 정정. 임시로 추가했던 `--selftest`/계측 코드는 두 세션 모두 최종적으로 `Program.cs`에서 제거해 원상 복구.
6. **프롬프트 기록 병합 (프롬프트 4, 현재 작업)**: 별도 파일(`10_heading_zigzag.md`)로 분리하지 않고, 사용자 지시에 따라 A(Step 10 최적화)와 B(헤딩 지그재그 수정) 세션 내용을 `prompts/09_optimization.md` 한 파일에 병합 — 제목/목적/프롬프트 목록/AI 응답 요약/사용자 피드백/참고를 전부 두 세션을 아우르도록 갱신.

## 사용자 피드백
- **A(Step 10 최적화) 세션**: 단일 대형 요청 → AI의 자가 검증(코드 리딩 → 패치 → 빌드 → 3개 맵 양방향 시나리오 실행 검증) → 결과 보고의 단발 흐름으로 진행되어, 이전 세션들([[08_visualization]] 등)에서 반복됐던 "구현 → 실제 확인 → 결함 리포트 → 재수정"의 다회 피드백 루프는 발생하지 않았음. 다만 요청 자체에 "네가 스스로 검증하고 최적화 패치까지 완결된 형태로 제시"라는 명시적 지침이 있었으므로, 결함을 나열만 하고 끝내지 않고 실제 코드 수정 → 빌드 → 3개 맵 실측까지 스스로 완료한 뒤 보고하는 것 자체가 요구된 작업 방식이었음.
- **B(헤딩 지그재그) 세션**: A 세션에서 이미 반영한 최적화 패치를 실제로 사용해 본 뒤에 나온 구체적 결함 리포트("직진 구간에서 좌우로 반복해서 꺾인다")였다는 점에서, 이번 세션은 08번 로그에서 관찰됐던 "구현 → 실제 확인 → 결함 리포트 → 근본 원인 수정"의 반복 루프가 다시 나타난 사례임. 결함 설명이 "이렇게 하지 마라"는 구체적 반례 없이 증상만 간결하게 주어졌으므로(원인 파악은 전적으로 AI에게 위임), 추측성 수정 대신 실제 비용 함수 코드를 먼저 읽어 근거를 확보한 뒤 패치하고, 수정 전/후 정량 지표(HeadingSignFlips)로 실측 검증까지 마친 뒤 보고하는 방식을 유지함.
- **가장 중요한 자기 교정**: A 세션에서 "gitignore 대상 파일의 복구 여부를 `git status` 부재로 확인했다"고 잘못 기록한 것을 B 세션에서 실제 파일을 다시 Read하다가 스스로 발견 — 사용자의 직접적인 지적 없이 자체 검증 과정에서 나온 교정이지만, 향후 유사 상황(추적되지 않는 설정/데이터 파일의 상태 확인)에서 반드시 파일을 직접 읽어 확인해야 한다는 원칙을 세운 계기.

## 참고
- 본 세션(A+B)은 로드맵 Step 5~6(Analytic Expansion 이전 성능 최적화, [[07_analytic_expansion]] 참고)에서 이미 진행된 1차 최적화 위에서 진행된 **Step 10(튜닝/최종 검증)** 단계 작업이며, B는 그 연장선에서 발견된 실사용 결함 수정임.
- 자가 검증 중 생성된 `results/result_map{1,2,3}_*.png`는 도구가 정상적으로 산출하는 결과물이라 별도로 삭제하지 않고 그대로 두었음(코드 변경 범위 아님).
- **gitignore 대상 파일 상태 확인 원칙**: `data/parameter.json`은 `.gitignore`(9번째 줄 `parameter.json`)에 포함돼 git이 추적하지 않으므로, `git status`/`git diff`에 아무것도 나타나지 않는 것은 "복구됨"의 증거가 될 수 없다. A 세션에서 이 오류로 `ReverseEnabled: true`가 그대로 남았던 것을 B 세션에서 파일을 직접 Read해 발견·수정했음 — 이후 유사한 gitignore 대상 설정 파일을 다룰 때는 반드시 파일 내용을 직접 읽어 상태를 확인해야 함.
- **Admissible/Consistent 검증과 실제 비용 함수 변경의 분리**: B에서 추가한 `SteeringChangePenalty`는 `g`(실제 누적 비용)에만 항을 더하는 변경이며, `h`(휴리스틱)는 전혀 건드리지 않았으므로 A 세션에서 검증·수정한 Admissible/Consistent 결론에 영향이 없음 — 다만 두 세션의 코드가 함께 작동하는 최종 상태 기준으로 이 성질이 성립함을 문서에 명시해 향후 참조 시 재검증 부담을 줄임.
- 두 세션에서 최종적으로 변경된 파일: `src/Planning/NodePool.cs`(신규), `src/Planning/HybridState.cs`, `src/Planning/PriorityOpenSet.cs`, `src/Planning/HybridAStarPlanner.cs`(Node Pool 재배선 + `SteeringChangePenalty` 반영 2단계 수정), `src/Planning/AnalyticExpansion.cs`, `src/Planning/Kinematics/Footprint.cs`, `src/Planning/Collision/FootprintCollisionChecker.cs`, `src/Planning/Heuristics/NonHolonomicHeuristic.cs`, `src/Planning/Kinematics/VehicleKinematics.cs`, `src/Planning/StateDiscretizer.cs`, `src/Map/OccupancyGrid.cs`, `src/App/PlanningPipeline.cs`, `src/Parameter/Parameters.cs`(`SteeringChangePenalty` 추가), `src_front/src/models/PlannerConfig.ts`, `src_front/src/components/ParameterPanel.vue`. `data/parameter.json`은 gitignore 대상이라 위 목록과 별개로 로컬 데이터로만 갱신됨(`SteeringChangePenalty: 8` 추가, `ReverseEnabled: false` 최종 확인). 커밋은 두 세션 모두 수행하지 않았음(사용자 확인 후 별도 요청 필요).
