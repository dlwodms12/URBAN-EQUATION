# URBAN EQUATION 개발 로드맵

기준: 2026-10-01 수정 기획서(33페이지)와 사용자 확정 답변.
작업 시작 전에 이 파일과 `Docs/PHASE3J_INTEGRATION.md`를 읽습니다.
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
- [ ] Phase 3-J: 실제 BuildingData 15종·Stage 1/2 및 전체 루프 통합 (코드·48개 신규 테스트 작성, Unity 검증 대기)
- [ ] Phase 4: Lobby/Game 실제 씬·프리팹·UI·최종 플레이 검증

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

2026-10-03T06:59:51+09:00: 사용자가 전체195개 테스트 통과와 Prototype 정상 동작을 확인했습니다.
3-I 비활성화 테스트 수정도 검증 완료로 기록하고 3-J를 진행합니다.
현재 브랜치: **codex/phase3j-stage-data**, 기준 3-I 커밋 `f7ae185f60e885424668a65ce8afa26e7aef49b0`.
변경/실행/해답: `Docs/PHASE3J_INTEGRATION.md`.

기획서의 건물15종 비용·획득·허용 타일, Stage1/2 카드·초기 자원·목표를 실제 asset으로 작성했습니다.
GameContentData가 카탈로그 간 참조를 검증하고 GameBootstrap이 명시적 저장 경로로 초기화합니다.
StageManager를 Session 연결 시점에 바인딩하여 첫 건설 전에도 입력 차단 상태를 공유합니다.
Editor 메뉴 `Tools > Urban Equation > Stage 1-2 Debug`에서 임시 씬과 좌표 선택 디버그 UI로 전체 루프를 실행합니다.
디버그 저장은 temporaryCachePath 하위 별도 파일입니다. 실제 Lobby/Game 씬과 최종 프리팹·UI는 Phase4입니다.
타일 논리 데이터는 3종이며 외형은 기존 Tile 프리팹을 임시 공유합니다.
실제 Stage1/2에 쓰이는 건물5종 외형만 연결하며 나머지10종 외형·카드 Sprite는 Phase4에서 연결합니다.
기존 Prototype, Fonts/Materials/Sprite 및 Assets/ScriptableObjects를 유지합니다.

다음 확인: Unity 종료 → Fetch origin → **codex/phase3j-stage-data** 선택/최신 Pull → Unity 재실행.
Console 컴파일 오류 없음 → Test Runner 검색/필터 해제 → EditMode Run All → 예상 전체 **243개**.

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
| 전체 | 243 |

기존 Prototype 호환과 새 디버그 Stage1/2 플레이를 모두 확인합니다.
이 환경에는 Unity/C# 컴파일러가 없어 3-J 컴파일·EditMode·플레이 실행을 직접 확인하지 못했습니다.
asset 정적 검증, 독립 자원/배치 계산 및 원격 반영 확인과 Unity 실행 검증을 구분합니다.
3-J 사용자 검증이 완료되면 Phase4로 진행합니다.
