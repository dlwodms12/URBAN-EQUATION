# Phase 3-H — 진행도·해금·최고 랭크 저장

## 상태와 범위

2026-10-03T04:51:49+09:00 사용자가 전체 123개 테스트 통과와 Prototype 정상 작동을 확인했습니다.
3-G 검증 완료 후 3-H 코드를 작성하고 SaveContractTests 32개를 추가했습니다.
현재 브랜치: **codex/phase3h-progress-save**.
기준: codex/phase3g-stage-goals, 3eb2fc690f57553c7e48d52b7eebc9b1e30fdb3e.
전체 예상 EditMode는 **155개**입니다. AI 환경에는 Unity/C# 컴파일러가 없어 이번 단계 실행 검증은 사용자 확인 대기입니다.

기획서 20/22페이지와 확정된 영구 저장 규칙을 적용했습니다.
Continue는 저장된 진행도로 스테이지 선택 화면에 들어갑니다.
Stage 1은 처음부터 해금, 이전 스테이지 클리어 후 다음 단계 해금, 낮은 재클리어는 최고 랭크 유지입니다.
런타임 보드/자원/카드/콤보/목표/Undo를 저장하거나 중도 보드를 복원하지 않습니다.

## 파일 처리

Assets 기준 경로는 Assets/_UrbanEquation입니다.

| 파일 | 처리 및 역할 |
|---|---|
| Scripts/Save/ProgressSaveData.cs | 신규, 읽기 전용 해금·최고 랭크 및 진행 규칙 |
| Scripts/Save/ProgressSaveCodec.cs | 신규, 버전이 있는 JSON 필드 한정 직렬화/검증 |
| Scripts/Save/ProgressFileStore.cs | 신규, UTF-8 파일 읽기·임시 파일 작성 후 교체 |
| Scripts/Save/SaveManager.cs | 신규, 명시적 설정/로드/새 게임·완료 저장·오류/재시도 |
| Tests/Editor/SaveContractTests.cs | 신규, 진행·JSON·파일·실패·완료/Undo 회귀 32개 |
| 위 신규 C#의 .meta 5개 | 신규, 고유 GUID |
| Docs/PHASE3H_SAVE.md | 신규, 변경·연결·검증 안내 |
| Docs/DEVELOPMENT_ROADMAP.md | 수정, 3-G 검증 완료·현재 3-H·155개 구성 |
| Docs/PHASE3G_STAGE_GOALS.md | 수정, 123개 사용자 검증 완료 및 최신 안내 |

총 13개(신규 11/수정 2). 기존 런타임 C#의 수정/대체/삭제 없음.
StageManager.OnStageCompleted를 새 SaveManager가 구독하며 완료 이벤트의 책임을 기존 코드에서 옮기지 않습니다.
기존 Scripts/Save.meta, 폴더 참조, 씬/프리팹 및 Fonts/Materials/Sprite·Assets/ScriptableObjects 위치를 유지합니다.

## 데이터와 파일

JSON 필드는 version(현재 1), stageCount, highestUnlockedStage, bestRanks 네 가지입니다.
bestRanks 인덱스 0은 Stage 1이며 0은 미클리어, 1~3은 최고 랭크입니다.
유효한 최고 랭크와 순차 해금 관계를 검사하여 앞 단계 미클리어/잠긴 단계의 기록/범위 밖 해금을 거부합니다.
정의되는 스테이지 수는 초기화 코드가 전달합니다. 현재 1차 목표는 5개이며 실제 제공 데이터 수와 일치하게 설정합니다.
다른 stageCount 또는 지원하지 않는 버전 파일은 자동 변환하지 않고 오류를 반환합니다. 확장/마이그레이션은 별도 변경입니다.

ProgressSaveData.TryRecordClear는 원본을 바꾸지 않고 다음 값을 만듭니다.
기록은 기존/이번 랭크 중 큰 값이며 해금은 다음 한 단계까지 늘어납니다.
마지막 단계 이후 번호를 해금하지 않습니다. 잠긴 단계나 Rank 0/4 등은 저장할 수 없습니다.
콜렉션은 복사된 읽기 전용 목록입니다.

기본 경로는 Application.persistentDataPath/URBAN-EQUATION/progress.json입니다.
별도 경로를 지정하여 테스트할 수 있습니다. 컴포넌트 추가/활성만으로 기본 파일을 읽거나 쓰지 않습니다.
FileStore는 같은 디렉터리의 고유 임시 파일에 작성/flush/close 후 기존 파일을 File.Replace로 교체합니다.
처음 저장하는 파일은 File.Move로 설치합니다. 기존 파일을 먼저 삭제하는 대체 경로는 없습니다.
교체를 지원하지 않는 환경이나 I/O 오류는 실패로 반환합니다. 대상 플랫폼별 실행 검증이 필요합니다.

## API 연결 순서

1. SaveManager.TryConfigureDefault(stageCount, out error) 또는 TryConfigure(stageCount, filePath, out error).
2. TryLoad(out error). 파일이 없으면 Stage 1만 해금된 초기 데이터, HasProgress=false이며 파일을 만들지 않습니다.
3. 로비에서 CanPlay/CanContinue/RequiresNewGameConfirmation을 조회합니다.
4. 확인된 Play/New Game 동작에서 TryStartNewGame(out error)를 호출합니다.
5. 해금된 StageData로 게임을 초기화한 뒤 TryBindStage(stageManager, out error)를 호출합니다.

| 로비 상태 | Continue | Play 처리 |
|---|---|---|
| 로드 성공, 저장 파일 없음 | 비활성 | Stage 1 새 게임 |
| 유효한 저장 진행 있음 | 활성, 스테이지 선택 진입 | 기존 진행 초기화 확인 후 새 게임 |
| 로드 실패 | 비활성 | 오류 표시, 기존 파일 덮어쓰기는 명시적 새 게임 확인 후 |

