# Phase 3-G — 스테이지 목표·랭크·NEXT STAGE·결과

## 상태와 범위

2026-10-03T04:18:29+09:00 사용자가 3-F 누적 테스트 95개 통과와 Prototype 정상 작동을 확인했습니다.
다음 3-G 코드를 작성하고 StageGoalContractTests 28개를 추가했습니다. 전체 예상 EditMode는 **123개**입니다.
현재 브랜치: **codex/phase3g-stage-goals**.
기준: codex/phase3f-turn-history, 2ad223d4268a5f9554b399e8b4b1c808a10e11aa.
AI 환경에는 Unity/C# 컴파일러가 없어 이번 단계의 실제 컴파일/테스트/Prototype 실행은 사용자 검증 대기입니다.

기획서 8/24/28/31/33페이지의 필수/추가 목표, 계속 건설, 메인 NEXT STAGE 완료 시점을 기준으로 구현했습니다.
추가 목표는 선택 달성 대상입니다. 각 StageData에는 추가 목표 정의 2개가 반드시 필요합니다.

| 목표 | 달성 조건 |
|---|---|
| ResourceAtLeast | 지정한 5종 자원 중 하나의 현재 값 >= 목표 값 |
| AllCardsUsed | 남은 건물 카드 수 = 0 |
| AdjacentBuildings | 지정한 건물 코드 A/B의 서로 다른 건물이 좌우/상하로 인접 |

인접은 순서 없는 코드 쌍입니다. 대각선·거리 2 이상은 제외하며 A+A는 두 건물이 필요합니다.
플래그 순서는 필수/추가1/추가2입니다.
필수 미달성은 추가 목표 달성 여부와 무관하게 Rank 0, 필수 달성은 Rank 1 + 추가 달성 수(최대 3)입니다.
음수를 포함한 현재 자원 값은 평가할 수 있으며 자원 목표 값은 음수로 설정할 수 없습니다.

## 파일 처리

Assets 기준 경로는 Assets/_UrbanEquation입니다.

| 파일 | 처리 및 역할 |
|---|---|
| Scripts/Stage/StageGoalEvaluator.cs | 신규, 복사된 배치/자원/카드 수의 순수 목표·랭크 판정 |
| Scripts/Stage/StageResult.cs | 신규, 완료 시 번호·이름·랭크·목표/설명·자원을 복사하는 결과 |
| Scripts/UI/Gameplay/NextStageButtonUI.cs | 신규, 메인 NEXT STAGE 요청 및 버튼 활성 갱신 |
| Tests/Editor/StageGoalContractTests.cs | 신규, 목표/거래/결과/Undo/UI/Prototype 회귀 28개 |
| 위 신규 C#의 .meta 4개 | 신규, 고유 GUID |
| Scripts/Stage/StageManager.cs | 수정, 데이터 목표 평가·완료 요청·상태/결과·기존 Prototype 호환 |
| Scripts/Core/GameSessionManager.cs | 수정, 단계 연결·건설/콤보 후 목표 적용·실패 복원·완료 후 건설 차단 |
| Scripts/Turn/TurnHistoryManager.cs | 수정, Stage 설정/Reset 변경으로 이전 이력 무효화 |
| Scripts/Data/StageGoalData.cs | 수정, 인접 목표의 양수 건물 코드 검사 |
| Docs/DEVELOPMENT_ROADMAP.md | 수정, 3-E/F 검증 완료 및 최신 123개 구성 |
| Docs/PHASE3F_UNDO.md | 수정, 95개 사용자 검증 완료와 최신 안내 연결 |
| Docs/PHASE3G_STAGE_GOALS.md | 신규, 이번 변경·연결·검증 안내 |

총 15개(신규 9/수정 6). 파일 이동/삭제 및 씬/프리팹 변경 없음.
기존 메타 참조, Fonts/Materials/Sprite, 임시 Assets/ScriptableObjects 위치를 유지합니다.

## 연결 순서

1. 같은 StageData로 BoardManager/ResourceManager/BuildingHandManager를 초기화합니다.
2. GameSessionManager.Configure(board, resources, hand, prefab), ConfigureCombos(combo)를 호출합니다.
3. StageManager.TryConfigure(stageData, board, resources, hand, out error)를 호출합니다.
4. GameSessionManager.TryConfigureStage(stageManager, out error)로 연결합니다.
5. TurnHistoryManager.TryConfigure(session, stageManager, out error)로 History를 연결합니다.
6. NextStageButtonUI.Configure(stageManager, button)로 메인 버튼을 연결합니다.

초기화하지 않은 보드/손패, 다른 StageData/매니저 참조, 잘못된 목표는 거부하며 기존 설정을 보존합니다.
이미 연결된 StageManager를 다른 세션에 동시에 공유할 수 없습니다.
Inspector Start를 사용할 때도 보드/손패 초기화 후 목표 설정 순서를 보장해야 합니다.
실제 공통 Game 초기화/Retry 흐름은 3-I/3-J에서 연결합니다.
직렬화 참조만 넣어놓고 초기화 순서를 자동 해결한 것으로 간주하지 않습니다.

## 턴 처리와 Undo

