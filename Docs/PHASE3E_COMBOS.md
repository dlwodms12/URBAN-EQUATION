# Phase 3-E — 콤보 판정·결과·순차 표시

## 범위와 상태

3-D는 사용자 테스트·Prototype 확인으로 완료했습니다.
3-E는 기획서의 콤보 30행 asset, 4방향 판정, 쌍별 중복 보상 방지,
읽기 전용 결과 기록, 보상 처리와 분리된 표시 큐, 건설 트랜잭션 연결을 구현했습니다.
ComboContractTests 20개 작성 및 정적 점검 완료. 전체 EditMode는 71개입니다.
AI 환경에는 Unity/C# 컴파일러가 없어 새 컴파일·테스트 실행·Prototype 확인은 사용자 검증 대기입니다.

## 파일 처리

아래 Assets 경로의 기준은 Assets/_UrbanEquation입니다.

| 파일 | 처리 및 역할 |
|---|---|
| Scripts/Combo/ComboResult.cs | 신규, 좌표·실제 건물 쌍·이름·설명·복사된 보상·표시 위치 |
| Scripts/Combo/ComboResult.cs.meta | 신규, 스크립트 GUID |
| Scripts/Combo/ComboPresentationQueue.cs | 신규, 현재 결과와 대기 결과·완료 확인 |
| Scripts/Combo/ComboPresentationQueue.cs.meta | 신규, 스크립트 GUID |
| Tests/Editor/ComboContractTests.cs | 신규, 콤보·큐·건설 연동·asset 테스트 20개 |
| Tests/Editor/ComboContractTests.cs.meta | 신규, 테스트 GUID |
| Data/Combos/ComboDatabase.asset | 신규, 기획서 30행의 실제 콤보 데이터 |
| Data/Combos/ComboDatabase.asset.meta | 신규, asset GUID |
| Scripts/Combo/ComboManager.cs | 수정, 판정·중복 방지·보상·결과·복원·표시 이벤트 |
| Scripts/Core/GameSessionManager.cs | 수정, 건설 비용/획득과 콤보 보상을 함께 검증·적용 |
| Scripts/Resource/ResourceManager.cs | 수정, 여러 보상을 순서대로 검증하고 최종 상태만 적용 |
| Scripts/Board/BoardManager.cs | 수정, Reset 이벤트·버전으로 콤보 상태 초기화 |
| Scripts/UI/ComboUI.cs | 수정, 현재 결과 완료 후 다음 결과 표시 및 모든 보상 문구 |
| Docs/DEVELOPMENT_ROADMAP.md | 수정, 3-D 완료·3-E 진행·다음 3-F |
| Docs/PHASE3D_BUILDINGS.md | 수정, 사용자 검증 완료 기록 |
| Docs/PHASE3E_COMBOS.md | 신규, 이번 변경·연결·검증 안내 |

총 16개(신규 9/수정 7). 이동/삭제 없음. 기존 .meta·씬·프리팹 참조 유지.
현재 Fonts/Materials/Sprite 및 임시 Assets/ScriptableObjects 위치를 유지합니다.

## 판정과 데이터

ComboManager.TryConfigure(board, resources, database, out error)로 새 데이터를 연결합니다.
database가 null이면 기존 Inspector의 combos 행을 사용하므로 Prototype의 연결을 유지합니다.
새 asset은 Assets/_UrbanEquation/Data/Combos/ComboDatabase.asset입니다.
실제 Game 씬 연결은 후속 통합 단계에서 수행합니다.

건물을 중심으로 왼쪽→아래→오른쪽→위의 인접 건물을 검사합니다.
대각선·빈 타일·보드 밖은 콤보에 포함하지 않습니다. A+B와 B+A는 같은 데이터 행이며
A+A도 허용합니다. 중복 데이터는 순서 없는 건물 코드 쌍으로 검사합니다.
같은 ComboCode가 서로 다른 쌍에 쓰이는 기획서 행을 보존합니다.
특히 C43001의 대규모 공업 지대(일자리 +3)와 랜드마크(물류 +3)는 별도 행입니다.
보상을 코드에서 추론하지 않고 해당 행의 Rewards를 사용합니다.

동일한 실제 타일 쌍은 보드의 현재 이력에서 한 번만 적용합니다.
같은 콤보 코드라도 다른 타일 쌍이면 각각 결과와 보상이 생깁니다.
각 인접 쌍은 한 번 검사하며 결과로 추가 건물/연쇄 콤보를 생성하지 않습니다.
TryGetAppliedCombo(a, b, out result)는 이미 발생한 쌍을 조회할 뿐 보상을 다시 적용하지 않습니다.

## 보상과 건설 경계

기존 CheckCombos(building)는 TryResolveCombos의 호환 진입점입니다.
직접 판정에서도 모든 보상을 먼저 검증하며 후반 보상의 정수 범위 초과가 앞선 보상만 남기지 않습니다.
기존 OnComboTriggered 이벤트는 보상 항목마다 유지하고 결과 단위 알림은 OnComboResolved입니다.

