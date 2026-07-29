# 하이브리드 A* CLI 프로젝트 초기 아키텍처 설계

## 사용 도구
Claude

## 목적
본격적인 개발에 앞서 1024×768 맵 이미지 기반 하이브리드 A* CLI 탐색 프로그램의 프로젝트 폴더 구조와 모듈별 책임을 먼저 설계하기 위함. 이후 appsettings.json(경로 설정)과 parameter.json(로봇/알고리즘 파라미터)의 책임을 분리하고, 연속 공간 탐색(Kinematics/Footprint 기반 충돌 검사)이라는 하이브리드 A*의 특성을 아키텍처에 명확히 반영하도록 설계를 개정함.

## 프롬프트 (원문 그대로)

### 1) 최초 아키텍처 설계 요청

```text
너는 C# .net 및 로봇 경로 탐색(하이브리드 A*) 알고리즘 전문가야.
나는 경로 탐색 알고리즘에 대한 깊은 지식이 없는 개발자야. 나랑 같이 1024*768 이미지 기반 하이브리드 A* CLI 탐색 프로그램을 만드는 프로젝트를 진행할거야.

프로젝트를 본격적으로 개발하기 전에, 전체적인 프로젝트 폴더 구조와 핵심 아키텍처 및 모듈별 역할을 명확하게 설계하고자 해.

[프로젝트 환경 및 기술 스택]
- C# .net 8.0 콘솔 어플리케이션 
- OpenCvSharp4 (이미지 로드, 이진화, 시각화 오버레이 및 PNG 저장)
- 입력 데이터 위치: ./maps 폴더 내부의 1024*768 맵 이미지들 (시작점과 종료점, 장애물 포함)
- 출력 데이터 위치: ./results 폴더 내에 원본 이미지 기준 경로가 그려진 result_{원본 파일명}.png 저장

[요구 사항]
1. 하이브리드 A* 알고리즘의 관심사 분리를 반영한 효율적인 "프로젝트 폴더 구조"를 보여줘. 
2. 각 폴더 및 객체 클래스 파일이 맡을 "역할과 책임"을 초보자도 이해하기 쉽게 설명해줘.
3. 이 아키텍처를 바탕으로 앞으로 개발을 진행할 "단계별 구현 로드맵"을 정리해줘.

초보자인 내가 전체 구조를 직관적으로 이해할 수 있도록 쉽게 풀어서 설명해 줘.
```

### 2) 파라미터 구조 및 연속 공간 탐색 반영 요청 (아키텍처 개정)

```text
기존에 설계해 준 아키텍처에서 아래와 같이 요구사항을 반영하여 폴더 구조와 모듈 설계를 변경해 줘.

[아키텍처 및 파라미터 구조 변경 요청]

1. 네이밍 및 폴더 구조 변경
   - Configuration/ 폴더 및 관련 파일명을 Parameter/ 로 변경해 줘.
   - 프로젝트 상위에 맵 이미지와 데이터 경로를 관리하는 appsettings.json이 존재하므로, 혼동을 피하기 위해 알고리즘 및 로봇 파라미터 전용 모듈로 분리함.
   - 변경 예시: Configuration/ -> Parameter/, Configuration/Appsettings.cs -> Parameter/Parameters.cs

2. 파라미터 파일(parameter.json) 연동 로직 추가
   - 차체 크기(Footprint), 최소 회전 반지름(Turning Radius), 로봇 파라미터, 하이브리드 A* 탐색 속성(Step Size, Grid Resolution 등)을 정의하는 `parameter.json` 파일을 `data/` 폴더 하위에 둔다.
   - 프로그램 시작 시 `data/parameter.json`을 읽어 C# 객체(`Parameters.cs`)로 변환(Deserialize)하는 로직을 `Parameter/ParameterLoader.cs` (또는 ParameterConverter.cs)에 구현한다.

3. 하이브리드 A* 알고리즘 특성 명확화
   - 단순 2D 격자(Grid) 기반 A*가 아닌, 연속 공간(Continuous Space)에서의 탐색을 반드시 반영해야 함.
   - 로봇의 회전 반지름(Turning Radius), 조향각(Steering Angle), 차량 진행 방향(Heading, θ) 개념이 포함된 Kinematics 모델과 Footprint 기반 충돌 검사 구조를 아키텍처에 반영할 것.

위 변경 사항을 반영해서 [업데이트된 폴더 구조]와 [각 클래스의 역할]을 다시 한눈에 이해하기 쉽게 정리해 줘.
```

### 3) 스캐폴딩 진행 승인

```text
진행해줘.
```

### 4) 아키텍처 문서화 요청 (.claude/CLAUDE.md + LOG_PROMPT 기록)

```text
방금 정의한 아키텍처와 폴더 구조, 파라미터 규칙을 .claude/claude.md 파일용 문서로 정리해 줘. 프로젝트 루트의 .claude/claude.md에 바로 저장할 수 있게 Markdown 컨텍스트 형태로 작성해 줘.
```

