# URBAN EQUATION 개발 로드맵

기준: 2026-10-03 최신 PDF(5), 33페이지와 사용자 확정 답변.
작업 시작 전에 이 파일과 `Docs/PHASE4D_FINAL_QA.md`를 읽습니다.
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
- [x] Phase 4-C: Lobby/Game 실제 씬·메뉴·팝업 (사용자 확인 및 생성물 원격 Push 확인)
- [ ] Phase 4-D: 최종 플레이 검증 (통합21개/최종Audit·복수콤보QA미리보기 준비, Unity/Player 검증 대기)

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

2026-10-03T11:24:47+09:00 사용자가4-C확인완료를보고했습니다. 직전379개안내와화면확인완료로반영하며이번발화에는테스트수를재명시하지않았습니다.
원격 `8fc357cb8f9eb12dea68d94c5a034479245aafc7`(프리팹 연결)의화면프리팹/설정/두씬/meta/닫기import Push10파일을확인했습니다.
원격EditorBuildSettings가Prototype만포함해최신씬GUID로Lobby/Game을0/1에등록했습니다.기존Prototype항목/configObjects를보존합니다.
현재4-D,새브랜치 **codex/phase4d-final-qa**,기준은위사용자커밋입니다.상세확인표/해답/미리보기/빌드순서는 **Docs/PHASE4D_FINAL_QA.md**.

신규최종점검메뉴 **Tools > Urban Equation > Phase 4D > 1 Audit Final Setup**은4-A~C참조·MissingScripts·15건물/30콤보·Stage1/2·빌드목록을읽어서확인합니다.
**2 Open Lobby for Final QA**는정식Lobby파일을열며Play는사용자가실행합니다.
**3 Multi Combo QA Preview**는원본데이터의메모리복제본(주택3장/일자리3),기존HUD,매실행GUID가다른임시save만사용합니다.
좌표(0,1)→(1,0)→(1,1)에서마지막건설에왼쪽/아래2콤보,인구5/일자리0,Undo후인구2/일자리1/주택1장복원,재건설/조회/팝업순서를확인합니다. Stop시자기임시폴더정리. 원본Stage1카드2장·일자리2유지.

FinalGameplayContractTests21개는실제사용자HUD/화면프리팹을함께바인딩해Stage1랭크1~3/Stage2최종자원·랭크/연속Undo전체상태·카드순서/비활성/최고랭크save/Pause·Continue·Retry/복수콤보큐·자동타이밍·조회·Undo를확인합니다.
자동테스트는OS GUID임시save와메모리세션만사용하며실제씬load/종료/productionsave/import/asset생성은하지않습니다. 실제비동기씬전환/입력/해상도/Player빌드는수동QA로확인합니다.
신규2C#+meta+안내서5파일,ProjectSettings빌드목록과로드맵2수정으로총7변경.기존런타임소스/379테스트/사용자생성물/데이터/메타/현재구조유지.삭제/이동없음.
다음확인:새브랜치Pull→컴파일오류없음→Audit정상→EditMode **400개**→정식Lobby QA-01~18/복수콤보미리보기→기존Prototype→Windows Player빌드·재실행·종료확인.

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
| FinalGameplayContractTests | 21 |
| 전체 | 400 |

AI환경에는Unity/C#컴파일러가없어실제컴파일·EditMode·Play·렌더/빌드를실행하지못했습니다.구문(컴파일아님)/기존원본hash/씬GUID/빌드목록/테스트선언수400/원격반영을확인합니다. 
현재4-C사용자확인완료와4-D소스/회귀/도구준비완료를구분합니다. 최종400개·Audit·수동/Player QA완료보고후Stage1/2Phase4완료로기록합니다. 
