# Phase 3-B 보드·타일 변경 안내

## 시작 기준

- Phase 3-A 기준 커밋: `58dfec81ae4d734f415ffed324016e0cf07aeceb`.
- 사용자가 Unity 재실행 후 컴파일 무오류, EditMode 테스트 통과, 기존 Prototype 정상 동작을 확인했습니다.
- 사용자의 최신 결정에 따라 Art 폴더는 만들지 않고 현재 폴더 위치를 유지합니다.
- Phase 3-A PR #1은 작업 시작 당시 main에 미병합 상태였습니다.
  이번 브랜치는 검증된 3-A 커밋에서 분기했으며 PR의 비교 기준도 3-A 브랜치입니다.
  3-B 브랜치에는 이전 데이터 코드도 포함되어 있으므로 그대로 checkout하여 검증할 수 있습니다.

## 파일 변경표

| 파일 | 처리 |
|---|---|
| Scripts/Board/BoardManager.cs | 수정. StageData 기반 가변 크기·타일별 프리팹 생성·조회·인접 순서·보드 재생성/정리 |
| Scripts/Board/Tile.cs | 수정. TileData 참조·허용 타일/점유 조회·점유 교체 방지·건물 정리 |
| Tests/Editor/BoardContractTests.cs 및 .meta | 신규. 보드 테스트 9개 |
| Docs/DEVELOPMENT_ROADMAP.md | 수정. 3-A 사용자 검증 완료 및 3-B 위치 기록 |
| Docs/PHASE3B_BOARD.md | 신규. 변경·적용·검증 안내 |

스크립트와 테스트 경로는 `Assets/_UrbanEquation` 아래입니다.
기존 BoardManager.cs/Tile.cs의 .meta는 수정하지 않았습니다.
**파일 이동·삭제 없음.** 프리팹, 씬, 임시 ScriptableObjects 및 외부 에셋도 변경하지 않았습니다.
BoardManager가 보드 생성/소유권을 계속 담당하며 Tile이 타일 종류와 점유를 담당합니다.
자원, 카드 소비, 콤보 보상, 목표 판정은 이번 코드에 넣지 않았습니다.

## 런타임 동작

- BoardManager의 Initial Stage가 있으면 그 StageData로 보드를 생성합니다.
- Initial Stage가 없고 기존 Tile Prefab 참조가 있으면 기존 8×8 보드를 생성합니다.
  Prototype은 기존 참조를 유지하므로 Inspector 변경 없이 플레이할 수 있습니다.
- 두 참조가 모두 없으면 빈 상태로 두며, 후속 GameSessionManager가 명시적으로 초기화할 수 있습니다.
- 새로운 보드는 StageData의 Width/Height와 각 TileData.TilePrefab을 사용합니다.
  타일 타입별 비주얼은 해당 TilePrefab에 들어 있는 비주얼을 그대로 사용합니다.
- 문서의 첫 행이 북쪽(+Z)입니다. 타일 좌표의 Y는 월드 Z에 대응합니다.
  스테이지 보드의 원점은 BoardManager의 월드 위치, 간격은 기존과 동일한 1 유닛입니다.
  부모/매니저의 회전과 관계없이 생성 루트의 월드 회전을 0으로 두어 북쪽을 유지합니다.
  프로토타입의 원점은 기존 코드와 동일한 월드 (0,0,0)을 유지합니다.
- 생성 전에 크기·타일 수·타일 타입·모든 프리팹 참조를 검증합니다.
  불완전한 입력이면 false와 오류 설명을 반환하고 기존 보드를 유지합니다.
- 새 루트에서 타일을 전부 준비한 후 기존 보드와 배치 건물을 정리합니다.
  Destroy가 지연되는 플레이 모드에서도 이전 루트와 건물을 즉시 비활성화하여 중복 클릭을 막습니다.
