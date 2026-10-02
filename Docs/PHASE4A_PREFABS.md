# Phase 4-A: 공통 프리팹 생성과 Unity 작업 안내

3-J의 전체 테스트·기존 Prototype·디버그 기능은 2026-10-03 사용자 확인으로 검증 완료했습니다.
이번 단계는 공통 프리팹입니다. 최종 HUD·카드 상세 호버는 4-B, Lobby/Game 실제 씬·팝업은 4-C에서 진행합니다.
대형 사무실 B23001은 사용자 선택에 따라 표의 파일명 `Building_House_Block`을 연결합니다.

## 먼저 실행할 작업

1. Unity를 종료합니다. GitHub Desktop에서 Fetch origin 후 **codex/phase4a-common-prefabs**를 선택하고 최신 변경을 받습니다.
2. Unity를 다시 실행해 Console의 컴파일 오류가 없는지 확인합니다.
3. Play를 정지한 상태에서 상단 메뉴 **Tools > Urban Equation > Phase 4A > 1 Create Missing Prefabs and Bind Data**를 실행합니다.
4. Console에 생성·연결 완료 메시지가 나타나면 **2 Validate Prefab Setup**을 실행합니다. 정상 메시지는 `Phase4-A Prefab Validation OK`입니다.
5. Test Runner의 검색/필터를 해제하고 EditMode Run All을 실행합니다. 예상 전체 **269개**, 신규 클래스 **PrefabContractTests 26개**입니다. 메뉴 실행 전에도 테스트할 수 있으나 메뉴 실행 후 다시 확인하면 생성한 데이터 연결까지 검사됩니다.
6. **3 Prefab Gameplay Preview**를 실행합니다. 현재 수정한 씬이 있다면 Unity 저장 확인창에서 저장하거나 취소할 수 있습니다. 미리보기는 임시 씬이며 새로운 정식 씬 파일을 저장할 필요가 없습니다.
7. Game 탭의 해상도 메뉴에서 **1920×1080**을 선택하고 필요하면 Maximize On Play를 켭니다. Play/Continue와 안내 OK는 왼쪽 디버그 패널, 실제 카드 드래그는 하단 카드 목록에서 합니다.
8. 아래 Inspector 확인과 플레이 확인을 마친 뒤 Stop을 누릅니다. GitHub Desktop에서 생성된 프리팹·데이터 변경·이미지 `.meta`를 함께 Commit/Push합니다.

소스 파일을 복사하거나 빈 프리팹을 직접 만드는 작업은 필요하지 않습니다. 메뉴가 생성과 참조 연결을 수행합니다.
이 문서의 경로는 Unity Project 창의 `Assets/`부터 시작하는 프로젝트 상대 경로입니다.

## 메뉴가 생성하는 정확한 파일

| 폴더 | 신규 파일 | 역할 |
|---|---|---|
| Assets/_UrbanEquation/Prefabs/Gameplay/Tiles/ | Pfb_Tile_Asphalt_001.prefab | 아스팔트 타일 |
| Assets/_UrbanEquation/Prefabs/Gameplay/Tiles/ | Pfb_Tile_Concrete_001.prefab | 콘크리트 타일 |
| Assets/_UrbanEquation/Prefabs/Gameplay/Tiles/ | Pfb_Tile_Grass_001.prefab | 잔디 타일 |
| Assets/_UrbanEquation/Prefabs/Gameplay/Buildings/ | BuildingRoot.prefab | 본 개발 건물 공통 루트 |
| Assets/_UrbanEquation/Prefabs/UI/Cards/ | BuildingCard.prefab | 건물 이미지·이름·비활성 오버레이 |
| Assets/_UrbanEquation/Prefabs/UI/Gameplay/ | ComboPopup.prefab | 콤보 이름·설명·보상·페이드 |
| Assets/_UrbanEquation/Data/Presentation/ | GameplayPrefabs.asset | 카드·콤보 프리팹 참조 묶음 |

