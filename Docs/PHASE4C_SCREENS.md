# Phase 4-C: 실제 Lobby/Game 씬·메뉴·팝업

2026-10-03 최신 PDF(5) 20~23·28~29페이지와 확정 규칙을 기준으로 합니다.
사용자가 4-B를 확인하고 Push한 `3bbaab7988e5255b9568679f62d6729fdae979fe`(프리팹 연결)에서 시작했습니다.
사용자는 이번 발화에서 테스트 개수를 반복하지 않았으며 직전 안내318개와 HUD 확인 완료로 반영합니다.
현재4-C 소스 구현 완료이며 실제 Unity 생성·컴파일·테스트·씬 전환/화면 확인은 대기입니다.

## 먼저 실행할 순서

1. Unity 종료 → GitHub Desktop Fetch origin → **codex/phase4c-screens** 선택 → Pull → Unity 재실행.
2. Console 컴파일 오류가 없는지 확인합니다. 4-A/4-B 생성 프리팹과 설정은 새 브랜치에 포함되어 있습니다.
3. Play를 정지한 상태에서 **Unity 창 맨 위 메뉴**의 **Tools → Urban Equation → Phase 4C → 1 Create Missing Screens and Scenes**를 실행합니다.
4. 같은 메뉴의 **2 Validate Screens and Scenes**를 실행합니다. 정상 로그는 **Phase4-C Screens Validation OK**입니다.
5. Test Runner 검색/필터 해제 → EditMode → Run All. 예상 전체 **379개**, 신규 **GameScreensContractTests 61개**입니다.
6. 같은 메뉴의 **4 Preview with Temporary Save**를 실행합니다. 임시 로비에서 Play가 시작되며 실제 생성된 Game/Lobby 씬을 오갑니다. **Game 탭 1920×1080 또는1280×720(16:9)**로 아래 기능을 확인합니다. 기존 디버그 패널은 이 모드에서 생성하지 않습니다.
7. Stop 후 **3 Open Lobby Scene**을 실행하고, Project의 실제 `Scenes/Lobby.unity`를 연 상태로 Unity Play 버튼을 누릅니다. 이 실행은 정식 저장 경로를 사용하므로 종료·다시 실행 후 Continue/랭크 유지도 확인합니다.
8. 기존 `Assets/_UrbanEquation/Scenes/Prototype.unity`의 건설·자원·콤보·Reset도 확인합니다.
9. Stop 후 아래 생성 파일과 `.meta`, 수정 import/meta·빌드 목록을 **같은4-C브랜치에 Commit/Push**해주세요. 임시 미리보기 씬은 저장하지 않습니다.

1/2/3/4는 Unity 상단 메뉴 항목입니다. Project 창에서 같은 이름의 파일을 찾는 조작이 아닙니다.
소스 복사, 빈 Canvas 수동 생성, Inspector 슬롯 수동 연결은 기본적으로 필요 없습니다.

## 생성 및 변경되는 파일

| 프로젝트 상대 경로 | 내용 |
|---|---|
| Assets/_UrbanEquation/Prefabs/UI/Popup/Pfb_Flow_Screens_001.prefab | Lobby·시작/종료 확인·Stage Select·안내·Pause·Clear·오류/로딩 화면 |
| Assets/_UrbanEquation/Data/Presentation/GameApplication.asset | GameContent·GameplayHud·화면 프리팹·두 씬 경로 |
| Assets/_UrbanEquation/Scenes/Lobby.unity | 로비 카메라·EventSystem·Scene Entry |
| Assets/_UrbanEquation/Scenes/Game.unity | 게임 카메라·조명·EventSystem·Scene Entry |
| Assets/_UrbanEquation/Prefabs/UI/Popup.meta | 새 프리팹 하위 폴더의 meta |
| Assets/_UrbanEquation/Sprite/UI/Spr_Ui_XButton_001.png.meta | 닫기 PNG를 Sprite (2D and UI) / Single로 import |
| ProjectSettings/EditorBuildSettings.asset | Lobby/Game을 활성화하여 빌드 씬 목록 앞에 등록 |

각 신규 asset/scene/prefab의 `.meta`도 함께 생성됩니다. 기본 최초 생성은 신규9파일(4개파일+각meta+폴더meta), 기존2파일 수정입니다.
이미 생성/설정되어 있거나 사용자가 편집한 경우 목록이 달라질 수 있습니다.
기존 씬 파일은 삭제·이동·덮어쓰지 않습니다. 빌드 목록의 기존 다른 씬은 순서와 enabled 값 그대로 두 씬 뒤에 보존합니다.
메뉴1은 이미 존재하는 화면 프리팹과 씬을 덮어쓰지 않습니다. 설정 asset 참조/import/빌드 목록만 재연결하며 슬롯 누락은 메뉴2로 알려줍니다.
부분 실패 시 완료된 생성물은 남습니다. Console 원인을 해결하고1→2를 다시 실행합니다.

## 파일별 Inspector 확인

`Data/Presentation/GameApplication.asset`을 선택합니다.

