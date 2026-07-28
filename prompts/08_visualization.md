# Step 8 Vue 3 인터랙티브 시각화 대시보드 구축 및 반복적 UI/UX 리팩토링(Vuetify 레이아웃, Toast)

## 사용 도구
Claude

## 목적
Step 0~7(설정/파라미터, 이미지 I/O, 맵 모델, 로봇 운동학, Hybrid A* 본 탐색, 이중 휴리스틱, Analytic Expansion)까지 완료된 CLI 전용 구조를, 로드맵 8단계에 맞춰 C# Kestrel WebServer + Vue 3 프론트엔드로 확장하기 위함. 백엔드는 FE 호출에 한해서만 경로 탐색이 동작하도록(CLI 자가 검증 코드 제거) REST API를 노출하고, 프론트엔드는 맵 시각화·경로 탐색 제어·파라미터 실시간 편집·결과 다운로드를 갖춘 대시보드로 구현한다. 이후 여러 차례에 걸쳐 실제 렌더링 결과(레이아웃 깨짐, 스크롤 발생, 폰트 크기, 캔버스 잘림, Toast 위치 등)를 확인하며 발견된 문제를 순차적으로 개선한 세션.

## 프롬프트 (원문 그대로)

### 1) C# 백엔드 WebServer & ApiController 구현 요청

```text
너는 C# .NET 8.0 웹 API 서버 및 Vue 3 프론트엔드 연동 전문가야.
우리는 1024*768 맵 기반 하이브리드 A* 로봇 경로 탐색 프로젝트를 진행 중이야.

[프로젝트 현황]
- Step 0 ~ Step 7 완료 (Kinematics, Footprint, 2D Heuristic Map, Hybrid A*, Analytic Expansion 모두 완성).
- 기존 CLI 구조에서 C# .NET Kestrel 기반 WebServer + Vue 연동 구조로 아키텍처를 확장할 예정.

[이번 단계 작업: Step 8 - Vue 3 기반 인터랙티브 경로 탐색 시각화 웹 애플리케이션 구축]

---

### 1. C# 백엔드 WebServer & API Controller 구현 (`/WebServer` 폴더)

`wwwroot` 정적 파일 제공 및 REST API 연동을 위한 백엔드를 구현해 줘.

1. **`appsettings.json` 설정**
   - 웹 서버 기본 포트를 `8888`로 관리할 수 있도록 JSON 설정 추가.

2. **`WebServer/WebServer.cs` (.net Kestrel 싱글톤 웹 서버)**
   - `appsettings.json`에서 포트를 읽어와 Kestrel을 바인딩 (Default: 8888).
   - Response Compression (Gzip, Brotli) 및 캐시 방지(No-Cache) 추가 필요.
   - 예외 발생 시 프론트엔드가 원인을 인지할 수 있도록 500 JSON Error Response 처리 포함.
   - `UseStaticFiles` 및 Vue SPA 라우팅 지원을 위한 `MapFallbackToFile("index.html")` 설정 필요.

3. **`WebServer/ApiController.cs` (경로 탐색 REST API)**
   - `[ApiController]`, `[Route("api")]` 기반의 컨트롤러 작성.

4. WebServer는 내가 Common 폴더에 구현해놓은 TaskBase를 통해 백그라운드 Task로 실행되도록 작성해 줘.
---
```

### 2) RunHybridAStarSelfCheck 제거 및 Vue 3 프론트엔드 아키텍처/16개 세부 기능 요청

