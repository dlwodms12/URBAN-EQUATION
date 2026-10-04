# 스테이지 확장 기반과 검증 절차

> 이 문서는 확장 기반을 만들었을 당시의 2개 콘텐츠·440개 테스트 기준 기록입니다. 사용자가 검증을 완료해 main에 병합했습니다. 현재는 정식 Stage1~5와 506개 테스트이며 최신 Pull/검증 절차는 `Docs/V020_TUTORIAL_UPDATE.md`와 `Docs/DEVELOPMENT_ROADMAP.md`를 사용합니다. 아래 저장 호환 설계는 현재도 유효합니다.

기준: 2026-10-04, main `c9d86c0bf547ae666cda3b9439b0ffcb8f05cd5c` ([v0.10]Build).
브랜치: `codex/stage-expansion-support`.

## 변경 내용

- `ProgressSaveData.TryExpandStages`가 기존 최고 별점을 유지하고 추가 스테이지의 별점을 0으로 채웁니다.
- `ProgressSaveCodec`는 원본 저장의 버전·해금·랭크를 먼저 검증한 뒤 현재 카탈로그가 더 클 때 확장합니다. 같은 개수의 저장 동작은 유지합니다.
- 기존 마지막 스테이지를 클리어했다면 바로 다음 스테이지 하나만 해금합니다. 미클리어 상태라면 기존 해금 범위를 유지합니다.
- 현재보다 더 많은 스테이지를 담은 저장, 손상된 저장, 지원하지 않는 버전은 계속 거부합니다.
- 로딩은 파일을 쓰지 않습니다. 최고 랭크/해금이 변경되는 다음 진행도 저장 또는 확인된 New Game에서 현재 스테이지 수가 기록됩니다. 같은 랭크 재플레이처럼 진행도 변화가 없다면 기존 파일을 유지합니다.
- Final QA의 정확히 두 스테이지만 허용하던 제한을 제거하고 모든 등록 StageData를 검사합니다. 기존 Stage1/2 튜토리얼의 보드·카드 수 검사는 유지합니다.
- 기존 Stage2 클리어/선택 목록 테스트는 카탈로그 개수에 맞춰 다음 스테이지·별점·해금을 검사합니다.

정식 콘텐츠는 여전히 Stage01/02 두 개입니다. 새 스테이지를 기획하거나 정식 데이터에 추가하지 않았습니다. UI 배치, 모델 크기/높이, 프리팹, 씬, 기존 StageData, 빌드 설정도 변경하지 않았습니다.

## 저장 호환 규칙

| 기존 상태 | 10개 카탈로그로 읽은 결과 |
|---|---|
| 2개 버전, Stage1 클리어·Stage2 미클리어 | 별점 유지, Stage2까지 해금, Stage3 잠금 |
| 2개 버전, Stage2까지 클리어 | 별점 유지, Stage3까지 해금, Stage4 이후 잠금 |
| 5개 버전, Stage5까지 클리어 | 별점 유지, Stage6까지 해금, Stage7 이후 잠금 |
| 10개 버전 저장을 2개 버전에서 로딩 | 거부; 자동 초기화·덮어쓰기 없음 |

기존 번호의 의미를 유지하고 카탈로그 뒤에 추가하는 업데이트를 지원합니다. 기존 스테이지 번호 재배치·삭제·다른 콘텐츠로 교체는 별도 호환 정책이 필요합니다. JSON 버전은 1이며 `version`, `stageCount`, `highestUnlockedStage`, `bestRanks` 네 필드를 유지합니다.

영구 저장은 진행도·최고 랭크입니다. Continue는 선택한 스테이지를 초기 상태로 시작하며, 실행 중 보드·카드·자원·Undo 이력은 저장하지 않습니다.

## Unity에서 이번 브랜치 확인

1. 수정 파일을 저장하고 Unity를 종료합니다.
2. GitHub Desktop에서 **Fetch origin**을 실행합니다.
3. **Current branch → codex/stage-expansion-support**로 전환하고 필요하면 **Pull origin**을 실행합니다.
4. Unity 6000.0.74f1로 프로젝트를 열고 컴파일 오류가 없는지 확인합니다.
5. **Window → General → Test Runner → EditMode → Run All**을 실행합니다.
6. 전체 **440개**를 확인합니다. 신규 **StageExpansionContractTests 19개**, **SaveContractTests 53개**가 보여야 합니다.
7. Play 종료 상태에서 **Tools → Urban Equation → Phase 4D → 1 Audit Final Setup**을 실행해 `Audit OK`를 확인합니다.
8. **Assets/_UrbanEquation/Scenes/Lobby.unity**를 열어 Play합니다. Stage1/2의 건설·콤보·Undo·목표·NEXT STAGE·Retry·Pause·Continue·Exit, 조정한 UI/건물 표시가 유지되는지 확인합니다.
9. **Assets/_UrbanEquation/Scenes/Prototype.unity**도 직접 열어 건설·콤보·Reset을 확인합니다. Prototype은 빌드 제외 상태를 유지합니다.
10. 가능하면 Windows로 다시 빌드해 로비→Stage1/2→종료→재실행→Continue를 확인합니다. 기존 해답·복수 콤보·Player QA 표는 `Docs/PHASE4D_FINAL_QA.md`를 참고합니다.

