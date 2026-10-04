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

public class GameHudContractTests
{
    private readonly List<UObject> objects = new List<UObject>();
    private string directory;
    private static Type Runtime(string name) => Type.GetType(name+", Assembly-CSharp",true);
    private static Type Editor(string name) => Type.GetType(name+", Assembly-CSharp-Editor",true);
    private static object Get(object target,string name) => target.GetType().GetProperty(name).GetValue(target);
    private static object Field(object target,string name) => target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
    private static void Set(object target,string name,object value) => target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    private static object Call(object target,string name,params object[] args) => target.GetType().GetMethod(name).Invoke(target,args);
    private static void Life(object target,string name) => target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
    private static object[] Items(object value) => ((IEnumerable)value).Cast<object>().ToArray();
    private T Own<T>(T value) where T:UObject { objects.Add(value); return value; }
    private GameObject Root(string name) => Own(new GameObject(name,typeof(RectTransform)));
    private Sprite Sprite()
    { var texture=Own(new Texture2D(8,8));return Own(UnityEngine.Sprite.Create(texture,new Rect(0,0,8,8),new Vector2(.5f,.5f))); }
    private static UObject Font()
    {
        Type type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("TMPro.TMP_FontAsset",false)).First(x=>x!=null);
        return AssetDatabase.LoadAssetAtPath("Assets/_UrbanEquation/Fonts/NotoSansKR-Medium SDF.asset",type);
    }
    private UObject Icons()
    {
        var icons=Own(ScriptableObject.CreateInstance(Runtime("ResourceIconSet")));
        foreach(string field in new[]{"neutral","gained","spent"})Set(icons,field,Enumerable.Range(0,5).Select(_=>Sprite()).ToArray());
        return icons;
    }
    private static object Amounts(params int[] pairs)
    {
        var amounts=Array.CreateInstance(Runtime("ResourceAmount"),pairs.Length/2);
        for(int i=0;i<pairs.Length;i+=2)amounts.SetValue(Activator.CreateInstance(Runtime("ResourceAmount"),Enum.ToObject(Runtime("ResourceType"),pairs[i]),pairs[i+1]),i/2);
        return amounts;
    }
    private static object Resource(int index) => Enum.ToObject(Runtime("ResourceType"),index);
    private Component Strip(UObject icons=null)
    {
        var root=Root("Strip");return (Component)Editor("UrbanEquationHudFactory").GetMethod("Strip").Invoke(null,new object[]{root,icons??Icons(),Font(),24f});
    }
    private Component Hud()
    {
        var common=AssetDatabase.LoadAssetAtPath("Assets/_UrbanEquation/Data/Presentation/GameplayPrefabs.asset",Runtime("GameplayPrefabSet"));
        Assert.That(common,Is.Not.Null,"Phase4-A generated prefabs must be committed before Phase4-B tests.");
        object[] args={common,Icons(),Font(),Sprite(),Sprite(),Sprite(),Sprite(),Sprite(),Sprite(),Sprite(),Sprite()};
        var root=Own((GameObject)Editor("UrbanEquationHudFactory").GetMethod("CreateHud").Invoke(null,args));
        return root.GetComponent(Runtime("GameHudUI"));
    }
    private Component Bootstrap(int stage=1)
    {
        var game=Root("HUD test session").AddComponent(Runtime("GameBootstrap"));
        var content=AssetDatabase.LoadAssetAtPath("Assets/_UrbanEquation/Data/GameContent.asset",Runtime("GameContentData"));
        object[] args={content,Path.Combine(directory,Guid.NewGuid().ToString("N")+".json"),false,null};
        Assert.That(Call(game,"TryInitialize",args),Is.True,args[3] as string);
        Command(Get(game,"Flow"),"TryPlay");Command(Get(game,"Flow"),"TryDismissIntro");
        if(stage==2)
        {
            Build(game,1,0,0);Build(game,2,1,0);Command(Get(game,"Flow"),"TryCompleteStage");
            Command(Get(game,"Flow"),"TryNextStage");Command(Get(game,"Flow"),"TryDismissIntro");
        }
        return game;
    }
    private static void Command(object target,string method)
    { object[] args={null};Assert.That(Call(target,method,args),Is.True,args[0] as string); }
    private static object Build(object game,int id,int x,int y)
    {
        object[] args={id,new Vector2Int(x,y),null,null};Assert.That(Call(Get(game,"Session"),"TryCommitBuild",args),Is.True,args[3] as string);return args[2];
    }
    private Component BoundHud(Component game)
    {
        var hud=Hud();var placement=Root("Placement").AddComponent(Runtime("BuildingPlacementController"));
        Call(placement,"Configure",Get(game,"Session"),null);Call(hud,"Bind",Get(game,"Session"),Get(game,"Flow"),placement,null);
        // EditMode does not guarantee play-only child activation callbacks.
        foreach(string field in new[]{"hand","undoView","nextView","automaticCombo","boardDetails"})Life(Field(hud,field),"OnEnable");
        return hud;
    }
    private static string Text(object text) => (string)Get(text,"text");
    private static object[] Rows(Component hud) => Items(Field(hud,"goals"));
    private static object Tooltip(Component hud) => Field(hud,"tooltip");
    private static object Auto(Component hud) => Field(hud,"automaticCombo");
    private static object Replay(Component hud) => Field(hud,"replayCombo");
    private static object Details(Component hud) => Field(hud,"boardDetails");
    private static object Resources(Component game) => Get(Get(game,"Session"),"Resources");
    private static object Combos(Component game) => Get(Get(game,"Session"),"Combos");
    private object Building(int code) => AssetDatabase.LoadAssetAtPath("Assets/_UrbanEquation/Data/Buildings/"+
        (code==13001?"B13001_LargeHouse":code==32001?"B32001_Cafe":"B11001_SmallHouse")+".asset",Runtime("BuildingData"));
    [SetUp] public void SetUp() => directory=Path.Combine(Path.GetTempPath(),"UE-HUD-"+Guid.NewGuid().ToString("N"));
    [TearDown] public void TearDown()
    {
        for(int i=objects.Count-1;i>=0;i--)if(objects[i]!=null)UObject.DestroyImmediate(objects[i]);objects.Clear();
        if(Directory.Exists(directory))Directory.Delete(directory,true);
    }

    [Test] public void ResourceIconsUseNeutralGainAndSpendVariants()
    {
        var icons=Icons();Assert.That(Call(icons,"GetIcon",Resource(2),0),Is.SameAs(((Sprite[])Field(icons,"neutral"))[2]));
        Assert.That(Call(icons,"GetIcon",Resource(2),1),Is.SameAs(((Sprite[])Field(icons,"gained"))[2]));
        Assert.That(Call(icons,"GetIcon",Resource(2),-1),Is.SameAs(((Sprite[])Field(icons,"spent"))[2]));
    }
    [Test] public void ResourceIconLookupRejectsUnknownResource()
    { Assert.That(Call(Icons(),"GetIcon",Resource(-1),0),Is.Null);Assert.That(Call(Icons(),"GetIcon",Resource(5),1),Is.Null); }
    [Test] public void MissingIconVariantsAreReported()
    {
        var icons=Own(ScriptableObject.CreateInstance(Runtime("ResourceIconSet")));var errors=new List<string>();Call(icons,"Validate",errors);
        Assert.That(errors.Count,Is.EqualTo(15));
    }
    [Test] public void ResourceStripShowsSignedValuesWithCorrectIcons()
    {
        var icons=Icons();var strip=Strip(icons);Call(strip,"Show",Amounts(2,3,3,-1),true);
        Assert.That(Text(strip.transform.GetChild(0).GetChild(1).GetComponent(RuntimeText())),Is.EqualTo("+3"));
        Assert.That(Text(strip.transform.GetChild(1).GetChild(1).GetComponent(RuntimeText())),Is.EqualTo("-1"));
        Assert.That(strip.transform.GetChild(0).GetComponentInChildren<Image>().sprite,Is.SameAs(((Sprite[])Field(icons,"gained"))[2]));
        Assert.That(strip.transform.GetChild(1).GetComponentInChildren<Image>().sprite,Is.SameAs(((Sprite[])Field(icons,"spent"))[3]));
    }
    private static Type RuntimeText() => AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("TMPro.TMP_Text",false)).First(x=>x!=null);
    [Test] public void CityResourceStripUsesNeutralIconsWithoutPlusSign()
    {
        var icons=Icons();var strip=Strip(icons);Call(strip,"Show",Amounts(0,2),false);
        Assert.That(Text(strip.transform.GetChild(0).GetChild(1).GetComponent(RuntimeText())),Is.EqualTo("2"));
        Assert.That(strip.GetComponentInChildren<Image>().sprite,Is.SameAs(((Sprite[])Field(icons,"neutral"))[0]));
    }
    [Test] public void ResourceStripReusesRowsAndHidesStaleValues()
    {
        var strip=Strip();Call(strip,"Show",Amounts(0,1,1,2),false);var first=strip.transform.GetChild(0);
        Call(strip,"Show",Amounts(2,3),true);Assert.That(strip.transform.childCount,Is.EqualTo(2));
        Assert.That(strip.transform.GetChild(0),Is.SameAs(first));Assert.That(strip.transform.GetChild(1).gameObject.activeSelf,Is.False);
        Call(strip,"Clear");Assert.That(Get(strip,"VisibleCount"),Is.EqualTo(0));
    }
    [Test] public void GoalAchievementChangesBackgroundAndStrikethrough()
    {
        var hud=Hud();var row=Rows(hud)[0];Call(row,"Show","인구 목표",false);
        Assert.That(((Image)Field(row,"background")).sprite,Is.SameAs(Field(row,"pendingSprite")));
        Call(row,"Show","인구 목표",true);Assert.That(((Image)Field(row,"background")).sprite,Is.SameAs(Field(row,"achievedSprite")));
        Assert.That(Get(Field(row,"description"),"fontStyle").ToString(),Does.Contain("Strikethrough"));
        Call(row,"Show","인구 목표",false);Assert.That(Get(Field(row,"description"),"fontStyle").ToString(),Is.EqualTo("Normal"));
    }
    [Test] public void TooltipDisplaysAllowedTilesAndSignedBuildingResources()
    {
        var tooltip=Tooltip(Hud());Call(tooltip,"Show",Building(13001),this,Vector2.zero);
        Assert.That(Text(Field(tooltip,"allowedTiles")),Does.Contain("Grass"));
        Assert.That(Get(Field(tooltip,"gained"),"VisibleCount"),Is.EqualTo(2));Assert.That(Get(Field(tooltip,"spent"),"VisibleCount"),Is.EqualTo(2));
        var costs=(Component)Field(tooltip,"spent");Assert.That(Text(costs.transform.GetChild(0).GetChild(1).GetComponent(RuntimeText())),Is.EqualTo("-3"));
    }
    [Test] public void TooltipTileListDoesNotInventDisallowedTypes()
    {
        var text=(string)Runtime("BuildingTooltipUI").GetMethod("FormatTiles").Invoke(null,new[]{Building(32001)});
        Assert.That(text,Does.Contain("Concrete"));Assert.That(text,Does.Contain("Grass"));Assert.That(text,Does.Not.Contain("Asphalt"));
    }
    [Test] public void TooltipExitFromPreviousOwnerDoesNotHideCurrentOwner()
    {
        var tooltip=Tooltip(Hud());var next=new object();Call(tooltip,"Show",Building(11001),this,Vector2.zero);
        Call(tooltip,"Show",Building(32001),next,Vector2.zero);Call(tooltip,"Hide",this);Assert.That(Get(tooltip,"IsVisible"),Is.True);
        Call(tooltip,"Hide",next);Assert.That(Get(tooltip,"IsVisible"),Is.False);
    }
    [Test] public void NullTooltipBuildingClearsThePreviousDetails()
    { var tooltip=Tooltip(Hud());Call(tooltip,"Show",Building(11001),this,Vector2.zero);Call(tooltip,"Show",null,this,Vector2.zero);Assert.That(Get(tooltip,"CurrentBuilding"),Is.Null); }
    [Test] public void HudFactoryWiresGameplayReferencesAndThreeGoalRows()
    {
        var hud=Hud();foreach(string name in new[]{"gameplayRoot","cityResources","stageTitle","hand","cardContent","commonPrefabs","undoView","undoButton","nextView","nextButton","pauseButton","placementFeedback","tooltip","automaticCombo","replayCombo","boardDetails"})Assert.That(Field(hud,name),Is.Not.Null,name);
        Assert.That(Rows(hud).Length,Is.EqualTo(3));Assert.That(hud.GetComponent<Canvas>().renderMode,Is.EqualTo(RenderMode.ScreenSpaceOverlay));
        Assert.That(hud.GetComponent<CanvasScaler>().referenceResolution,Is.EqualTo(new Vector2(1920,1080)));
    }
    [Test] public void TooltipAndBothComboViewsNeverInterceptBoardInput()
    {
        var hud=Hud();foreach(var view in new[]{(Component)Tooltip(hud),(Component)Auto(hud),(Component)Replay(hud)})
        {Assert.That(view.GetComponent<CanvasGroup>().blocksRaycasts,Is.False);Assert.That(view.GetComponentsInChildren<Graphic>(true).All(g=>!g.raycastTarget),Is.True);}
    }
    [Test] public void HudComboTimingFollowsDocumentAndLeavesCommonPrefabUnchanged()
    {
        var common=AssetDatabase.LoadAssetAtPath("Assets/_UrbanEquation/Data/Presentation/GameplayPrefabs.asset",Runtime("GameplayPrefabSet"));
        var before=Field(Get(common,"ComboPopupPrefab"),"displayDuration");
        var hud=Hud();Assert.That(Field(Auto(hud),"displayDuration"),Is.EqualTo(1.5f));Assert.That(Field(Auto(hud),"fadeDuration"),Is.EqualTo(.5f));
        Assert.That(Field(Get(common,"ComboPopupPrefab"),"displayDuration"),Is.EqualTo(before));
    }
    [Test] public void MissingHudSetReferencesAreReported()
    {
        var set=Own(ScriptableObject.CreateInstance(Runtime("GameplayHudSet")));var errors=new List<string>();Call(set,"Validate",errors);Assert.That(errors.Count,Is.EqualTo(2));
    }
    [Test] public void CompleteHudSetReferencesValidate()
    {
        var set=Own(ScriptableObject.CreateInstance(Runtime("GameplayHudSet")));Set(set,"hudPrefab",Hud());Set(set,"resourceIcons",Icons());
        var errors=new List<string>();Call(set,"Validate",errors);Assert.That(errors,Is.Empty);
    }
    [Test] public void HudPlayingShowsFiveResourcesAndTwoSeparateCards()
    {
        var hud=BoundHud(Bootstrap());Assert.That(Get(hud,"GameplayVisible"),Is.True);
        Assert.That(Get(Field(hud,"cityResources"),"VisibleCount"),Is.EqualTo(5));Assert.That(((Transform)Field(hud,"cardContent")).childCount,Is.EqualTo(2));
        Assert.That(Rows(hud).All(r=>!(bool)Get(r,"IsAchieved")),Is.True);Assert.That(Text(Field(hud,"stageTitle")),Is.EqualTo("Stage 1"));
    }
    [Test] public void HudUsesOptionalRuntimeCardHoversWithoutModifyingCommonCardAsset()
    {
        var hud=BoundHud(Bootstrap());var content=(Transform)Field(hud,"cardContent");
        Assert.That(content.GetComponentsInChildren(Runtime("BuildingCardHoverUI"),false).Length,Is.EqualTo(2));
        var common=Get(Field(hud,"commonPrefabs"),"BuildingCardPrefab") as Component;Assert.That(common.GetComponent(Runtime("BuildingCardHoverUI")),Is.Null);
    }
    [Test] public void BuildingRefreshesResourcesGoalsAndConsumedCardList()
    {
        var game=Bootstrap();var hud=BoundHud(game);Build(game,1,0,0);Assert.That((bool)Get(Rows(hud)[0],"IsAchieved"),Is.True);
        Assert.That(((Transform)Field(hud,"cardContent")).childCount,Is.EqualTo(1));Build(game,2,1,0);
        Assert.That(Rows(hud).All(r=>(bool)Get(r,"IsAchieved")),Is.True);Assert.That(((Transform)Field(hud,"cardContent")).childCount,Is.Zero);
        Assert.That(Text(((Component)Field(hud,"cityResources")).transform.GetChild(0).GetChild(1).GetComponent(RuntimeText())),Is.EqualTo("3"));
    }
    [Test] public void UndoRestoresGoalResourceAndCardOrder()
    {
        var game=Bootstrap();var hud=BoundHud(game);Build(game,1,0,0);Build(game,2,1,0);
        Command(Get(Get(game,"Session"),"History"),"TryUndo");Assert.That((bool)Get(Rows(hud)[2],"IsAchieved"),Is.False);
        var content=(Transform)Field(hud,"cardContent");Assert.That(content.childCount,Is.EqualTo(1));
        Assert.That(Get(content.GetChild(0).GetComponent(Runtime("BuildingCardUI")),"CardId"),Is.EqualTo(2));
        Assert.That(Text(((Component)Field(hud,"cityResources")).transform.GetChild(0).GetChild(1).GetComponent(RuntimeText())),Is.EqualTo("1"));
    }
    [Test] public void PauseHidesGameplayAndCancelRestoresIt()
    {
        var game=Bootstrap();var hud=BoundHud(game);Command(Get(game,"Flow"),"TryRequestPause");Assert.That(Get(hud,"GameplayVisible"),Is.False);
        Command(Get(game,"Flow"),"TryCancel");Assert.That(Get(hud,"GameplayVisible"),Is.True);
    }
    [Test] public void RetryRefreshesTheStartingGoalsAndCardHand()
    {
        var game=Bootstrap();var hud=BoundHud(game);Build(game,1,0,0);Build(game,2,1,0);Command(Get(game,"Flow"),"TryCompleteStage");
        Assert.That(Get(hud,"GameplayVisible"),Is.False);Command(Get(game,"Flow"),"TryRetry");Command(Get(game,"Flow"),"TryDismissIntro");
        Assert.That(Rows(hud).All(r=>!(bool)Get(r,"IsAchieved")),Is.True);Assert.That(((Transform)Field(hud,"cardContent")).childCount,Is.EqualTo(2));
    }
    [Test] public void NextStageUpdatesTitleAndAllThreeGoalDescriptions()
    {
        var game=Bootstrap();var hud=BoundHud(game);Build(game,1,0,0);Build(game,2,1,0);Command(Get(game,"Flow"),"TryCompleteStage");
        Command(Get(game,"Flow"),"TryNextStage");Command(Get(game,"Flow"),"TryDismissIntro");Assert.That(Text(Field(hud,"stageTitle")),Is.EqualTo("Stage 2"));
        Assert.That(((Transform)Field(hud,"cardContent")).childCount,Is.EqualTo(5));Assert.That(Rows(hud).All(r=>!(bool)Get(r,"IsAchieved")),Is.True);
    }
    [Test] public void UndoButtonEnablesAfterSecondBuildAndDisablesAfterUndo()
    {
        var game=Bootstrap();var hud=BoundHud(game);var button=(Button)Field(hud,"undoButton");
        Assert.That(button.interactable,Is.False);Build(game,1,0,0);Assert.That(button.interactable,Is.False);Build(game,2,1,0);Assert.That(button.interactable,Is.True);
        button.onClick.Invoke();Assert.That(button.interactable,Is.False);Assert.That(((Transform)Field(hud,"cardContent")).childCount,Is.EqualTo(1));
    }
    [Test] public void NextStageButtonRequiresGoalAndKeepsOptionalBuildingAvailable()
    {
        var game=Bootstrap();var hud=BoundHud(game);var button=(Button)Field(hud,"nextButton");Assert.That(button.interactable,Is.False);
        Build(game,1,0,0);Assert.That(button.interactable,Is.True);Assert.That(((Transform)Field(hud,"cardContent")).childCount,Is.EqualTo(1));
        button.onClick.Invoke();Assert.That(Get(Get(game,"Flow"),"State").ToString(),Is.EqualTo("StageClear"));Assert.That(Get(hud,"GameplayVisible"),Is.False);
    }
    [Test] public void PauseButtonOpensFlowConfirmation()
    { var game=Bootstrap();var hud=BoundHud(game);((Button)Field(hud,"pauseButton")).onClick.Invoke();Assert.That(Get(Get(game,"Flow"),"State").ToString(),Is.EqualTo("PauseConfirmation")); }
    [Test] public void RebindingHudUnsubscribesPreviousResources()
    {
        var first=Bootstrap();var hud=BoundHud(first);var second=Bootstrap();Call(hud,"Bind",Get(second,"Session"),Get(second,"Flow"),null,null);
        var label=((Component)Field(hud,"cityResources")).transform.GetChild(0).GetChild(1).GetComponent(RuntimeText());
        Call(Resources(first),"Add",Resource(0),9);Assert.That(Text(label),Is.EqualTo("0"));Call(Resources(second),"Add",Resource(0),2);Assert.That(Text(label),Is.EqualTo("2"));
    }
    [Test] public void DisablingAndReenablingHudRestoresPlayingPresentation()
    {
        var hud=BoundHud(Bootstrap());hud.gameObject.SetActive(false);Life(hud,"OnDisable");Assert.That(Get(hud,"GameplayVisible"),Is.False);
        hud.gameObject.SetActive(true);Life(hud,"OnEnable");Assert.That(Get(hud,"GameplayVisible"),Is.True);
    }
    [Test] public void AppliedAdjacentPairOpensPersistentReadOnlyCombo()
    {
        var game=Bootstrap();var hud=BoundHud(game);var a=Build(game,1,0,0);var b=Build(game,2,1,0);
        var before=Call(Resources(game),"CaptureResourceState");var queued=Get(Combos(game),"CurrentPresentation");
        Assert.That(Call(Details(hud),"SelectBuilding",a),Is.False);Assert.That(Call(Details(hud),"SelectBuilding",b),Is.True);
        Assert.That(Get(Replay(hud),"IsPersistent"),Is.True);Call(Replay(hud),"Tick",100f);
        Assert.That(Get(Replay(hud),"IsPersistent"),Is.True);Assert.That(Get(Combos(game),"CurrentPresentation"),Is.SameAs(queued));
        Assert.That(Call(Resources(game),"CaptureResourceState"),Is.EqualTo(before));
    }
    [Test] public void AdjacentPairCanBeSelectedInReverseOrder()
    {
        var game=Bootstrap();var hud=BoundHud(game);var a=Build(game,1,0,0);var b=Build(game,2,1,0);
        Call(Details(hud),"SelectBuilding",b);Assert.That(Call(Details(hud),"SelectBuilding",a),Is.True);
    }
    [Test] public void SelectingTheSameBuildingTwiceDoesNotOpenCombo()
    {
        var game=Bootstrap();var hud=BoundHud(game);var a=Build(game,1,0,0);Call(Details(hud),"SelectBuilding",a);
        Assert.That(Call(Details(hud),"SelectBuilding",a),Is.False);Assert.That(Get(Replay(hud),"CurrentResult"),Is.Null);
    }
    [Test] public void AdjacentBuildingsWithoutAppliedComboDoNotShowReplay()
    {
        var game=Bootstrap(2);var hud=BoundHud(game);var a=Build(game,1,0,0);var b=Build(game,3,1,0);
        Call(Details(hud),"SelectBuilding",a);Assert.That(Call(Details(hud),"SelectBuilding",b),Is.False);Assert.That(Get(Replay(hud),"CurrentResult"),Is.Null);
    }
    [Test] public void EmptyClickDismissesReplayWithoutClearingAutomaticQueue()
    {
        var game=Bootstrap();var hud=BoundHud(game);var a=Build(game,1,0,0);var b=Build(game,2,1,0);
        Call(Details(hud),"SelectBuilding",a);Call(Details(hud),"SelectBuilding",b);var queued=Get(Combos(game),"CurrentPresentation");
        Call(Details(hud),"SelectBuilding",new object[]{null});Assert.That(Get(Replay(hud),"CurrentResult"),Is.Null);
        Assert.That(Get(Combos(game),"CurrentPresentation"),Is.SameAs(queued));
    }
    [Test] public void UndoClearsSelectedBuildingsAndReplay()
    {
        var game=Bootstrap();var hud=BoundHud(game);var a=Build(game,1,0,0);var b=Build(game,2,1,0);
        Call(Details(hud),"SelectBuilding",a);Call(Details(hud),"SelectBuilding",b);Command(Get(Get(game,"Session"),"History"),"TryUndo");
        Assert.That(Get(Details(hud),"SelectedBuilding"),Is.Null);Assert.That(Get(Replay(hud),"CurrentResult"),Is.Null);
    }
    [Test] public void PauseBlocksBuildingSelectionAndDismissesReplay()
    {
        var game=Bootstrap();var hud=BoundHud(game);var a=Build(game,1,0,0);var b=Build(game,2,1,0);
        Call(Details(hud),"SelectBuilding",a);Call(Details(hud),"SelectBuilding",b);Command(Get(game,"Flow"),"TryRequestPause");
        Assert.That(Get(Replay(hud),"CurrentResult"),Is.Null);Assert.That(Call(Details(hud),"SelectBuilding",a),Is.False);
    }
    [Test] public void ForeignBuildingObjectCannotBeSelected()
    {
        var first=Bootstrap();var second=Bootstrap();var foreign=Build(first,1,0,0);var hud=BoundHud(second);
        Assert.That(Call(Details(hud),"SelectBuilding",foreign),Is.False);Assert.That(Get(Details(hud),"SelectedBuilding"),Is.Null);
    }
    [Test] public void HudAutomaticComboFadesDuringLastHalfSecondAndCompletesOnce()
    {
        var game=Bootstrap();var hud=BoundHud(game);Build(game,1,0,0);Build(game,2,1,0);var before=Call(Resources(game),"CaptureResourceState");
        Call(Auto(hud),"Tick",1.25f);Assert.That(((Component)Auto(hud)).GetComponent<CanvasGroup>().alpha,Is.EqualTo(.5f).Within(.00001f));
        Call(Auto(hud),"Tick",.25f);Assert.That(Get(Combos(game),"CurrentPresentation"),Is.Null);Assert.That(Call(Resources(game),"CaptureResourceState"),Is.EqualTo(before));
    }
    [TestCase(-1f,0f)] [TestCase(1f,2f)] [TestCase(1f,-1f)]
    public void InvalidPopupTimingIsRejected(float duration,float fade)
    { var popup=Auto(Hud());Assert.That(Call(popup,"TryConfigureTiming",duration,fade),Is.False);Assert.That(Field(popup,"displayDuration"),Is.EqualTo(1.5f)); }
    [Test] public void PersistentPopupCannotAdvanceTheAutomaticQueue()
    {
        var game=Bootstrap();var hud=BoundHud(game);Build(game,1,0,0);Build(game,2,1,0);var current=Get(Combos(game),"CurrentPresentation");
        Call(Replay(hud),"ShowPersistent",current,null);Assert.That(Call(Replay(hud),"TryCompleteCurrent"),Is.False);
        Call(Replay(hud),"DismissPersistent");Assert.That(Get(Combos(game),"CurrentPresentation"),Is.SameAs(current));
    }
    [Test] public void BindingAfterPersistentDisplayRestoresAutomaticMode()
    {
        var game=Bootstrap();var hud=BoundHud(game);Build(game,1,0,0);Build(game,2,1,0);var current=Get(Combos(game),"CurrentPresentation");
        Call(Replay(hud),"ShowPersistent",current,null);Call(Replay(hud),"Bind",Combos(game),null);
        Assert.That(Get(Replay(hud),"IsPersistent"),Is.False);Assert.That(Get(Replay(hud),"CurrentResult"),Is.SameAs(current));
    }
    [Test] public void UnconfiguredHudPreviewCreatesNoUiOrSession()
    {
        var root=Root("Unconfigured preview");var preview=root.AddComponent(Runtime("GameplayHudPreview"));Life(preview,"Update");
        Assert.That(root.transform.childCount,Is.Zero);Assert.That(root.GetComponent(Runtime("GameBootstrap")),Is.Null);
    }
    [Test] public void CancellingDragClearsPlacementFeedbackEvenWithoutTargetChange()
    {
        var hud=BoundHud(Bootstrap());var placement=Field(hud,"placement");
        Assert.That(Call(placement,"TryBeginDrag",1),Is.True);Life(hud,"LateUpdate");
        Assert.That(Text(Field(hud,"placementFeedback")),Is.Not.Empty);Call(placement,"CancelDrag");Life(hud,"LateUpdate");
        Assert.That(Text(Field(hud,"placementFeedback")),Is.Empty);
    }
    [Test] public void NonAdjacentBuildingsDoNotOpenReplay()
    {
        var game=Bootstrap();var hud=BoundHud(game);var a=Build(game,1,0,0);var b=Build(game,2,1,1);
        Call(Details(hud),"SelectBuilding",a);Assert.That(Call(Details(hud),"SelectBuilding",b),Is.False);
        Assert.That(Get(Replay(hud),"CurrentResult"),Is.Null);
    }
    [Test] public void RebindingBoardDetailsDisconnectsPreviousRestoreNotifications()
    {
        var first=Bootstrap();var hud=BoundHud(first);Build(first,1,0,0);Build(first,2,1,0);
        var second=Bootstrap();var a=Build(second,1,0,0);var b=Build(second,2,1,0);
        Call(Details(hud),"Bind",Get(second,"Session"),null,null,Tooltip(hud),Replay(hud));
        Call(Details(hud),"SelectBuilding",a);Call(Details(hud),"SelectBuilding",b);var current=Get(Replay(hud),"CurrentResult");
        Command(Get(Get(first,"Session"),"History"),"TryUndo");
        Assert.That(Get(Replay(hud),"CurrentResult"),Is.SameAs(current));Assert.That(Get(Details(hud),"SelectedBuilding"),Is.SameAs(b));
    }
}
