# Phase 3-D — 건물 카드 및 건설

## 범위와 상태

StageData.BuildingCards를 순서대로 펼쳐 Count만큼 개별 카드를 생성합니다.
같은 BuildingData도 CardId가 다른 독립 카드이며 사용한 카드만 제거하고 나머지 순서를 유지합니다.
자원에 따른 카드 활성/비활성, 드래그 프리뷰, 타일·점유·자원 재검사 및 실제 건설 처리를 구현했습니다.
3-C는 사용자 테스트/Prototype 확인으로 완료했습니다.
3-D 정적 점검 및 BuildingContractTests 20개 작성 완료, Unity 실행 검증은 대기입니다.

## 파일 처리

기준 경로는 Assets/_UrbanEquation입니다.

| 파일 | 처리 및 역할 |
|---|---|
| Scripts/Building/BuildingCardState.cs | 신규, 개별 카드의 고정 ID 및 BuildingData |
| Scripts/Building/BuildingHandManager.cs | 신규, 카드 생성·순서·활성 조회·복원 |
| Scripts/Building/BuildingPlacementValidator.cs | 신규, 카드 존재·보드 범위·점유·타일 종류·자원 검사 |
| Scripts/Building/BuildingPlacementController.cs | 신규, 드래그·프리뷰·취소·배치 요청 |
| Scripts/Core/GameSessionManager.cs | 신규, 건설 성공/실패 상태와 이벤트 경계 |
| Scripts/UI/Gameplay/BuildingCardUI.cs | 신규, 이름·이미지·비활성 Overlay·포인터 입력 |
| Scripts/UI/Gameplay/BuildingHandUI.cs | 신규, 카드별 UI 생성·삭제·표시 순서 및 활성 갱신 |
| Tests/Editor/BuildingContractTests.cs | 신규, 카드·건설·프리뷰·UI·호환 테스트 20개 |
| 위 신규 C# 파일의 .meta 8개 | 신규, 고유 GUID |
| Scripts/Resource/ResourceManager.cs | 수정, 세션 내부에서 상태 적용 후 이벤트를 지연 발행 |
| Scripts/Board/BuildingPlacement.cs | 수정, 기존 Prototype 복수 자원 검사·선택 차단 및 안전한 실패 처리 |
| Scripts/UI/BuildingSlotUI.cs | 수정, 자원에 따른 기존 버튼 활성 갱신·비활성 포인터 차단 |
| Docs/DEVELOPMENT_ROADMAP.md | 수정, 3-C 완료 및 3-D 진행 기록 |
| Docs/PHASE3C_RESOURCES.md | 수정, 사용자 검증 완료 기록 |
| Docs/PHASE3D_BUILDINGS.md | 신규, 변경·연결·검증 안내 |

총 22개 파일입니다. 파일 이동/삭제와 씬/프리팹 변경은 없습니다.
기존 BuildingPlacement/BuildingDatabase/BuildingSlotUI/ResetManager는 Prototype 참조를 보존합니다.
새 런타임의 연결이 완료될 때 교체/삭제합니다. 기존 직렬화 필드와 .meta GUID는 유지했습니다.
현재 Fonts/Materials/Sprite 및 임시 Assets/ScriptableObjects 위치를 유지합니다.

## 카드와 활성 상태

BuildingHandManager.TryInitializeFromStage(stage, resources, out error)로 초기화합니다.
카드 데이터만 검증하며 보드/목표/초기 자원을 대신 초기화하지 않습니다.
빈 초기 목록, 0 이하 수량, 잘못된 BuildingData, 같은 코드의 서로 다른 정의는 실패하고 기존 손패를 보존합니다.
같은 정의를 여러 번 나열하는 것은 허용하며 문서에 입력한 순서로 펼칩니다.

Cards는 읽기 전용 목록, TryGetCard는 ID 조회, IsCardAvailable은 모든 요구 자원 충족 여부입니다.
허용 타일이 현재 남아 있는지 등의 추가 카드 비활성 조건은 도입하지 않았습니다.
자원 변경 시 OnCardAvailabilityChanged로 UI를 갱신합니다.
OnHandChanged는 생성/성공한 건설/복원/Reset 등 목록 갱신을 알립니다.

CaptureCards는 독립 배열, TryRestoreCards는 해당 초기 손패의 카드 ID/객체를 검증하고 입력 순서를 복원합니다.
중복/다른 손패의 카드/누락 객체는 거부합니다. 빈 손패 복원은 가능합니다.
ResetCards는 초기 카드와 순서를 복원합니다.
이것은 카드 상태 복원 API이며 전체 Undo 버튼/이력은 3-F입니다.

## 건설 트랜잭션

GameSessionManager.Configure(board, resources, hand, buildingPrefab) 또는 Inspector로 참조를 연결합니다.
손패와 세션은 같은 ResourceManager를 사용해야 합니다.
TryCommitBuild(cardId, coordinate, out building, out error)는 다음을 수행합니다.

1. 카드 존재, 현재 보드 범위, 타일 점유/종류, 전체 요구 자원 및 프리팹 연결 검사
2. 비활성 임시 루트 아래에 건물/외형 생성
3. 타일 점유 및 해당 카드 제거
4. 요구/획득 자원을 전체 적용
5. 건물을 타일 아래 활성화하고 자원·손패·건설 성공 이벤트 발행