| 슬롯 | 연결 |
|---|---|
| Content | Data/GameContent.asset |
| Hud | Data/Presentation/GameplayHud.asset |
| Screens Prefab | Pfb_Flow_Screens_001.prefab 루트 GameScreensUI |
| Lobby Scene Path | Assets/_UrbanEquation/Scenes/Lobby.unity |
| Game Scene Path | Assets/_UrbanEquation/Scenes/Game.unity |

두 씬을 각각 열고 Hierarchy의 **Scene Entry**를 선택합니다.
GameSceneEntry의 Settings는 GameApplication.asset, Destination은 각Lobby/Game, Board Camera는 해당 씬 Main Camera입니다.
**Temporary Save는 두 정식 씬 모두 꺼짐**, Frame Board는 기본 켜짐입니다. Game의 카메라를 직접 고정할 경우에만 Frame Board를 끄고 편집해주세요.
각 씬 EventSystem에 **InputSystemUIInputModule**과 기본 Actions가 연결되어야 합니다.
화면과 HUD는 Play에서 생성되므로 Edit 상태의 씬 Hierarchy에는 Canvas가 없습니다. UI 레이아웃은 해당 프리팹을 Prefab Mode로 열어 편집합니다.

`Pfb_Flow_Screens_001.prefab` 루트 Canvas는 Screen Space - Overlay / Sort Order100,
CanvasScaler는1920×1080 / Match0.5이며 GraphicRaycaster와 GameScreensUI가 연결됩니다.
GameScreensUI의 Lobby Background/Lobby/Modal Blocker와 각 화면 루트, 제목/안내/오류 TMP, 각Button 슬롯은 생성 메뉴가 자동 연결합니다.
Clear Goals/Stars는 각각3개입니다. Stage Row Prefab은 Stage Select/Viewport/Rows/Stage Row Template,
Stage Row Content는 같은Rows Transform입니다. 원본 Template은 비활성 상태이며 실행 때 StageCatalog 개수만큼 복제합니다.
이 프로젝트의 상세 데이터는 Stage1/2뿐이므로 선택 목록도2개입니다. 기획 그림의3~5를 임의의 스테이지로 추가하지 않습니다.

## 플레이 확인: 로비·확인 팝업·Stage Select

- 저장이 없는 최초 실행: Lobby의 Continue는 비활성입니다. Play → Game 씬 → Stage1 안내가 표시됩니다.
- 저장이 있는 실행: Play → “지난 게임을 이어 할까요?” 팝업. X/ESC는 취소, “이어하기”는 Stage Select, “새로 시작하기”는 저장 진행도/랭크 초기화 후 Stage1 안내입니다.
- 저장이 손상돼 Continue가 불가능하면 시작 확인의 “이어하기”도 비활성입니다. 명시적으로 새로 시작하기를 누르면 새 진행도를 만듭니다.
- Continue → Stage Select: 저장된 최고 랭크만큼 별(최대3개), 미해금 스테이지는 잠금 아이콘/비활성 버튼입니다. 화살표/ESC는 Lobby로 돌아갑니다.
- 해금된 스테이지 클릭 → Game 씬의 해당 안내. 씬 전환 중에는 로딩 화면이 입력을 가립니다.
- Exit → 종료 확인: 아니오/ESC는 Lobby, 예는 실행 종료. **Unity Editor에서는 Play를 정지하고, 빌드된 Player에서는 Application.Quit를 실행합니다.**

## 플레이 확인: 안내·Pause·Clear·Retry·Next

- 안내: 단계명/설명/필수 목표/추가 목표2개를 표시하고 뒤쪽 HUD/보드를 어둡게 보여줍니다. OK 전에는 건설 불가, OK 후에는4-B HUD로 조작합니다.
- Pause 버튼/ESC → 중단 확인. “아니오”/ESC는 현재 보드·자원·카드·Undo를 그대로 복원합니다. “예”는 현재 실행 상태를 폐기하고 실제Lobby씬으로 돌아갑니다.
- Continue로 다시 그 스테이지를 선택하면 시작 상태입니다. 중도 보드·자원·Undo는 영구 저장하지 않습니다.
- Stage1 주택(0,0) 한 채 후 NEXT: Rank1. 두 번째를(1,1)에 지으면 Rank2, (1,0)에 지으면 콤보/인구3으로Rank3입니다.
- NEXT → Clear: 달성 랭크 별, 필수/추가 목표의 파랑/분홍과 취소선을 확인합니다. 저장 실패 시 Retry/Next/Stage Select가 비활성이고 “저장 재시도”가 표시됩니다.
- Retry → 같은Game씬에서 해당 스테이지를 시작 상태로 다시 만들고 안내를 표시합니다. Scene Entry/카메라/EventSystem/세션이 중복 생성되면 안 됩니다.
- Clear의 NEXT STAGE → 해금된다음단계 안내. Stage1→2는 가능하며 현재마지막Stage2에서는NEXT가비활성입니다. Stage Select 버튼으로선택목록으로돌아갈수있습니다.
- Stage2 Rank3 예시: 주택(0,0)→등대(0,1)→공장(0,2)→식당(1,0)→사무실(1,2). 카드5개 사용/인접목표2개/Rank3 확인.
- 높은 랭크로 완료 후 다시 낮은 랭크로 완료해도 Stage Select의 최고 랭크는 내려가지 않아야 합니다.
- 카드 상세/건물 상세/읽기전용 콤보 조회·자동 콤보·Undo 규칙은4-B와 동일합니다. 팝업 뒤에서는 UI/보드 클릭이 실행되지 않아야 합니다.

