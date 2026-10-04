# v0.20: Complaint·화면 스크롤·Stage3~5

기준: 최신 PDF(6)와 사용자 확정 답변. 작업 시작점은 main `a770f3ee1023decfb7f9b183ee011c558709b105`입니다.
검증 브랜치: `codex/v020-complaints-and-tutorial-stages`.

## 반영 내용

- 대형 주택은 Grass, 소형 사무실은 모든 타일, 대형 사무실은 Asphalt에서 건설합니다. 카지노의 획득 자원은 기존대로 **자금 +3, 관광 +1**입니다.
- 기존 30개 콤보에 중형 건물 콤보 4개와 Complaint 4개를 추가했습니다. Complaint는 인접한 건물 쌍에서 발생하는 음수 자원 결과이며 기존 순차 연출·두 건물 조회를 사용합니다.
- 자원은 음수까지 내려갈 수 있습니다. 기존 비용 판정은 보유량이 요구량보다 적으면 건설을 막으므로, 0/음수 상태인 자원을 요구하는 건물은 건설할 수 없습니다. 다른 자원만 요구하는 건물은 건설 가능합니다.
- 콤보/Complaint 개수는 건물 코드나 콤보 코드의 종류 수가 아니라 현재 보드의 **서로 다른 건물 쌍** 수입니다. 같은 조합이 다른 위치에 생기면 각각 셉니다. Undo는 쌍과 개수를 해당 턴 상태로 복원합니다.
- 직접 조회 목표는 해당 종류의 쌍을 한 번만 조회하면 달성합니다. 자동 연출은 조회로 세지 않습니다. 조회는 자원을 다시 지급/차감하거나 턴을 추가하지 않습니다. 조회 여부도 현재 턴 Snapshot에 반영하며, 조회 전 턴으로 Undo하면 그 턴의 조회 상태로 돌아갑니다.
- 로비는 새 도시 이미지를 사용하며 스테이지 선택은 기존 우주 배경을 유지합니다. 스테이지 목록은 세로 휠·드래그·스크롤바를 지원합니다.
- 건물 목록이 실제 표시 영역보다 넓으면 좌우 버튼이 나타납니다. 클릭은 카드 하나와 간격만큼 이동하고 누르고 있으면 0.5초마다 반복합니다. 각 방향 끝에서는 해당 버튼을 숨깁니다. 건물 목록의 마우스 휠 이동은 끕니다.
- 정식 Stage03/04/05를 생성하고 StageCatalog 끝에 등록했습니다. 기존 저장의 별점/해금은 기존 확장 로직을 사용합니다.

기존 씬·Stage01/02·건물 모델 크기/높이·HUD의 텍스트/카드 위치는 유지했습니다. 기존 HUD와 화면 프리팹에는 새 설정 참조 하나를 연결했고 스크롤 버튼/바는 실행 중 생성합니다. **프리팹 재생성 메뉴를 실행하거나 Inspector에 리소스를 직접 연결할 필요가 없습니다.**

## Pull과 자동 테스트

1. 현재 편집 내용을 저장하고 Unity를 종료합니다.
2. GitHub Desktop에서 **Fetch origin → Current branch → codex/v020-complaints-and-tutorial-stages**를 선택하고 필요하면 **Pull origin**을 실행합니다.
3. Unity **6000.0.74f1**로 프로젝트를 열고 컴파일이 끝날 때까지 기다립니다. Console에 컴파일 오류가 없는지 확인합니다.
4. **Window → General → Test Runner → EditMode → Run All**을 실행합니다. 예상 실행 수는 **505개**입니다.
5. 신규 **TutorialStageContractTests 38개**, **UiUpdateContractTests 27개**가 보여야 합니다. 기존 440개는 건설 조건·정식 스테이지 수·콤보 수의 변경에 맞춰 기대값을 갱신했습니다. 전체 클래스별 수는 `Docs/DEVELOPMENT_ROADMAP.md`에 있습니다.
6. Play를 종료한 상태에서 **Tools → Urban Equation → Phase 4D → 1 Audit Final Setup**을 실행하여 `Audit OK`를 확인합니다.
7. **Assets/_UrbanEquation/Scenes/Lobby.unity**를 열어 Play하고 아래 순서로 확인합니다. Game View는 우선 **1920×1080**으로 사용합니다.

AI 환경에서는 C# 구문·Unity YAML·GUID/참조·기존 파일 보존·실제 에셋으로 계산한 해답 자원 흐름을 확인했습니다. **Unity 실행 환경과 C# 컴파일러가 없어 실제 컴파일, EditMode 505개, Play, Player 빌드를 실행하지 못했습니다.** 이 문서의 개수는 기대 실행 수입니다.