```text
RunHybridAStarSelfCheck은 테스트 코드로 이제 필요없으므로, 제거해줘. 앞으로는 FE의 호출에 한해서만 경로 탐색이 동작하게 할거야.

프론트엔드는 모듈화와 유지보수를 위해 반드시 아래의 폴더/컴포넌트 구조를 엄격히 준수하여 작성해 줘.
Front 프로젝트는 /src_front 하위에 구성할거야.

[Vue 3 프론트엔드 아키텍처 및 요구사항]

models/: API 데이터 타입 및 인터페이스/클래스 정의 (Pose, PathNode, PlannerConfig 등)

pages/: 메인 대시보드 및 시각화 화면 페이지 컴포넌트

services/: Axios/Fetch 기반 백엔드 REST API 통신 모듈 (uploadMap, startPlan, stopPlan, fetchConfig, updateConfig 등)

stores/: Pinia 기반 중앙 상태 관리 모듈 (현재 맵 정보, 탐색 상태, parameter.json 설정값, 세션 저장 속도값 관리)

[세부 구현 기능]

1. 1024*768 원본 이미지 선택 및 업로드
2. 이미지 업로드 UI 구성, 업로드 완료 시 Canvas 배경으로 1024*768 맵 렌더링.
3. 경로 탐색 / 탐색 종료 버튼 & 비동기 Task 제어
4. [경로 탐색] 버튼 클릭 시 백엔드 탐색 Task 시작.
5. [탐색 종료] 버튼 클릭 시 진행 중인 백엔드 Task 취소 요청 및 주행 정지.
6. Canvas 로봇 Footprint 주행 가시화
7. 맵 위에 설정된 Footprint 크기(차체 사각형)를 오버레이.
8. 탐색된 경로 점(Path Node)을 따라 차량 Footprint가 실시간으로 전진, 후진(isReverse 반영), 회전(Heading)하며 이동 애니메이션 렌더링.
9. 차량 이동 속도 조절 (웹 세션 저장)
10. UI에 속도 조절 슬라이더/입력창 배치.
11. 설정 값은 Pinia store를 통해 브라우저 sessionStorage에 저장되어 웹 페이지를 새로고침해도 유지되도록 처리.
12. parameter.json 시각화 및 실시간 파라미터 제어
13. parameter.json 내의 설정값들을 조회하여 UI 패널(입력 폼/슬라이더)로 가시화, 프론트엔드에서 수정한 즉시 services/를 통해 백엔드 API로 전송되어 parameter.json 및 내부 탐색 인스턴스 설정에 실시간 적용.
14. 백엔드 탐색 시 로딩 Modal: 백엔드에서 경로 탐색이 진행 중인 동안 화면 중앙에 스피너가 포함된 로딩 Dialog 출력
15. 최종 결과 이미지 다운로드: [결과 이미지 다운로드] 버튼 클릭 시, 원본 이미지 위에 탐색된 최단 경로선이 그려진 최종 PNG 파일 다운로드.
16. 실시간 알림 Toast 출력 : 맵 업로드 완료, 파라미터 수정 완료, 탐색 시작, 탐색 성공/실패, 탐색 취소 등 모든 이벤트와 백엔드 응답 결과를 화면에 실시간 Toast 메시지로 시각화.
```

### 3) 결과 이미지/업로드 제거/경로 clear/Vuetify grid/헤딩 화살표 개선 요청

```text
1. 결과 이미지 다운로드 시, 결과 이미지는 차량이 포함되지 않은 "원본 이미지 + 경로"의 형태여야 함. 
2. 맵 이미지 업로드 기능은 불필요함. 제거 필요. 
3. 맵 변경 시, FE에 그려진 기존 경로는 clear 필요. 
4. 레이아웃은 v-container를 사용하여, png 이미지는 cols="10", 사이드 메뉴는 cols="2"로 구성해줘. 단, 브라우저 최소 너비가 일정 이상 작아지면 줄을 분리해서 표시해줘. 
5. 차량의 헤딩 방향 가시화를 추가해줘. 헤딩 방향 쪽에 화살표 가시화를 추가할거야.
```

### 4) 우측 패널 찌그러짐 → Vuetify App Shell(app-bar/navigation-drawer/main) 전면 리팩토링 요청