자원 데이터 오류/정수 범위 초과 등 4번 실패 시 타일 점유와 정확한 카드 위치를 복구합니다.
실패한 건물과 임시 루트는 정리되며 자원/손패/성공 이벤트는 없습니다.
자원 이벤트에서도 건물·손패·최종 자원 상태를 함께 조회할 수 있습니다.
처리 중 중복 건설 요청은 거부하고 완료 후 다음 건설을 허용합니다.
사용자 작성 이벤트 핸들러는 일반 Unity 이벤트와 같이 예외 없이 반환하도록 작성해야 합니다.

ResourceManager의 기존 공개 API는 그대로이며 내부 Deferred 메서드만 추가했습니다.
OnBuildingCommitted는 건설 성공 알림입니다.
후속 3-E에서 콤보 결과, 3-G에서 목표, 3-F에서 완료 턴 이력 처리에 연결합니다.
이번 단계에서 전체 완료 턴/게임 진행/저장 시스템을 구현한 것으로 취급하지 않습니다.

## 프리뷰 및 UI 코드

BuildingPlacementController.Configure(session, camera)로 연결합니다.
TryBeginDrag(cardId), UpdatePointer(screenPosition), TryFinishDrag(screenPosition, out error),
CancelDrag를 제공합니다. 실제 UI 입력은 EventSystem의 포인터 이벤트를 사용하므로
구/신 Input System 중 특정 백엔드에 직접 의존하지 않습니다.

프리뷰의 모든 Collider(비활성 자식 포함)를 끄고 Rigidbody 충돌을 비활성화합니다.
현재 보드 소속 타일만 검사하며 유효한 타일에만 Highlight를 켭니다.
프리뷰와 성공한 건물은 타일 위치에 맞추고 보드 높이를 사용합니다.
빈 공간/다른 UI 위에서 놓거나 드래그 중 자원이 부족해지면 카드 소비 없이 취소합니다.
손패가 재초기화되어 같은 ID의 카드 객체가 달라져도 이전 드래그를 취소합니다.
배치 확정 시 현재 상태를 다시 검사합니다.

BuildingCardUI는 BuildingCardState를 전달받아 표시하고 비활성 카드는
검은색 alpha 0.5 Overlay와 버튼 비활성화로 표시합니다.
포인터 처리에서도 활성 상태를 검사하므로 Button 비활성만으로 입력 차단을 가정하지 않습니다.
BuildingHandUI는 CardId별 뷰를 유지해 사용 카드만 삭제하고 SetSiblingIndex로 순서를 갱신합니다.
Content에 HorizontalLayoutGroup을 연결하면 사용 카드가 빠진 공간을 나머지 카드가 채웁니다.
리소스 필드/Overlay/Button/이미지/이름 및 LayoutGroup의 실제 프리팹 제작은 Phase 4입니다.
호버 정보 팝업 및 콤보 순차 UI도 이번 구현 범위 밖입니다.

## 기존 Prototype 호환

기존 4개 BuildingData가 Use Resource Lists=false일 때 단일 consume/produce 값을 계속 사용합니다.
기존 TileData 없는 8x8 보드는 기존 데이터 건설을 허용합니다.
새 목록 데이터는 TileData가 있어야 타일 조건을 검사하여 건설합니다.
기존 수량/콤보/StageClear/Reset 흐름과 기존 Inspector 참조는 유지합니다.
기존 버튼은 이제 자원 부족 시 비활성화되고 자원이 회복되면 다시 활성화됩니다.
기존 초기값 필드와 GetRemainingCount/GetMaxCount/ResetBuildings 등 공개 호출은 유지했습니다.

## 검증 및 후속 작업

새 테스트 20개 + 이전 데이터 8개/보드 9개/자원 14개 = 전체 EditMode 51개입니다.
테스트는 순서·독립 ID·불완전 초기화 보존·자원 활성 이벤트/구독 해제·스냅샷 복사/복원,
범위/타일/점유/자원 거부, 중간 카드 소비, overflow 실패 복원,
전체 상태의 이벤트 조회/중복 요청 거부, 연결 누락/자원 참조 불일치,
카드 재사용 거부, Collider 비활성/취소, 손패·자원 변경 중 드래그 취소,
보드 외부 놓기, 실제 Physics raycast 배치, UI 삭제/순서/Overlay/입력 차단,
기존 Prototype 카드 활성 호환을 검사합니다.

이 환경에는 Unity/C# 컴파일러가 없어 실제 컴파일/테스트 실행은 하지 못했습니다.
Unity를 종료한 뒤 codex/phase3d-building-cards 브랜치의 최신 변경을 받습니다.
재실행 후 컴파일·Console, EditMode 51개, 기존 Prototype 건설/자원/콤보/Reset을 확인합니다.
부족한 자원의 건물 버튼이 비활성화되고 자원 회복/Reset 후 다시 선택되는지도 확인합니다.
새 테스트는 필요한 데이터/컴포넌트를 임시 생성하므로 Inspector를 변경할 필요가 없습니다.
일반 MonoBehaviour의 런타임 콜백에 의존하지 않도록 테스트에서는 필요한 Awake/OnEnable/OnDisable을
명시적으로 호출합니다. 런타임 코드에는 ExecuteAlways를 추가하지 않습니다.
참고: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ExecuteAlways.html

다음은 Phase 3-E(콤보 판정/결과/순차 연출 데이터)입니다.
실제 BuildingData 15종/Stage 1·2 입력과 새 게임 씬 통합은 예정대로 후속 단계에서 진행합니다.