## 화면과 저장 확인

1. 로비에 도시 배경이 나오고 타이틀/시작 버튼을 가리지 않는지 확인합니다.
2. 기존 저장이 있으면 먼저 Continue를 사용해 별점과 해금이 유지되는지 확인합니다. 기존 Stage2를 클리어한 저장이면 Stage3까지 해금되고 Stage4/5는 잠겨 있어야 합니다. 기존 저장을 지우거나 JSON을 수정할 필요는 없습니다. New Game은 별도 확인 시 기존 진행도를 초기화하므로 보존할 저장이 있다면 먼저 Continue를 확인합니다.
3. 스테이지 선택에서 휠과 오른쪽 스크롤바/핸들을 사용해 Stage5 행까지 이동합니다. 행 순서·잠금·별점이 맞고 목록 밖으로 행이 나오지 않는지 확인합니다. 목록이 표시 영역에 모두 들어가는 크기라면 스크롤바는 자동으로 숨겨집니다.
4. 기존 Stage1/2의 건설·콤보·Undo·목표·NEXT STAGE·Retry·Pause·Exit를 확인합니다. **Assets/_UrbanEquation/Scenes/Prototype.unity**도 별도로 실행해 기존 건설·콤보·Reset을 확인합니다.
5. 아래 Stage3~5의 해답을 순서대로 사용합니다. 필수 목표가 달성되어도 조회/추가 건설을 마친 뒤 NEXT STAGE를 눌러야 별 3개를 확인할 수 있습니다.

## Stage3 해답

3×3 보드의 문서 위쪽/북쪽은 y=2, 아래쪽/남쪽은 y=0입니다. 위쪽은 Asphalt, 가운데는 Concrete, 아래쪽은 Grass입니다. 모든 신규 스테이지가 이 구성을 사용합니다.

초기 자원 순서는 **인구, 일자리, 자금, 물류, 관광**이며 Stage3는 **0, 3, 4, 1, 4**입니다.

| 순서 | 건물 | 좌표 |
|---|---|---|
| 1 | 대형 공장 | x=1, y=1 |
| 2 | 대형 사무실 | x=1, y=2 |
| 3 | 대형 주택 | x=1, y=0 |

모든 타일 종류에 건설, 인구 4 이상, 건설 3개 이하 목표를 모두 달성합니다. 최종 자원은 **4, 4, 1, 3, 0**, 별 3개입니다. 추가로 소형 건물을 네 번째로 설치하면 건설 개수 목표가 해제되고 Undo하면 다시 달성되는지 확인합니다.

## Stage4 해답 — 사용자 원본 좌표

초기 자원은 **5, 5, 5, 5, 5**입니다.

| 순서 | 건물 | 좌표 |
|---|---|---|
| 1 | 카페 | x=1, y=1 |
| 2 | 절 | x=1, y=0 |
| 3 | 중형 사무실 | x=1, y=2 |
| 4 | 중형 주택 | x=0, y=1 |
| 5 | 중형 공장 | **x=2, y=2** |

최종 **콤보 3쌍, Complaint 0쌍**, 자원 **7, 7, 7, 5, 5**입니다. 자동 팝업만 본 상태는 조회 목표가 미달성이므로 별 2개입니다.

건설 후 **카페 (1,1) → 중형 사무실 (1,2)**을 차례로 클릭해 콤보를 직접 조회합니다. 조회 목표가 달성되어 별 3개가 되어야 하며 자원은 변하지 않아야 합니다. 같은 쌍을 반복 조회해도 개수/자원은 변하지 않습니다. 마지막 건설을 Undo하면 콤보는 2쌍으로 돌아가고 3쌍 목표가 해제됩니다. 조회 직전 턴으로 돌아갔다면 조회 목표도 그 턴의 상태로 복원됩니다.

## Stage5 해답 — 사용자 원본 좌표

초기 자원은 **10, 10, 10, 10, 10**입니다.

| 순서 | 건물 | 좌표 |
|---|---|---|
| 1 | 중형 주택 | x=1, y=1 |
| 2 | 중형 사무실 | x=1, y=2 |
| 3 | 중형 공장 | x=0, y=1 |
| 4 | 절 | x=0, y=0 |
| 5 | 카페 | x=1, y=0 |

최종 **Complaint 3쌍, 콤보 1쌍**, 자원 **11, 10, 10, 9, 9**입니다. 자동 Complaint 팝업만 본 상태는 별 2개입니다.

