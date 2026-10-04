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

// Real stage assets and real HUD; every save belongs to a disposable test directory.
public class TutorialStageContractTests
{
    private readonly List<UObject> owned = new List<UObject>();
    private string directory;
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Get(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
    private static object Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name).Invoke(target, args);
    private static object[] Items(object value) => ((IEnumerable)value).Cast<object>().ToArray();
    private T Own<T>(T value) where T : UObject { owned.Add(value); return value; }
    private static ScriptableObject Content => (ScriptableObject)AssetDatabase.LoadAssetAtPath("Assets/_UrbanEquation/Data/GameContent.asset", Runtime("GameContentData"));
    private static object Data(int number) => Items(Get(Get(Content, "Stages"), "Stages"))[number - 1];
    private static object Session(Component game) => Get(game, "Session");
    private static object Stage(Component game) => Get(Session(game), "Stage");
    private static object Combos(Component game) => Get(Session(game), "Combos");
    private static object Flow(Component game) => Get(game, "Flow");
    private static object Resource(int value) => Enum.ToObject(Runtime("ResourceType"), value);
    private static int[] Resources(Component game) => Enumerable.Range(0, 5).Select(i => (int)Call(Get(Session(game), "Resources"), "GetResource", Resource(i))).ToArray();
    private static object[] Results(Component game) => Items(Get(Combos(game), "Results"));
    private static int Count(Component game, bool complaint) => Results(game).Count(r => (bool)Get(r, "IsComplaint") == complaint);
    private static bool[] Goals(Component game) => Items(Get(Stage(game), "GoalStates")).Cast<bool>().ToArray();
    private static void Command(object target, string name)
    { object[] args = { null }; Assert.That(Call(target, name, args), Is.True, args[0] as string); }
    private Component Start(int number, ScriptableObject content = null)
    {
        string path = Path.Combine(directory, "progress-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, "{\"version\":1,\"stageCount\":5,\"highestUnlockedStage\":5,\"bestRanks\":[3,3,3,3,0]}");
        var game = Own(new GameObject("Tutorial fixture")).AddComponent(Runtime("GameBootstrap"));
        object[] args = { content ?? Content, path, false, null };
        Assert.That(Call(game, "TryInitialize", args), Is.True, args[3] as string);
        Command(Flow(game), "TryContinue");
        args = new object[] { number, null };
        Assert.That(Call(Flow(game), "TrySelectStage", args), Is.True, args[1] as string);
        Command(Flow(game), "TryDismissIntro"); return game;
    }
    private static void Build(Component game, int code, int x, int y)
    {
        object card = Items(Get(Get(Session(game), "Hand"), "Cards")).First(c => (int)Get(Get(c, "Building"), "BuildingCode") == code);
        object[] args = { Get(card, "CardId"), new Vector2Int(x, y), null, null };
        Assert.That(Call(Session(game), "TryCommitBuild", args), Is.True, args[3] as string);
    }
    private Component Hud(Component game)
    {
        var data = AssetDatabase.LoadAssetAtPath("Assets/_UrbanEquation/Data/Presentation/GameplayHud.asset", Runtime("GameplayHudSet"));
        var prefab = (Component)Get(data, "HudPrefab");
        var hud = Own(UObject.Instantiate(prefab.gameObject)).GetComponent(Runtime("GameHudUI"));
        Call(hud, "Bind", Session(game), Flow(game), null, null); return hud;
    }
    private static bool Review(Component hud, Component game, object result)
    {
        object board = Get(Session(game), "Board"), details = Field(hud, "boardDetails");
        object a = Get(Call(board, "GetTile", Get(result, "SourceCoordinate")), "Building");
        object b = Get(Call(board, "GetTile", Get(result, "AdjacentCoordinate")), "Building");
        Call(details, "ClearSelection"); Call(details, "SelectBuilding", a);
        return (bool)Call(details, "SelectBuilding", b);
    }
    private static void StageThreeSolution(Component game)
    { Build(game, 43001, 1, 1); Build(game, 23001, 1, 2); Build(game, 13001, 1, 0); }
    private static void StageFourSolution(Component game, int turns = 5)
    {
        int[] code = { 32001, 52001, 22001, 12001, 42001 };
        var coordinates = new[] { new Vector2Int(1,1), new Vector2Int(1,0), new Vector2Int(1,2), new Vector2Int(0,1), new Vector2Int(2,2) };
        for (int i = 0; i < turns; i++) Build(game, code[i], coordinates[i].x, coordinates[i].y);
    }
    private static void StageFiveSolution(Component game, int turns = 5)
    {
        int[] code = { 12001, 22001, 42001, 52001, 32001 };
        var coordinates = new[] { new Vector2Int(1,1), new Vector2Int(1,2), new Vector2Int(0,1), new Vector2Int(0,0), new Vector2Int(1,0) };
        for (int i = 0; i < turns; i++) Build(game, code[i], coordinates[i].x, coordinates[i].y);
    }
    [SetUp] public void SetUp() { directory = Path.Combine(Path.GetTempPath(), "UE-Tutorial-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); }
    [TearDown] public void TearDown()
    { for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) UObject.DestroyImmediate(owned[i]); owned.Clear(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }

    [TestCase(3, 9, "0,3,4,1,4", "13001,23001,43001,11001,21001,41001")]
    [TestCase(4, 5, "5,5,5,5,5", "12001,22001,32001,42001,52001")]
    [TestCase(5, 5, "10,10,10,10,10", "12001,22001,32001,42001,52001")]
    public void AuthoredStagesHaveExactCardsResourcesAndOrder(int number, int total, string resources, string codes)
    {
        var game = Start(number);
        Assert.That(string.Join(",", Resources(game)), Is.EqualTo(resources));
        Assert.That(Items(Get(Get(Session(game), "Hand"), "Cards")).Length, Is.EqualTo(total));
        Assert.That(string.Join(",", Items(Get(Data(number), "BuildingCards")).Select(c => Get(Get(c, "Building"), "BuildingCode"))), Is.EqualTo(codes));
        Assert.That(Get(Get(Content, "Stages"), "Count"), Is.EqualTo(5));
    }
    [TestCase(3)] [TestCase(4)] [TestCase(5)]
    public void MixedBoardsUseNorthAsphaltMiddleConcreteSouthGrass(int number)
    {
        var game = Start(number); object board = Get(Session(game), "Board");
        for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++)
            Assert.That(Get(Get(Call(board, "GetTile", new Vector2Int(x,y)), "Data"), "TileType").ToString(), Is.EqualTo(new[] { "Grass", "Concrete", "Asphalt" }[y]));
    }
    [TestCase(12001,22001,11101,0,-1,true)]
    [TestCase(32001,42001,31101,2,-1,true)]
    [TestCase(12001,42001,41101,3,-1,true)]
    [TestCase(42001,52001,51101,4,-1,true)]
    [TestCase(12001,32001,12001,0,2,false)]
    [TestCase(22001,32001,22001,1,2,false)]
    [TestCase(22001,42001,32001,2,2,false)]
    [TestCase(12001,52001,52001,4,2,false)]
    public void NewInteractionsUseExactUnorderedPairsAndSignedResources(int a, int b, int code, int resource, int amount, bool complaint)
    {
        object database = Get(Content, "Combos");
        foreach (var pair in new[] { new[] { a,b }, new[] { b,a } })
        {
            object[] args = { pair[0], pair[1], null }; Assert.That(Call(database, "TryGetCombo", args), Is.True);
            Assert.That(Get(args[2], "ComboCode"), Is.EqualTo(code)); Assert.That(Get(args[2], "IsComplaint"), Is.EqualTo(complaint));
            object reward = Items(Get(args[2], "Rewards")).Single();
            Assert.That(Convert.ToInt32(Get(reward, "Resource")), Is.EqualTo(resource)); Assert.That(Get(reward, "Amount"), Is.EqualTo(amount));
        }
    }
    [Test] public void StageThreeThreeBuildSolutionMeetsAllGoalsAndExactResources()
    { var game = Start(3); StageThreeSolution(game); Assert.That(Goals(game), Is.EqualTo(new[] { true,true,true })); Assert.That(Resources(game), Is.EqualTo(new[] {4,4,1,3,0})); Assert.That(Get(Stage(game), "Rank"), Is.EqualTo(3)); }
    [Test] public void StageThreeFourthBuildLosesOnlyTheCountGoalAndUndoRestoresIt()
    {
        var game = Start(3); StageThreeSolution(game); Build(game, 21001, 0, 0);
        Assert.That(Goals(game), Is.EqualTo(new[] { true,true,false })); Command(Get(Session(game), "History"), "TryUndo");
        Assert.That(Goals(game), Is.EqualTo(new[] { true,true,true }));
    }
    [Test] public void LargeBuildingsRejectWrongTileWithoutRemovingCardsOrResources()
    {
        var game = Start(3); int[] resources = Resources(game);
        foreach (var attempt in new[] { new[] { 13001,1,2 }, new[] { 23001,1,1 } })
        {
            object card = Items(Get(Get(Session(game), "Hand"), "Cards")).First(c => (int)Get(Get(c, "Building"), "BuildingCode") == attempt[0]);
            object[] args = { Get(card, "CardId"), new Vector2Int(attempt[1],attempt[2]), null, null };
            Assert.That(Call(Session(game), "TryCommitBuild", args), Is.False);
        }
        Assert.That(Resources(game), Is.EqualTo(resources)); Assert.That(Items(Get(Get(Session(game), "Hand"), "Cards")).Length, Is.EqualTo(9));
    }
    [Test] public void StageFourOriginalSolutionIncludesFactoryAtTwoTwoAndThreePositivePairs()
    {
        var game = Start(4); StageFourSolution(game); Assert.That(Count(game,false), Is.EqualTo(3)); Assert.That(Count(game,true), Is.Zero);
        Assert.That(Resources(game), Is.EqualTo(new[] {7,7,7,5,5})); Assert.That(Goals(game), Is.EqualTo(new[] {true,false,true}));
        Assert.That(Review(Hud(game), game, Results(game)[0]), Is.True); Assert.That(Get(Stage(game), "Rank"), Is.EqualTo(3));
    }
    [Test] public void StageFiveOriginalSolutionHasThreeComplaintsAndOnePositivePair()
    {
        var game = Start(5); StageFiveSolution(game); Assert.That(Count(game,true), Is.EqualTo(3)); Assert.That(Count(game,false), Is.EqualTo(1));
        Assert.That(Resources(game), Is.EqualTo(new[] {11,10,10,9,9})); Assert.That(Goals(game), Is.EqualTo(new[] {true,false,true}));
        Assert.That(Review(Hud(game), game, Results(game).First(r => (bool)Get(r,"IsComplaint"))), Is.True);
        Assert.That(Goals(game), Is.EqualTo(new[] {true,true,true}));
    }
    [Test] public void AutomaticComplaintPopupNeverCompletesManualReviewGoal()
    {
        var game = Start(5); var hud = Hud(game); StageFiveSolution(game);
        Assert.That(Get(Field(hud,"automaticCombo"), "CurrentResult"), Is.Not.Null);
        Assert.That(Goals(game)[1], Is.False); Assert.That(Get(Call(Stage(game),"CaptureProgressState"),"ComplaintReviewed"), Is.False);
    }
    [Test] public void ReviewingPositiveComboDoesNotMeetComplaintReviewGoal()
    {
        var game = Start(5); StageFiveSolution(game); var hud = Hud(game);
        Assert.That(Review(hud, game, Results(game).Single(r => !(bool)Get(r,"IsComplaint"))), Is.True);
        Assert.That(Goals(game)[1], Is.False);
    }
    [Test] public void RepeatedManualReviewDoesNotRepayResourcesIncreaseCountsOrAddTurns()
    {
        var game = Start(5); StageFiveSolution(game); var hud = Hud(game); object result = Results(game)[0];
        int[] before = Resources(game); int turns = (int)Get(Get(Session(game),"History"),"Count");
        Assert.That(Review(hud,game,result), Is.True); Assert.That(Review(hud,game,result), Is.True);
        Assert.That(Resources(game), Is.EqualTo(before)); Assert.That(Count(game,true), Is.EqualTo(3));
        Assert.That(Get(Get(Session(game),"History"),"Count"), Is.EqualTo(turns));
    }
    [Test] public void UnrelatedAndRepeatedSingleBuildingClicksCannotCompleteReview()
    {
        var game = Start(4); StageFourSolution(game); var hud = Hud(game); object details = Field(hud,"boardDetails");
        object a = Get(Call(Get(Session(game),"Board"),"GetTile",new Vector2Int(1,0)),"Building");
        object b = Get(Call(Get(Session(game),"Board"),"GetTile",new Vector2Int(2,2)),"Building");
        Call(details,"SelectBuilding",a); Assert.That(Call(details,"SelectBuilding",a), Is.False);
        Assert.That(Call(details,"SelectBuilding",b), Is.False); Assert.That(Goals(game)[1], Is.False);
    }
    [Test] public void UndoRecomputesCountsAndRollsBackReviewFromTheRemovedTurn()
    {
        var game = Start(5); StageFiveSolution(game); Review(Hud(game),game,Results(game)[0]);
        object history = Get(Session(game),"History"); Command(history,"TryUndo");
        Assert.That(Count(game,true), Is.EqualTo(3)); Assert.That(Goals(game)[1], Is.False);
        Command(history,"TryUndo"); Assert.That(Count(game,true), Is.EqualTo(2)); Assert.That(Goals(game), Is.EqualTo(new[] {true,false,false}));
        Command(history,"TryUndo"); Assert.That(Count(game,true), Is.EqualTo(1)); Assert.That(Get(Stage(game),"NextStageAvailable"), Is.False);
    }
    [Test] public void ReviewRecordedBeforeNextBuildSurvivesUndoToThatTurn()
    {
        var game = Start(4); StageFourSolution(game,4); Assert.That(Review(Hud(game),game,Results(game)[0]), Is.True);
        Build(game,42001,2,2); Command(Get(Session(game),"History"),"TryUndo");
        Assert.That(Count(game,false), Is.EqualTo(2)); Assert.That(Goals(game), Is.EqualTo(new[] {true,true,false}));
    }
    [Test] public void RemovedPairResultCannotBeConfirmedAfterUndo()
    {
        var game = Start(5); StageFiveSolution(game,4); object result = Results(game).Last();
        Command(Get(Session(game),"History"),"TryUndo"); object[] args = { result, null };
        Assert.That(Call(Stage(game),"TryConfirmInteraction",args), Is.False); Assert.That(Goals(game)[1], Is.False);
    }
    [Test] public void DisabledGameplayRejectsManualReview()
    {
        var game = Start(5); StageFiveSolution(game); object[] enabled = { false, null };
        Assert.That(Call(Session(game),"TrySetGameplayEnabled",enabled), Is.True); object[] args = { Results(game)[0], null };
        Assert.That(Call(Stage(game),"TryConfirmInteraction",args), Is.False); Assert.That(Goals(game)[1], Is.False);
    }
    [Test] public void RetryClearsInteractionsReviewFlagsAndHistory()
    {
        var game = Start(5); StageFiveSolution(game); Review(Hud(game),game,Results(game)[0]); Command(Flow(game),"TryCompleteStage");
        Command(Flow(game),"TryRetry"); Command(Flow(game),"TryDismissIntro");
        Assert.That(Results(game), Is.Empty); Assert.That(Goals(game), Is.EqualTo(new[] {false,false,false}));
        Assert.That(Get(Get(Session(game),"History"),"Count"), Is.Zero); Assert.That(Resources(game), Is.EqualTo(new[] {10,10,10,10,10}));
    }
    [Test] public void ComplaintCanMakeResourcesNegativeAndDependentBuildingIsBlocked()
    {
        var game = Start(5); Build(game,12001,1,1); Call(Get(Session(game),"Resources"),"Add",Resource(0),-12); Build(game,22001,1,2);
        Assert.That(Resources(game)[0], Is.EqualTo(-1)); Assert.That(Count(game,true), Is.EqualTo(1));
        object card = Items(Get(Get(Session(game),"Hand"),"Cards")).Single(c => (int)Get(Get(c,"Building"),"BuildingCode")==52001);
        object[] args = { Get(card,"CardId"), new Vector2Int(0,0), null };
        Assert.That(Call(Session(game),"CanBuild",args), Is.False);
        Build(game,32001,2,1); // No Population cost, so the negative resource does not block this card.
    }
    [TestCase(0)] [TestCase(-1)]
    public void ZeroAndNegativeResourcesBothBlockTheirUpfrontCosts(int population)
    {
        var game = Start(5); Call(Get(Session(game),"Resources"),"Add",Resource(0),population-10);
        object card = Items(Get(Get(Session(game),"Hand"),"Cards")).Single(c => (int)Get(Get(c,"Building"),"BuildingCode")==52001);
        object[] args = { Get(card,"CardId"), new Vector2Int(0,0), null, null };
        Assert.That(Call(Session(game),"TryCommitBuild",args), Is.False); Assert.That(Resources(game)[0], Is.EqualTo(population));
    }
    [Test] public void StageFiveFinalClearHasNoNextAndPersistsBestRank()
    {
        var game = Start(5); StageFiveSolution(game); Review(Hud(game),game,Results(game)[0]); Command(Flow(game),"TryCompleteStage");
        Assert.That(Get(Flow(game),"CanNextStage"), Is.False); Assert.That(Call(Flow(game),"GetBestRank",5), Is.EqualTo(3));
        Assert.That(Get(Get(Flow(game),"Result"),"Rank"), Is.EqualTo(3));
    }
    [Test] public void DuplicateResultEntriesNeverCountAsAdditionalPairs()
    {
        var game = Start(4); StageFourSolution(game,4);
        object[] capture = { null, null }; Assert.That(Call(Get(Session(game),"Board"),"TryCaptureBuildings",capture), Is.True);
        object[] results = Results(game); var repeated = Array.CreateInstance(Runtime("ComboResult"), results.Length * 2);
        for (int i = 0; i < repeated.Length; i++) repeated.SetValue(results[i % results.Length], i);
        object[] args = { Data(4), capture[0], Call(Get(Session(game),"Resources"),"CaptureResourceState"), 1, repeated, false, false, null, null };
        Assert.That(Runtime("StageGoalEvaluator").GetMethod("TryEvaluateStageWithInteractions").Invoke(null,args), Is.True,args[8] as string);
        Assert.That(Get(args[7],"GoalStates"), Is.EqualTo(new[] {true,false,false}));
    }
    [Test] public void DistinctPairsWithTheSameComboCodeEachCountOnce()
    {
        var stage = Own(UObject.Instantiate((ScriptableObject)Data(1)));
        Set(Items(Get(stage,"BuildingCards"))[0],"count",3);
        foreach (object amount in Items(Get(stage,"InitialResources")))
            if (Convert.ToInt32(Get(amount,"Resource"))==1)
            {
                var values=(Array)Field(stage,"initialResources");
                values.SetValue(Activator.CreateInstance(Runtime("ResourceAmount"),Resource(1),3),1);
            }
        object goal=Get(stage,"RequiredGoal"); Set(goal,"goalType",Enum.ToObject(Runtime("StageGoalType"),5)); Set(goal,"targetCount",2);
        var catalog=Own(UObject.Instantiate((ScriptableObject)Get(Content,"Stages"))); var stages=(Array)Field(catalog,"stages"); stages.SetValue(stage,0);
        var content=Own(UObject.Instantiate(Content)); Set(content,"stageCatalog",catalog);
        var game=Start(1,content); Build(game,11001,0,1); Build(game,11001,1,0);
        Assert.That(Get(Stage(game),"NextStageAvailable"), Is.False);
        Build(game,11001,1,1); Assert.That(Count(game,false), Is.EqualTo(2));
        Assert.That(Results(game).Select(r=>Get(r,"ComboCode")).Distinct().Count(), Is.EqualTo(1));
        Assert.That(Get(Stage(game),"NextStageAvailable"), Is.True);
    }
    [Test] public void FirstResourceNotificationObservesAllNewComplaintGoals()
    {
        var game=Start(5); StageFiveSolution(game,3); object resources=Get(Session(game),"Resources"); int seen=0;
        Action callback=()=> { seen++; Assert.That(Count(game,true), Is.EqualTo(3)); Assert.That(Goals(game), Is.EqualTo(new[] {true,false,true})); };
        EventInfo changed=resources.GetType().GetEvent("OnResourcesChanged"); changed.AddEventHandler(resources,callback);
        try { Build(game,52001,0,0); } finally { changed.RemoveEventHandler(resources,callback); }
        Assert.That(seen, Is.GreaterThan(0));
    }
    [TestCase(4)] [TestCase(5)] [TestCase(6)]
    public void NewCountGoalsRejectNonpositiveThresholds(int type)
    {
        object goal=Activator.CreateInstance(Runtime("StageGoalData")); Set(goal,"goalType",Enum.ToObject(Runtime("StageGoalType"),type)); Set(goal,"targetCount",0);
        var errors=new List<string>(); Call(goal,"Validate",errors); Assert.That(errors, Has.Some.Contains("positive target"));
    }
}
