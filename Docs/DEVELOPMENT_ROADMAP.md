# URBAN EQUATION 개발 로드맵

기준: 2026-10-03 최신 PDF(5), 33페이지와 사용자 확정 답변.
작업 시작 전에 이 파일과 `Docs/PHASE4C_SCREENS.md`를 읽습니다.
상세 로드맵: `dlwodms12/LJE_GPT_Log`의 `memory/entries/MEM-20261001-0610-urban-equation-development-roadmap-v1.md`.

## 진행 순서

- [x] Phase 1: 기획 분석·질문·작업 순서 확정
- [x] Phase 2: 폴더 구조 설계 및 사용자 이동 작업 확인
- [x] Phase 3-A: 공통 데이터 정의 (8개 테스트·Prototype 사용자 검증)
- [x] Phase 3-B: StageData 기반 보드·타일 생성 (17개·Prototype 사용자 검증)
- [x] Phase 3-C: 5종 자원·요구/획득 자원 목록 (31개·Prototype 사용자 검증)
- [x] Phase 3-D: 카드 순서·수량·드래그·건설 (51개·Prototype 사용자 검증)
- [x] Phase 3-E: 콤보 판정·결과·순차 연출 데이터 (사용자 검증)
- [x] Phase 3-F: 턴 완료 Snapshot·다단계 Undo (95개·Prototype 사용자 검증)
- [x] Phase 3-G: 목표·랭크·NEXT STAGE (123개·Prototype 사용자 검증)
- [x] Phase 3-H: 진행도·해금·최고 랭크 저장 (사용자 검증)
- [x] Phase 3-I: Lobby/Game 화면 상태·New Game/Continue·Retry·Exit (195개·Prototype 사용자 검증)
- [x] Phase 3-J: 실제 BuildingData15종·Stage1/2 전체루프 (전체테스트·Prototype·디버그 기능 사용자 검증 완료)
- [x] Phase 4-A: 공통 프리팹 (사용자 테스트·실제 동작 확인 및 프리팹/연결 데이터 Push 완료)
- [x] Phase 4-B: Main Game HUD·카드/건물 상세·콤보 조회 (사용자 확인 및 생성 파일 Push 완료)
- [ ] Phase 4-C: Lobby/Game 실제 씬·메뉴·팝업 (소스/61개 신규 테스트 작성, Unity 생성/검증 대기)
- [ ] Phase 4-D: 최종 플레이 검증

## 확정 규칙

- Scene은 Lobby와 Game, 스테이지 선택은 Lobby 내부 UI입니다.
- 사용한 카드 제거, Undo 시 카드 목록과 순서까지 복원합니다.
- A/B/C 건설 후 Undo는 C→B→A까지만 가능하며 빈 초기 보드로는 돌아가지 않습니다.
- Undo는 보드·자원·콤보 결과·목표·랭크·NEXT STAGE 등 해당 턴 상태 전체를 복원합니다.
- 영구 저장은 진행도와 최고 랭크이며 실행 중 보드 및 Undo 이력은 저장하지 않습니다.
- 보드 북쪽은 +Z, 문서 위쪽 행이 북쪽 행입니다.
- 콤보는 왼쪽→아래→오른쪽→위 순서로 검사하고 복수 결과를 순차 표시합니다.
- 모호한 기획은 질문 후 확정합니다. 파일 변경·대체·삭제는 사용자에게 명시합니다.

## 현재 위치와 검증 경계

2026-10-03T10:50:15+09:00 사용자가 “확인하고 Push 했어”라고 보고했습니다.
직전 안내는 전체318개와HUD검증이며 이번발화에개수를재명시하지않았습니다.4-B사용자확인완료로반영합니다.
4-B원격커밋 `3bbaab7988e5255b9568679f62d6729fdae979fe`(프리팹 연결): HUD/ResourceIcons/GameplayHud와meta 신규6파일, 자원아이콘import meta15수정을 확인했습니다.
4-A/4-B 사용자 생성물과 데이터·모델·기존테스트를 유지하며 현재4-C입니다.
새브랜치 **codex/phase4c-screens**, 기준은위사용자커밋. 상세순서/경로/슬롯/빌드목록/플레이확인은 **Docs/PHASE4C_SCREENS.md**.

4-C는실제Lobby/Game씬,로비Play/Continue/Exit,시작·종료확인,StageSelect해금/최고랭크,첫진입안내,Pause/Clear/Retry/Next/저장·로딩오류복구를연결합니다.
GameApplication이세션/저장/Flow/Router를유지하며씬카메라/EventSystem/뷰만교체합니다.로딩동안입력차단,Retry/Next는같은Game씬에서스테이지초기화,Exit확인시EditorPlay정지/Player종료입니다.
기존디버그메뉴와Prototype는유지합니다. 새Game씬에서만HUD뒤배경표시옵션을켜며게임입력gate는그대로적용합니다.
새메뉴 **Tools > Urban Equation > Phase 4C > 1 Create Missing Screens and Scenes → 2 Validate Screens and Scenes → 4 Preview with Temporary Save**.
정식씬은 **3 Open Lobby Scene** 이후Play로실행합니다.
사용자메뉴1이Pfb_Flow_Screens_001.prefab/GameApplication.asset/Lobby.unity/Game.unity와meta/Popup폴더meta를생성하고,XButtonPNGmeta와EditorBuildSettings.asset를수정합니다.
빌드목록은Lobby/Game을앞에추가하고기존다른씬항목순서/enabled를보존합니다.기존HUD/프리팹/데이터/씬파일삭제·이동·덮어쓰기없음.
신규9C#+각meta 및안내서19파일,위HUD/Flow/로드맵3수정으로총22소스/문서변경입니다.생성물은Unity실행후사용자Commit/Push필요합니다.
다음확인: Unity종료→새브랜치Pull→컴파일오류없음→메뉴1/2 `Phase4-C Screens Validation OK`→EditMode전체 **379개**→메뉴4실제씬전환/화면→메뉴3정식저장재실행/Continue→기존Prototype→생성파일/meta·수정import/빌드목록 Commit/Push.

| 테스트 클래스 | 예상 수 |
|---|---:|
| DataContractTests | 8 |
| BoardContractTests | 9 |
| ResourceContractTests | 14 |
| BuildingContractTests | 20 |
| ComboContractTests | 20 |
| TurnHistoryContractTests | 24 |
| StageGoalContractTests | 28 |
| SaveContractTests | 32 |
| GameFlowContractTests | 40 |
| StageIntegrationContractTests | 48 |
| PrefabContractTests | 29 |
| GameHudContractTests | 46 |
| GameScreensContractTests | 61 |
| 전체 | 379 |

AI환경에는Unity/C#컴파일러가없어실제생성/import·컴파일·EditMode·Play·렌더를실행하지못했습니다. 소스구문(컴파일아님)/사용자참조GUID/테스트선언수/기존파일보존/원격반영을확인합니다.
실제4-C프리팹/설정/씬은사용자메뉴실행에서생성하며4-C검증완료로간주하지않습니다.확인및생성파일Push후4-D최종Stage1/2검증으로진행합니다.