```text
현재 경로 탐색 대시보드의 화면 레이아웃이 우측 패널 찌그러짐 현상으로 깨져있어 리팩토링이 필요해.

첨부된 요구사항과 Vuetify 컴포넌트 구조를 적용하여 컴팩트하고 현대적인(Modern & Clean) UI로 개편해 줘.

---

### [UI/UX 리팩토링 핵심 요구사항]

1. **상단 툴바 (Top App Bar: `v-app-bar` density="compact")**
   - 상단 툴바에 주요 액션 버튼과 컨트롤들을 가로로 배치할 것.
   - 구성 요소:
     - 대시보드 타이틀 (PathSearch — Hybrid A*)
     - 맵 선택 / 이미지 업로드 드롭다운 (`v-select` / `v-file-input`)
     - 경로 탐색 / 탐색 중지 버튼 (`v-btn` color="primary", color="error")
     - 주행 속도 조절 슬라이더 (`v-slider` compact)
     - 결과 이미지 다운로드 버튼 (`v-btn` icon 또는 variant="outlined")
     - 파라미터 제어 사이드바 토글 버튼 (오른쪽 끝, 톱니바퀴 아이콘 `v-btn`)

2. **접이식 우측 파라미터 사이드바 (Navigation Drawer: `v-navigation-drawer` location="right")**
   - 기존의 좁아서 깨지던 우측 영역을 `v-navigation-drawer` 접이식 패널로 변경.
   - 기본 폭: `width="360"` ~ `380px` 확보 (텍스트 줄바꿈 방지).
   - `parameter.json` 내의 로봇 Footprint, TurnRadius, StepSize, Penalty 값 등을 아코디언(`v-expansion-panels`) 및 카드(`v-card`) 형태로 그룹화하여 컴팩트하게 배치.

3. **메인 영역 (Main Content: `v-main`) 및 캔버스 뷰**
   - `v-container fluid` 내부에서 1024*768 Canvas가 화면 중앙에 적절한 비율로 꽉 차게 렌더링되도록 flex 레이아웃 구성.
   - 더 이상 `cols="10"`, `cols="2"` 같은 비대칭 억지 비율 grid를 사용하지 말고, Canvas가 메인 영역의 주인이 되도록 구성 (`v-main` 내부 flex-grow 활용).
   - Canvas 외곽에 깔끔한 그림자(`elevation="2"`) 및 둥근 테두리(`rounded-lg`) 디자인 적용.

4. **컴포넌트 스타일링 가이드**
   - Vuetify 3 테마(Dark/Light 모드 호환)를 반영하여 모던하고 현대적인 Industrial HMI 느낌을 줄 것.
   - 모든 버튼과 입력 폼은 `density="compact"`를 사용하여 여백 손실을 줄이고 화면 효율을 높일 것.
   - 텍스트가 버튼 밖으로 튀어나오거나 세로로 찌그러지는 현상이 절대로 없도록 CSS `white-space: nowrap` 및 적절한 패딩 적용.

---

### [출력 요구사항]
- 수정된 Vue 3 메인 페이지 컴포넌트 소스 코드 (`pages/` 또는 `App.vue`)
- Vuetify 3 Layout 구조(`v-app`, `v-app-bar`, `v-navigation-drawer`, `v-main`)가 완벽히 포함된 완성형 template 및 script 코드 작성.
```

### 5) 스크롤 제거/폰트 12px/라벨 이해도/여백 컴팩트화 요청

```text
1. 브라우저에 스크롤이 생기지 않도록 전체 너비/높이  레이아웃을 맞춰줘.
2. 폰트 크기는 최대 12px로 줄여줘.
3. 파라미터의 label은 백엔드의 model 주석을 활용하여 초보자도 쉽게 이해할 수 있도록 변경해줘.
4. 불필요한 paading과 margin은 제거하고, 최소한의 기본 간격으로 깔끔하고 컴팩트하게 레이아웃을 구성해줘.
```

