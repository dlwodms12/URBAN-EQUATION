# URBAN EQUATION 개발 로드맵

기준: 2026-10-01 수정 기획서(33페이지)와 사용자 확정 답변.
작업 시작 전에 이 파일과 현재 단계의 변경 안내(`Docs/PHASE3G_STAGE_GOALS.md`)를 읽습니다.
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
- [ ] Phase 3-G: 목표·랭크·NEXT STAGE 판정 (코드·테스트 28개 추가 완료, 전체 123개 Unity 검증 대기)
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

2026-10-03T04:18:29+09:00: 사용자가 전체 95개 테스트 통과와 기존 Prototype 정상 동작을 확인했습니다.
3-E/3-F의 누적 검증과 이전 테스트 목록 누락 확인을 완료 처리하고 3-G를 진행했습니다.
이는 Prototype에 새 Undo/목표 UI를 연결했다는 뜻은 아닙니다. 새 시스템은 EditMode fixture로 검증합니다.

현재 브랜치: **codex/phase3g-stage-goals**.
기준 3-F 커밋: 2ad223d4268a5f9554b399e8b4b1c808a10e11aa.
변경/연결/검증 상세: Docs/PHASE3G_STAGE_GOALS.md.
필수 목표와 추가 목표 2개를 자원 기준/모든 카드 사용/4방향 인접으로 판정합니다.
필수 목표 미달성 Rank 0, 필수 달성 Rank 1 + 추가 목표 달성 수입니다.
필수 목표 달성은 NEXT STAGE를 활성화하며 더 건설할 수 있습니다.
메인 NEXT STAGE 요청 때 클리어와 읽기 전용 StageResult를 확정합니다.
완료 결과는 다음 저장/흐름 단계에서 사용하며 이번 단계에서 씬 전환하지 않습니다.
건설·콤보 후 목표 상태를 모든 알림 전에 적용하고 완료 턴에 저장합니다.
Undo는 목표·랭크·NEXT STAGE 및 클리어 이전 상태를 복원하고 완료 이벤트를 재발행하지 않습니다.
기존 Prototype의 Population>=4 및 CheckStageClear/ResetStage API를 유지합니다.

다음 확인: Unity 종료 → Fetch origin → codex/phase3g-stage-goals 선택/최신 Pull → Unity 재실행.
Console 컴파일 오류 없음 → 검색/선택 필터 해제 → EditMode Run All → 전체 **123개**.

| 테스트 클래스 | 예상 수 |
|---|---:|
| DataContractTests | 8 |
| BoardContractTests | 9 |
| ResourceContractTests | 14 |
| BuildingContractTests | 20 |
| ComboContractTests | 20 |
| TurnHistoryContractTests | 24 |
| StageGoalContractTests | 28 |
| 전체 | 123 |

기존 Prototype 건설/자원/복수 콤보/클리어/Reset 호환도 확인합니다.
새 테스트는 fixture를 생성하므로 Inspector 변경 없이 실행합니다.
이 환경에는 Unity/C# 컴파일러가 없어 새 3-G 컴파일·테스트·Prototype 실행은 사용자 검증 대기입니다.
다음 개발은 3-G 검증 완료 후 **3-H: 진행도·스테이지 해금·최고 랭크 저장**입니다.
실제 BuildingData 15종·Stage 1/2 asset 및 Game 씬 연결은 후속 통합/Phase 4입니다.
최종 목표 텍스트 색/취소선, 결과 팝업 및 버튼 프리팹 제작도 Phase 4입니다.
Art 폴더를 만들지 않고 Fonts/Materials/Sprite 및 임시 Assets/ScriptableObjects 위치를 유지합니다.