## 저장과 빌드 목록

정식 씬 실행: `Application.persistentDataPath/URBAN-EQUATION/progress.json`.
메뉴4 임시 미리보기: `Application.temporaryCachePath/UrbanEquationPhase4CPreview/progress.json`.
이전 Stage1-2 Debug/4-A/4-B 미리보기의 `UrbanEquationDebug/phase3j-progress.json`과 분리합니다.
임시 미리보기도 같은 임시 경로를 다시 사용하므로 재실행하면 Continue가 켜질 수 있습니다. 초기화가 필요하면 Play→새로 시작하기를 사용합니다.
자동 테스트는 GUID가 포함된 OS 임시 폴더만 사용하며 실제 저장 파일을 쓰거나 씬을 로드하거나 종료하지 않습니다.

Unity **File → Build Profiles → Scene List**에서0=Lobby,1=Game이활성인지 확인합니다.
메뉴1은 전역 EditorBuildSettings 목록을 갱신합니다. 특정 Build Profile이 자체 Scene List를 덮어쓰고 있다면 같은두씬을등록하거나 전역목록사용으로설정해주세요.
새 런타임 GameApplication 한 개가 DontDestroyOnLoad로 세션/저장/Flow/Router를 유지합니다.
씬별 카메라·EventSystem·Canvas·드래그 컨트롤러는 씬과 함께 교체됩니다. Game씬을직접Play하면초기Lobby로안내합니다.

## UI 그림 교체와 오류 확인

- 배경은 기획서에 지정된 GUI팩 `Background_Images/extra large/background-1-extra-large.png`를 사용합니다. 예시의 도시 커버와 정확한 외형으로 교체하려면 화면 프리팹의 Lobby Background/Image/Sprite를 원하는 Sprite로 바꾸고 저장합니다. 별도 커버 이미지 제작은 이번 작업에 포함되지 않습니다.
- 팝업/버튼/별/잠금/재생/빈랭크 슬롯은 기존 Space_Exploration_GUI_Kit을 사용하며 외부팩 import 설정은 바꾸지 않습니다. 닫기만프로젝트의Spr_Ui_XButton_001를import합니다.
- 메뉴가 안 보이면 최신4-C브랜치와 Console 컴파일 오류, `Editor/UrbanEquationScreensSetup.cs`를 확인합니다.
- 씬 로딩 오류는 Scene List와 GameApplication의 경로를 확인하고 “다시 시도”를 누릅니다. 스테이지 초기화 오류는 “Stage Select”로 돌아가 데이터/참조를 확인합니다.
- 한국어/★/←/… 글리프 경고가 발생하면 기존 `Fonts/NotoSansKR-Medium SDF.asset`의 Source Font 및 Dynamic 설정을 확인합니다. 폰트를삭제하지않습니다.
- 기존 화면 프리팹/씬을 직접 편집했다면 메뉴2의 누락 슬롯/경로를 위표대로 복구해주세요. 메뉴1은 레이아웃을 강제로 덮어쓰지 않습니다.

## 소스 변경 범위 및 검증 경계

- 신규9C#+각meta: Data/GameApplicationSettings; Core/GameApplication·GameSceneRouter·GameSceneEntry; UI/Lobby/StageSelectRowUI; UI/Popup/GameScreensUI; Editor/UrbanEquationScreensFactory·ScreensSetup; Tests/Editor/GameScreensContractTests. 신규안내서 포함19파일.
- 수정: Core/SceneFlowManager(읽기전용 CanResumeSavedGame), UI/Gameplay/GameHudUI(4-C에서만팝업뒤HUD표시옵션), Docs/DEVELOPMENT_ROADMAP.md. 총22소스/문서파일이며 삭제/이동없음.
- 기존318개테스트/사용자HUD프리팹·설정·자원아이콘meta/4-A프리팹·데이터/기존Prototype·Test/현재폴더구조유지.
- 신규61개는 화면/버튼/슬롯/저장실패복구/해금·랭크/씬로딩gate·실패·재시도/라이프사이클/종료callback/설정/빌드목록과HUD옵션을 확인합니다. IO는콜백으로대체하며 실제Unity씬전환/Player종료/렌더링은 사용자검증입니다.
- AI는구문(컴파일아님)·기존파일hash/GUID·데이터참조·기획/경로·테스트선언379·원격반영을확인합니다. Unity/C#컴파일러가없어실제컴파일/테스트/생성/Play는실행하지못했습니다.
- 사용자4-C테스트/두씬실행/프로토타입확인과생성파일Push후4-D최종Stage1/2 QA로진행합니다.

Unity API: [LoadSceneAsync](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityengine/scenemanagement/scenemanager/loadsceneasync), [EditorBuildSettings](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/editorbuildsettings).
