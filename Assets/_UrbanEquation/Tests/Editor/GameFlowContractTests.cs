using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class GameFlowContractTests
{
    private readonly List<string> directories = new List<string>();
    private readonly List<GameObject> objects = new List<GameObject>();
    private readonly List<ScriptableObject> assets = new List<ScriptableObject>();

    private sealed class Context
    {
        public Component Board, Resources, Combo, Hand, Session, History, Stage, Prefab;
        public ScriptableObject A, B, C, StageData, TileData, Database;
    }
    private sealed class Observer
    {
        public Action Callback;
        public void Record<TA, TB>(TA a, TB b) => Callback();
        public void RecordOne<T>(T value) => Callback();
    }
    private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private GameObject Object(string name, bool active = false, bool ui = false)
    {
        var result = ui ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
        result.SetActive(active);
        objects.Add(result);
        return result;
    }
    private Component Component(string name, string type, bool active = false) =>
        Object(name, active).AddComponent(RuntimeType(type));
    private ScriptableObject Asset(string type)
    {
        var result = ScriptableObject.CreateInstance(RuntimeType(type));
        assets.Add(result);
        return result;
    }
    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static object Get(object target, string property) => target.GetType().GetProperty(property).GetValue(target);
    private static object Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method).Invoke(target, args);
    private static void Lifecycle(object target, string method) =>
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    private static Array Values(string type, params object[] values)
    {
        Array result = Array.CreateInstance(RuntimeType(type), values.Length);
        for (int i = 0; i < values.Length; i++) result.SetValue(values[i], i);
        return result;
    }
    private static object Resource(string type) => Enum.Parse(RuntimeType("ResourceType"), type);
    private static object Amount(string type, int amount) =>
        Activator.CreateInstance(RuntimeType("ResourceAmount"), Resource(type), amount);
    private ScriptableObject Building(int code)
    {
        var result = Asset("BuildingData");
        Set(result, "buildingCode", code);
        Set(result, "buildingName", "Building " + code);
        Set(result, "visualPrefab", Object("Building visual"));
        Set(result, "useResourceLists", true);
        Set(result, "requiredResources", Values("ResourceAmount", Amount("Jobs", 1)));
        Set(result, "gainedResources", Values("ResourceAmount", Amount("Population", 1)));
        Set(result, "allowedTileTypes", Values("TileType", Enum.Parse(RuntimeType("TileType"), "Grass")));
        return result;
    }
    private static object ComboDefinition(int code, int a, int b, string resource, int amount)
    {
        object result = Activator.CreateInstance(RuntimeType("ComboDefinition"));
        Set(result, "comboCode", code); Set(result, "buildingCodeA", a); Set(result, "buildingCodeB", b);
        Set(result, "comboName", "Test combo"); Set(result, "description", "History test");
        Set(result, "rewards", Values("ResourceAmount", Amount(resource, amount)));
        return result;
    }
    private static void Rows(Context context, params object[] definitions)
    {
        IList rows = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(RuntimeType("ComboDefinition")));
        foreach (object row in definitions) rows.Add(row);
        Set(context.Database, "combos", rows);
    }
    private Context Fixture(bool withStage = true, int stageNumber = 1)
    {
        var context = new Context();
        context.A = Building(1001); context.B = Building(2001); context.C = Building(3001);
        Component tileTemplate = Component("Tile template", "Tile");
        context.TileData = Asset("TileData");
        Set(context.TileData, "tileType", Enum.Parse(RuntimeType("TileType"), "Grass"));
        Set(context.TileData, "tilePrefab", tileTemplate);
        context.StageData = Asset("StageData");
        Set(context.StageData, "stageNumber", stageNumber);
        Set(context.StageData, "width", 3); Set(context.StageData, "height", 2);
        var tiles = new object[6];
        for (int i = 0; i < tiles.Length; i++) tiles[i] = context.TileData;
        Set(context.StageData, "tiles", Values("TileData", tiles));
        var entries = new object[3];
        var definitions = new[] { context.A, context.B, context.C };
        for (int i = 0; i < entries.Length; i++)
        {
            entries[i] = Activator.CreateInstance(RuntimeType("StageBuildingCardData"));
            Set(entries[i], "building", definitions[i]); Set(entries[i], "count", 2);
        }
        Set(context.StageData, "buildingCards", Values("StageBuildingCardData", entries));
        Set(context.StageData, "initialResources", Values("ResourceAmount",
            Amount("Population", 2), Amount("Jobs", 10), Amount("Money", 2),
            Amount("Logistics", 2), Amount("Tourism", 0)));
        context.Board = Component("Board", "BoardManager");
        context.Board.transform.position = new Vector3(10f, 2f, -4f);
        object[] boardArgs = { context.StageData, null };
        Assert.That(Call(context.Board, "TryCreateBoard", boardArgs), Is.True, boardArgs[1] as string);
        context.Resources = Component("Resources", "ResourceManager");
        Assert.That(Call(context.Resources, "TryInitializeFromStage", context.StageData), Is.True);
        context.Hand = Component("Hand", "BuildingHandManager", true);
        object[] handArgs = { context.StageData, context.Resources, null };
        Assert.That(Call(context.Hand, "TryInitializeFromStage", handArgs), Is.True, handArgs[2] as string);
        context.Database = Asset("ComboDatabase");
        Rows(context, ComboDefinition(1, 1001, 2001, "Money", 2),
            ComboDefinition(2, 2001, 3001, "Jobs", -1));
        context.Combo = Component("Combo manager", "ComboManager", true);
        object[] comboArgs = { context.Board, context.Resources, context.Database, null };
        Assert.That(Call(context.Combo, "TryConfigure", comboArgs), Is.True, comboArgs[3] as string);
        context.Prefab = Component("Building template", "BuildingInstance");
        context.Session = Component("Session", "GameSessionManager");
        Call(context.Session, "Configure", context.Board, context.Resources, context.Hand, context.Prefab);
        Call(context.Session, "ConfigureCombos", context.Combo);
        if (withStage)
        {
            context.Stage = Component("Stage state", "StageManager");
            Goals(context.StageData, Goal("Population", 3), Goal("Population", 4), Goal("Population", 5));
            object[] stageArgs = { context.StageData, context.Board, context.Resources, context.Hand, null };
            Assert.That(Call(context.Stage, "TryConfigure", stageArgs), Is.True, stageArgs[4] as string);
            object[] bindArgs = { context.Stage, null };
            Assert.That(Call(context.Session, "TryConfigureStage", bindArgs), Is.True, bindArgs[1] as string);
        }
        context.History = Component("History", "TurnHistoryManager", true);
        object[] historyArgs = { context.Session, context.Stage, null };
        Assert.That(Call(context.History, "TryConfigure", historyArgs), Is.True, historyArgs[2] as string);
        return context;
    }
    private static Component Tile(Context context, Vector2Int coordinate) =>
        (Component)Call(context.Board, "GetTile", coordinate);
    private static Component Build(Context context, int cardId, int x, int y = 0)
    {
        object[] args = { cardId, new Vector2Int(x, y), null, null };
        Assert.That(Call(context.Session, "TryCommitBuild", args), Is.True, args[3] as string);
        return (Component)args[2];
    }
    private static bool Undo(Context context, out string error)
    {
        object[] args = { null };
        bool result = (bool)Call(context.History, "TryUndo", args);
        error = args[0] as string;
        return result;
    }
    private static int[] ResourceValues(Context context)
    {
        var names = new[] { "Population", "Jobs", "Money", "Logistics", "Tourism" };
        var result = new int[names.Length];
        for (int i = 0; i < result.Length; i++) result[i] = (int)Call(context.Resources, "GetResource", Resource(names[i]));
        return result;
    }
    private static int[] CardIds(Context context)
    {
        IList cards = (IList)Get(context.Hand, "Cards");
        var ids = new int[cards.Count];
        for (int i = 0; i < ids.Length; i++) ids[i] = (int)Get(cards[i], "CardId");
        return ids;
    }
    private static object Progress(bool cleared, int rank, bool next, params bool[] goals) =>
        Activator.CreateInstance(RuntimeType("StageProgressState"), cleared, goals, rank, next);
    private static void StageState(Context context, bool cleared, int rank, bool next, params bool[] goals) =>
        Assert.That(Call(context.Stage, "TryApplyProgressState", Progress(cleared, rank, next, goals)), Is.True);
    private static void Watch(object target, string name, Action callback) =>
        target.GetType().GetEvent(name).AddEventHandler(target, callback);
    private static void WatchPair(object target, string name, Action callback)
    {
        EventInfo info = target.GetType().GetEvent(name);
        Type[] arguments = info.EventHandlerType.GetGenericArguments();
        var observer = new Observer { Callback = callback };
        MethodInfo method = typeof(Observer).GetMethod("Record").MakeGenericMethod(arguments);
        info.AddEventHandler(target, Delegate.CreateDelegate(info.EventHandlerType, observer, method));
    }
    [TearDown]
    public void TearDown()
    {
        foreach (GameObject root in objects)
        {
            if (root == null) continue;
            foreach (string type in new[] { "TurnHistoryManager", "ComboManager", "BuildingHandManager",
                "BuildingPlacementController", "UndoButtonUI", "NextStageButtonUI", "SaveManager", "SceneFlowManager" })
            {
                Component component = root.GetComponent(RuntimeType(type));
                if (component != null) Lifecycle(component, "OnDisable");
            }
            Component clear = root.GetComponent(RuntimeType("ClearUI"));
            if (clear != null) Lifecycle(clear, "OnDestroy");
        }
        for (int i = objects.Count - 1; i >= 0; i--)
            if (objects[i] != null) UnityEngine.Object.DestroyImmediate(objects[i]);
        foreach (ScriptableObject asset in assets)
            if (asset != null) UnityEngine.Object.DestroyImmediate(asset);
        objects.Clear(); assets.Clear();
        foreach (string directory in directories)
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        directories.Clear();
    }

    private static object Goal(string resource, int amount)
    {
        object goal = Activator.CreateInstance(RuntimeType("StageGoalData"));
        Set(goal, "description", resource + " >= " + amount);
        Set(goal, "resourceTarget", Amount(resource, amount));
        return goal;
    }
    private static object AllCardsGoal()
    {
        object goal = Goal("Population", 0);
        Set(goal, "goalType", Enum.Parse(RuntimeType("StageGoalType"), "AllCardsUsed"));
        return goal;
    }
    private static object AdjacencyGoal(ScriptableObject a, ScriptableObject b)
    {
        object goal = Goal("Population", 0);
        Set(goal, "goalType", Enum.Parse(RuntimeType("StageGoalType"), "AdjacentBuildings"));
        Set(goal, "buildingA", a); Set(goal, "buildingB", b);
        return goal;
    }
    private static void Goals(ScriptableObject stage, object required, object a, object b)
    {
        Set(stage, "requiredGoal", required);
        Set(stage, "additionalGoals", Values("StageGoalData", a, b));
    }
    private static Array Resources(int population = 0) => Values("ResourceAmount",
        Amount("Population", population), Amount("Jobs", 0), Amount("Money", 0),
        Amount("Logistics", 0), Amount("Tourism", 0));
    private static object Placement(ScriptableObject building, int x, int y) =>
        Activator.CreateInstance(RuntimeType("BuildingStateSnapshot"), new Vector2Int(x, y), building);
    private static bool Evaluate(object goal, Array buildings, Array resources, int cards, out bool achieved)
    {
        object[] args = { goal, buildings, resources, cards, false, null };
        bool valid = (bool)RuntimeType("StageGoalEvaluator").GetMethod("TryEvaluateGoal").Invoke(null, args);
        achieved = (bool)args[4];
        return valid;
    }
    private static object EvaluateStage(ScriptableObject stage, Array resources)
    {
        object[] args = { stage, Values("BuildingStateSnapshot"), resources, 2, null, null };
        Assert.That(RuntimeType("StageGoalEvaluator").GetMethod("TryEvaluateStage").Invoke(null, args),
            Is.True, args[5] as string);
        return args[4];
    }
    private static bool Complete(Context context, out object result)
    {
        object[] args = { null, null };
        bool success = (bool)Call(context.Stage, "TryCompleteStage", args);
        result = args[0];
        return success;
    }
    private static void Refresh(Context context)
    {
        object[] args = { null };
        Assert.That(Call(context.Stage, "TryRefreshGoals", args), Is.True, args[0] as string);
    }
    private static void WatchResult(object target, Action callback)
    {
        EventInfo info = target.GetType().GetEvent("OnStageCompleted");
        var observer = new Observer { Callback = callback };
        MethodInfo method = typeof(Observer).GetMethod("RecordOne")
            .MakeGenericMethod(info.EventHandlerType.GetGenericArguments());
        info.AddEventHandler(target, Delegate.CreateDelegate(info.EventHandlerType, observer, method));
    }

    private string SavePath()
    {
        string directory = Path.Combine(Path.GetTempPath(), "UrbanEquationTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        directories.Add(directory);
        return Path.Combine(directory, "progress.json");
    }
    private static bool Command(object target, string name)
    {
        object[] args = { null };
        return (bool)Call(target, name, args);
    }
    private Component SaveManager(string path = null, int count = 3, bool load = true)
    {
        Component manager = Component("Save manager", "SaveManager", true);
        object[] args = { count, path ?? SavePath(), null };
        Assert.That(Call(manager, "TryConfigure", args), Is.True, args[2] as string);
        if (load) Assert.That(Command(manager, "TryLoad"), Is.True);
        return manager;
    }
    private static object Fresh(int count = 3)
    {
        object[] args = { count, null, null };
        Assert.That(RuntimeType("ProgressSaveData").GetMethod("TryCreate").Invoke(null, args), Is.True);
        return args[1];
    }
    private static object Record(object data, int stage, int rank)
    {
        object[] args = { stage, rank, null, null };
        Assert.That(Call(data, "TryRecordClear", args), Is.True, args[3] as string);
        return args[2];
    }
    private static bool Save(Component manager, int stage, int rank)
    {
        object[] args = { stage, rank, null };
        return (bool)Call(manager, "TryRecordClear", args);
    }
    private static int Best(Component manager, int number) =>
        (int)Call(Get(manager, "Progress"), "GetBestRank", number);
    private static string Encode(object data)
    {
        object[] args = { data, null, null };
        Assert.That(RuntimeType("ProgressSaveCodec").GetMethod("TrySerialize").Invoke(null, args), Is.True);
        return (string)args[1];
    }
    private static bool Decode(string json, int count, out object data)
    {
        object[] args = { json, count, null, null };
        bool result = (bool)RuntimeType("ProgressSaveCodec").GetMethod("TryDeserialize").Invoke(null, args);
        data = args[2];
        return result;
    }
    private static string Json(int version = 1, int count = 3, int unlocked = 1, string ranks = "[0,0,0]") =>
        "{\"version\":" + version + ",\"stageCount\":" + count
        + ",\"highestUnlockedStage\":" + unlocked + ",\"bestRanks\":" + ranks + "}";
    private static object FileStore(string path) => Activator.CreateInstance(RuntimeType("ProgressFileStore"), path);
    private static bool Write(object store, string content)
    {
        object[] args = { content, null };
        return (bool)Call(store, "TryWrite", args);
    }
    private static void Bind(Component manager, Context context)
    {
        object[] args = { context.Stage, null };
        Assert.That(Call(manager, "TryBindStage", args), Is.True, args[1] as string);
    }

    private sealed class FlowContext
    {
        public Context Game;
        public Component Flow, Save;
        public ScriptableObject Catalog;
        public string Path;
    }
    private FlowContext FlowFixture(bool saved = false, int rank = 0, string path = null)
    {
        var fixture = new FlowContext { Game = Fixture(), Path = path ?? SavePath() };
        ScriptableObject second = UnityEngine.Object.Instantiate(fixture.Game.StageData);
        assets.Add(second);
        Set(second, "stageNumber", 2);
        Set(second, "stageName", "Second stage");
        fixture.Catalog = Asset("StageCatalog");
        Set(fixture.Catalog, "stages", Values("StageData", fixture.Game.StageData, second));
        fixture.Save = SaveManager(fixture.Path, 2, load: false);
        if (saved)
        {
            Assert.That(Command(fixture.Save, "TryStartNewGame"), Is.True);
            if (rank > 0) Assert.That(Save(fixture.Save, 1, rank), Is.True);
        }
        fixture.Flow = Component("Flow", "SceneFlowManager", true);
        ConfigureFlow(fixture);
        return fixture;
    }
    private static void ConfigureFlow(FlowContext fixture)
    {
        object[] args = { fixture.Catalog, fixture.Save, fixture.Game.Session, fixture.Game.Database, null };
        Assert.That(Call(fixture.Flow, "TryConfigure", args), Is.True, args[4] as string);
    }
    private static string Screen(FlowContext fixture) => Get(fixture.Flow, "State").ToString();
    private static void Do(FlowContext fixture, string command)
    {
        object[] args = { null };
        Assert.That(Call(fixture.Flow, command, args), Is.True, args[0] as string);
    }
    private static bool Select(FlowContext fixture, int number)
    {
        object[] args = { number, null };
        return (bool)Call(fixture.Flow, "TrySelectStage", args);
    }
    private static void Playing(FlowContext fixture)
    {
        if ((bool)Get(fixture.Save, "HasProgress"))
        {
            Do(fixture, "TryContinue");
            Assert.That(Select(fixture, 1), Is.True);
        }
        else Do(fixture, "TryPlay");
        Do(fixture, "TryDismissIntro");
    }
    private static void Cleared(FlowContext fixture)
    {
        Playing(fixture);
        Build(fixture.Game, 1, 0);
        Do(fixture, "TryCompleteStage");
        Assert.That(Screen(fixture), Is.EqualTo("StageClear"));
    }
    private static void FailClearWrite(FlowContext fixture)
    {
        Playing(fixture);
        Build(fixture.Game, 1, 0);
        File.Delete(fixture.Path);
        Directory.CreateDirectory(fixture.Path); // deterministic IO failure on every supported platform
        Do(fixture, "TryCompleteStage");
    }
    private static void WatchOne(object target, string name, Action callback)
    {
        EventInfo info = target.GetType().GetEvent(name);
        var observer = new Observer { Callback = callback };
        MethodInfo method = typeof(Observer).GetMethod("RecordOne")
            .MakeGenericMethod(info.EventHandlerType.GetGenericArguments());
        info.AddEventHandler(target, Delegate.CreateDelegate(info.EventHandlerType, observer, method));
    }
    private static void EmptyTurn(FlowContext fixture)
    {
        Assert.That(CardIds(fixture.Game), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6 }));
        Assert.That(ResourceValues(fixture.Game), Is.EqualTo(new[] { 2, 10, 2, 2, 0 }));
        Assert.That(Get(fixture.Game.History, "Count"), Is.EqualTo(0));
        Assert.That(((IList)Get(fixture.Game.Combo, "Results")).Count, Is.EqualTo(0));
        Assert.That(Get(fixture.Game.Combo, "CurrentPresentation"), Is.Null);
        Assert.That(((IList)Get(fixture.Game.Session, "LastComboResults")).Count, Is.EqualTo(0));
        Assert.That(Get(fixture.Game.Stage, "IsCleared"), Is.False);
        Assert.That(Get(fixture.Game.Stage, "Rank"), Is.EqualTo(0));
        for (int y = 0; y < 2; y++)
            for (int x = 0; x < 3; x++)
                Assert.That(Get(Tile(fixture.Game, new Vector2Int(x, y)), "IsOccupied"), Is.False);
    }

    [Test]
    public void FreshLobbyDoesNotCreateAFileAndDisablesContinueAndGameplay()
    {
        FlowContext fixture = FlowFixture();
        Assert.That(Screen(fixture), Is.EqualTo("Lobby"));
        Assert.That(File.Exists(fixture.Path), Is.False);
        Assert.That(Get(fixture.Flow, "CanContinue"), Is.False);
        Assert.That(Get(fixture.Game.Session, "GameplayEnabled"), Is.False);
        Assert.That(Get(fixture.Flow, "Scene").ToString(), Is.EqualTo("Lobby"));
    }
    [Test]
    public void PlayWithoutProgressPersistsNewGameAndShowsStageOneIntro()
    {
        FlowContext fixture = FlowFixture();
        Do(fixture, "TryPlay");
        Assert.That(Screen(fixture), Is.EqualTo("StageIntro"));
        Assert.That(Get(fixture.Flow, "SelectedStageNumber"), Is.EqualTo(1));
        Assert.That(File.Exists(fixture.Path), Is.True);
        Assert.That(Get(fixture.Game.Session, "GameplayEnabled"), Is.False);
        EmptyTurn(fixture);
    }
    [Test]
    public void IntroBlocksDirectBuildDragAndStageCompletion()
    {
        FlowContext fixture = FlowFixture();
        Do(fixture, "TryPlay");
        object[] build = { 1, Vector2Int.zero, null, null };
        Assert.That(Call(fixture.Game.Session, "TryCommitBuild", build), Is.False);
        Assert.That(build[3] as string, Does.Contain("disabled"));
        Component placement = Component("Placement", "BuildingPlacementController", true);
        Call(placement, "Configure", fixture.Game.Session, null);
        Assert.That(Call(placement, "TryBeginDrag", 1), Is.False);
        Assert.That(Complete(fixture.Game, out _), Is.False);
        EmptyTurn(fixture);
    }
    [Test]
    public void IntroOkEnablesGameplayBeforePublishingPlayingScreen()
    {
        FlowContext fixture = FlowFixture();
        Do(fixture, "TryPlay");
        bool observed = false;
        Watch(fixture.Flow, "OnStateChanged", () =>
        {
            if (Screen(fixture) != "Playing") return;
            Assert.That(Get(fixture.Game.Session, "GameplayEnabled"), Is.True);
            observed = true;
        });
        Do(fixture, "TryDismissIntro");
        Assert.That(observed, Is.True);
        Build(fixture.Game, 1, 0);
    }
    [Test]
    public void ContinueWithoutSavedProgressCannotLeaveLobby()
    {
        FlowContext fixture = FlowFixture();
        Assert.That(Command(fixture.Flow, "TryContinue"), Is.False);
        Assert.That(Screen(fixture), Is.EqualTo("Lobby"));
        Assert.That(File.Exists(fixture.Path), Is.False);
    }
    [Test]
    public void PlayWithProgressOnlyAsksConfirmationAndLeavesFileUntouched()
    {
        FlowContext fixture = FlowFixture(true, 3);
        string before = File.ReadAllText(fixture.Path);
        Do(fixture, "TryPlay");
        Assert.That(Screen(fixture), Is.EqualTo("NewGameConfirmation"));
        Assert.That(File.ReadAllText(fixture.Path), Is.EqualTo(before));
        Assert.That(Get(fixture.Flow, "SelectedStageNumber"), Is.EqualTo(0));
    }
    [Test]
    public void CancelNewGamePreservesUnlocksAndBestRank()
    {
        FlowContext fixture = FlowFixture(true, 3);
        string before = File.ReadAllText(fixture.Path);
        Do(fixture, "TryPlay"); Do(fixture, "TryCancel");
        Assert.That(Screen(fixture), Is.EqualTo("Lobby"));
        Assert.That(Best(fixture.Save, 1), Is.EqualTo(3));
        Assert.That(Get(Get(fixture.Save, "Progress"), "HighestUnlockedStage"), Is.EqualTo(2));
        Assert.That(File.ReadAllText(fixture.Path), Is.EqualTo(before));
    }
    [Test]
    public void ConfirmNewGameResetsPermanentProgressAndStartsStageOne()
    {
        FlowContext fixture = FlowFixture(true, 3);
        Do(fixture, "TryPlay"); Do(fixture, "TryConfirmNewGame");
        Assert.That(Screen(fixture), Is.EqualTo("StageIntro"));
        Assert.That(Best(fixture.Save, 1), Is.EqualTo(0));
        Assert.That(Get(Get(fixture.Save, "Progress"), "HighestUnlockedStage"), Is.EqualTo(1));
        Assert.That(Get(fixture.Flow, "SelectedStageNumber"), Is.EqualTo(1));
        EmptyTurn(fixture);
    }
    [Test]
    public void ContinueFromNewGamePromptOpensStageSelectionWithoutReset()
    {
        FlowContext fixture = FlowFixture(true, 2);
        string before = File.ReadAllText(fixture.Path);
        Do(fixture, "TryPlay"); Do(fixture, "TryContinue");
        Assert.That(Screen(fixture), Is.EqualTo("StageSelect"));
        Assert.That(Get(fixture.Flow, "Scene").ToString(), Is.EqualTo("Lobby"));
        Assert.That(File.ReadAllText(fixture.Path), Is.EqualTo(before));
        Assert.That(Call(fixture.Flow, "GetBestRank", 1), Is.EqualTo(2));
    }
    [Test]
    public void ContinueDoesNotInitializeOrRestoreAnInProgressBoard()
    {
        FlowContext fixture = FlowFixture(true);
        object version = Get(fixture.Game.Board, "ResetVersion");
        Do(fixture, "TryContinue");
        Assert.That(Get(fixture.Game.Board, "ResetVersion"), Is.EqualTo(version));
        Assert.That(Get(fixture.Flow, "SelectedStageNumber"), Is.EqualTo(0));
        Assert.That(Get(fixture.Game.Session, "GameplayEnabled"), Is.False);
    }
    [Test]
    public void LockedAndOutOfRangeStagesCannotBeSelected()
    {
        FlowContext fixture = FlowFixture(true);
        Do(fixture, "TryContinue");
        Assert.That(Select(fixture, 0), Is.False);
        Assert.That(Select(fixture, 2), Is.False);
        Assert.That(Select(fixture, 3), Is.False);
        Assert.That(Call(fixture.Flow, "IsStageUnlocked", 1), Is.True);
        Assert.That(Call(fixture.Flow, "IsStageUnlocked", 2), Is.False);
        Assert.That(Screen(fixture), Is.EqualTo("StageSelect"));
    }
    [Test]
    public void SelectingAnUnlockedStageUsesItsOwnDataAndShowsIntro()
    {
        FlowContext fixture = FlowFixture(true, 1);
        Do(fixture, "TryContinue");
        Assert.That(Select(fixture, 2), Is.True);
        Assert.That(Screen(fixture), Is.EqualTo("StageIntro"));
        Assert.That(Get(fixture.Flow, "SelectedStageNumber"), Is.EqualTo(2));
        Assert.That(Get(Get(fixture.Game.Stage, "CurrentStage"), "StageNumber"), Is.EqualTo(2));
        Assert.That(Get(fixture.Game.Board, "CurrentStage"), Is.SameAs(Get(fixture.Flow, "SelectedStage")));
        EmptyTurn(fixture);
    }
    [Test]
    public void CatalogRejectsMissingReorderedAndNonconsecutiveStages()
    {
        FlowContext fixture = FlowFixture();
        Set(fixture.Catalog, "stages", Values("StageData", fixture.Game.StageData, null));
        var errors = new List<string>();
        Call(fixture.Catalog, "Validate", errors);
        Assert.That(errors, Is.Not.Empty);
        Set(fixture.Catalog, "stages", Values("StageData", fixture.Game.StageData, fixture.Game.StageData));
        errors.Clear(); Call(fixture.Catalog, "Validate", errors);
        Assert.That(errors, Is.Not.Empty);
        Set(fixture.Catalog, "stages", Values("StageData"));
        errors.Clear(); Call(fixture.Catalog, "Validate", errors);
        Assert.That(errors, Is.Not.Empty);
    }
    [Test]
    public void MismatchedSaveStageCountCannotConfigureFlow()
    {
        FlowContext fixture = FlowFixture();
        Component wrongSave = SaveManager(count: 3);
        object[] args = { fixture.Catalog, wrongSave, fixture.Game.Session, fixture.Game.Database, null };
        Assert.That(Call(fixture.Flow, "TryConfigure", args), Is.False);
        Assert.That(Screen(fixture), Is.EqualTo("Lobby"));
        Assert.That(File.Exists(fixture.Path), Is.False);
    }
    [Test]
    public void MissingVisualCannotEraseProgressOnConfirmedNewGame()
    {
        FlowContext fixture = FlowFixture(true, 3);
        Do(fixture, "TryPlay");
        string before = File.ReadAllText(fixture.Path);
        Set(fixture.Game.A, "visualPrefab", null);
        Assert.That(Command(fixture.Flow, "TryConfirmNewGame"), Is.False);
        Assert.That(Screen(fixture), Is.EqualTo("NewGameConfirmation"));
        Assert.That(File.ReadAllText(fixture.Path), Is.EqualTo(before));
        Assert.That(Best(fixture.Save, 1), Is.EqualTo(3));
    }
    [Test]
    public void MissingTilePrefabPreservesLiveBoardOnSelectionFailure()
    {
        FlowContext fixture = FlowFixture(true);
        Do(fixture, "TryContinue");
        Component tile = Tile(fixture.Game, Vector2Int.zero);
        object version = Get(fixture.Game.Board, "ResetVersion");
        Set(fixture.Game.TileData, "tilePrefab", null);
        Assert.That(Select(fixture, 1), Is.False);
        Assert.That(Tile(fixture.Game, Vector2Int.zero), Is.SameAs(tile));
        Assert.That(Get(fixture.Game.Board, "ResetVersion"), Is.EqualTo(version));
        Assert.That(Screen(fixture), Is.EqualTo("StageSelect"));
    }
    [Test]
    public void NextStageCannotCompleteBeforeRequiredGoal()
    {
        FlowContext fixture = FlowFixture(); Playing(fixture);
        Assert.That(Command(fixture.Flow, "TryCompleteStage"), Is.False);
        Assert.That(Screen(fixture), Is.EqualTo("Playing"));
        Assert.That(Get(fixture.Flow, "Result"), Is.Null);
        Assert.That(Best(fixture.Save, 1), Is.EqualTo(0));
    }
    [Test]
    public void RequiredGoalAllowsFurtherBuildsUntilPlayerConfirmsClear()
    {
        FlowContext fixture = FlowFixture(); Playing(fixture);
        Build(fixture.Game, 1, 0);
        Assert.That(Get(fixture.Game.Stage, "NextStageAvailable"), Is.True);
        Assert.That(Screen(fixture), Is.EqualTo("Playing"));
        Build(fixture.Game, 3, 1);
        Assert.That(Get(fixture.Game.History, "Count"), Is.EqualTo(2));
        Assert.That(Get(fixture.Game.Stage, "Rank"), Is.EqualTo(2));
    }
    [Test]
    public void MainNextStageProducesClearResultAndPersistsUnlock()
    {
        FlowContext fixture = FlowFixture(); Cleared(fixture);
        Assert.That(Get(Get(fixture.Flow, "Result"), "Rank"), Is.EqualTo(1));
        Assert.That(Best(fixture.Save, 1), Is.EqualTo(1));
        Assert.That(Get(fixture.Flow, "CanNextStage"), Is.True);
        Assert.That(Get(fixture.Game.Session, "GameplayEnabled"), Is.False);
        Assert.That(Get(fixture.Flow, "SaveError"), Is.Null);
    }
    [Test]
    public void ExistingNextStageButtonAlsoOpensTheFlowClearScreen()
    {
        FlowContext fixture = FlowFixture(); Playing(fixture); Build(fixture.Game, 1, 0);
        Button button = Object("Next button", true, true).AddComponent<Button>();
        Component ui = Component("Next UI", "NextStageButtonUI", true);
        Call(ui, "Configure", fixture.Game.Stage, button);
        Assert.That(button.interactable, Is.True);
        button.onClick.Invoke();
        Assert.That(Screen(fixture), Is.EqualTo("StageClear"));
        Assert.That(button.interactable, Is.False);
        Assert.That(Best(fixture.Save, 1), Is.EqualTo(1));
    }
    [Test]
    public void PauseBlocksBuildUndoAndCompletionAndRefreshesButtons()
    {
        FlowContext fixture = FlowFixture(); Playing(fixture);
        Build(fixture.Game, 1, 0); Build(fixture.Game, 3, 1);
        Button undoButton = Object("Undo button", true, true).AddComponent<Button>();
        Component undo = Component("Undo UI", "UndoButtonUI", true);
        Call(undo, "Configure", fixture.Game.History, undoButton);
        Button nextButton = Object("Next button", true, true).AddComponent<Button>();
        Component next = Component("Next UI", "NextStageButtonUI", true);
        Call(next, "Configure", fixture.Game.Stage, nextButton);
        Do(fixture, "TryRequestPause");
        object[] build = { 2, new Vector2Int(2, 0), null, null };
        Assert.That(Call(fixture.Game.Session, "TryCommitBuild", build), Is.False);
        Assert.That(Undo(fixture.Game, out _), Is.False);
        Assert.That(Complete(fixture.Game, out _), Is.False);
        Assert.That(undoButton.interactable, Is.False);
        Assert.That(nextButton.interactable, Is.False);
        Assert.That(Get(fixture.Game.History, "Count"), Is.EqualTo(2));
        Do(fixture, "TryCancel");
        Assert.That(undoButton.interactable, Is.True);
        Assert.That(nextButton.interactable, Is.True);
    }
    [Test]
    public void OpeningPauseCancelsActiveDragAndClearsItsPreview()
    {
        FlowContext fixture = FlowFixture(); Playing(fixture);
        Component placement = Component("Placement", "BuildingPlacementController", true);
        Call(placement, "Configure", fixture.Game.Session, null);
        Assert.That(Call(placement, "TryBeginDrag", 1), Is.True);
        Do(fixture, "TryRequestPause");
        Assert.That(Get(placement, "IsDragging"), Is.False);
        Assert.That(Get(placement, "PreviewTile"), Is.Null);
        Assert.That(Call(placement, "TryBeginDrag", 1), Is.False);
    }
    [Test]
    public void CancelPausePreservesBoardCardsResourcesHistoryAndCombos()
    {
        FlowContext fixture = FlowFixture(); Playing(fixture);
        Component first = Build(fixture.Game, 1, 0); Build(fixture.Game, 3, 1);
        int[] resources = ResourceValues(fixture.Game), cards = CardIds(fixture.Game);
        object snapshot = Get(fixture.Game.History, "CurrentSnapshot");
        object combo = Get(fixture.Game.Combo, "CurrentPresentation");
        Do(fixture, "TryRequestPause"); Do(fixture, "TryCancel");
        Assert.That(Get(Tile(fixture.Game, Vector2Int.zero), "Building"), Is.SameAs(first));
        Assert.That(ResourceValues(fixture.Game), Is.EqualTo(resources));
        Assert.That(CardIds(fixture.Game), Is.EqualTo(cards));
        Assert.That(Get(fixture.Game.History, "CurrentSnapshot"), Is.SameAs(snapshot));
        Assert.That(Get(fixture.Game.Combo, "CurrentPresentation"), Is.SameAs(combo));
        Assert.That(Get(fixture.Game.History, "CanUndo"), Is.True);
    }
    [Test]
    public void ConfirmPauseDiscardsAllLiveTurnsAndPreservesPermanentProgress()
    {
        FlowContext fixture = FlowFixture(true, 3); Playing(fixture);
        Build(fixture.Game, 1, 0); Build(fixture.Game, 3, 1);
        string before = File.ReadAllText(fixture.Path);
        Do(fixture, "TryRequestPause"); Do(fixture, "TryConfirmPause");
        Assert.That(Screen(fixture), Is.EqualTo("Lobby"));
        Assert.That(Get(fixture.Game.Session, "GameplayEnabled"), Is.False);
        EmptyTurn(fixture);
        Assert.That(File.ReadAllText(fixture.Path), Is.EqualTo(before));
        Assert.That(Best(fixture.Save, 1), Is.EqualTo(3));
    }
    [Test]
    public void ReentryAfterMidstageExitAlwaysStartsFromStageBeginning()
    {
        FlowContext fixture = FlowFixture(); Playing(fixture); Build(fixture.Game, 1, 0);
        Do(fixture, "TryRequestPause"); Do(fixture, "TryConfirmPause");
        Do(fixture, "TryContinue"); Assert.That(Select(fixture, 1), Is.True);
        Assert.That(Screen(fixture), Is.EqualTo("StageIntro"));
        EmptyTurn(fixture);
    }
    [Test]
    public void RetryShowsIntroAndResetsAllRuntimeStateWithoutLoweringBestRank()
    {
        FlowContext fixture = FlowFixture(true, 3); Playing(fixture);
        Build(fixture.Game, 1, 0); Build(fixture.Game, 3, 1); Do(fixture, "TryCompleteStage");
        Do(fixture, "TryRetry");
        Assert.That(Screen(fixture), Is.EqualTo("StageIntro"));
        Assert.That(Get(fixture.Flow, "SelectedStageNumber"), Is.EqualTo(1));
        Assert.That(Get(fixture.Flow, "Result"), Is.Null);
        Assert.That(Best(fixture.Save, 1), Is.EqualTo(3));
        EmptyTurn(fixture);
    }
    [Test]
    public void ClearNextStageInitializesTheNextStageAndDropsPreviousHistory()
    {
        FlowContext fixture = FlowFixture(); Playing(fixture);
        Build(fixture.Game, 1, 0); Build(fixture.Game, 3, 1); Do(fixture, "TryCompleteStage");
        Do(fixture, "TryNextStage");
        Assert.That(Screen(fixture), Is.EqualTo("StageIntro"));
        Assert.That(Get(fixture.Flow, "SelectedStageNumber"), Is.EqualTo(2));
        Assert.That(Get(Get(fixture.Game.Stage, "CurrentStage"), "StageNumber"), Is.EqualTo(2));
        EmptyTurn(fixture);
    }
    [Test]
    public void LastStageHasNoNextStageButAllowsStageSelection()
    {
        FlowContext fixture = FlowFixture(); Cleared(fixture);
        Do(fixture, "TryNextStage"); Do(fixture, "TryDismissIntro");
        Build(fixture.Game, 1, 0); Do(fixture, "TryCompleteStage");
        Assert.That(Best(fixture.Save, 2), Is.EqualTo(1));
        Assert.That(Get(fixture.Flow, "CanNextStage"), Is.False);
        Assert.That(Command(fixture.Flow, "TryNextStage"), Is.False);
        Assert.That(Screen(fixture), Is.EqualTo("StageClear"));
        Do(fixture, "TryReturnToStageSelect");
        Assert.That(Screen(fixture), Is.EqualTo("StageSelect"));
        Assert.That(Get(fixture.Flow, "Scene").ToString(), Is.EqualTo("Lobby"));
    }
    [Test]
    public void LeavingClearForSelectionDiscardsRuntimeAndKeepsSavedRanks()
    {
        FlowContext fixture = FlowFixture(); Cleared(fixture);
        Do(fixture, "TryReturnToStageSelect");
        EmptyTurn(fixture);
        Assert.That(Best(fixture.Save, 1), Is.EqualTo(1));
        Assert.That(Get(fixture.Flow, "Result"), Is.Null);
        Assert.That(Get(fixture.Game.Session, "GameplayEnabled"), Is.False);
    }
    [Test]
    public void FailedClearSavePreservesResultAndBlocksEveryDepartureCommand()
    {
        FlowContext fixture = FlowFixture(); FailClearWrite(fixture);
        object result = Get(fixture.Flow, "Result");
        Assert.That(result, Is.Not.Null);
        Assert.That(Get(fixture.Save, "PendingResult"), Is.SameAs(result));
        Assert.That(Get(fixture.Flow, "SaveError"), Is.Not.Null.And.Not.Empty);
        Assert.That(Get(fixture.Flow, "CanNextStage"), Is.False);
        Assert.That(Command(fixture.Flow, "TryRetry"), Is.False);
        Assert.That(Command(fixture.Flow, "TryNextStage"), Is.False);
        Assert.That(Command(fixture.Flow, "TryReturnToStageSelect"), Is.False);
        Assert.That(Command(fixture.Flow, "TryPlay"), Is.False);
        Assert.That(Command(fixture.Flow, "TryConfirmPause"), Is.False);
        Assert.That(Screen(fixture), Is.EqualTo("StageClear"));
        Assert.That(Get(fixture.Game.History, "Count"), Is.EqualTo(1));
        Assert.That(Best(fixture.Save, 1), Is.EqualTo(0));
    }
    [Test]
    public void RepairingStorageAndRetryingSaveUnlocksDeparture()
    {
        FlowContext fixture = FlowFixture(); FailClearWrite(fixture);
        Directory.Delete(fixture.Path);
        Do(fixture, "TryRetrySave");
        Assert.That(Get(fixture.Save, "PendingResult"), Is.Null);
        Assert.That(Get(fixture.Flow, "SaveError"), Is.Null);
        Assert.That(Get(fixture.Flow, "CanLeaveClear"), Is.True);
        Assert.That(Get(fixture.Flow, "CanNextStage"), Is.True);
        Assert.That(Best(fixture.Save, 1), Is.EqualTo(1));
        Do(fixture, "TryNextStage");
        Assert.That(Screen(fixture), Is.EqualTo("StageIntro"));
    }
    [Test]
    public void RepeatedSaveFailureRetainsTheSameResultAndProgress()
    {
        FlowContext fixture = FlowFixture(); FailClearWrite(fixture);
        object result = Get(fixture.Flow, "Result"), progress = Get(fixture.Save, "Progress");
        Assert.That(Command(fixture.Flow, "TryRetrySave"), Is.False);
        Assert.That(Get(fixture.Flow, "Result"), Is.SameAs(result));
        Assert.That(Get(fixture.Save, "PendingResult"), Is.SameAs(result));
        Assert.That(Get(fixture.Save, "Progress"), Is.SameAs(progress));
        Assert.That(Get(fixture.Flow, "CanLeaveClear"), Is.False);
    }
    [Test]
    public void CorruptSaveKeepsLobbyUsableAndRequiresExplicitReplacement()
    {
        string path = SavePath(); File.WriteAllText(path, "invalid JSON");
        FlowContext fixture = FlowFixture(path: path);
        Assert.That(Get(fixture.Flow, "CanContinue"), Is.False);
        Assert.That(Get(fixture.Flow, "LastError"), Is.Not.Null);
        Do(fixture, "TryPlay");
        Assert.That(Screen(fixture), Is.EqualTo("NewGameConfirmation"));
        Assert.That(Command(fixture.Flow, "TryContinue"), Is.False);
        Do(fixture, "TryCancel");
        Assert.That(File.ReadAllText(path), Is.EqualTo("invalid JSON"));
        Do(fixture, "TryPlay"); Do(fixture, "TryConfirmNewGame");
        Assert.That(Screen(fixture), Is.EqualTo("StageIntro"));
        Assert.That(Decode(File.ReadAllText(path), 2, out _), Is.True);
    }
    [Test]
    public void LobbyExitOnlyRequestsQuitAfterConfirmationAndEmitsOnce()
    {
        FlowContext fixture = FlowFixture();
        int count = 0; Watch(fixture.Flow, "OnQuitRequested", () => count++);
        Assert.That(Command(fixture.Flow, "TryConfirmExit"), Is.False);
        Do(fixture, "TryRequestExit");
        Assert.That(count, Is.EqualTo(0));
        Do(fixture, "TryCancel");
        Assert.That(Get(fixture.Flow, "QuitRequested"), Is.False);
        Do(fixture, "TryRequestExit"); Do(fixture, "TryConfirmExit");
        Assert.That(Get(fixture.Flow, "QuitRequested"), Is.True);
        Assert.That(count, Is.EqualTo(1));
        Assert.That(Command(fixture.Flow, "TryConfirmExit"), Is.False);
        Assert.That(count, Is.EqualTo(1));
    }
    [Test]
    public void EscapePausesAndResumesMainButCannotSkipIntroOrClear()
    {
        FlowContext fixture = FlowFixture();
        Assert.That(Command(fixture.Flow, "TryHandleEscape"), Is.False);
        Do(fixture, "TryPlay");
        Assert.That(Command(fixture.Flow, "TryHandleEscape"), Is.False);
        Do(fixture, "TryDismissIntro"); Do(fixture, "TryHandleEscape");
        Assert.That(Screen(fixture), Is.EqualTo("PauseConfirmation"));
        Do(fixture, "TryHandleEscape");
        Assert.That(Screen(fixture), Is.EqualTo("Playing"));
        Build(fixture.Game, 1, 0); Do(fixture, "TryCompleteStage");
        Assert.That(Command(fixture.Flow, "TryHandleEscape"), Is.False);
        Assert.That(Screen(fixture), Is.EqualTo("StageClear"));
    }
    [Test]
    public void ResourceCallbackCannotPauseOrReconfigureAnUnfinishedBuild()
    {
        FlowContext fixture = FlowFixture(); Playing(fixture);
        bool observed = false;
        Watch(fixture.Game.Resources, "OnResourcesChanged", () =>
        {
            if (!(bool)Get(fixture.Game.Session, "IsBusy")) return;
            Assert.That(Command(fixture.Flow, "TryRequestPause"), Is.False);
            object[] args = { fixture.Catalog, fixture.Save, fixture.Game.Session, fixture.Game.Database, null };
            Assert.That(Call(fixture.Flow, "TryConfigure", args), Is.False);
            observed = true;
        });
        Build(fixture.Game, 1, 0);
        Assert.That(observed, Is.True);
        Assert.That(Screen(fixture), Is.EqualTo("Playing"));
        Assert.That(Get(fixture.Game.History, "Count"), Is.EqualTo(1));
    }
    [Test]
    public void StageCompletionCallbacksCannotNavigateBeforeResultIsPublished()
    {
        FlowContext fixture = FlowFixture(); Playing(fixture); Build(fixture.Game, 1, 0);
        bool observed = false;
        Watch(fixture.Game.Stage, "OnStateChanged", () =>
        {
            if (!(bool)Get(fixture.Game.Stage, "IsBusy")) return;
            Assert.That(Command(fixture.Flow, "TryRequestPause"), Is.False);
            observed = true;
        });
        Assert.That(Complete(fixture.Game, out _), Is.True);
        Assert.That(observed, Is.True);
        Assert.That(Screen(fixture), Is.EqualTo("StageClear"));
        Assert.That(Best(fixture.Save, 1), Is.EqualTo(1));
    }
    [Test]
    public void DisablingFlowBlocksInputAndReenablingRestoresItsPlayingState()
    {
        FlowContext fixture = FlowFixture(); Playing(fixture);
        fixture.Flow.gameObject.SetActive(false);
        // Ordinary MonoBehaviour callbacks are not automatically dispatched in EditMode.
        // Explicit calls also check handlers that are safe if Unity already dispatched them.
        Lifecycle(fixture.Flow, "OnDisable");
        Assert.That(fixture.Flow.gameObject.activeInHierarchy, Is.False);
        Assert.That(Get(fixture.Game.Session, "GameplayEnabled"), Is.False);
        Assert.That(Command(fixture.Flow, "TryRequestPause"), Is.False);
        object[] build = { 1, Vector2Int.zero, null, null };
        Assert.That(Call(fixture.Game.Session, "TryCommitBuild", build), Is.False);
        fixture.Flow.gameObject.SetActive(true);
        Lifecycle(fixture.Flow, "OnEnable");
        Assert.That(fixture.Flow.gameObject.activeInHierarchy, Is.True);
        Assert.That(Get(fixture.Game.Session, "GameplayEnabled"), Is.True);
        int clearNotifications = 0;
        Watch(fixture.Flow, "OnStateChanged", () => { if (Screen(fixture) == "StageClear") clearNotifications++; });
        Build(fixture.Game, 1, 0); Do(fixture, "TryCompleteStage");
        Assert.That(clearNotifications, Is.EqualTo(1));
    }
    [Test]
    public void LogicalSceneRequestsOnlyChangeBetweenLobbyAndGame()
    {
        FlowContext fixture = FlowFixture();
        int count = 0; WatchOne(fixture.Flow, "OnSceneRequested", () => count++);
        Do(fixture, "TryPlay"); Assert.That(count, Is.EqualTo(1));
        Do(fixture, "TryDismissIntro"); Do(fixture, "TryRequestPause"); Do(fixture, "TryCancel");
        Build(fixture.Game, 1, 0); Do(fixture, "TryCompleteStage"); Do(fixture, "TryRetry");
        Assert.That(count, Is.EqualTo(1));
        Do(fixture, "TryDismissIntro"); Do(fixture, "TryRequestPause"); Do(fixture, "TryConfirmPause");
        Assert.That(count, Is.EqualTo(2));
        Do(fixture, "TryContinue"); Do(fixture, "TryCancel");
        Assert.That(count, Is.EqualTo(2));
    }
    [Test]
    public void ReconfigurationCannotBypassAnUnsavedClearScreen()
    {
        FlowContext fixture = FlowFixture(); FailClearWrite(fixture);
        object[] args = { fixture.Catalog, fixture.Save, fixture.Game.Session, fixture.Game.Database, null };
        Assert.That(Call(fixture.Flow, "TryConfigure", args), Is.False);
        Assert.That(Screen(fixture), Is.EqualTo("StageClear"));
        Assert.That(Get(fixture.Flow, "Result"), Is.Not.Null);
        Assert.That(Get(fixture.Game.Session, "GameplayEnabled"), Is.False);
    }
}
