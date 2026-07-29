# ObstacleInflator 디버그 검증 테스트 및 Footprint 파라미터 튜닝

## 사용 도구
Claude

## 목적
Step 2에서 구현한 `ObstacleInflator`의 BFS 경계 처리, 성능/GC 부하, 실제 부풀림 정확도를 `Program.cs`에서 직접 실행해 눈으로 검증할 수 있는 `#if DEBUG` 전용 테스트 하네스를 구축하고, Footprint 파라미터를 맵의 시작/목표 마커 크기 및 튜닝 값에 맞춰 조정하면서 그 결과를 재검증하기 위함.

## 프롬프트 (원문 그대로)

### 1) OccupancyGrid/ObstacleInflator 검증 테스트 코드 작성 요청

```text
현재 Map/OccupancyGrid 및 ObstacleInflator 모듈을 검증하려고 해.

[검증 목표]
1. OccupancyGrid가 맵 이미지를 정상적으로 그리드 셀 데이터로 변환했는지 검증
2. Footprint 외접원 반지름 + 안전 마진 기반으로 ObstacleInflator가 장애물 영역을 정확히 부풀리는지 검증
3. 부풀려진 영역과 원본 장애물 영역의 경계 처리 예외 검증

[요구사항]
1. 현재 ObstacleInflator.cs의 BFS 알고리즘에서 맵 경계 처리 시 인덱스 초과나 경계 왜곡 이슈가 없는지 검토해 줘.
2. 부풀리기 계산 성능이 수십 ms 이내로 유의미하게 빠르고 GC 부하가 없는지 확인해 줘.

[테스트 요구 사항]
Program.cs에서 직접 실행하여 눈으로 확인할 수 있는 테스트 코드를 작성해 줘.
- `#if DEBUG` 전처리기를 사용하여 디버그 모드에서만 동작하도록 할 것
- Footprint 크기로 radiusPx을 계산하여 ObstacleInflator.Inflate 호출
- 원본 맵 이미지 위에:
  - 원본 장애물: 검은색 또는 빨간색 표시
  - 부풀려진 장애물 영역(Inflated Zone): 주황색/노란색 투명 오버레이 표시
- 결과를 `./test_output/inflated_map_test.png` 파일로 저장
- 콘솔 출력: 원본 장애물 비율(%), 부풀려진 장애물 비율(%), 처리 소요 시간(ms) 출력
```

## AI 응답 요약

1. `ObstacleInflator.cs`의 BFS 경계 처리를 재검토: 모든 이웃 접근이 `IsInside()` 체크 후에만 배열에 접근해 인덱스 초과 위험 없음, 이미지 경계에서 자연스럽게 clip되는 것도 올바른 동작임을 확인. 다만 `Math.Ceiling(radiusPx)`로 인해 항상 안전 방향으로 최대 1px 미만 더 넓게 부풀려지는 점, 8방향 BFS가 Chebyshev 거리(원이 아닌 사각형 근사) 기반이라는 두 가지 설계 특성을 확인 및 설명(CLAUDE.md의 "부정확하지만 빠름" 의도와 일치).
2. `Program.cs`에 `#if DEBUG` 블록으로 `RunObstacleInflatorDebugTest()` 신규 작성: `maps/` 첫 이미지 로드 → Footprint 외접원 반지름(√((L/2)²+(W/2)²)) + 안전마진(테스트 전용 상수) 계산 → `ObstacleInflator.Inflate` 실행 → 원본 장애물(빨강)/인플레이트 전용 영역(주황 45% 반투명) 오버레이 시각화를 `test_output/inflated_map_test.png`로 저장, 콘솔에 원본/부풀림 비율(%), 소요 시간(ms), `GC.GetAllocatedBytesForCurrentThread` 기반 할당 메모리(KB) 출력. Release 빌드에서는 관련 `using` 포함 전체가 완전히 제외되도록 구성.
3. 최초 측정에서 `Queue<(int,int)>`가 내부 배열을 여러 번 더블링 재할당하며 7.9MB를 할당하는 것을 발견 → BFS 특성상 각 셀은 최단거리 확정 시 단 한 번만 큐에 들어간다는 점을 이용해 `Queue<int>(cellCount)`로 사전 용량을 지정하고 좌표를 튜플 대신 flat index로 저장하도록 `ObstacleInflator.cs` 리팩터링, 할당량을 이론적 최소치(≈6.75MB)까지 감소. 재검증 결과 점유 비율은 동일(정확성 유지), 시각화 이미지에서 각 장애물 주위에 균일한 폭의 주황 버퍼가 사각형 모서리 그대로(Chebyshev 특성) 형성됨을 육안 확인, start/goal 마커도 인플레이트로 침범되지 않음을 확인.
4. 맵1의 시작/목표 마커를 `MapImageParser`와 동일한 색상 판별 로직(채널 우세 마진 60)으로 재스캔해 지름 약 32px(반지름 16px)의 원임을 실측 → `RobotParameters.FootprintLength`/`FootprintWidth` 기본값(및 `data/parameter.json`)을 32.0/32.0으로 설정, 디버그 테스트로 외접원 반지름이 22.63px(=√(16²+16²))로 정확히 반영됨을 재확인.

## 반영 여부 및 이유

### 1) OccupancyGrid/ObstacleInflator 검증 테스트 코드 작성 요청
반영 여부: 그대로 반영
이유: 눈으로 직접 확인할 수 있는 테스트 코드를 `#if DEBUG`로 감싸서 만들어달라고 요청한 대로, 실제 맵 이미지에 장애물/부풀린 영역을 오버레이해서 보여주고 ms·KB 단위 수치까지 출력하도록 반영됨.
