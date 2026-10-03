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
using UObject=UnityEngine.Object;

public class FinalGameplayContractTests
{
    private readonly List<UObject> objects=new List<UObject>();
    private string directory;
    private static Type Runtime(string name) => Type.GetType(name+", Assembly-CSharp",true);
    private static Type Editor(string name) => Type.GetType(name+", Assembly-CSharp-Editor",true);
    private static object Get(object target,string name) => target.GetType().GetProperty(name).GetValue(target);
    private static object Field(object target,string name) => target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
    private static object Call(object target,string name,params object[] args) => target.GetType().GetMethod(name).Invoke(target,args);
    private static void Life(object target,string name) => target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
    private static object[] Items(object source) => ((IEnumerable)source).Cast<object>().ToArray();
    private T Own<T>(T value) where T:UObject { objects.Add(value);return value; }
    private GameObject Root(string name) => Own(new GameObject(name,typeof(RectTransform)));
    private UObject Asset(string path,string type) => AssetDatabase.LoadAssetAtPath(path,Runtime(type));
    private UObject Content() => Asset("Assets/_UrbanEquation/Data/GameContent.asset","GameContentData");
    private Component Bootstrap(UObject content=null,string path=null)
    {
        var game=Root("Final QA test session").AddComponent(Runtime("GameBootstrap"));
        object[] args={content??Content(),path??Path.Combine(directory,Guid.NewGuid().ToString("N")+".json"),false,null};
        Assert.That(Call(game,"TryInitialize",args),Is.True,args[3] as string);return game;
    }
    private static object Flow(Component game) => Get(game,"Flow");
    private static object Session(Component game) => Get(game,"Session");
    private static object Board(Component game) => Get(Session(game),"Board");
    private static object Combos(Component game) => Get(Session(game),"Combos");
    private static object History(Component game) => Get(Session(game),"History");
    private static object Stage(Component game) => Get(Session(game),"Stage");
    private static void Command(object target,string method)
    { object[] args={null};Assert.That(Call(target,method,args),Is.True,args[0] as string); }
    private static bool TryCommand(object target,string method)
    { object[] args={null};return (bool)Call(target,method,args); }
    private static int[] Resources(Component game) => Items(Call(Get(Session(game),"Resources"),"CaptureResourceState")).Select(x=>(int)Get(x,"Amount")).ToArray();
    private static int[] CardIds(Component game) => Items(Get(Get(Session(game),"Hand"),"Cards")).Select(x=>(int)Get(x,"CardId")).ToArray();
    private static object Build(Component game,int id,int x,int y)
    { object[] args={id,new Vector2Int(x,y),null,null};Assert.That(Call(Session(game),"TryCommitBuild",args),Is.True,args[3] as string);return args[2]; }
    private static object Building(Component game,int x,int y) => Get(Call(Board(game),"GetTile",new Vector2Int(x,y)),"Building");
    private Component Playing(int stage=1,UObject content=null,string path=null)
    {
        var game=Bootstrap(content,path);Command(Flow(game),"TryPlay");Command(Flow(game),"TryDismissIntro");
        if(stage==2) { Build(game,1,0,0);Build(game,2,1,0);Command(Flow(game),"TryCompleteStage");Command(Flow(game),"TryNextStage");Command(Flow(game),"TryDismissIntro"); }
        return game;
    }
    private Component Hud(Component game)
    {
        var set=Asset("Assets/_UrbanEquation/Data/Presentation/GameplayHud.asset","GameplayHudSet");var prefab=(Component)Get(set,"HudPrefab");
        var root=Own(UObject.Instantiate(prefab.gameObject));var view=root.GetComponent(Runtime("GameHudUI"));
        var placement=Root("Final QA placement").AddComponent(Runtime("BuildingPlacementController"));Call(placement,"Configure",Session(game),null);
        Call(view,"SetScreenBackdropVisible",true);Call(view,"Bind",Session(game),Flow(game),placement,null);
        foreach(string field in new[]{"hand","undoView","nextView","automaticCombo","boardDetails"})Life(Field(view,field),"OnEnable");
        return view;
    }
    private Component Screens(Component game)
    {
        var data=Asset("Assets/_UrbanEquation/Data/Presentation/GameApplication.asset","GameApplicationSettings");Assert.That(data,Is.Not.Null,"Commit Phase4-C generated settings before final QA.");
        var prefab=(Component)Get(data,"ScreensPrefab");var root=Own(UObject.Instantiate(prefab.gameObject));var view=root.GetComponent(Runtime("GameScreensUI"));Call(view,"Bind",Flow(game),null);return view;
    }
    private UObject Sandbox()
    {
        object[] args={Content(),null,null};var content=(UObject)Editor("UrbanEquationFinalQa").GetMethod("CreateMultiComboContent").Invoke(null,args);
        Own((UObject)args[2]);Own((UObject)args[1]);Own(content);return content;
    }
    private static object[] Cards(Component hud) => ((Transform)Field(hud,"cardContent")).Cast<Transform>().Select(x=>(object)x.GetComponent(Runtime("BuildingCardUI"))).ToArray();
    private static string Fingerprint(Component game)
    {
        object[] args={null,null};Assert.That(Call(Board(game),"TryCaptureBuildings",args),Is.True,args[1] as string);
        string buildings=string.Join(";",Items(args[0]).Select(x=>Get(x,"Coordinate")+":"+Get(x,"BuildingCode")).OrderBy(x=>x));
        string combos=string.Join(";",Items(Get(Combos(game),"Results")).Select(x=>Get(x,"ResultId")+":"+Get(x,"ComboCode")+":"+Get(x,"SourceCoordinate")+":"+Get(x,"AdjacentCoordinate")));
        string goals=string.Join(",",Items(Get(Stage(game),"GoalStates")));
        var hand=Get(Session(game),"Hand");string availability=string.Join(",",CardIds(game).Select(id=>Call(hand,"IsCardAvailable",id)));
        string last=string.Join(",",Items(Get(Session(game),"LastComboResults")).Select(x=>Get(x,"ResultId")));
        return buildings+"|"+string.Join(",",Resources(game))+"|"+string.Join(",",CardIds(game))+"|"+availability+"|"+combos+"|"+last+"|"+goals+"|"+Get(Stage(game),"Rank")+"|"+Get(Stage(game),"NextStageAvailable")+"|"+Get(Stage(game),"IsCleared")+"|"+Get(History(game),"Count");
    }
    private static void StageTwoBuilds(Component game)
    { Build(game,1,0,0);Build(game,5,0,1);Build(game,4,0,2);Build(game,3,1,0);Build(game,2,1,2); }
    [SetUp] public void SetUp() => directory=Path.Combine(Path.GetTempPath(),"UE-FinalQA-"+Guid.NewGuid().ToString("N"));
    [TearDown] public void TearDown()
    {
        for(int i=objects.Count-1;i>=0;i--)if(objects[i]!=null)UObject.DestroyImmediate(objects[i]);objects.Clear();
        if(Directory.Exists(directory))Directory.Delete(directory,true);
    }
    [Test] public void CommittedFinalSetupHasNoMissingReferencesOrScripts()
    { var errors=(IEnumerable)Editor("UrbanEquationFinalQa").GetMethod("CollectValidationErrors").Invoke(null,null);Assert.That(errors,Is.Empty); }
    [Test] public void BuildListStartsWithCommittedLobbyGameAndEnablesBoth()
    { var scenes=EditorBuildSettings.scenes;Assert.That(scenes.Length,Is.GreaterThanOrEqualTo(2));Assert.That(scenes[0].path,Is.EqualTo("Assets/_UrbanEquation/Scenes/Lobby.unity"));Assert.That(scenes[1].path,Is.EqualTo("Assets/_UrbanEquation/Scenes/Game.unity"));Assert.That(scenes.Take(2).All(x=>x.enabled),Is.True); }
    [TestCase(1)][TestCase(2)][TestCase(3)]
    public void StageOneAllRanksReachCommittedClearUiAndSave(int rank)
    {
        var game=Playing();var hud=Hud(game);var screens=Screens(game);Build(game,1,0,0);if(rank>1)Build(game,2,1,rank==2?1:0);
        Assert.That(Get(Stage(game),"Rank"),Is.EqualTo(rank));((Button)Field(hud,"nextButton")).onClick.Invoke();
        Assert.That(Get(Flow(game),"State").ToString(),Is.EqualTo("StageClear"));Assert.That(Call(Flow(game),"GetBestRank",1),Is.EqualTo(rank));
        Assert.That(Items(Field(screens,"clearStars")).Cast<Image>().Count(x=>x.enabled),Is.EqualTo(rank));
        Assert.That(Items(Field(screens,"clearGoals")).Select(x=>Get(x,"IsAchieved")),Is.EqualTo(new[]{true,rank>=2,rank==3}));
    }
    [Test] public void StageTwoGoldenSolutionMatchesResourcesRankAndFinalUi()
    {
        var game=Playing(2);var hud=Hud(game);var screens=Screens(game);StageTwoBuilds(game);
        Assert.That(Resources(game),Is.EqualTo(new[]{0,1,0,1,0}));Assert.That(Get(Stage(game),"Rank"),Is.EqualTo(3));Assert.That(Cards(hud),Is.Empty);
        ((Button)Field(hud,"nextButton")).onClick.Invoke();Assert.That(((Button)Field(screens,"nextButton")).interactable,Is.EqualTo((int)Get(Flow(game),"StageCount")>2));
        Assert.That(Items(Field(screens,"clearStars")).Cast<Image>().All(x=>x.enabled),Is.True);
    }
    [Test] public void StageTwoUndoEachCompletedTurnRestoresWholeStateAndHudOrder()
    {
        var game=Playing(2);var hud=Hud(game);var states=new List<string>();
        foreach(var build in new[]{new[]{1,0,0},new[]{5,0,1},new[]{4,0,2},new[]{3,1,0},new[]{2,1,2}})
        { Build(game,build[0],build[1],build[2]);states.Add(Fingerprint(game)); }
        for(int i=3;i>=0;i--)
        {
            ((Button)Field(hud,"undoButton")).onClick.Invoke();Assert.That(Fingerprint(game),Is.EqualTo(states[i]));
            Assert.That(Cards(hud).Select(x=>(int)Get(x,"CardId")),Is.EqualTo(CardIds(game)));
            Assert.That(Items(Field(hud,"goals")).Select(x=>Get(x,"IsAchieved")),Is.EqualTo(Items(Get(Stage(game),"GoalStates"))));
        }
        Assert.That(((Button)Field(hud,"undoButton")).interactable,Is.False);Assert.That(TryCommand(History(game),"TryUndo"),Is.False);
    }
    [Test] public void BuildAfterUndoDiscardsOldFutureAndDoesNotDoublePayCombo()
    {
        var game=Playing(2);var hud=Hud(game);Build(game,1,0,0);Build(game,5,0,1);string second=Fingerprint(game);Build(game,4,0,2);
        Command(History(game),"TryUndo");Assert.That(Fingerprint(game),Is.EqualTo(second));Build(game,4,1,1);
        Assert.That(Get(History(game),"Count"),Is.EqualTo(3));Command(History(game),"TryUndo");Assert.That(Fingerprint(game),Is.EqualTo(second));Assert.That(Cards(hud).Select(x=>(int)Get(x,"CardId")),Is.EqualTo(new[]{2,3,4}));
    }
    [Test] public void StageTwoOverlayAndPointerBlockMatchAffordability()
    {
        var game=Playing(2);var hud=Hud(game);var cards=Cards(hud);
        Assert.That(cards.Select(x=>(bool)Get(x,"IsAvailable")),Is.EqualTo(new[]{true,false,true,false,false}));
        foreach(var card in cards)
        { bool available=(bool)Get(card,"IsAvailable");Assert.That(((Image)Field(card,"disabledOverlay")).gameObject.activeSelf,Is.EqualTo(!available));Assert.That(((Button)Field(card,"button")).interactable,Is.EqualTo(available)); }
        var placement=Field(hud,"placement");Assert.That(Call(placement,"TryBeginDrag",2),Is.False);Assert.That(Resources(game),Is.EqualTo(new[]{0,1,0,1,0}));
    }
    [Test] public void ConsumedCardDisappearsAndUndoRestoresOriginalSlots()
    { var game=Playing(2);var hud=Hud(game);Build(game,1,0,0);Build(game,5,0,1);Assert.That(Cards(hud).Select(x=>(int)Get(x,"CardId")),Is.EqualTo(new[]{2,3,4}));Command(History(game),"TryUndo");Assert.That(Cards(hud).Select(x=>(int)Get(x,"CardId")),Is.EqualTo(new[]{2,3,4,5})); }
    [Test] public void PauseCancelKeepsAllTurnStateAndConfirmationDropsRuntimeOnly()
    {
        var game=Playing(2);var hud=Hud(game);var screens=Screens(game);Build(game,1,0,0);Build(game,5,0,1);string before=Fingerprint(game);
        ((Button)Field(hud,"pauseButton")).onClick.Invoke();Assert.That(Get(Session(game),"GameplayEnabled"),Is.False);Assert.That(Call(Field(hud,"placement"),"TryBeginDrag",2),Is.False);
        ((Button)Field(screens,"pauseNoButton")).onClick.Invoke();Assert.That(Fingerprint(game),Is.EqualTo(before));
        ((Button)Field(hud,"pauseButton")).onClick.Invoke();((Button)Field(screens,"pauseYesButton")).onClick.Invoke();
        Assert.That(Get(Flow(game),"State").ToString(),Is.EqualTo("Lobby"));Assert.That(Get(History(game),"Count"),Is.Zero);Assert.That(Call(Flow(game),"GetBestRank",1),Is.EqualTo(3));
    }
    [Test] public void ContinueSelectedStageStartsFreshAndKeepsBestRank()
    {
        string path=Path.Combine(directory,"roundtrip.json");var first=Playing(2,path:path);StageTwoBuilds(first);Command(Flow(first),"TryCompleteStage");
        var second=Bootstrap(path:path);var screens=Screens(second);Assert.That(Call(screens,"TryContinue"),Is.True);Assert.That(Call(screens,"TrySelectStage",2),Is.True);
        Assert.That(Resources(second),Is.EqualTo(new[]{0,1,0,1,0}));Assert.That(CardIds(second),Is.EqualTo(new[]{1,2,3,4,5}));Assert.That(Get(History(second),"Count"),Is.Zero);
        Assert.That(Call(Flow(second),"GetBestRank",2),Is.EqualTo(3));Assert.That(Get(Session(second),"GameplayEnabled"),Is.False);
    }
    [Test] public void LowerRankReplayCannotLowerPersistedHighestRank()
    {
        string path=Path.Combine(directory,"best-rank.json");var first=Playing(path:path);Build(first,1,0,0);Build(first,2,1,0);Command(Flow(first),"TryCompleteStage");Command(Flow(first),"TryRetry");Command(Flow(first),"TryDismissIntro");Build(first,1,0,0);Command(Flow(first),"TryCompleteStage");
        var second=Bootstrap(path:path);Assert.That(Call(Flow(second),"GetBestRank",1),Is.EqualTo(3));Assert.That(Call(Flow(second),"IsStageUnlocked",2),Is.True);
    }
    [Test] public void RetryClearsComboReplaySelectionAndCompletedTurnState()
    {
        var game=Playing();var hud=Hud(game);var screens=Screens(game);Build(game,1,0,0);Build(game,2,1,0);
        var details=Field(hud,"boardDetails");Call(details,"SelectBuilding",Building(game,0,0));Call(details,"SelectBuilding",Building(game,1,0));Assert.That(Get(Field(hud,"replayCombo"),"IsPersistent"),Is.True);
        ((Button)Field(hud,"nextButton")).onClick.Invoke();((Button)Field(screens,"retryButton")).onClick.Invoke();
        Assert.That(Get(History(game),"Count"),Is.Zero);Assert.That(Items(Get(Combos(game),"Results")),Is.Empty);Assert.That(Get(Field(hud,"replayCombo"),"IsPersistent"),Is.False);Assert.That(Get(details,"SelectedBuilding"),Is.Null);
    }
    [Test] public void MultiComboQaClonesLeaveAuthoredContentUnchanged()
    {
        var source=Content();string before=EditorJsonUtility.ToJson(source);var original=Get(source,"Stages");var first=Items(Get(original,"Stages"))[0];string stageBefore=EditorJsonUtility.ToJson((UObject)first);
        var content=Sandbox();var clone=Items(Get(Get(content,"Stages"),"Stages"))[0];Assert.That(clone,Is.Not.SameAs(first));Assert.That(Get(Items(Get(clone,"BuildingCards"))[0],"Count"),Is.EqualTo(3));
        Assert.That(EditorJsonUtility.ToJson(source),Is.EqualTo(before));Assert.That(EditorJsonUtility.ToJson((UObject)first),Is.EqualTo(stageBefore));Assert.That(Items(Get(Get(content,"Stages"),"Stages"))[1],Is.SameAs(Items(Get(original,"Stages"))[1]));
    }
    [Test] public void OneBuildQueuesTwoActualComboResultsInLeftThenDownOrder()
    {
        var game=Playing(content:Sandbox());Hud(game);Build(game,1,0,1);Build(game,2,1,0);Build(game,3,1,1);
        var results=Items(Get(Combos(game),"Results"));Assert.That(results.Select(x=>Get(x,"AdjacentCoordinate")),Is.EqualTo(new[]{new Vector2Int(0,1),new Vector2Int(1,0)}));
        Assert.That(Resources(game),Is.EqualTo(new[]{5,0,0,0,0}));Assert.That(Get(Combos(game),"PendingPresentationCount"),Is.EqualTo(1));Assert.That(Get(Combos(game),"CurrentPresentation"),Is.SameAs(results[0]));
    }
    [Test] public void ActualHudTicksBothCombosOnceWithoutChangingResources()
    {
        var game=Playing(content:Sandbox());var hud=Hud(game);Build(game,1,0,1);Build(game,2,1,0);Build(game,3,1,1);var popup=Field(hud,"automaticCombo");var results=Items(Get(Combos(game),"Results"));
        string before=Fingerprint(game);Assert.That(Get(popup,"CurrentResult"),Is.SameAs(results[0]));Call(popup,"Tick",1.49f);Assert.That(Get(popup,"CurrentResult"),Is.SameAs(results[0]));Call(popup,"Tick",.02f);Assert.That(Get(popup,"CurrentResult"),Is.SameAs(results[1]));
        Call(popup,"Tick",1.51f);Assert.That(Get(Combos(game),"CurrentPresentation"),Is.Null);Call(popup,"Tick",10f);Assert.That(Fingerprint(game),Is.EqualTo(before));
    }
    [Test] public void MultiComboUndoClearsPendingUiAndRebuildPaysOnlyOnce()
    {
        var game=Playing(content:Sandbox());var hud=Hud(game);Build(game,1,0,1);Build(game,2,1,0);string before=Fingerprint(game);Build(game,3,1,1);
        ((Button)Field(hud,"undoButton")).onClick.Invoke();Assert.That(Fingerprint(game),Is.EqualTo(before));Assert.That(Get(Combos(game),"CurrentPresentation"),Is.Null);Assert.That(Get(Field(hud,"automaticCombo"),"CurrentResult"),Is.Null);
        Build(game,3,1,1);Assert.That(Resources(game),Is.EqualTo(new[]{5,0,0,0,0}));Assert.That(Items(Get(Combos(game),"Results")).Length,Is.EqualTo(2));
    }
    [Test] public void MultiComboManualReplayDoesNotConsumeAutomaticQueue()
    {
        var game=Playing(content:Sandbox());var hud=Hud(game);Build(game,1,0,1);Build(game,2,1,0);Build(game,3,1,1);string before=Fingerprint(game);object current=Get(Combos(game),"CurrentPresentation");
        var details=Field(hud,"boardDetails");Call(details,"SelectBuilding",Building(game,0,1));Assert.That(Call(details,"SelectBuilding",Building(game,1,1)),Is.True);
        Call(Field(hud,"replayCombo"),"Tick",100f);Assert.That(Get(Field(hud,"replayCombo"),"IsPersistent"),Is.True);Assert.That(Get(Combos(game),"CurrentPresentation"),Is.SameAs(current));Assert.That(Get(Combos(game),"PendingPresentationCount"),Is.EqualTo(1));Assert.That(Fingerprint(game),Is.EqualTo(before));
    }
    [Test] public void FirstBuildRemainsUndoBoundaryWithRealHud()
    { var game=Playing();var hud=Hud(game);Build(game,1,0,0);string before=Fingerprint(game);Assert.That(((Button)Field(hud,"undoButton")).interactable,Is.False);Assert.That(TryCommand(History(game),"TryUndo"),Is.False);Assert.That(Fingerprint(game),Is.EqualTo(before)); }
    [Test] public void SelectingStageTwoClearReturnsRanksAndUnlocksInRealUi()
    {
        var game=Playing(2);var screens=Screens(game);StageTwoBuilds(game);Command(Flow(game),"TryCompleteStage");
        ((Button)Field(screens,"clearSelectButton")).onClick.Invoke();Assert.That(Get(Flow(game),"State").ToString(),Is.EqualTo("StageSelect"));
        var rows=Items(Field(screens,"rows"));int count=(int)Get(Flow(game),"StageCount");
        Assert.That(rows.Length,Is.EqualTo(count));
        Assert.That(rows.Select(x=>Get(x,"BestRank")),Is.EqualTo(Enumerable.Range(1,count).Select(number=>number<=2?3:0)));
        Assert.That(rows.Select(x=>(bool)Get(x,"IsUnlocked")),Is.EqualTo(Enumerable.Range(1,count).Select(number=>number<=3)));
    }
}
