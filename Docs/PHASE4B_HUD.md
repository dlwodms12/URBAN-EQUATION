# Phase 4-B: 게임 HUD·카드/건물 상세·콤보 조회

기준은 2026-10-03 최신 PDF(5) 12·24~27페이지와 사용자 확정 규칙입니다.
사용자가 4-A 테스트·실제 동작 확인 후 프리팹6개/연결 데이터를 Push한 커밋 `02d97210cb1d0fce77181283b0fb56b87f7d73e1`에서 시작합니다.
이번 확인 발화에는 테스트 수를 재명시하지 않았으며 앞서 안내한 전체272개 검증 완료 확인으로 반영합니다.
현재 작업은4-B이며, 실제 Lobby/Game씬·최종메뉴·팝업은4-C입니다.

## 사용자 작업 순서

1. Unity를 종료하고 GitHub Desktop에서 Fetch origin → **codex/phase4b-game-hud** 선택 → 최신 Pull합니다.
2. Unity 재실행 후 Console에 컴파일 오류가 없는지 확인합니다. 4-A에서 만든 프리팹과 데이터는 새 브랜치에 포함됩니다.
3. Play를 정지한 상태에서 Unity 창 **맨 위 메뉴**를 엽니다. **Tools → Urban Equation → Phase 4B → 1 Create Missing HUD and Bind Icons**를 실행합니다.
4. 같은 하위 메뉴의 **2 Validate HUD Setup**을 실행합니다. 정상 로그는 `Phase4-B HUD Validation OK`입니다.
5. Test Runner 검색·필터 해제 → EditMode → Run All. 예상 전체 **318개**, 새 **GameHudContractTests 46개**입니다. 테스트는 메모리에서 임시 HUD를 만들며 생성 메뉴·실제 asset 저장/이미지 import를 자동 실행하지 않습니다. 생성물 연결은 메뉴2로 별도 검사합니다.
6. 같은 메뉴의 **3 HUD Gameplay Preview**를 실행합니다. 저장 확인창이 뜨면 현재 수정 씬을 저장하거나 취소할 수 있습니다. 새 정식 씬은 만들지 않으며 임시 씬이 생성되고 Play가 시작됩니다.
7. **Game 탭**의 해상도를 **1920×1080 또는1280×720(16:9)**으로 설정합니다. Lobby/단계선택/안내/완료/중단 확인은 이번 단계까지 기존 왼쪽 디버그 패널을 사용합니다. 안내 OK 후 Playing에 들어가면 그 패널이 숨겨지고 실제 HUD가 표시됩니다.
8. 아래 플레이 확인을 진행하고 Stop을 누릅니다. 기존 `Assets/_UrbanEquation/Scenes/Prototype.unity`도 건설·자원·콤보·Reset을 확인합니다.
9. 새 HUD prefab/설정 asset/.meta와 수정된 아이콘 import `.meta`를 **같은4-B브랜치에 Commit/Push**합니다. 미리보기 임시 씬은 정식 씬 asset으로 저장하지 않습니다.

소스 복사나 빈 UI 오브젝트 수동 생성은 필요 없습니다. 생성 메뉴가 Canvas와 참조를 연결합니다.
메뉴 이름의1/2/3은 실제 Unity 상단메뉴 항목입니다. Project 창에서 같은 이름의 파일을 찾는 조작이 아닙니다.

## 메뉴가 생성하는 파일과 자동 변경

| 프로젝트 상대 경로 | 내용 |
|---|---|
| Assets/_UrbanEquation/Prefabs/UI/Gameplay/Pfb_Main_GameHud_001.prefab | Canvas·자원·목표3개·카드목록·Undo/NEXT/Pause·상세·자동/조회 콤보를 포함한 전체HUD |
| Assets/_UrbanEquation/Data/Presentation/ResourceIcons.asset | 자원5종 × 현재/획득/소비3색 Sprite 참조 |
| Assets/_UrbanEquation/Data/Presentation/GameplayHud.asset | HUD 프리팹과 ResourceIcons 참조 |
| Assets/_UrbanEquation/Sprite/Icon/Spr_Ui_I*.png.meta 15개 | 기존 PNG를 Sprite (2D and UI) / Single로 import |

