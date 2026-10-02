# URBAN EQUATION 개발 로드맵

기준: 2026-10-01 수정 기획서(33페이지)와 사용자 확정 답변.
작업 시작 전에 이 파일과 현재 단계의 변경 안내(`Docs/PHASE3H_SAVE.md`)를 읽습니다.
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
- [ ] Phase 3-H: 진행도·해금·최고 랭크 저장 (코드·테스트 32개 추가, 전체 155개 Unity 검증 대기)
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

2026-10-03T04:51:49+09:00: 사용자가 전체 123개 테스트 통과와 기존 Prototype 정상 작동을 확인했습니다.
3-G 검증 완료 후 다음 단계 3-H를 작성했습니다.

현재 브랜치: **codex/phase3h-progress-save**.
기준 3-G 커밋: 3eb2fc690f57553c7e48d52b7eebc9b1e30fdb3e.
변경/연결/검증 상세: Docs/PHASE3H_SAVE.md.
저장 파일에는 버전·스테이지 수·최고 해금 단계·스테이지별 최고 랭크만 포함합니다.
Stage 1은 처음부터 해금되며 클리어 시 다음 하나를 해금합니다. 마지막 스테이지 범위를 넘지 않습니다.
낮은 랭크 재클리어는 최고 랭크나 해금 진행을 낮추지 않습니다.
Continue는 저장 진행도로 스테이지 선택 화면에 들어가는 기능입니다. 플레이 중 보드를 이어받지 않습니다.
저장 파일이 없으면 Continue 비활성, 기존 진행도가 있으면 새 게임 확인이 필요합니다.
새 게임 확인/스테이지 선택/실제 화면 전환은 3-I에서 연결합니다.
SaveManager는 명시적 Configure/Load 후 Stage 완료 결과에 연결합니다. 컴포넌트 추가만으로 개인 저장 파일에 접근하지 않습니다.
저장은 파일 교체 성공 후 메모리 진행도를 갱신하며 실패 시 이전 진행도와 재시도할 완료 결과를 보존합니다.
손상된 JSON/지원하지 않는 버전은 오류를 반환하며 자동으로 덮어쓰지 않습니다.
기존 Prototype에는 새 저장 매니저를 자동 연결하지 않습니다.

다음 확인: Unity 종료 → Fetch origin → codex/phase3h-progress-save 선택/최신 Pull → Unity 재실행.
Console 컴파일 오류 없음 → 필터 해제 → EditMode Run All → 전체 **155개**.

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
| 전체 | 155 |

기존 Prototype 건설/자원/복수 콤보/클리어/Reset 호환도 확인합니다.
새 저장 테스트는 개별 임시 폴더를 사용하고 종료 후 정리합니다. Inspector 변경 없이 실행합니다.
이 환경에는 Unity/C# 컴파일러가 없어 새 3-H 컴파일·테스트·Prototype 직접 실행은 하지 못했습니다.
다음 개발은 3-H 검증 완료 후 **3-I: Lobby/Game 화면 상태·New Game/Continue·Retry·Exit**입니다.
실제 BuildingData 15종·Stage 1/2 asset 및 Game 씬 연결은 3-J/Phase 4입니다.
최종 프리팹/UI 및 모든 목표 패널/결과 팝업 연결도 후속 단계입니다.
Art 폴더를 만들지 않고 Fonts/Materials/Sprite 및 임시 Assets/ScriptableObjects 위치를 유지합니다.
