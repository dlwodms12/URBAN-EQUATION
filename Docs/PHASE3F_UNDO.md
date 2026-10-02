# Phase 3-F — 완료 턴 Snapshot·다단계 Undo

## 범위와 상태

건설 및 콤보 처리 후 완료 턴을 저장하고 직전 완료 턴으로 복원합니다.
A→B→C 건설 후 C→B→A까지 Undo할 수 있으며 빈 초기 보드로 돌아가지 않습니다.
Undo 후 새 건설은 되돌리기 전의 이후 경로를 버리고 새로운 완료 턴을 기록합니다.
Redo와 실행 중 상태의 영구 저장은 구현하지 않습니다.

2026-10-03T04:18:29+09:00: 사용자가 전체 95개 테스트 통과와 기존 Prototype 정상 동작을 확인했습니다.
3-F 검증 및 누적 3-E 테스트 확인을 완료 처리했습니다.
이 문서는 3-F 당시의 95개 구성입니다. 최신 3-G 변경/123개 검증은 Docs/PHASE3G_STAGE_GOALS.md를 봅니다.
AI가 Unity를 직접 실행한 것으로 기록하지 않습니다.

## 파일 처리

Assets 경로의 기준은 Assets/_UrbanEquation입니다.

| 파일 | 처리 및 역할 |
|---|---|
| Scripts/Turn/GameStateSnapshot.cs | 신규, 완료 턴 및 배치 건물의 읽기 전용 Snapshot |
| Scripts/Turn/TurnHistoryManager.cs | 신규, 완료 턴 기록·첫 건설 제한·연속 Undo·이력 범위 관리 |
| Scripts/Stage/StageProgressState.cs | 신규, 클리어·목표 플래그·랭크·NEXT STAGE 상태 복사 |
| Scripts/UI/Gameplay/UndoButtonUI.cs | 신규, Undo 요청과 버튼 활성 갱신 |
| Tests/Editor/TurnHistoryContractTests.cs | 신규, 상태·이력·실패·UI 회귀 테스트 24개 |
| 위 신규 C# 파일의 .meta 5개 | 신규, 고유 GUID |
| Scripts/Core/GameSessionManager.cs | 수정, 완료 턴 기록·복원 거래·재진입 차단 |
| Scripts/Board/BoardManager.cs | 수정, 배치 캡처·복원 건물 사전 준비·상태 교체 |
| Scripts/Resource/ResourceManager.cs | 수정, 자원 복원 검증/적용/알림 분리 |
| Scripts/Building/BuildingHandManager.cs | 수정, 손패 복원 검증/적용/알림 및 초기화 버전 |
| Scripts/Combo/ComboManager.cs | 수정, 복원될 보드 기준 결과 검증 및 지연 알림 |
| Scripts/Combo/ComboPresentationQueue.cs | 수정, 전체 복원 중 표시 알림 지연을 위한 내부 초기화 |
| Scripts/Building/BuildingPlacementController.cs | 수정, 복원 시작 시 드래그·프리뷰 취소 |
| Scripts/Stage/StageManager.cs | 수정, 진행 상태 캡처·적용·양방향 변경 알림 |
| Scripts/UI/ClearUI.cs | 수정, 클리어 이전으로 복원할 때 기존 표시 숨기기 |
| Docs/DEVELOPMENT_ROADMAP.md | 수정, 3-E 보고 범위·3-F 진행·다음 3-G |
| Docs/PHASE3E_COMBOS.md | 수정, 51개 보고와 전체 실행 확인 대기 기록 |
| Docs/PHASE3F_UNDO.md | 신규, 변경·연결·검증 안내 |

총 22개(신규 11/수정 11). 파일 이동/삭제 없음. 기존 .meta·씬·프리팹 참조 유지.
현재 Fonts/Materials/Sprite 및 임시 Assets/ScriptableObjects 위치를 유지합니다.

## 연결과 완료 턴

먼저 새 GameSession의 보드·자원·손패·프리팹·콤보를 초기화합니다.
TurnHistoryManager.TryConfigure(session, stageManager, out error)로 History를 연결합니다.
StageManager를 연결할 때는 세션과 같은 ResourceManager를 사용해야 합니다.
스테이지 상태를 사용하지 않는 테스트/구성에서는 stageManager=null을 허용합니다.
다른 History가 이미 연결된 세션 또는 참조 불일치·미초기화는 거부하며 기존 설정/이력을 보존합니다.
Inspector로 연결한 History도 초기화된 세션을 대상으로 Start에서 설정합니다.

세션의 성공한 건설과 콤보 처리 후, OnBuildingCommitted 알림 뒤에 Snapshot을 기록하고
OnBuildResolved를 발행합니다. 거부/실패한 건설은 완료 턴을 만들지 않습니다.
자원 조회/수정이나 단순 손패 복원만으로 턴이 기록되지는 않습니다.
Snapshot은 해당 완료 시점의 전체 배치, 5종 자원, 남은 카드 객체와 순서,
전체 콤보 기록, 마지막 건설 콤보 결과, 연결된 스테이지 진행 상태를 보관합니다.
컬렉션은 복사된 읽기 전용 목록이며 GameObject/Transform/Visual 객체를 저장하지 않습니다.
BuildingData와 초기 CardId 객체는 런타임의 정의 참조이므로 실행 중 정의를 바꾸지 않습니다.

