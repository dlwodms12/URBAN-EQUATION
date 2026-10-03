using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UrbanEquationHudFactory
{
    public static GameObject CreateHud(GameplayPrefabSet common, ResourceIconSet icons, TMP_FontAsset font,
        Sprite city, Sprite goals, Sprite pending, Sprite achieved, Sprite undo, Sprite next, Sprite details, Sprite star)
    {
        if (common == null || common.BuildingCardPrefab == null || common.ComboPopupPrefab == null || icons == null
            || font == null || city == null || goals == null || pending == null || achieved == null
            || undo == null || next == null || details == null || star == null)
            throw new ArgumentException("HUD construction requires common prefabs, icon set, font and all eight GUI sprites.");
        var root = Rect("Pfb_Main_GameHud_001", null, new Vector2(1920,1080));
        var canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
        root.AddComponent<GraphicRaycaster>();
        var view = root.AddComponent<GameHudUI>();
        var playing = Rect("Gameplay",root.transform,Vector2.zero); Stretch(playing);
        var cityPanel = Panel("City Status",playing.transform,city,new Vector2(.015f,.83f),new Vector2(.32f,.97f));
        Label("City Title",cityPanel.transform,font,"도시 상태",new Vector2(0,1),new Vector2(0,1),new Vector2(18,-14),new Vector2(230,38),25,Color.white);
        var resources = Rect("Resources",cityPanel.transform,new Vector2(400,45));
        Place(resources,new Vector2(0,0),new Vector2(0,0),new Vector2(20,15),new Vector2(400,45));
        var cityStrip = Strip(resources,icons,font,25);
        var goalPanel = Panel("Stage Goals",playing.transform,goals,new Vector2(.72f,.70f),new Vector2(.985f,.97f));
        var title = Label("Stage Title",goalPanel.transform,font,"Stage",new Vector2(0,1),new Vector2(0,1),new Vector2(18,-12),new Vector2(420,40),28,Color.white);
        Label("Required Heading",goalPanel.transform,font,"필수 목표",new Vector2(0,1),new Vector2(0,1),new Vector2(18,-55),new Vector2(200,28),18,Color.white);
        Label("Additional Heading",goalPanel.transform,font,"추가 목표",new Vector2(0,1),new Vector2(0,1),new Vector2(18,-126),new Vector2(200,28),18,Color.white);
        var rows = new StageGoalRowUI[3];
        for (int i = 0; i < rows.Length; i++)
        {
            var row = Rect("Goal "+i,goalPanel.transform,new Vector2(460,42));
            Place(row,new Vector2(0,1),new Vector2(0,1),new Vector2(18,-(i == 0 ? 85 : 155 + (i-1)*46)),new Vector2(460,42));
            var background = row.AddComponent<Image>(); background.sprite = pending; background.type = Image.Type.Sliced; background.raycastTarget = false;
            var badge = Rect("Rank Star",row.transform,new Vector2(28,28)); Place(badge,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(8,0),new Vector2(28,28));
            var badgeImage = badge.AddComponent<Image>(); badgeImage.sprite = star; badgeImage.preserveAspect = true; badgeImage.raycastTarget = false;
            var text = Label("Description",row.transform,font,"",new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(42,0),new Vector2(410,36),18,Color.black);
            text.richText = false;
            rows[i] = row.AddComponent<StageGoalRowUI>(); Ref(rows[i],"description",text); Ref(rows[i],"background",background);
            Ref(rows[i],"pendingSprite",pending); Ref(rows[i],"achievedSprite",achieved);
        }
        var handPanel = Panel("Building Hand",playing.transform,city,new Vector2(.16f,.025f),new Vector2(.84f,.175f));
        Label("Hand Title",playing.transform,font,"건물 목록",new Vector2(.16f,.175f),new Vector2(0,0),new Vector2(0,4),new Vector2(220,34),22,Color.white);
        var viewport = Rect("Viewport",handPanel.transform,Vector2.zero); Stretch(viewport);
        var viewportRect = (RectTransform)viewport.transform; viewportRect.offsetMin = new Vector2(12,10); viewportRect.offsetMax = new Vector2(-12,-10);
        viewport.AddComponent<RectMask2D>();
        var content = Rect("Cards",viewport.transform,Vector2.zero); var contentRect = (RectTransform)content.transform;
        contentRect.anchorMin = Vector2.zero; contentRect.anchorMax = new Vector2(0,1); contentRect.pivot = new Vector2(0,.5f);
        var layout = content.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 12;
        layout.padding = new RectOffset(6,6,0,0); layout.childControlWidth = false; layout.childControlHeight = false;
        layout.childForceExpandWidth = false; layout.childForceExpandHeight = false; layout.childAlignment = TextAnchor.MiddleLeft;
        content.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = handPanel.AddComponent<ScrollRect>(); scroll.viewport = viewportRect; scroll.content = contentRect;
        scroll.horizontal = true; scroll.vertical = false; scroll.movementType = ScrollRect.MovementType.Clamped;
        var hand = handPanel.AddComponent<BuildingHandUI>();
        var undoButton = Button("Undo",playing.transform,undo,font,"되돌리기",new Vector2(.075f,.10f),new Vector2(120,120));
        var undoView = undoButton.gameObject.AddComponent<UndoButtonUI>();
        var nextButton = Button("Next Stage",playing.transform,next,font,"NEXT\nSTAGE",new Vector2(.925f,.10f),new Vector2(120,120));
        var nextView = nextButton.gameObject.AddComponent<NextStageButtonUI>();
        var pause = Button("Pause",playing.transform,pending,font,"일시정지",new Vector2(.5f,.945f),new Vector2(150,46));
        var feedback = Label("Placement Feedback",playing.transform,font,"",new Vector2(.5f,.22f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(600,40),22,Color.white);
        feedback.alignment = TextAlignmentOptions.Center;
        var tooltipRoot = Rect("Building Tooltip",playing.transform,new Vector2(430,160));
        var tooltipImage = tooltipRoot.AddComponent<Image>(); tooltipImage.sprite = details; tooltipImage.type = Image.Type.Sliced; tooltipImage.raycastTarget = false;
        var group = tooltipRoot.AddComponent<CanvasGroup>(); group.interactable = false; group.blocksRaycasts = false; group.alpha = 0;
        var tooltipName = Label("Building Name",tooltipRoot.transform,font,"",new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-8),new Vector2(400,30),20,Color.black);
        var tiles = Label("Allowed Tiles",tooltipRoot.transform,font,"",new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-40),new Vector2(400,34),17,Color.black);
        var gainRoot = Rect("Gained Resources",tooltipRoot.transform,new Vector2(390,36));
        Place(gainRoot,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,-10),new Vector2(390,36));
        var costRoot = Rect("Spent Resources",tooltipRoot.transform,new Vector2(390,36));
        Place(costRoot,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,-48),new Vector2(390,36));
        var tooltip = tooltipRoot.AddComponent<BuildingTooltipUI>();
        Ref(tooltip,"buildingName",tooltipName); Ref(tooltip,"allowedTiles",tiles); Ref(tooltip,"canvasGroup",group);
        Ref(tooltip,"gained",Strip(gainRoot,icons,font,24)); Ref(tooltip,"spent",Strip(costRoot,icons,font,24));
        var auto = CreateCombo(common.ComboPopupPrefab,playing.transform,icons,font,"Automatic Combo");
        var replay = CreateCombo(common.ComboPopupPrefab,playing.transform,icons,font,"Combo Replay");
        var boardDetails = playing.AddComponent<BoardDetailsUI>();
        Ref(view,"gameplayRoot",playing); Ref(view,"cityResources",cityStrip); Ref(view,"stageTitle",title);
        var serialized = new SerializedObject(view); var property = serialized.FindProperty("goals"); property.arraySize = 3;
        for(int i=0;i<3;i++) property.GetArrayElementAtIndex(i).objectReferenceValue = rows[i]; serialized.ApplyModifiedPropertiesWithoutUndo();
        Ref(view,"hand",hand); Ref(view,"cardContent",content.transform); Ref(view,"commonPrefabs",common);
        Ref(view,"undoView",undoView); Ref(view,"undoButton",undoButton); Ref(view,"nextView",nextView); Ref(view,"nextButton",nextButton);
        Ref(view,"pauseButton",pause); Ref(view,"placementFeedback",feedback); Ref(view,"tooltip",tooltip);
        Ref(view,"automaticCombo",auto); Ref(view,"replayCombo",replay); Ref(view,"boardDetails",boardDetails);
        playing.SetActive(false);
        return root;
    }
    private static ComboPopupUI CreateCombo(ComboPopupUI source,Transform parent,ResourceIconSet icons,TMP_FontAsset font,string name)
    {
        var popup = UnityEngine.Object.Instantiate(source,parent); popup.name = name; popup.TryConfigureTiming(1.5f,.5f);
        if (PrefabUtility.IsPartOfPrefabInstance(popup.gameObject))
            PrefabUtility.UnpackPrefabInstance(popup.gameObject,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
        var root = Rect("Reward Icons",popup.transform,new Vector2(190,66));
        ((RectTransform)root.transform).anchoredPosition = new Vector2(0,-77);
        Ref(popup,"rewardsView",Strip(root,icons,font,24));
        var text = popup.transform.Find("Rewards"); if (text != null) text.gameObject.SetActive(false);
        return popup;
    }
    public static ResourceStripUI Strip(GameObject root,ResourceIconSet icons,TMP_FontAsset font,float size)
    {
        var layout = root.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 2; layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
        var strip = root.AddComponent<ResourceStripUI>(); Ref(strip,"icons",icons); Ref(strip,"font",font);
        var serialized = new SerializedObject(strip); serialized.FindProperty("fontSize").floatValue = size; serialized.ApplyModifiedPropertiesWithoutUndo();
        return strip;
    }
    private static GameObject Panel(string name,Transform parent,Sprite sprite,Vector2 min,Vector2 max)
    {
        var root=Rect(name,parent,Vector2.zero); var rect=(RectTransform)root.transform;
        rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=Vector2.zero; rect.offsetMax=Vector2.zero;
        var image=root.AddComponent<Image>(); image.sprite=sprite; image.type=Image.Type.Sliced;
        return root;
    }
    private static Button Button(string name,Transform parent,Sprite sprite,TMP_FontAsset font,string label,Vector2 anchor,Vector2 size)
    {
        var root=Rect(name,parent,size); Place(root,anchor,new Vector2(.5f,.5f),Vector2.zero,size);
        var image=root.AddComponent<Image>(); image.sprite=sprite; image.type=Image.Type.Sliced;
        var button=root.AddComponent<Button>(); button.targetGraphic=image;
        var text=Label("Label",root.transform,font,label,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,size-new Vector2(12,12),22,Color.black);
        text.alignment=TextAlignmentOptions.Center; return button;
    }
    private static TMP_Text Label(string name,Transform parent,TMP_FontAsset font,string value,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size,int fontSize,Color color)
    {
        var root=Rect(name,parent,size); Place(root,anchor,pivot,position,size);
        var text=root.AddComponent<TextMeshProUGUI>(); text.font=font; text.text=value; text.fontSize=fontSize;
        text.enableAutoSizing=true; text.fontSizeMin=12; text.fontSizeMax=fontSize; text.alignment=TextAlignmentOptions.MidlineLeft;
        text.color=color; text.raycastTarget=false; text.overflowMode=TextOverflowModes.Ellipsis; return text;
    }
    private static GameObject Rect(string name,Transform parent,Vector2 size)
    { var root=new GameObject(name,typeof(RectTransform)); root.transform.SetParent(parent,false); ((RectTransform)root.transform).sizeDelta=size; return root; }
    private static void Place(GameObject root,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
    { var rect=(RectTransform)root.transform; rect.anchorMin=rect.anchorMax=anchor; rect.pivot=pivot; rect.anchoredPosition=position; rect.sizeDelta=size; }
    private static void Stretch(GameObject root)
    { var rect=(RectTransform)root.transform; rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; }
    private static void Ref(UnityEngine.Object target,string name,UnityEngine.Object value) => UrbanEquationPrefabFactory.Reference(target,name,value);
}
