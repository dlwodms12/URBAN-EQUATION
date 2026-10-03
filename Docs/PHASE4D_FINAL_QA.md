# Phase 4-D: Stage 1·2 최종 QA

기준: 최신 PDF(5)와 확정한 Undo/저장/카드/콤보 규칙.
2026-10-03 사용자가 4-C 확인 완료를 보고했습니다. 이번 발화에는 테스트 수를 다시 명시하지 않았습니다.
원격 `8fc357cb8f9eb12dea68d94c5a034479245aafc7`(프리팹 연결)의 화면 프리팹·설정·두 씬·meta 등 생성물 Push를 확인했습니다.
원격 빌드 목록에는 Prototype만 남아 있어 **사용자 씬의 실제 GUID로 Lobby·Game을 첫 두 항목에 등록**했습니다. 기존 Prototype과 Input System 설정은 보존했습니다.
4-D 소스와 확인 절차는 준비되었으며, 실제 Unity 테스트·플레이·Windows Player 확인은 대기 중입니다.

## 실행 순서

1. Unity 종료 → Git Fetch → **codex/phase4d-final-qa** 선택 → Pull → Unity 재실행, 컴파일 오류가 없는지 확인합니다.
2. Play 정지 상태에서 Unity 창 **맨 위 메뉴** **Tools → Urban Equation → Phase 4D → 1 Audit Final Setup**을 실행합니다.
   정상 로그: **Phase4-D Final Setup Audit OK**. 이 로그는 참조/설정 점검 결과이며 테스트 통과·플레이 완료를 의미하지 않습니다.
3. Test Runner 검색/필터 해제 → EditMode → Run All. 전체 예상 **400개**, 신규 **FinalGameplayContractTests 21개**입니다.
4. 같은 메뉴의 **2 Open Lobby for Final QA**를 실행하고 Unity Play를 누릅니다. Game 탭을 1920×1080와 1280×720(16:9)로 바꾸며 아래 표를 확인합니다.
5. Stop 후 **3 Multi Combo QA Preview**를 실행해 복수 콤보 순차 표시를 확인합니다. 아래 별도 순서대로 드래그합니다. 임시 미리보기 씬은 저장하지 않습니다.
6. Stop 후 기존 `Assets/_UrbanEquation/Scenes/Prototype.unity`의 건설·자원·콤보·Reset을 확인합니다.
7. Windows Player 빌드·재실행·종료까지 확인한 뒤 결과를 알려주세요. 문제가 있으면 QA 번호, 테스트 이름 또는 Console 로그, 실행 브랜치와 커밋을 함께 공유해주세요.

이번에는 기존 4-C 생성물을 사용합니다. Inspector를 수동으로 다시 연결할 필요가 없습니다.
번호 1·2·3은 **Unity 상단 메뉴 항목**입니다. Audit가 생성물 누락을 알리면 지적된 경로를 확인하고 해당 단계의 생성 메뉴를 실행하세요.

## 자동 점검과 회귀 범위

- Audit는 4-A~C 참조, Stage 1·2 크기와 카드 수, 건물 15종·콤보 30종, 프리팹 8개의 Missing Script, 씬 GUID와 빌드 목록을 점검합니다.
- 신규 21개는 실제 Push된 HUD와 화면 프리팹을 함께 사용해 Stage 1 랭크 1~3, Stage 2 최종 상태, 연속 Undo·카드 순서·목표 UI, 비활성 카드, Pause·Continue·Retry, 최고 랭크 저장, 복수 콤보 순서·타이밍·조회·Undo를 확인합니다.
- 기존 379개도 함께 실행합니다. 테스트는 GUID가 다른 OS 임시 저장 경로를 사용합니다. 정식 저장 쓰기, asset 생성/import, 실제 씬 전환과 프로그램 종료는 실행하지 않습니다. 참조 점검은 기존 씬을 읽기 위해 잠시 추가로 열 수 있습니다.
- 실제 비동기 씬 전환, 마우스 입력, 화면 배치와 Player 종료는 아래 수동 QA로 확인합니다.

## 수동 확인표