### 6) 배경색/슬라이더 tooltip 위치/캔버스 하단 잘림 수정 요청

```text
1. 배경색은 연한 회색으로 변경
2. 속도 슬라이더의 tooltip은 top이 아니라 bottom에 배치
3. canvas의 하단이 잘리는 현상이 있어서, 원본 이미지가 잘리지 않도록 영역 조정 필요
```

### 7) Toast 위치/z-index 수정 요청

```text
toast 메세지는 우측 하단을 기준으로 쌓여야 하며, app-bar 아래에 겹쳐지지 않도록 z-index는 최상위여야 함.
```

### 8) 프롬프트 기록 요청

```text
해당 세션에서 나눈 대화를 /commands/LOG_PROMPT.md를 사용하여 "08_visualization.md"로 제작해줘.
```

## AI 응답 요약

1. **C# 백엔드 WebServer/ApiController (1번 프롬프트)**: `src/appsettings.json`에 `WebServer:Port`(기본 8888) 추가, `App/AppConfig.cs`에 `WebServerPort` 프로퍼티 추가. `WebServer/WebServer.cs`를 `Common/TaskBase.cs`를 상속하는 싱글톤으로 신규 작성 — `WorkRoutineAsync`에서 `WebApplication`을 빌드해 `RunAsync(ct)`로 실행하고, Gzip/Brotli `ResponseCompression`, 모든 응답에 `Cache-Control: no-store` 미들웨어, `UseExceptionHandler`로 500 JSON 에러 응답, `UseStaticFiles`+`MapFallbackToFile("index.html")`을 구성. `PathSearch.csproj`는 콘솔 SDK를 유지한 채 `<FrameworkReference Include="Microsoft.AspNetCore.App" />`만 추가해 Kestrel/MVC를 사용할 수 있게 함(SDK 전환 없이 self-host 패턴). `WebServer/ApiController.cs`를 `[Route("api")]`로 신규 작성해 `GET /api/maps`, `GET /api/maps/{fileName}`, `POST /api/plan/{fileName}`(동기, `PlanningPipeline.Run` 호출), `GET /api/results/{fileName}`을 구현 — 기존 `Program.cs`의 `RunHybridAStarSelfCheck` 로직을 `App/PlanningPipeline.cs`(신규)로 추출해 CLI 자가 검증과 API 양쪽에서 재사용하도록 리팩토링. `Program.cs`는 `WebServer.Instance.RunAsync(_cancelToken)` 호출을 추가해 기존 자가 검증과 나란히 웹서버가 백그라운드로 뜨도록 연결. 모든 엔드포인트를 curl로 직접 기동해 검증(맵 목록/이미지 서빙/plan 실행/result 이미지/404·경로 조작 방어 확인).
2. **RunHybridAStarSelfCheck 제거 + Vue 3 프론트엔드 전체 구축 (2번 프롬프트)**: 규모와 아키텍처적 의사결정(비동기 취소 방식, config 실시간 반영 방식, 프론트 툴링 선택)이 커서 EnterPlanMode로 전환해 계획을 먼저 세움 — 취소는 별도 상태관리 클래스 없이 ASP.NET Core의 `HttpContext.RequestAborted`(FE의 `AbortController.abort()` → 커넥션 종료 → `CancellationToken` 자동 바인딩)를 그대로 활용하기로 결정. 승인 후: `HybridAStarPlanner.Search`/`PlanningPipeline.Run`에 `CancellationToken` 매개변수 추가(루프마다 `ThrowIfCancellationRequested`), `ParameterLoader.Save` 신규(직렬화 담당 추가), `Program.cs`에 `UpdateParameters` 추가(파라미터 교체+`data/parameter.json` 영속화), `RunHybridAStarSelfCheck` 완전 삭제. `ApiController`에 `Plan` 액션을 `async`+`CancellationToken` 파라미터로 변경(`Task.Run` 오프로딩, 취소 시 499 반환), `GET/PUT /api/config`, `POST /api/maps/upload`(1024x768 검증은 기존 `MapImageLoader.Load` 재사용) 추가.
   프론트엔드는 `/src_front`에 Vite+Vue3+TypeScript+Pinia+Axios로 신규 스캐폴딩: `models/`(Pose, PathNode, PlanResult, PlannerConfig, PlanStatus), `services/`(apiClient, mapService, planService, configService), `stores/`(mapStore, planStore — `animationSpeed`를 `sessionStorage` 키 `pathsearch:animationSpeed`에 영속화, configStore, toastStore), `components/`(UploadPanel, MapCanvas, ControlPanel, ParameterPanel, LoadingModal, ToastContainer — 4개 필수 폴더 외 추가한 것으로 별도 명시), `pages/DashboardPage.vue`. `MapCanvas.vue`는 `requestAnimationFrame` 루프로 `planStore.result.path`를 따라 Footprint 사각형을 전진/후진 색상 구분 + heading 틱과 함께 애니메이션. `vite.config.ts`의 build `outDir`을 `../src/wwwroot`로 지정해 `npm run build` 결과가 Kestrel의 정적 파일 서빙 위치에 바로 들어가도록 연결. `.gitignore`에 `src_front/node_modules/`, `src/wwwroot/` 추가. `dotnet build`+`npm run build`+`dotnet run` 후 curl로 업로드→plan→result 전체 시퀀스와 클라이언트 강제 abort(499) 상황까지 검증.
