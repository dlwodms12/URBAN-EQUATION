# Phase 3-C — 자원 시스템

## 범위와 상태

5종 자원을 단일 ResourceManager에서 관리하고 건물의 복수 요구/획득 자원, 콤보 보상,
카드 활성화에 사용할 조회, StageData 초기값, 후속 Undo용 자원 복원을 제공합니다.
Phase 3-B는 사용자의 전체 EditMode 17개 통과 및 기존 Prototype 확인으로 완료했습니다.
3-C 코드와 새 EditMode 테스트 14개를 작성했고, Unity 실행 검증은 대기입니다.

## 파일 처리

| 파일 | 처리 |
|---|---|
| Assets/_UrbanEquation/Scripts/Resource/ResourceManager.cs | 수정 |
| Assets/_UrbanEquation/Tests/Editor/ResourceContractTests.cs | 신규 |
| Assets/_UrbanEquation/Tests/Editor/ResourceContractTests.cs.meta | 신규 |
| Docs/DEVELOPMENT_ROADMAP.md | 수정 |
| Docs/PHASE3C_RESOURCES.md | 신규 |

파일 이동·삭제는 없습니다. 기존 ResourceManager.cs.meta와 씬/프리팹은 변경하지 않았습니다.
초기값 직렬화 필드 initialPopulation/initialJobs/initialGoods/initialLogistics/initialTourism을 유지합니다.
initialGoods 필드는 이전 단계와 같이 Money 값에 대응합니다.
임시 Assets/ScriptableObjects 및 현재 Fonts/Materials/Sprite 폴더를 유지합니다.

## 자원 API

| API | 동작 |
|---|---|
| GetResource / TryGetResource | 5종 자원 조회 |
| CanAffordResources / CanAffordBuilding | 요구 자원 전체 검사, 상태 변경 없음 |
| TryConsumeResources | 요구 자원 전체가 충분하면 한 번에 차감 |
| TryAddResources | 여러 자원 증감 적용, 기존 signed Add 동작 유지 |
| TryApplyBuildingResources | 건물 RequiredResources 차감 및 GainedResources 획득 |
| TryApplyComboResources | ComboDefinition.Rewards 적용 |
| TryInitializeFromStage | StageData.InitialResources로 초기화 및 Retry 기준값 보관 |
| ResetResources | 스테이지 기준값 또는 기존 Inspector 초기값으로 초기화 |
| CaptureResourceState | 5종 자원 값을 독립 배열로 반환 |
| TryRestoreResources | 완전한 5종 자원 스냅샷 복원, Retry 기준값 유지 |

기존 GetResource/CanConsume/Consume/Add/ApplyBuildingResource/ResetResources 호출을 유지합니다.
Use Resource Lists가 꺼진 기존 BuildingData는 단일 consume/produce 필드를 계속 사용합니다.
StageData를 전달하지 않으면 기존 Inspector 초기값을 사용하므로 현재 씬 연결 수정은 필요 없습니다.

복수 자원은 검증 후 전체를 적용합니다. 요구 자원은 건설 전 상태에서 충족해야 하며,
같은 건설의 획득 자원으로 부족한 요구 자원을 보충할 수 없습니다.
부족한 요구 자원, 알 수 없는 종류, 중복 종류, 음수 요구/건물 획득량, 정수 범위 초과는
false를 반환하고 상태·이벤트를 변경하지 않습니다. 빈 요구/획득 목록은 허용합니다.
중복 자원은 데이터 정의의 검증 규칙과 같이 거부하며 합산하지 않습니다.
스테이지 초기값은 모든 5종이 정확히 한 번씩 있어야 하고 음수는 허용하지 않습니다.
기존 Add 및 콤보의 음수 증감은 유지하며 임의의 상한이나 0 하한을 추가하지 않습니다.

## 이벤트와 복원

OnResourceChanged(ResourceType, int)는 일반 거래에서 값이 바뀐 자원마다 발생합니다.
그 전에 전체 자원 상태를 적용하므로 첫 이벤트에서도 최종 5종 자원 값을 조회할 수 있습니다.
OnResourcesChanged는 일반 거래가 자원을 변경했을 때 한 번 발생하며 후속 카드 갱신에 사용할 수 있습니다.
빈 목록이나 차감/획득 상쇄로 실제 값이 그대로인 거래는 성공하되 변경 이벤트가 없습니다.
스테이지 초기화·Reset·복원은 전체 갱신을 위해 개별 5개 이벤트와 전체 1개 이벤트를 발생시킵니다.
스냅샷은 독립 복사이며 복원 후에도 스테이지의 Retry 기준값을 덮어쓰지 않습니다.
자원 복원 API만 마련했습니다. 보드·카드·콤보·목표를 포함한 전체 Undo는 3-F입니다.

## 후속 연결

현재 BuildingPlacement의 단일 자원 사전 검사는 3-D에서 CanAffordBuilding 기반으로 교체합니다.
TryApplyBuildingResources의 성공 여부를 카드 제거 및 건설 처리와 함께 연결하는 작업도 3-D입니다.
ComboDefinition을 사용하는 새 콤보 판정·순차 적용은 3-E입니다.
본 개발용 실제 데이터 입력 및 씬 연결은 후속 단계에서 진행합니다.

## 검증

ResourceContractTests 14개는 기존 초기값/호환 호출, Stage 초기화·Retry 기준,
실패 시 무변경, 복수 자원 거래, 자가 보상으로 비용 충당 금지, 조회 무변경,
이벤트에서 전체 상태 조회, signed 콤보 보상, 스냅샷 복사·복원, 잘못된 데이터,
정수 범위 초과, 무변경 거래를 검사합니다.
기존 데이터 8개 + 보드 9개 + 자원 14개 = 전체 EditMode 31개입니다.
정적 점검은 완료했으며 이 환경에는 Unity 또는 C# 컴파일러가 없어 컴파일·테스트 실행은 하지 못했습니다.

Unity를 종료하고 codex/phase3c-resources 브랜치의 최신 변경을 받은 뒤 다시 실행합니다.
Console 컴파일 오류 없음, Test Runner의 EditMode 전체 31개 통과를 확인합니다.
기존 Prototype에서 건설/자원 차감·획득/콤보/Reset 동작과 Console을 확인합니다.
새 테스트는 런타임 데이터 fixture를 생성하므로 Inspector의 새 asset 연결은 필요하지 않습니다.

