using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class TurnHistoryContractTests
{
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
    private Context Fixture(bool withStage = true)
    {
        var context = new Context();
        context.A = Building(1001); context.B = Building(2001); context.C = Building(3001);
        Component tileTemplate = Component("Tile template", "Tile");
        context.TileData = Asset("TileData");
        Set(context.TileData, "tileType", Enum.Parse(RuntimeType("TileType"), "Grass"));
        Set(context.TileData, "tilePrefab", tileTemplate);
        context.StageData = Asset("StageData");
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
            Set(context.Stage, "resourceManager", context.Resources);
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
                "BuildingPlacementController", "UndoButtonUI" })
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
    }

    [Test]
    public void HistoryRejectsMissingOrUninitializedSessionWithoutAttaching()
    {
        Component history = Component("History", "TurnHistoryManager");
        object[] missing = { null, null, null };
        Assert.That(Call(history, "TryConfigure", missing), Is.False);
        Component session = Component("Uninitialized session", "GameSessionManager");
        object[] args = { session, null, null };
        Assert.That(Call(history, "TryConfigure", args), Is.False);
        Assert.That(Get(session, "History"), Is.Null);
        Assert.That(Get(history, "Count"), Is.Zero);
    }

    [Test]
    public void WrongStageReferenceRejectsReconfigurationAndPreservesHistory()
    {
        Context context = Fixture();
        Build(context, 1, 0);
        Component foreignStage = Component("Foreign stage", "StageManager");
        Set(foreignStage, "resourceManager", Component("Foreign resources", "ResourceManager"));
        object snapshot = Get(context.History, "CurrentSnapshot");
        object[] args = { context.Session, foreignStage, null };
        Assert.That(Call(context.History, "TryConfigure", args), Is.False);
        Assert.That(Get(context.History, "CurrentSnapshot"), Is.SameAs(snapshot));
        Assert.That(Get(context.Session, "History"), Is.SameAs(context.History));
    }

    [Test]
    public void InitialAndFirstBuildCannotUndoButSecondBuildCan()
    {
        Context context = Fixture();
        Assert.That(Get(context.History, "Count"), Is.Zero);
        Assert.That(Undo(context, out _), Is.False);
        Build(context, 1, 0);
        Assert.That(Get(context.History, "Count"), Is.EqualTo(1));
        Assert.That(Get(context.History, "CanUndo"), Is.False);
        Assert.That(Undo(context, out _), Is.False);
        Build(context, 3, 1);
        Assert.That(Get(context.History, "Count"), Is.EqualTo(2));
        Assert.That(Get(context.History, "CanUndo"), Is.True);
    }

    [Test]
    public void RepeatedUndoRestoresCompletedTurnsAndStopsAtFirstBuilding()
    {
        Context context = Fixture();
        Build(context, 1, 0); Build(context, 3, 1); Build(context, 5, 2);
        Assert.That(ResourceValues(context), Is.EqualTo(new[] { 5, 6, 4, 2, 0 }));
        Assert.That(Undo(context, out string error), Is.True, error);
        Assert.That(ResourceValues(context), Is.EqualTo(new[] { 4, 8, 4, 2, 0 }));
        Assert.That(Get(Tile(context, new Vector2Int(2, 0)), "IsOccupied"), Is.False);
        Assert.That(((IList)Get(context.Combo, "Results")).Count, Is.EqualTo(1));
        Assert.That(Undo(context, out error), Is.True, error);
        Assert.That(ResourceValues(context), Is.EqualTo(new[] { 3, 9, 2, 2, 0 }));
        Assert.That(Get(Tile(context, Vector2Int.zero), "IsOccupied"), Is.True);
        Assert.That(Get(Tile(context, Vector2Int.right), "IsOccupied"), Is.False);
        Assert.That((IList)Get(context.Combo, "Results"), Is.Empty);
        Assert.That(CardIds(context), Is.EqualTo(new[] { 2, 3, 4, 5, 6 }));
        Assert.That(Undo(context, out _), Is.False);
        Assert.That(Get(context.History, "Count"), Is.EqualTo(1));
    }

    [Test]
    public void UndoReturnsMiddleCardToItsOriginalOrderAndRestoresAvailability()
    {
        Context context = Fixture();
        Build(context, 3, 0); Build(context, 5, 1);
        Assert.That(CardIds(context), Is.EqualTo(new[] { 1, 2, 4, 6 }));
        Assert.That(Undo(context, out string error), Is.True, error);
        Assert.That(CardIds(context), Is.EqualTo(new[] { 1, 2, 4, 5, 6 }));
        Assert.That(Call(context.Hand, "IsCardAvailable", 5), Is.True);
        Assert.That(Get(Get(Tile(context, Vector2Int.zero), "Building"), "Data"), Is.SameAs(context.B));
    }

    [Test]
    public void FirstRestoreNotificationSeesAllFinalStateAndShortenedHistory()
    {
        Context context = Fixture();
        Build(context, 1, 0);
        StageState(context, true, 2, true, true, true, false);
        Build(context, 3, 1);
        StageState(context, true, 3, true, true, true, true);
        Build(context, 5, 2);
        int resources = 0, hand = 0, combos = 0, stage = 0, history = 0, restored = 0;
        Action assertState = () =>
        {
            Assert.That(ResourceValues(context), Is.EqualTo(new[] { 4, 8, 4, 2, 0 }));
            Assert.That(CardIds(context), Is.EqualTo(new[] { 2, 4, 5, 6 }));
            Assert.That(Get(context.History, "Count"), Is.EqualTo(2));
            Assert.That(Get(Tile(context, new Vector2Int(2, 0)), "IsOccupied"), Is.False);
            Assert.That(((IList)Get(context.Combo, "Results")).Count, Is.EqualTo(1));
            Assert.That(Get(context.Combo, "CurrentPresentation"), Is.Null);
            object progress = Call(context.Stage, "CaptureProgressState");
            Assert.That(Get(progress, "Rank"), Is.EqualTo(2));
            Assert.That(Get(progress, "NextStageAvailable"), Is.True);
            Assert.That((IList)Get(progress, "GoalStates"), Is.EqualTo(new[] { true, true, false }));
        };
        WatchPair(context.Resources, "OnResourceChanged", () => { resources++; assertState(); });
        Watch(context.Hand, "OnHandChanged", () => { hand++; assertState(); });
        Watch(context.Combo, "OnResultsChanged", () => { combos++; assertState(); });
        Watch(context.Stage, "OnStateChanged", () => { stage++; assertState(); });
        Watch(context.History, "OnHistoryChanged", () => { history++; assertState(); });
        Watch(context.Session, "OnStateRestored", () => { restored++; assertState(); });
        Assert.That(Undo(context, out string error), Is.True, error);
        Assert.That(new[] { resources, hand, combos, stage, history, restored }, Is.EqualTo(new[] { 5, 1, 1, 1, 1, 1 }));
    }

    [Test]
    public void UndoRestoresGoalRankNextStageAndHidesPreviouslyClearedUI()
    {
        Context context = Fixture();
        Component clearUI = Component("Clear UI", "ClearUI");
        GameObject textRoot = Object("Clear text", false, true);
        textRoot.transform.SetParent(clearUI.transform, false);
        Component text = textRoot.AddComponent(Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true));
        Set(clearUI, "stageManager", context.Stage); Set(clearUI, "clearText", text);
        Lifecycle(clearUI, "Awake"); Lifecycle(clearUI, "Start");
        StageState(context, false, 0, false, false, false, false);
        Build(context, 1, 0);
        StageState(context, true, 2, true, true, false, true);
        Build(context, 3, 1);
        Assert.That(textRoot.activeSelf, Is.True);
        Assert.That(Undo(context, out string error), Is.True, error);
        object progress = Call(context.Stage, "CaptureProgressState");
        Assert.That(Get(progress, "IsCleared"), Is.False);
        Assert.That((IList)Get(progress, "GoalStates"), Is.EqualTo(new[] { false, false, false }));
        Assert.That(Get(progress, "Rank"), Is.Zero);
        Assert.That(Get(progress, "NextStageAvailable"), Is.False);
        Assert.That(textRoot.activeSelf, Is.False);
    }

    [Test]
    public void CapturedCollectionsAndGoalInputsCannotMutateCompletedTurn()
    {
        Context context = Fixture();
        bool[] goals = { true, false, false };
        object progress = Progress(false, 1, false, goals);
        goals[0] = false;
        Call(context.Stage, "TryApplyProgressState", progress);
        Build(context, 1, 0);
        object snapshot = Get(context.History, "CurrentSnapshot");
        foreach (string property in new[] { "Buildings", "Resources", "Cards", "Combos", "LastBuildCombos" })
            Assert.Throws<NotSupportedException>(() => ((IList)Get(snapshot, property)).Clear());
        Assert.That(((IList)Get(Get(snapshot, "StageProgress"), "GoalStates"))[0], Is.True);
        Call(context.Resources, "Add", Resource("Tourism"), 7);
        Assert.That(Get(((IList)Get(snapshot, "Resources"))[4], "Amount"), Is.Zero);
        Assert.That(((IList)Get(snapshot, "Buildings")).Count, Is.EqualTo(1));
    }

    [Test]
    public void RejectedBuildDoesNotAppendSnapshotOrEmitHistoryChange()
    {
        Context context = Fixture();
        Build(context, 1, 0);
        object snapshot = Get(context.History, "CurrentSnapshot");
        int events = 0;
        Watch(context.History, "OnHistoryChanged", () => events++);
        object[] args = { 3, Vector2Int.zero, null, null };
        Assert.That(Call(context.Session, "TryCommitBuild", args), Is.False);
        Assert.That(Get(context.History, "CurrentSnapshot"), Is.SameAs(snapshot));
        Assert.That(events, Is.Zero);
    }

    [Test]
    public void FailedComboTransactionDoesNotCreateCompletedTurn()
    {
        Context context = Fixture();
        Build(context, 1, 0);
        Rows(context, ComboDefinition(1, 1001, 2001, "Money", int.MaxValue));
        object[] args = { 3, Vector2Int.right, null, null };
        Assert.That(Call(context.Session, "TryCommitBuild", args), Is.False);
        Assert.That(Get(context.History, "Count"), Is.EqualTo(1));
        Assert.That(ResourceValues(context), Is.EqualTo(new[] { 3, 9, 2, 2, 0 }));
        Assert.That(CardIds(context), Is.EqualTo(new[] { 2, 3, 4, 5, 6 }));
        Assert.That(Get(Tile(context, Vector2Int.right), "IsOccupied"), Is.False);
    }

    [Test]
    public void OnlyBuildCompletionCapturesAndIncludesEarlierStageUpdate()
    {
        Context context = Fixture();
        WatchPair(context.Session, "OnBuildingCommitted", () => StageState(context, true, 2, true, true, true, false));
        WatchPair(context.Session, "OnBuildResolved", () =>
            Assert.That(Get(Get(context.History, "CurrentSnapshot"), "StageProgress"), Is.Not.Null));
        Build(context, 1, 0);
        object snapshot = Get(context.History, "CurrentSnapshot");
        Assert.That(Get(Get(snapshot, "StageProgress"), "Rank"), Is.EqualTo(2));
        Call(context.Resources, "Add", Resource("Tourism"), 1);
        Call(context.Hand, "TryRestoreCards", Call(context.Hand, "CaptureCards"), null);
        Assert.That(Get(context.History, "Count"), Is.EqualTo(1));
        Assert.That(Get(context.History, "CurrentSnapshot"), Is.SameAs(snapshot));
    }

    [Test]
    public void BoardResetClearsHistoryAndRetryResourcesKeepTheirInitialBaseline()
    {
        Context context = Fixture();
        Build(context, 1, 0); Build(context, 3, 1);
        Call(context.Board, "ResetBoard");
        Assert.That(Get(context.History, "Count"), Is.Zero);
        Assert.That(Get(context.History, "CanUndo"), Is.False);
        Assert.That((IList)Get(context.Combo, "Results"), Is.Empty);
        Call(context.Resources, "ResetResources"); Call(context.Hand, "ResetCards");
        Build(context, 1, 0);
        Assert.That(Get(context.History, "Count"), Is.EqualTo(1));
        Assert.That(ResourceValues(context), Is.EqualTo(new[] { 3, 9, 2, 2, 0 }));
        Assert.That(Undo(context, out _), Is.False);
    }

    [Test]
    public void ReplacingBoardInvalidatesOldHistoryScope()
    {
        Context context = Fixture();
        Build(context, 1, 0); Build(context, 3, 1);
        object old = Get(context.History, "CurrentSnapshot");
        object[] args = { context.StageData, null };
        Assert.That(Call(context.Board, "TryCreateBoard", args), Is.True);
        Assert.That(Get(context.History, "Count"), Is.Zero);
        Assert.That(Undo(context, out _), Is.False);
        Assert.That(((IList)Get(old, "Buildings")).Count, Is.EqualTo(2));
    }

    [Test]
    public void ReinitializingHandInvalidatesOldCardSnapshots()
    {
        Context context = Fixture();
        Build(context, 1, 0); Build(context, 3, 1);
        object[] args = { context.StageData, context.Resources, null };
        Assert.That(Call(context.Hand, "TryInitializeFromStage", args), Is.True);
        Assert.That(Get(context.History, "Count"), Is.Zero);
        Assert.That(Undo(context, out _), Is.False);
        Assert.That(CardIds(context), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void SessionAndComboReconfigurationInvalidateHistoryWithoutChangingCurrentBoard()
    {
        Context context = Fixture();
        Build(context, 1, 0); Build(context, 3, 1);
        Call(context.Session, "ConfigureCombos", new object[] { null });
        Assert.That(Get(context.History, "Count"), Is.Zero);
        Assert.That(Get(Tile(context, Vector2Int.zero), "IsOccupied"), Is.True);
        Assert.That(Undo(context, out _), Is.False);
        Call(context.Session, "ConfigureCombos", context.Combo);
        Build(context, 5, 2);
        Assert.That(Get(context.History, "Count"), Is.EqualTo(1));
        object[] args = { context.Board, context.Resources, context.Database, null };
        Assert.That(Call(context.Combo, "TryConfigure", args), Is.True);
        Assert.That(Get(context.History, "Count"), Is.Zero);
    }

    [Test]
    public void DisabledHistoryDetectsResetMissedWhileUnsubscribed()
    {
        Context context = Fixture();
        Build(context, 1, 0); Build(context, 3, 1);
        Lifecycle(context.History, "OnDisable");
        context.History.gameObject.SetActive(false);
        Call(context.Board, "ResetBoard");
        Assert.That(Get(context.History, "Count"), Is.Zero);
        Assert.That(Undo(context, out _), Is.False);
    }

    [Test]
    public void MissingVisualDuringRestorePreservesCurrentBoardResourcesAndHistory()
    {
        Context context = Fixture();
        Build(context, 1, 0); Component current = Build(context, 3, 1);
        object snapshot = Get(context.History, "CurrentSnapshot");
        int notifications = 0;
        Watch(context.Resources, "OnResourcesChanged", () => notifications++);
        Watch(context.History, "OnHistoryChanged", () => notifications++);
        Set(context.A, "visualPrefab", null);
        Assert.That(Undo(context, out _), Is.False);
        Assert.That(Get(context.History, "CurrentSnapshot"), Is.SameAs(snapshot));
        Assert.That(Get(Tile(context, Vector2Int.right), "Building"), Is.SameAs(current));
        Assert.That(ResourceValues(context), Is.EqualTo(new[] { 4, 8, 4, 2, 0 }));
        Assert.That(CardIds(context), Is.EqualTo(new[] { 2, 4, 5, 6 }));
        Assert.That(notifications, Is.Zero);
        Assert.That(Get(context.Session, "IsBusy"), Is.False);
    }

    [Test]
    public void ChangedBuildingCodeRejectsOldSnapshotBeforeDestroyingAnything()
    {
        Context context = Fixture();
        Component first = Build(context, 1, 0); Component second = Build(context, 3, 1);
        Set(context.A, "buildingCode", 7001);
        Assert.That(Undo(context, out string error), Is.False);
        Assert.That(error, Does.Contain("changed"));
        Assert.That(Get(Tile(context, Vector2Int.zero), "Building"), Is.SameAs(first));
        Assert.That(Get(Tile(context, Vector2Int.right), "Building"), Is.SameAs(second));
        Assert.That(Get(context.History, "Count"), Is.EqualTo(2));
    }

    [Test]
    public void ChangedTileConstraintRejectsUndoAndKeepsCurrentInstances()
    {
        Context context = Fixture();
        Build(context, 1, 0); Component current = Build(context, 3, 1);
        Set(context.TileData, "tileType", Enum.Parse(RuntimeType("TileType"), "Asphalt"));
        Assert.That(Undo(context, out _), Is.False);
        Assert.That(Get(Tile(context, Vector2Int.right), "Building"), Is.SameAs(current));
        Assert.That(Get(context.History, "Count"), Is.EqualTo(2));
        Assert.That(ResourceValues(context), Is.EqualTo(new[] { 4, 8, 4, 2, 0 }));
    }

    [Test]
    public void BuildAfterUndoDiscardsFutureAndLateComboCompletionCannotAdvanceNewResult()
    {
        Context context = Fixture();
        Build(context, 1, 0); Build(context, 3, 1); Build(context, 5, 2);
        object abandoned = Get(context.History, "CurrentSnapshot");
        object[] last = { new Vector2Int(1, 0), new Vector2Int(2, 0), null };
        Assert.That(Call(context.Combo, "TryGetAppliedCombo", last), Is.True);
        object oldCombo = last[2];
        Assert.That(Undo(context, out string error), Is.True, error);
        Assert.That(Get(context.Combo, "CurrentPresentation"), Is.Null);
        Build(context, 5, 2);
        object replacement = Get(context.History, "CurrentSnapshot");
        Assert.That(Get(context.History, "Count"), Is.EqualTo(3));
        Assert.That(replacement, Is.Not.SameAs(abandoned));
        Assert.That(Get(replacement, "TurnNumber"), Is.EqualTo(3));
        object currentCombo = Get(context.Combo, "CurrentPresentation");
        Assert.That(Get(currentCombo, "ResultId"), Is.EqualTo(Get(oldCombo, "ResultId")));
        Assert.That(currentCombo, Is.Not.SameAs(oldCombo));
        Assert.That(Call(context.Combo, "TryCompletePresentation", oldCombo), Is.False);
        Assert.That(Get(context.Combo, "CurrentPresentation"), Is.SameAs(currentCombo));
        Assert.That(ResourceValues(context), Is.EqualTo(new[] { 5, 6, 4, 2, 0 }));
    }

    [Test]
    public void RestoredComboLookupAndRecheckDoNotRepayOrReplay()
    {
        Context context = Fixture();
        Build(context, 1, 0); Build(context, 3, 1); Build(context, 5, 2);
        Assert.That(Undo(context, out string error), Is.True, error);
        int events = 0;
        Watch(context.Resources, "OnResourcesChanged", () => events++);
        object[] lookup = { Vector2Int.zero, Vector2Int.right, null };
        Assert.That(Call(context.Combo, "TryGetAppliedCombo", lookup), Is.True);
        object[] resolve = { Get(Tile(context, Vector2Int.right), "Building"), null, null };
        Assert.That(Call(context.Combo, "TryResolveCombos", resolve), Is.True);
        Assert.That(((Array)resolve[1]).Length, Is.Zero);
        Assert.That(Get(context.Combo, "CurrentPresentation"), Is.Null);
        Assert.That(ResourceValues(context), Is.EqualTo(new[] { 4, 8, 4, 2, 0 }));
        Assert.That(((IList)Get(context.Session, "LastComboResults")).Count, Is.EqualTo(1));
        Assert.That(events, Is.Zero);
    }

    [Test]
    public void RestoreRejectsReentrantUndoBuildAndReconfiguration()
    {
        Context context = Fixture();
        Build(context, 1, 0); Build(context, 3, 1);
        int checkedEvents = 0;
        Action check = () =>
        {
            checkedEvents++;
            Assert.That(Undo(context, out _), Is.False);
            object[] build = { 5, new Vector2Int(2, 0), null, null };
            Assert.That(Call(context.Session, "TryCommitBuild", build), Is.False);
            object[] configure = { context.Session, context.Stage, null };
            Assert.That(Call(context.History, "TryConfigure", configure), Is.False);
            Assert.That(Call(context.History, "TryClearHistory"), Is.False);
        };
        Watch(context.Session, "OnStateRestoring", check);
        Watch(context.Resources, "OnResourcesChanged", check);
        Watch(context.Session, "OnStateRestored", check);
        Assert.That(Undo(context, out string error), Is.True, error);
        Assert.That(checkedEvents, Is.EqualTo(3));
        Assert.That(Get(context.Session, "IsBusy"), Is.False);
        Assert.That(Get(context.History, "Count"), Is.EqualTo(1));
    }

    [Test]
    public void UndoImmediatelyCancelsAnExistingDragAndPreview()
    {
        Context context = Fixture();
        Build(context, 1, 0); Build(context, 3, 1);
        Component controller = Component("Placement controller", "BuildingPlacementController");
        Call(controller, "Configure", context.Session, null);
        Assert.That(Call(controller, "TryBeginDrag", 4), Is.True);
        Assert.That(Get(controller, "IsDragging"), Is.True);
        Assert.That(Undo(context, out string error), Is.True, error);
        Assert.That(Get(controller, "IsDragging"), Is.False);
        Assert.That(Get(controller, "SelectedCardId"), Is.Zero);
        Assert.That(Get(controller, "PreviewTile"), Is.Null);
    }

    [Test]
    public void UndoButtonTracksFirstBuildSecondBuildAndReturnToFirst()
    {
        Context context = Fixture(false);
        Component ui = Component("Undo UI", "UndoButtonUI");
        Button button = Object("Undo button", false, true).AddComponent<Button>();
        Call(ui, "Configure", context.History, button);
        Lifecycle(ui, "OnEnable");
        Assert.That(button.interactable, Is.False);
        Build(context, 1, 0);
        Assert.That(button.interactable, Is.False);
        Build(context, 3, 1);
        Assert.That(button.interactable, Is.True);
        button.onClick.Invoke();
        Assert.That(Get(context.History, "Count"), Is.EqualTo(1));
        Assert.That(button.interactable, Is.False);
        Lifecycle(ui, "OnDisable");
        Build(context, 5, 2);
        Assert.That(button.interactable, Is.False);
        Lifecycle(ui, "OnEnable");
        Assert.That(button.interactable, Is.True);
    }
}