3. **차량 제외 결과 이미지/업로드 제거/경로 clear/Vuetify grid/헤딩 화살표 (3번 프롬프트)**: 4번 요구사항(맵 선택 드롭다운 vs 업로드)이 직전 프롬프트에서 명시적으로 제거를 지시했던 기능과 다시 언급되어 충돌하므로, 진행 전 AskUserQuestion으로 확인 — 사용자가 "맵 선택 드롭다운만 유지"를 선택해 업로드 기능은 재도입하지 않기로 확정. 이후: `Visualization/PathOverlayRenderer.cs`에서 `Footprint` 매개변수와 사각형 렌더링(`DrawFootprint`)을 완전히 제거해 다운로드용 정적 이미지는 "원본+경로선"만 남도록 축소(FE 애니메이션이 차량 표시를 전담). `ApiController`에서 `POST /api/maps/upload` 삭제 및 관련 usings 정리. FE에서 `uploadMap`/`mapStore.upload`/`UploadPanel.vue` 제거, `MapSelectPanel.vue`(드롭다운 전용)로 대체. 맵 변경 시 경로가 안 지워지던 버그의 근본 원인(`mapStore.selectMap`이 `planStore`를 리셋하지 않음)을 찾아 `selectMap`에서 `usePlanStore().reset()` 호출을 추가(순환 참조 회피를 위해 `extractErrorMessage`를 `mapStore.ts`에서 `services/apiClient.ts`로 이동). `DashboardPage.vue`를 `v-container`/`v-row`/`v-col`(`cols="12" md="10"`/`md="2"`)로 재구성해 좁은 화면에서 자동으로 줄바꿈되도록 처리, Vuetify를 신규 도입(이전엔 미사용)하며 처음엔 그리드 컴포넌트만 선택 등록. `MapCanvas.vue`의 `drawFootprint`에 화살촉(삼각형)이 포함된 heading 화살표(`drawHeadingArrow`)를 추가. 파이썬(PIL)으로 결과 PNG의 픽셀을 직접 검사해 녹색 Footprint 픽셀이 실제로 0개임을 확인, curl로 업로드 엔드포인트 405/취소 200 등도 재검증.
4. **우측 패널 찌그러짐 → Vuetify App Shell 전면 리팩토링 (4번 프롬프트)**: 툴바 스펙에 "이미지 업로드 드롭다운(`v-file-input`)"이 다시 언급되어 3번 프롬프트에서 확정한 결정과 재충돌하므로 다시 AskUserQuestion으로 확인 — "맵 선택 드롭다운만 유지"로 재확정. 컴포넌트가 버튼/셀렉트/슬라이더/카드/아코디언 등 Vuetify 전반으로 확장되므로 `main.ts`를 전체 컴포넌트/디렉티브 등록(`vuetify/components`, `vuetify/directives`) + `@mdi/font` 아이콘 세트로 전환. `DashboardPage.vue`를 `v-app > v-app-bar(compact) + v-navigation-drawer(right, temporary, width 380) + v-main`으로 재작성 — 캔버스는 `v-main` 안에서 중앙 정렬된 `v-sheet(elevation="2" rounded="lg")`로 감쌈. `ControlPanel.vue`(경로탐색/탐색중지 `v-btn`, 속도 `v-slider`, 다운로드 아이콘 버튼, 상태 `v-chip`), `MapSelectPanel.vue`(`v-select`), `ParameterPanel.vue`(`v-expansion-panels`로 Robot/Search 그룹화, `v-text-field`/`v-switch`, blur/toggle 시점에 저장해 매 키입력마다 저장하지 않도록)로 각각 재작성. `npm run build` 타입체크 통과 확인(번들 크기가 커진 점은 트레이드오프로 명시).
5. **스크롤 제거/폰트 12px/라벨 이해도/여백 컴팩트화 (5번 프롬프트)**: `App.vue`에 전역(비-scoped) 스타일로 `html,body{height:100%;overflow:hidden}` 체인 추가, 아이콘을 제외한 모든 텍스트 요소에 `font-size:12px !important` 강제(Vuetify 컴포넌트가 자체 font-size를 명시적으로 갖고 있어 단순 상속으로는 적용되지 않으므로 `!important` 사용을 의도적으로 명시). `Parameter/Parameters.cs`의 XML 주석(예: "차체 전장/높이", "모션 프리미티브 스텝 거리", "Analytic Expansion 시도 간격")을 실제로 읽어 초보자용 한국어 표현으로 재작성하면서 원래 C# 필드명/단위를 괄호로 병기(예: "한 걸음 이동 거리 (StepSize, px)") — 라벨이 길어져 드로어 폭(380px)에서 2열이 부적절해져 파라미터 필드를 1열(`cols="12"`)로 전환. 앱바 gap, ControlPanel gap, 드로어 카드 타이틀, 캔버스 컨테이너, 파라미터 행/열 패딩을 축소.
6. **배경색/슬라이더 tooltip 위치/캔버스 잘림 근본 수정 (6번 프롬프트)**: 배경색은 하드코딩 대신 Vuetify 테마 토큰(`createVuetify({ theme: { themes: { light: { colors: { background: '#eeeeee' } } } } })`)으로 변경해 테마 시스템과 일관되게 처리. 슬라이더 tooltip 위치와 캔버스 잘림 모두, 실제로 설치된 `node_modules/vuetify`의 컴파일된 CSS를 직접 읽어(추측 대신 근거 확보) 정확한 선택자/값을 찾아 수정 — `.v-slider-thumb__label`의 `bottom`/wedge `clip-path`를 반전해 tooltip을 썸 아래로 이동. 캔버스 잘림의 근본 원인은 `.v-application__wrap`이 `min-height:100vh`만 갖고 있어(정의된 `height`가 아님) `.v-main`(`flex:1 0 auto`, shrink 불가)이 자식에게 전달하는 percentage-height 체인이 "definite height" 기준을 갖지 못해 `max-height:100%`가 `none`으로 무시되고, 결과적으로 뷰포트가 좁을 때 캔버스가 넘쳐 `overflow:hidden`에 잘리는 것으로 특정. Vuetify가 `v-main`의 padding에 실제로 쓰는 `--v-layout-top`/`--v-layout-bottom` CSS 변수를 그대로 재사용해 캔버스 컨테이너에 `height: calc(100dvh - var(--v-layout-top) - var(--v-layout-bottom))`을 명시적으로 지정, `.v-application__wrap`도 `100vh`/`100dvh`로 고정해 percentage 체인 전체가 유효한 기준을 갖도록 근본적으로 재설계. 빌드된 CSS 번들을 직접 grep해 두 수정 모두 실제로 반영됐는지 확인.
7. **Toast 위치/z-index 수정 (7번 프롬프트)**: `ToastContainer.vue`를 `top`에서 `bottom`(우측 하단 고정, `flex-direction:column`이 그대로면 최신 토스트가 모서리에 가장 가깝게 쌓임)으로 변경. z-index는 임의의 큰 수 대신, 설치된 Vuetify의 `VSnackbar.css`(Vuetify 자체 토스트 컴포넌트)가 실제로 사용하는 `z-index: 10000`을 근거로 동일하게 맞춰 app-bar/drawer(훨씬 낮은 동적 z-index)보다 항상 위에 오도록 처리. 사용자가 그사이 `ControlPanel.vue`에서 `thumb-label`을 제거하고 항상 보이는 속도 값 텍스트로 대체했음을 확인하고, 그로 인해 무의미해진(매칭 대상이 사라진) 6번 프롬프트의 `.v-slider-thumb__label` 관련 `:deep()` CSS를 함께 정리(죽은 코드 제거).
8. **프롬프트 기록 (8번 프롬프트)**: `/commands/LOG_PROMPT.md`(=`LOG_PROMPT` 스킬)의 프로세스를 그대로 따라 `prompts/` 폴더의 기존 `01~07` 파일과 동일한 확장 양식(사용 도구/목적/프롬프트/AI 응답 요약/사용자 피드백/참고)으로 이번 세션 전체(8개 프롬프트)를 정리해 `prompts/08_visualization.md`로 저장.

