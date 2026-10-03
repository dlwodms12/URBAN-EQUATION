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

// Expanded stages are memory clones. Tests never edit shipped assets or the player's save.
public class StageExpansionContractTests
{
    private readonly List<UObject> owned = new List<UObject>();
    private string directory;
    private string SavePath => Path.Combine(directory, "progress.json");
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static Type EditorType(string name) => Type.GetType(name + ", Assembly-CSharp-Editor", true);
    private static object Get(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
    private static object Field(object target, string name) => target.GetType().GetField(name,
        BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name,
        BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name).Invoke(target, args);
    private static object[] Items(object value) => ((IEnumerable)value).Cast<object>().ToArray();
    private T Own<T>(T value) where T : UObject { owned.Add(value); return value; }
    private ScriptableObject Content => (ScriptableObject)AssetDatabase.LoadAssetAtPath(
        "Assets/_UrbanEquation/Data/GameContent.asset", Runtime("GameContentData"));
    private static object Flow(Component game) => Get(game, "Flow");
    private static object Session(Component game) => Get(game, "Session");
    private static void Command(object target, string method)
    {
        object[] args = { null };
        Assert.That(Call(target, method, args), Is.True, args[0] as string);
    }
    private static void Select(object flow, int number)
    {
        object[] args = { number, null };
        Assert.That(Call(flow, "TrySelectStage", args), Is.True, args[1] as string);
    }
    private static string Saved(int count, bool finalCleared = true)
    {
        int[] ranks = Enumerable.Repeat(1, count).ToArray();
        ranks[0] = 3;
        if (count > 1) ranks[1] = 2;
        if (!finalCleared) ranks[count - 1] = 0;
        return "{\"version\":1,\"stageCount\":" + count + ",\"highestUnlockedStage\":" + count
            + ",\"bestRanks\":[" + string.Join(",", ranks) + "]}";
    }
    private ScriptableObject Expanded(int count)
    {
        var source = Content;
        Assert.That(source, Is.Not.Null);
        var originals = Items(Get(Get(source, "Stages"), "Stages"));
        var stages = Array.CreateInstance(Runtime("StageData"), count);
        for (int i = 0; i < count; i++)
        {
            if (i < 2) stages.SetValue(originals[i], i);
            else
            {
                var stage = Own(UObject.Instantiate((ScriptableObject)originals[0]));
                Set(stage, "stageNumber", i + 1); Set(stage, "stageName", "Expansion test " + (i + 1));
                stages.SetValue(stage, i);
            }
        }
        var catalog = Own(UObject.Instantiate((ScriptableObject)Get(source, "Stages")));
        Set(catalog, "stages", stages);
        var content = Own(UObject.Instantiate(source)); Set(content, "stageCatalog", catalog);
        return content;
    }
    private Component Bootstrap(ScriptableObject content)
    {
        var root = Own(new GameObject("Stage expansion test session"));
        var game = root.AddComponent(Runtime("GameBootstrap"));
        object[] args = { content, SavePath, false, null };
        Assert.That(Call(game, "TryInitialize", args), Is.True, args[3] as string);
        return game;
    }
    private Component Screens(Component game)
    {
        var data = AssetDatabase.LoadAssetAtPath("Assets/_UrbanEquation/Data/Presentation/GameApplication.asset",
            Runtime("GameApplicationSettings"));
        var prefab = (Component)Get(data, "ScreensPrefab");
        var root = Own(UObject.Instantiate(prefab.gameObject));
        var view = root.GetComponent(Runtime("GameScreensUI")); Call(view, "Bind", Flow(game), null);
        return view;
    }
    private static List<string> Audit(object catalog)
    {
        var errors = new List<string>();
        EditorType("UrbanEquationFinalQa").GetMethod("ValidateStageCatalog").Invoke(null, new[] { catalog, errors });
        return errors;
    }
    private static void BuildFirstCard(Component game)
    {
        object[] args = { 1, new Vector2Int(0, 0), null, null };
        Assert.That(Call(Session(game), "TryCommitBuild", args), Is.True, args[3] as string);
    }
    private static void InitializeAll(Component game, ScriptableObject content)
    {
        object session = Session(game);
        foreach (object stage in Items(Get(Get(content, "Stages"), "Stages")))
        {
            object[] args = { session, stage, Get(content, "Combos"), null };
            Assert.That(Runtime("StageSessionInitializer").GetMethod("TryInitialize").Invoke(null, args), Is.True,
                "Stage " + Get(stage, "StageNumber") + ": " + args[3]);
            Assert.That(Get(Get(session, "Board"), "Width"), Is.EqualTo(Get(stage, "Width")));
            Assert.That(Get(Get(session, "Board"), "Height"), Is.EqualTo(Get(stage, "Height")));
            Assert.That(Get(Get(session, "Stage"), "CurrentStage"), Is.SameAs(stage));
            int cards = Items(Get(stage, "BuildingCards")).Sum(entry => (int)Get(entry, "Count"));
            Assert.That(Items(Get(Get(session, "Hand"), "Cards")).Length, Is.EqualTo(cards));
            Assert.That(Items(Get(Get(session, "Stage"), "GoalStates")).Length, Is.EqualTo(3));
            Assert.That(Get(Get(session, "History"), "Count"), Is.Zero);
        }
    }
    [SetUp] public void SetUp()
    {
        directory = Path.Combine(Path.GetTempPath(), "UE-StageExpansion-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
    }
    [TearDown] public void TearDown()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) UObject.DestroyImmediate(owned[i]);
        owned.Clear(); if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }

