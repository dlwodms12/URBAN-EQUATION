# Phase 3-J: 실제 데이터와 Stage 1/2 통합

기준: 수정 기획서 33페이지, 건물 표 13~14페이지와 Stage 표 31~33페이지.
3-I의 전체195개 테스트·기존 Prototype 정상 동작은 사용자가 확인했습니다.
이번 변경은 실제 데이터와 코드 연결이며 최종 씬·UI는 Phase4입니다.

## 파일과 책임

- 신규 `Data/Buildings`: BuildingData15개 및 BuildingCatalog. 비용은 양수 소비량, 획득은 양수 증가량입니다.
- 신규 `Data/Tiles`: Asphalt/Concrete/Grass TileData3개. 논리 종류는 구분하지만 외형은 기존 Tile 프리팹을 공유합니다.
- 신규 `Data/Stages`: Stage01/02와 순서가 확정된 StageCatalog.
- 신규 `Data/GameContent.asset`, `Scripts/Data/GameContentData.cs`: 건물·콤보·스테이지·루트 프리팹의 단일 진입점 및 참조 검증.
- 신규 `Scripts/Core/GameBootstrap.cs`: 명시적으로 전달된 content와 저장 경로로 세션을 생성합니다. 컴포넌트 추가만으로 초기화/파일 접근을 하지 않습니다. 초기화 실패 시 후보 객체를 정리합니다.
- 신규 `Editor/UrbanEquationDebugMenu.cs`: 메뉴에서 검증 후 수정 씬 저장 확인을 거쳐 임시 씬을 만들고 Play에 진입합니다. 씬 asset이나 Build Settings를 변경하지 않습니다.
- 신규 `Tests/Editor/StageIntegrationContractTests.cs`: 실제 asset을 불러오는 48개 테스트.
- 수정 `GameSessionManager.cs`: Stage 연결 시 ownerSession도 연결하여 첫 건설 전 입력 게이트가 적용됩니다.
- 수정 `GameFlowDebugUI.cs`: 선택 좌표 건설과 콤보 순차 표시 버튼. 수동 좌표 모드를 켜지 않은 기존 연결은 빈 타일 자동 선택을 유지합니다.
- 수정 `Docs/DEVELOPMENT_ROADMAP.md`: 3-I 검증 완료, 3-J 검증 대기 및 정확한 테스트 목록.
- 기존 Prototype 씬·프리팹·ComboDatabase30행·테스트195개 및 Assets/ScriptableObjects는 유지합니다. 삭제/이동은 없습니다.

건설/콤보/Undo/목표/저장/화면 판단 책임은 기존 관리자에 그대로 있습니다. Bootstrap은 연결과 초기화만 담당합니다.
실제 Stage에 쓰이는 소형 주택/소형 사무실/식당/소형 공장/등대는 기존 pack 외형을 참조합니다.
미사용10종의 외형과 모든 카드 Sprite는 Phase4에서 연결합니다. 현재 이미지 파일은 Sprite로 import되어 있지 않아 카드 필드에 연결하지 않았습니다.
최종 타일 외형·카드 이미지·모델 대조는 Phase4 범위입니다. 기획서의 대형 사무실 모델명/이미지 대응은 연결 전 확인해야 합니다.

## Unity 확인 순서

1. Unity를 종료하고 `codex/phase3j-stage-data` 최신 버전을 받은 뒤 다시 실행합니다.
2. Console 컴파일 오류 없음, Test Runner 필터 해제, EditMode Run All **243개**를 확인합니다. 신규 클래스는 `StageIntegrationContractTests` **48개**입니다.
3. 기존 Prototype 건설·자원·복수 콤보·클리어·Reset을 확인합니다.
4. `Tools > Urban Equation > Stage 1-2 Debug`를 실행합니다. 현재 씬에 수정이 있다면 Unity 저장 확인창이 먼저 표시됩니다. Game 탭에서 Play → 안내 OK → 타일 좌표 버튼 → 건물 카드 버튼 순으로 건설합니다.
5. 필수 목표를 만족해도 건설을 계속할 수 있습니다. NEXT STAGE는 클리어 확정, Clear 화면의 Next Stage는 다음 단계 이동입니다.
6. Stage1에서 두 건설 후 Undo, 다시 건설, NEXT STAGE·Retry를 확인합니다. Stage2를 완료하고 Stop 후 메뉴 재실행 → Continue → Stage Select → Stage2가 처음부터 시작되는지 확인합니다.
7. Pause 취소는 현재 진행 보존, Pause 확정은 Lobby로 복귀, Play → 새로 시작하기는 진행도 초기화를 확인합니다.

좌표는 (x,y), y가 커지는 쪽이 북쪽 +Z입니다. 디버그 좌표 버튼의 위쪽 행이 북쪽입니다.
콤보는 `다음 콤보 표시` 버튼으로 순차 확인합니다. Stage2는 모두 다른 소형 건물이므로 현 콤보 표에 해당 쌍이 없고, 인접 배치는 추가 목표를 달성합니다.

## 재현 가능한 랭크3 배치

Stage1: 소형 주택 카드1 (0,0) → 소형 주택 카드2 (1,0).
Jobs2→1→0, Population0→1→3(건설+1, 같은 주택 인접 콤보+1). 대각선 (1,1)에 두 번째 주택을 지으면 랭크2, 한 채만 건설하고 완료하면 랭크1입니다.

| Stage2 순서 A | 카드 ID | 좌표 |
|---|---:|---|
| 소형 주택 | 1 | (0,0) |
| 등대 | 5 | (0,1) |
| 소형 공장 | 4 | (0,2) |
| 식당 | 3 | (1,0) |
| 소형 사무실 | 2 | (1,2) |

| Stage2 순서 B | 카드 ID | 좌표 |
|---|---:|---|
| 식당 | 3 | (0,0) |
| 소형 사무실 | 2 | (0,1) |
| 소형 주택 | 1 | (1,0) |
| 등대 | 5 | (0,2) |
| 소형 공장 | 4 | (1,1) |

두 순서 모두 모든 비용을 지불하며 주택-식당, 공장-사무실 인접을 만족합니다.
Stage2 완료 자원 순서 (Population,Jobs,Money,Logistics,Tourism)는 (0,1,0,1,0)입니다.

## 저장과 검증 범위

디버그 파일은 `Application.temporaryCachePath/UrbanEquationDebug/phase3j-progress.json`입니다.
디버그 실행 시 기존 디버그 진행도를 읽고, 첫 Play/새 게임 확정/클리어 확정에서 기존 SaveManager 규칙대로 기록합니다.
사용자 기본 진행도 경로를 사용하지 않습니다. 캐시가 지워지면 디버그 진행도도 사라질 수 있습니다.
테스트는 GUID 기반 OS 임시 폴더만 사용하고 종료 시 정리하며 프로젝트 asset을 수정하지 않습니다.
Exit는 기존 종료 요청 이벤트까지만 확인합니다. Unity에서 실행 종료는 Stop으로 합니다. Application.Quit와 Lobby/Game 실제 씬 로더는 Phase4에서 연결합니다.

AI 검증: 15종 수치·asset YAML/GUID/참조·기존 파일 보존·243개 테스트 선언 집계·독립 Stage1/2 비용/랭크 해답 계산·Git 반영 대조.
Unity/C# 컴파일러가 없는 환경이므로 실제 컴파일/테스트 통과/Play 결과는 사용자 검증 대기입니다.