이번 단계의 StageProgressState는 상태 저장/복원 형식입니다.
StageManager.TryApplyProgressState로 전달한 목표 플래그·Rank(0~3)·NEXT STAGE·클리어 상태를 보존합니다.
기존 Prototype의 Population>=4 검사와 이벤트는 유지합니다.
StageData 목표 자동 판정·랭크 계산 및 실제 NEXT STAGE 버튼은 3-G/Phase 4입니다.
3-G에서 목표 평가를 완료 턴 캡처 전에 연결합니다.

## Undo와 복원 실패

History.Count는 저장된 완료 턴 수입니다. 0/1개일 때 CanUndo=false입니다.
TryUndo(out error)는 끝에서 두 번째 완료 턴을 복원한 뒤 마지막 Snapshot을 제거합니다.
현재 단계에는 Redo 버튼/이력을 제공하지 않습니다.
새 건설의 TurnNumber는 남은 History에 이어지며 이전 경로의 Snapshot을 재사용하지 않습니다.

복원할 건물을 비활성 임시 루트에 먼저 생성합니다.
건물 정의/코드/타일 조건, 카드 소속·순서·중복, 5종 자원, 해당 건물 쌍의 콤보 소유/ID/코드,
마지막 건설 결과 및 스테이지 상태를 모두 검증한 뒤 상태를 교체합니다.
준비 실패는 현재 건물 인스턴스·자원·손패·이력·표시 큐를 보존하고 임시 복원 건물을 정리합니다.

성공 시 OnStateRestoring으로 드래그를 취소하고, 보드·자원·손패·콤보·스테이지·History를 적용합니다.
그 뒤 자원(5종)·손패·콤보·스테이지·History·OnStateRestored 순서로 알립니다.
첫 복원 알림에서도 모든 최종 데이터 상태와 줄어든 History를 읽을 수 있습니다.
복원 중 재진입 Undo/건설/History 재설정 요청은 거부합니다.
사용자 이벤트 핸들러는 상태를 직접 교체하거나 예외를 던지지 않도록 작성합니다.

기존 건물 객체는 정리하고 타일 위치/높이에 새 건물 객체를 복원하므로
외부 UI는 기존 BuildingInstance 객체를 영구 보관하지 않고 좌표/결과를 다시 조회합니다.
콤보는 Snapshot 자원 값을 복원하며 보상을 다시 계산/지급하지 않습니다.
콤보 결과/쌍 기록은 복원하고 현재/대기 표시 큐는 비웁니다.
이전 턴의 늦은 완료 콜백은 객체 일치 검사를 통과하지 못합니다.
클리어 이전 턴으로 돌아가면 ClearUI도 숨깁니다.

Board Reset/교체, 손패 재초기화, 세션/콤보 재설정은 이전 이력을 무효화합니다.
비활성 중 놓친 Reset도 버전 검사로 반영합니다.
TryClearHistory는 보드/자원을 바꾸지 않고 기록만 지우며 거래 중에는 거부합니다.
Undo로 자원의 Retry 초기값을 덮어쓰지 않습니다.

## UI와 Prototype 경계

UndoButtonUI.Configure(history, button) 또는 Inspector로 버튼을 연결합니다.
History 변경에 따라 버튼을 갱신하며 첫 건설 상태로 돌아오면 비활성화합니다.
최종 HUD/프리팹 제작과 실제 Game 씬 연결은 예정된 Phase 4/통합 단계입니다.
기존 Prototype은 기존 BuildingPlacement/ResetManager 흐름을 사용하므로 새 History/Undo 버튼을 자동으로 붙이지 않습니다.
새 Undo 동작은 EditMode의 임시 GameSession fixture로 검증합니다.
이번 작업을 확인하려고 기존 Prototype의 Inspector를 바꿀 필요는 없습니다.

## 검증 절차

1. Unity 종료 후 codex/phase3f-turn-history 브랜치로 변경하고 최신 변경을 받습니다.
2. Unity를 재실행하고 Console 컴파일 오류가 없는지 확인합니다.
3. Test Runner의 검색/선택 필터를 해제하고 EditMode에서 Run All을 실행합니다.
4. 아래 6개 테스트 클래스와 전체 95개를 확인합니다.
5. 기존 Prototype 건설/자원/복수 콤보/클리어/Reset 호환을 확인합니다.

| 클래스 | 예상 테스트 수 |
|---|---:|
| DataContractTests | 8 |
| BoardContractTests | 9 |
| ResourceContractTests | 14 |
| BuildingContractTests | 20 |
| ComboContractTests | 20 |
| TurnHistoryContractTests | 24 |
| 전체 | 95 |

새 테스트는 fixture를 생성하며 Inspector 변경 없이 실행합니다.
클래스가 누락되면 현재 커밋/브랜치·Assets/_UrbanEquation/Tests/Editor의 파일 존재·Test Runner 필터를 확인합니다.
정적 점검: C# 구문 구분자/타입 중복, 기존 필드/API, 복원 사전 검증/알림 순서,
메타 GUID, 테스트 enum 문자열 및 전체 테스트 수.
3-G 목표·랭크·NEXT STAGE 코드를 작성했습니다. 최신 안내는 Docs/PHASE3G_STAGE_GOALS.md입니다.