각 신규 파일의 `.meta`도 함께 생성합니다. 기존 `Data/Presentation` 및 `Prefabs/UI/Gameplay` 폴더를 재사용합니다.
기본 최초 생성은 신규6파일(3asset+meta), 기존아이콘meta15개 수정입니다. 이미 Sprite로 import했거나 생성물을 편집한 경우 목록은 달라질 수 있습니다.
PNG 원본/GUID·외부GUI팩 import 설정은 유지합니다. 기존 공통카드/콤보/타일/건물루트 프리팹은 덮어쓰지 않습니다.
목적지HUD가 이미 있으면 다시 만들지 않아 사용자 레이아웃 편집을 유지합니다. 재실행은 아이콘과설정asset 참조를 재연결합니다.
도중 실패하면 생성 완료 파일을 유지합니다. Console 원인을 해결하고 메뉴1→2를 다시 실행합니다.

## 자원 아이콘 대응표

공통 폴더는 `Assets/_UrbanEquation/Sprite/Icon/`입니다. 아래 접두어 뒤 `_001.png`(현재/검은색), `_002.png`(획득/초록색), `_003.png`(소비/빨간색)를 붙입니다.

| 순서/ResourceType | PNG 접두어 | 화면 의미 |
|---|---|---|
| 0 Population | Spr_Ui_IPopulation | 인구 |
| 1 Jobs | Spr_Ui_IJob | 일자리 |
| 2 Money | Spr_Ui_IFunds | 자금 |
| 3 Logistics | Spr_Ui_IGoods | 물류 |
| 4 Tourism | Spr_Ui_ITourism | 관광 |

ResourceIcons.asset Inspector의 **Neutral / Gained / Spent** 배열은 각각5개이며 위 순서입니다.
도시 상태는 검은색 아이콘과 현재 수치, 상세와콤보는 초록색/빨간색 아이콘과 `+`/`-` 수치입니다. 비용과획득을 표시만 하며 자원값을 수정하지 않습니다.

## HUD 프리팹 Inspector 확인

`Assets/_UrbanEquation/Prefabs/UI/Gameplay/Pfb_Main_GameHud_001.prefab`을 더블 클릭해 Prefab Mode로 엽니다.
루트에는 **Canvas / CanvasScaler / GraphicRaycaster / GameHudUI**가 있어야 합니다.
Canvas는 Screen Space - Overlay, CanvasScaler는 Scale With Screen Size / Reference Resolution1920×1080 / Match0.5입니다.
`Gameplay` 자식은 시작 시 꺼져 있고 Playing에서 런타임에 켜집니다. Prefab Mode에서 필요하면 잠시 켜서 배치를 본 뒤 기본꺼짐으로 저장합니다.

| GameHudUI 슬롯 | 연결 오브젝트/컴포넌트 |
|---|---|
| Gameplay Root | Gameplay |
| City Resources | Gameplay/City Status/Resources의 ResourceStripUI |
| Stage Title | Gameplay/Stage Goals/Stage Title의 TMP |
| Goals (3개) | Stage Goals/Goal 0·Goal 1·Goal 2의 StageGoalRowUI |
| Hand / Card Content | Building Hand의 BuildingHandUI / Viewport/Cards |
| Common Prefabs | Data/Presentation/GameplayPrefabs.asset |
| Undo View / Undo Button | Gameplay/Undo의 UndoButtonUI / Button |
| Next View / Next Button | Gameplay/Next Stage의 NextStageButtonUI / Button |
| Pause Button | Gameplay/Pause의 Button |
| Placement Feedback | Gameplay/Placement Feedback의 TMP |
| Tooltip | Gameplay/Building Tooltip의 BuildingTooltipUI |
| Automatic Combo / Replay Combo | Gameplay/Automatic Combo·Combo Replay의 ComboPopupUI |
| Board Details | Gameplay의 BoardDetailsUI |