## 사용자 피드백
- **모듈화 구조를 엄격하게 지정**: 2번 프롬프트에서 `models/`, `pages/`, `services/`, `stores/` 4개 폴더 역할을 명시적으로 강제 — 이 지침에 따라 `components/`(위 4개와 별개 관심사) 추가는 계획 단계에서 사전에 사용자에게 근거를 밝히고 진행함.
- **동일한 기능 요구가 세션 내에서 두 번 서로 다르게 표현되어 충돌**: 2번 프롬프트에서 이미지 업로드 기능을 명시적으로 요구했으나, 3번 프롬프트에서 "맵 이미지 업로드 기능은 불필요함. 제거 필요"로 명시적으로 뒤집었고, 4번 프롬프트의 툴바 스펙에 "이미지 업로드 드롭다운(`v-file-input`)"이 다시 등장해 재충돌 — 두 번 모두 임의로 판단하지 않고 AskUserQuestion으로 확인했으며, 두 번 다 "맵 선택 드롭다운만 유지"로 일관되게 답변받음. 이는 스펙 문서(사용자가 미리 작성해 붙여넣는 대량의 요구사항)가 실제 최신 결정을 항상 반영하지는 않을 수 있음을 시사 — 이후 유사 상황에서도 이전 세션에서 명시적으로 결정된 사항과 새 스펙이 충돌하면 추측 대신 먼저 확인해야 함.
- **레이아웃/스타일 지시가 "이렇게 하지 마라"는 반례와 함께 오는 경우가 많음**: 4번 프롬프트의 "더 이상 `cols=10`/`cols=2` 같은 비대칭 억지 비율 grid를 사용하지 말고"처럼, 직전에 AI가 구현한 접근을 명시적으로 폐기하고 새 방향을 지정하는 패턴이 반복됨(3→4번, 5→6번 모두 이전 라운드의 결과물을 실제 화면에서 확인한 뒤 구체적인 결함을 짚어 재요청). 즉 이번 세션은 "한 번에 완성"이 아니라 "구현→실제 확인→결함 리포트→근본 원인 수정"의 반복 루프로 진행되었음.
- **외부(사용자/린터) 직접 수정이 세션 중간중간 반영됨**: `MapSelectPanel.vue`(`variant="filled"`), `ControlPanel.vue`(아이콘 prepend, 상태 dot, 속도 값 상시 노출로 재설계, `thumb-size`/`track-size` 조정), `DashboardPage.vue`(app-bar 배경 `#212121`, 캔버스 영역 배경 `#f2f2f2`, 드로어 폭 380→300), `ParameterPanel.vue`(라벨에서 영문 필드명 괄호 병기를 제거하고 한글+단위만 남기는 방향으로 단순화)가 대화 중간에 시스템 알림으로 통지됨 — 이런 외부 변경은 되돌리지 않고 이후 작업의 전제로 그대로 수용해야 한다는 지침을 일관되게 따름(예: 7번 프롬프트에서 사용자가 지운 `thumb-label`을 되살리지 않고, 그로 인해 무의미해진 내 CSS만 정리).
- **근거 없는 CSS/레이아웃 수정에 대한 불신 확인**: 6번 프롬프트("canvas 하단 잘림")는 5번에서 시도한 `height:100%` 체인 수정이 실제로는 불완전했음을 드러낸 버그 리포트였음 — 이를 계기로 이후에는 Vuetify 내부 동작을 추측하지 않고 `node_modules/vuetify`의 실제 컴파일된 CSS를 직접 읽어 근거를 확보한 뒤 수정하는 방식으로 전환(슬라이더 tooltip, 캔버스 높이, toast z-index 모두 동일 원칙 적용).