- ResetBoard는 건물과 하이라이트만 초기화하고 타일 배열/TileData는 유지합니다.
- ClearBoard는 생성 타일과 건물까지 정리합니다. BoardManager의 무관한 자식 오브젝트는 삭제하지 않습니다.

## 주요 API

| API | 용도 |
|---|---|
| BoardManager.TryCreateBoard(stage, out error) | 검증 후 스테이지 보드 생성/교체 |
| BoardManager.Width / Height / CurrentStage / HasBoard | 현재 보드 상태 조회 |
| BoardManager.GetTile(coordinate) | 미초기화/범위 밖은 null |
| BoardManager.GetAdjacentTiles(coordinate) | 왼쪽→아래→오른쪽→위. 보드 밖은 건너뜀 |
| BoardManager.CanPlaceBuilding(building, coordinate) | 존재·점유·허용 타일 검사. 자원/카드는 후속 Validator가 검사 |
| BoardManager.ResetBoard() / ClearBoard() | 건물 초기화 / 보드 전체 정리 |
| Tile.Data / Coordinate / Building / IsOccupied | 정적 타일 데이터 및 현재 점유 조회 |
| Tile.Initialize(coordinate, data) | 좌표·TileData 연결. 기존 한 인자 Initialize도 유지 |
| Tile.TrySetBuilding(building) | 다른 건물로 점유 덮어쓰기를 거부. 동일 참조 또는 null은 허용 |
| Tile.SetBuilding(building) | 기존 호출자 호환. 거부된 덮어쓰기에는 경고 |
| Tile.CanAcceptBuilding(building) | 비어 있고 TileData가 있으며 건물 허용 목록에 포함되는지 검사 |
| Tile.ClearBuilding() | 연결 해제 및 해당 건물 오브젝트 제거 |

Tile.SetBuilding(null)은 참조만 해제하므로 Undo 복원 등에서 사용 가능합니다.
Tile.ClearBuilding()은 실제 건물도 제거하므로 Reset/보드 정리에서 사용합니다.
데이터가 없는 레거시 타일의 종류를 외형만 보고 추측하지 않습니다.
레거시 BuildingPlacement는 기존대로 동작하며, 새 배치 API의 타일 검사 연결은 Phase 3-D에서 수행합니다.
BoardSize 상수는 레거시 8×8 호환용입니다. 새 코드에서는 Width/Height를 사용합니다.

## 검증 및 적용

새 BoardContractTests 9개는 혼합 직사각형 보드·북쪽 방향·경계,
유효하지 않은 데이터 입력 시 기존 보드 유지, 프리팹 누락,
재생성 시 이전 건물 정리, Reset, 점유 덮어쓰기 방지,
타일 조건, 인접 순서, 8×8 레거시 초기화를 검사합니다.
기존 DataContractTests 8개는 유지합니다(합계 17개).

현재 실행 환경에는 Unity/C# 컴파일러가 없어 새 테스트를 실행하지 못했습니다.
정적 확인과 테스트 코드 작성 완료를 Unity 실행 성공으로 표시하지 않습니다.

1. Unity를 종료합니다.
2. GitHub Desktop에서 Fetch origin 후 `codex/phase3b-board-tiles` 브랜치를 선택합니다.
3. Unity를 다시 실행하고 Console의 빨간 컴파일 오류를 확인합니다.
4. Test Runner > EditMode에서 DataContractTests 8개와 BoardContractTests 9개를 모두 실행합니다.
5. 기존 Prototype에서 건설·자원·콤보·Reset이 정상 동작하는지 확인합니다.

이번 테스트는 자체적으로 임시 데이터와 타일 오브젝트를 만들고 정리하므로,
기존 Prototype의 Initial Stage를 바꾸거나 새로운 asset/프리팹을 수동으로 만들 필요가 없습니다.
실제 Stage 1/2 데이터와 타일 3종 프리팹 제작은 원래 계획된 후속 단계에서 연결합니다.