### 5) 체크리스트 및 프롬프트 문서 갱신 요청

```text
체크 리스트 갱신 및, step별로 prompt를 갱신하려고 해. 

방금 만든 "/prompts/01_initial_design.md"를 위 내용이 포함된 결과물로 갱신해줘.
```

### 6) Parameters.cs 주석 간결화 요청

```text
아래 C# 파라미터 클래스들의 주석이 너무 길고 설명이 장황해서 코드 가독성이 떨어져.
C# 코드 스타일 규칙을 준수하면서 주석을 깔끔하고 간결하게 다듬어 줘.

[주석 정리 규칙]
1. 각 속성(Property)에 붙은 XML summary 주석을 핵심 개념과 단위(px, deg, count 등)만 포함하여 '한 줄(Inline Summary)' 형태로 압축할 것.
   - 예시: /// <summary>차체 전장 (단위: px)</summary>
2. '사용처'나 '어떻게 계산되는지'에 대한 장황한 상세 설명은 모두 제거하고, 해당 파라미터가 '무엇'인지만 직관적으로 설명할 것.
3. 코드의 개행(줄바꿈)을 줄이고 속성 간 격차를 줄여 전체 클래스 구조가 한 화면에 들어오도록 컴팩트하게 정리할 것.
```

### 7) PathOptions 제거 및 AppConfig(IConfigurationRoot 정적 접근) 구현 요청

```text
appsettings.json 데이터 접근을 위해 PathOptions와 같은 별도의 클래스를 생성하지 않고, C# ConfigurationBuilder를 통해 생성된 IConfigurationRoot 객체를 정적으로 참조하여 직접 접근할 수 있도록 AppConfig 클래스를 구현해 줘.

[요구 조건]
1. appsettings.json, DataDirectory, MapDirectory는 필수 요소이다. 
   - 셋 중 하나라도 누락 시 콘솔에 에러 로그를 출력하고 Program.cs에서 return하여 프로그램을 강제 종료한다.
2. ParameterLoader에서 parameter.json을 로드할 때
   - 파일이 존재하는 경우: JSON을 읽어 파싱 후 `Parameters` 객체 반환
   - 파일이 존재하지 않는 경우: 예외를 던지지 않고 default 생성자로 `new Parameters()` 기본 객체를 생성
```

### 8) CLAUDE.md의 PathOptions 관련 서술 갱신 요청

```text
갱신해줘
```

### 9) 프롬프트 문서 갱신 요청 (본 항목)

```text
위 내용으로 01_initial_design.md를 갱신해줘.
```

## AI 응답 요약

