# URBAN EQUATION 개발 로드맵

기준: 2026-10-01 수정 기획서(33페이지)와 사용자 확정 답변.
작업 시작 전에 이 파일과 현재 단계의 변경 안내(`Docs/PHASE3F_UNDO.md`)를 읽습니다.
전체 상세 로드맵: `dlwodms12/LJE_GPT_Log`의
`memory/entries/MEM-20261001-0610-urban-equation-development-roadmap-v1.md`.

## 진행 순서

- [x] Phase 1: 기획 분석·질문·작업 순서 확정
- [x] Phase 2: 폴더 구조 설계 및 사용자 이동 작업 확인
- [x] Phase 3-A: 공통 데이터 정의 (사용자 Unity 컴파일·EditMode 8개·Prototype 검증 완료)
- [x] Phase 3-B: StageData 기반 보드·타일 생성 (사용자 EditMode 17개·Prototype 검증 완료)
- [x] Phase 3-C: 5종 자원·요구/획득 자원 목록 처리 (사용자 EditMode 31개·Prototype 검증 완료)
- [x] Phase 3-D: 카드 순서·수량·드래그·건설 (사용자 EditMode 51개·Prototype 검증 완료)
- [ ] Phase 3-E: 콤보 판정·결과·순차 연출 데이터 (사용자 3-E 브랜치에서 51개 통과·복수 콤보 로그 확인, 전체 71개 확인 대기)
- [ ] Phase 3-F: 턴 완료 Snapshot·다단계 Undo (코드·테스트 작성 완료, Unity 검증 대기)
- [ ] Phase 3-G: 목표·랭크·NEXT STAGE 판정
- [ ] Phase 3-H: 진행도·해금·최고 랭크 저장
- [ ] Phase 3-I: Lobby/Game 화면 전환·Retry·Exit
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

2026-10-03: 사용자가 Phase 3-A 브랜치의 Unity 컴파일 무오류, EditMode 테스트 통과,
기존 Prototype 정상 동작을 확인했습니다. 3-A를 완료 처리했습니다.
Art 폴더는 새로 만들지 않고 현재 Fonts/Materials/Sprite 위치로 계속 진행합니다.
2026-10-03: 사용자가 좌표 허용 오차 수정 후 전체 EditMode 17개 통과를 확인했습니다.
앞서 확인한 Console 무오류·Prototype 정상 동작 결과와 함께 3-B를 완료 처리했습니다.
2026-10-03: 사용자가 Phase 3-C 테스트 및 Prototype 정상 작동을 확인하여 3-C를 완료 처리했습니다.
2026-10-03: 사용자가 Phase 3-D 테스트 및 Prototype 정상 작동을 확인하여 3-D를 완료 처리했습니다.
Phase 3-E의 4방향 콤보 판정·쌍별 중복 보상 방지·결과 저장·표시 큐·건설 연동을 작성했습니다.
기획서의 30행을 Data/Combos/ComboDatabase.asset에 입력했습니다.
ComboContractTests 20개를 추가하여 전체 EditMode는 71개입니다.
2026-10-03T03:28:37+09:00: 사용자가 3-E 브랜치에서 테스트 51개 통과와 Prototype 복수 콤보 콘솔 출력을 확인했습니다.
원격 3-E에는 ComboContractTests 20개가 포함되며 예상 합계는 71개입니다.
전체 71개 통과나 순차 UI/표시 중 Reset까지 검증된 것으로 확대 기록하지 않습니다.
사용자의 다음 단계 진행 요청에 따라 3-F를 작성했습니다.
3-F는 건설+콤보 처리 후 완료 턴 Snapshot을 기록하고, 첫 건설 상태까지 연속 Undo합니다.
보드·5종 자원·카드 목록/순서·콤보 기록·스테이지 상태와 마지막 건설 콤보 결과를 함께 복원합니다.
목표/랭크/NEXT STAGE 상태 저장·복원 API는 포함하며, 자동 목표/랭크 판정은 예정된 3-G입니다.
TurnHistoryContractTests 24개를 추가하여 전체 예상 EditMode는 95개입니다.
이 환경에는 Unity/C# 컴파일러가 없어 3-F 컴파일·테스트 실행·Prototype은 사용자 검증 대기입니다.
브랜치: codex/phase3f-turn-history. 기준: 3-E 커밋 a286f5ce733ec12317963d7d4b1e3daa0ea20465.
다음 확인: Test Runner 검색/선택 필터 해제 → EditMode Run All → 전체 95개.
ComboContractTests 20개와 TurnHistoryContractTests 24개가 목록에 포함되는지도 확인합니다.
기존 Prototype 건설/자원/콤보/Reset 호환 확인도 계속 수행합니다.
새 테스트는 fixture를 생성하므로 Inspector 변경 없이 실행합니다.
다음 개발은 3-F 검증 완료 후 Phase 3-G(스테이지 목표·랭크·NEXT STAGE 판정)입니다.
본 개발용 BuildingData 15종·Stage 1/2 asset 입력 및 새 씬/프리팹 연결은 후속 단계입니다.
새 콤보 asset의 실제 Game 씬 연결 및 최종 팝업 위치·시간·페이드는 후속 통합/Phase 4에서 수행합니다.
기존 `Assets/ScriptableObjects`의 데이터는 대체 런타임과 새 데이터 연결이 끝날 때까지 유지합니다.
