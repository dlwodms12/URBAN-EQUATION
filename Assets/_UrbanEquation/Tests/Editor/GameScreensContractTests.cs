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

public class GameScreensContractTests
{
    private readonly List<UObject> objects=new List<UObject>();
    private string directory;
    private readonly List<string> loads=new List<string>();
    private int quits;
    private bool available;
    private bool throwLoad;
    private static Type Runtime(string name) => Type.GetType(name+", Assembly-CSharp",true);
    private static Type Editor(string name) => Type.GetType(name+", Assembly-CSharp-Editor",true);
    private static object Get(object target,string name) => target.GetType().GetProperty(name).GetValue(target);
    private static object Field(object target,string name) => target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target);
    private static void Set(object target,string name,object value) => target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);
    private static object Call(object target,string name,params object[] args) => target.GetType().GetMethod(name).Invoke(target,args);
    private static void Life(object target,string name) => target.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,null);
    private static object[] Items(object value) => ((IEnumerable)value).Cast<object>().ToArray();
    private T Own<T>(T value) where T:UObject { objects.Add(value); return value; }
    private GameObject Root(string name) => Own(new GameObject(name,typeof(RectTransform)));
    private UObject Asset(string path,string type) => AssetDatabase.LoadAssetAtPath(path,Runtime(type));
    private Sprite Sprite()
    { var texture=Own(new Texture2D(8,8)); return Own(UnityEngine.Sprite.Create(texture,new Rect(0,0,8,8),new Vector2(.5f,.5f))); }
    private Component Screens()
    {
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(x=>x.GetType("TMPro.TMP_FontAsset",false)).First(x=>x!=null);
        var font=AssetDatabase.LoadAssetAtPath("Assets/_UrbanEquation/Fonts/NotoSansKR-Medium SDF.asset",type);
        var root=Own((GameObject)Editor("UrbanEquationScreensFactory").GetMethod("CreateScreens").Invoke(null,new object[]{font,Enumerable.Range(0,20).Select(_=>Sprite()).ToArray()}));
        return root.GetComponent(Runtime("GameScreensUI"));
    }
    private UObject Settings(Component view=null)
    {
        var data=Own(ScriptableObject.CreateInstance(Runtime("GameApplicationSettings")));
        Set(data,"content",Asset("Assets/_UrbanEquation/Data/GameContent.asset","GameContentData"));
        Set(data,"hud",Asset("Assets/_UrbanEquation/Data/Presentation/GameplayHud.asset","GameplayHudSet"));
        Set(data,"screensPrefab",view??Screens()); return data;
    }
    private Component Bootstrap()
    {
        var game=Root("Screens test session").AddComponent(Runtime("GameBootstrap"));
        object[] args={Asset("Assets/_UrbanEquation/Data/GameContent.asset","GameContentData"),Path.Combine(directory,Guid.NewGuid().ToString("N")+".json"),false,null};
        Assert.That(Call(game,"TryInitialize",args),Is.True,args[3] as string); return game;
    }
    private object Flow(Component game) => Get(game,"Flow");
    private object Session(Component game) => Get(game,"Session");
    private static void Command(object target,string method)
    { object[] args={null}; Assert.That(Call(target,method,args),Is.True,args[0] as string); }
    private static string State(object flow) => Get(flow,"State").ToString();
    private static object Scene(string name) => Enum.Parse(Runtime("GameFlowScene"),name);
    private Component Router(Component game,UObject settings=null)
    {
        var router=Root("Scene router").AddComponent(Runtime("GameSceneRouter"));
        Func<string,bool> canLoad=_=>available;
        Action<string> load=path=>{if(throwLoad)throw new IOException("Injected load failure");loads.Add(path);};
        Action quit=()=>quits++;
        object[] args={Flow(game),Session(game),settings??Settings(),canLoad,load,quit,null};
        Assert.That(Call(router,"TryConfigure",args),Is.True,args[6] as string); return router;
    }
    private Component Bound(Component game,Component router=null)
    { var view=Screens(); Call(view,"Bind",Flow(game),router); return view; }
    private static bool Active(Component view,string field) => ((GameObject)Field(view,field)).activeSelf;
    private static Button Button(Component view,string field) => (Button)Field(view,field);
    private static string Text(object value) => (string)Get(value,"text");
    private static bool Ui(Component view,string method) => (bool)Call(view,method);
    private static void Build(Component game,int card,int x,int y)
    { object[] args={card,new Vector2Int(x,y),null,null};Assert.That(Call(Get(game,"Session"),"TryCommitBuild",args),Is.True,args[3] as string); }
    private Component Playing()
    { var game=Bootstrap(); Command(Flow(game),"TryPlay"); Command(Flow(game),"TryDismissIntro"); return game; }
    private Component Cleared()
    { var game=Playing(); Build(game,1,0,0); Build(game,2,1,0); Command(Flow(game),"TryCompleteStage"); return game; }
    [SetUp] public void SetUp()
    { directory=Path.Combine(Path.GetTempPath(),"UE-Screens-"+Guid.NewGuid().ToString("N"));loads.Clear();quits=0;available=true;throwLoad=false; }
    [TearDown] public void TearDown()
    {
        for(int i=objects.Count-1;i>=0;i--)if(objects[i]!=null)UObject.DestroyImmediate(objects[i]);objects.Clear();
        if(Directory.Exists(directory))Directory.Delete(directory,true);
    }
    [Test] public void ApplicationSettingsRejectMissingReferences()
    { var data=Own(ScriptableObject.CreateInstance(Runtime("GameApplicationSettings")));var errors=new List<string>();Call(data,"Validate",errors);Assert.That(errors.Count,Is.EqualTo(3)); }
    [Test] public void ApplicationSettingsAcceptCommittedHudAndContent()
    { var errors=new List<string>();Call(Settings(),"Validate",errors);Assert.That(errors,Is.Empty); }
    [TestCase("Game")][TestCase("Assets/../Game.unity")][TestCase("Assets/Game.txt")][TestCase("Assets\\Game.unity")]
    public void ApplicationSettingsRejectInvalidScenePaths(string path)
    { var data=Settings();Set(data,"gameScenePath",path);var errors=new List<string>();Call(data,"Validate",errors);Assert.That(errors.Any(x=>x.Contains("scene paths")),Is.True); }
    [Test] public void ApplicationSettingsRequireDistinctScenes()
    { var data=Settings();Set(data,"gameScenePath",Call(data,"ScenePath",Scene("Lobby")));var errors=new List<string>();Call(data,"Validate",errors);Assert.That(errors,Is.Not.Empty); }
    [Test] public void PreviewSavePathIsSeparateFromProductionAndOldDebug()
    {
        var method=Runtime("GameApplication").GetMethod("SavePath");string preview=(string)method.Invoke(null,new object[]{true});string actual=(string)method.Invoke(null,new object[]{false});
        Assert.That(preview,Is.Not.EqualTo(actual));Assert.That(preview,Does.Contain("UrbanEquationPhase4CPreview"));Assert.That(actual,Does.Contain("URBAN-EQUATION"));Assert.That(preview,Does.Not.Contain("phase3j-progress"));
    }
    [Test] public void AddingApplicationAndSceneEntryDoesNotInitializeOrLoad()
    { var root=Root("Unconfigured application");var app=root.AddComponent(Runtime("GameApplication"));root.AddComponent(Runtime("GameSceneEntry"));Assert.That(Get(app,"IsInitialized"),Is.False);Assert.That(Runtime("GameApplication").GetProperty("Instance").GetValue(null),Is.Null);Assert.That(Directory.Exists(directory),Is.False); }
    [Test] public void ApplicationPublishesOneSessionAndRejectsSecondOwner()
    {
        var data=Settings();var app=Root("Application owner").AddComponent(Runtime("GameApplication"));
        object[] args={data,Path.Combine(directory,"application.json"),new Func<string,bool>(_=>true),new Action<string>(_=>{}),new Action(()=>{}),null};
        Assert.That(Call(app,"TryInitialize",args),Is.True,args[5] as string);Assert.That(Get(app,"IsInitialized"),Is.True);
        Assert.That(Runtime("GameApplication").GetProperty("Instance").GetValue(null),Is.SameAs(app));
        var second=Root("Second application").AddComponent(Runtime("GameApplication"));Assert.That(Call(second,"TryInitialize",args),Is.False);
        Assert.That(Get(second,"IsInitialized"),Is.False);Assert.That(Get(app,"Bootstrap"),Is.Not.Null);
    }
    [Test] public void RouterRejectsMissingIoWithoutPublishing()
    { var game=Bootstrap();var router=Root("Invalid router").AddComponent(Runtime("GameSceneRouter"));object[] args={Flow(game),Session(game),Settings(),null,null,null,null};Assert.That(Call(router,"TryConfigure",args),Is.False);Assert.That(Get(router,"IsReady"),Is.False); }
    [Test] public void RouterWaitsForInitialSceneReady()
    { var game=Bootstrap();var router=Router(game);Assert.That(Get(router,"IsReady"),Is.False);Assert.That(loads,Is.Empty);Call(router,"NotifySceneReady",Scene("Lobby"));Assert.That(Get(router,"IsReady"),Is.True);Assert.That(loads,Is.Empty); }
    [Test] public void PlayRequestsGameWhileRetainingSessionManagers()
    {
        var game=Bootstrap();var session=Session(game);var router=Router(game);Call(router,"NotifySceneReady",Scene("Lobby"));Command(Flow(game),"TryPlay");
        Assert.That(loads.Single(),Does.EndWith("/Game.unity"));Assert.That(Get(router,"IsLoading"),Is.True);Assert.That(Session(game),Is.SameAs(session));Assert.That(Get(session,"GameplayEnabled"),Is.False);
    }
    [Test] public void PlayingCommandBeforeSceneReadyCannotEnableBuildInput()
    { var game=Bootstrap();var router=Router(game);Call(router,"NotifySceneReady",Scene("Lobby"));Command(Flow(game),"TryPlay");Command(Flow(game),"TryDismissIntro");Assert.That(Get(Session(game),"GameplayEnabled"),Is.False); }
    [Test] public void GameReadyEnablesOnlyPlayingSession()
    { var game=Bootstrap();var router=Router(game);Call(router,"NotifySceneReady",Scene("Lobby"));Command(Flow(game),"TryPlay");Command(Flow(game),"TryDismissIntro");Call(router,"NotifySceneReady",Scene("Game"));Assert.That(Get(router,"IsReady"),Is.True);Assert.That(Get(Session(game),"GameplayEnabled"),Is.True); }
    [Test] public void IntroRemainsInputBlockedAfterGameReady()
    { var game=Bootstrap();var router=Router(game);Call(router,"NotifySceneReady",Scene("Lobby"));Command(Flow(game),"TryPlay");Call(router,"NotifySceneReady",Scene("Game"));Assert.That(Get(Session(game),"GameplayEnabled"),Is.False); }
    [Test] public void RetryWithinGameDoesNotReloadScene()
    { var game=Cleared();var router=Router(game);Call(router,"NotifySceneReady",Scene("Game"));Command(Flow(game),"TryRetry");Assert.That(loads,Is.Empty);Assert.That(State(Flow(game)),Is.EqualTo("StageIntro")); }
    [Test] public void ContinueAndBackStayOnLobbyScene()
    { var game=Playing();Command(Flow(game),"TryRequestPause");Command(Flow(game),"TryConfirmPause");var router=Router(game);Call(router,"NotifySceneReady",Scene("Lobby"));Command(Flow(game),"TryContinue");Command(Flow(game),"TryCancel");Assert.That(loads,Is.Empty); }
    [Test] public void MissingSceneBlocksInputAndCanBeRetried()
    {
        var game=Bootstrap();var router=Router(game);Call(router,"NotifySceneReady",Scene("Lobby"));available=false;Command(Flow(game),"TryPlay");
        Assert.That(Get(router,"LastError"),Is.Not.Null);Assert.That(Get(router,"IsReady"),Is.False);Assert.That(Get(Session(game),"GameplayEnabled"),Is.False);
        available=true;object[] args={null};Assert.That(Call(router,"TryRetryLoad",args),Is.True);Assert.That(loads.Count,Is.EqualTo(1));
    }
    [Test] public void LoaderExceptionLeavesRecoverableFailure()
    { var game=Bootstrap();var router=Router(game);Call(router,"NotifySceneReady",Scene("Lobby"));throwLoad=true;Command(Flow(game),"TryPlay");Assert.That(Get(router,"IsLoading"),Is.False);Assert.That(Get(router,"LastError"),Is.EqualTo("Injected load failure"));throwLoad=false;object[] args={null};Assert.That(Call(router,"TryRetryLoad",args),Is.True); }
    [Test] public void LoadingDisablesAllScreenCommands()
    { var game=Bootstrap();var router=Router(game);Call(router,"NotifySceneReady",Scene("Lobby"));var view=Bound(game,router);Assert.That(Ui(view,"TryPlay"),Is.True);Assert.That(Get(view,"CanInteract"),Is.False);Assert.That(Ui(view,"TryDismissIntro"),Is.False);Assert.That(Active(view,"transition"),Is.True); }
    [Test] public void WrongReadyNotificationDoesNotReleaseInFlightLoad()
    { var game=Bootstrap();var router=Router(game);Call(router,"NotifySceneReady",Scene("Lobby"));Command(Flow(game),"TryPlay");Call(router,"NotifySceneReady",Scene("Lobby"));Assert.That(Get(router,"IsLoading"),Is.True); }
    [Test] public void RouterRebindUnsubscribesOldFlow()
    {
        var first=Bootstrap();var second=Bootstrap();var router=Router(first);
        object[] args={Flow(second),Session(second),Settings(),new Func<string,bool>(_=>true),new Action<string>(path=>loads.Add(path)),new Action(()=>quits++),null};
        Assert.That(Call(router,"TryConfigure",args),Is.True);Call(router,"NotifySceneReady",Scene("Lobby"));Command(Flow(first),"TryPlay");Assert.That(loads,Is.Empty);
    }
    [Test] public void DisablingRouterBlocksBuildAndReenablingRestoresReadyPlay()
    { var game=Playing();var router=Router(game);Call(router,"NotifySceneReady",Scene("Game"));router.gameObject.SetActive(false);Life(router,"OnDisable");Assert.That(Get(Session(game),"GameplayEnabled"),Is.False);router.gameObject.SetActive(true);Life(router,"OnEnable");Assert.That(Get(Session(game),"GameplayEnabled"),Is.True); }
    [Test] public void QuitCallbackRunsOnlyForConfirmedExit()
    { var game=Bootstrap();var router=Router(game);Call(router,"NotifySceneReady",Scene("Lobby"));Command(Flow(game),"TryRequestExit");Assert.That(quits,Is.Zero);Command(Flow(game),"TryCancel");Assert.That(quits,Is.Zero);Command(Flow(game),"TryRequestExit");Command(Flow(game),"TryConfirmExit");Assert.That(quits,Is.EqualTo(1)); }
    [TestCase("Lobby","lobby",false)][TestCase("NewGameConfirmation","newGame",true)][TestCase("StageSelect","stageSelect",true)]
    [TestCase("StageIntro","intro",true)][TestCase("PauseConfirmation","pause",true)][TestCase("StageClear","clear",true)][TestCase("ExitConfirmation","exit",true)]
    public void ScreenStateShowsCorrectPanelAndModalBlocker(string state,string panel,bool modal)
    {
        var game=Bootstrap();var flow=Flow(game);var stateProperty=flow.GetType().GetProperty("State");stateProperty.SetValue(flow,Enum.Parse(Runtime("GameFlowState"),state));
        var view=Bound(game);Assert.That(Active(view,panel),Is.True);Assert.That(Active(view,"modalBlocker"),Is.EqualTo(modal));
    }
    [Test] public void LobbyContinueIsDisabledWithoutSave()
    { var view=Bound(Bootstrap());Assert.That(Button(view,"continueButton").interactable,Is.False);Assert.That(Ui(view,"TryContinue"),Is.False); }
    [Test] public void ContinueBecomesAvailableAfterStartingAndLeavingStage()
    { var game=Playing();Command(Flow(game),"TryRequestPause");Command(Flow(game),"TryConfirmPause");var view=Bound(game);Assert.That(Button(view,"continueButton").interactable,Is.True);Assert.That(Ui(view,"TryContinue"),Is.True);Assert.That(State(Flow(game)),Is.EqualTo("StageSelect")); }
    [Test] public void CancellingNewGamePreservesSavedProgress()
    { var game=Cleared();Command(Flow(game),"TryReturnToStageSelect");Command(Flow(game),"TryCancel");var view=Bound(game);Assert.That(Ui(view,"TryPlay"),Is.True);Assert.That(Active(view,"newGame"),Is.True);Assert.That(Ui(view,"TryCancel"),Is.True);Assert.That(Call(Flow(game),"GetBestRank",1),Is.EqualTo(3)); }
    [Test] public void ConfirmedNewGameResetsSavedBestRank()
    { var game=Cleared();Command(Flow(game),"TryReturnToStageSelect");Command(Flow(game),"TryCancel");var view=Bound(game);Ui(view,"TryPlay");Assert.That(Ui(view,"TryConfirmNewGame"),Is.True);Assert.That(Call(Flow(game),"GetBestRank",1),Is.EqualTo(0));Assert.That(State(Flow(game)),Is.EqualTo("StageIntro")); }
    [Test] public void NewGameConfirmationOffersContinueForValidProgress()
    { var game=Playing();Command(Flow(game),"TryRequestPause");Command(Flow(game),"TryConfirmPause");var view=Bound(game);Ui(view,"TryPlay");Assert.That(Button(view,"resumeButton").interactable,Is.True);Assert.That(Ui(view,"TryContinue"),Is.True);Assert.That(State(Flow(game)),Is.EqualTo("StageSelect")); }
    [Test] public void CorruptSaveDisablesResumeWhileAllowingConfirmedNewGame()
    {
        Directory.CreateDirectory(directory);string path=Path.Combine(directory,"corrupt.json");File.WriteAllText(path,"not json");
        var game=Root("Corrupt save session").AddComponent(Runtime("GameBootstrap"));object[] args={Asset("Assets/_UrbanEquation/Data/GameContent.asset","GameContentData"),path,false,null};
        Assert.That(Call(game,"TryInitialize",args),Is.True);var view=Bound(game);Assert.That(Button(view,"continueButton").interactable,Is.False);
        Ui(view,"TryPlay");Assert.That(Active(view,"newGame"),Is.True);Assert.That(Button(view,"resumeButton").interactable,Is.False);
        Assert.That(Ui(view,"TryConfirmNewGame"),Is.True);Assert.That(State(Flow(game)),Is.EqualTo("StageIntro"));
    }
    [Test] public void ExitCancelReturnsToLobbyWithoutRequestingQuit()
    { var game=Bootstrap();var view=Bound(game);Ui(view,"TryRequestExit");Assert.That(Ui(view,"TryCancel"),Is.True);Assert.That(State(Flow(game)),Is.EqualTo("Lobby"));Assert.That(Get(Flow(game),"QuitRequested"),Is.False); }
    [Test] public void StageListUsesActualAuthoredCatalog()
    { var game=Bootstrap();var view=Bound(game);Assert.That(Get(view,"StageRowCount"),Is.EqualTo(Get(Flow(game),"StageCount"))); }
    [Test] public void StageTwoHasLockIconAndNoInputBeforeUnlock()
    { var game=Playing();Command(Flow(game),"TryRequestPause");Command(Flow(game),"TryConfirmPause");Command(Flow(game),"TryContinue");var view=Bound(game);var row=Items(Field(view,"rows"))[1];Assert.That(Get(row,"IsUnlocked"),Is.False);Assert.That(((Button)Field(row,"selectButton")).interactable,Is.False);Assert.That(((Image)Field(row,"selectIcon")).sprite,Is.SameAs(Field(row,"lockIcon")));Assert.That(Call(view,"TrySelectStage",2),Is.False); }
    [Test] public void StageListShowsSavedHighestRankStars()
    { var game=Cleared();Command(Flow(game),"TryReturnToStageSelect");var view=Bound(game);var row=Items(Field(view,"rows"))[0];Assert.That(Get(row,"BestRank"),Is.EqualTo(3));Assert.That(Items(Field(row,"stars")).Cast<Image>().Select(x=>x.enabled),Is.EqualTo(new[]{true,true,true})); }
    [Test] public void StageIntroShowsStageAndAllThreeGoals()
    { var game=Bootstrap();Command(Flow(game),"TryPlay");var view=Bound(game);var stage=Get(Flow(game),"SelectedStage");Assert.That(Text(Field(view,"introTitle")),Does.Contain("Stage 1"));string text=Text(Field(view,"introDescription"));Assert.That(text,Does.Contain((string)Get(Get(stage,"RequiredGoal"),"Description")));foreach(var goal in Items(Get(stage,"AdditionalGoals")))Assert.That(text,Does.Contain((string)Get(goal,"Description"))); }
    [Test] public void IntroOkEnablesGameplayAndHidesModal()
    { var game=Bootstrap();Command(Flow(game),"TryPlay");var view=Bound(game);Assert.That(Get(Session(game),"GameplayEnabled"),Is.False);Assert.That(Ui(view,"TryDismissIntro"),Is.True);Assert.That(Get(Session(game),"GameplayEnabled"),Is.True);Assert.That(Active(view,"modalBlocker"),Is.False); }
    [Test] public void PauseNoRestoresBoardAndHistory()
    { var game=Playing();Build(game,1,0,0);Build(game,2,1,0);Command(Flow(game),"TryRequestPause");var view=Bound(game);Assert.That(Ui(view,"TryCancel"),Is.True);Assert.That(Get(Get(Session(game),"History"),"CanUndo"),Is.True);Assert.That(Get(Session(game),"GameplayEnabled"),Is.True); }
    [Test] public void PauseYesDiscardsCurrentTurnAndReturnsLobby()
    { var game=Playing();Build(game,1,0,0);Command(Flow(game),"TryRequestPause");var view=Bound(game);Assert.That(Ui(view,"TryConfirmPause"),Is.True);Assert.That(State(Flow(game)),Is.EqualTo("Lobby"));Assert.That(Get(Get(Session(game),"History"),"CanUndo"),Is.False); }
    [Test] public void ClearScreenUsesImmutableCompletedGoalsAndRank()
    { var game=Cleared();var view=Bound(game);var result=Get(Flow(game),"Result");Assert.That(Items(Field(view,"clearStars")).Cast<Image>().Count(x=>x.enabled),Is.EqualTo(Get(result,"Rank")));var rows=Items(Field(view,"clearGoals"));var flags=Items(Get(result,"GoalStates"));for(int i=0;i<3;i++)Assert.That(Get(rows[i],"IsAchieved"),Is.EqualTo(flags[i])); }
    [Test] public void RetryResetsBoardResourcesAndCardsWithIntro()
    { var game=Cleared();var view=Bound(game);Assert.That(Ui(view,"TryRetry"),Is.True);Assert.That(State(Flow(game)),Is.EqualTo("StageIntro"));Assert.That(Items(Get(Get(Session(game),"Hand"),"Cards")).Length,Is.EqualTo(2));Assert.That(Get(Get(Session(game),"History"),"CanUndo"),Is.False); }
    [Test] public void NextOpensUnlockedStageTwo()
    { var game=Cleared();var view=Bound(game);Assert.That(Button(view,"nextButton").interactable,Is.True);Assert.That(Ui(view,"TryNextStage"),Is.True);Assert.That(Get(Flow(game),"SelectedStageNumber"),Is.EqualTo(2));Assert.That(State(Flow(game)),Is.EqualTo("StageIntro")); }
    [Test] public void StageTwoNavigationFollowsActualStageCount()
    {
        var game=Cleared();Command(Flow(game),"TryNextStage");Command(Flow(game),"TryDismissIntro");
        Build(game,1,0,0);Build(game,5,0,1);Build(game,4,0,2);Build(game,3,1,0);Build(game,2,1,2);Command(Flow(game),"TryCompleteStage");
        var view=Bound(game);bool hasNext=(int)Get(Flow(game),"StageCount")>2;
        Assert.That(Button(view,"nextButton").interactable,Is.EqualTo(hasNext));Assert.That(Button(view,"clearSelectButton").interactable,Is.True);Assert.That(Ui(view,"TryNextStage"),Is.EqualTo(hasNext));
    }
    [Test] public void ClearStageSelectReturnsSavedRanks()
    { var game=Cleared();var view=Bound(game);Assert.That(Ui(view,"TryReturnToStageSelect"),Is.True);Assert.That(State(Flow(game)),Is.EqualTo("StageSelect"));Assert.That(Call(Flow(game),"GetBestRank",1),Is.EqualTo(3));Assert.That(Call(Flow(game),"IsStageUnlocked",2),Is.True); }
    [Test] public void RebindingScreensIgnoresPreviousFlowNotifications()
    { var first=Bootstrap();var second=Bootstrap();var view=Bound(first);Call(view,"Bind",Flow(second),null);Command(Flow(first),"TryPlay");Assert.That(Active(view,"lobby"),Is.True);Assert.That(Active(view,"intro"),Is.False); }
    [Test] public void ReenableScreensDoesNotDuplicateButtonListeners()
    { var game=Bootstrap();var view=Bound(game);Life(view,"OnDisable");Life(view,"OnEnable");Life(view,"OnEnable");Button(view,"playButton").onClick.Invoke();Assert.That(State(Flow(game)),Is.EqualTo("StageIntro"));Assert.That(Get(Flow(game),"LastError"),Is.Null); }
    [Test] public void DisabledScreenMethodsCannotRunCommands()
    { var game=Bootstrap();var view=Bound(game);view.gameObject.SetActive(false);Assert.That(Ui(view,"TryPlay"),Is.False);Assert.That(State(Flow(game)),Is.EqualTo("Lobby")); }
    [Test] public void SceneFailureShowsRetryAndBlocksStageInput()
    { var game=Bootstrap();var router=Router(game);Call(router,"NotifySceneReady",Scene("Lobby"));var view=Bound(game,router);available=false;Ui(view,"TryPlay");Assert.That(Active(view,"loadFailure"),Is.True);Assert.That(Button(view,"loadRetryButton").interactable,Is.True);Assert.That(Ui(view,"TryDismissIntro"),Is.False);available=true;Assert.That(Ui(view,"TryRetryLoad"),Is.True); }
    [Test] public void SaveFailureBlocksClearNavigationUntilRetrySucceeds()
    {
        var game=Playing();string path=(string)Get(Get(game,"Saves"),"FilePath");File.Delete(path);Directory.CreateDirectory(path);
        Build(game,1,0,0);Build(game,2,1,0);Command(Flow(game),"TryCompleteStage");var view=Bound(game);
        Assert.That(Button(view,"retryButton").interactable,Is.False);Assert.That(Button(view,"nextButton").interactable,Is.False);
        Assert.That(Button(view,"saveRetryButton").gameObject.activeSelf,Is.True);Assert.That(Ui(view,"TryReturnToStageSelect"),Is.False);
        Directory.Delete(path);Assert.That(Ui(view,"TryRetrySave"),Is.True);Assert.That(Button(view,"retryButton").interactable,Is.True);Assert.That(Button(view,"nextButton").interactable,Is.True);
    }
    [Test] public void FactoryWiresAllPanelsButtonsAndThreeRankSlots()
    { var view=Screens();foreach(string field in new[]{"lobbyBackground","lobby","modalBlocker","newGame","exit","stageSelect","intro","pause","clear","loadFailure","transition","stageRowPrefab","stageRowContent","introTitle","introDescription","clearTitle","saveRetryButton"})Assert.That(Field(view,field),Is.Not.Null,field);Assert.That(Items(Field(view,"clearStars")).Length,Is.EqualTo(3));Assert.That(Items(Field(view,"clearGoals")).Length,Is.EqualTo(3)); }
    [Test] public void ModalCanvasRendersAboveHudAndBlocksPointer()
    { var view=Screens();Assert.That(view.GetComponent<Canvas>().sortingOrder,Is.EqualTo(100));Assert.That(view.GetComponent<GraphicRaycaster>(),Is.Not.Null);Assert.That(((GameObject)Field(view,"modalBlocker")).GetComponent<Image>().raycastTarget,Is.True);Assert.That(view.GetComponent<CanvasScaler>().referenceResolution,Is.EqualTo(new Vector2(1920,1080))); }
    [Test] public void BuildSceneListPrependsLobbyGameAndPreservesExistingEntries()
    { var old=new[]{new EditorBuildSettingsScene("Assets/Prototype.unity",true),new EditorBuildSettingsScene("Assets/Other.unity",false)};var result=(EditorBuildSettingsScene[])Editor("UrbanEquationScreensSetup").GetMethod("BuildScenes").Invoke(null,new object[]{old});Assert.That(result.Select(x=>x.path),Is.EqualTo(new[]{"Assets/_UrbanEquation/Scenes/Lobby.unity","Assets/_UrbanEquation/Scenes/Game.unity","Assets/Prototype.unity","Assets/Other.unity"}));Assert.That(result[3].enabled,Is.False); }
    [Test] public void BuildSceneListIsIdempotentWithoutDuplicateDestinations()
    { var method=Editor("UrbanEquationScreensSetup").GetMethod("BuildScenes");var first=(EditorBuildSettingsScene[])method.Invoke(null,new object[]{new EditorBuildSettingsScene[0]});var second=(EditorBuildSettingsScene[])method.Invoke(null,new object[]{first});Assert.That(second.Select(x=>x.path),Is.EqualTo(first.Select(x=>x.path)));Assert.That(second.All(x=>x.enabled),Is.True); }
    [Test] public void HudBackdropIsOptInAndPauseStillBlocksGameplay()
    {
        var prefab=(Component)Get(Asset("Assets/_UrbanEquation/Data/Presentation/GameplayHud.asset","GameplayHudSet"),"HudPrefab");
        var hudRoot=Own(UObject.Instantiate(prefab.gameObject));var hud=hudRoot.GetComponent(Runtime("GameHudUI"));var game=Bootstrap();Command(Flow(game),"TryPlay");Call(hud,"Bind",Session(game),Flow(game),null,null);
        Assert.That(Get(hud,"GameplayVisible"),Is.False);Call(hud,"SetScreenBackdropVisible",true);Assert.That(Get(hud,"GameplayVisible"),Is.True);
        Command(Flow(game),"TryDismissIntro");Command(Flow(game),"TryRequestPause");Assert.That(Get(hud,"GameplayVisible"),Is.True);Assert.That(Get(Session(game),"GameplayEnabled"),Is.False);
        Assert.That(Field(prefab,"screenBackdropVisible"),Is.False);
    }
}
