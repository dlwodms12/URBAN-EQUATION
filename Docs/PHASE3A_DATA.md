# Phase 3-A 변경 및 적용 안내

## Git 폴더 이동 점검

확인한 커밋: `dd8544760ec141ae2376677e605467513430cc8f` (폴더 구조 최적화).
이동된 파일 및 `.meta`는 모두 내용 변경 없는 rename으로 확인했습니다.
따라서 이번 이동으로 스크립트·프리팹·씬 GUID가 바뀐 흔적은 없습니다.
Unity Console 무오류 여부는 사용자가 확인한 결과이며, 원격 점검만으로 Inspector 전체를 검증하지는 못합니다.

| 항목 | 상태/처리 |
|---|---|
| `Assets/ScriptableObjects` | 사용자 요청대로 임시 데이터 위치 유지 |
| `_UrbanEquation/Fonts`, `Materials`, `Sprite` | Art 하위라는 원안과 차이는 있지만 기능상 문제 없음. 현 위치 유지 |
| `BuildingSlot.prefab` | Gameplay/Buildings에 있는 기존 UI 프리팹 유지. Phase 4에서 BuildingCard로 대체 |
| Build Settings | 남아 있던 `Assets/Scenes/Prototype.unity` 경로를 실제 이동 경로로 수정. 기존 enabled 값 유지 |

## 파일 변경표

데이터 스크립트 경로는 `Assets/_UrbanEquation/Scripts/Data`입니다.

| 파일 | 처리·역할 |
|---|---|
| ResourceType.cs | 신규. 기존 ResourceManager 안 enum을 추출. Money=2 유지, Tourism=4 추가 |
| ResourceAmount.cs | 신규. 자원 종류 및 정수 수량 |
| ResourceData.cs | 신규. R코드·이름·일반/획득/소비 아이콘 |
| TileType.cs, TileData.cs | 신규. 타일 3종·T코드·Tile 프리팹 |
| BuildingData.cs | 수정. 카드 이미지·요구/획득 목록·허용 타일. 기존 필드와 .meta 유지 |
| BuildingCatalog.cs | 신규. 정적 건물 조회. 기존 BuildingDatabase는 아직 유지 |
| ComboDefinition.cs, ComboDatabase.cs | 신규. 설명·건물 쌍·보상. 동일 콤보 코드를 공유하는 표 행 허용 |
| StageBuildingCardData.cs | 신규. 순서 있는 카드 항목과 수량 |
| StageGoalData.cs | 신규. 자원 최소치/모든 카드 사용/특정 건물 쌍 인접 목표 |
| StageData.cs | 신규. 크기·타일 배열·초기 자원·카드·필수 목표 1개·추가 목표 2개 |
| DataValidation.cs | 신규. 중복 자원/음수 수량 등 데이터 입력 오류 보고 |
| Resource/ResourceManager.cs | enum 선언 제거. Money 명칭 및 Tourism 초기값 추가. 기존 initialGoods 필드명 보존 |
| UI/ResourceUI.cs, UI/ComboUI.cs | Goods 참조를 Money로 갱신. 표시 명칭을 자금으로 변경 |
| ProjectSettings/EditorBuildSettings.asset | 이동 전 경로 수정 |
| Tests/Editor/DataContractTests.cs 및 asmdef | 신규. 데이터 계약 검증용 EditMode 테스트 8개 |
| Docs/DEVELOPMENT_ROADMAP.md, PHASE3A_DATA.md | 신규. 진행 상태와 적용 안내 |

**이번 단계에서 삭제·이동한 기존 파일은 없습니다.** 신규 Unity 파일에는 .meta가 포함됩니다.
Resource enum의 책임만 ResourceManager에서 ResourceType.cs로 이동합니다.
기존 BuildingPlacement/BuildingDatabase/ResetManager/UI 및 4개 임시 asset은 유지합니다.

## 데이터 입력 규칙

- 코드 숫자 필드에는 B/C 접두어 없이 11001 등을 입력합니다. Code 프로퍼티가 B11001/C11001로 표시합니다.
- 신규 BuildingData는 `Use Resource Lists`를 켭니다. 기존 4개 임시 데이터는 꺼진 상태를 유지합니다.
- 요구 자원 수량은 양수로 입력합니다(일자리 -3이라는 기획은 Jobs, 3).
  실제 차감은 Phase 3-C에서 수행합니다. 획득 자원도 양수로 입력합니다.
- 허용 타일이 비어 있으면 건설 허용 목록이 없는 데이터로 판정합니다.
  모든 타일에서 건설 가능한 건물은 Asphalt/Concrete/Grass를 모두 입력합니다.
- 기존 Produce/Consume 프로퍼티는 프로토타입 호출자 호환용입니다.
  새 런타임은 RequiredResources/GainedResources를 사용해야 합니다.
  새 건물 데이터를 기존 BuildingPlacement에 연결하면 복수 자원 규칙을 처리하지 못하므로 연결하지 않습니다.
- StageData의 Initial Resources에는 5종을 중복 없이 전부 입력하며, 0도 명시합니다.
- Tile 배열은 문서 위쪽 행부터, 각 행은 왼쪽부터 입력합니다.
  (x,z)의 인덱스는 `(Height - 1 - z) * Width + x`입니다.
- 카드 항목은 입력 순서대로 펼치고 Count만큼 독립 카드를 생성하도록 후속 런타임이 처리합니다.
- 목표 타입은 현재 Stage 1/2에 필요한 3가지로 정의했습니다.
  랭크 판정은 필수 목표 + 추가 목표 2개의 달성 상태로 Phase 3-G에서 구현합니다.
- Validate 메서드는 입력 오류 목록을 반환하도록 설계했으며 기획 데이터를 자동 보정하지 않습니다.

## 검증

이번 환경에서 수행: 이동 커밋 diff, 기존 .meta 보존, 기존 데이터 무변경,
자원 enum 번호/참조, Build Settings 씬 GUID/경로, 신규 metadata 및 test asmdef 검사.
수행하지 못함: Unity 컴파일, EditMode 테스트 실행, Inspector/게임 실행.

Unity 적용 후 확인 순서:

1. GitHub Desktop에서 Fetch origin 후 `codex/phase3a-data-definitions` 브랜치를 선택합니다.
2. Unity import가 끝나면 Console의 빨간 컴파일 오류를 확인합니다.
3. Window > General > Test Runner > EditMode에서 `DataContractTests` 8개를 실행합니다.
4. Prototype 씬에서 기존 건물 4종의 참조 및 배치/자원/Reset 흐름을 확인합니다.
5. 성공 결과를 로드맵에 기록하고 PR 병합 후 main을 Pull한 뒤 Phase 3-B를 진행합니다.

테스트는 enum 직렬화 번호, 레거시 건물 자원, 복수 자원 목록,
북쪽 행 좌표 대응, 건물 순서와 무관한 콤보, 동일 콤보 코드 여러 행,
타일 허용 목록, 초기 자원의 중복/음수 오류를 확인합니다.
테스트 asmdef는 프로토타입의 Assembly-CSharp 경계를 변경하지 않도록 Reflection으로 데이터 타입을 조회합니다.
