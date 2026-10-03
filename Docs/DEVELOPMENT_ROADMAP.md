# URBAN EQUATION 개발 로드맵

기준: 2026-10-03 최신 PDF(5), 33페이지와 사용자 확정 답변.
작업 시작 전에 이 파일과 `Docs/PHASE4B_HUD.md`를 읽습니다.
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
- [ ] Phase 4-B: Main Game HUD·카드/건물 상세·콤보 조회 (코드/46개 신규 테스트 작성, Unity 생성/검증 대기)
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

2026-10-03T09:41:35+09:00 사용자가 테스트와 실제 동작 확인 및 변경 파일 Push를 보고했습니다.
이번 발화에 개수를 직접 반복하지 않았으며 앞서 안내한272개 검증 완료 확인으로 반영합니다.
4-A 사용자 커밋 `02d97210cb1d0fce77181283b0fb56b87f7d73e1`(Prefab 연결): 새6prefab과GameplayPrefabs/meta16파일, 수정19data/16image importer meta를 확인했습니다.
대형사무실은 최신PDF(5)의Building_Skyscraper를 유지합니다. 4-A 사용자 검증 완료이며 현재4-B입니다.
새브랜치: **codex/phase4b-game-hud**, 기준4-A 사용자 커밋위동일. 상세 **Docs/PHASE4B_HUD.md**.

4-B는 Overlay HUD/도시자원5종/목표3개 색·취소선/카드·Undo·NEXT·Pause/호버 상세/배치 피드백/자동 콤보1.5초·마지막0.5페이드/두인접건물 클릭 읽기전용 콤보조회를 연결합니다.
새메뉴 Tools > Urban Equation > Phase 4B > 1 Create Missing HUD and Bind Icons → 2 Validate HUD Setup → 3 HUD Gameplay Preview.
기존4-A 공통prefab/데이터를 유지하고 Pfb_Main_GameHud_001.prefab, ResourceIcons.asset, GameplayHud.asset을 사용자 Unity에서 생성합니다. Icon15PNG를 Sprite Single로import하며 PNG/GUID/외부GUI팩 설정은 유지합니다.
Playing에서는 이전디버그패널을 숨기고 실제HUD로 조작합니다. Lobby/StageSelect/안내/Clear/Pause는4-C전까지기존디버그화면을사용합니다. 새정식씬/BuildSettings/LoadScene/Quit는이번단계에서변경하지않습니다.
BuildingHandUI에는옵션호버연결,ComboPopupUI에는옵션자원아이콘/시간/별도영구조회,GameFlowDebugUI에는옵션Playing패널숨김,EditorDebugMenu에는4-B메뉴를추가했습니다. 기본Prototype/4-A미리보기 동작을 유지합니다.
신규12C#+meta 및안내서(25신규), 위4C#+로드맵(5수정), 총30변경입니다. 삭제/이동없음.
다음 확인: Unity종료→새브랜치Pull→컴파일오류없음→메뉴1생성/메뉴2 `Phase4-B HUD Validation OK`→EditMode Run All **318개**→메뉴3 실제HUD와기존Prototype→생성asset/meta·수정Iconmeta Commit/Push.

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
| GameHudContractTests | 46 |
| 전체 | 318 |

AI환경에는Unity/C#컴파일러가없어실제생성/import·컴파일·EditMode·Play·렌더를실행하지못했습니다. 소스구문(컴파일아님)/사용자참조GUID/테스트선언수/기존asset·테스트보존/원격반영을확인합니다.
실제새HUD파일은사용자메뉴실행에서생성하며4-B검증완료로간주하지않습니다. 확인및생성파일Push후4-C최종메뉴/팝업·실제Lobby/Game씬을진행합니다.