새 게임 API는 진행을 초기화하고 즉시 저장하므로 아직 클리어하지 않아도 이후 Continue가 가능합니다.
새 게임 API가 사용자 확인 창을 직접 띄우지는 않습니다. 후속 3-I가 확인 및 화면 이동을 연결합니다.
새 게임은 진행만 초기화하며 현재 보드/Stage/History를 직접 Reset하지 않습니다. 새 플레이 초기화는 3-I의 책임입니다.
설정 직후 아직 로드하지 않았다면 기존 진행 여부를 알 수 없어 RequiresNewGameConfirmation=true입니다.
CanPlay/CanContinue는 로비 상태 조회입니다. 이벤트 처리 중 실제 명령 재진입은 별도 busy 검사로 거부합니다.
TryBindStage는 초기화된 해금 스테이지 및 로드 성공 상태를 요구합니다. 실패 시 기존 구독을 유지합니다.
비활성/파괴 시 구독을 해제하고, 다시 활성화할 때 이미 완료된 결과를 자동 저장하지 않습니다.
필요하면 명시적으로 TryRecordStageResult(stage.CurrentResult, out error)를 호출합니다.

## 완료 저장·실패·Undo

메인 NEXT STAGE의 StageManager.OnStageCompleted(result)가 저장을 요청합니다.
필수 목표 준비/일반 건설/Snapshot 복원/Undo는 저장 요청이 아닙니다.
TryRecordStageResult는 필수 목표 및 랭크 일치를 확인하고 단계의 최고 랭크/다음 해금을 계산합니다.
파일 교체 성공 후에만 Progress/HasProgress/LastError를 갱신하고 OnProgressChanged를 발행합니다.
동일하거나 낮은 재클리어로 값이 같으면 파일/이벤트를 반복 처리하지 않습니다.
OnProgressChanged에서는 적용된 최종 진행과 Continue 여부를 읽을 수 있습니다.

I/O 오류나 잘못된 JSON은 LastError 및 OnSaveFailed(error)로 보고하며 성공한 것으로 처리하지 않습니다.
실패한 로드는 현재 Progress를 보존하고 Continue 및 일반 완료 기록을 차단합니다.
손상된/다른 버전 파일을 새 초기 파일로 자동 덮어쓰지 않습니다.
사용자 확인을 받은 TryStartNewGame만 기존 기록을 초기화할 수 있습니다.
읽기/쓰기 실패 시 저장 경로 안의 관계없는 파일은 삭제하지 않습니다.

Stage 완료 저장 실패는 PendingResult에 결과를 보관하고 TrySavePendingResult(out error)로 재시도합니다.
보류 결과가 있는 동안 TryLoad는 거부하여 아직 저장하지 않은 결과를 지우지 않습니다.
TryStartNewGame은 명시적 초기화 명령이므로 성공 시 보류 결과도 비웁니다.
StageManager.TryCompleteStage의 성공은 스테이지 완료를 의미합니다. 디스크 저장 성공과는 별도입니다.
후속 3-I는 저장 오류/보류 결과를 확인하고 재시도 UI와 다음 화면 이동을 조정해야 합니다.
이벤트 핸들러는 상태를 임의로 변경하거나 예외를 던지지 않아야 합니다.

이미 저장한 최고 랭크/해금은 영구 진행입니다. 완료 후 런타임 Undo로 돌아가도 이전 최고 기록을 지우지 않습니다.
Undo가 자동 저장되거나 완료 이벤트를 다시 발행하지 않습니다.
파일에는 실행 중 Board/Resources/Hand/History/Combo 상태가 들어가지 않습니다.

## 사용자 검증

1. Unity 종료 → GitHub Desktop Fetch origin → codex/phase3h-progress-save 선택 → 최신 Pull.
2. Unity 재실행 후 Console 컴파일 오류 없음 확인.
3. Test Runner 검색/선택 필터를 해제하고 EditMode Run All.
4. 아래 8개 클래스 및 전체 **155개** 확인.
5. 기존 Prototype의 건설/자원/복수 콤보/인구 4 클리어/Reset 호환 확인.

| 클래스 | 예상 테스트 수 |
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

저장 테스트는 Path.GetTempPath 아래의 테스트별 GUID 디렉터리를 만들고 TearDown에서 정리합니다.
실제 기본 저장 경로를 사용하지 않으며 Inspector 변경 없이 실행합니다.
검사 범위는 초기/순차 해금, 최고 랭크, 마지막 단계, 복사/읽기 전용, JSON 범위/잘못된 버전/누락,
UTF-8 생성/교체/임시 파일 정리, 파일 I/O 실패 보존/재시도, 새 매니저 재로드,
로비 상태, New Game, 완료 자동 저장/보류/중복/구독/재진입, 실제 완료 후 Undo입니다.
기존 123개 테스트 소스와 assembly 정의는 그대로 유지합니다.
새 매니저/버튼/씬을 기존 Prototype에 자동 연결하지 않습니다.
다음 개발은 사용자 검증 완료 후 **3-I: Lobby/Game 화면 상태·New Game/Continue·Retry·Exit**입니다.

## API 근거

- Unity JSON serializer: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/JsonUtility.html
- Unity save path: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-persistentDataPath.html
- File replacement: https://learn.microsoft.com/en-us/dotnet/api/system.io.file.replace

DTO의 숫자 필드는 초기값을 주지 않으며 필수 값/버전을 검사합니다. 역직렬화 생성자 동작에 의존해 누락 필드를 기본 진행도로 보정하지 않습니다.