1. **최초 설계**: `IO / Map / Planning / Visualization / App` 계층으로 관심사를 분리한 폴더 구조, 각 클래스 역할 표, 0~10단계 구현 로드맵 제시.
2. **아키텍처 개정**: `Configuration/` → `Parameter/`로 이름 변경, `data/parameter.json` + `Parameter/ParameterLoader.cs` 도입, `appsettings.json`(경로)과 `parameter.json`(로봇/알고리즘 파라미터)의 책임 분리, `Planning/Kinematics/`(VehicleKinematics, Footprint, MotionPrimitiveGenerator)와 `Planning/Collision/FootprintCollisionChecker.cs`를 신설해 연속 공간 탐색 특성을 명시. `Map/ObstacleInflator`는 휴리스틱 전용으로 역할 축소.
3. **스캐폴딩 실행**: `data/parameter.json` 샘플, `src/Parameter/Parameters.cs`(RobotParameters/SearchParameters), `src/Parameter/ParameterLoader.cs` 생성 및 `dotnet build` 성공 확인.
4. **아키텍처 문서화**: 확정 아키텍처/폴더 구조/파라미터 규칙/로드맵을 `.claude/CLAUDE.md`에 프로젝트 컨텍스트 문서로 신규 작성. `LOG_PROMPT` 양식에 따라 `prompts/01_initial_design.md` 최초 작성(위 1~3 프롬프트 기록).
5. **주석 추가 및 0단계 마무리**: `Parameters.cs`의 모든 속성(`FootprintLength`, `FootprintWidth`, `TurningRadius`, `MaxSteeringAngleDeg`, `StepSize`, `GridResolution`, `HeadingResolutionDeg`, `SteeringAngleSamples`, `ReverseEnabled`, `ReversePenalty`, `DirectionChangePenalty`, `AnalyticExpansionInterval`)에 단위(px/deg/count/배율)와 목적을 명시하는 XML 주석 추가. 로드맵 0단계 마무리: `appsettings.json`에 `ResultDirectory` 추가, `App/PathOptions.cs` 신규(appsettings.json 전담 바인딩 로더), `Program.cs`에서 시작 시 `PathOptions.Load` → `ParameterLoader.Load` 순서로 두 설정을 로드하도록 연결(엔트리 클래스명이 `PathSearch.App` 네임스페이스와 충돌해 `App` → `Program`으로 변경). `dotnet build` 및 실행으로 두 설정이 정상 로드됨을 콘솔 출력으로 확인.
6. **문서 갱신**: `.claude/CLAUDE.md` 진행 상황 체크리스트에 0단계 완료 항목 반영. `prompts/01_initial_design.md`를 4~6번 프롬프트와 결과가 포함되도록 갱신.
7. **주석 간결화**: `Parameters.cs`의 모든 XML summary 주석을 `/// <summary>차체 전장 (단위: px)</summary>` 형태의 한 줄 인라인 요약으로 압축하고 사용처/계산 방식 설명을 제거, 속성 간 개행을 줄여 클래스 전체가 한 화면에 들어오도록 컴팩트하게 정리. `dotnet build`로 컴파일 확인.
8. **PathOptions → AppConfig 전환**: `src/App/PathOptions.cs` 삭제, `src/App/AppConfig.cs` 신규 — `ConfigurationBuilder`로 생성한 `IConfigurationRoot`를 정적 필드로 보관하고 `MapDirectory`/`DataDirectory`/`ResultDirectory`를 인덱서로 직접 노출. `Validate(out string error)`로 appsettings.json 파일 존재 및 `MapDirectory`/`DataDirectory` 필수값을 검사, `Program.cs`는 실패 시 에러 로그 출력 후 `return`으로 종료(예외 없음). `ParameterLoader.Load`는 `data/parameter.json`이 없으면 예외 대신 `new Parameters()`로 폴백하도록 변경. `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.Configuration.Json` 패키지(8.0.0) 추가. 정상 실행/appsettings.json 누락/parameter.json 누락 3가지 시나리오를 직접 실행해 동작 확인.
9. **문서 갱신**: `.claude/CLAUDE.md`의 폴더 구조, 파라미터 관리 규칙 표, 로드맵 0단계, 진행 상황 체크리스트에서 `PathOptions` 관련 서술을 모두 `AppConfig` 기준(IConfigurationRoot 정적 접근, `Validate()` 필수값 검증, `ParameterLoader` 폴백 동작, 신규 NuGet 패키지)으로 교체.
10. **프롬프트 문서 갱신**: `prompts/01_initial_design.md`에 7~10번 프롬프트와 결과를 이어서 누적 기록(본 항목).

## 반영 여부 및 이유

### 1) 최초 아키텍처 설계 요청
반영 여부: 그대로 반영
이유: 아키텍처 구조와 로드맵이 이해하기 쉽고 구현에 무리 없다고 판단하여 수정없이 진행함.

### 2) 파라미터 구조 및 연속 공간 탐색 반영 요청 (아키텍처 개정)
반영 여부: 그대로 반영
이유: appsettings.json은 경로 설정, parameter.json은 로봇/알고리즘 파라미터로 역할을 확실히 나누고 싶었던 의도가 그대로 반영되어, Parameter/ 폴더와 data/parameter.json 구조로 정확히 분리함.

### 3) 스캐폴딩 진행 승인
반영 여부: 그대로 반영
이유: 앞서 확정한 설계 그대로 진행해달라고 승인만 한 것이라 별도 요청 사항 없음.

### 4) 아키텍처 문서화 요청 (.claude/CLAUDE.md + LOG_PROMPT 기록)
반영 여부: 그대로 반영
이유: 이미 확정한 아키텍처 내용을 문서로 옮겨 적어달라고 한 요청이라 그대로 반영함.

### 5) 체크리스트 및 프롬프트 문서 갱신 요청
반영 여부: 그대로 반영
이유: 진행 상황 체크리스트와 프롬프트 기록을 요청한 그대로 갱신.

### 6) Parameters.cs 주석 간결화 요청
반영 여부: 그대로 반영
이유: 주석이 길어서 코드가 눈에 잘 안 들어와 한 줄로 압축해달라고 했고, 그대로 반영됨.

### 7) PathOptions 제거 및 AppConfig(IConfigurationRoot 정적 접근) 구현 요청
반영 여부: 그대로 반영
이유: 커스텀 클래스를 새로 만들기보다 표준 라이브러리 기능을 그대로 정적으로 활용하고 싶었고, 요청한 대로 구현됨.

### 8) CLAUDE.md의 PathOptions 관련 서술 갱신 요청
반영 여부: 그대로 반영
이유: 앞선 변경 사항을 문서에 반영해달라는 요청이라 그대로 갱신.

### 9) 프롬프트 문서 갱신 요청 (본 항목)
반영 여부: 그대로 반영
이유: 이 문서 자체를 최신 내용으로 갱신해달라는 요청이라 그대로 반영.