Unity가 각 파일의 `.meta`와 새 폴더의 `.meta`를 함께 생성합니다. 이 파일들도 Git에 포함합니다.
기존 목적지 프리팹이 있으면 재생성하거나 덮어쓰지 않습니다. 레이아웃을 직접 수정한 뒤 메뉴를 재실행해도 프리팹 편집 내용은 유지됩니다.
재실행은 지정된 데이터 참조와 이미지 import 설정을 다시 연결합니다. 같은 경로에 다른 종류의 asset이 있으면 오류를 표시합니다.
도중 실패 시 이미 생성된 파일을 삭제하지 않습니다. Console에 표시된 원인을 해결하고 다시 실행합니다.

## 자동으로 수정되는 기존 데이터와 이미지

| 대상 | 변경 내용 |
|---|---|
| Assets/_UrbanEquation/Data/Tiles/T00001_Asphalt.asset | Tile Prefab → Pfb_Tile_Asphalt_001의 Tile 컴포넌트 |
| Assets/_UrbanEquation/Data/Tiles/T00002_Concrete.asset | Tile Prefab → Pfb_Tile_Concrete_001의 Tile 컴포넌트 |
| Assets/_UrbanEquation/Data/Tiles/T00003_Grass.asset | Tile Prefab → Pfb_Tile_Grass_001의 Tile 컴포넌트 |
| Assets/_UrbanEquation/Data/GameContent.asset | Building Prefab → BuildingRoot의 BuildingInstance |
| Assets/_UrbanEquation/Data/Buildings/B*.asset 15개 | Visual Prefab 및 Card Image 연결. 비용·획득·허용 타일 수치는 유지 |
| Assets/_UrbanEquation/Sprite/Building_Image/*.png 15개 | Texture Type Sprite (2D and UI), Sprite Mode Single로 import |
| Assets/_UrbanEquation/Sprite/UI/Spr_Ui_Combo_001.png | 같은 Sprite import 설정 |

PNG 원본과 GUID를 유지하고 해당16개 이미지의 `.meta` 설정을 변경합니다. 외부 에셋 팩의 import 설정은 변경하지 않습니다.
기존 `Tile.prefab`, `Building.prefab`, `BuildingSlot.prefab`, `Prototype.unity`, `Test.unity`, `Assets/ScriptableObjects/`는 수정·이동·삭제하지 않습니다.
Fonts/Materials/Sprite 현재 폴더를 유지하고 Art 폴더를 만들지 않습니다. 이번 메뉴는 Build Settings를 바꾸지 않습니다.

## 타일 프리팹에서 확인할 항목

1. `Assets/_UrbanEquation/Prefabs/Gameplay/Tiles/Pfb_Tile_Grass_001.prefab`을 더블 클릭해 Prefab Mode로 엽니다.
2. 루트에 **Tile**과 **Box Collider**가 있어야 합니다. Transform Scale은 기존 타일과 같은 `(1, 0.1, 1)`입니다.
3. Tile의 **Highlight** 슬롯에 루트 아래 `Highlight` 오브젝트가 연결되어 있어야 합니다. Highlight의 기본 활성 상태는 꺼져 있습니다.
4. 루트 아래 기존 도로 외형과 새 **Surface**가 함께 있어야 합니다. Surface는 종류별 `Tile_Plain_Asphalt/Concrete/Grass` 모델이며 도로 테두리 안쪽의 폭0.72 영역에 맞춰집니다.
5. Surface의 Collider는 제거되며 입력은 루트 Box Collider로 처리합니다. 새 Collider를 Surface에 추가하지 않습니다.
6. Asphalt와 Concrete 프리팹도 같은 구조인지 확인합니다. 색은 각각 짙은색/회색/녹색이며 외부 pack의 기존 재질을 그대로 사용합니다.
7. 세 TileData asset을 선택해 **Tile Prefab**이 서로 다른 새 프리팹을 가리키는지 확인합니다. 직접 설정할 필요는 없지만, 슬롯이 비었다면 위 표의 프리팹을 Project 창에서 드래그해 연결합니다.

도로·Surface·Highlight 형상은 Scene/Prefab 뷰에서 시각적으로 확인합니다. 수치 조정은 해당 새 프리팹에서만 합니다.

## 건물 루트와 외형에서 확인할 항목

1. `Assets/_UrbanEquation/Prefabs/Gameplay/Buildings/BuildingRoot.prefab`을 엽니다.
2. 루트에 **BuildingInstance**와 기존 루트 Collider가 있고 Scale은 `(0.1, 0.1, 0.1)`이어야 합니다.
3. BuildingInstance의 새 **Visual Offset**은 생성된 Surface 윗면에 건물 밑면을 맞추는 값입니다. 메뉴가 타일 높이를 측정해 자동 설정합니다. 보드 좌표·루트 위치는 그대로이며 외형 자식만 올라갑니다.
4. BuildingRoot 자체에는 특정 건물 외형을 직접 넣지 않습니다. 런타임에 BuildingData의 Visual Prefab이 생성됩니다.
5. Preview에서 건물이 Surface 속에 묻히거나 떠 보이는 경우, 새 BuildingRoot의 Visual Offset Y만 조금 조정합니다. 루트 Scale Y가0.1이므로 Visual Offset Y를0.1 변경하면 월드 높이는0.01 바뀝니다. 기존 Building.prefab은 편집하지 않습니다.
6. `Assets/_UrbanEquation/Data/Buildings/`의 각 asset을 선택해 **Visual Prefab**과 **Card Image**가 비어 있지 않은지 확인합니다.

외형의 공통 경로는 `Assets/polyperfect/Low Poly Ultimate Pack/_T/Prefabs_T/Buildings_T/`입니다.
아래 표의 이름에 `.prefab`을 붙입니다. 카드 이미지의 공통 경로는 `Assets/_UrbanEquation/Sprite/Building_Image/`이며 이름에 `.png`를 붙입니다.

| BuildingData 파일 | Visual Prefab 이름 | Card Image 이름 |
|---|---|---|
| B11001_SmallHouse.asset | Building_House_Block | Spr_Ui_SHouse_001 |
| B12001_MediumHouse.asset | Building_House_Family_Small | Spr_Ui_MHouse_001 |
| B13001_LargeHouse.asset | Building_House_Middle | Spr_Ui_BHouse_001 |
| B21001_SmallOffice.asset | Building_Office_Rounded | Spr_Ui_SOffice_001 |
| B22001_MediumOffice.asset | Building_Office | Spr_Ui_MOffice_001 |
| B23001_LargeOffice.asset | Building_House_Block | Spr_Ui_BOffice_001 |
| B31001_Restaurant.asset | Building_Restaurant | Spr_Ui_SRestaurant_001 |
| B32001_Cafe.asset | Building_Cafe | Spr_Ui_MCafe_001 |
| B33001_Casino.asset | Building_Casino | Spr_Ui_BCasino_001 |
| B41001_SmallFactory.asset | Industry_Storage | Spr_Ui_SFactory_001 |
| B42001_MediumFactory.asset | Industry_Factory_Old | Spr_Ui_MFactory_001 |
| B43001_LargeFactory.asset | Incineration_Plant | Spr_Ui_BFactory_001 |
| B51001_Lighthouse.asset | Lighthouse | Spr_Ui_SLighthouse_001 |
| B52001_Temple.asset | Building_Temple_China | Spr_Ui_MTemple_001 |
| B53001_Stadium.asset | Building_Stadium | Spr_Ui_BStadium_001 |

## 건물 카드 프리팹에서 확인할 항목

1. `Assets/_UrbanEquation/Prefabs/UI/Cards/BuildingCard.prefab`을 엽니다. 루트에 RectTransform, Image, Button, LayoutElement, **BuildingCardUI**가 있어야 합니다.
2. BuildingCardUI의 **Building Name Text** → `BuildingName`의 TextMeshProUGUI, **Card Image** → `BuildingImage`의 Image, **Disabled Overlay** → `DisabledOverlay`의 Image, **Button** → 루트 Button이 연결되어야 합니다.
3. 루트 Image의 Raycast Target은 켜고 BuildingImage·BuildingName·DisabledOverlay의 Raycast Target은 끕니다. 오버레이가 입력을 가로채지 않고 카드 코드가 비활성 입력을 거부합니다.
4. DisabledOverlay가 마지막 자식이고 전체 카드에 Stretch되어 있어야 합니다. Image Color는 RGBA `(0,0,0,0.5)`입니다. 기본 꺼짐 상태이며 자원 부족/사용된 카드의 상태 갱신에서 켜집니다.
5. 루트 크기는110×140, LayoutElement Preferred Width/Height도110/140입니다. 이름 글꼴은 NotoSansKR-Medium SDF입니다.
6. 이미지와 이름은 실행 중 카드 Bind에서 채워집니다. 빈 프리팹에 특정 BuildingData나 Card Image를 고정해서 넣을 필요가 없습니다.

## 콤보 프리팹에서 확인할 항목

1. `Assets/_UrbanEquation/Prefabs/UI/Gameplay/ComboPopup.prefab`을 엽니다.
2. **ComboPopupUI**의 Combo Name Text → `ComboName`, Description Text → `Description`, Rewards Text → `Rewards`, Canvas Group → 루트 CanvasGroup 연결을 확인합니다.
3. `ComboHeader` Image에 `Spr_Ui_Combo_001`이 들어 있고 모든 Graphic의 Raycast Target이 꺼져 있어야 합니다. 루트 CanvasGroup의 **Interactable/Blocks Raycasts**도 꺼져 있습니다.
4. 기본 Display Duration=2초, Fade Duration=0.25초, Screen Offset=(0,80)입니다. 이 값은 초기 연출값이며 이 새 프리팹의 Inspector에서 조정할 수 있습니다.
5. 실행 중 콤보 발생 위치 위에 이름·설명·부호가 있는 보상을 표시합니다. 획득은 녹색, 감소는 빨간색이며 이미 적용된 보상을 다시 지급하지 않습니다.
6. 여러 결과는 관리자 큐 순서로 표시합니다. 팝업을 비활성화하면 표시를 멈추고 큐를 남겨 두며 재활성화 시 현재 결과부터 표시합니다.

`Assets/_UrbanEquation/Data/Presentation/GameplayPrefabs.asset`에서는 **Building Card Prefab** → BuildingCard, **Combo Popup Prefab** → ComboPopup 참조를 확인합니다.

## 미리보기 플레이 확인

- 기존 저장이 있으면 Play → 새로 시작하기 또는 Continue → Stage Select에서 원하는 단계를 선택합니다. 저장 파일은 이전 디버그와 같은 `Application.temporaryCachePath/UrbanEquationDebug/phase3j-progress.json`입니다.
- 안내 OK 후 하단 카드를 타일 위까지 누른 채 드래그합니다. 유효 타일에서만 Highlight가 켜지고, 놓으면 건설·카드 제거·남은 카드 재정렬이 이루어져야 합니다.
- 타일이 없는 곳/이미 점유된 타일/다른 UI 위에 놓으면 건설되지 않아야 합니다.
- Stage1에서 두 주택을 상하좌우로 인접하게 건설하면 콤보 팝업이 자동으로 나타났다 사라지고 Population=3이어야 합니다. Undo 후 카드와 자원·목표를 확인하고 다시 건설합니다.
- Stage2 처음에는 소형 사무실·공장·등대가 자원 부족으로 검은 반투명 오버레이를 표시하고, 눌러도 드래그 프리뷰가 시작되지 않아야 합니다. 주택·식당은 활성입니다.
- Stage2의 랭크3 순서 A: 주택(0,0) → 등대(0,1) → 공장(0,2) → 식당(1,0) → 사무실(1,2). 자원과 두 인접 목표를 확인하고 완료합니다.
- 왼쪽 패널은 단계 이동·자원·Undo 등을 위한 기존 디버그 조작입니다. 최종 HUD 배치는 다음 단계입니다.
- Pause/안내/Clear에서는 하단 카드 목록과 팝업이 숨겨져야 합니다. Pause 취소 시 목록이 복원되어야 합니다.
- Stop 후 기존 `Assets/_UrbanEquation/Scenes/Prototype.unity`도 실행하여 기존 건설·자원·콤보·Reset 호환을 확인합니다.

## 오류가 발생하면 확인할 파일

| 증상 | 확인 위치와 조치 |
|---|---|
| 메뉴가 보이지 않음 | 먼저 Console 컴파일 오류를 확인. Editor/UrbanEquationPrefabSetup.cs·UrbanEquationPrefabFactory.cs가 최신 브랜치에 있는지 확인 |
| Required font 오류 | Assets/_UrbanEquation/Fonts/NotoSansKR-Medium SDF.asset 존재/정상 import 확인. 원본 NotoSansKR-Medium.otf는 그대로 유지 |
| 한국어 글자가 네모/누락 경고 | 위 SDF asset Inspector에서 Source Font File이 NotoSansKR-Medium.otf인지 확인. 새 글자가 필요하면 Atlas Population Mode를 Dynamic으로 설정하고 Apply/저장. 기존 폰트 파일을 삭제하지 않음 |
| Missing building/tile input | Console에 표시된 정확한 원본 asset 경로 확인. 외부 팩 폴더를 이동하지 않음 |
| 기존 목적지 프리팹 참조 누락 | 해당 새 프리팹을 열어 위 BuildingCardUI/ComboPopupUI 슬롯 연결 후 저장하고 Validate 재실행 |
| 카드 이미지가 비어 있음 | 해당 BuildingData의 Card Image와 Sprite/Building_Image의 PNG import 설정 확인. 메뉴1 재실행으로 참조 복구 |
| 카드가 반응하지 않음 | Preview 씬 Hierarchy의 Preview EventSystem에 InputSystemUIInputModule, Preview Canvas에 GraphicRaycaster 확인. 자원 부족 카드는 입력을 받지 않는 것이 정상 |
| 생성 중 실패 | 생성된 파일은 유지됨. 오류 원인을 해결하고 메뉴1 → 메뉴2 재실행. 이미 수정한 프리팹은 재생성하지 않음 |

## Git에 올릴 변경

메뉴 실행 후 새 프리팹6개와 GameplayPrefabs.asset 및 그 `.meta`, 새 `Data/Presentation`·`Prefabs/UI/Gameplay` 폴더 `.meta`를 포함합니다.
또한 수정된 건물15개 asset, TileData3개 asset, GameContent.asset, 이미지16개 `.png.meta`도 포함합니다.
정상 기본 생성 기준으로 새파일16개·기존수정35개입니다. 이미 파일이 있거나 직접 모양을 수정한 경우 목록은 달라질 수 있습니다.
폰트 설정을 조정했다면 폰트 변경도 포함합니다. 이번 미리보기 씬은 정식 씬 asset으로 저장하지 않습니다.
이 브랜치에서 Commit/Push하신 뒤 테스트·생성 검증·카드 드래그/오버레이/콤보 결과를 알려주시면 4-B로 이어갑니다.

## 이번 코드 변경과 검증 경계

- 신규: Editor/UrbanEquationPrefabFactory.cs·UrbanEquationPrefabSetup.cs, Scripts/Data/GameplayPrefabSet.cs, Scripts/UI/Gameplay/ComboPopupUI.cs·GameplayPrefabPreview.cs, Tests/Editor/PrefabContractTests.cs 및 각 meta.
- 수정: Editor/UrbanEquationDebugMenu.cs(미리보기 메뉴), Scripts/Building/BuildingInstance.cs(외형 오프셋, 기존 기본값0), Docs/DEVELOPMENT_ROADMAP.md.
- 기존 관리자의 게임 규칙과 테스트243개 파일, Prototype용 프리팹/씬, 임시 ScriptableObjects는 유지합니다. 삭제/이동 없음.
- AI 확인: 입력 asset 경로/참조·C# 구문·테스트 선언269개·변경 범위 및 원격 반영.
- 이 환경에는 Unity/C# 컴파일러가 없어 에디터 생성 메뉴·실제 프리팹 import·EditMode·Play·렌더링은 사용자 Unity 실행 검증 대기입니다. 프리팹 파일은 메뉴 실행 시 사용자 프로젝트에서 생성됩니다.

Unity API 참고: [SaveAsPrefabAsset](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PrefabUtility.SaveAsPrefabAsset.html), [SaveAssetIfDirty](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AssetDatabase.SaveAssetIfDirty.html), [InputSystemUIInputModule](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html).