건물/손패 변경 → 건물 비용/획득 및 전체 콤보 보상 거래 → 목표·랭크·NEXT STAGE 적용 →
자원/손패/콤보/목표 알림 → OnBuildingCommitted → 완료 Snapshot → OnBuildResolved 순서입니다.
첫 자원 이벤트에서도 최종 목표 상태를 읽을 수 있습니다.
자원 거래 또는 목표 준비 실패는 카드/타일/자원을 복원하고 목표/콤보/History 알림을 발행하지 않습니다.

Snapshot에 목표 플래그·랭크·NEXT STAGE 및 실제 클리어 상태를 보존합니다.
Undo는 이전 완료 턴을 복원하여 현재 판정으로 덮어쓰지 않습니다.
복원은 완료 결과 이벤트를 발행하거나 콤보 보상을 다시 지급하지 않습니다.
클리어 후 Undo는 이전 미클리어 턴으로 돌아가고 CurrentResult를 비웁니다.
최종 팝업의 입력 허용/화면 전환 정책은 후속 흐름 단계에서 적용합니다.

Stage 재설정/Reset은 ConfigurationVersion을 변경하고 이전 History를 무효화합니다.
일반 목표 갱신/Undo는 설정 버전을 바꾸지 않습니다.
BuildingData/StageData 등 정의는 플레이 중 변경하지 않습니다. 변경이 필요하면 TryConfigure로 다시 설정합니다.
리소스만 외부에서 수정하는 디버그 코드에는 TryRefreshGoals(out error)를 명시적으로 호출합니다.
일반 건설은 세션이 갱신하며 완료 요청도 현재 상태를 재평가합니다.
사용자 이벤트 핸들러는 게임 상태를 직접 교체하거나 예외를 던지지 않아야 합니다.

## NEXT STAGE와 결과

필수 달성은 NextStageAvailable=true이며 IsCleared=false입니다.
이 상태에서 계속 건설하여 추가 목표와 더 높은 랭크를 노릴 수 있습니다.
메인 NEXT STAGE는 TryCompleteStage(out StageResult, out error)를 호출합니다.
현재 필수 목표를 재확인한 뒤 IsCleared=true, NextStageAvailable=false로 확정합니다.
이후 새 건설은 거부합니다. OnStateChanged/OnStageCleared/OnStageCompleted(result)를 한 번 발행합니다.
반복 요청은 같은 결과를 반환하고 이벤트를 다시 발행하지 않습니다.
거래/완료 이벤트 처리 중 재진입 요청은 거부합니다.

StageResult는 StageNumber/StageName/Rank/GoalStates/GoalDescriptions/Resources를 값으로 복사합니다.
읽기 전용 컬렉션이며 GameObject/StageData/BuildingData 객체를 결과에 보관하지 않습니다.
이번 단계에서는 저장하지 않습니다. 3-H가 진행도/최고 랭크 저장을, 3-I가 클리어 팝업 후 다음 스테이지 흐름을 연결합니다.
메인 NEXT STAGE와 클리어 팝업의 NEXT STAGE는 서로 다른 흐름입니다.
최종 목표 패널의 색/취소선과 클리어 팝업·프리팹은 Phase 4입니다.

## Prototype와 검증

StageData를 설정하지 않은 기존 StageManager는 Population>=4, CheckStageClear/ResetStage 및 기존 이벤트를 유지합니다.
기존 Prototype에는 새 목표/버튼/Undo를 자동 연결하지 않습니다. 확인을 위해 Inspector를 바꿀 필요가 없습니다.
새 테스트는 임시 Board/Hand/Combo/Stage/History/UI fixture를 만들어 실행합니다.

1. Unity 종료 → GitHub Desktop Fetch origin → codex/phase3g-stage-goals 선택 → 필요 시 Pull origin.
2. Unity 재실행 후 Console 컴파일 오류가 없는지 확인합니다.
3. Test Runner 필터를 해제하고 EditMode Run All을 실행합니다.
4. 아래 7개 클래스와 전체 **123개**를 확인합니다.
5. 기존 Prototype의 건설/자원/복수 콤보/인구 4 클리어/Reset 호환을 확인합니다.

| 클래스 | 예상 테스트 수 |
|---|---:|
| DataContractTests | 8 |
| BoardContractTests | 9 |
| ResourceContractTests | 14 |
| BuildingContractTests | 20 |
| ComboContractTests | 20 |
| TurnHistoryContractTests | 24 |
| StageGoalContractTests | 28 |
| 전체 | 123 |

새 28개는 자원 경계/음수 값, 카드 수, 4방향/쌍/거리/큰 좌표, 잘못된 목표/컨텍스트,
랭크 조합, 설정 실패 보존, 콤보 후 첫 알림, Snapshot/Undo, 완료 조건/값 복사/중복/재진입,
외부 변경 재평가, 완료 후 Undo, Stage 재설정/Reset, 실패 턴, 버튼 구독, 기존 인구 4 흐름을 검사합니다.
테스트 클래스가 누락되면 브랜치와 실제 Editor 파일 목록, Console/필터를 확인합니다.
다음 단계는 사용자 검증 완료 후 **3-H: 진행도·해금·최고 랭크 저장**입니다.
