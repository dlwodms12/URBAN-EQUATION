# URBAN EQUATION 개발 로드맵

기준: 2026-10-03 최신 PDF(5), 33페이지와 사용자 확정 답변.
작업 시작 전에 이 파일과 `Docs/STAGE_EXPANSION.md`, `Docs/PHASE4D_FINAL_QA.md`를 읽습니다.
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
- [x] Phase 3-J: 실제 BuildingData15종·Stage1/2 전체루프 (전체테스트·Prototype·디버그 기능 사용자 검증 완료)
- [x] Phase 4-A: 공통 프리팹 (사용자 테스트·실제 동작 확인 및 프리팹/연결 데이터 Push 완료)
- [x] Phase 4-B: Main Game HUD·카드/건물 상세·콤보 조회 (사용자 확인 및 생성 파일 Push 완료)
- [x] Phase 4-C: Lobby/Game 실제 씬·메뉴·팝업 (사용자 확인 및 생성물 원격 Push 확인)
- [ ] Phase 4-D: 최종 플레이 검증 (통합21개/최종Audit·복수콤보QA미리보기 준비, Unity/Player 검증 대기)

- [ ] 스테이지 확장 기반: 5~10개 카탈로그·기존 저장 호환·QA 기준 갱신 (확장 회귀40개 추가, 전체440개 사용자 검증 대기)

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

2026-10-04 사용자가 UI/건물 표시를 폴리싱하고 main `[v0.10]Build` (`c9d86c0`)에 반영했습니다. 이번 작업은 그 커밋을 기준으로 스테이지 확장 기반을 준비합니다.
브랜치는 **codex/stage-expansion-support**, 상세 변경·저장 호환·Pull/테스트·스테이지 제작 절차는 **Docs/STAGE_EXPANSION.md**입니다.

ProgressSaveData/Codec는 원본 저장을 먼저 검증하고 추가 스테이지의 랭크를 0으로 확장합니다. 기존 마지막 스테이지를 완료했을 때만 새 후속 스테이지 하나를 해금합니다. 로딩은 읽기 전용이며 이후 진행도 변경 저장에서 확장된 개수가 반영됩니다.
Final QA는 모든 등록 스테이지를 검사하며 기존 Stage1/2 튜토리얼 계약을 유지합니다. 실제 콘텐츠는 여전히 두 스테이지입니다. 확장 테스트용 Stage3~10은 메모리 복제본이며 정식 기획 데이터가 아닙니다.

기존 UI 배치·모델 크기/높이·프리팹·씬·Stage01/02·빌드 설정을 유지합니다. Lobby/Game은 활성, Prototype은 빌드 제외 상태입니다. 삭제/이동은 없습니다.
다음 확인: Unity 종료→브랜치 전환/Pull→컴파일 오류 없음→EditMode **440개**→Audit 정상→정식 Lobby/Stage1·2/Prototype 회귀→Windows Player 확인.

기존 Phase4-D의 플레이 해답·복수 콤보 미리보기·빌드 확인표는 **Docs/PHASE4D_FINAL_QA.md**에 있습니다. 그 문서의400개는 당시 기준이며 현재 기대 개수는 아래와 같습니다.

| 테스트 클래스 | 예상 수 |
|---|---:|
| DataContractTests | 8 |
| BoardContractTests | 9 |
| ResourceContractTests | 14 |
| BuildingContractTests | 20 |
| ComboContractTests | 20 |
| TurnHistoryContractTests | 24 |
| StageGoalContractTests | 28 |
| SaveContractTests | 53 |
| GameFlowContractTests | 40 |
| StageIntegrationContractTests | 48 |
| PrefabContractTests | 29 |
| GameHudContractTests | 46 |
| GameScreensContractTests | 61 |
| FinalGameplayContractTests | 21 |
| StageExpansionContractTests | 19 |
| 전체 | 440 |

AI 환경에는 Unity/C# 컴파일러가 없어 실제 컴파일·EditMode·Play·빌드를 실행하지 못했습니다. C# 구문, 변경하지 않은 파일 해시, 테스트 선언440개와 원격 반영을 확인합니다.
이번 브랜치의440개·Audit·기존 Stage1/2·Prototype 사용자 확인을 받은 뒤 신규 스테이지 기획 데이터를 추가합니다. Windows Player의 실제 저장/Continue/종료도 별도로 확인합니다.
