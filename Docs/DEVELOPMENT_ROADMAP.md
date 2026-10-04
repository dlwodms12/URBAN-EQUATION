# URBAN EQUATION 개발 로드맵

기준: 최신 PDF(6), 44페이지와 사용자 확정 답변.
작업 시작 전에 이 파일과 `Docs/V020_TUTORIAL_UPDATE.md`를 읽습니다. 저장 확장 설계는 `Docs/STAGE_EXPANSION.md`, 기존 해답/Player QA는 `Docs/PHASE4D_FINAL_QA.md`를 참고합니다.
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
- [x] Phase 4-D: 통합·최종Audit·플레이 검증 (사용자 Unity 테스트·프로토타입·정식 기능 확인 및 빌드 작업 진행)

- [x] 스테이지 확장 기반: 5~10개 카탈로그·기존 저장 호환 (전체440개 사용자 통과·기존 기능 확인 후 main 병합)
- [ ] v0.20: 건설 조건·로비 이미지·목록 스크롤·Complaint·Stage3~5 (구현 및 신규66개 테스트 작성 완료, Unity 확인 대기)

## 확정 규칙

- Scene은 Lobby와 Game, 스테이지 선택은 Lobby 내부 UI입니다.
- 사용한 카드 제거, Undo 시 카드 목록과 순서까지 복원합니다.
- A/B/C 건설 후 Undo는 C→B→A까지만 가능하며 빈 초기 보드로는 돌아가지 않습니다.
- Undo는 보드·자원·콤보 결과·목표·랭크·NEXT STAGE 등 해당 턴 상태 전체를 복원합니다.
- 영구 저장은 진행도와 최고 랭크이며 실행 중 보드 및 Undo 이력은 저장하지 않습니다.
- 보드 북쪽은 +Z, 문서 위쪽 행이 북쪽 행입니다.
- 콤보/Complaint는 왼쪽→아래→오른쪽→위 순서로 검사하고 복수 결과를 순차 표시합니다. Complaint는 음수 자원을 허용하며 해당 자원이 부족한 건물의 선불 비용은 계속 막습니다.
- 콤보/Complaint 목표는 서로 다른 현재 건물 쌍 수입니다. 직접 조회는 각 종류를 한 번 확인하면 달성하며 자동 팝업은 세지 않습니다. 개수와 조회 상태는 턴 Snapshot으로 복원합니다.
- 카지노는 자금 +3·관광 +1을 유지합니다. Stage4의 중형 공장 정답 좌표는 (2,2)입니다.
- 관광지 카페는 카페(B32001)와 절(B52001)의 인접 조합입니다. 사용자 정정 후 Stage4 해답은 콤보4쌍, Stage5 해답은 콤보2쌍·Complaint3쌍입니다.
- 모호한 기획은 질문 후 확정합니다. 파일 변경·대체·삭제는 사용자에게 명시합니다.

## 현재 위치와 검증 경계

사용자가 스테이지 확장 기반의 440개 테스트와 기존 기능을 확인하여 main에 병합했습니다. 신규 리소스가 추가된 main `a770f3ee1023decfb7f9b183ee011c558709b105`를 기준으로 이번 업데이트를 구현했습니다. 사용자 확인에서 신규 자동 팝업 테스트 두 개의 활성화 절차 누락을 발견하여 보완했고 관광지 카페 조합 정정과 회귀 테스트를 반영했습니다.
브랜치는 **codex/v020-complaints-and-tutorial-stages**, 상세 변경·Pull/테스트·Stage3~5 해답·수동 QA 절차는 **Docs/V020_TUTORIAL_UPDATE.md**입니다.

정식 StageCatalog는 이제 **5개**입니다. Stage03/04/05와 신규 목표를 추가했고 콤보 데이터는 일반 콤보34개 + Complaint4개 = **38개**입니다. ProgressSaveData/Codec의 기존 2→5/10개 확장 동작을 계속 사용합니다. 보드/Undo 이력은 영구 저장하지 않습니다.

기존 Stage01/02·모델 크기/높이·씬·빌드 설정은 유지했습니다. 기존 HUD/화면 프리팹에 공유 UI 설정 참조를 연결하고 실행 중에 스크롤 버튼/바와 Complaint 스타일을 적용합니다. 사용자가 조정한 텍스트/카드 Rect Transform 값은 그대로입니다.
다음 확인: Unity 종료→브랜치 전환/Pull→컴파일 오류 없음→EditMode **506개**→Audit 정상→Lobby·Stage1~5·Prototype→필요 시 Windows Player 확인. 프리팹 재생성이나 이미지 참조 수동 연결은 필요하지 않습니다.

기존 문서의400/440개는 당시 단계 기준이며 현재 기대 개수는 아래와 같습니다.

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
| TutorialStageContractTests | 39 |
| UiUpdateContractTests | 27 |
| 전체 | 506 |

AI 환경에는 Unity/C# 컴파일러가 없어 실제 컴파일·EditMode·Play·빌드를 실행하지 못했습니다. C# 구문, Unity YAML/참조, 변경하지 않은 파일 해시, 테스트 선언506개, 실제 데이터에 따른 해답의 자원/쌍 수와 원격 반영을 검증합니다. **506개 통과는 사용자 Unity 확인 대기**입니다.
