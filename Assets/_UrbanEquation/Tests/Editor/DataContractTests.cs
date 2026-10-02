using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Reflection keeps the test assembly independent of Assembly-CSharp; adding
// runtime asmdefs now would change the existing prototype's assembly boundaries.
public class DataContractTests
{
    private readonly List<ScriptableObject> created = new List<ScriptableObject>();

    private static Type DataType(string name)
    {
        Type type = Type.GetType(name + ", Assembly-CSharp", true);
        return type;
    }

    private ScriptableObject Create(string name)
    {
        ScriptableObject value = ScriptableObject.CreateInstance(DataType(name));
        created.Add(value);
        return value;
    }

    private static void Set(object target, string field, object value)
    {
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(target, value);
    }

    private static object Get(object target, string property)
    {
        return target.GetType().GetProperty(property).GetValue(target);
    }

    private static object Call(object target, string method, params object[] args)
    {
        return target.GetType().GetMethod(method).Invoke(target, args);
    }

    private static object Resource(string name) => Enum.Parse(DataType("ResourceType"), name);

    private static object Amount(string name, int amount)
    {
        return Activator.CreateInstance(DataType("ResourceAmount"), Resource(name), amount);
    }

    private static Array Amounts(params object[] amounts)
    {
        Array result = Array.CreateInstance(DataType("ResourceAmount"), amounts.Length);
        for (int i = 0; i < amounts.Length; i++) result.SetValue(amounts[i], i);
        return result;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (ScriptableObject value in created) UnityEngine.Object.DestroyImmediate(value);
        created.Clear();
    }

    [Test]
    public void ResourceNumbersPreservePrototypeSerialization()
    {
        Assert.That(Enum.GetValues(DataType("ResourceType")).Length, Is.EqualTo(5));
        Assert.That(Convert.ToInt32(Resource("Population")), Is.EqualTo(0));
        Assert.That(Convert.ToInt32(Resource("Jobs")), Is.EqualTo(1));
        Assert.That(Convert.ToInt32(Resource("Money")), Is.EqualTo(2));
        Assert.That(Convert.ToInt32(Resource("Logistics")), Is.EqualTo(3));
        Assert.That(Convert.ToInt32(Resource("Tourism")), Is.EqualTo(4));
    }

    [Test]
    public void PrototypeBuildingKeepsLegacyCostsAndGains()
    {
        object building = Create("BuildingData");
        Set(building, "consumeResource", Resource("Jobs")); Set(building, "consumeAmount", 1);
        Set(building, "produceResource", Resource("Population")); Set(building, "produceAmount", 1);
        Assert.That(Get(building, "UsesResourceLists"), Is.False);
        IList costs = (IList)Get(building, "RequiredResources");
        IList gains = (IList)Get(building, "GainedResources");
        Assert.That(costs.Count, Is.EqualTo(1)); Assert.That(gains.Count, Is.EqualTo(1));
        Assert.That(Get(costs[0], "Resource"), Is.EqualTo(Resource("Jobs")));
        Assert.That(Get(costs[0], "Amount"), Is.EqualTo(1));
        Assert.That(Get(gains[0], "Resource"), Is.EqualTo(Resource("Population")));
    }

    [Test]
    public void ProductionBuildingSupportsMultipleCostsWithoutLegacyFallback()
    {
        object building = Create("BuildingData");
        Set(building, "useResourceLists", true);
        Set(building, "consumeAmount", 99);
        Set(building, "requiredResources", Amounts(Amount("Jobs", 3), Amount("Tourism", 1)));
        Set(building, "gainedResources", Amounts(Amount("Population", 3), Amount("Money", 1)));
        IList costs = (IList)Get(building, "RequiredResources");
        Assert.That(costs.Count, Is.EqualTo(2));
        Assert.That(Get(costs[1], "Resource"), Is.EqualTo(Resource("Tourism")));
        Set(building, "requiredResources", Amounts());
        Assert.That(((IList)Get(building, "RequiredResources")).Count, Is.EqualTo(0));
    }

    [Test]
    public void BoardDocumentNorthRowMapsToPositiveZ()
    {
        object stage = Create("StageData");
        Set(stage, "width", 3); Set(stage, "height", 2);
        Array tiles = Array.CreateInstance(DataType("TileData"), 6);
        for (int i = 0; i < 6; i++) tiles.SetValue(Create("TileData"), i);
        Set(stage, "tiles", tiles);
        object[] northWest = { new Vector2Int(0, 1), null };
        object[] southEast = { new Vector2Int(2, 0), null };
        Assert.That(Call(stage, "TryGetTile", northWest), Is.True);
        Assert.That(northWest[1], Is.SameAs(tiles.GetValue(0)));
        Assert.That(Call(stage, "TryGetTile", southEast), Is.True);
        Assert.That(southEast[1], Is.SameAs(tiles.GetValue(5)));
        Assert.That(Call(stage, "TryGetTile", new Vector2Int(-1, 0), null), Is.False);
        Set(stage, "width", 4);
        Assert.That(Call(stage, "TryGetTile", new Vector2Int(0, 0), null), Is.False);
    }

    private static object Combo(int code, int a, int b)
    {
        object combo = Activator.CreateInstance(DataType("ComboDefinition"));
        Set(combo, "comboCode", code); Set(combo, "buildingCodeA", a); Set(combo, "buildingCodeB", b);
        return combo;
    }

    [Test]
    public void ComboPairIgnoresConstructionOrderAndSupportsIdenticalBuildings()
    {
        object combo = Combo(11001, 11001, 12001);
        Assert.That(Call(combo, "Matches", 12001, 11001), Is.True);
        Assert.That(Call(combo, "Matches", 11001, 13001), Is.False);
        Assert.That(Call(Combo(11001, 11001, 11001), "Matches", 11001, 11001), Is.True);
    }

    [Test]
    public void SharedComboCodesAreAllowedButDuplicatePairsAreRejected()
    {
        object database = Create("ComboDatabase");
        IList rows = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(DataType("ComboDefinition")));
        rows.Add(Combo(11001, 11001, 11001)); rows.Add(Combo(11001, 11001, 12001));
        Set(database, "combos", rows);
        object[] lookup = { 12001, 11001, null };
        Assert.That(Call(database, "TryGetCombo", lookup), Is.True);
        Assert.That(lookup[2], Is.SameAs(rows[1]));
        rows.Add(Combo(11001, 12001, 11001));
        Assert.That(Call(database, "TryGetCombo", lookup), Is.False);
        Assert.That(lookup[2], Is.Null);
    }

    [Test]
    public void EmptyTilePermissionsDoNotAllowEveryTile()
    {
        object building = Create("BuildingData");
        object grass = Enum.Parse(DataType("TileType"), "Grass");
        object concrete = Enum.Parse(DataType("TileType"), "Concrete");
        Assert.That(Call(building, "CanBuildOn", grass), Is.False);
        Array types = Array.CreateInstance(DataType("TileType"), 1); types.SetValue(grass, 0);
        Set(building, "allowedTileTypes", types);
        Assert.That(Call(building, "CanBuildOn", grass), Is.True);
        Assert.That(Call(building, "CanBuildOn", concrete), Is.False);
    }

    [Test]
    public void DuplicateAndNegativeInitialResourcesAreReported()
    {
        var errors = new List<string>();
        DataType("DataValidation").GetMethod("ValidateResources").Invoke(null, new object[]
        {
            Amounts(Amount("Jobs", 1), Amount("Jobs", -1)), true, errors, "Test"
        });
        Assert.That(errors.Count, Is.EqualTo(2));
    }
}