기존 저장이 있으면 Continue와 최고 별점 유지도 확인합니다. 파일이 없다면 New Game부터 확인합니다. 자동 테스트를 위해 실제 저장 파일을 삭제하거나 JSON을 직접 수정할 필요는 없습니다.

## 테스트 범위

| 클래스 | 개수 | 범위 |
|---|---:|---|
| SaveContractTests | 53 | 기존32 + 신규21: 2→5/10, 5→10, 완료/미클리어 경계, 한 스테이지 저장, 동일 개수, 원본 불변, 손상/다운그레이드 거부, 재저장·실패 후 재시도 |
| StageExpansionContractTests | 19 | 5/10개 카탈로그·QA·전체 초기화, 실제 선택 UI 행/잠금/스크롤, Stage3 진입·저장, 마지막 Stage5/10·Retry, New Game, 정식 데이터 불변 |
| StageIntegrationContractTests | 48 | 기존 해답·자원·콤보·목표·Undo·초기화, 개수 확장에 맞춘 검사 |
| FinalGameplayContractTests | 21 | 실제 사용자 HUD/팝업 회귀, 카탈로그 수에 따른 Stage2 이후 이동과 목록 |
| 나머지11개 클래스 | 299 | 기존 데이터·보드·자원·건설·콤보·Undo·목표·화면·프리팹 |
| 전체 | 440 | 기존400 + 신규40 |

확장 테스트는 정식 StageData를 메모리에서 복제하고 GUID별 OS 임시 폴더에만 저장합니다. 정식 Stage03~10 생성, 실제 플레이어 저장 접근, 에셋 저장/삭제, 실제 씬 로딩·종료는 수행하지 않습니다. Stage3 이후 복제본은 흐름 검사용이며 실제 레벨 기획안이 아닙니다.

AI 환경에서 C# 구문, 변경하지 않은 파일의 해시, 테스트 선언 수와 참조를 확인합니다. Unity/C# 컴파일러가 없으므로 실제 컴파일·440개 테스트·Play·빌드 통과 여부는 사용자 확인 대기입니다.

## 검증 후 새 스테이지 제작

1. Project의 **Assets/_UrbanEquation/Data/Stages/**에서 우클릭 → **Create → Urban Equation → Stage Data**를 선택합니다.
2. 현재 Stage01~05가 등록되어 있으므로 다음 이름을 `Stage06.asset` 등으로 정하고 Stage Number를 해당 번호로 설정합니다. Stage Name·Introduction을 입력합니다.
3. Width/Height를 정하고 Tiles에 정확히 Width×Height개의 TileData를 지정합니다. **북쪽 행부터, 각 행은 서쪽→동쪽**입니다. 타일 정의는 **Assets/_UrbanEquation/Data/Tiles/**에 있습니다.
4. Initial Resources에 Population/Jobs/Money/Logistics/Tourism 다섯 종류를 각각 한 번씩, 0 이상으로 입력합니다.
5. Building Cards에는 **Assets/_UrbanEquation/Data/Buildings/**의 정식 건물과 양수 수량을 지정합니다. 입력 순서대로 수량만큼 카드가 생성됩니다. **Assets/ScriptableObjects/**의 임시 Prototype 데이터는 사용하지 않습니다.
6. 필수 목표 한 개와 추가 목표 두 개를 입력합니다. Description은 표시 문구이며 실제 조건은 Goal Type·Resource Target·Building A/B에서 설정합니다. 신규 개수 목표는 Target Count, 직접 조회 목표는 Combo Reviewed/Complaint Reviewed를 사용합니다. Each Tile Type Built는 세 타일 종류 각각에 건설되었는지 검사합니다.
7. 같은 폴더의 **StageCatalog.asset**을 선택해 Stages 배열 끝에 추가합니다. Stage01부터 번호와 배열 순서가 연속해야 합니다.
8. **Assets/_UrbanEquation/Data/GameContent.asset**의 Stage Catalog 참조를 유지합니다. 새 스테이지마다 씬이나 Build Scene List 항목은 필요하지 않습니다.
9. 저장 후 Audit·전체 EditMode를 다시 실행합니다. 신규 스테이지의 클리어 가능성·별점별 해답·자원 흐름 테스트는 실제 기획 데이터에 맞춰 별도로 추가해야 합니다.

목록과 다음 스테이지 이동은 등록 개수를 따라갑니다. 긴 안내문·큰 보드·많은 카드는 실제 1920×1080 화면에서도 확인해야 합니다.
