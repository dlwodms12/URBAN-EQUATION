using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class ComboContractTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private readonly List<ScriptableObject> assets = new List<ScriptableObject>();
    private Action resourceObserver;
    private Action buildObserver;

    private sealed class Context
    {
        public Component Board, Resources, Manager, Source, Hand, Session, Prefab;
        public ScriptableObject A, B, C, Stage, Database;
        public Vector2Int Center = new Vector2Int(1, 1);
    }
    private sealed class Observer
    {
        public Action<object> Callback;
        public void Record<T>(T value) => Callback(value);
        public Action<object[]> Legacy;
        public void RecordLegacy<T>(int code, int a, int b, T resource, int amount) =>
            Legacy(new object[] { code, a, b, resource, amount });
    }

    private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private GameObject Object(string name, bool active = false)
    {
        var result = new GameObject(name);
        result.SetActive(active);
        objects.Add(result);
        return result;
    }
    private Component Component(string name, string type, bool active = false) =>
        Object(name, active).AddComponent(RuntimeType(type));
    private ScriptableObject Asset(string name)
    {
        var result = ScriptableObject.CreateInstance(RuntimeType(name));
        assets.Add(result);
        return result;
    }
    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static object Get(object target, string property) => target.GetType().GetProperty(property).GetValue(target);
    private static object Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method).Invoke(target, args);
    private static Array Values(string type, params object[] values)
    {
        Array result = Array.CreateInstance(RuntimeType(type), values.Length);
        for (int i = 0; i < values.Length; i++) result.SetValue(values[i], i);
        return result;
    }
    private static object Resource(string name) => Enum.Parse(RuntimeType("ResourceType"), name);
    private static object Amount(string name, int value) =>
        Activator.CreateInstance(RuntimeType("ResourceAmount"), Resource(name), value);
    private static object Definition(int code, int a, int b, params object[] rewards)
    {
        object result = Activator.CreateInstance(RuntimeType("ComboDefinition"));
        Set(result, "comboCode", code); Set(result, "buildingCodeA", a); Set(result, "buildingCodeB", b);
        Set(result, "comboName", "콤보 " + code); Set(result, "description", "검증용 설명");
        Set(result, "rewards", Values("ResourceAmount", rewards));
        return result;
    }
    private static IList List(string type, params object[] values)
    {
        IList result = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(RuntimeType(type)));
        foreach (object value in values) result.Add(value);
        return result;
    }
    private ScriptableObject Building(int code)
    {
        var result = Asset("BuildingData");
        Set(result, "buildingCode", code); Set(result, "buildingName", "Building " + code);
        Set(result, "useResourceLists", true);
        Set(result, "requiredResources", Values("ResourceAmount", Amount("Jobs", 1)));
        Set(result, "gainedResources", Values("ResourceAmount", Amount("Population", 1)));
        Set(result, "allowedTileTypes", Values("TileType", Enum.Parse(RuntimeType("TileType"), "Grass")));
        Set(result, "visualPrefab", Object("Visual template"));
        return result;
    }
    private Context Fixture(bool source = true)
    {
        var context = new Context();
        context.A = Building(1001); context.B = Building(2001); context.C = Building(3001);
        Component template = Component("Tile template", "Tile");
        var tileData = Asset("TileData");
        Set(tileData, "tileType", Enum.Parse(RuntimeType("TileType"), "Grass"));
        Set(tileData, "tilePrefab", template);
        context.Stage = Asset("StageData");
        Set(context.Stage, "width", 3); Set(context.Stage, "height", 3);
        var tiles = new object[9];
        for (int i = 0; i < tiles.Length; i++) tiles[i] = tileData;
        Set(context.Stage, "tiles", Values("TileData", tiles));
        object entry = Activator.CreateInstance(RuntimeType("StageBuildingCardData"));
        Set(entry, "building", context.A); Set(entry, "count", 2);
        Set(context.Stage, "buildingCards", Values("StageBuildingCardData", entry));
        context.Board = Component("Board", "BoardManager");
        context.Board.transform.position = new Vector3(10f, 2f, -4f);
        object[] boardArgs = { context.Stage, null };
        Assert.That(Call(context.Board, "TryCreateBoard", boardArgs), Is.True, boardArgs[1] as string);
        context.Resources = Component("Resources", "ResourceManager");
        Call(context.Resources, "CaptureResourceState");
        context.Manager = Component("Combos", "ComboManager", true);
        context.Database = Asset("ComboDatabase");
        Configure(context);
        if (source) context.Source = Place(context, context.A, context.Center);
        return context;
    }
    private static void Configure(Context context)
    {
        object[] args = { context.Board, context.Resources, context.Database, null };
        Assert.That(Call(context.Manager, "TryConfigure", args), Is.True, args[3] as string);
    }
    private static Component Tile(Context context, Vector2Int coordinate) =>
        (Component)Call(context.Board, "GetTile", coordinate);
    private Component Place(Context context, ScriptableObject data, Vector2Int coordinate)
    {
        Component building = Component("Building", "BuildingInstance");
        building.transform.position = Tile(context, coordinate).transform.position;
        Call(building, "Initialize", data, coordinate);
        Assert.That(Call(Tile(context, coordinate), "TrySetBuilding", building), Is.True);
        return building;
    }
    private static void Rows(Context context, params object[] definitions) =>
        Set(context.Database, "combos", List("ComboDefinition", definitions));
    private static Array Resolve(Context context, Component source = null)
    {
        object[] args = { source == null ? context.Source : source, null, null };
        Assert.That(Call(context.Manager, "TryResolveCombos", args), Is.True, args[2] as string);
        return (Array)args[1];
    }
    private static int[] ResourceValues(Context context)
    {
        string[] names = { "Population", "Jobs", "Money", "Logistics", "Tourism" };
        var values = new int[5];
        for (int i = 0; i < values.Length; i++)
            values[i] = (int)Call(context.Resources, "GetResource", Resource(names[i]));
        return values;
    }
    private static int[] CardIds(object hand)
    {
        IList cards = (IList)Get(hand, "Cards");
        var ids = new int[cards.Count];
        for (int i = 0; i < ids.Length; i++) ids[i] = (int)Get(cards[i], "CardId");
        return ids;
    }
    private static void Watch(object target, string name, Action<object> callback)
    {
        EventInfo info = target.GetType().GetEvent(name);
        Type argument = info.EventHandlerType.GetGenericArguments()[0];
        var observer = new Observer { Callback = callback };
        MethodInfo method = typeof(Observer).GetMethod("Record").MakeGenericMethod(argument);
        info.AddEventHandler(target, Delegate.CreateDelegate(info.EventHandlerType, observer, method));
    }
    private void WatchResources(Context context, Action callback)
    {
        resourceObserver = callback;
        EventInfo info = context.Resources.GetType().GetEvent("OnResourceChanged");
        MethodInfo method = GetType().GetMethod("RecordResource", BindingFlags.Instance | BindingFlags.NonPublic)
            .MakeGenericMethod(RuntimeType("ResourceType"));
        info.AddEventHandler(context.Resources, Delegate.CreateDelegate(info.EventHandlerType, this, method));
    }
    private void RecordResource<T>(T type, int amount) => resourceObserver?.Invoke();
    private void RecordBuild<T, TResult>(T building, TResult results) => buildObserver?.Invoke();
    private void AttachSession(Context context)
    {
        context.Hand = Component("Hand", "BuildingHandManager", true);
        object[] args = { context.Stage, context.Resources, null };
        Assert.That(Call(context.Hand, "TryInitializeFromStage", args), Is.True, args[2] as string);
        context.Prefab = Component("Building template", "BuildingInstance");
        context.Session = Component("Session", "GameSessionManager");
        Call(context.Session, "Configure", context.Board, context.Resources, context.Hand, context.Prefab);
        Call(context.Session, "ConfigureCombos", context.Manager);
    }
    private static bool Commit(Context context, int id, out string error)
    {
        object[] args = { id, context.Center, null, null };
        bool result = (bool)Call(context.Session, "TryCommitBuild", args);
        error = args[3] as string;
        return result;
    }

    [TearDown]
    public void TearDown()
    {
        resourceObserver = null; buildObserver = null;
        for (int i = objects.Count - 1; i >= 0; i--)
        {
            if (objects[i] == null) continue;
            Component manager = objects[i].GetComponent(RuntimeType("ComboManager"));
            if (manager != null)
                manager.GetType().GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
            UnityEngine.Object.DestroyImmediate(objects[i]);
        }
        foreach (ScriptableObject asset in assets) UnityEngine.Object.DestroyImmediate(asset);
        objects.Clear(); assets.Clear();
    }

    [Test]
    public void FourDirectionsResolveLeftDownRightUpAndPreserveRepeatedComboCodes()
    {
        Context context = Fixture();
        Vector2Int[] offsets = { Vector2Int.left, Vector2Int.down, Vector2Int.right, Vector2Int.up };
        var definitions = new object[4];
        for (int i = 0; i < offsets.Length; i++)
        {
            ScriptableObject building = Building(2001 + i);
            Place(context, building, context.Center + offsets[i]);
            definitions[i] = Definition(11001, 1001, 2001 + i, Amount("Money", i + 1));
        }
        Rows(context, definitions);
        var published = new List<object>();
        Watch(context.Manager, "OnComboResolved", result => published.Add(result));
        Array results = Resolve(context);
        Assert.That(results.Length, Is.EqualTo(4));
        for (int i = 0; i < results.Length; i++)
        {
            Assert.That(Get(results.GetValue(i), "Direction"), Is.EqualTo(offsets[i]));
            Assert.That(Get(results.GetValue(i), "ResultId"), Is.EqualTo(i + 1));
            Assert.That(Get(results.GetValue(i), "ComboCode"), Is.EqualTo(11001));
            Assert.That(published[i], Is.SameAs(results.GetValue(i)));
            Vector3 expected = (context.Source.transform.position
                + Tile(context, context.Center + offsets[i]).transform.position) * 0.5f;
            Assert.That(Vector3.Distance((Vector3)Get(results.GetValue(i), "PresentationPosition"), expected),
                Is.LessThanOrEqualTo(0.00001f));
        }
        Assert.That(ResourceValues(context), Is.EqualTo(new[] { 2, 2, 12, 2, 0 }));
        Assert.That(Get(context.Manager, "CurrentPresentation"), Is.SameAs(results.GetValue(0)));
        Assert.That(Get(context.Manager, "PendingPresentationCount"), Is.EqualTo(3));
    }

    [Test]
    public void BuildingOrderIsIrrelevantAndEqualCodePairsAreValid()
    {
        Context context = Fixture();
        Place(context, context.B, context.Center + Vector2Int.left);
        Place(context, context.A, context.Center + Vector2Int.right);
        Rows(context, Definition(1, 2001, 1001, Amount("Jobs", 1)),
            Definition(2, 1001, 1001, Amount("Tourism", 1)));
        Array results = Resolve(context);
        Assert.That(results.Length, Is.EqualTo(2));
        Assert.That(Get(results.GetValue(0), "ComboCode"), Is.EqualTo(1));
        Assert.That(Get(results.GetValue(1), "ComboCode"), Is.EqualTo(2));
        Assert.That(ResourceValues(context), Is.EqualTo(new[] { 2, 3, 2, 2, 1 }));
    }

    [Test]
    public void DiagonalEmptyAndEdgeTilesAreSkippedWithoutRecursiveComboChains()
    {
        Context context = Fixture();
        Place(context, context.B, context.Center + Vector2Int.right);
        Place(context, context.C, new Vector2Int(2, 2));
        Rows(context, Definition(1, 1001, 2001, Amount("Money", 1)),
            Definition(2, 2001, 3001, Amount("Tourism", 9)));
        Assert.That(Resolve(context).Length, Is.EqualTo(1));
        Component corner = Place(context, context.A, Vector2Int.zero);
        Assert.That(Resolve(context, corner).Length, Is.Zero);
        Assert.That(ResourceValues(context), Is.EqualTo(new[] { 2, 2, 3, 2, 0 }));
    }

    [Test]
    public void ForeignSourceOrMalformedAdjacentBuildingRejectsWithoutEffects()
    {
        Context context = Fixture();
        Component foreign = Component("Foreign source", "BuildingInstance");
        Call(foreign, "Initialize", context.A, context.Center);
        object[] args = { foreign, null, null };
        Assert.That(Call(context.Manager, "TryResolveCombos", args), Is.False);
        Component adjacent = Component("Missing data", "BuildingInstance");
        Call(Tile(context, context.Center + Vector2Int.left), "TrySetBuilding", adjacent);
        args = new object[] { context.Source, null, null };
        Assert.That(Call(context.Manager, "TryResolveCombos", args), Is.False);
        Assert.That((IList)Get(context.Manager, "Results"), Is.Empty);
        Assert.That(ResourceValues(context), Is.EqualTo(new[] { 2, 2, 2, 2, 0 }));
    }

    [Test]
    public void RepeatResolutionAndPairLookupNeverPayRewardsAgain()
    {
        Context context = Fixture();
        Component adjacent = Place(context, context.B, context.Center + Vector2Int.left);
        Rows(context, Definition(1, 1001, 2001, Amount("Population", 2)));
        object original = Resolve(context).GetValue(0);
        int events = 0;
        WatchResources(context, () => events++);
        Assert.That(Resolve(context).Length, Is.Zero);
        Assert.That(Resolve(context, adjacent).Length, Is.Zero);
        object[] lookup = { adjacent.GetType().GetProperty("Coordinate").GetValue(adjacent), context.Center, null };
        Assert.That(Call(context.Manager, "TryGetAppliedCombo", lookup), Is.True);
        Assert.That(lookup[2], Is.SameAs(original));
        Assert.That(ResourceValues(context), Is.EqualTo(new[] { 4, 2, 2, 2, 0 }));
        Assert.That(events, Is.Zero);
        Assert.That(((IList)Get(context.Manager, "Results")).Count, Is.EqualTo(1));
    }

    [Test]
    public void ResultsCopyDefinitionValuesAndCapturedArraysAreIndependent()
    {
        Context context = Fixture();
        Place(context, context.B, context.Center + Vector2Int.left);
        object definition = Definition(1, 1001, 2001, Amount("Money", 2));
        Rows(context, definition);
        object result = Resolve(context).GetValue(0);
        Set(definition, "rewards", Values("ResourceAmount", Amount("Money", 99)));
        Set(definition, "comboName", "changed");
        Assert.That(Get(result, "ComboName"), Is.EqualTo("콤보 1"));
        IList rewards = (IList)Get(result, "Rewards");
        Assert.That(rewards.IsReadOnly, Is.True);
        Assert.That(Get(rewards[0], "Amount"), Is.EqualTo(2));
        Array snapshot = (Array)Call(context.Manager, "CaptureResults");
        snapshot.SetValue(null, 0);
        Assert.That(((IList)Get(context.Manager, "Results"))[0], Is.SameAs(result));
    }

    [Test]
    public void PresentationQueueAppendsAndAdvancesOnlyWhenCurrentResultIsAcknowledged()
    {
        Context context = Fixture();
        Place(context, context.B, context.Center + Vector2Int.left);
        Place(context, context.C, context.Center + Vector2Int.right);
        Rows(context, Definition(1, 1001, 2001, Amount("Money", 1)),
            Definition(2, 1001, 3001, Amount("Jobs", 1)));
        Array results = Resolve(context);
        object queue = Activator.CreateInstance(RuntimeType("ComboPresentationQueue"));
        var shown = new List<object>();
        Watch(queue, "OnCurrentChanged", value => shown.Add(value));
        Call(queue, "EnqueueResults", Values("ComboResult", results.GetValue(0)));
        Call(queue, "EnqueueResults", Values("ComboResult", results.GetValue(1)));
        Assert.That(shown.Count, Is.EqualTo(1));
        Assert.That(Call(queue, "TryComplete", results.GetValue(1)), Is.False);
        Assert.That(Call(queue, "TryComplete", results.GetValue(0)), Is.True);
        Assert.That(Get(queue, "Current"), Is.SameAs(results.GetValue(1)));
        Assert.That(Call(queue, "TryComplete", results.GetValue(0)), Is.False);
        Assert.That(Call(queue, "TryComplete", results.GetValue(1)), Is.True);
        Assert.That(Get(queue, "Current"), Is.Null);
        Assert.That(Get(queue, "PendingCount"), Is.Zero);
        Assert.That(shown, Is.EqualTo(new object[] { results.GetValue(0), results.GetValue(1), null }));
    }

    [Test]
    public void ResetRejectsStalePresentationAcknowledgementEvenWhenIdsRepeat()
    {
        Context context = Fixture();
        Place(context, context.B, context.Center + Vector2Int.left);
        Rows(context, Definition(1, 1001, 2001, Amount("Money", 1)));
        object old = Resolve(context).GetValue(0);
        Call(context.Board, "ResetBoard");
        context.Source = Place(context, context.A, context.Center);
        Place(context, context.B, context.Center + Vector2Int.left);
        object current = Resolve(context).GetValue(0);
        Assert.That(Get(old, "ResultId"), Is.EqualTo(Get(current, "ResultId")));
        Assert.That(Call(context.Manager, "TryCompletePresentation", old), Is.False);
        Assert.That(Get(context.Manager, "CurrentPresentation"), Is.SameAs(current));
    }

    [Test]
    public void BoardResetClearsLedgerAndQueueAndAllowsARebuiltPairOnce()
    {
        Context context = Fixture();
        Place(context, context.B, context.Center + Vector2Int.left);
        Rows(context, Definition(1, 1001, 2001, Amount("Money", 1)));
        Resolve(context);
        Call(context.Board, "ResetBoard");
        Assert.That((IList)Get(context.Manager, "Results"), Is.Empty);
        Assert.That(Get(context.Manager, "CurrentPresentation"), Is.Null);
        Assert.That(Get(context.Manager, "PendingPresentationCount"), Is.Zero);
        context.Source = Place(context, context.A, context.Center);
        Place(context, context.B, context.Center + Vector2Int.left);
        Resolve(context); Resolve(context);
        Assert.That(ResourceValues(context)[2], Is.EqualTo(4));
    }

    [Test]
    public void RestoreAfterBoardResetRestoresLedgerWithoutRepayingOrReplaying()
    {
        Context context = Fixture();
        Place(context, context.B, context.Center + Vector2Int.left);
        Rows(context, Definition(1, 1001, 2001, Amount("Money", 1)));
        Resolve(context);
        Array snapshot = (Array)Call(context.Manager, "CaptureResults");
        Call(context.Board, "ResetBoard");
        context.Source = Place(context, context.A, context.Center);
        Place(context, context.B, context.Center + Vector2Int.left);
        int events = 0;
        WatchResources(context, () => events++);
        Assert.That(Call(context.Manager, "TryRestoreResults", snapshot, null), Is.True);
        Assert.That(Get(context.Manager, "CurrentPresentation"), Is.Null);
        Assert.That(Resolve(context).Length, Is.Zero);
        Assert.That(ResourceValues(context)[2], Is.EqualTo(3));
        Assert.That(events, Is.Zero);
    }

    [Test]
    public void InvalidSnapshotsPreserveLedgerAndCurrentPresentation()
    {
        Context context = Fixture();
        Place(context, context.B, context.Center + Vector2Int.left);
        Rows(context, Definition(1, 1001, 2001, Amount("Money", 1)));
        object result = Resolve(context).GetValue(0);
        Context foreign = Fixture();
        Place(foreign, foreign.B, foreign.Center + Vector2Int.left);
        Rows(foreign, Definition(1, 1001, 2001, Amount("Money", 1)));
        Resolve(foreign);
        Assert.That(Call(context.Manager, "TryRestoreResults",
            Values("ComboResult", result, result), null), Is.False);
        Assert.That(Call(context.Manager, "TryRestoreResults", Call(foreign.Manager, "CaptureResults"), null), Is.False);
        Assert.That(Call(context.Manager, "TryRestoreResults", null, null), Is.False);
        Assert.That(((IList)Get(context.Manager, "Results"))[0], Is.SameAs(result));
        Assert.That(Get(context.Manager, "CurrentPresentation"), Is.SameAs(result));
    }

    [Test]
    public void AmbiguousDatabaseRowsRejectWithoutChangingExistingState()
    {
        Context context = Fixture();
        Place(context, context.B, context.Center + Vector2Int.left);
        Rows(context, Definition(1, 1001, 2001, Amount("Money", 1)));
        object existing = Resolve(context).GetValue(0);
        Rows(context, Definition(1, 1001, 2001, Amount("Money", 1)),
            Definition(2, 2001, 1001, Amount("Jobs", 1)));
        object[] args = { context.Source, null, null };
        Assert.That(Call(context.Manager, "TryResolveCombos", args), Is.False);
        Assert.That(((IList)Get(context.Manager, "Results"))[0], Is.SameAs(existing));
        Assert.That(Get(context.Manager, "CurrentPresentation"), Is.SameAs(existing));
        Assert.That(ResourceValues(context)[2], Is.EqualTo(3));
    }

    [Test]
    public void LaterComboOverflowRejectsTheWholeBatchAndDoesNotConsumeResultIds()
    {
        Context context = Fixture();
        Place(context, context.B, context.Center + Vector2Int.left);
        Place(context, context.C, context.Center + Vector2Int.right);
        object overflow = Definition(2, 1001, 3001, Amount("Money", int.MaxValue));
        Rows(context, Definition(1, 1001, 2001, Amount("Tourism", 1)), overflow);
        int events = 0;
        WatchResources(context, () => events++);
        object[] args = { context.Source, null, null };
        Assert.That(Call(context.Manager, "TryResolveCombos", args), Is.False);
        Assert.That(ResourceValues(context), Is.EqualTo(new[] { 2, 2, 2, 2, 0 }));
        Assert.That((IList)Get(context.Manager, "Results"), Is.Empty);
        Assert.That(Get(context.Manager, "CurrentPresentation"), Is.Null);
        Assert.That(events, Is.Zero);
        Set(overflow, "rewards", Values("ResourceAmount", Amount("Money", 1)));
        Array successful = Resolve(context);
        Assert.That(Get(successful.GetValue(0), "ResultId"), Is.EqualTo(1));
        Assert.That(Get(successful.GetValue(1), "ResultId"), Is.EqualTo(2));
    }

    [Test]
    public void PrototypeSerializedRowsKeepSignedRewardsAndLegacyEventSignature()
    {
        Context context = Fixture();
        Place(context, context.B, context.Center + Vector2Int.left);
        Type rowType = RuntimeType("ComboManager").GetNestedType("ComboData", BindingFlags.NonPublic);
        object row = Activator.CreateInstance(rowType, true);
        Set(row, "comboCode", 2101); Set(row, "buildingCodeA", 1001); Set(row, "buildingCodeB", 2001);
        Set(row, "rewardResource", Resource("Population")); Set(row, "rewardAmount", -1);
        IList rows = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(rowType));
        rows.Add(row); Set(context.Manager, "combos", rows); Set(context.Manager, "comboDatabase", null);
        object[] payload = null;
        var observer = new Observer { Legacy = value => payload = value };
        EventInfo info = context.Manager.GetType().GetEvent("OnComboTriggered");
        MethodInfo method = typeof(Observer).GetMethod("RecordLegacy").MakeGenericMethod(RuntimeType("ResourceType"));
        info.AddEventHandler(context.Manager, Delegate.CreateDelegate(info.EventHandlerType, observer, method));
        Call(context.Manager, "CheckCombos", context.Source);
        Assert.That(ResourceValues(context)[0], Is.EqualTo(1));
        Assert.That(payload, Is.EqualTo(new object[] { 2101, 1001, 2001, Resource("Population"), -1 }));
    }

    [Test]
    public void SessionCommitsBuildingAndCombosBeforeResourceAndBuildResolvedEvents()
    {
        Context context = Fixture(false);
        Place(context, context.B, context.Center + Vector2Int.left);
        Place(context, context.C, context.Center + Vector2Int.right);
        Rows(context, Definition(1, 1001, 2001, Amount("Money", 1)),
            Definition(2, 1001, 3001, Amount("Jobs", 3)));
        AttachSession(context);
        int resourceEvents = 0, completed = 0;
        WatchResources(context, () =>
        {
            resourceEvents++;
            Assert.That(Get(Tile(context, context.Center), "IsOccupied"), Is.True);
            Assert.That(CardIds(context.Hand), Is.EqualTo(new[] { 2 }));
            Assert.That(ResourceValues(context), Is.EqualTo(new[] { 3, 4, 3, 2, 0 }));
            Assert.That(((IList)Get(context.Manager, "Results")).Count, Is.EqualTo(2));
            Assert.That(Commit(context, 2, out _), Is.False);
        });
        buildObserver = () => completed++;
        EventInfo info = context.Session.GetType().GetEvent("OnBuildResolved");
        MethodInfo method = GetType().GetMethod("RecordBuild", BindingFlags.Instance | BindingFlags.NonPublic)
            .MakeGenericMethod(RuntimeType("BuildingInstance"), info.EventHandlerType.GetGenericArguments()[1]);
        info.AddEventHandler(context.Session, Delegate.CreateDelegate(info.EventHandlerType, this, method));
        Assert.That(Commit(context, 1, out string error), Is.True, error);
        Assert.That(resourceEvents, Is.EqualTo(3));
        Assert.That(completed, Is.EqualTo(1));
        Assert.That(((IList)Get(context.Session, "LastComboResults")).Count, Is.EqualTo(2));
        Assert.That(Get(context.Manager, "PendingPresentationCount"), Is.EqualTo(1));
    }

    [Test]
    public void SessionComboFailureRestoresBuildingCardResourcesAndComboState()
    {
        Context context = Fixture(false);
        Place(context, context.B, context.Center + Vector2Int.left);
        Rows(context, Definition(1, 1001, 2001, Amount("Money", int.MaxValue)));
        AttachSession(context);
        int events = 0;
        WatchResources(context, () => events++);
        context.Hand.GetType().GetEvent("OnHandChanged").AddEventHandler(context.Hand, new Action(() => events++));
        Watch(context.Manager, "OnComboResolved", _ => events++);
        Assert.That(Commit(context, 1, out _), Is.False);
        Assert.That(Get(Tile(context, context.Center), "IsOccupied"), Is.False);
        Assert.That(Tile(context, context.Center).transform.childCount, Is.Zero);
        Assert.That(CardIds(context.Hand), Is.EqualTo(new[] { 1, 2 }));
        Assert.That(ResourceValues(context), Is.EqualTo(new[] { 2, 2, 2, 2, 0 }));
        Assert.That((IList)Get(context.Manager, "Results"), Is.Empty);
        Assert.That(Get(context.Manager, "CurrentPresentation"), Is.Null);
        Assert.That(events, Is.Zero);
    }

    [Test]
    public void SessionRejectsAComboManagerBoundToDifferentBoardOrResources()
    {
        Context context = Fixture(false);
        Context other = Fixture(false);
        AttachSession(context);
        Call(context.Session, "ConfigureCombos", other.Manager);
        Assert.That(Commit(context, 1, out _), Is.False);
        Assert.That(CardIds(context.Hand), Is.EqualTo(new[] { 1, 2 }));
        Assert.That(ResourceValues(context), Is.EqualTo(new[] { 2, 2, 2, 2, 0 }));
    }

    [Test]
    public void ComboEventsCannotReenterResolution()
    {
        Context context = Fixture();
        Place(context, context.B, context.Center + Vector2Int.left);
        Rows(context, Definition(1, 1001, 2001, Amount("Money", 1)));
        int published = 0;
        Watch(context.Manager, "OnComboResolved", _ =>
        {
            published++;
            object[] args = { context.Source, null, null };
            Assert.That(Call(context.Manager, "TryResolveCombos", args), Is.False);
        });
        Resolve(context);
        Assert.That(published, Is.EqualTo(1));
        Assert.That(ResourceValues(context)[2], Is.EqualTo(3));
    }

    [Test]
    public void StructuredResultFormattingShowsEveryRewardAndKoreanResourceNames()
    {
        Context context = Fixture();
        Place(context, context.B, context.Center + Vector2Int.left);
        Rows(context, Definition(1, 1001, 2001,
            Amount("Money", 2), Amount("Tourism", 1), Amount("Jobs", -1)));
        object result = Resolve(context).GetValue(0);
        string text = (string)RuntimeType("ComboUI").GetMethod("FormatComboResult").Invoke(null, new[] { result });
        Assert.That(text, Does.Contain("콤보 1"));
        Assert.That(text, Does.Contain("검증용 설명"));
        Assert.That(text, Does.Contain("자금 +2"));
        Assert.That(text, Does.Contain("관광 +1"));
        Assert.That(text, Does.Contain("일자리 -1"));
    }

    [Test]
    public void AuthoredGddDatabaseImportsThirtyEightUniquePairsIncludingSharedSpecialCode()
    {
        var database = AssetDatabase.LoadAssetAtPath(
            "Assets/_UrbanEquation/Data/Combos/ComboDatabase.asset", RuntimeType("ComboDatabase"));
        Assert.That(database, Is.Not.Null);
        var errors = new List<string>();
        Call(database, "Validate", errors);
        Assert.That(errors, Is.Empty);
        Assert.That(((IList)Get(database, "Combos")).Count, Is.EqualTo(38));
        string[] resourceNames = { "Population", "Jobs", "Money", "Logistics", "Tourism" };
        int[] first = { 11001, 21001, 31001, 41001, 51001 };
        foreach (int building in first)
        {
            int group = Array.IndexOf(first, building);
            object[] args = { building, building, null };
            Assert.That(Call(database, "TryGetCombo", args), Is.True);
            IList rewards = (IList)Get(args[2], "Rewards");
            Assert.That(Get(rewards[0], "Resource"), Is.EqualTo(Resource(resourceNames[group])));
            Assert.That(Get(rewards[0], "Amount"), Is.EqualTo(1));
        }
        object[] industrial = { 43001, 43001, null }, landmark = { 53001, 53001, null };
        Assert.That(Call(database, "TryGetCombo", industrial), Is.True);
        Assert.That(Call(database, "TryGetCombo", landmark), Is.True);
        Assert.That(Get(industrial[2], "ComboCode"), Is.EqualTo(43001));
        Assert.That(Get(landmark[2], "ComboCode"), Is.EqualTo(43001));
        Assert.That(Get(industrial[2], "ComboName"), Is.EqualTo("대규모 공업 지대"));
        Assert.That(Get(landmark[2], "ComboName"), Is.EqualTo("랜드마크"));
        Assert.That(Get(((IList)Get(industrial[2], "Rewards"))[0], "Resource"), Is.EqualTo(Resource("Jobs")));
        Assert.That(Get(((IList)Get(landmark[2], "Rewards"))[0], "Resource"), Is.EqualTo(Resource("Logistics")));
    }
}

