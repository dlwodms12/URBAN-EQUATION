# Phase 3-I — 게임 흐름 / 화면 상태

기준: 최신 33페이지 기획서의 Lobby·확인창·Stage Select·진입 안내·Clear·Pause 화면(20~23, 28~29쪽).
3-H 검증을 확인하신 사용자 요청에 따라 진행했고, 중단 후 작업 폴더와 GitHub를 대조해 재개했습니다.

브랜치: `codex/phase3i-game-flow`.
기준: `codex/phase3h-progress-save`, `b27fd7011472ef0ee624874cad1ea2e2c29028b0`.

## 구현 범위

`SceneFlowManager`가 현재 화면과 사용 가능한 명령을 관리합니다. 화면 변경 알림 전에 건설·드래그·Undo·클리어 확정 입력을 차단하거나 활성화합니다.
`GameFlowScene` / `OnSceneRequested`는 Lobby/Game 목적지를 알립니다. Stage Select는 Lobby 목적지이며 안내·게임·Pause·Clear는 Game 목적지입니다.
이번 단계에서는 실제 `SceneManager.LoadScene`을 호출하거나 Lobby/Game 씬을 생성하지 않습니다. 실제 씬 로더와 최종 UI 연결은 Phase 4입니다.
`GameFlowDebugUI`는 명시적으로 연결할 수 있는 임시 화면/버튼을 제공하며, 자체적으로 저장 파일 접근이나 설정을 시작하지 않습니다.

| 화면/명령 | 동작 |
|---|---|
| Lobby / Play, 저장 없음 | 새 진행도를 저장한 뒤 Stage 1 안내 진입 |
| Lobby / Play, 기존 또는 손상된 저장 있음 | New Game 확인창만 표시; 아직 기존 파일을 교체하지 않음 |
| 확인 / 새로 시작하기 | Stage 1 데이터 사전 검증 후 저장 초기화 및 Stage 1 안내 진입 |
| Lobby 또는 확인 / Continue | 유효 저장이 있을 때 Stage Select 진입 |
| Stage Select | 해금된 1~N 단계와 최고 랭크 표시; 선택한 단계 처음부터 진입 |
| Stage Intro / OK | 목표 안내를 닫고 Playing 입력 활성화 |
| Playing / NEXT STAGE | 필수 목표 충족 시 완료 결과 확정·저장 및 Stage Clear 표시 |
| Playing / ESC 또는 중단 | Pause 확인창, 모든 게임 입력 차단 |
| Pause / 아니오, ESC | 건물·자원·카드·콤보·이력을 유지하고 Playing 복귀 |
| Pause / 예 | 현재 건물·자원 변화·카드 소비·콤보/연출·마지막 콤보 결과·목표/랭크·이력을 폐기하고 Lobby 복귀 |
| Clear / Retry | 동일 스테이지의 보드·자원·카드·목표·콤보·이력을 재초기화하고 안내 표시 |
| Clear / Next Stage | 다음 단계가 실제 존재하고 해금됐을 때 그 단계 초기화 및 안내 표시 |
| Clear / Stage Select | 저장 성공 확인 후 현재 런타임 진행을 폐기하고 선택 화면 진입; 마지막 단계에서도 가능 |
| Clear / 저장 실패 | 결과와 저장 오류 유지; Retry·Next Stage·Stage Select 차단; 저장 재시도 제공 |
| Lobby / Exit | 종료 확인; 취소 가능; 예 선택 시 `OnQuitRequested` 한 번 발행 |

진입 안내는 진입/재진입/Retry마다 표시합니다. 기획서 23쪽의 '스테이지에 진입했을 때 출력' 규칙을 따르며 별도 '이미 본 안내' 영구 저장 항목을 추가하지 않습니다.
Continue는 진행도를 기반으로 선택 화면을 여는 기능입니다. 플레이 중이던 보드나 Undo 이력을 복구하지 않습니다.
최고 랭크와 해금 상태는 Retry나 중도 이탈로 낮아지지 않습니다.
Pause는 턴 게임의 입력 차단으로 처리하며 전역 `Time.timeScale`을 변경하지 않습니다.
Exit 이벤트는 최종 애플리케이션 부트스트랩에서 `Application.Quit` 등에 연결합니다. 테스트/디버그 UI에서 Editor를 강제 종료하지 않습니다.

## 신규 및 수정 파일

신규:

- `Scripts/Data/StageCatalog.cs` + meta: 1~N 순서 및 누락/중복/번호/StageData 검증.
- `Scripts/Core/GameFlowState.cs` + meta: 화면 상태 및 Lobby/Game 목적지.
- `Scripts/Core/StageSessionInitializer.cs` + meta: 실행 전 데이터/참조/프리팹 검증, 단계 초기화, 중도 이탈 초기화.
- `Scripts/Core/SceneFlowManager.cs` + meta: 화면 명령·저장 확인·입력 게이트·중복/재진입 차단.
- `Scripts/UI/GameFlowDebugUI.cs` + meta: 단계 선택/목표 안내/자원/카드/Undo/NEXT/Retry/중단/저장 재시도 임시 조작 UI.
- `Tests/Editor/GameFlowContractTests.cs` + meta: 40개 회귀 테스트.
- 이 변경 안내 문서.

