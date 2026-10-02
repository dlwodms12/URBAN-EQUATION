using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Keep runtime Assembly-CSharp and the existing prototype assembly boundaries intact.
public class ResourceContractTests
{
    private static readonly string[] Names = { "Population", "Jobs", "Money", "Logistics", "Tourism" };
    private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();
    private readonly List<string> changes = new List<string>();
    private readonly List<int[]> observedStates = new List<int[]>();
    private object watchedManager;
    private int batchCount;

    private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Resource(string name) => Enum.Parse(RuntimeType("ResourceType"), name);
    private static object Amount(string name, int value) =>
        Activator.CreateInstance(RuntimeType("ResourceAmount"), Resource(name), value);

    private static Array Amounts(params object[] values)
    {
        Array result = Array.CreateInstance(RuntimeType("ResourceAmount"), values.Length);
        for (int i = 0; i < values.Length; i++) result.SetValue(values[i], i);
        return result;
    }

    private static Array FullState(params int[] values)
    {
        Assert.That(values.Length, Is.EqualTo(Names.Length));
        var result = new object[Names.Length];
        for (int i = 0; i < Names.Length; i++) result[i] = Amount(Names[i], values[i]);
        return Amounts(result);
    }

    private static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    private static object Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method).Invoke(target, args);

    private static int[] ReadAll(object manager)
    {
        var result = new int[Names.Length];
        for (int i = 0; i < Names.Length; i++)
            result[i] = (int)Call(manager, "GetResource", Resource(Names[i]));
        return result;
    }

    private object Manager()
    {
        var root = new GameObject("ResourceContractTest");
        root.SetActive(false);
        created.Add(root);
        object manager = root.AddComponent(RuntimeType("ResourceManager"));
        Call(manager, "CaptureResourceState");
        return manager;
    }

    private ScriptableObject Data(string name)
    {
        ScriptableObject result = ScriptableObject.CreateInstance(RuntimeType(name));
        created.Add(result);
        return result;
    }

    private object Stage(params int[] values)
    {
        object result = Data("StageData");
        Set(result, "initialResources", FullState(values));
        return result;
    }

    private object Building(Array costs, Array gains)
    {
        object result = Data("BuildingData");
        Set(result, "useResourceLists", true);
        Set(result, "requiredResources", costs);
        Set(result, "gainedResources", gains);
        return result;
    }

    private void Watch(object manager)
    {
        watchedManager = manager;
        EventInfo perResource = manager.GetType().GetEvent("OnResourceChanged");
        MethodInfo method = GetType().GetMethod("RecordChange", BindingFlags.Instance | BindingFlags.NonPublic)
            .MakeGenericMethod(RuntimeType("ResourceType"));
        perResource.AddEventHandler(manager, Delegate.CreateDelegate(perResource.EventHandlerType, this, method));
        manager.GetType().GetEvent("OnResourcesChanged").AddEventHandler(manager, new Action(() => batchCount++));
    }

    private void RecordChange<T>(T resource, int value)
    {
        changes.Add(resource.ToString() + ":" + value);
        observedStates.Add(ReadAll(watchedManager));
    }

    private void AssertNoChange(object manager, int[] before)
    {
        Assert.That(ReadAll(manager), Is.EqualTo(before));
        Assert.That(changes, Is.Empty);
        Assert.That(batchCount, Is.Zero);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (UnityEngine.Object value in created) UnityEngine.Object.DestroyImmediate(value);
        created.Clear();
        changes.Clear();
        observedStates.Clear();
        watchedManager = null;
        batchCount = 0;
    }

    [Test]
    public void PrototypeDefaultsAndLegacyMethodsKeepWorkingForFiveResources()
    {
        object manager = Manager();
        Assert.That(ReadAll(manager), Is.EqualTo(new[] { 2, 2, 2, 2, 0 }));
        Assert.That(Call(manager, "CanConsume", Resource("Jobs"), 2), Is.True);
        Assert.That(Call(manager, "Consume", Resource("Jobs"), 2), Is.True);
        Assert.That(Call(manager, "Consume", Resource("Jobs"), 1), Is.False);
        Call(manager, "Add", Resource("Tourism"), 3);
        Call(manager, "Add", Resource("Money"), -3);
        Assert.That(ReadAll(manager), Is.EqualTo(new[] { 2, 0, -1, 2, 3 }));
        Call(manager, "ResetResources");
        Assert.That(ReadAll(manager), Is.EqualTo(new[] { 2, 2, 2, 2, 0 }));
    }

    [Test]
    public void StageInitialValuesBecomeRetryBaselineAndPublishAllFiveResources()
    {
        object manager = Manager();
        Watch(manager);
        object stage = Stage(8, 7, 6, 5, 4);
        Assert.That(Call(manager, "TryInitializeFromStage", stage), Is.True);
        Assert.That(ReadAll(manager), Is.EqualTo(new[] { 8, 7, 6, 5, 4 }));
        Assert.That(changes.Count, Is.EqualTo(5));
        Assert.That(batchCount, Is.EqualTo(1));
        Set(stage, "initialResources", FullState(99, 99, 99, 99, 99));
        Call(manager, "Add", Resource("Money"), 10);
        Call(manager, "ResetResources");
        Assert.That(ReadAll(manager), Is.EqualTo(new[] { 8, 7, 6, 5, 4 }));
    }

    [Test]
    public void InvalidStageInitializationPreservesResourcesAndPreviousRetryBaseline()
    {
        object manager = Manager();
        Assert.That(Call(manager, "TryInitializeFromStage", Stage(4, 5, 6, 7, 8)), Is.True);
        Call(manager, "Add", Resource("Money"), 1);
        int[] before = ReadAll(manager);
        Watch(manager);
        object incomplete = Data("StageData");
        Set(incomplete, "initialResources", Amounts(Amount("Population", 100)));
        Assert.That(Call(manager, "TryInitializeFromStage", incomplete), Is.False);
        Assert.That(Call(manager, "TryInitializeFromStage", Stage(4, 5, -1, 7, 8)), Is.False);
        Assert.That(Call(manager, "TryInitializeFromStage", new object[] { null }), Is.False);
        AssertNoChange(manager, before);
        Call(manager, "ResetResources");
        Assert.That(ReadAll(manager), Is.EqualTo(new[] { 4, 5, 6, 7, 8 }));
    }

    [Test]
    public void BuildingCommitsMultipleCostsAndGainsBeforePublishingEvents()
    {
        object manager = Manager();
        Watch(manager);
        object building = Building(
            Amounts(Amount("Jobs", 2), Amount("Money", 1)),
            Amounts(Amount("Population", 3), Amount("Money", 4), Amount("Tourism", 1)));
        Assert.That(Call(manager, "TryApplyBuildingResources", building), Is.True);
        int[] expected = { 5, 0, 5, 2, 1 };
        Assert.That(ReadAll(manager), Is.EqualTo(expected));
        Assert.That(changes, Is.EqualTo(new[] { "Population:5", "Jobs:0", "Money:5", "Tourism:1" }));
        Assert.That(batchCount, Is.EqualTo(1));
        foreach (int[] observed in observedStates) Assert.That(observed, Is.EqualTo(expected));
    }

    [Test]
    public void UnaffordableBuildingRejectsEveryCostGainAndEvent()
    {
        object manager = Manager();
        int[] before = ReadAll(manager);
        Watch(manager);
        object building = Building(Amounts(Amount("Population", 1), Amount("Tourism", 1)),
            Amounts(Amount("Money", 10)));
        Assert.That(Call(manager, "TryApplyBuildingResources", building), Is.False);
        AssertNoChange(manager, before);
    }

    [Test]
    public void BuildingGainsCannotFundItsOwnUpfrontCost()
    {
        object manager = Manager();
        int[] before = ReadAll(manager);
        Watch(manager);
        object building = Building(Amounts(Amount("Money", 3)), Amounts(Amount("Money", 10)));
        Assert.That(Call(manager, "CanAffordBuilding", building), Is.False);
        Assert.That(Call(manager, "TryApplyBuildingResources", building), Is.False);
        AssertNoChange(manager, before);
    }

    [Test]
    public void AffordabilityChecksAreReadOnlyAndUseAllRequiredResources()
    {
        object manager = Manager();
        int[] before = ReadAll(manager);
        Watch(manager);
        Assert.That(Call(manager, "CanAffordResources", Amounts(Amount("Jobs", 2), Amount("Money", 2))), Is.True);
        Assert.That(Call(manager, "CanAffordResources", Amounts(Amount("Jobs", 2), Amount("Tourism", 1))), Is.False);
        Assert.That(Call(manager, "CanAffordBuilding", Building(Amounts(), Amounts())), Is.True);
        Assert.That(Call(manager, "CanAffordBuilding", new object[] { null }), Is.False);
        AssertNoChange(manager, before);
    }

    [Test]
    public void LegacyBuildingEntryPointUsesExistingSingleCostAndGainFields()
    {
        object manager = Manager();
        object building = Data("BuildingData");
        Set(building, "consumeResource", Resource("Jobs"));
        Set(building, "consumeAmount", 1);
        Set(building, "produceResource", Resource("Population"));
        Set(building, "produceAmount", 3);
        Call(manager, "ApplyBuildingResource", building);
        Assert.That(ReadAll(manager), Is.EqualTo(new[] { 5, 1, 2, 2, 0 }));
    }

    [Test]
    public void ComboRewardsSupportMultipleResourcesAndSignedPrototypeDeltas()
    {
        object manager = Manager();
        Watch(manager);
        object combo = Activator.CreateInstance(RuntimeType("ComboDefinition"));
        Set(combo, "rewards", Amounts(Amount("Money", 4), Amount("Tourism", 2), Amount("Jobs", -3)));
        Assert.That(Call(manager, "TryApplyComboResources", combo), Is.True);
        Assert.That(ReadAll(manager), Is.EqualTo(new[] { 2, -1, 6, 2, 2 }));
        Assert.That(batchCount, Is.EqualTo(1));
        Set(combo, "rewards", Amounts());
        Assert.That(Call(manager, "TryApplyComboResources", combo), Is.False);
        Assert.That(batchCount, Is.EqualTo(1));
    }

    [Test]
    public void SnapshotCopiesStateAndRestoreKeepsTheStageRetryBaseline()
    {
        object manager = Manager();
        Assert.That(Call(manager, "TryInitializeFromStage", Stage(4, 5, 6, 7, 8)), Is.True);
        Array snapshot = (Array)Call(manager, "CaptureResourceState");
        snapshot.SetValue(Amount("Population", -3), 0);
        Assert.That(ReadAll(manager), Is.EqualTo(new[] { 4, 5, 6, 7, 8 }));
        Watch(manager);
        Assert.That(Call(manager, "TryRestoreResources", snapshot), Is.True);
        Assert.That(ReadAll(manager), Is.EqualTo(new[] { -3, 5, 6, 7, 8 }));
        Assert.That(changes.Count, Is.EqualTo(5));
        Assert.That(batchCount, Is.EqualTo(1));
        snapshot.SetValue(Amount("Population", 99), 0);
        Assert.That(ReadAll(manager)[0], Is.EqualTo(-3));
        Call(manager, "ResetResources");
        Assert.That(ReadAll(manager), Is.EqualTo(new[] { 4, 5, 6, 7, 8 }));
    }

    [Test]
    public void InvalidSnapshotsRejectMissingDuplicateUnknownAndNullResources()
    {
        object manager = Manager();
        int[] before = ReadAll(manager);
        Watch(manager);
        Assert.That(Call(manager, "TryRestoreResources", Amounts(Amount("Money", 100))), Is.False);
        Array duplicate = FullState(1, 2, 3, 4, 5);
        duplicate.SetValue(Amount("Population", 10), 4);
        Assert.That(Call(manager, "TryRestoreResources", duplicate), Is.False);
        Array unknown = FullState(1, 2, 3, 4, 5);
        unknown.SetValue(Activator.CreateInstance(RuntimeType("ResourceAmount"),
            Enum.ToObject(RuntimeType("ResourceType"), 99), 10), 4);
        Assert.That(Call(manager, "TryRestoreResources", unknown), Is.False);
        Assert.That(Call(manager, "TryRestoreResources", new object[] { null }), Is.False);
        AssertNoChange(manager, before);
    }

    [Test]
    public void InvalidResourceListsAndNegativeBuildingAmountsHaveNoPartialEffects()
    {
        object manager = Manager();
        int[] before = ReadAll(manager);
        Watch(manager);
        Assert.That(Call(manager, "TryConsumeResources",
            Amounts(Amount("Money", 1), Amount("Money", 1))), Is.False);
        Assert.That(Call(manager, "TryConsumeResources", Amounts(Amount("Money", -1))), Is.False);
        Assert.That(Call(manager, "TryAddResources", new object[] { null }), Is.False);
        object unknown = Activator.CreateInstance(RuntimeType("ResourceAmount"),
            Enum.ToObject(RuntimeType("ResourceType"), 99), 1);
        Assert.That(Call(manager, "TryAddResources", Amounts(Amount("Population", 1), unknown)), Is.False);
        Assert.That(Call(manager, "TryApplyBuildingResources",
            Building(Amounts(Amount("Jobs", 1)), Amounts(Amount("Money", -1)))), Is.False);
        AssertNoChange(manager, before);
    }

    [Test]
    public void IntegerOverflowAndUnderflowRejectTheEntireTransaction()
    {
        object manager = Manager();
        Assert.That(Call(manager, "TryRestoreResources", FullState(2, 2, int.MaxValue, 2, int.MinValue)), Is.True);
        int[] before = ReadAll(manager);
        Watch(manager);
        Assert.That(Call(manager, "TryAddResources",
            Amounts(Amount("Population", 10), Amount("Money", 1))), Is.False);
        Assert.That(Call(manager, "TryAddResources",
            Amounts(Amount("Population", 10), Amount("Tourism", -1))), Is.False);
        Assert.That(Call(manager, "TryApplyBuildingResources",
            Building(Amounts(Amount("Jobs", 1)), Amounts(Amount("Money", 1)))), Is.False);
        AssertNoChange(manager, before);
    }

    [Test]
    public void EmptyAndNetZeroTransactionsSucceedWithoutChangeEvents()
    {
        object manager = Manager();
        int[] before = ReadAll(manager);
        Watch(manager);
        Assert.That(Call(manager, "TryConsumeResources", Amounts()), Is.True);
        Assert.That(Call(manager, "TryAddResources", Amounts(Amount("Money", 0))), Is.True);
        Assert.That(Call(manager, "TryApplyBuildingResources",
            Building(Amounts(Amount("Money", 1)), Amounts(Amount("Money", 1)))), Is.True);
        AssertNoChange(manager, before);
    }
}

