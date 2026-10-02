# URBAN EQUATION 개발 로드맵

기준: 2026-10-01 수정 기획서(33페이지)와 사용자 확정 답변.
작업 시작 전에 이 파일과 현재 단계의 변경 안내(`Docs/PHASE3I_GAME_FLOW.md`)를 읽습니다.
전체 상세 로드맵: `dlwodms12/LJE_GPT_Log`의
`memory/entries/MEM-20261001-0610-urban-equation-development-roadmap-v1.md`.

## 진행 순서

- [x] Phase 1: 기획 분석·질문·작업 순서 확정
- [x] Phase 2: 폴더 구조 설계 및 사용자 이동 작업 확인
- [x] Phase 3-A: 공통 데이터 정의 (사용자 Unity 컴파일·EditMode 8개·Prototype 검증 완료)
- [x] Phase 3-B: StageData 기반 보드·타일 생성 (사용자 EditMode 17개·Prototype 검증 완료)
- [x] Phase 3-C: 5종 자원·요구/획득 자원 목록 처리 (사용자 EditMode 31개·Prototype 검증 완료)
- [x] Phase 3-D: 카드 순서·수량·드래그·건설 (사용자 EditMode 51개·Prototype 검증 완료)
- [x] Phase 3-E: 콤보 판정·결과·순차 연출 데이터 (누적 EditMode 95개·Prototype 사용자 검증 완료)
- [x] Phase 3-F: 턴 완료 Snapshot·다단계 Undo (사용자 전체 EditMode 95개·Prototype 검증 완료)
- [x] Phase 3-G: 목표·랭크·NEXT STAGE 판정 (사용자 전체 EditMode 123개·Prototype 검증 완료)
- [x] Phase 3-H: 진행도·해금·최고 랭크 저장 (사용자 검증 확인)
- [ ] Phase 3-I: Lobby/Game 화면 상태·New Game/Continue·Retry·Exit (코드·회귀40개 작성, 전체195개 Unity 검증 대기)
- [ ] Phase 3-J: 실제 설계 데이터 및 Stage 1/2 통합 검증
- [ ] Phase 4: 프리팹·UI·최종 플레이 검증

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

2026-10-03T05:15:18+09:00: 사용자가 이전 3-H 검증을 확인하고 다음 단계 진행을 요청했습니다.
2026-10-03T06:18:42+09:00: 중단된 3-I 작업의 상태 점검 및 재개를 요청하셨습니다.
작업 폴더에는 3-I 초안이 남아 있었고 원격은 3-H 기준 커밋 그대로임을 확인한 후 작업을 이어 갔습니다.

현재 브랜치: **codex/phase3i-game-flow**.
기준 3-H 커밋: b27fd7011472ef0ee624874cad1ea2e2c29028b0.
변경/연결/검증 상세: Docs/PHASE3I_GAME_FLOW.md.

화면 상태 관리자, 스테이지 목록, 초기화 연결 및 명시적으로 연결하는 임시 디버그 UI를 작성했습니다.
Play는 기존/손상된 저장이 있을 때 확인창만 열고, 새 게임 확정 후에만 진행도를 초기화합니다.
Continue는 유효 진행도로 Lobby 내부 Stage Select에 들어가며, 선택한 단계는 항상 처음부터 시작합니다.
안내·Pause·Clear·Lobby에서는 건설/드래그/Undo/클리어 확정이 차단됩니다.
필수 목표 충족 후에도 건설을 계속할 수 있고 NEXT STAGE를 누르면 Clear 결과 화면에 들어갑니다.
중단 취소는 현재 진행을 보존합니다. 중단 확정은 건물·자원 변화·카드 소비·콤보/연출·목표·이력을 초기화하고 Lobby로 돌아갑니다.
Retry 및 Next Stage는 데이터 기반 재초기화 후 안내 화면을 보여 줍니다. 마지막 단계에서는 Next를 제공하지 않습니다.
완료 저장 실패는 결과를 유지하고 Retry/Next/Stage Select 이탈을 막습니다. 저장 재시도 성공 후 이동할 수 있습니다.
Exit는 확인 후 종료 요청 이벤트를 발행합니다.

Lobby/Game 목적지 요청 이벤트를 제공하며, 이번 단계에서 실제 씬을 만들거나 LoadScene을 호출하지 않습니다.
최종 씬 로더·UI·종료 호출 연결은 Phase 4입니다. 실제 Stage 1/2 데이터는 다음 3-J입니다.
기존 Prototype에는 새 Flow/저장/Undo/목표 UI를 자동 추가하지 않습니다.
Stage 1/2 실제 asset, 씬 및 최종 UI를 통한 전체 플레이는 후속 통합에서 확인합니다.

다음 확인: Unity 종료 → Fetch origin → codex/phase3i-game-flow 선택/최신 Pull → Unity 재실행.
Console 컴파일 오류 없음 → Test Runner 검색/필터 해제 → EditMode Run All → 전체 **195개**.

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
| 전체 | 195 |

기존 Prototype 건설/자원/복수 콤보/클리어/Reset 호환도 확인합니다.
새 테스트는 GUID 임시 폴더만 사용하고 종료 후 정리합니다. 개인 기본 저장 경로, 실제 씬 로드, Application.Quit을 호출하지 않습니다.
Inspector 변경 없이 테스트 및 기존 Prototype 검증이 가능합니다.
이 환경에는 Unity/C# 컴파일러가 없어 새 3-I 컴파일·테스트·Prototype 직접 실행은 하지 못했습니다.
정적 점검과 원격 파일 반영 확인을 수행했으며 실제 Unity 실행 검증은 사용자 확인 대기입니다.
다음 개발은 3-I 검증 완료 후 **3-J: BuildingData 15종 및 실제 Stage 1/2 데이터·전체 루프 통합**입니다.
Art 폴더를 만들지 않고 Fonts/Materials/Sprite 및 임시 Assets/ScriptableObjects 위치를 유지합니다.