수정:

- `GameSessionManager`: 기본 활성인 입력 게이트와 상태 알림, 단계 초기화 시 마지막 건설 콤보 결과 정리.
- `TurnHistoryManager`: 게이트가 닫힌 동안 Undo 거부/버튼 갱신; 이력 자체는 Pause 취소 시 보존.
- `StageManager`: 게이트가 닫힌 동안 완료 확정 거부, 입력 알림과 처리 중 상태 조회.
- `BuildingPlacementController`: 게이트 검사와 입력 차단 시 드래그 즉시 취소.
- `NextStageButtonUI`: 안내/Pause/Clear 등에서 버튼 비활성; 기존 완료 버튼도 Flow의 Clear 흐름에 연결.
- `SaveManager`: 처리 중 상태의 읽기 전용 조회만 추가.
- `Docs/DEVELOPMENT_ROADMAP.md`.

기존 파일 삭제/이동 없음. 기존 `.meta` GUID, 테스트 155개, assembly 설정, 씬·프리팹·데이터는 유지했습니다.
기존 Prototype에는 새 Flow를 자동 추가하거나 개인 저장 경로를 자동 연결하지 않습니다.

## 초기화 및 후속 연결

Stage 1/2 실제 데이터는 3-J에서 작성합니다. `StageCatalog.stages`에는 번호 순서대로 StageData를 지정합니다.
Flow의 입력 차단 기본값은 기존 Prototype 호환을 위해 `GameSessionManager`에서 true이며, Flow를 명시적으로 설정하면 Lobby에서 false로 바뀝니다.
설정 시 Session의 Board/Resources/Hand/BuildingPrefab/Combos/Stage/History가 모두 연결되고 초기화돼 있어야 합니다.
History는 해당 Session에 연결되어 있어야 합니다. 새 Game 씬 초기 연결은 3-J/Phase 4에서 이 순서를 묶습니다.

1. StageData로 보드·자원·카드 초기화, ComboDatabase 구성.
2. `session.Configure(board, resources, hand, buildingPrefab)` 및 `ConfigureCombos(combos)`.
3. `stage.TryConfigure(stageData, board, resources, hand, out error)` 후 `session.TryConfigureStage(stage, out error)`.
4. `history.TryConfigure(session, stage, out error)`.
5. `save.TryConfigure(catalog.Count, explicitPath, out error)` 또는 최종 제품에서 기본 경로 설정.
6. `flow.TryConfigure(catalog, save, session, comboDatabase, out error)`. 아직 로드되지 않은 저장만 여기서 로드합니다.
7. 임시 확인 UI를 사용할 경우 `debugUI.Configure(flow, session)`.

생산 경로는 `Application.persistentDataPath/URBAN-EQUATION/progress.json`이지만 Flow 테스트는 모두 GUID 임시 경로만 사용합니다.
예상치 못한 Instantiate/런타임 초기화 실패는 입력을 차단한 `StageLoadFailed` 상태로 보고합니다.
사전 검증 실패는 기존 보드/자원/카드/진행 파일을 바꾸지 않습니다. 여러 매니저가 발행하는 초기화 알림 전체에 대한 원자적 롤백을 제공하는 것은 아닙니다.
저장 파일 교체 성공 후 런타임 초기화가 실패하면 이미 새 게임으로 초기화한 진행도는 유지되고 선택 화면에서 재진입할 수 있습니다.

## 검증

기존 155개 + 신규 `GameFlowContractTests` 40개 = 전체 **195개**.
기존 8개 테스트 클래스·assembly·meta는 수정하지 않았습니다.
신규 테스트는 실제 기본 저장 경로, 실제 씬 로드, Application.Quit을 호출하지 않으며 Inspector 수정 없이 실행됩니다.

확인 순서:

1. Unity 종료 → Git Fetch → `codex/phase3i-game-flow` 선택 및 최신 Pull → Unity 재실행.
2. Console 컴파일 오류 없음.
3. Test Runner의 검색/필터 해제 → EditMode Run All → `GameFlowContractTests` 40개 포함 전체 **195개** 확인.
4. 기존 Prototype의 건설·자원·복수 콤보·클리어·Reset 정상 동작 확인.

이 환경에는 Unity와 C# 컴파일러가 없어 직접 컴파일/EditMode/Prototype 실행은 하지 못했습니다.
정적 점검·테스트 목록·원격 반영 검증만 수행했으며 실제 Unity 실행은 사용자 확인 대기입니다.
실제 데이터 및 씬/최종 UI를 통한 눈으로 보는 전체 플레이 루프 검증은 3-J/Phase 4에서 진행합니다.
