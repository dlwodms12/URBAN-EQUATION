using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UObject = UnityEngine.Object;

// Loads the shipped assets rather than constructing a parallel set of design definitions.
public class StageIntegrationContractTests
{
    private readonly List<GameObject> roots = new List<GameObject>();
    private readonly List<ScriptableObject> clones = new List<ScriptableObject>();
    private string directory;
    private string SavePath => Path.Combine(directory, "progress.json");
    private static Type TypeOf(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Get(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
    private static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    private static object Call(object target, string name, params object[] args) =>
        target.GetType().GetMethod(name).Invoke(target, args);
    private static object[] Items(object value) => ((IEnumerable)value).Cast<object>().ToArray();
    private static ScriptableObject Content => (ScriptableObject)AssetDatabase.LoadAssetAtPath(
        "Assets/_UrbanEquation/Data/GameContent.asset", TypeOf("GameContentData"));
    private static object StageData(int number) => Items(Get(Get(Content, "Stages"), "Stages"))[number - 1];
    private static object Resource(int number) => Enum.ToObject(TypeOf("ResourceType"), number);
    private static int[] Amounts(object rows)
    {
        var values = new int[5];
        foreach (object row in Items(rows)) values[Convert.ToInt32(Get(row, "Resource"))] = (int)Get(row, "Amount");
        return values;
    }
    private static int[] Resources(object session) => Enumerable.Range(0, 5)
        .Select(i => (int)Call(Get(session, "Resources"), "GetResource", Resource(i))).ToArray();
    private static void Command(object target, string method)
    {
        object[] args = { null };
        Assert.That(Call(target, method, args), Is.True, args[0] as string);
    }
    private Component Bootstrap(ScriptableObject content = null, string path = null, bool debug = false)
    {
        var root = new GameObject("Stage integration fixture");
        roots.Add(root);
        Component bootstrap = root.AddComponent(TypeOf("GameBootstrap"));
        object[] args = { content ?? Content, path ?? SavePath, debug, null };
        Assert.That(Call(bootstrap, "TryInitialize", args), Is.True, args[3] as string);
        return bootstrap;
    }
    private Component EmptyBootstrap()
    {
        var root = new GameObject("Empty bootstrap"); roots.Add(root);
        return root.AddComponent(TypeOf("GameBootstrap"));
    }
    private static object Session(object bootstrap) => Get(bootstrap, "Session");
    private static object Flow(object bootstrap) => Get(bootstrap, "Flow");
    private static void Build(object session, int card, int x, int y)
    {
        object[] args = { card, new Vector2Int(x, y), null, null };
        Assert.That(Call(session, "TryCommitBuild", args), Is.True, args[3] as string);
        Assert.That(args[2], Is.Not.Null);
    }
    private static object StartStageOne(object bootstrap)
    {
        Command(Flow(bootstrap), "TryPlay");
        Assert.That(Get(Flow(bootstrap), "State").ToString(), Is.EqualTo("StageIntro"));
        Command(Flow(bootstrap), "TryDismissIntro");
        return Session(bootstrap);
    }
    private static object StartStageTwo(object bootstrap)
    {
        object session = StartStageOne(bootstrap);
        Build(session, 1, 0, 0); Build(session, 2, 1, 0);
        Command(Flow(bootstrap), "TryCompleteStage");
        Command(Flow(bootstrap), "TryNextStage");
        Command(Flow(bootstrap), "TryDismissIntro");
        return session;
    }
    private static void StageTwoSolution(object session, int order, int rank, int turns = 5)
    {
        int[] cards = order == 0 ? new[] { 1, 5, 4, 3, 2 } : new[] { 3, 2, 1, 5, 4 };
        Vector2Int[] positions;
        if (order == 0)
            positions = new[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2),
                rank == 1 ? new Vector2Int(1,1) : new Vector2Int(1,0),
                rank == 1 ? new Vector2Int(1,0) : rank == 2 ? new Vector2Int(1,1) : new Vector2Int(1,2) };
        else
            positions = new[] { new Vector2Int(0,0), new Vector2Int(0,1),
                rank == 3 ? new Vector2Int(1,0) : new Vector2Int(0,2),
                rank == 3 ? new Vector2Int(0,2) : new Vector2Int(1,0),
                rank == 1 ? new Vector2Int(1,2) : new Vector2Int(1,1) };
        for (int i = 0; i < turns; i++) Build(session, cards[i], positions[i].x, positions[i].y);
    }
    private ScriptableObject Clone(ScriptableObject original)
    { var clone = UObject.Instantiate(original); clones.Add(clone); return clone; }
    [SetUp] public void SetUp() => directory = Path.Combine(Path.GetTempPath(), "UE-StageIntegration-" + Guid.NewGuid().ToString("N"));
    [TearDown] public void TearDown()
    {
        for (int i = roots.Count - 1; i >= 0; i--) if (roots[i] != null) UObject.DestroyImmediate(roots[i]);
        foreach (ScriptableObject clone in clones) if (clone != null) UObject.DestroyImmediate(clone);
        roots.Clear(); clones.Clear();
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }

    [TestCase(11001,"0,1,0,0,0","1,0,0,0,0","0,1,2")]
    [TestCase(12001,"0,2,0,0,0","2,0,0,0,0","1,2")]
    [TestCase(13001,"0,3,0,0,1","3,0,1,0,0","2")]
    [TestCase(21001,"0,0,1,0,0","0,1,0,0,0","0,1,2")]
    [TestCase(22001,"0,0,2,0,0","0,2,0,0,0","0,1")]
    [TestCase(23001,"0,0,3,1,0","1,3,0,0,0","0")]
    [TestCase(31001,"0,0,0,1,0","0,0,1,0,0","0,1,2")]
    [TestCase(32001,"0,0,0,2,0","0,0,2,0,0","1,2")]
    [TestCase(33001,"1,0,0,3,0","0,0,3,0,1","0,1")]
    [TestCase(41001,"0,0,0,0,1","0,0,0,1,0","0,1,2")]
    [TestCase(42001,"0,0,0,0,2","0,0,0,2,0","0,1")]
    [TestCase(43001,"0,0,1,0,3","0,1,0,3,0","1")]
    [TestCase(51001,"1,0,0,0,0","0,0,0,0,1","1")]
    [TestCase(52001,"2,0,0,0,0","0,0,0,0,2","2")]
    [TestCase(53001,"3,1,0,0,0","0,0,0,1,3","0")]
    public void ShippedBuildingMatchesDocument(int code, string costs, string gains, string tiles)
    {
        object[] entries = Items(Get(Get(Content, "Buildings"), "Buildings"));
        Assert.That(entries.Length, Is.EqualTo(15));
        object building = entries.Single(x => (int)Get(x, "BuildingCode") == code);
        Assert.That(Get(building, "UsesResourceLists"), Is.True);
        Assert.That(string.Join(",", Amounts(Get(building, "RequiredResources"))), Is.EqualTo(costs));
        Assert.That(string.Join(",", Amounts(Get(building, "GainedResources"))), Is.EqualTo(gains));
        Assert.That(string.Join(",", Items(Get(building, "AllowedTileTypes")).Select(x=>Convert.ToInt32(x)).OrderBy(x=>x)), Is.EqualTo(tiles));
    }

    [TestCase(1,2,"Grass","0,2,0,0,0",2)]
    [TestCase(2,3,"Concrete","0,1,0,1,0",5)]
    public void ShippedStageMatchesDocument(int number, int width, string tile, string resources, int cards)
    {
        object stage = StageData(number);
        Assert.That(Get(stage,"StageNumber"), Is.EqualTo(number));
        Assert.That(Get(stage,"Width"), Is.EqualTo(width)); Assert.That(Get(stage,"Height"), Is.EqualTo(width));
        Assert.That(Items(Get(stage,"Tiles")).Length, Is.EqualTo(width*width));
        Assert.That(Items(Get(stage,"Tiles")).All(x=>Get(x,"TileType").ToString()==tile && Get(x,"TilePrefab") != null), Is.True);
        Assert.That(string.Join(",",Amounts(Get(stage,"InitialResources"))), Is.EqualTo(resources));
        Assert.That(Items(Get(stage,"BuildingCards")).Sum(x=>(int)Get(x,"Count")), Is.EqualTo(cards));
        Assert.That(Items(Get(stage,"AdditionalGoals")).Length, Is.EqualTo(2));
    }

    [Test] public void ContentAndEveryComboReferenceValidate()
    {
        Assert.That(Content, Is.Not.Null);
        var errors = new List<string>(); Call(Content,"Validate",errors);
        Assert.That(errors, Is.Empty);
        Assert.That(Get(Get(Content,"Stages"),"Count"), Is.GreaterThanOrEqualTo(2));
        Assert.That(Items(Get(Get(Content,"Combos"),"Combos")).Length, Is.EqualTo(38));
    }
    [Test] public void StageOneGoalTargetsArePopulationOneTwoAndThree()
    {
        object stage=StageData(1);
        var goals=new[]{Get(stage,"RequiredGoal")}.Concat(Items(Get(stage,"AdditionalGoals"))).ToArray();
        Assert.That(goals.Select(x=>Get(x,"GoalType").ToString()), Is.All.EqualTo("ResourceAtLeast"));
        Assert.That(goals.Select(x=>Convert.ToInt32(Get(Get(x,"ResourceTarget"),"Resource"))), Is.EqualTo(new[]{0,0,0}));
        Assert.That(goals.Select(x=>(int)Get(Get(x,"ResourceTarget"),"Amount")), Is.EqualTo(new[]{1,2,3}));
    }
    [Test] public void StageTwoGoalsAndCardOrderUseCanonicalDefinitions()
    {
        object stage=StageData(2);
        Assert.That(Get(Get(stage,"RequiredGoal"),"GoalType").ToString(), Is.EqualTo("AllCardsUsed"));
        object[] goals=Items(Get(stage,"AdditionalGoals"));
        Assert.That(goals.Select(x=>Get(x,"GoalType").ToString()), Is.All.EqualTo("AdjacentBuildings"));
        Assert.That(goals.Select(x=>(int)Get(Get(x,"BuildingA"),"BuildingCode")), Is.EqualTo(new[]{11001,41001}));
        Assert.That(goals.Select(x=>(int)Get(Get(x,"BuildingB"),"BuildingCode")), Is.EqualTo(new[]{31001,21001}));
        Assert.That(Items(Get(stage,"BuildingCards")).Select(x=>(int)Get(Get(x,"Building"),"BuildingCode")),
            Is.EqualTo(new[]{11001,21001,31001,41001,51001}));
    }
    [Test] public void BootstrapAdditionHasNoSessionOrDiskSideEffects()
    {
        var bootstrap = EmptyBootstrap();
        Assert.That(Get(bootstrap,"IsInitialized"), Is.False);
        Assert.That(bootstrap.transform.childCount, Is.Zero);
        Assert.That(Directory.Exists(directory), Is.False);
    }
    [Test] public void StartupOpensLobbyWithoutCreatingSaveOrEnablingInput()
    {
        var bootstrap = Bootstrap();
        Assert.That(Get(Flow(bootstrap),"State").ToString(), Is.EqualTo("Lobby"));
        Assert.That(Get(Session(bootstrap),"GameplayEnabled"), Is.False);
        Assert.That(Get(Get(Session(bootstrap),"Stage"),"GameplayEnabled"), Is.False);
        Assert.That(File.Exists(SavePath), Is.False);
    }
    [Test] public void MissingContentIsRejectedBeforeCreatingObjects()
    {
        var bootstrap=EmptyBootstrap(); object[] args={null,SavePath,false,null};
        Assert.That(Call(bootstrap,"TryInitialize",args), Is.False);
        Assert.That(bootstrap.transform.childCount, Is.Zero);
    }
    [Test] public void EmptySavePathIsRejectedBeforeCreatingObjects()
    {
        var bootstrap=EmptyBootstrap(); object[] args={Content,"",false,null};
        Assert.That(Call(bootstrap,"TryInitialize",args), Is.False);
        Assert.That(bootstrap.transform.childCount, Is.Zero);
    }
    [Test] public void InvalidContentDoesNotTouchExistingProgress()
    {
        Directory.CreateDirectory(directory);File.WriteAllText(SavePath,"untouched");
        var invalid=Clone(Content);Set(invalid,"buildingPrefab",null);
        var bootstrap=EmptyBootstrap();object[] args={invalid,SavePath,false,null};
        Assert.That(Call(bootstrap,"TryInitialize",args), Is.False);
        Assert.That(File.ReadAllText(SavePath), Is.EqualTo("untouched"));
        Assert.That(bootstrap.transform.childCount, Is.Zero);
    }
    [Test] public void InactiveBootstrapRejectsInitialization()
    {
        var bootstrap=EmptyBootstrap();bootstrap.gameObject.SetActive(false);
        object[] args={Content,SavePath,false,null};Assert.That(Call(bootstrap,"TryInitialize",args), Is.False);
    }
    [Test] public void SecondInitializationPreservesExistingSession()
    {
        var bootstrap=Bootstrap();object session=Session(bootstrap);
        object[] args={Content,SavePath,true,null};Assert.That(Call(bootstrap,"TryInitialize",args), Is.False);
        Assert.That(Session(bootstrap), Is.SameAs(session));Assert.That(bootstrap.transform.childCount, Is.EqualTo(1));
    }
    [Test] public void InvalidStoragePathCleansUpCandidateAndAllowsRetry()
    {
        var bootstrap=EmptyBootstrap();object[] args={Content,"invalid\0path",false,null};
        Assert.That(Call(bootstrap,"TryInitialize",args), Is.False);
        Assert.That(Get(bootstrap,"IsInitialized"), Is.False);Assert.That(bootstrap.transform.childCount, Is.Zero);
        args=new object[]{Content,SavePath,false,null};Assert.That(Call(bootstrap,"TryInitialize",args), Is.True,args[3] as string);
    }
    [Test] public void DebugUiIsExplicitlyOptional()
    {
        var plain=Bootstrap();var debug=Bootstrap(path:Path.Combine(directory,"debug.json"),debug:true);
        Assert.That(plain.GetComponentInChildren(TypeOf("GameFlowDebugUI")), Is.Null);
        Assert.That(debug.GetComponentInChildren(TypeOf("GameFlowDebugUI")), Is.Not.Null);
    }
    [Test] public void IntroBlocksStageInputBeforeFirstConstruction()
    {
        var bootstrap=Bootstrap();Command(Flow(bootstrap),"TryPlay");object session=Session(bootstrap);
        Assert.That(Get(Get(session,"Stage"),"GameplayEnabled"), Is.False);
        object[] args={1,new Vector2Int(0,0),null,null};Assert.That(Call(session,"TryCommitBuild",args), Is.False);
        object[] complete={null,null};Assert.That(Call(Get(session,"Stage"),"TryCompleteStage",complete), Is.False);
        Command(Flow(bootstrap),"TryDismissIntro");Assert.That(Get(Get(session,"Stage"),"GameplayEnabled"), Is.True);
    }
    [Test] public void IntroBlocksDirectCompletionEvenWhenInitialGoalAlreadyMet()
    {
        var content=Clone(Content);var catalog=Clone((ScriptableObject)Get(content,"Stages"));
        var first=Clone((ScriptableObject)StageData(1));
        Array amounts=Array.CreateInstance(TypeOf("ResourceAmount"),5);
        int[] initial={1,2,0,0,0};
        for(int i=0;i<5;i++)amounts.SetValue(Activator.CreateInstance(TypeOf("ResourceAmount"),Resource(i),initial[i]),i);
        Set(first,"initialResources",amounts);
        Array stages=Array.CreateInstance(TypeOf("StageData"),2);stages.SetValue(first,0);stages.SetValue(StageData(2),1);
        Set(catalog,"stages",stages);Set(content,"stageCatalog",catalog);
        var bootstrap=Bootstrap(content);Command(Flow(bootstrap),"TryPlay");
        object stage=Get(Session(bootstrap),"Stage");Assert.That(Get(stage,"NextStageAvailable"), Is.True);
        object[] args={null,null};Assert.That(Call(stage,"TryCompleteStage",args), Is.False);
        Assert.That(Get(stage,"IsCleared"), Is.False);
        Command(Flow(bootstrap),"TryDismissIntro");Command(Flow(bootstrap),"TryCompleteStage");
        Assert.That(Get(stage,"IsCleared"), Is.True);
    }
    [TestCase(1)] [TestCase(2)] [TestCase(3)]
    public void StageOneRanksUseActualResourcesAndOrthogonalCombo(int rank)
    {
        var bootstrap=Bootstrap();object session=StartStageOne(bootstrap);Build(session,1,0,0);
        if(rank>1)Build(session,2,1,rank==2 ? 1 : 0);
        Assert.That(Resources(session), Is.EqualTo(new[]{rank,rank==1?1:0,0,0,0}));
        Assert.That(Get(Get(session,"Stage"),"Rank"), Is.EqualTo(rank));
        Assert.That(Items(Get(Get(session,"Combos"),"Results")).Length, Is.EqualTo(rank==3?1:0));
        Assert.That(Get(Get(session,"Stage"),"IsCleared"), Is.False);
        Command(Flow(bootstrap),"TryCompleteStage");
        Assert.That(Get(Get(Flow(bootstrap),"Result"),"Rank"), Is.EqualTo(rank));
        Assert.That(Call(Flow(bootstrap),"GetBestRank",1), Is.EqualTo(rank));
        Assert.That(Call(Flow(bootstrap),"IsStageUnlocked",2), Is.True);
    }
    [Test] public void StageOneUndoRestoresComboCardResourcesAndGoalRank()
    {
        var bootstrap=Bootstrap();object session=StartStageOne(bootstrap);
        Build(session,1,0,0);Build(session,2,1,0);Command(Get(session,"History"),"TryUndo");
        Assert.That(Resources(session), Is.EqualTo(new[]{1,1,0,0,0}));
        Assert.That(Items(Get(Get(session,"Hand"),"Cards")).Select(x=>(int)Get(x,"CardId")), Is.EqualTo(new[]{2}));
        Assert.That(Items(Get(Get(session,"Combos"),"Results")), Is.Empty);
        Assert.That(Get(Get(session,"Stage"),"Rank"), Is.EqualTo(1));
        Assert.That(Get(Get(session,"History"),"CanUndo"), Is.False);
        Build(session,2,1,1);Assert.That(Get(Get(session,"Stage"),"Rank"), Is.EqualTo(2));
    }
    [TestCase(0,1)] [TestCase(0,2)] [TestCase(0,3)]
    [TestCase(1,1)] [TestCase(1,2)] [TestCase(1,3)]
    public void StageTwoDocumentOrdersReachAllRanks(int order,int rank)
    {
        var bootstrap=Bootstrap();object session=StartStageTwo(bootstrap);StageTwoSolution(session,order,rank);
        Assert.That(Resources(session), Is.EqualTo(new[]{0,1,0,1,0}));
        Assert.That(Items(Get(Get(session,"Hand"),"Cards")), Is.Empty);
        Assert.That(Get(Get(session,"Stage"),"Rank"), Is.EqualTo(rank));
        Command(Flow(bootstrap),"TryCompleteStage");
        Assert.That(Get(Get(Flow(bootstrap),"Result"),"Rank"), Is.EqualTo(rank));
        Assert.That(Get(Flow(bootstrap),"CanNextStage"), Is.EqualTo((int)Get(Flow(bootstrap),"StageCount")>2));
    }
    [Test] public void StageTwoInitialAvailabilityMatchesResourceCosts()
    {
        var bootstrap=Bootstrap();object session=StartStageTwo(bootstrap);object hand=Get(session,"Hand");
        Assert.That(Enumerable.Range(1,5).Select(id=>(bool)Call(hand,"IsCardAvailable",id)), Is.EqualTo(new[]{true,false,true,false,false}));
        object[] args={2,new Vector2Int(0,0),null,null};Assert.That(Call(session,"TryCommitBuild",args), Is.False);
        Assert.That(Resources(session), Is.EqualTo(new[]{0,1,0,1,0}));
        Assert.That(Get(Get(session,"History"),"Count"), Is.Zero);
    }
    [Test] public void StageTwoMandatoryGoalWaitsForLastCard()
    {
        var bootstrap=Bootstrap();object session=StartStageTwo(bootstrap);StageTwoSolution(session,0,3,4);
        Assert.That(Get(Get(session,"Stage"),"NextStageAvailable"), Is.False);
        object[] args={null};Assert.That(Call(Flow(bootstrap),"TryCompleteStage",args), Is.False);
        Build(session,2,1,2);Assert.That(Get(Get(session,"Stage"),"NextStageAvailable"), Is.True);
    }
    [Test] public void StageTwoUndoReopensMandatoryGoalAndRestoresOfficeCard()
    {
        var bootstrap=Bootstrap();object session=StartStageTwo(bootstrap);StageTwoSolution(session,0,3);
        Command(Get(session,"History"),"TryUndo");
        Assert.That(Get(Get(session,"Stage"),"NextStageAvailable"), Is.False);
        Assert.That(Resources(session), Is.EqualTo(new[]{0,0,1,1,0}));
        Assert.That(Items(Get(Get(session,"Hand"),"Cards")).Select(x=>(int)Get(x,"CardId")), Is.EqualTo(new[]{2}));
        Build(session,2,1,2);Assert.That(Get(Get(session,"Stage"),"Rank"), Is.EqualTo(3));
    }
    [Test] public void RetryRecreatesStageAndClearsTurnAndComboState()
    {
        var bootstrap=Bootstrap();object session=StartStageOne(bootstrap);Build(session,1,0,0);Build(session,2,1,0);
        Command(Flow(bootstrap),"TryCompleteStage");Command(Flow(bootstrap),"TryRetry");
        Assert.That(Resources(session), Is.EqualTo(new[]{0,2,0,0,0}));
        Assert.That(Get(Get(session,"History"),"Count"), Is.Zero);
        Assert.That(Items(Get(Get(session,"Combos"),"Results")), Is.Empty);
        Assert.That(Items(Get(Get(session,"Hand"),"Cards")).Length, Is.EqualTo(2));
        Assert.That(Get(Get(session,"Stage"),"GameplayEnabled"), Is.False);
        Assert.That(Get(Get(session,"Stage"),"Rank"), Is.Zero);
    }
    [Test] public void ContinueLoadsRanksButStartsSelectedStageWithFreshRuntime()
    {
        var first=Bootstrap();object session=StartStageTwo(first);StageTwoSolution(session,0,3);Command(Flow(first),"TryCompleteStage");
        var second=Bootstrap();Assert.That(Get(Flow(second),"CanContinue"), Is.True);
        Command(Flow(second),"TryContinue");object[] args={2,null};Assert.That(Call(Flow(second),"TrySelectStage",args), Is.True,args[1] as string);
        Assert.That(Call(Flow(second),"GetBestRank",2), Is.EqualTo(3));
        Assert.That(Resources(Session(second)), Is.EqualTo(new[]{0,1,0,1,0}));
        Assert.That(Items(Get(Get(Session(second),"Hand"),"Cards")).Length, Is.EqualTo(5));
        Assert.That(Get(Get(Session(second),"History"),"Count"), Is.Zero);
    }
    [Test] public void ConfirmedNewGameResetsTwoStageRanksAndUnlocks()
    {
        var first=Bootstrap();object session=StartStageTwo(first);StageTwoSolution(session,0,3);Command(Flow(first),"TryCompleteStage");
        var second=Bootstrap();Command(Flow(second),"TryPlay");
        Assert.That(Get(Flow(second),"State").ToString(), Is.EqualTo("NewGameConfirmation"));
        Command(Flow(second),"TryConfirmNewGame");
        Assert.That(Call(Flow(second),"GetBestRank",1), Is.Zero);
        Assert.That(Call(Flow(second),"IsStageUnlocked",2), Is.False);
        Assert.That(Resources(Session(second)), Is.EqualTo(new[]{0,2,0,0,0}));
    }
    [Test] public void PauseCancelPreservesActualStageAndConfirmDiscardsIt()
    {
        var bootstrap=Bootstrap();object session=StartStageOne(bootstrap);Build(session,1,0,0);
        Command(Flow(bootstrap),"TryRequestPause");Command(Flow(bootstrap),"TryCancel");
        Assert.That(Resources(session), Is.EqualTo(new[]{1,1,0,0,0}));
        Command(Flow(bootstrap),"TryRequestPause");Command(Flow(bootstrap),"TryConfirmPause");
        Assert.That(Get(Flow(bootstrap),"State").ToString(), Is.EqualTo("Lobby"));
        Assert.That(Resources(session), Is.EqualTo(new[]{0,2,0,0,0}));
        Assert.That(Get(Get(session,"History"),"Count"), Is.Zero);
    }
}
