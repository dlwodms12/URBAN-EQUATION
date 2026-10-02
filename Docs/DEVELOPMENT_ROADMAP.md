# URBAN EQUATION 개발 로드맵

기준: 2026-10-01 수정 기획서(33페이지)와 사용자 확정 답변.
작업 시작 전에 이 파일과 현재 단계의 변경 안내(`Docs/PHASE3B_BOARD.md`)를 읽습니다.
전체 상세 로드맵: `dlwodms12/LJE_GPT_Log`의
`memory/entries/MEM-20261001-0610-urban-equation-development-roadmap-v1.md`.

## 진행 순서

- [x] Phase 1: 기획 분석·질문·작업 순서 확정
- [x] Phase 2: 폴더 구조 설계 및 사용자 이동 작업 확인
- [x] Phase 3-A: 공통 데이터 정의 (사용자 Unity 컴파일·EditMode 8개·Prototype 검증 완료)
- [ ] Phase 3-B: StageData 기반 보드·타일 생성 (코드·테스트 작성 완료, Unity 검증 대기)
- [ ] Phase 3-C: 5종 자원·요구/획득 자원 목록 처리
- [ ] Phase 3-D: 카드 순서·수량·드래그·건설
- [ ] Phase 3-E: 콤보 판정·결과·순차 연출 데이터
- [ ] Phase 3-F: 턴 완료 Snapshot·다단계 Undo
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
Phase 3-B 코드 및 BoardContractTests 9개 작성, 실행 환경에 Unity가 없어 새 테스트는 실행 대기입니다.
사용자가 Console 무오류 및 Prototype 정상 동작을 확인했으며, 전체 17개 중 월드 좌표 정확 비교 1개가 실패했습니다.
좌표 테스트를 거리 오차 0.00001 이내 비교와 정밀 진단 메시지로 수정했습니다. 해당 테스트 재실행 대기입니다.
모든 테스트 통과 확인 후 3-B를 완료 처리합니다.
그다음은 Phase 3-C(자원)입니다.
본 개발용 BuildingData 15종·콤보 표·Stage 1/2 asset 입력 및 프리팹 연결은 아직 수행하지 않았습니다.
기존 `Assets/ScriptableObjects`의 데이터는 대체 런타임과 새 데이터 연결이 끝날 때까지 유지합니다.
