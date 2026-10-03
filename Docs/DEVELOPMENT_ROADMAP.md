# URBAN EQUATION 개발 로드맵

기준: 2026-10-03 최신 PDF(5), 33페이지와 사용자 확정 답변.
작업 시작 전에 이 파일과 `Docs/PHASE4A_PREFABS.md`를 읽습니다.
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
- [ ] Phase 4-A: 공통 프리팹 (생성/연결 도구·표시 코드·29개 신규 테스트 작성, Unity 생성/검증 대기)
- [ ] Phase 4-B: 최종 Main Game HUD·카드 상세·연출
- [ ] Phase 4-C: Lobby/Game 실제 씬·메뉴·팝업
- [ ] Phase 4-D: 최종 플레이 검증

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

2026-10-03T07:47:39+09:00: 사용자가 3-J 전체 테스트·기존 Prototype·디버그 게임 기능 동작을 확인하고 다음 작업과 상세 Unity 작업 안내를 요청했습니다.
이번 메시지에 개수는 직접 재명시하지 않았으며 앞서 안내한 예상 전체243개에 대한 검증 확인으로 반영합니다.
대형 사무실 B23001은 사용자 선택에 따라 당시 PDF(4) 표의 파일명 Building_House_Block을 연결했습니다. 최신 PDF(5)의 명시적 수정으로 현재는 Building_Skyscraper를 연결합니다.
2026-10-03T09:14:29+09:00 중단 재개 점검: 이전4-A 코드/문서는 PR #11에 게시 완료이며, 생성 프리팹/Unity 검증 확인은 아직 없습니다. 새 PDF 전체33페이지를 이전본과 동일 해상도로 비교해13페이지 차이만 확인했습니다. B23001 모델을 Building_Skyscraper로 수정하고, 검증 메뉴가 잘못된 실제 모델 참조도 거부하도록 보완했습니다. 참조누락/구모델거부/신모델허용 회귀3개를 추가했습니다. 사용자 PC의 미커밋 생성물은 확인하지 못했으며4-A 사용자검증 대기를 유지합니다.
현재 브랜치: **codex/phase4a-common-prefabs**, 기준J 커밋 `2a5f8d8be0cb7f0eafcd9ff7e32938a932fb650a`.
Unity 작업과 파일별 Inspector 안내: **Docs/PHASE4A_PREFABS.md**.

Phase4-A 생성 메뉴는 새 타일3종·BuildingRoot·BuildingCard·ComboPopup 및 GameplayPrefabs.asset을 만들고 본개발 데이터 참조를 연결합니다.
생성된 목적지 프리팹은 재생성하지 않으며 현재 편집 내용을 유지합니다. 참조와16개 이미지 import 설정은 재연결합니다.
원래 Tile/Building 프리팹은 유지하며 새로운 BuildingRoot의 Visual Offset으로 Surface 위에 외형을 올립니다. 기존 기본값0으로 Prototype 동작을 유지합니다.
카드 이미지15개와 콤보 이미지1개를 Sprite Single로 import하고 외부 pack 및 PNG/GUID는 유지합니다.
ComboPopupUI는 이미 지급된 결과의 표시·위치·부호/색·페이드·순차 완료만 처리합니다. GameplayPrefabPreview는 카드 드래그/오버레이/콤보를 임시 씬에 연결합니다.
Source와 도구는 작성했으며 **프리팹 asset 자체는 사용자 Unity에서 메뉴 실행 시 생성**됩니다. 아직 4-A 생성/import/Play 검증 완료로 기록하지 않습니다.
기존 Prototype 씬/프리팹/테스트243개, Fonts/Materials/Sprite 및 Assets/ScriptableObjects는 유지합니다. 삭제/이동 없음.
본 단계는 공통 프리팹이며 최종 HUD·호버는4-B, 실제Lobby/Game씬·메뉴·팝업은4-C, 최종 검증은4-D입니다.

이전 메뉴를 실행했어도 메뉴1→2를 재실행하면 최신 모델 참조를 적용하고 기존 프리팹 편집을 유지합니다. 생성파일 Push와 사용자 검증 이후4-B를 진행합니다.

다음 확인: Unity 종료 → 브랜치 최신 Pull → 재실행/컴파일 오류 없음 → Tools > Urban Equation > Phase 4A 메뉴1 생성 → 메뉴2 검증 → EditMode Run All **272개** → 메뉴3 미리보기/기존Prototype → 생성파일과meta·수정데이터 Commit/Push.

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
| PrefabContractTests | 29 |
| 전체 | 272 |

새 테스트는 메모리에서 카드/타일/팝업을 생성하고 GUID OS임시 저장 경로를 정리합니다. 프로젝트 이미지 import/프리팹 파일 생성 메뉴를 자동 실행하지 않습니다.
이 환경에는 Unity/C# 컴파일러가 없어 실제 컴파일·생성/import·EditMode·Play·화면 렌더링은 사용자 검증 대기입니다.
정적 참조/구문/테스트 선언 집계 및 원격 반영을 확인합니다. 4-A 사용자 검증 후4-B로 진행합니다.