모든 참조는 메뉴가 자동 연결합니다. 비어 있으면 해당 자식 컴포넌트를 슬롯에 드래그해 연결하고 저장 후 메뉴2를 실행합니다.
Goals 각 행은 Description TMP / Background Image / Achieved Sprite(파란색) / Pending Sprite(분홍색) 참조가 있습니다.
달성 행은 파란색 배경과 취소선, 미달성 행은 분홍색 배경과 일반 글꼴입니다. Undo/Retry/단계전환 시 다시 갱신됩니다.
ResourceStripUI는 **Icons → ResourceIcons.asset**, **Font → NotoSansKR-Medium SDF**입니다. 실행 중 Icon/Amount 자식을 만들며 prefab에 수치를 고정하지 않습니다.
Tooltip과두Combo의 CanvasGroup **Interactable / Blocks Raycasts**는 꺼져 있어야 합니다. 모든 장식 Image/TMP의 Raycast Target도 꺼집니다.
자원·목표·카드 컨테이너와 버튼의 Image는 UI 위 배치를 차단하기 위해 Raycast Target을 유지합니다.

## 카드/건물 상세와 배치 확인

- **좌측 상단** 도시 상태에5종 자원, **우측 상단** Stage/필수·추가목표3개, **하단 중앙** 카드 목록, **하단 좌우** Undo/NEXT가 표시됩니다.
- 카드 위에 마우스를 올리면 해당 건물의 허용타일과획득/소비 자원을 표시합니다. 자원부족 카드는 오버레이와입력차단을 유지하며 상세는 볼 수 있습니다.
- 카드를 누른 채 보드로 드래그하면 건물프리뷰와상세가 함께 이동합니다. 유효타일 Highlight와 초록색 `건설 가능`, 잘못된 타일/점유/UI영역은 빨간색 안내를 확인합니다. 놓거나취소하면 안내가 제거됩니다.
- 성공하면 사용카드가 제거되고 나머지가 왼쪽으로 당겨집니다. Undo에서원래순서를복원합니다.
- 지어진건물 위에마우스를올리면같은상세를표시합니다. 건물에서포인터를빼면닫힙니다.
- Stage2 시작시주택·식당만활성이고사무실·공장·등대는검은반투명Overlay/입력차단입니다. 주택건설로자원이바뀌면활성상태를갱신합니다.
- 첫건설후Undo는비활성, 두번째부터활성이라는사용자확정규칙을유지합니다. A→B→C 이후Undo는C→B→A까지이며빈보드로돌아가지않습니다.
- NEXT는필수목표달성시켜지고추가목표를위해계속건설할수있습니다. NEXT를누르면Flow의완료화면으로이동합니다.
- 상단일시정지버튼또는ESC는기존중단확인화면을엽니다. 취소시HUD/카드/자원이복원됩니다. 이번단계에서최종팝업이나실제씬로드는추가하지않습니다.

## 콤보 자동 표시와 다시 보기 확인

1. Stage1에서첫주택(0,0)→둘째주택(1,0)을드래그건설합니다. Population=3이며두건물사이에콤보이름·설명·초록색보상이표시되어야합니다.
2. HUD자동팝업의전체표시시간은 **1.5초**, 마지막 **0.5초**에페이드합니다. 여러결과는기존관리자큐순서대로표시합니다. 시간설정은새HUD안의Automatic Combo에서조정할수있으며기존공통ComboPopup.prefab값은그대로입니다.
3. 자동표시가끝난뒤지어진두주택을차례로클릭합니다. 같은콤보조회팝업이두건물사이에나타나고시간이흘러도남아있어야합니다.
4. 빈곳·다른건물·UI를클릭하면조회가닫힙니다. 한건물을반복클릭하거나대각선/콤보없는쌍에서는조회팝업이뜨지않아야합니다.
5. 조회전후Population=3과자원전체값이동일한지확인합니다. 조회는보상을다시지급하거나자동큐를넘기지않습니다.
6. Undo/Retry/Pause/단계이탈에서는선택과조회가닫힙니다. Undo에서복원된현재보드객체를기준으로새로조회합니다.