    [TestCase(5)] [TestCase(10)]
    public void FinalAuditAcceptsExpandedCatalogAndPreservesTutorialContracts(int count)
    { Assert.That(Audit(Get(Expanded(count), "Stages")), Is.Empty); }

    [Test]
    public void FinalAuditRejectsInvalidAppendedStageData()
    {
        var content = Expanded(5); var catalog = Get(content, "Stages");
        object card = Items(Get(Items(Get(catalog, "Stages"))[2], "BuildingCards"))[0]; Set(card, "count", 0);
        Assert.That(Audit(catalog), Has.Some.Contains("positive count"));
    }
    [Test]
    public void FinalAuditRejectsMissingAppendedStage()
    {
        var catalog = Get(Expanded(5), "Stages"); ((Array)Field(catalog, "stages")).SetValue(null, 3);
        Assert.That(Audit(catalog), Has.Some.Contains("missing stage"));
    }
    [Test]
    public void FinalAuditRejectsDuplicateOrOutOfOrderStageNumbers()
    {
        var catalog = Get(Expanded(5), "Stages"); Set(Items(Get(catalog, "Stages"))[2], "stageNumber", 2);
        Assert.That(Audit(catalog), Has.Some.Contains("consecutive stages"));
    }
    [TestCase(5)] [TestCase(10)]
    public void ExpandedCatalogCanInitializeEveryStageWithMatchingBoardCardsAndGoals(int count)
    { var content = Expanded(count); InitializeAll(Bootstrap(content), content); }