정식 실행은 `Assets/_UrbanEquation/Scenes/Lobby.unity`에서 시작합니다.
기존 저장이 있으면 Play → 새로 시작하기로 초기화할 수 있습니다. 이 버튼은 정식 진행도를 초기화하므로, 최고 랭크 유지 검증은 초기화 후 진행해주세요.

| 번호 | 조작 | 기대 결과 |
|---|---|---|
| QA-01 | Lobby에서Continue/Play/Exit 확인 | 저장이없으면Continue비활성. 기존저장이있으면Play시시작확인. X/ESC취소,이어하기선택화면,Exit아니오복귀 |
| QA-02 | Stage1안내 전 카드/UI/보드 클릭 → OK | 안내에3목표. OK전건설불가,후HUD조작. 화면위검은딤영역은입력을차단 |
| QA-03 | Stage1주택(0,0)1채 → NEXT | 인구1/일자리1,Undo비활성,필수달성/추가2개미달성,ClearRank1 |
| QA-04 | Retry → 주택(0,0)·(1,1) → NEXT | 대각선콤보없음,인구2/일자리0,ClearRank2 |
| QA-05 | Retry → 주택(0,0)·(1,0) → NEXT | 인구3/일자리0,주택콤보1개,ClearRank3,Stage2해금 |
| QA-06 | Clear→Retry/Stage Select/Next | Retry는동일단계초기화·안내,선택은최고랭크별표시,Next는Stage2안내. 카메라/EventSystem/세션중복없음 |
| QA-07 | Stage2초기 카드/hover/잘못된배치 | 주택·식당만활성,사무실·공장·등대검은오버레이와선택차단. 비활성카드도상세확인가능. 점유타일/UI영역놓기실패시자원·카드변경없음 |
| QA-08 | Stage2해답을아래순서로건설 | 모든5카드소비,최종자원 인구0/일자리1/자금0/물류1/관광0,목표3개달성,Rank3 |
| QA-09 | QA-08에서NEXT전 Undo4번 | 마지막→첫건설까지보드/5자원/콤보/카드순서·활성/목표·랭크·NEXT복원. 첫건설에서Undo비활성,빈보드로못돌아감 |
| QA-10 | Undo후다른유효위치에재건설 | 사용카드는다시제거되고새턴기록. 폐기된미래가다시복원되거나콤보보상이중복지급되지않음 |
| QA-11 | 건설·콤보조회·드래그중Pause/ESC→아니오 | 드래그/조회는닫히고중단확인이입력을막음. 아니오후보드·자원·카드·Undo유지 |
| QA-12 | Pause예→Lobby→Continue→같은Stage선택 | 현재보드/자원/Undo폐기. Stage초기상태와안내로시작. 최고랭크/해금은유지 |
| QA-13 | 최고Rank3이후Rank1로다시완료,Stop·재실행 | Continue/별3개·해금유지. 낮은재클리어가최고랭크를낮추지않음 |
| QA-14 | Stage2클리어 | 마지막단계Next비활성,Retry/Stage Select사용가능. 존재하지않는Stage3진입안함 |
| QA-15 | 카드·지어진건물hover,콤보건물2개클릭·다른곳클릭 | 허용타일/획득/소비상세,콤보조회는시간지나도유지하고다른클릭에닫힘. 자원재지급/자동큐소비없음 |
| QA-16 | 1920×1080·1280×720에서팝업/카드/목표/버튼 | 주요텍스트·버튼이잘리거나겹치지않음. 팝업이게임입력을차단. 한국어/★/화살표글리프경고없음 |
| QA-17 | 기존Prototype건설/자원/콤보/Reset | 이전프로토타입동작유지,Console오류없음 |
| QA-18 | Windows Player에서로비→1·2→재실행→Exit예 | 실제씬전환/저장/Continue/최고랭크동작. 종료확인예로프로그램종료 |

Stage2 Rank3 순서: **주택(0,0) → 등대(0,1) → 공장(0,2) → 식당(1,0) → 사무실(1,2)**.
문서 위쪽은 북쪽(+Z)이며 좌표는 보드 x/z입니다. 테스트 CardId 순서는 1/5/4/3/2입니다.
QA-09는 NEXT를 눌러 Clear 화면으로 넘어가기 전에 진행합니다. Clear 이후에는 Undo를 할 수 없습니다.

## 복수 콤보 미리보기