Stage1/2에는한번의건설로복수콤보를만드는세주택카드가없습니다. 복수결과순서는기존Combo/Prefab 회귀를포함하여확인하며, 향후Stage데이터가추가되어도같은큐를사용합니다.
Stage2 랭크3 예시: 주택(0,0)→등대(0,1)→공장(0,2)→식당(1,0)→사무실(1,2). 사용전카드활성·각자원·목표3개색과NEXT를확인합니다.

## 문제가 있으면

| 증상 | 확인할 위치/조치 |
|---|---|
| 메뉴가보이지않음 | Unity맨위 Tools/Urban Equation/Phase4B, 최신4-B브랜치와Console컴파일오류 확인. Editor/UrbanEquationHudSetup.cs·DebugMenu.cs |
|4-A검증오류 | 기존Phase4A메뉴2확인. GameContent/타일/건물/GameplayPrefabs의사용자Push가브랜치에있는지확인 |
| Required GUI sprite 오류 | Console원본경로확인. 외부GUI팩의폴더를이동하거나import설정을임의변경하지않음 |
| Missing resource image 오류 | Sprite/Icon의위15PNG가있는지확인. 메뉴1재실행으로Sprite/Single설정연결 |
| 카드가반응하지않음 | Preview EventSystem의InputSystemUIInputModule,HUD루트GraphicRaycaster,안내OK/Playing및자원부족여부 확인 |
| 한국어/타일동그라미 글리프누락 | Fonts/NotoSansKR-Medium SDF.asset의SourceFont가NotoSansKR-Medium.otf인지확인. 필요시Atlas Population Mode를Dynamic으로설정하여저장. 폰트를삭제하지않음 |
| HUD가안보임 | Gameplay는Playing에서만켜짐. 왼쪽디버그Play/Continue/단계선택/안내OK후확인.16:9 Game해상도/MaximizeOnPlay확인 |
| 기존HUD슬롯누락 |새HUDprefab을열어위표의참조를연결하고저장후메뉴2. 메뉴1은기존HUD레이아웃을덮어쓰지않음 |

## 파일 변경 범위와 검증 경계

- 신규: Scripts/Data/ResourceIconSet.cs·GameplayHudSet.cs, Scripts/UI/Gameplay의ResourceStripUI·StageGoalRowUI·BuildingTooltipUI·BuildingCardHoverUI·BoardDetailsUI·GameHudUI·GameplayHudPreview, Editor/UrbanEquationHudFactory.cs·UrbanEquationHudSetup.cs, Tests/Editor/GameHudContractTests.cs 및meta,이문서.
- 수정: BuildingHandUI(옵션호버부착),ComboPopupUI(옵션자원아이콘/시간설정/별도조회모드),GameFlowDebugUI(4-B에서만Playing패널숨김),Editor/UrbanEquationDebugMenu(4-B미리보기),DEVELOPMENT_ROADMAP.md.
- 기존4-A생성프리팹/데이터/이미지PNG와이전272개테스트/assembly/meta,Prototype/Test,임시Assets/ScriptableObjects유지. 삭제/이동없음. 새정식씬/BuildSettings수정없음.
- AI확인: 기획화면시각판독/사용자커밋참조/GUID·구문·테스트선언318개·변경범위/원격반영. 구문검사는컴파일이아닙니다.
- 이환경에는Unity/C#컴파일러가없어실제생성/import/컴파일/EditMode/Play/렌더링은사용자검증대기입니다. 신규HUDasset3개는사용자Unity메뉴실행에서생성합니다.
- 테스트경로는GUID가포함된OS임시폴더이며종료시정리합니다. 기본실사용저장파일을쓰기/씬로드/종료하지않습니다. 미리보기저장경로는기존debug와같은temporaryCachePath/UrbanEquationDebug/phase3j-progress.json입니다.
- 확인및생성파일Push후4-C실제Lobby/Game씬·최종메뉴/팝업/LoadScene/Application.Quit를진행합니다.

Unity API 참고: [TMP FontStyles](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/api/TMPro.FontStyles.html), [GetComponentsInChildren](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Component.GetComponentsInChildren.html).