    [TestCase(5)] [TestCase(10)]
    public void ActualStageSelectionUiCreatesAllRowsWithLocksAndVerticalScrolling(int count)
    {
        var game = Bootstrap(Expanded(count)); Command(Flow(game), "TryPlay"); var view = Screens(game);
        Assert.That(Get(view, "StageRowCount"), Is.EqualTo(count));
        var rows = Items(Field(view, "rows"));
        Assert.That(rows.Select(row => Get(row, "StageNumber")), Is.EqualTo(Enumerable.Range(1, count)));
        Assert.That(rows.Select(row => Get(row, "IsUnlocked")), Is.EqualTo(Enumerable.Range(1, count).Select(n => n == 1)));
        var content = (Transform)Field(view, "stageRowContent"); var scroll = content.GetComponentInParent<ScrollRect>(true);
        Assert.That(scroll, Is.Not.Null); Assert.That(scroll.vertical, Is.True); Assert.That(scroll.horizontal, Is.False);
        Assert.That(scroll.content, Is.SameAs(content));
    }
    [TestCase(5)] [TestCase(10)]
    public void OldCompletedSaveContinuesIntoStageThreeAndPersistsItsClear(int count)
    {
        File.WriteAllText(SavePath, Saved(2)); var game = Bootstrap(Expanded(count)); var view = Screens(game);
        Assert.That(Call(view, "TryContinue"), Is.True); Assert.That(Call(view, "TrySelectStage", 3), Is.True);
        Assert.That(Get(Flow(game), "State").ToString(), Is.EqualTo("StageIntro"));
        Command(Flow(game), "TryDismissIntro"); BuildFirstCard(game); Command(Flow(game), "TryCompleteStage");
        Assert.That(Call(Flow(game), "GetBestRank", 1), Is.EqualTo(3)); Assert.That(Call(Flow(game), "GetBestRank", 2), Is.EqualTo(2));
        Assert.That(Call(Flow(game), "IsStageUnlocked", 4), Is.True); Assert.That(Call(Flow(game), "IsStageUnlocked", 5), Is.False);
        Assert.That(Call(view, "TryNextStage"), Is.True); Assert.That(Get(Flow(game), "SelectedStageNumber"), Is.EqualTo(4));
        var reloaded = Bootstrap(Expanded(count)); Assert.That(Get(Flow(reloaded), "CanContinue"), Is.True);
        Assert.That(Call(Flow(reloaded), "GetBestRank", 3), Is.EqualTo(1)); Assert.That(Call(Flow(reloaded), "IsStageUnlocked", 4), Is.True);
    }
    [TestCase(5)] [TestCase(10)]
    public void ActualFinalStageDisablesNextAndRetryStartsWithFreshRuntime(int count)
    {
        File.WriteAllText(SavePath, Saved(count, false)); var game = Bootstrap(Expanded(count));
        Command(Flow(game), "TryContinue"); Select(Flow(game), count); Command(Flow(game), "TryDismissIntro");
        var view = Screens(game); BuildFirstCard(game); Command(Flow(game), "TryCompleteStage");
        Assert.That(Get(Flow(game), "CanNextStage"), Is.False);
        Assert.That(((Button)Field(view, "nextButton")).interactable, Is.False);
        Assert.That(Call(view, "TryNextStage"), Is.False); Assert.That(Call(view, "TryRetry"), Is.True);
        Assert.That(Get(Flow(game), "SelectedStageNumber"), Is.EqualTo(count));
        Assert.That(Get(Flow(game), "State").ToString(), Is.EqualTo("StageIntro"));
        Assert.That(Get(Get(Session(game), "History"), "Count"), Is.Zero);
        Assert.That(Items(Get(Get(Session(game), "Hand"), "Cards")).Length, Is.EqualTo(2));
        Assert.That(Get(Get(Session(game), "Stage"), "Rank"), Is.Zero);
    }
    [Test]
    public void CompletedFiveStageSaveSelectsOnlyStageSixAfterExpansionToTen()
    {
        File.WriteAllText(SavePath, Saved(5)); var game = Bootstrap(Expanded(10));
        Command(Flow(game), "TryContinue"); Select(Flow(game), 6);
        Assert.That(Get(Flow(game), "SelectedStageNumber"), Is.EqualTo(6));
        Assert.That(Call(Flow(game), "IsStageUnlocked", 7), Is.False);
        Assert.That(Call(Flow(game), "GetBestRank", 1), Is.EqualTo(3));
    }
    [Test]
    public void InvalidAppendedDefinitionBlocksStartupWithoutTouchingTheSave()
    {
        var content = Expanded(5); var stage = Items(Get(Get(content, "Stages"), "Stages"))[4]; Set(stage, "width", 0);
        string original = Saved(2); File.WriteAllText(SavePath, original);
        var game = Own(new GameObject("Invalid expansion test")).AddComponent(Runtime("GameBootstrap"));
        object[] args = { content, SavePath, false, null }; Assert.That(Call(game, "TryInitialize", args), Is.False);
        Assert.That(Get(game, "IsInitialized"), Is.False); Assert.That(File.ReadAllText(SavePath), Is.EqualTo(original));
    }
    [Test]
    public void ExpansionFixturesDoNotModifyAuthoredContentOrStageAssets()
    {
        var source = Content; var catalog = (ScriptableObject)Get(source, "Stages");
        var stages = Items(Get(catalog, "Stages")).Cast<UObject>().ToArray();
        string before = EditorJsonUtility.ToJson(source), catalogBefore = EditorJsonUtility.ToJson(catalog);
        var stageBefore = stages.Select(EditorJsonUtility.ToJson).ToArray(); var expanded = Expanded(10);
        Assert.That(EditorJsonUtility.ToJson(source), Is.EqualTo(before)); Assert.That(EditorJsonUtility.ToJson(catalog), Is.EqualTo(catalogBefore));
        Assert.That(stages.Select(EditorJsonUtility.ToJson), Is.EqualTo(stageBefore));
        Assert.That(Get(expanded, "Stages"), Is.Not.SameAs(catalog));
    }
    [Test]
    public void EveryAuthoredStageCanInitializeWithoutASeparateScene()
    { var content = Content; InitializeAll(Bootstrap(content), content); }

    [TestCase(5)] [TestCase(10)]
    public void ConfirmedNewGameResetsAllExpandedRanksAndUnlocks(int count)
    {
        File.WriteAllText(SavePath, Saved(2)); var game = Bootstrap(Expanded(count));
        Command(Flow(game), "TryPlay"); Assert.That(Get(Flow(game), "State").ToString(), Is.EqualTo("NewGameConfirmation"));
        Command(Flow(game), "TryConfirmNewGame");
        Assert.That(Enumerable.Range(1, count).Select(n => Call(Flow(game), "GetBestRank", n)), Is.All.EqualTo(0));
        Assert.That(Call(Flow(game), "IsStageUnlocked", 1), Is.True); Assert.That(Call(Flow(game), "IsStageUnlocked", 2), Is.False);
        Assert.That(Get(Flow(game), "SelectedStageNumber"), Is.EqualTo(1));
        Assert.That(Get(Get(Session(game), "History"), "Count"), Is.Zero);
    }
}