정식 Stage 1에는 주택 2장, Stage 2에는 서로 다른 카드 5장이 있습니다.
메뉴 3은 Play 진입 후 **Stage 1·StageCatalog·GameContent의 메모리 복제본**에 주택 3장과 일자리 3을 설정합니다.
기존 4-B HUD를 사용하며 게임플레이 밖 화면은 디버그 패널입니다. 원본 asset·씬·HUD 프리팹은 수정하지 않습니다.

1. 첫주택을 **(0,1)**, 둘째를 **(1,0)**에놓습니다. 서로대각선이며인구2/일자리1,콤보없음.
2. 셋째주택을 **(1,1)**에놓습니다. 왼쪽→아래두쌍의주택콤보가발생합니다.
3. 두콤보가서로다른건물사이에서순차표시되는지확인합니다. 각각전체1.5초이며마지막0.5초에페이드합니다. 최종인구5/일자리0입니다.
4. Undo를누릅니다. 셋째건물제거,인구2/일자리1/주택카드1장복원,남은자동팝업과조회가닫혀야합니다.
5. 다시(1,1)에건설합니다. 두콤보재표시,인구5이며이전보상이덧붙어인구7등이되지않아야합니다.
6. 두건물을차례로클릭해조회합니다. 조회전후자원동일,자동순차표시가있다면조회가큐를넘기지않아야합니다.
7. Stop을누릅니다. 임시씬저장불필요. 원본 `Data/Stages/Stage01.asset` 카드2장/일자리2가유지되는지확인합니다.

미리보기는 매번 GUID가 다른 `Application.temporaryCachePath/UrbanEquationPhase4D/<GUID>/progress.json`을 사용하고 Stop 시 해당 임시 폴더를 정리합니다.
정식 실행의 `Application.persistentDataPath/URBAN-EQUATION/progress.json`은 사용하거나 초기화하지 않습니다.

## Windows 빌드 확인

1. Unity **File → Build Profiles**에서 Windows플랫폼을선택합니다. 필요한빌드지원모듈이없으면Unity Hub에서같은에디터버전의Windows Build Support를설치합니다.
2. **Scene List** 첫두항목이활성Lobby/Game인지확인합니다. 자체목록override를사용한다면두씬을등록하거나전역목록사용으로설정합니다. Audit는전역EditorBuildSettings를점검합니다.
3. Development Build로 **Build And Run**을실행합니다. 출력폴더는프로젝트밖(예:바탕화면`UrbanEquation_QA`)으로선택하고실행파일이름을 `UrbanEquation.exe`로설정합니다.
4. QA-18을확인합니다. Player에서는Exit예가실제종료이고Editor에서는Play정지입니다. 기본save는Editor와Player의Application.persistentDataPath환경에따라서로다를수있습니다.
5. 빌드실행파일/데이터폴더는Git소스에Commit하지않습니다. Editor설정/프리팹을직접수정했다면해당파일과meta만같은4-D브랜치에Commit/Push해주세요.

## 변경 범위와 완료 기준

- 신규 5파일: `Editor/UrbanEquationFinalQa.cs`·meta, `Tests/Editor/FinalGameplayContractTests.cs`·meta, 이 안내서.
- 수정 2파일: `ProjectSettings/EditorBuildSettings.asset`(누락 씬 등록), `Docs/DEVELOPMENT_ROADMAP.md`. 총 7파일입니다.
- 기존 런타임 코드·379개 테스트·사용자 생성물·데이터·meta·폴더 구조를 유지했습니다. 삭제와 이동은 없습니다.
- AI 확인 범위는 기존 파일 해시, C# 구문, 씬 GUID·빌드 목록·데이터 참조, 테스트 선언 수 400개, 원격 변경입니다. 이 환경에는 Unity/C# 컴파일러가 없어 실제 컴파일·테스트·Play·빌드를 실행하지 못했습니다.
- **400개 통과 + Audit 정상 + QA-01~18 및 복수 콤보 확인 완료**를 보고하면 Stage 1·2의 Phase 4 완료로 반영합니다. 현재 최종 QA는 대기 중입니다.
- Stage 3~5 상세 데이터와 게임 커버 교체 등 추가 콘텐츠는 이번 작업에 포함하지 않았습니다.
