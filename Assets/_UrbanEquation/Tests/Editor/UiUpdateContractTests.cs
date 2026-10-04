using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UObject = UnityEngine.Object;

public class UiUpdateContractTests
{
    private readonly List<UObject> owned = new List<UObject>();
    private string directory;
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Get(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
    private static object Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name).Invoke(target, args);
    private static object[] Items(object value) => ((IEnumerable)value).Cast<object>().ToArray();
    private T Own<T>(T value) where T : UObject { owned.Add(value); return value; }
    private static UObject Asset(string path, string type) => AssetDatabase.LoadAssetAtPath(path, Runtime(type));
    private static UObject Settings => Asset("Assets/_UrbanEquation/Data/Presentation/GameUiUpdate.asset", "GameUiUpdateSettings");
    private static object Session(Component game) => Get(game,"Session");
    private static object Flow(Component game) => Get(game,"Flow");
    private static void Command(object target, string name)
    { object[] args={null}; Assert.That(Call(target,name,args),Is.True,args[0] as string); }
    private Component Game(int stage = 0)
    {
        string path=Path.Combine(directory,Guid.NewGuid().ToString("N")+".json");
        File.WriteAllText(path,"{\"version\":1,\"stageCount\":5,\"highestUnlockedStage\":5,\"bestRanks\":[3,3,3,3,0]}");
        var game=Own(new GameObject("UI update session")).AddComponent(Runtime("GameBootstrap"));
        object[] args={Asset("Assets/_UrbanEquation/Data/GameContent.asset","GameContentData"),path,false,null};
        Assert.That(Call(game,"TryInitialize",args),Is.True,args[3] as string);
        if(stage>0)
        { Command(Flow(game),"TryContinue"); args=new object[]{stage,null}; Assert.That(Call(Flow(game),"TrySelectStage",args),Is.True); Command(Flow(game),"TryDismissIntro"); }
        return game;
    }
    private Component Hud(Component game)
    {
        var data=Asset("Assets/_UrbanEquation/Data/Presentation/GameplayHud.asset","GameplayHudSet");
        var prefab=(Component)Get(data,"HudPrefab"); var hud=Own(UObject.Instantiate(prefab.gameObject)).GetComponent(Runtime("GameHudUI"));
        Call(hud,"Bind",Session(game),Flow(game),null,null); return hud;
    }
    private Component Screens(Component game)
    {
        var data=Asset("Assets/_UrbanEquation/Data/Presentation/GameApplication.asset","GameApplicationSettings");
        var prefab=(Component)Get(data,"ScreensPrefab"); var view=Own(UObject.Instantiate(prefab.gameObject)).GetComponent(Runtime("GameScreensUI"));
        Call(view,"Bind",Flow(game),null); return view;
    }
    private static void Build(Component game,int card,int x,int y)
    { object[] args={card,new Vector2Int(x,y),null,null};Assert.That(Call(Session(game),"TryCommitBuild",args),Is.True,args[3] as string); }
    private static object FirstResult(Component game) => Items(Get(Get(Session(game),"Combos"),"Results"))[0];
    private Component Hand(float width=1000)
    {
        var root=Own(new GameObject("Scroll fixture",typeof(RectTransform),typeof(ScrollRect)));
        ((RectTransform)root.transform).sizeDelta=new Vector2(300,100);
        var viewport=new GameObject("Viewport",typeof(RectTransform));viewport.transform.SetParent(root.transform,false);
        ((RectTransform)viewport.transform).sizeDelta=new Vector2(300,100);
        var content=new GameObject("Cards",typeof(RectTransform),typeof(HorizontalLayoutGroup));content.transform.SetParent(viewport.transform,false);
        ((RectTransform)content.transform).sizeDelta=new Vector2(width,100);
        var layout=content.GetComponent<HorizontalLayoutGroup>();layout.spacing=10;layout.childControlWidth=false;layout.childControlHeight=false;
        layout.childForceExpandWidth=false;layout.childForceExpandHeight=false;
        for(int i=0;i<10;i++)
        {var card=new GameObject("Card",typeof(RectTransform));card.transform.SetParent(content.transform,false);((RectTransform)card.transform).sizeDelta=new Vector2(100,80);}
        var scroll=root.GetComponent<ScrollRect>();scroll.content=(RectTransform)content.transform;scroll.viewport=(RectTransform)viewport.transform;
        var view=root.AddComponent(Runtime("BuildingHandScrollUI"));
        Call(view,"Configure",scroll,Get(Settings,"LeftScrollButton"),Get(Settings,"RightScrollButton"),null);return view;
    }
    private static Button Left(Component hand)=>(Button)Get(hand,"LeftButton");
    private static Button Right(Component hand)=>(Button)Get(hand,"RightButton");
    private static float Offset(Component hand)=>(float)Get(hand,"Offset");
    [SetUp] public void SetUp(){directory=Path.Combine(Path.GetTempPath(),"UE-UiUpdate-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);}
    [TearDown] public void TearDown()
    {for(int i=owned.Count-1;i>=0;i--)if(owned[i]!=null)UObject.DestroyImmediate(owned[i]);owned.Clear();if(Directory.Exists(directory))Directory.Delete(directory,true);}

    [TestCase("LobbyBGI")] [TestCase("ComplaintBGI")] [TestCase("Complaint")]
    [TestCase("LScrollButton")] [TestCase("RScrollButton")]
    public void NewImagesImportAsSingleUiSpritesWithoutMipmaps(string name)
    {
        string path="Assets/_UrbanEquation/Sprite/UI/Spr_Ui_"+name+"_001.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        Assert.That(importer.textureType,Is.EqualTo(TextureImporterType.Sprite));Assert.That(importer.spriteImportMode,Is.EqualTo(SpriteImportMode.Single));
        Assert.That(importer.mipmapEnabled,Is.False);Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(path),Is.Not.Null);
    }
    [Test] public void SharedSettingsContainAllSevenSpriteReferences()
    {Assert.That(Settings,Is.Not.Null);var errors=new List<string>();Call(Settings,"Validate",errors);Assert.That(errors,Is.Empty);}
    [Test] public void BothAuthoredPrefabComponentsReferenceSharedSettings()
    {
        foreach(var item in new[]{new[]{"Assets/_UrbanEquation/Prefabs/UI/Gameplay/Pfb_Main_GameHud_001.prefab","GameHudUI"},new[]{"Assets/_UrbanEquation/Prefabs/UI/Popup/Pfb_Flow_Screens_001.prefab","GameScreensUI"}})
            Assert.That(Field(AssetDatabase.LoadAssetAtPath<GameObject>(item[0]).GetComponent(Runtime(item[1])),"uiUpdate"),Is.SameAs(Settings));
    }
    [Test] public void LobbyUsesSuppliedCityBackground()
    {var view=Screens(Game());Assert.That(((GameObject)Field(view,"lobbyBackground")).GetComponent<Image>().sprite,Is.SameAs(Get(Settings,"LobbyBackground")));}
    [Test] public void StageSelectionRetainsItsOriginalSpaceBackground()
    {
        var game=Game();var view=Screens(game);Sprite original=(Sprite)Field(view,"stageSelectBackground");
        Assert.That(original,Is.Not.Null);Command(Flow(game),"TryContinue");
        Assert.That(((GameObject)Field(view,"lobbyBackground")).GetComponent<Image>().sprite,Is.SameAs(original));
        Command(Flow(game),"TryCancel");Assert.That(((GameObject)Field(view,"lobbyBackground")).GetComponent<Image>().sprite,Is.SameAs(Get(Settings,"LobbyBackground")));
    }
    [Test] public void StageScrollbarIsVerticalConnectedAndNotDuplicatedOnRebind()
    {
        var game=Game();var view=Screens(game);var panel=(GameObject)Field(view,"stageSelect");var scroll=panel.GetComponent<ScrollRect>();
        Assert.That(scroll.verticalScrollbar,Is.Not.Null);Assert.That(scroll.verticalScrollbar.direction,Is.EqualTo(Scrollbar.Direction.BottomToTop));
        Assert.That(scroll.horizontal,Is.False);Assert.That(scroll.vertical,Is.True);Assert.That(scroll.scrollSensitivity,Is.GreaterThan(0));
        Call(view,"Bind",Flow(game),null);Assert.That(panel.GetComponentsInChildren<Scrollbar>(true).Length,Is.EqualTo(1));
    }
    [Test] public void StageScrollbarCanReachTheFifthStageWithoutChangingRowOrder()
    {
        var game=Game();var view=Screens(game);Command(Flow(game),"TryContinue");
        var scroll=((GameObject)Field(view,"stageSelect")).GetComponent<ScrollRect>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=0;
        Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(0).Within(.01f));
        Assert.That(Items(Field(view,"rows")).Select(r=>Get(r,"StageNumber")),Is.EqualTo(new[]{1,2,3,4,5}));
    }
    [Test] public void ComplaintUsesItsOwnBackgroundHeaderAndRedLossIcon()
    {
        var game=Game(5);var hud=Hud(game);Build(game,1,1,1);Build(game,2,1,2);
        var popup=(Component)Field(hud,"automaticCombo");Assert.That(Get(FirstResult(game),"IsComplaint"),Is.True);
        Assert.That(popup.GetComponent<Image>().sprite,Is.SameAs(Get(Settings,"ComplaintBackground")));
        Assert.That(popup.transform.Find("ComboHeader").GetComponent<Image>().sprite,Is.SameAs(Get(Settings,"ComplaintHeader")));
        var strip=(Component)Field(popup,"rewardsView");
        var icons=Asset("Assets/_UrbanEquation/Data/Presentation/ResourceIcons.asset","ResourceIconSet");
        Assert.That(strip.transform.GetChild(0).GetComponentInChildren<Image>().sprite,Is.SameAs(((Sprite[])Field(icons,"spent"))[0]));
    }
    [Test] public void PositiveComboRestoresNormalSpritesAfterComplaintReplay()
    {
        var complaint=Game(5);Build(complaint,1,1,1);Build(complaint,2,1,2);var hud=Hud(complaint);
        var popup=(Component)Field(hud,"replayCombo");Sprite normal=popup.GetComponent<Image>().sprite;
        Call(popup,"ShowPersistent",FirstResult(complaint),null);Assert.That(popup.GetComponent<Image>().sprite,Is.SameAs(Get(Settings,"ComplaintBackground")));
        var positive=Game(1);Build(positive,1,0,0);Build(positive,2,1,0);
        Call(popup,"ShowPersistent",FirstResult(positive),null);Assert.That(popup.GetComponent<Image>().sprite,Is.SameAs(normal));
        Assert.That(popup.transform.Find("ComboHeader").GetComponent<Image>().sprite,Is.SameAs(Field(popup,"comboHeader")));
    }
    [Test] public void ComplaintStyleDoesNotChangeAuthoredComboPrefab()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_UrbanEquation/Prefabs/UI/Gameplay/ComboPopup.prefab");Sprite before=prefab.GetComponent<Image>().sprite;
        var game=Game(5);var hud=Hud(game);Build(game,1,1,1);Build(game,2,1,2);
        Call(Field(hud,"replayCombo"),"ShowPersistent",FirstResult(game),null);Assert.That(prefab.GetComponent<Image>().sprite,Is.SameAs(before));
    }
    [Test] public void HandButtonsHideAtTheirRespectiveBounds()
    {
        var hand=Hand();Assert.That(Left(hand).gameObject.activeSelf,Is.False);Assert.That(Right(hand).gameObject.activeSelf,Is.True);
        for(int i=0;i<20;i++)Call(hand,"Step",1);
        Assert.That(Offset(hand),Is.EqualTo(Get(hand,"MaximumOffset")));Assert.That(Left(hand).gameObject.activeSelf,Is.True);Assert.That(Right(hand).gameObject.activeSelf,Is.False);
    }
    [Test] public void HandWithoutOverflowHidesBothButtons()
    {var hand=Hand(250);Assert.That(Left(hand).gameObject.activeSelf,Is.False);Assert.That(Right(hand).gameObject.activeSelf,Is.False);Assert.That(Offset(hand),Is.Zero);}
    [Test] public void HandMovesExactlyOneCardAndSpacingPerClick()
    {var hand=Hand();Right(hand).onClick.Invoke();Assert.That(Offset(hand),Is.EqualTo(110).Within(.001f));Left(hand).onClick.Invoke();Assert.That(Offset(hand),Is.Zero);}
    [Test] public void HoldingHandButtonRepeatsEveryHalfSecond()
    {
        var hand=Hand();Call(hand,"BeginHold",1);Assert.That(Offset(hand),Is.EqualTo(110).Within(.001f));
        Call(hand,"Tick",.49f);Assert.That(Offset(hand),Is.EqualTo(110).Within(.001f));
        Call(hand,"Tick",.02f);Assert.That(Offset(hand),Is.EqualTo(220).Within(.001f));
    }
    [Test] public void ReleasingHandButtonStopsFurtherRepeat()
    {var hand=Hand();Call(hand,"BeginHold",1);Call(hand,"EndHold");Call(hand,"Tick",2f);Assert.That(Offset(hand),Is.EqualTo(110).Within(.001f));}
    [Test] public void PointerReleaseClickDoesNotDuplicateInitialStep()
    {var hand=Hand();Call(hand,"BeginHold",1);Call(hand,"EndHold");Right(hand).onClick.Invoke();Assert.That(Offset(hand),Is.EqualTo(110).Within(.001f));}
    [Test] public void RemovingCardsClampsScrollAndHidesUnneededButtons()
    {
        var hand=Hand();for(int i=0;i<20;i++)Call(hand,"Step",1);
        hand.GetComponent<ScrollRect>().content.sizeDelta=new Vector2(250,100);Call(hand,"Refresh");
        Assert.That(Offset(hand),Is.Zero);Assert.That(Left(hand).gameObject.activeSelf,Is.False);Assert.That(Right(hand).gameObject.activeSelf,Is.False);
    }
    [Test] public void RebindingHandDoesNotDuplicateButtons()
    {
        var hand=Hand();Call(hand,"Configure",hand.GetComponent<ScrollRect>(),Get(Settings,"LeftScrollButton"),Get(Settings,"RightScrollButton"),null);
        Assert.That(hand.GetComponentsInChildren<Button>(true).Length,Is.EqualTo(2));
    }
    [Test] public void InvalidRepeatDeltasDoNotMoveCards()
    {var hand=Hand();Call(hand,"BeginHold",1);foreach(float delta in new[]{-1f,float.NaN,float.PositiveInfinity})Call(hand,"Tick",delta);Assert.That(Offset(hand),Is.EqualTo(110).Within(.001f));}
    [Test] public void DisabledHandStopsHeldScroll()
    {var hand=Hand();Call(hand,"BeginHold",1);hand.gameObject.SetActive(false);Call(hand,"Tick",1f);Assert.That(Offset(hand),Is.EqualTo(110).Within(.001f));}
    [Test] public void HandMouseWheelIsDisabledWhileStageWheelRemainsEnabled()
    {var hand=Hand();Assert.That(hand.GetComponent<ScrollRect>().scrollSensitivity,Is.Zero);var screens=Screens(Game());Assert.That(((GameObject)Field(screens,"stageSelect")).GetComponent<ScrollRect>().scrollSensitivity,Is.GreaterThan(0));}
    [Test] public void PausePreventsHandButtonCommands()
    {
        var game=Game(3);var hud=Hud(game);var hand=(Component)Field(hud,"hand");var buttons=hand.GetComponent(Runtime("BuildingHandScrollUI"));
        Assert.That(buttons,Is.Not.Null);Command(Flow(game),"TryRequestPause");Assert.That(Call(buttons,"Step",1),Is.False);
    }
    [Test] public void ComplaintAutoAndReplayGraphicsNeverBlockBoardInput()
    {
        var game=Game(5);var hud=Hud(game);Build(game,1,1,1);Build(game,2,1,2);Call(Field(hud,"replayCombo"),"ShowPersistent",FirstResult(game),null);
        foreach(var field in new[]{"automaticCombo","replayCombo"})
        {var popup=(Component)Field(hud,field);Assert.That(popup.GetComponent<CanvasGroup>().blocksRaycasts,Is.False);Assert.That(popup.GetComponentsInChildren<Graphic>(true).All(g=>!g.raycastTarget),Is.True);}
    }
}
