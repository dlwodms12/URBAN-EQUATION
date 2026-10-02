using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Reflection preserves the existing runtime/test assembly boundaries.
public class BuildingContractTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private readonly List<ScriptableObject> assets = new List<ScriptableObject>();
    private Action resourceObserver;
    private Action commitObserver;
    private static readonly string[] ResourceNames = { "Population", "Jobs", "Money", "Logistics", "Tourism" };

    private sealed class Context
    {
        public Component Board, Resources, Hand, Session, Prefab;
        public ScriptableObject Stage, Building;
    }

    private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static Type LoadedType(string name)
    {
        Type known = Type.GetType(name + ", Unity.ugui", false)
            ?? Type.GetType(name + ", UnityEngine.UI", false);
        if (known != null) return known;
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(name);
            if (type != null) return type;
        }
        throw new InvalidOperationException("Type not loaded: " + name);
    }

    private GameObject Object(string name, bool active = false, bool rect = false)
    {
        var value = rect ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
        value.SetActive(active);
        objects.Add(value);
        return value;
    }

    private Component Component(string name, string type, bool active = false) =>
        Object(name, active).AddComponent(RuntimeType(type));

    private ScriptableObject Asset(string type)
    {
        var value = ScriptableObject.CreateInstance(RuntimeType(type));
        assets.Add(value);
        return value;
    }

    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static object Get(object target, string property) => target.GetType().GetProperty(property).GetValue(target);
    private static object Field(object target, string name) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static object Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method).Invoke(target, args);
    // Runtime callbacks are not relied on in synchronous EditMode tests.
    private static void Lifecycle(object target, string method) =>
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    private static object Resource(string name) => Enum.Parse(RuntimeType("ResourceType"), name);
    private static object Amount(string name, int value) =>
        Activator.CreateInstance(RuntimeType("ResourceAmount"), Resource(name), value);

    private static Array Values(string type, params object[] values)
    {
        var result = Array.CreateInstance(RuntimeType(type), values.Length);
        for (int i = 0; i < values.Length; i++) result.SetValue(values[i], i);
        return result;
    }

    private ScriptableObject Building(int code = 1001, string cost = "Jobs", int amount = 1)
    {
        var building = Asset("BuildingData");
        Set(building, "buildingCode", code);
        Set(building, "buildingName", "Building " + code);
        Set(building, "useResourceLists", true);
        Set(building, "requiredResources", Values("ResourceAmount", Amount(cost, amount)));
        Set(building, "gainedResources", Values("ResourceAmount", Amount("Population", 1)));
        Set(building, "allowedTileTypes", Values("TileType", Enum.Parse(RuntimeType("TileType"), "Grass")));
        GameObject visual = Object("Visual template", true);
        visual.transform.position = new Vector3(10000f, 10000f, 10000f);
        visual.AddComponent<BoxCollider>();
        Set(building, "visualPrefab", visual);
        return building;
    }

    private static object Entry(object building, int count)
    {
        object value = Activator.CreateInstance(RuntimeType("StageBuildingCardData"));
        Set(value, "building", building);
        Set(value, "count", count);
        return value;
    }

    private ScriptableObject Stage(params object[] entries)
    {
        var template = Component("Tile template", "Tile", true);
        template.transform.position = new Vector3(10000f, 10000f, 10000f);
        template.gameObject.AddComponent<BoxCollider>();
        var grass = Asset("TileData");
        Set(grass, "tileType", Enum.Parse(RuntimeType("TileType"), "Grass"));
        Set(grass, "tilePrefab", template);
        var concrete = Asset("TileData");
        Set(concrete, "tileType", Enum.Parse(RuntimeType("TileType"), "Concrete"));
        Set(concrete, "tilePrefab", template);
        var stage = Asset("StageData");
        Set(stage, "width", 3); Set(stage, "height", 1);
        Set(stage, "tiles", Values("TileData", grass, grass, concrete));
        Set(stage, "buildingCards", Values("StageBuildingCardData", entries));
        return stage;
    }

    private Component Resources()
    {
        Component value = Component("Resources", "ResourceManager");
        Call(value, "CaptureResourceState");
        return value;
    }

    private static void InitializeHand(object hand, object stage, object resources)
    {
        object[] args = { stage, resources, null };
        Assert.That(Call(hand, "TryInitializeFromStage", args), Is.True, args[2] as string);
    }

    private Context Fixture(ScriptableObject building = null, int count = 3)
    {
        var context = new Context();
        context.Building = building == null ? Building() : building;
        context.Stage = Stage(Entry(context.Building, count));
        context.Board = Component("Board", "BoardManager");
        context.Board.transform.position = new Vector3(3000f, 10f, 3000f);
        object[] args = { context.Stage, null };
        Assert.That(Call(context.Board, "TryCreateBoard", args), Is.True, args[1] as string);
        context.Resources = Resources();
        context.Hand = Component("Hand", "BuildingHandManager", true);
        InitializeHand(context.Hand, context.Stage, context.Resources);
        context.Prefab = Component("Building template", "BuildingInstance");
        context.Session = Component("Session", "GameSessionManager");
        Call(context.Session, "Configure", context.Board, context.Resources, context.Hand, context.Prefab);
        return context;
    }

    private static int[] CardIds(object hand)
    {
        var cards = (IList)Get(hand, "Cards");
        var ids = new int[cards.Count];
        for (int i = 0; i < cards.Count; i++) ids[i] = (int)Get(cards[i], "CardId");
        return ids;
    }

    private static int[] ResourceValues(object resources)
    {
        var values = new int[ResourceNames.Length];
        for (int i = 0; i < values.Length; i++) values[i] = (int)Call(resources, "GetResource", Resource(ResourceNames[i]));
        return values;
    }

    private static Component Tile(Context context, int x = 0) =>
        (Component)Call(context.Board, "GetTile", new Vector2Int(x, 0));

    private static bool Commit(Context context, int id, int x, out object building, out string error)
    {
        object[] args = { id, new Vector2Int(x, 0), null, null };
        bool result = (bool)Call(context.Session, "TryCommitBuild", args);
        building = args[2]; error = args[3] as string;
        return result;
    }

    private void WatchResources(object resources, Action observer)
    {
        resourceObserver = observer;
        EventInfo info = resources.GetType().GetEvent("OnResourceChanged");
        MethodInfo method = GetType().GetMethod("RecordResource", BindingFlags.Instance | BindingFlags.NonPublic)
            .MakeGenericMethod(RuntimeType("ResourceType"));
        info.AddEventHandler(resources, Delegate.CreateDelegate(info.EventHandlerType, this, method));
    }

    private void RecordResource<T>(T resource, int amount) => resourceObserver?.Invoke();
    private void WatchCommit(object session, Action observer)
    {
        commitObserver = observer;
        EventInfo info = session.GetType().GetEvent("OnBuildingCommitted");
        MethodInfo method = GetType().GetMethod("RecordCommit", BindingFlags.Instance | BindingFlags.NonPublic)
            .MakeGenericMethod(RuntimeType("BuildingInstance"), RuntimeType("BuildingCardState"));
        info.AddEventHandler(session, Delegate.CreateDelegate(info.EventHandlerType, this, method));
    }
    private void RecordCommit<TBuilding, TCard>(TBuilding building, TCard card) => commitObserver?.Invoke();
    private static void WatchAction(object target, string name, Action action) =>
        target.GetType().GetEvent(name).AddEventHandler(target, action);

    private Component Controller(Context context, Camera camera = null)
    {
        Component result = Component("Placement controller", "BuildingPlacementController");
        Call(result, "Configure", context.Session, camera);
        return result;
    }

    private Component CardTemplate()
    {
        GameObject root = Object("Card template", false, true);
        root.AddComponent(LoadedType("UnityEngine.UI.Button"));
        Component card = root.AddComponent(RuntimeType("BuildingCardUI"));
        GameObject overlayRoot = Object("Disabled overlay", true, true);
        overlayRoot.transform.SetParent(root.transform, false);
        Component overlay = overlayRoot.AddComponent(LoadedType("UnityEngine.UI.Image"));
        Set(card, "disabledOverlay", overlay);
        return card;
    }

    private Component HandView(Context context, Component controller, out Transform content)
    {
        content = Object("Card content", true, true).transform;
        Component view = Component("Hand view", "BuildingHandUI", true);
        Call(view, "Bind", context.Hand, controller, CardTemplate(), content);
        return view;
    }

    private static int[] ViewIds(Transform content)
    {
        var result = new int[content.childCount];
        for (int i = 0; i < result.Length; i++)
            result[i] = (int)Get(content.GetChild(i).GetComponent(RuntimeType("BuildingCardUI")), "CardId");
        return result;
    }

    [TearDown]
    public void TearDown()
    {
        resourceObserver = null; commitObserver = null;
        for (int i = objects.Count - 1; i >= 0; i--)
            if (objects[i] != null) UnityEngine.Object.DestroyImmediate(objects[i]);
        foreach (ScriptableObject asset in assets) UnityEngine.Object.DestroyImmediate(asset);
        objects.Clear(); assets.Clear();
    }

    [Test]
    public void StageEntriesExpandToDistinctCardsInDocumentOrder()
    {
        var a = Building();
        var b = Building(2001);
        var stage = Stage(Entry(a, 2), Entry(b, 1), Entry(a, 1));
        var hand = Component("Hand", "BuildingHandManager", true);
        InitializeHand(hand, stage, Resources());
        IList cards = (IList)Get(hand, "Cards");
        Assert.That(CardIds(hand), Is.EqualTo(new[] { 1, 2, 3, 4 }));
        Assert.That(cards[0], Is.Not.SameAs(cards[1]));
        Assert.That(Get(cards[0], "Building"), Is.SameAs(a));
        Assert.That(Get(cards[2], "Building"), Is.SameAs(b));
        Assert.That(Get(cards[3], "Building"), Is.SameAs(a));
        Assert.That(cards.IsReadOnly, Is.True);
    }

    [Test]
    public void InvalidInitializationKeepsHandAndResourceBindingWithoutEvents()
    {
        Context context = Fixture();
        Array before = (Array)Call(context.Hand, "CaptureCards");
        int changes = 0;
        WatchAction(context.Hand, "OnHandChanged", () => changes++);
        object[] args = { Stage(Entry(context.Building, 0)), Resources(), null };
        Assert.That(Call(context.Hand, "TryInitializeFromStage", args), Is.False);
        Assert.That(Get(context.Hand, "Resources"), Is.SameAs(context.Resources));
        Assert.That((IList)Get(context.Hand, "Cards"), Is.EqualTo(before));
        Assert.That(changes, Is.Zero);
        object[] empty = { Stage(), context.Resources, null };
        Assert.That(Call(context.Hand, "TryInitializeFromStage", empty), Is.False);
        var conflicting = Building(1001);
        object[] duplicateCode = { Stage(Entry(context.Building, 1), Entry(conflicting, 1)), context.Resources, null };
        Assert.That(Call(context.Hand, "TryInitializeFromStage", duplicateCode), Is.False);
        Assert.That(changes, Is.Zero);
    }

    [Test]
    public void CardAvailabilityUsesResourcesAndUpdatesWithoutChangingOrder()
    {
        Context context = Fixture(Building(1001, "Tourism", 1));
        int availability = 0;
        WatchAction(context.Hand, "OnCardAvailabilityChanged", () => availability++);
        Assert.That(Call(context.Hand, "IsCardAvailable", 1), Is.False);
        Call(context.Resources, "Add", Resource("Tourism"), 1);
        Assert.That(Call(context.Hand, "IsCardAvailable", 1), Is.True);
        Assert.That(Call(context.Hand, "IsCardAvailable", 99), Is.False);
        Assert.That(Call(context.Resources, "Consume", Resource("Money"), 99), Is.False);
        Assert.That(availability, Is.EqualTo(1));
        Assert.That(CardIds(context.Hand), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void DisabledHandUnsubscribesAndReenabledHandObservesResourcesAgain()
    {
        Context context = Fixture();
        int availability = 0;
        WatchAction(context.Hand, "OnCardAvailabilityChanged", () => availability++);
        context.Hand.gameObject.SetActive(false);
        Lifecycle(context.Hand, "OnDisable");
        Call(context.Resources, "Add", Resource("Money"), 1);
        Assert.That(availability, Is.Zero);
        context.Hand.gameObject.SetActive(true);
        Lifecycle(context.Hand, "OnEnable");
        Call(context.Resources, "Add", Resource("Money"), 1);
        Assert.That(availability, Is.EqualTo(1));
    }

    [Test]
    public void CardSnapshotRestoresOrderAndResetUsesIndependentInitialOrder()
    {
        Context context = Fixture();
        Array snapshot = (Array)Call(context.Hand, "CaptureCards");
        object first = snapshot.GetValue(0);
        snapshot.SetValue(snapshot.GetValue(2), 0);
        snapshot.SetValue(first, 2);
        Assert.That(CardIds(context.Hand), Is.EqualTo(new[] { 1, 2, 3 }));
        object[] args = { snapshot, null };
        Assert.That(Call(context.Hand, "TryRestoreCards", args), Is.True);
        Assert.That(CardIds(context.Hand), Is.EqualTo(new[] { 3, 2, 1 }));
        snapshot.SetValue(null, 0);
        Assert.That(CardIds(context.Hand), Is.EqualTo(new[] { 3, 2, 1 }));
        Call(context.Hand, "ResetCards");
        Assert.That(CardIds(context.Hand), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void InvalidOrForeignCardSnapshotsHaveNoPartialEffects()
    {
        Context context = Fixture();
        Context foreign = Fixture();
        Array snapshot = (Array)Call(context.Hand, "CaptureCards");
        snapshot.SetValue(snapshot.GetValue(0), 1);
        int changes = 0;
        WatchAction(context.Hand, "OnHandChanged", () => changes++);
        Assert.That(Call(context.Hand, "TryRestoreCards", snapshot, null), Is.False);
        Assert.That(Call(context.Hand, "TryRestoreCards", Call(foreign.Hand, "CaptureCards"), null), Is.False);
        Assert.That(Call(context.Hand, "TryRestoreCards", null, null), Is.False);
        Assert.That(CardIds(context.Hand), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(changes, Is.Zero);
    }

    [Test]
    public void PlacementRejectsMissingCardOutOfBoundsDisallowedAndOccupiedTiles()
    {
        Context context = Fixture();
        Assert.That(Commit(context, 99, 0, out _, out _), Is.False);
        Assert.That(Commit(context, 1, -1, out _, out _), Is.False);
        Assert.That(Commit(context, 1, 3, out _, out _), Is.False);
        Assert.That(Commit(context, 1, 2, out _, out _), Is.False);
        Component occupied = Component("Existing building", "BuildingInstance");
        Call(Tile(context), "TrySetBuilding", occupied);
        Assert.That(Commit(context, 1, 0, out _, out _), Is.False);
        Assert.That(Get(Tile(context), "Building"), Is.SameAs(occupied));
        Assert.That(CardIds(context.Hand), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(ResourceValues(context.Resources), Is.EqualTo(new[] { 2, 2, 2, 2, 0 }));
    }

    [Test]
    public void SuccessfulBuildConsumesOnlyTheSelectedCardAndKeepsRemainingOrder()
    {
        Context context = Fixture();
        int handChanges = 0, commits = 0;
        WatchAction(context.Hand, "OnHandChanged", () => handChanges++);
        WatchCommit(context.Session, () => commits++);
        Assert.That(Commit(context, 2, 0, out object building, out string error), Is.True, error);
        Assert.That(Get(Tile(context), "Building"), Is.SameAs(building));
        Assert.That(Get(building, "Data"), Is.SameAs(context.Building));
        Assert.That(Get(building, "Coordinate"), Is.EqualTo(Vector2Int.zero));
        Assert.That(((Component)building).transform.parent, Is.SameAs(Tile(context).transform));
        Assert.That(((Component)building).gameObject.activeSelf, Is.True);
        Assert.That(CardIds(context.Hand), Is.EqualTo(new[] { 1, 3 }));
        Assert.That(ResourceValues(context.Resources), Is.EqualTo(new[] { 3, 1, 2, 2, 0 }));
        Assert.That(handChanges, Is.EqualTo(1));
        Assert.That(commits, Is.EqualTo(1));
    }

    [Test]
    public void ResourceShortageAtCommitRejectsWithoutBuildingOrCardConsumption()
    {
        Context context = Fixture();
        Assert.That(Call(context.Hand, "IsCardAvailable", 1), Is.True);
        Call(context.Resources, "Consume", Resource("Jobs"), 2);
        int[] before = ResourceValues(context.Resources);
        int events = 0;
        WatchResources(context.Resources, () => events++);
        WatchAction(context.Hand, "OnHandChanged", () => events++);
        WatchCommit(context.Session, () => events++);
        Assert.That(Commit(context, 1, 0, out object building, out string error), Is.False);
        Assert.That(building, Is.Null); Assert.That(error, Is.Not.Empty);
        Assert.That(Get(Tile(context), "IsOccupied"), Is.False);
        Assert.That(CardIds(context.Hand), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(ResourceValues(context.Resources), Is.EqualTo(before));
        Assert.That(events, Is.Zero);
    }

    [Test]
    public void OverflowRollsBackTileAndExactCardPositionWithoutEventsOrCandidateLeaks()
    {
        var building = Building();
        Set(building, "gainedResources", Values("ResourceAmount", Amount("Money", int.MaxValue)));
        Context context = Fixture(building);
        int events = 0;
        WatchResources(context.Resources, () => events++);
        WatchAction(context.Hand, "OnHandChanged", () => events++);
        WatchCommit(context.Session, () => events++);
        Assert.That(Commit(context, 2, 0, out _, out _), Is.False);
        Assert.That(Get(Tile(context), "IsOccupied"), Is.False);
        Assert.That(Tile(context).transform.childCount, Is.Zero);
        Assert.That(CardIds(context.Hand), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(ResourceValues(context.Resources), Is.EqualTo(new[] { 2, 2, 2, 2, 0 }));
        Assert.That(events, Is.Zero);
    }

    [Test]
    public void ResourceEventsObserveCommittedTileCardAndResourcesAndRejectNestedBuilds()
    {
        Context context = Fixture();
        int notifications = 0;
        WatchResources(context.Resources, () =>
        {
            notifications++;
            Assert.That(Get(Tile(context), "IsOccupied"), Is.True);
            Component building = (Component)Get(Tile(context), "Building");
            Assert.That(building.gameObject.activeSelf, Is.True);
            Assert.That(building.transform.parent, Is.SameAs(Tile(context).transform));
            Assert.That(CardIds(context.Hand), Is.EqualTo(new[] { 2, 3 }));
            Assert.That(ResourceValues(context.Resources), Is.EqualTo(new[] { 3, 1, 2, 2, 0 }));
            Assert.That(Commit(context, 2, 1, out _, out _), Is.False);
        });
        Assert.That(Commit(context, 1, 0, out _, out _), Is.True);
        Assert.That(notifications, Is.EqualTo(2));
        Assert.That(Get(Tile(context, 1), "IsOccupied"), Is.False);
        // The guard is released after the completed build.
        resourceObserver = null;
        Assert.That(Commit(context, 2, 1, out _, out _), Is.True);
    }

    [Test]
    public void MissingPrefabVisualOrMismatchedResourcesRejectBeforeMutation()
    {
        Context context = Fixture();
        Call(context.Session, "Configure", context.Board, context.Resources, context.Hand, null);
        Assert.That(Commit(context, 1, 0, out _, out _), Is.False);
        Call(context.Session, "Configure", context.Board, context.Resources, context.Hand, context.Prefab);
        Set(context.Building, "visualPrefab", null);
        Assert.That(Commit(context, 1, 0, out _, out _), Is.False);
        Call(context.Session, "Configure", context.Board, Resources(), context.Hand, context.Prefab);
        Assert.That(Commit(context, 1, 0, out _, out _), Is.False);
        Assert.That(CardIds(context.Hand), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(ResourceValues(context.Resources), Is.EqualTo(new[] { 2, 2, 2, 2, 0 }));
        Assert.That(Get(Tile(context), "IsOccupied"), Is.False);
    }

    [Test]
    public void ConsumedCardCannotBeReusedAndSnapshotRestoresItsOriginalIdentity()
    {
        Context context = Fixture();
        Array snapshot = (Array)Call(context.Hand, "CaptureCards");
        Assert.That(Commit(context, 2, 0, out _, out _), Is.True);
        int[] resources = ResourceValues(context.Resources);
        Assert.That(Commit(context, 2, 1, out _, out _), Is.False);
        Assert.That(Get(Tile(context, 1), "IsOccupied"), Is.False);
        Assert.That(ResourceValues(context.Resources), Is.EqualTo(resources));
        Assert.That(Call(context.Hand, "TryRestoreCards", snapshot, null), Is.True);
        Assert.That(CardIds(context.Hand), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(((IList)Get(context.Hand, "Cards"))[1], Is.SameAs(snapshot.GetValue(1)));
    }

    [Test]
    public void PreviewDisablesAllCollidersAndCancelConsumesNoResourcesOrCards()
    {
        Context context = Fixture();
        GameObject visual = (GameObject)Get(context.Building, "VisualPrefab");
        GameObject child = Object("Inactive visual child");
        child.transform.SetParent(visual.transform, false);
        child.AddComponent<BoxCollider>();
        Component controller = Controller(context);
        Assert.That(Call(controller, "TryBeginDrag", 1), Is.True);
        Component preview = (Component)Field(controller, "preview");
        foreach (Collider collider in preview.GetComponentsInChildren<Collider>(true))
            Assert.That(collider.enabled, Is.False);
        Assert.That(Get(controller, "SelectedCardId"), Is.EqualTo(1));
        Call(controller, "CancelDrag");
        Assert.That(Get(controller, "IsDragging"), Is.False);
        Assert.That(Get(controller, "SelectedCardId"), Is.Zero);
        Assert.That(controller.transform.childCount, Is.Zero);
        Assert.That(Get(Tile(context), "IsOccupied"), Is.False);
        Assert.That(CardIds(context.Hand), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(ResourceValues(context.Resources), Is.EqualTo(new[] { 2, 2, 2, 2, 0 }));
        Call(context.Resources, "Consume", Resource("Jobs"), 2);
        Assert.That(Call(controller, "TryBeginDrag", 1), Is.False);
        Assert.That(controller.transform.childCount, Is.Zero);
    }

    [Test]
    public void DragCancelsWhenAffordabilityOrCardIdentityChanges()
    {
        Context context = Fixture();
        Component controller = Controller(context);
        Assert.That(Call(controller, "TryBeginDrag", 1), Is.True);
        Call(context.Resources, "Consume", Resource("Jobs"), 2);
        Call(controller, "UpdatePointer", Vector2.zero);
        Assert.That(Get(controller, "IsDragging"), Is.False);
        Call(context.Resources, "ResetResources");
        Assert.That(Call(controller, "TryBeginDrag", 1), Is.True);
        InitializeHand(context.Hand, context.Stage, context.Resources);
        Call(controller, "UpdatePointer", Vector2.zero);
        Assert.That(Get(controller, "IsDragging"), Is.False);
        Assert.That(Get(Tile(context), "IsOccupied"), Is.False);
        Assert.That(CardIds(context.Hand), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void ReleasingOutsideBoardCancelsWithoutStateChanges()
    {
        Context context = Fixture();
        Component controller = Controller(context);
        Assert.That(Call(controller, "TryBeginDrag", 1), Is.True);
        object[] args = { Vector2.zero, null };
        Assert.That(Call(controller, "TryFinishDrag", args), Is.False);
        Assert.That(args[1], Is.Not.Null);
        Assert.That(Get(controller, "IsDragging"), Is.False);
        Assert.That(Get(Tile(context), "IsOccupied"), Is.False);
        Assert.That(CardIds(context.Hand), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(ResourceValues(context.Resources), Is.EqualTo(new[] { 2, 2, 2, 2, 0 }));
    }

    [Test]
    public void PointerRaycastSnapsPreviewAndCommitsOnlyOnAValidBoardTile()
    {
        Context context = Fixture();
        context.Board.gameObject.SetActive(true);
        Camera camera = Object("Test camera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4f;
        camera.transform.position = Tile(context).transform.position + Vector3.up * 10f;
        camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        Component controller = Controller(context, camera);
        Physics.SyncTransforms();
        Vector2 screen = camera.WorldToScreenPoint(Tile(context).transform.position);
        Assert.That(Call(controller, "TryBeginDrag", 1), Is.True);
        Call(controller, "UpdatePointer", screen);
        Assert.That(Get(controller, "PreviewTile"), Is.SameAs(Tile(context)));
        Assert.That(Get(controller, "PreviewCanPlace"), Is.True);
        Component preview = (Component)Field(controller, "preview");
        Assert.That(Vector3.Distance(preview.transform.position, Tile(context).transform.position),
            Is.LessThanOrEqualTo(0.00001f));
        object[] args = { screen, null };
        Assert.That(Call(controller, "TryFinishDrag", args), Is.True, args[1] as string);
        Assert.That(Get(controller, "IsDragging"), Is.False);
        Assert.That(Get(Tile(context), "IsOccupied"), Is.True);
        Assert.That(CardIds(context.Hand), Is.EqualTo(new[] { 2, 3 }));
    }

    [Test]
    public void HandViewRemovesOnlyConsumedCardAndRestoresSiblingOrder()
    {
        Context context = Fixture();
        Component controller = Controller(context);
        HandView(context, controller, out Transform content);
        Assert.That(ViewIds(content), Is.EqualTo(new[] { 1, 2, 3 }));
        Transform firstView = content.GetChild(0);
        Array snapshot = (Array)Call(context.Hand, "CaptureCards");
        Assert.That(Commit(context, 2, 0, out _, out _), Is.True);
        Assert.That(ViewIds(content), Is.EqualTo(new[] { 1, 3 }));
        Assert.That(content.GetChild(0), Is.SameAs(firstView));
        object first = snapshot.GetValue(0);
        snapshot.SetValue(snapshot.GetValue(2), 0); snapshot.SetValue(first, 2);
        Assert.That(Call(context.Hand, "TryRestoreCards", snapshot, null), Is.True);
        Assert.That(ViewIds(content), Is.EqualTo(new[] { 3, 2, 1 }));
        Assert.That(content.GetChild(2), Is.SameAs(firstView));
    }

    [Test]
    public void DisabledCardOverlayAndPointerBlockFollowResourceAvailability()
    {
        Context context = Fixture(Building(1001, "Tourism", 1));
        Component controller = Controller(context);
        HandView(context, controller, out Transform content);
        Component card = content.GetChild(0).GetComponent(RuntimeType("BuildingCardUI"));
        Component button = card.GetComponent(LoadedType("UnityEngine.UI.Button"));
        Component overlay = (Component)Field(card, "disabledOverlay");
        Assert.That(Get(button, "interactable"), Is.False);
        Assert.That(overlay.gameObject.activeSelf, Is.True);
        Assert.That(Get(overlay, "color"), Is.EqualTo(new Color(0f, 0f, 0f, 0.5f)));
        Assert.That(Get(overlay, "raycastTarget"), Is.False);
        object pointer = Activator.CreateInstance(LoadedType("UnityEngine.EventSystems.PointerEventData"),
            new object[] { null });
        Call(card, "OnPointerDown", pointer);
        Assert.That(Get(controller, "IsDragging"), Is.False);
        Call(context.Resources, "Add", Resource("Tourism"), 1);
        Assert.That(Get(button, "interactable"), Is.True);
        Assert.That(overlay.gameObject.activeSelf, Is.False);
        Call(card, "OnPointerDown", pointer);
        Assert.That(Get(controller, "IsDragging"), Is.True);
        Call(controller, "CancelDrag");
    }

    [Test]
    public void PrototypeSelectionAndSlotAvailabilityUseAllRequiredResources()
    {
        ScriptableObject building = Building(1001, "Tourism", 1);
        Component resources = Resources();
        Component database = Component("Legacy database", "BuildingDatabase");
        IList definitions = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(RuntimeType("BuildingData")));
        definitions.Add(building);
        Set(database, "buildings", definitions);
        Lifecycle(database, "Awake");
        Component placement = Component("Legacy placement", "BuildingPlacement");
        Set(placement, "buildingDatabase", database);
        Set(placement, "resourceManager", resources);
        Lifecycle(placement, "OnEnable");
        Assert.That(Call(placement, "CanSelectBuilding", 1001), Is.False);
        Call(placement, "StartBuildingDrag", 1001);
        Assert.That(Field(placement, "previewBuilding"), Is.Null);

        GameObject slotRoot = Object("Legacy slot", false, true);
        Component button = slotRoot.AddComponent(LoadedType("UnityEngine.UI.Button"));
        Component slot = slotRoot.AddComponent(RuntimeType("BuildingSlotUI"));
        Set(slot, "buildingPlacement", placement);
        Set(slot, "buildingDatabase", database);
        Set(slot, "buildingCode", 1001);
        Lifecycle(slot, "Awake");
        Lifecycle(slot, "Start");
        Assert.That(Get(button, "interactable"), Is.False);
        Call(resources, "Add", Resource("Tourism"), 1);
        Assert.That(Call(placement, "CanSelectBuilding", 1001), Is.True);
        Assert.That(Get(button, "interactable"), Is.True);
        Assert.That(Call(placement, "GetRemainingCount", 1001), Is.EqualTo(2));
        Lifecycle(slot, "OnDestroy");
        Lifecycle(placement, "OnDisable");
    }
}
