using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class SaveContractTests
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
                "BuildingPlacementController", "UndoButtonUI", "NextStageButtonUI", "SaveManager" })
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

    [Test]
    public void FreshProgressUnlocksOnlyStageOneAndHasNoRanks()
    {
        object data = Fresh(5);
        Assert.That(Get(data, "HighestUnlockedStage"), Is.EqualTo(1));
        Assert.That((IList)Get(data, "BestRanks"), Is.EqualTo(new[] { 0, 0, 0, 0, 0 }));
        Assert.That(Call(data, "IsStageUnlocked", 1), Is.True);
        Assert.That(Call(data, "IsStageUnlocked", 2), Is.False);
        Assert.That(Call(data, "IsStageUnlocked", 0), Is.False);
        Assert.That(Call(data, "IsStageUnlocked", 6), Is.False);
        object[] args = { 0, null, null };
        Assert.That(RuntimeType("ProgressSaveData").GetMethod("TryCreate").Invoke(null, args), Is.False);
    }

    [Test]
    public void ClearRecordsRankAndUnlocksExactlyTheSuccessor()
    {
        object data = Record(Fresh(), 1, 2);
        Assert.That(Get(data, "HighestUnlockedStage"), Is.EqualTo(2));
        Assert.That((IList)Get(data, "BestRanks"), Is.EqualTo(new[] { 2, 0, 0 }));
        Assert.That(Call(data, "IsStageUnlocked", 3), Is.False);
    }

    [Test]
    public void LowerReplayNeverReducesHighestRankOrUnlockProgress()
    {
        object data = Record(Record(Fresh(), 1, 3), 2, 2);
        data = Record(data, 1, 1);
        Assert.That(Get(data, "HighestUnlockedStage"), Is.EqualTo(3));
        Assert.That((IList)Get(data, "BestRanks"), Is.EqualTo(new[] { 3, 2, 0 }));
    }

    [Test]
    public void HigherReplayImprovesOnlyThatStagesBestRank()
    {
        object data = Record(Record(Fresh(), 1, 1), 2, 2);
        data = Record(data, 1, 3);
        Assert.That((IList)Get(data, "BestRanks"), Is.EqualTo(new[] { 3, 2, 0 }));
        Assert.That(Get(data, "HighestUnlockedStage"), Is.EqualTo(3));
    }

    [Test]
    public void LockedStagesAndRanksOutsideOneThroughThreeAreRejected()
    {
        object data = Fresh();
        foreach (int[] pair in new[] { new[] { 2, 1 }, new[] { 0, 1 }, new[] { 4, 1 },
            new[] { 1, 0 }, new[] { 1, 4 }, new[] { 1, -1 } })
        {
            object[] args = { pair[0], pair[1], null, null };
            Assert.That(Call(data, "TryRecordClear", args), Is.False);
            Assert.That(args[2], Is.Null);
            Assert.That(args[3], Is.Not.Null);
        }
        Assert.That((IList)Get(data, "BestRanks"), Is.EqualTo(new[] { 0, 0, 0 }));
    }

    [Test]
    public void FinalStageClearDoesNotUnlockBeyondConfiguredStages()
    {
        object data = Record(Fresh(1), 1, 3);
        Assert.That(Get(data, "HighestUnlockedStage"), Is.EqualTo(1));
        Assert.That(Call(data, "IsStageUnlocked", 2), Is.False);
        data = Record(Record(Record(Fresh(), 1, 1), 2, 2), 3, 3);
        Assert.That(Get(data, "HighestUnlockedStage"), Is.EqualTo(3));
        Assert.That((IList)Get(data, "BestRanks"), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void NewProgressDoesNotMutateOldProgressAndRanksAreReadOnly()
    {
        object original = Fresh();
        object next = Record(original, 1, 3);
        Assert.That((IList)Get(original, "BestRanks"), Is.EqualTo(new[] { 0, 0, 0 }));
        Assert.That((IList)Get(next, "BestRanks"), Is.EqualTo(new[] { 3, 0, 0 }));
        Assert.Throws<NotSupportedException>(() => ((IList)Get(next, "BestRanks"))[0] = 0);
    }

    [Test]
    public void CodecRoundTripPreservesUnlockAndEachBestRank()
    {
        object progress = Record(Record(Fresh(), 1, 3), 2, 1);
        Assert.That(Decode(Encode(progress), 3, out object decoded), Is.True);
        Assert.That(Get(decoded, "HighestUnlockedStage"), Is.EqualTo(3));
        Assert.That((IList)Get(decoded, "BestRanks"), Is.EqualTo(new[] { 3, 1, 0 }));
    }

    [Test]
    public void JsonContainsOnlyVersionStageCountUnlockAndRanks()
    {
        string json = Encode(Record(Fresh(), 1, 1));
        foreach (string field in new[] { "version", "stageCount", "highestUnlockedStage", "bestRanks" })
            Assert.That(json, Does.Contain("\"" + field + "\""));
        foreach (string field in new[] { "board", "resources", "cards", "history", "combos", "instanceID", "goalStates" })
            Assert.That(json.ToLowerInvariant(), Does.Not.Contain("\"" + field.ToLowerInvariant() + "\""));
        Assert.That(json.Split(':').Length - 1, Is.EqualTo(4));
    }

    [Test]
    public void UnsupportedVersionOrSmallerStageCatalogCannotLoad()
    {
        foreach (string json in new[] { Json(version: 0), Json(version: 2), Json(count: 4, ranks: "[0,0,0,0]") })
        {
            Assert.That(Decode(json, 3, out object data), Is.False);
            Assert.That(data, Is.Null);
        }
    }

    [TestCase(5)]
    [TestCase(10)]
    public void ExpandedCatalogPreservesIncompleteFrontierAndPadsNewRanks(int count)
    {
        Assert.That(Decode(Json(count: 2, unlocked: 2, ranks: "[3,0]"), count, out object data), Is.True);
        Assert.That(Get(data, "StageCount"), Is.EqualTo(count));
        Assert.That(Get(data, "HighestUnlockedStage"), Is.EqualTo(2));
        var expected = new int[count]; expected[0] = 3;
        Assert.That((IList)Get(data, "BestRanks"), Is.EqualTo(expected));
        Assert.That(Call(data, "IsStageUnlocked", 3), Is.False);
    }

    [TestCase(5)]
    [TestCase(10)]
    public void ClearedOldFinalStageUnlocksOnlyTheFirstAppendedStage(int count)
    {
        Assert.That(Decode(Json(count: 2, unlocked: 2, ranks: "[3,2]"), count, out object data), Is.True);
        Assert.That(Get(data, "HighestUnlockedStage"), Is.EqualTo(3));
        var expected = new int[count]; expected[0] = 3; expected[1] = 2;
        Assert.That((IList)Get(data, "BestRanks"), Is.EqualTo(expected));
        Assert.That(Call(data, "IsStageUnlocked", 3), Is.True);
        Assert.That(Call(data, "IsStageUnlocked", 4), Is.False);
    }

    [TestCase(1, "[0]", 1)]
    [TestCase(1, "[3]", 2)]
    public void SingleStageSavesExpandAccordingToWhetherTheirFinalStageWasCleared(int oldCount, string ranks, int unlocked)
    {
        Assert.That(Decode(Json(count: oldCount, unlocked: 1, ranks: ranks), 10, out object data), Is.True);
        Assert.That(Get(data, "HighestUnlockedStage"), Is.EqualTo(unlocked));
    }

    [Test]
    public void FiveCompletedStagesExpandToTenWithoutUnlockingTheEntireCatalog()
    {
        Assert.That(Decode(Json(count: 5, unlocked: 5, ranks: "[1,2,3,2,1]"), 10, out object data), Is.True);
        Assert.That(Get(data, "HighestUnlockedStage"), Is.EqualTo(6));
        Assert.That((IList)Get(data, "BestRanks"), Is.EqualTo(new[] { 1,2,3,2,1,0,0,0,0,0 }));
        Assert.That(Call(data, "IsStageUnlocked", 7), Is.False);
    }

    [Test]
    public void ExpansionDoesNotMutateTheOriginalProgressOrItsRanks()
    {
        object original = Record(Record(Fresh(2), 1, 3), 2, 2);
        object[] args = { 10, null, null };
        Assert.That(Call(original, "TryExpandStages", args), Is.True, args[2] as string);
        Assert.That(Get(original, "StageCount"), Is.EqualTo(2));
        Assert.That((IList)Get(original, "BestRanks"), Is.EqualTo(new[] { 3,2 }));
        Assert.That(Get(args[1], "HighestUnlockedStage"), Is.EqualTo(3));
        Assert.Throws<NotSupportedException>(() => ((IList)Get(args[1], "BestRanks"))[0] = 0);
    }

    [Test]
    public void MatchingCatalogDoesNotChangeCompletedFinalStageProgress()
    {
        object original = Record(Record(Fresh(2), 1, 3), 2, 2);
        object[] args = { 2, null, null };
        Assert.That(Call(original, "TryExpandStages", args), Is.True);
        Assert.That(args[1], Is.SameAs(original));
        Assert.That(Decode(Encode(original), 2, out object decoded), Is.True);
        Assert.That(Get(decoded, "HighestUnlockedStage"), Is.EqualTo(2));
    }

    [TestCase(0)]
    [TestCase(1)]
    public void ExpansionRejectsSmallerOrInvalidCatalogWithoutMutatingProgress(int count)
    {
        object original = Fresh(2);
        object[] args = { count, null, null };
        Assert.That(Call(original, "TryExpandStages", args), Is.False);
        Assert.That(args[1], Is.Null); Assert.That(args[2], Is.Not.Null);
        Assert.That(Get(original, "StageCount"), Is.EqualTo(2));
    }

    [TestCase(2, 2, "[0,3]")]
    [TestCase(2, 1, "[3,0]")]
    [TestCase(2, 2, "[3,4]")]
    [TestCase(2, 2, "[3]")]
    [TestCase(2, 3, "[3,2]")]
    [TestCase(0, 1, "[]")]
    public void ExpansionRejectsMalformedOriginalProgressBeforeAddingNewStages(int count, int unlocked, string ranks)
    {
        Assert.That(Decode(Json(count: count, unlocked: unlocked, ranks: ranks), 10, out object data), Is.False);
        Assert.That(data, Is.Null);
    }

    [Test]
    public void MigratedProgressRoundTripIsIdempotentAndKeepsTheExistingJsonSchema()
    {
        Assert.That(Decode(Json(count: 2, unlocked: 2, ranks: "[3,2]"), 10, out object first), Is.True);
        string encoded = Encode(first);
        Assert.That(Decode(encoded, 10, out object second), Is.True);
        Assert.That(Get(second, "HighestUnlockedStage"), Is.EqualTo(3));
        Assert.That(Encode(second), Is.EqualTo(encoded));
        Assert.That(encoded.Split(':').Length - 1, Is.EqualTo(4));
    }

    [Test]
    public void ExpandedSaveManagerLoadsReadOnlyThenPersistsNewStageProgress()
    {
        string path = SavePath();
        string original = Json(count: 2, unlocked: 2, ranks: "[3,2]");
        File.WriteAllText(path, original);
        Component manager = SaveManager(path, 10);
        Assert.That(Get(manager, "CanContinue"), Is.True);
        Assert.That(File.ReadAllText(path), Is.EqualTo(original));
        Assert.That(Save(manager, 3, 1), Is.True);
        Assert.That(Decode(File.ReadAllText(path), 10, out object stored), Is.True);
        Assert.That(Get(stored, "StageCount"), Is.EqualTo(10));
        Assert.That(Get(stored, "HighestUnlockedStage"), Is.EqualTo(4));
        Component reloaded = SaveManager(path, 10);
        Assert.That(Best(reloaded, 1), Is.EqualTo(3)); Assert.That(Best(reloaded, 2), Is.EqualTo(2));
        Assert.That(Best(reloaded, 3), Is.EqualTo(1));
        Assert.That(Directory.GetFiles(Path.GetDirectoryName(path), "*.tmp"), Is.Empty);
    }

    [Test]
    public void CorruptOlderSaveCannotBeSilentlyExpandedOrRewritten()
    {
        string path = SavePath(); string invalid = Json(count: 2, unlocked: 2, ranks: "[0,3]");
        File.WriteAllText(path, invalid);
        Component manager = SaveManager(path, 10, false);
        Assert.That(Command(manager, "TryLoad"), Is.False);
        Assert.That(Get(manager, "CanContinue"), Is.False);
        Assert.That(File.ReadAllText(path), Is.EqualTo(invalid));
    }

    [Test]
    public void FailedExpandedProgressWriteRetainsOriginalSaveAndCanRetry()
    {
        string path = SavePath(); string original = Json(count: 2, unlocked: 2, ranks: "[3,2]");
        File.WriteAllText(path, original); Component manager = SaveManager(path, 10);
        File.Move(path, path + ".old"); Directory.CreateDirectory(path);
        object before = Get(manager, "Progress");
        Assert.That(Save(manager, 3, 1), Is.False);
        Assert.That(Get(manager, "Progress"), Is.SameAs(before));
        Assert.That(File.ReadAllText(path + ".old"), Is.EqualTo(original));
        Directory.Delete(path); File.Move(path + ".old", path);
        Assert.That(Save(manager, 3, 1), Is.True);
        Assert.That(Get(Get(SaveManager(path, 10), "Progress"), "HighestUnlockedStage"), Is.EqualTo(4));
    }

    [Test]
    public void InvalidUnlockSkippedClearsAndInvalidRanksCannotLoad()
    {
        foreach (string json in new[] { Json(unlocked: 0), Json(unlocked: 4),
            Json(unlocked: 2), Json(ranks: "[3,0,0]"), Json(ranks: "[-1,0,0]"),
            Json(ranks: "[4,0,0]"), Json(ranks: "[0,1,0]") })
            Assert.That(Decode(json, 3, out _), Is.False, json);
    }

    [Test]
    public void EmptyMalformedAndMissingRequiredDocumentsAreRejected()
    {
        foreach (string json in new[] { null, "", "[]", "{}", "{", "{\"version\":1}",
            "{\"version\":1,\"stageCount\":3,\"highestUnlockedStage\":1}",
            Json(ranks: "null"), Json(ranks: "[0,0]"), "{ not valid json }" })
            Assert.That(Decode(json, 3, out _), Is.False);
    }

    [Test]
    public void MissingFileReadReturnsMissingWithoutCreatingAnyFile()
    {
        string path = SavePath();
        object[] args = { null, false, null };
        Assert.That(Call(FileStore(path), "TryRead", args), Is.True);
        Assert.That(args[0], Is.Null);
        Assert.That(args[1], Is.False);
        Assert.That(File.Exists(path), Is.False);
        Assert.That(Directory.GetFiles(Path.GetDirectoryName(path)), Is.Empty);
    }

    [Test]
    public void FileStoreCreatesAndReplacesCompleteUtf8ContentWithoutTemporaryFiles()
    {
        string path = SavePath();
        object store = FileStore(path);
        Assert.That(Write(store, "첫 저장"), Is.True);
        Assert.That(Write(store, "두 번째 저장"), Is.True);
        Assert.That(File.ReadAllText(path), Is.EqualTo("두 번째 저장"));
        Assert.That(Directory.GetFiles(Path.GetDirectoryName(path), "*.tmp"), Is.Empty);
        object[] args = { null, false, null };
        Assert.That(Call(store, "TryRead", args), Is.True);
        Assert.That(args[0], Is.EqualTo("두 번째 저장"));
        Assert.That(args[1], Is.True);
    }

    [Test]
    public void FileStoreRejectsBlockedParentAndLeavesExistingDataUntouched()
    {
        string path = SavePath();
        File.WriteAllText(path, "existing data");
        object store = FileStore(Path.Combine(path, "progress.json"));
        Assert.That(Write(store, "replacement"), Is.False);
        Assert.That(File.ReadAllText(path), Is.EqualTo("existing data"));
        File.WriteAllBytes(path, new byte[] { 255 });
        object[] read = { null, false, null };
        Assert.That(Call(FileStore(path), "TryRead", read), Is.False);
        Assert.That(read[2], Is.Not.Null);
        Assert.That(File.ReadAllBytes(path), Is.EqualTo(new byte[] { 255 }));
    }

    [Test]
    public void LoadedMissingProgressAllowsPlayButDisablesContinueAndConfirmation()
    {
        Component manager = SaveManager();
        Assert.That(Get(manager, "IsLoaded"), Is.True);
        Assert.That(Get(manager, "HasProgress"), Is.False);
        Assert.That(Get(manager, "CanPlay"), Is.True);
        Assert.That(Get(manager, "CanContinue"), Is.False);
        Assert.That(Get(manager, "RequiresNewGameConfirmation"), Is.False);
        Assert.That(File.Exists((string)Get(manager, "FilePath")), Is.False);
        object before = Get(manager, "Progress");
        string originalPath = (string)Get(manager, "FilePath");
        object[] invalid = { 3, "", null };
        Assert.That(Call(manager, "TryConfigure", invalid), Is.False);
        Assert.That(Get(manager, "Progress"), Is.SameAs(before));
        Assert.That(Get(manager, "FilePath"), Is.EqualTo(originalPath));
        Assert.That(Get(manager, "IsLoaded"), Is.True);
    }

    [Test]
    public void NewGameImmediatelyPersistsFreshProgressAndEnablesContinue()
    {
        string path = SavePath();
        Component manager = SaveManager(path);
        Assert.That(Command(manager, "TryStartNewGame"), Is.True);
        Assert.That(Get(manager, "HasProgress"), Is.True);
        Assert.That(Get(manager, "RequiresNewGameConfirmation"), Is.True);
        Assert.That(Get(manager, "CanContinue"), Is.True);
        Component reloaded = SaveManager(path);
        Assert.That(Get(reloaded, "CanContinue"), Is.True);
        Assert.That(Get(Get(reloaded, "Progress"), "HighestUnlockedStage"), Is.EqualTo(1));
        Assert.That(Best(reloaded, 1), Is.Zero);
    }

    [Test]
    public void FreshManagerLoadsSavedUnlocksAndBestRanks()
    {
        string path = SavePath();
        Component manager = SaveManager(path);
        Assert.That(Save(manager, 1, 3), Is.True);
        Assert.That(Save(manager, 2, 2), Is.True);
        Component reloaded = SaveManager(path);
        Assert.That(Best(reloaded, 1), Is.EqualTo(3));
        Assert.That(Best(reloaded, 2), Is.EqualTo(2));
        Assert.That(Get(Get(reloaded, "Progress"), "HighestUnlockedStage"), Is.EqualTo(3));
    }

    [Test]
    public void InvalidClearLeavesMemoryDiskAndProgressNotificationsUnchanged()
    {
        string path = SavePath();
        Component manager = SaveManager(path);
        int changes = 0;
        Watch(manager, "OnProgressChanged", () => changes++);
        object before = Get(manager, "Progress");
        Assert.That(Save(manager, 2, 3), Is.False);
        Assert.That(Save(manager, 1, 0), Is.False);
        object[] missing = { null, null };
        Assert.That(Call(manager, "TryRecordStageResult", missing), Is.False);
        Assert.That(Get(manager, "Progress"), Is.SameAs(before));
        Assert.That(File.Exists(path), Is.False);
        Assert.That(changes, Is.Zero);
    }

    [Test]
    public void NewGameResetsUnlocksAndRanksWithoutDeletingUnrelatedFiles()
    {
        string path = SavePath();
        string unrelated = Path.Combine(Path.GetDirectoryName(path), "unrelated.txt");
        File.WriteAllText(unrelated, "keep");
        Component manager = SaveManager(path);
        Assert.That(Save(manager, 1, 3), Is.True);
        Assert.That(Save(manager, 2, 2), Is.True);
        Assert.That(Command(manager, "TryStartNewGame"), Is.True);
        Assert.That(Get(Get(manager, "Progress"), "HighestUnlockedStage"), Is.EqualTo(1));
        Assert.That((IList)Get(Get(manager, "Progress"), "BestRanks"), Is.EqualTo(new[] { 0, 0, 0 }));
        Assert.That(File.ReadAllText(unrelated), Is.EqualTo("keep"));
    }

    [Test]
    public void CorruptLoadPreservesCurrentProgressAndOriginalBytes()
    {
        string path = SavePath();
        Component manager = SaveManager(path);
        Assert.That(Save(manager, 1, 3), Is.True);
        object before = Get(manager, "Progress");
        string bad = Json(version: 99);
        File.WriteAllText(path, bad);
        int failures = 0;
        manager.GetType().GetEvent("OnSaveFailed").AddEventHandler(manager, new Action<string>(_ => failures++));
        Assert.That(Command(manager, "TryLoad"), Is.False);
        Assert.That(Get(manager, "Progress"), Is.SameAs(before));
        Assert.That(Get(manager, "CanContinue"), Is.False);
        Assert.That(Get(manager, "LastError"), Is.Not.Null);
        Assert.That(File.ReadAllText(path), Is.EqualTo(bad));
        Assert.That(failures, Is.EqualTo(1));
    }

    [Test]
    public void FailedWritePreservesMemoryReportsFailureAndCanRetry()
    {
        string path = SavePath();
        Component manager = SaveManager(path);
        Assert.That(Save(manager, 1, 1), Is.True);
        string old = File.ReadAllText(path);
        File.Move(path, path + ".old");
        Directory.CreateDirectory(path);
        object before = Get(manager, "Progress");
        int changes = 0, failures = 0;
        Watch(manager, "OnProgressChanged", () => changes++);
        manager.GetType().GetEvent("OnSaveFailed").AddEventHandler(manager, new Action<string>(_ => failures++));
        Assert.That(Save(manager, 1, 3), Is.False);
        Assert.That(Get(manager, "Progress"), Is.SameAs(before));
        Assert.That(File.ReadAllText(path + ".old"), Is.EqualTo(old));
        Assert.That(changes, Is.Zero);
        Assert.That(failures, Is.EqualTo(1));
        Assert.That(Directory.GetFiles(Path.GetDirectoryName(path), "*.tmp"), Is.Empty);
        Directory.Delete(path);
        File.Move(path + ".old", path);
        Assert.That(Save(manager, 1, 3), Is.True);
        Assert.That(Best(manager, 1), Is.EqualTo(3));
        Assert.That(Get(manager, "LastError"), Is.Null);
    }

    [Test]
    public void ActualStageCompletionSavesRankAndUnlockBeforeProgressNotification()
    {
        Context context = Fixture();
        Component manager = SaveManager();
        Bind(manager, context);
        int changes = 0;
        Watch(manager, "OnProgressChanged", () =>
        {
            changes++;
            Assert.That(Best(manager, 1), Is.EqualTo(2));
            Assert.That(Get(Get(manager, "Progress"), "HighestUnlockedStage"), Is.EqualTo(2));
            Assert.That(Get(manager, "PendingResult"), Is.Null);
            Assert.That(Get(manager, "CanContinue"), Is.True);
        });
        Build(context, 1, 0); Build(context, 3, 1);
        Assert.That(Complete(context, out _), Is.True);
        Assert.That(changes, Is.EqualTo(1));
    }

    [Test]
    public void ReadinessAndSnapshotRestoreDoNotSaveCompletion()
    {
        Context context = Fixture();
        Component manager = SaveManager();
        Bind(manager, context);
        Build(context, 1, 0); Build(context, 3, 1);
        Assert.That(Get(context.Stage, "NextStageAvailable"), Is.True);
        Assert.That(File.Exists((string)Get(manager, "FilePath")), Is.False);
        Assert.That(Call(context.Stage, "TryApplyProgressState",
            Progress(true, 2, false, true, true, false)), Is.True);
        Assert.That(Get(context.Stage, "IsCleared"), Is.True);
        Assert.That(File.Exists((string)Get(manager, "FilePath")), Is.False);
    }

    [Test]
    public void UndoAfterSavedCompletionKeepsPermanentBestRankAndUnlock()
    {
        Context context = Fixture();
        Component manager = SaveManager();
        Bind(manager, context);
        Build(context, 1, 0); Build(context, 3, 1);
        Assert.That(Complete(context, out _), Is.True);
        string saved = File.ReadAllText((string)Get(manager, "FilePath"));
        Assert.That(Undo(context, out string error), Is.True, error);
        Assert.That(Get(context.Stage, "Rank"), Is.EqualTo(1));
        Assert.That(Best(manager, 1), Is.EqualTo(2));
        Assert.That(Get(Get(manager, "Progress"), "HighestUnlockedStage"), Is.EqualTo(2));
        Assert.That(File.ReadAllText((string)Get(manager, "FilePath")), Is.EqualTo(saved));
        Assert.That(Complete(context, out _), Is.True);
        Assert.That(Best(manager, 1), Is.EqualTo(2));
    }

    [Test]
    public void FailedCompletionRetainsPendingResultForExplicitRetry()
    {
        Context context = Fixture();
        Component manager = SaveManager();
        string path = (string)Get(manager, "FilePath");
        Bind(manager, context);
        Directory.CreateDirectory(path);
        manager.GetType().GetEvent("OnSaveFailed").AddEventHandler(manager, new Action<string>(_ =>
            Assert.That(Get(manager, "PendingResult"), Is.Not.Null)));
        Build(context, 1, 0);
        Assert.That(Complete(context, out object result), Is.True);
        Assert.That(Get(manager, "PendingResult"), Is.SameAs(result));
        Assert.That(Best(manager, 1), Is.Zero);
        Assert.That(Command(manager, "TryLoad"), Is.False);
        Assert.That(Get(manager, "PendingResult"), Is.SameAs(result));
        Directory.Delete(path);
        Assert.That(Command(manager, "TrySavePendingResult"), Is.True);
        Assert.That(Get(manager, "PendingResult"), Is.Null);
        Assert.That(Best(manager, 1), Is.EqualTo(1));
        Assert.That(Get(manager, "CanContinue"), Is.True);
    }

    [Test]
    public void InvalidStageBindingPreservesExistingCompletionSubscription()
    {
        Context context = Fixture();
        Component manager = SaveManager();
        Bind(manager, context);
        Context locked = Fixture(stageNumber: 2);
        object[] args = { locked.Stage, null };
        Assert.That(Call(manager, "TryBindStage", args), Is.False);
        args = new object[] { Component("Unconfigured stage", "StageManager"), null };
        Assert.That(Call(manager, "TryBindStage", args), Is.False);
        Build(context, 1, 0);
        Assert.That(Complete(context, out _), Is.True);
        Assert.That(Best(manager, 1), Is.EqualTo(1));
    }

    [Test]
    public void RepeatedOrLowerResultDoesNotRewriteOrRepublishProgress()
    {
        Context context = Fixture();
        Component manager = SaveManager();
        Bind(manager, context);
        Build(context, 1, 0);
        Assert.That(Complete(context, out object result), Is.True);
        int changes = 0;
        Watch(manager, "OnProgressChanged", () => changes++);
        string saved = File.ReadAllText((string)Get(manager, "FilePath"));
        object[] args = { result, null };
        Assert.That(Call(manager, "TryRecordStageResult", args), Is.True);
        Assert.That(Save(manager, 1, 1), Is.True);
        Assert.That(changes, Is.Zero);
        Assert.That(File.ReadAllText((string)Get(manager, "FilePath")), Is.EqualTo(saved));
    }

    [Test]
    public void DisabledManagerUnsubscribesAndDoesNotBackfillOnEnable()
    {
        Context context = Fixture();
        Component manager = SaveManager();
        Bind(manager, context);
        Lifecycle(manager, "OnDisable");
        Build(context, 1, 0);
        Assert.That(Complete(context, out object result), Is.True);
        Assert.That(Get(manager, "HasProgress"), Is.False);
        Lifecycle(manager, "OnEnable");
        Assert.That(Get(manager, "HasProgress"), Is.False);
        object[] args = { result, null };
        Assert.That(Call(manager, "TryRecordStageResult", args), Is.True);
        Assert.That(Get(manager, "HasProgress"), Is.True);
    }

    [Test]
    public void ProgressNotificationsRejectReentrantLoadNewGameRecordAndConfiguration()
    {
        Component manager = SaveManager();
        int checks = 0;
        Watch(manager, "OnProgressChanged", () =>
        {
            checks++;
            Assert.That(Command(manager, "TryLoad"), Is.False);
            Assert.That(Command(manager, "TryStartNewGame"), Is.False);
            Assert.That(Save(manager, 1, 3), Is.False);
            object[] args = { 3, (string)Get(manager, "FilePath"), null };
            Assert.That(Call(manager, "TryConfigure", args), Is.False);
        });
        Assert.That(Save(manager, 1, 2), Is.True);
        Assert.That(checks, Is.EqualTo(1));
    }

    [Test]
    public void NewGameDoesNotResetRunningBoardAndClearsPendingResult()
    {
        Context context = Fixture();
        Component manager = SaveManager();
        Bind(manager, context);
        string path = (string)Get(manager, "FilePath");
        Directory.CreateDirectory(path);
        Build(context, 1, 0);
        Assert.That(Complete(context, out _), Is.True);
        Assert.That(Get(manager, "PendingResult"), Is.Not.Null);
        object building = Get(Tile(context, Vector2Int.zero), "Building");
        Directory.Delete(path);
        Assert.That(Command(manager, "TryStartNewGame"), Is.True);
        Assert.That(Get(manager, "PendingResult"), Is.Null);
        Assert.That(Get(Tile(context, Vector2Int.zero), "Building"), Is.SameAs(building));
        Assert.That(Get(context.History, "Count"), Is.EqualTo(1));
        Assert.That(Get(context.Stage, "IsCleared"), Is.True);
        Assert.That(Best(manager, 1), Is.Zero);
    }

    [Test]
    public void FailedInitialLoadBlocksContinueAndWritesUntilExplicitNewGame()
    {
        string path = SavePath();
        File.WriteAllText(path, "{broken");
        Component manager = SaveManager(path, load: false);
        Assert.That(Command(manager, "TryLoad"), Is.False);
        Assert.That(Get(manager, "CanContinue"), Is.False);
        Assert.That(Get(manager, "RequiresNewGameConfirmation"), Is.True);
        Assert.That(Save(manager, 1, 3), Is.False);
        Assert.That(File.ReadAllText(path), Is.EqualTo("{broken"));
        Assert.That(Command(manager, "TryStartNewGame"), Is.True);
        Assert.That(Get(manager, "CanContinue"), Is.True);
        Assert.That(Decode(File.ReadAllText(path), 3, out _), Is.True);
    }
}