새 GameSessionManager는 ConfigureCombos(comboManager) 또는 Inspector로 연결합니다.
세션과 콤보는 같은 BoardManager/ResourceManager를 사용해야 합니다.
건물 생성·타일 점유·카드 소비 후 비용/획득 자원과 모든 콤보 보상을 순서대로 검증합니다.
어느 보상에서 실패해도 카드 위치와 타일을 복원하고 임시 건물을 제거하며 자원/결과를 남기지 않습니다.
성공 시 결과 기록까지 반영한 뒤 최종 자원·손패·콤보·건설 이벤트를 발행합니다.
첫 자원 이벤트에서도 보드·손패·전체 최종 자원·콤보 기록을 함께 조회할 수 있습니다.
LastComboResults와 OnBuildResolved는 이번 건설의 읽기 전용 결과를 제공합니다.
목표/랭크 및 완료 턴 이력은 후속 단계에서 연결합니다.
이벤트 핸들러는 예외 없이 반환하도록 작성합니다.

기존 Prototype의 BuildingPlacement는 건설과 CheckCombos를 호출하는 기존 흐름을 유지합니다.
새 세션의 건설+콤보 전체 거래 경계는 Prototype 씬에 아직 연결하지 않았습니다.

## 결과·표시 큐·Reset·복원

ComboResult는 결과 ID, 코드, 이름, 설명, 실제 건물 코드/좌표, 방향,
두 건물의 월드 위치 중간점, 복사된 읽기 전용 Rewards를 보관합니다.
판정 후 데이터 정의를 수정해도 발생한 결과의 문구/보상이 바뀌지 않습니다.

보상은 판정 시 적용하고 표시 완료로 다시 적용하지 않습니다.
CurrentPresentation이 한 결과를 가리키며 PendingPresentationCount는 현재 결과를 제외한 대기 수입니다.
TryCompletePresentation(expected)는 현재 객체와 일치해야 다음으로 넘어갑니다.
Reset 전의 늦은 완료 콜백이 Reset 후 새 결과를 건너뛰지 않습니다.
복수 건설의 결과도 큐 뒤에 추가되며 이미 표시 중인 결과를 덮어쓰지 않습니다.

ComboUI는 결과 큐를 구독하며 기존 displayDuration 후 다음 결과를 표시합니다.
이름/설명과 모든 보상 항목을 표시하고 ResetUI에서 표시 큐를 비웁니다.
BoardManager.ResetBoard/ClearBoard는 결과·중복 적용 기록·큐를 함께 비웁니다.
비활성 중 놓친 Reset도 버전 검사로 반영합니다.
UI의 표시 초기화만으로 이미 지급된 콤보의 중복 방지 기록을 지우지는 않습니다.

CaptureResults는 독립 배열입니다. TryRestoreResults는 같은 매니저·설정에서 발생한 결과만 받고,
결과 ID 순서·쌍 중복·복원된 보드의 두 건물 코드를 검증합니다.
실패는 기존 기록/큐를 보존합니다. 성공은 기록을 복원하되 보상이나 표시를 재실행하지 않습니다.
이는 후속 Undo용 API입니다. 전체 Undo 이력과 버튼은 3-F입니다.

기획서의 위치 팝업, 1.5초 표시+0.5초 페이드, 호버/클릭 상세 패널은 Phase 4입니다.
이번 단계는 필요한 결과/위치/조회/순차 진행 API와 기존 텍스트 UI를 제공합니다.

## 검증과 다음 단계

- 새 테스트 20개: 4방향 순서, 순서 없는 쌍/A+A, 대각선·범위·연쇄 제외,
  잘못된 건물·데이터 거부, 재판정 중복 방지, 결과 복사, FIFO/늦은 완료 거부,
  Reset·복원·오류 복원 보존, 복수 보상 overflow 전체 취소, 기존 행/이벤트 호환,
  세션 최종 상태 알림·실패 전체 복원·참조 불일치·재진입 거부, 문구 및 실제 asset 가져오기.
- 기존 테스트 51개 + 신규 20개 = 전체 EditMode 71개.
- 정적 확인: C# 구문 구분자/공개 타입 중복, 직렬화 필드/호환 API,
  자원 적용·알림 경계, YAML 30행/쌍/보상 및 스크립트 GUID, 신규 메타 GUID.
- Unity 실행 확인: Unity 종료 → codex/phase3e-combos 최신 변경 받기 → Unity 재실행,
  Console 컴파일 무오류·EditMode 71개·기존 Prototype 건설/자원/콤보/Reset 정상 작동.
- 복수 콤보가 한 번에 생기는 배치를 만들어 순차 표시를 확인하고,
  표시 도중 Reset 후 남은 콤보 문구가 다시 나타나지 않는지 확인합니다.
- 테스트는 임시 fixture를 만들므로 기존 Inspector를 변경하지 않습니다.
- 검증 후 다음 개발: Phase 3-F — 턴 완료 Snapshot 및 다단계 Undo.