## 참고
- 본 세션은 이전 대화(Step 0~7, Analytic Expansion 및 Footprint 시각화)를 이어받아 시작됨; Step 7 구현·성능 최적화 기록은 [[07_analytic_expansion]] 참고.
- 로드맵상 Step 8(시각화)은 이번 세션에서 CLI 렌더링(`PathOverlayRenderer`)이 아닌 **웹 대시보드(FE 애니메이션)** 중심으로 사실상 재정의됨 — 정적 결과 이미지는 "원본+경로"로 단순화되고, 실제 "차량 주행 시각화"는 전량 `MapCanvas.vue`의 Canvas 애니메이션으로 이관됨. Step 9(`CliRunner.cs`로 `maps/` 폴더 전체 순회)는 이번 세션에서 오히려 정반대 방향(FE 호출 전용, CLI 자가 검증 제거)으로 진행되어 재검토가 필요할 수 있음.
- 세션 내내 브라우저를 직접 제어할 수 있는 도구가 없어(`claude-in-chrome` 확장 설치를 사용자가 보류) 모든 레이아웃/스타일 변경은 `dotnet build`/`npm run build` 통과, curl API 시퀀스, 빌드된 CSS 번들 grep, PIL 픽셀 검사 등 코드/정적 산출물 수준에서만 검증되었고 실제 브라우저 렌더링으로 눈으로 확인되지는 않음 — 6번 프롬프트(캔버스 잘림)처럼 이전 라운드의 "코드상 맞아 보이는" 수정이 실제로는 틀렸던 사례가 있었으므로, 다음 세션에서 가능하다면 실제 브라우저 확인이 강하게 권장됨.
- 데이터 파일 `data/parameter.json`은 세션 중 여러 차례 API를 통해 값이 바뀌었다가(`FootprintWidth` 20→30 등) 원상 복구되었음 — 해당 파일은 `.gitignore`에 포함되어 있어 버전관리 대상이 아니며, 최종 값 자체는 이번 세션의 작업 범위(코드/레이아웃)에 포함되지 않음.