건설 후 **중형 주택 (1,1) → 중형 사무실 (1,2)**을 차례로 클릭해 Complaint를 직접 조회합니다. 새 Complaint 배경/표식과 빨간 감소 자원 아이콘이 표시되고 별 3개가 되어야 합니다. 직접 조회에는 추가 차감이 없어야 합니다. 중형 주택과 카페를 조회하는 것은 일반 콤보이므로 Complaint 조회 목표를 대신 달성하지 않습니다.

Undo를 두 번 실행해 절 설치 전 상태로 돌아가면 Complaint는 2쌍이며 3쌍 목표는 해제되어야 합니다. Retry는 건물·쌍·직접 조회 목표를 초기화합니다. 마지막 Stage5를 클리어한 결과에는 다음 스테이지 버튼이 없어야 합니다. Exit → Continue에서 최고 별점이 유지되는지도 확인합니다.

## 건물 목록 버튼 확인

카드가 한 화면에 모두 들어가면 좌우 버튼이 보이지 않는 것이 정상입니다. 해상도와 현재 카드 수에 따라 신규 스테이지에서도 전부 들어갈 수 있습니다.

넘침 상태를 별도로 확인하려면 Stage3 Play 중 Hierarchy의 실행 중 HUD 아래 **Gameplay → Building Hand → Viewport**를 선택합니다. Rect Transform이 좌우 Stretch인 경우 **Left와 Right를 각각 일시적으로 크게 증가**시켜 표시 영역을 좁힙니다. 카드 자체의 크기/Content는 수정하지 않습니다. **Play 중인 오브젝트만 수정하고 프리팹에 Apply하지 않습니다.** Stop하면 편집 내용이 원래대로 돌아갑니다.

오른쪽 버튼 클릭 시 한 카드씩 이동, 누른 채 유지 시 0.5초 간격 이동, 손을 떼거나 버튼 밖으로 나가면 정지하는지 확인합니다. 오른쪽 끝에는 왼쪽 버튼만, 왼쪽 끝에는 오른쪽 버튼만 보여야 합니다. 카드 소모/Undo 후 범위를 벗어나지 않아야 하고 Pause 중에는 이동하지 않아야 합니다. 건물 목록의 휠은 이동하지 않아야 하며 스테이지 선택의 휠은 동작해야 합니다.

## 변경 파일 위치

| 용도 | 파일/폴더 |
|---|---|
| 신규 스테이지 | `Assets/_UrbanEquation/Data/Stages/Stage03.asset` ~ `Stage05.asset`, `StageCatalog.asset` |
| 건설 조건 | `Assets/_UrbanEquation/Data/Buildings/B13001_LargeHouse.asset`, `B21001_SmallOffice.asset`, `B23001_LargeOffice.asset` |
| 콤보/Complaint 정의 | `Assets/_UrbanEquation/Data/Combos/ComboDatabase.asset` |
| 로비/Complaint/버튼/스크롤바 이미지 연결 | `Assets/_UrbanEquation/Data/Presentation/GameUiUpdate.asset` |
| 이미지 Import 설정 | `Assets/_UrbanEquation/Sprite/UI/`의 신규 PNG 5종 `.meta` |
| 목표 판정·수동 조회·턴 복원 | `Scripts/Stage/`, `Scripts/Turn/TurnHistoryManager.cs`, `Scripts/Core/GameSessionManager.cs` (모두 `_UrbanEquation` 아래) |
| 버튼 이동·팝업 | `Assets/_UrbanEquation/Scripts/UI/Gameplay/BuildingHandScrollUI.cs`, `ComboPopupUI.cs`, `GameHudUI.cs`, `BoardDetailsUI.cs` |
| 로비/목록 스크롤 | `Assets/_UrbanEquation/Scripts/UI/Popup/GameScreensUI.cs`, `Scripts/UI/Gameplay/GameUiUpdateSettings.cs` (모두 `_UrbanEquation` 아래) |
| 신규 테스트 | `Assets/_UrbanEquation/Tests/Editor/TutorialStageContractTests.cs`, `UiUpdateContractTests.cs` |

새 PNG 원본은 사용자가 main에 올린 파일을 그대로 사용합니다. 필요한 이미지/모델 리소스가 추가로 누락된 부분은 없습니다. 테스트 실패 시 전체 오류 메시지와 파일/행 번호를 공유해 주세요. 화면 문제는 실제 해상도와 함께 알려주시면 됩니다.
