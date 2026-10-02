using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class StageGoalContractTests
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
                "BuildingPlacementController", "UndoButtonUI", "NextStageButtonUI" })
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

    [Test]
    public void EveryResourceGoalUsesAnInclusiveThreshold()
    {
        string[] names = { "Population", "Jobs", "Money", "Logistics", "Tourism" };
        foreach (string name in names)
        {
            Array amounts = Resources();
            for (int i = 0; i < names.Length; i++)
                if (names[i] == name) amounts.SetValue(Amount(name, 4), i);
            Assert.That(Evaluate(Goal(name, 4), Values("BuildingStateSnapshot"), amounts, 1, out bool met), Is.True);
            Assert.That(met, Is.True, name);
            Assert.That(Evaluate(Goal(name, 5), Values("BuildingStateSnapshot"), amounts, 1, out met), Is.True);
            Assert.That(met, Is.False, name);
        }
    }

    [Test]
    public void SignedRuntimeResourcesRemainValidButDoNotMeetPositiveTargets()
    {
        Assert.That(Evaluate(Goal("Population", 0), Values("BuildingStateSnapshot"),
            Resources(-1), 1, out bool met), Is.True);
        Assert.That(met, Is.False);
        Assert.That(Evaluate(Goal("Population", -1), Values("BuildingStateSnapshot"),
            Resources(), 1, out _), Is.False);
    }

    [Test]
    public void AllCardsUsedRequiresExactlyZeroRemainingCards()
    {
        foreach (int count in new[] { 0, 1, 6 })
        {
            Assert.That(Evaluate(AllCardsGoal(), Values("BuildingStateSnapshot"), Resources(), count, out bool met), Is.True);
            Assert.That(met, Is.EqualTo(count == 0));
        }
    }

    [Test]
    public void AdjacencyMatchesEitherBuildingOrderInAllFourDirections()
    {
        ScriptableObject a = Building(1001), b = Building(2001);
        foreach (Vector2Int offset in new[] { Vector2Int.left, Vector2Int.down, Vector2Int.right, Vector2Int.up })
        {
            Array placements = Values("BuildingStateSnapshot", Placement(a, 2, 2),
                Placement(b, 2 + offset.x, 2 + offset.y));
            foreach (object goal in new[] { AdjacencyGoal(a, b), AdjacencyGoal(b, a) })
            {
                Assert.That(Evaluate(goal, placements, Resources(), 1, out bool met), Is.True);
                Assert.That(met, Is.True);
            }
        }
    }

    [Test]
    public void DiagonalAndDistantBuildingsDoNotMeetAdjacencyGoals()
    {
        ScriptableObject a = Building(1001), b = Building(2001);
        foreach (Vector2Int coordinate in new[] { new Vector2Int(1, 1), new Vector2Int(2, 0) })
        {
            Assert.That(Evaluate(AdjacencyGoal(a, b), Values("BuildingStateSnapshot",
                Placement(a, 0, 0), Placement(b, coordinate.x, coordinate.y)), Resources(), 1, out bool met), Is.True);
            Assert.That(met, Is.False);
        }
    }

    [Test]
    public void SameCodeAdjacencyRequiresTwoDistinctBuildings()
    {
        ScriptableObject a = Building(1001);
        Assert.That(Evaluate(AdjacencyGoal(a, a), Values("BuildingStateSnapshot",
            Placement(a, 0, 0)), Resources(), 1, out bool met), Is.True);
        Assert.That(met, Is.False);
        Assert.That(Evaluate(AdjacencyGoal(a, a), Values("BuildingStateSnapshot",
            Placement(a, 0, 0), Placement(a, 1, 0)), Resources(), 1, out met), Is.True);
        Assert.That(met, Is.True);
    }

    [Test]
    public void LargeCoordinateAdjacencyDoesNotOverflow()
    {
        ScriptableObject a = Building(1001), b = Building(2001);
        Assert.That(Evaluate(AdjacencyGoal(a, b), Values("BuildingStateSnapshot",
            Placement(a, int.MaxValue, int.MaxValue), Placement(b, 0, 0)), Resources(), 0, out bool met), Is.True);
        Assert.That(met, Is.False);
        Assert.That(Evaluate(AdjacencyGoal(a, b), Values("BuildingStateSnapshot",
            Placement(a, int.MaxValue, 0), Placement(b, int.MaxValue - 1, 0)), Resources(), 0, out met), Is.True);
        Assert.That(met, Is.True);
    }

    [Test]
    public void UnknownGoalsAndMissingOrInvalidAdjacencyDefinitionsAreRejected()
    {
        object goal = Goal("Population", 1);
        Set(goal, "goalType", Enum.ToObject(RuntimeType("StageGoalType"), 99));
        Assert.That(Evaluate(goal, Values("BuildingStateSnapshot"), Resources(), 0, out _), Is.False);
        Assert.That(Evaluate(AdjacencyGoal(null, null), Values("BuildingStateSnapshot"), Resources(), 0, out _), Is.False);
        Assert.That(Evaluate(AdjacencyGoal(Building(0), Building(1001)),
            Values("BuildingStateSnapshot"), Resources(), 0, out _), Is.False);
        Assert.That(Evaluate(null, Values("BuildingStateSnapshot"), Resources(), 0, out _), Is.False);
    }

    [Test]
    public void MissingDuplicateAndUnknownResourceInputsAreRejected()
    {
        Assert.That(Evaluate(Goal("Population", 1), Values("BuildingStateSnapshot"),
            Values("ResourceAmount", Amount("Population", 2)), 0, out _), Is.False);
        Array values = Resources();
        values.SetValue(Amount("Population", 1), 1);
        Assert.That(Evaluate(Goal("Population", 1), Values("BuildingStateSnapshot"), values, 0, out _), Is.False);
        values = Resources();
        values.SetValue(Activator.CreateInstance(RuntimeType("ResourceAmount"),
            Enum.ToObject(RuntimeType("ResourceType"), 99), 0), 1);
        Assert.That(Evaluate(Goal("Population", 1), Values("BuildingStateSnapshot"), values, 0, out _), Is.False);
    }

    [Test]
    public void InvalidCardCountsAndDuplicateOrChangedPlacementsAreRejected()
    {
        ScriptableObject building = Building(1001);
        object placement = Placement(building, 0, 0);
        Assert.That(Evaluate(AllCardsGoal(), Values("BuildingStateSnapshot"), Resources(), -1, out _), Is.False);
        Assert.That(Evaluate(AllCardsGoal(), Values("BuildingStateSnapshot", placement, placement),
            Resources(), 0, out _), Is.False);
        Set(building, "buildingCode", 2001);
        Assert.That(Evaluate(AllCardsGoal(), Values("BuildingStateSnapshot", placement), Resources(), 0, out _), Is.False);
    }

    [Test]
    public void RankRequiresMandatoryGoalAndCountsEachAdditionalGoal()
    {
        ScriptableObject stage = Asset("StageData");
        Goals(stage, Goal("Population", 3), Goal("Population", 1), Goal("Population", 2));
        object progress = EvaluateStage(stage, Resources(2));
        Assert.That(Get(progress, "Rank"), Is.Zero);
        Assert.That(Get(progress, "NextStageAvailable"), Is.False);
        for (int count = 0; count < 3; count++)
        {
            Goals(stage, Goal("Population", 1), Goal("Population", count > 0 ? 1 : 4),
                Goal("Population", count > 1 ? 1 : 4));
            progress = EvaluateStage(stage, Resources(1));
            Assert.That(Get(progress, "Rank"), Is.EqualTo(1 + count));
            Assert.That(Get(progress, "IsCleared"), Is.False);
            Assert.That(Get(progress, "NextStageAvailable"), Is.True);
        }
    }

    [Test]
    public void InvalidReconfigurationPreservesProgressAndHistory()
    {
        Context context = Fixture();
        Build(context, 1, 0);
        object snapshot = Get(context.History, "CurrentSnapshot");
        long version = (long)Get(context.Stage, "ConfigurationVersion");
        Set(context.StageData, "additionalGoals", Values("StageGoalData", Goal("Population", 1)));
        object[] args = { context.StageData, context.Board, context.Resources, context.Hand, null };
        Assert.That(Call(context.Stage, "TryConfigure", args), Is.False);
        Assert.That(Get(context.Stage, "ConfigurationVersion"), Is.EqualTo(version));
        Assert.That(Get(context.Stage, "Rank"), Is.EqualTo(1));
        Assert.That(Get(context.History, "CurrentSnapshot"), Is.SameAs(snapshot));
    }

    [Test]
    public void ConfigurationRejectsForeignStageMissingManagersAndUninitializedHand()
    {
        Context context = Fixture();
        object[] args = { Asset("StageData"), context.Board, context.Resources, context.Hand, null };
        Assert.That(Call(context.Stage, "TryConfigure", args), Is.False);
        args = new object[] { context.StageData, null, context.Resources, context.Hand, null };
        Assert.That(Call(context.Stage, "TryConfigure", args), Is.False);
        args = new object[] { context.StageData, context.Board, context.Resources,
            Component("Empty hand", "BuildingHandManager"), null };
        Assert.That(Call(context.Stage, "TryConfigure", args), Is.False);
        args = new object[] { context.StageData, context.Board,
            Component("Foreign resources", "ResourceManager"), context.Hand, null };
        Assert.That(Call(context.Stage, "TryConfigure", args), Is.False);
    }

    [Test]
    public void PureEvaluationDoesNotMutateInputsAndProgressCopiesGoalFlags()
    {
        ScriptableObject stage = Asset("StageData");
        Goals(stage, Goal("Population", 1), Goal("Population", 2), Goal("Population", 3));
        Array values = Resources(2);
        object progress = EvaluateStage(stage, values);
        values.SetValue(Amount("Population", 99), 0);
        Assert.That(Get(progress, "Rank"), Is.EqualTo(2));
        IList flags = (IList)Get(progress, "GoalStates");
        Assert.Throws<NotSupportedException>(() => flags[0] = false);
        Assert.That(Get(stage, "RequiredGoal"), Is.Not.Null);
    }

    [Test]
    public void RequiredGoalEnablesNextStageWithoutAutoClearAndAllowsHigherRanks()
    {
        Context context = Fixture();
        int cleared = 0, completed = 0;
        Watch(context.Stage, "OnStageCleared", () => cleared++);
        WatchResult(context.Stage, () => completed++);
        Build(context, 1, 0);
        Assert.That(Get(context.Stage, "NextStageAvailable"), Is.True);
        Assert.That(Get(context.Stage, "IsCleared"), Is.False);
        Build(context, 3, 1); Build(context, 5, 2);
        Assert.That(Get(context.Stage, "Rank"), Is.EqualTo(3));
        Assert.That(cleared, Is.Zero);
        Assert.That(completed, Is.Zero);
    }

    [Test]
    public void FirstResourceNotificationSeesGoalsAfterAllComboRewards()
    {
        Context context = Fixture();
        Rows(context, ComboDefinition(1, 1001, 2001, "Population", 2));
        object[] config = { context.Board, context.Resources, context.Database, null };
        Assert.That(Call(context.Combo, "TryConfigure", config), Is.True);
        Build(context, 1, 0);
        int notifications = 0;
        WatchPair(context.Resources, "OnResourceChanged", () =>
        {
            notifications++;
            Assert.That(ResourceValues(context)[0], Is.EqualTo(6));
            Assert.That(Get(context.Stage, "Rank"), Is.EqualTo(3));
            Assert.That(Get(context.Stage, "NextStageAvailable"), Is.True);
            Assert.That(((IList)Get(context.Hand, "Cards")).Count, Is.EqualTo(4));
        });
        Build(context, 3, 1);
        Assert.That(notifications, Is.GreaterThan(0));
        Assert.That(Get(Get(context.History, "CurrentSnapshot"), "StageProgress"), Is.Not.Null);
    }

    [Test]
    public void CompletedTurnCapturesAdjacencyAndUndoRestoresItsEarlierRank()
    {
        Context context = Fixture();
        Goals(context.StageData, Goal("Population", 3), AdjacencyGoal(context.A, context.B), AllCardsGoal());
        Refresh(context);
        Build(context, 1, 0); Build(context, 3, 1);
        Assert.That(Get(context.Stage, "Rank"), Is.EqualTo(2));
        object progress = Get(Get(context.History, "CurrentSnapshot"), "StageProgress");
        Assert.That(Get(progress, "Rank"), Is.EqualTo(2));
        Assert.That((IList)Get(progress, "GoalStates"), Is.EqualTo(new[] { true, true, false }));
        Assert.That(Undo(context, out string error), Is.True, error);
        Assert.That(Get(context.Stage, "Rank"), Is.EqualTo(1));
        Assert.That((IList)Get(context.Stage, "GoalStates"), Is.EqualTo(new[] { true, false, false }));
    }

    [Test]
    public void UndoRemovesRequiredGoalReadinessAndDisablesNextStage()
    {
        Context context = Fixture();
        Goals(context.StageData, Goal("Population", 4), Goal("Population", 5), Goal("Population", 6));
        Refresh(context);
        Build(context, 1, 0); Build(context, 3, 1);
        Assert.That(Get(context.Stage, "NextStageAvailable"), Is.True);
        Assert.That(Undo(context, out string error), Is.True, error);
        Assert.That(Get(context.Stage, "NextStageAvailable"), Is.False);
        Assert.That(Get(context.Stage, "Rank"), Is.Zero);
    }

    [Test]
    public void CompletionRequiresMandatoryGoalAndCopiesResultValues()
    {
        Context context = Fixture();
        Assert.That(Complete(context, out _), Is.False);
        Build(context, 1, 0);
        Assert.That(Complete(context, out object result), Is.True);
        Assert.That(Get(result, "Rank"), Is.EqualTo(1));
        Assert.That(Get(result, "StageNumber"), Is.EqualTo(1));
        Set(context.StageData, "stageName", "Changed name");
        Set(Get(context.StageData, "RequiredGoal"), "description", "Changed description");
        Call(context.Resources, "Add", Resource("Population"), 10);
        Assert.That(((IList)Get(result, "GoalDescriptions"))[0], Is.EqualTo("Population >= 3"));
        Assert.That(Get(((IList)Get(result, "Resources"))[0], "Amount"), Is.EqualTo(3));
        Assert.That(Get(result, "StageName"), Is.Not.EqualTo("Changed name"));
        Assert.Throws<NotSupportedException>(() => ((IList)Get(result, "GoalStates"))[0] = false);
        Assert.That(Get(context.Stage, "NextStageAvailable"), Is.False);
        object[] build = { 3, Vector2Int.right, null, null };
        Assert.That(Call(context.Session, "TryCommitBuild", build), Is.False);
    }

    [Test]
    public void RepeatedCompletionReturnsSameResultAndPublishesOnlyOnce()
    {
        Context context = Fixture();
        int cleared = 0, completed = 0;
        Watch(context.Stage, "OnStageCleared", () => cleared++);
        WatchResult(context.Stage, () => completed++);
        Build(context, 1, 0);
        Assert.That(Complete(context, out object first), Is.True);
        Assert.That(Complete(context, out object second), Is.True);
        Assert.That(second, Is.SameAs(first));
        Refresh(context);
        Assert.That(cleared, Is.EqualTo(1));
        Assert.That(completed, Is.EqualTo(1));
    }

    [Test]
    public void BuildAndCompletionCallbacksRejectReentrantCompletionOrConfiguration()
    {
        Context context = Fixture();
        WatchPair(context.Session, "OnBuildingCommitted", () =>
        {
            Assert.That(Complete(context, out _), Is.False);
            object[] args = { context.StageData, context.Board, context.Resources, context.Hand, null };
            Assert.That(Call(context.Stage, "TryConfigure", args), Is.False);
            Assert.That(Call(context.Stage, "TryApplyProgressState",
                Progress(false, 1, true, true, false, false)), Is.False);
        });
        Build(context, 1, 0);
        WatchResult(context.Stage, () =>
        {
            Assert.That(Complete(context, out _), Is.False);
            object[] args = { 3, Vector2Int.right, null, null };
            Assert.That(Call(context.Session, "TryCommitBuild", args), Is.False);
        });
        Assert.That(Complete(context, out _), Is.True);
    }

    [Test]
    public void CompletionRechecksResourcesInsteadOfTrustingStaleNextStageState()
    {
        Context context = Fixture();
        Build(context, 1, 0);
        Call(context.Resources, "Add", Resource("Population"), -1);
        Assert.That(Get(context.Stage, "NextStageAvailable"), Is.True);
        Assert.That(Complete(context, out _), Is.False);
        Assert.That(Get(context.Stage, "NextStageAvailable"), Is.False);
        Assert.That(Get(context.Stage, "Rank"), Is.Zero);
    }

    [Test]
    public void UndoAfterCompletionReopensStageWithoutPublishingAnotherResult()
    {
        Context context = Fixture();
        int completed = 0;
        WatchResult(context.Stage, () => completed++);
        Build(context, 1, 0); Build(context, 3, 1);
        Assert.That(Complete(context, out _), Is.True);
        Assert.That(Undo(context, out string error), Is.True, error);
        Assert.That(Get(context.Stage, "IsCleared"), Is.False);
        Assert.That(Get(context.Stage, "CurrentResult"), Is.Null);
        Assert.That(Get(context.Stage, "Rank"), Is.EqualTo(1));
        Assert.That(Get(context.Stage, "NextStageAvailable"), Is.True);
        Assert.That(completed, Is.EqualTo(1));
        Build(context, 5, 2);
        Assert.That(Get(context.History, "Count"), Is.EqualTo(2));
    }

    [Test]
    public void SuccessfulStageReconfigurationInvalidatesOldTurnHistory()
    {
        Context context = Fixture();
        Build(context, 1, 0); Build(context, 3, 1);
        object[] args = { context.StageData, context.Board, context.Resources, context.Hand, null };
        Assert.That(Call(context.Stage, "TryConfigure", args), Is.True);
        Assert.That(Get(context.History, "Count"), Is.Zero);
        Assert.That(Get(context.History, "CanUndo"), Is.False);
    }

    [Test]
    public void ResetReopensStageAndClearsHistoryAfterManagersReset()
    {
        Context context = Fixture();
        Build(context, 1, 0); Build(context, 3, 1);
        Assert.That(Complete(context, out _), Is.True);
        Call(context.Board, "ResetBoard");
        Call(context.Resources, "ResetResources");
        Call(context.Hand, "ResetCards");
        Call(context.Stage, "ResetStage");
        Assert.That(Get(context.Stage, "IsCleared"), Is.False);
        Assert.That(Get(context.Stage, "Rank"), Is.Zero);
        Assert.That(Get(context.Stage, "NextStageAvailable"), Is.False);
        Assert.That(Get(context.Stage, "CurrentResult"), Is.Null);
        Assert.That(Get(context.History, "Count"), Is.Zero);
        Build(context, 1, 0);
    }

    [Test]
    public void RejectedBuildDoesNotChangeGoalsOrPublishACompletedTurn()
    {
        Context context = Fixture();
        Build(context, 1, 0);
        int changed = 0;
        Watch(context.Stage, "OnStateChanged", () => changed++);
        object[] args = { 3, Vector2Int.zero, null, null };
        Assert.That(Call(context.Session, "TryCommitBuild", args), Is.False);
        Assert.That(Get(context.Stage, "Rank"), Is.EqualTo(1));
        Assert.That(Get(context.History, "Count"), Is.EqualTo(1));
        Assert.That(changed, Is.Zero);
    }

    [Test]
    public void NextStageButtonTracksReadinessCompletionAndDisabledSubscription()
    {
        Context context = Fixture();
        GameObject root = Object("Next button", true, true);
        Button button = root.AddComponent<Button>();
        Component ui = root.AddComponent(RuntimeType("NextStageButtonUI"));
        Call(ui, "Configure", context.Stage, button);
        Assert.That(button.interactable, Is.False);
        Build(context, 1, 0);
        Assert.That(button.interactable, Is.True);
        button.onClick.Invoke();
        Assert.That(Get(context.Stage, "IsCleared"), Is.True);
        Assert.That(button.interactable, Is.False);
        Assert.That(Complete(context, out object original), Is.True);
        Lifecycle(ui, "OnDisable");
        Call(context.Stage, "ResetStage");
        button.onClick.Invoke();
        Assert.That(Get(context.Stage, "IsCleared"), Is.False);
        Lifecycle(ui, "OnEnable");
        Assert.That(button.interactable, Is.True);
        button.onClick.Invoke();
        Assert.That(Get(context.Stage, "CurrentResult"), Is.Not.SameAs(original));
    }

    [Test]
    public void LegacyPrototypeStillClearsAtPopulationFourAndResets()
    {
        Context context = Fixture(false);
        Component legacy = Component("Legacy Stage", "StageManager");
        Set(legacy, "resourceManager", context.Resources);
        Lifecycle(legacy, "Start");
        Call(legacy, "CheckStageClear");
        Assert.That(Get(legacy, "IsCleared"), Is.False);
        Call(context.Resources, "Add", Resource("Population"), 2);
        Call(legacy, "CheckStageClear");
        Assert.That(Get(legacy, "IsCleared"), Is.True);
        Call(legacy, "ResetStage");
        Assert.That(Get(legacy, "IsCleared"), Is.False);
        Assert.That(Get(legacy, "IsConfigured"), Is.False);
    }
}
