using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UrbanEquationScreensFactory
{
    // background, overlay, popup, list, row, empty, star, large blue/yellow/pink,
    // medium blue/yellow, square blue/pink/disabled, play, lock, close, goal blue/pink
    public const int ArtCount = 20;
    public static GameObject CreateScreens(TMP_FontAsset font, Sprite[] art)
    {
        if(font==null || art==null || art.Length!=ArtCount || Array.Exists(art,x=>x==null))
            throw new ArgumentException("Screen construction requires a font and all twenty GUI sprites.");
        var root=Rect("Pfb_Flow_Screens_001",null,new Vector2(1920,1080));
        var canvas=root.AddComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=100;
        var scaler=root.AddComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f; root.AddComponent<GraphicRaycaster>();
        var view=root.AddComponent<GameScreensUI>();
        var background=Rect("Lobby Background",root.transform,Vector2.zero); Stretch(background); Image(background,art[0],true,false);
        Ref(view,"lobbyBackground",background);
        var lobby=Rect("Lobby",root.transform,Vector2.zero); Stretch(lobby); Ref(view,"lobby",lobby);
        Text("Title",lobby.transform,font,"URBAN EQUATION",new Vector2(.73f,.67f),new Vector2(650,95),60);
        Ref(view,"playButton",Button("Play",lobby.transform,font,"Play",art[7],new Vector2(.73f,.51f),new Vector2(510,84)));
        Ref(view,"continueButton",Button("Continue",lobby.transform,font,"Continue",art[8],new Vector2(.73f,.405f),new Vector2(510,84)));
        Ref(view,"exitButton",Button("Exit",lobby.transform,font,"Exit",art[9],new Vector2(.73f,.30f),new Vector2(510,84)));
        var blocker=Rect("Modal Blocker",root.transform,Vector2.zero); Stretch(blocker);
        Image(blocker,art[1],true,false); blocker.GetComponent<Image>().color=new Color(1,1,1,.8f); Ref(view,"modalBlocker",blocker);
        var newGame=Panel("New Game Confirmation",root.transform,art[2],new Vector2(620,380)); Ref(view,"newGame",newGame);
        Badge(newGame.transform,art[6]); Text("Question",newGame.transform,font,"지난 게임을 이어 할까요?",new Vector2(.5f,.46f),new Vector2(550,60),29);
        Ref(view,"newButton",Button("New",newGame.transform,font,"새로 시작하기",art[10],new Vector2(.27f,.20f),new Vector2(250,60)));
        Ref(view,"resumeButton",Button("Resume",newGame.transform,font,"이어하기",art[11],new Vector2(.73f,.20f),new Vector2(250,60)));
        Ref(view,"newCancelButton",Button("Close",newGame.transform,font,"",art[17],new Vector2(.97f,.96f),new Vector2(48,48)));
        var exit=Panel("Exit Confirmation",root.transform,art[2],new Vector2(600,380)); Ref(view,"exit",exit);
        Badge(exit.transform,art[6]); Text("Question",exit.transform,font,"게임을 종료할까요?",new Vector2(.5f,.46f),new Vector2(540,60),29);
        Ref(view,"exitYesButton",Button("Yes",exit.transform,font,"예",art[10],new Vector2(.28f,.20f),new Vector2(230,60)));
        Ref(view,"exitNoButton",Button("No",exit.transform,font,"아니오",art[11],new Vector2(.72f,.20f),new Vector2(230,60)));
        var select=Panel("Stage Select",root.transform,art[3],new Vector2(880,800)); Ref(view,"stageSelect",select);
        Text("Title",select.transform,font,"Stage List",new Vector2(.30f,.94f),new Vector2(460,55),32);
        Ref(view,"selectBackButton",Button("Back",select.transform,font,"←",art[11],new Vector2(.88f,.94f),new Vector2(100,50)));
        var viewport=Rect("Viewport",select.transform,new Vector2(800,620)); Place(viewport,new Vector2(.5f,.46f),new Vector2(800,620)); viewport.AddComponent<RectMask2D>();
        var content=Rect("Rows",viewport.transform,Vector2.zero); var rect=(RectTransform)content.transform;
        rect.anchorMin=new Vector2(0,1); rect.anchorMax=Vector2.one; rect.pivot=new Vector2(.5f,1); rect.sizeDelta=Vector2.zero;
        var layout=content.AddComponent<VerticalLayoutGroup>(); layout.spacing=14; layout.childControlWidth=true; layout.childControlHeight=true;
        layout.childForceExpandHeight=false; layout.childForceExpandWidth=true;
        content.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var scroll=select.AddComponent<ScrollRect>(); scroll.content=rect; scroll.viewport=(RectTransform)viewport.transform;
        scroll.horizontal=false; scroll.vertical=true; scroll.movementType=ScrollRect.MovementType.Clamped;
        var row=CreateStageRow(content.transform,font,art); row.gameObject.SetActive(false);
        Ref(view,"stageRowPrefab",row); Ref(view,"stageRowContent",content.transform);
        var intro=Panel("Stage Intro",root.transform,art[2],new Vector2(720,650)); Ref(view,"intro",intro);
        Ref(view,"introTitle",Text("Stage Title",intro.transform,font,"Stage",new Vector2(.5f,.91f),new Vector2(650,60),32));
        var introText=Text("Introduction and Goals",intro.transform,font,"",new Vector2(.5f,.49f),new Vector2(620,440),27);
        introText.alignment=TextAlignmentOptions.TopLeft; introText.overflowMode=TextOverflowModes.Truncate; Ref(view,"introDescription",introText);
        Ref(view,"introOkButton",Button("OK",intro.transform,font,"OK",art[10],new Vector2(.5f,.065f),new Vector2(260,56)));
        var pause=Panel("Pause Confirmation",root.transform,art[2],new Vector2(620,400)); Ref(view,"pause",pause);
        Text("Title",pause.transform,font,"일시 정지",new Vector2(.5f,.85f),new Vector2(540,55),32);
        Text("Question",pause.transform,font,"타이틀 화면으로\n이동하시겠습니까?",new Vector2(.5f,.48f),new Vector2(540,100),29);
        Ref(view,"pauseYesButton",Button("Yes",pause.transform,font,"예",art[10],new Vector2(.28f,.17f),new Vector2(230,60)));
        Ref(view,"pauseNoButton",Button("No",pause.transform,font,"아니오",art[11],new Vector2(.72f,.17f),new Vector2(230,60)));
        var clear=Panel("Stage Clear",root.transform,art[3],new Vector2(800,820)); Ref(view,"clear",clear);
        Ref(view,"clearTitle",Text("Title",clear.transform,font,"Stage Clear!",new Vector2(.5f,.92f),new Vector2(700,60),42));
        var stars=Stars(clear.transform,art[5],art[6],new Vector2(.5f,.79f),80,16); Refs(view,"clearStars",stars);
        Text("Required Heading",clear.transform,font,"필수 목표",new Vector2(.5f,.65f),new Vector2(650,40),25);
        Text("Additional Heading",clear.transform,font,"추가 목표",new Vector2(.5f,.51f),new Vector2(650,40),25);
        var goals=new StageGoalRowUI[3];
        for(int i=0;i<3;i++)
        {
            var goal=Rect("Goal "+i,clear.transform,new Vector2(650,55)); Place(goal,new Vector2(.5f,i==0?.59f:i==1?.45f:.37f),new Vector2(650,55));
            var image=Image(goal,art[19],false,true); var star=Rect("Star",goal.transform,new Vector2(38,38)); Place(star,new Vector2(.04f,.5f),new Vector2(38,38)); Image(star,art[6],false,false);
            var label=Text("Description",goal.transform,font,"",new Vector2(.55f,.5f),new Vector2(560,45),24); label.color=Color.black;
            goals[i]=goal.AddComponent<StageGoalRowUI>(); Ref(goals[i],"description",label); Ref(goals[i],"background",image);
            Ref(goals[i],"achievedSprite",art[18]); Ref(goals[i],"pendingSprite",art[19]);
        }
        Refs(view,"clearGoals",goals);
        Ref(view,"retryButton",Button("Retry",clear.transform,font,"Retry",art[13],new Vector2(.33f,.19f),new Vector2(140,140)));
        Ref(view,"nextButton",Button("Next Stage",clear.transform,font,"NEXT\nSTAGE",art[12],new Vector2(.67f,.19f),new Vector2(140,140)));
        Ref(view,"clearSelectButton",Button("Stage Select",clear.transform,font,"Stage Select",art[11],new Vector2(.5f,.055f),new Vector2(320,48)));
        Ref(view,"saveErrorText",Text("Save Error",clear.transform,font,"",new Vector2(.5f,.315f),new Vector2(720,32),18));
        Ref(view,"saveRetryButton",Button("Save Retry",clear.transform,font,"저장 재시도",art[10],new Vector2(.85f,.055f),new Vector2(170,48)));
        var failed=Panel("Load Failure",root.transform,art[2],new Vector2(620,340)); Ref(view,"loadFailure",failed);
        Text("Message",failed.transform,font,"화면 또는 스테이지를\n불러오지 못했습니다.",new Vector2(.5f,.62f),new Vector2(550,120),28);
        Ref(view,"loadSelectButton",Button("Stage Select",failed.transform,font,"Stage Select",art[11],new Vector2(.5f,.20f),new Vector2(340,60)));
        Ref(view,"loadRetryButton",Button("Retry Load",failed.transform,font,"다시 시도",art[10],new Vector2(.5f,.20f),new Vector2(340,60)));
        var transition=Rect("Loading",root.transform,Vector2.zero); Stretch(transition); var shade=Image(transition,null,true,false); shade.color=new Color(.08f,.06f,.15f,.95f);
        Text("Message",transition.transform,font,"불러오는 중…",new Vector2(.5f,.5f),new Vector2(800,100),36); Ref(view,"transition",transition);
        Ref(view,"errorText",Text("Error",root.transform,font,"",new Vector2(.5f,.015f),new Vector2(1400,32),20));
        view.Refresh(); return root;
    }
    private static StageSelectRowUI CreateStageRow(Transform parent,TMP_FontAsset font,Sprite[] art)
    {
        var root=Rect("Stage Row Template",parent,new Vector2(800,125)); Image(root,art[4],true,true);
        root.AddComponent<LayoutElement>().preferredHeight=125;
        var row=root.AddComponent<StageSelectRowUI>(); Ref(row,"title",Text("Title",root.transform,font,"Stage",new Vector2(.15f,.5f),new Vector2(210,55),29));
        Refs(row,"stars",Stars(root.transform,art[5],art[6],new Vector2(.52f,.5f),75,10));
        var button=Button("Select",root.transform,font,"",art[12],new Vector2(.88f,.5f),new Vector2(90,90)); Ref(row,"selectButton",button);
        var icon=Rect("Icon",button.transform,new Vector2(48,48)); Ref(row,"selectIcon",Image(icon,art[15],false,false));
        Ref(row,"playIcon",art[15]); Ref(row,"lockIcon",art[16]); Ref(row,"unlockedSprite",art[12]); Ref(row,"lockedSprite",art[14]); return row;
    }
    private static Image[] Stars(Transform parent,Sprite empty,Sprite star,Vector2 anchor,float size,float spacing)
    {
        var images=new Image[3];
        for(int i=0;i<3;i++)
        {
            var slot=Rect("Rank Slot "+i,parent,new Vector2(size,size)); Place(slot,anchor,new Vector2(size,size)); ((RectTransform)slot.transform).anchoredPosition=new Vector2((i-1)*(size+spacing),0);
            Image(slot,empty,false,false); var badge=Rect("Star",slot.transform,new Vector2(size*.86f,size*.86f)); images[i]=Image(badge,star,false,false); images[i].enabled=false;
        }
        return images;
    }
    private static void Badge(Transform parent,Sprite sprite)
    { var root=Rect("Star",parent,new Vector2(90,90)); Place(root,new Vector2(.5f,.78f),new Vector2(90,90)); Image(root,sprite,false,false); }
    private static GameObject Panel(string name,Transform parent,Sprite sprite,Vector2 size)
    { var root=Rect(name,parent,size); Image(root,sprite,true,true); return root; }
    private static Button Button(string name,Transform parent,TMP_FontAsset font,string label,Sprite sprite,Vector2 anchor,Vector2 size)
    {
        var root=Rect(name,parent,size); Place(root,anchor,size); var image=Image(root,sprite,true,true);
        var button=root.AddComponent<Button>(); button.targetGraphic=image;
        var text=Text("Label",root.transform,font,label,new Vector2(.5f,.5f),size-new Vector2(14,8),30); text.color=Color.black; return button;
    }
    private static TMP_Text Text(string name,Transform parent,TMP_FontAsset font,string value,Vector2 anchor,Vector2 size,int fontSize)
    {
        var root=Rect(name,parent,size); Place(root,anchor,size); var text=root.AddComponent<TextMeshProUGUI>();
        text.font=font; text.text=value; text.richText=false; text.fontSize=fontSize; text.enableAutoSizing=true; text.fontSizeMin=14; text.fontSizeMax=fontSize;
        text.alignment=TextAlignmentOptions.Center; text.color=Color.white; text.raycastTarget=false; text.overflowMode=TextOverflowModes.Ellipsis; return text;
    }
    private static Image Image(GameObject root,Sprite sprite,bool raycast,bool sliced)
    { var image=root.AddComponent<Image>(); image.sprite=sprite; image.raycastTarget=raycast; image.type=sliced?UnityEngine.UI.Image.Type.Sliced:UnityEngine.UI.Image.Type.Simple; image.preserveAspect=!sliced && !raycast; return image; }
    private static GameObject Rect(string name,Transform parent,Vector2 size)
    { var root=new GameObject(name,typeof(RectTransform)); root.transform.SetParent(parent,false); ((RectTransform)root.transform).sizeDelta=size; return root; }
    private static void Place(GameObject root,Vector2 anchor,Vector2 size)
    { var r=(RectTransform)root.transform; r.anchorMin=r.anchorMax=anchor; r.pivot=new Vector2(.5f,.5f); r.anchoredPosition=Vector2.zero; r.sizeDelta=size; }
    private static void Stretch(GameObject root)
    { var r=(RectTransform)root.transform; r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=r.offsetMax=Vector2.zero; }
    private static void Ref(UnityEngine.Object target,string name,UnityEngine.Object value) => UrbanEquationPrefabFactory.Reference(target,name,value);
    private static void Refs(UnityEngine.Object target,string name,UnityEngine.Object[] values)
    { var s=new SerializedObject(target); var p=s.FindProperty(name); p.arraySize=values.Length; for(int i=0;i<values.Length;i++) p.GetArrayElementAtIndex(i).objectReferenceValue=values[i]; s.ApplyModifiedPropertiesWithoutUndo(); }
}
