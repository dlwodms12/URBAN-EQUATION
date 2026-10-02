using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BoardContractTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private readonly List<ScriptableObject> assets = new List<ScriptableObject>();

    private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);

    private GameObject Object(string name, bool active = true)
    {
        var value = new GameObject(name);
        value.SetActive(active);
        objects.Add(value);
        return value;
    }

    private ScriptableObject Asset(string name)
    {
        var value = ScriptableObject.CreateInstance(RuntimeType(name));
        assets.Add(value);
        return value;
    }

    private static void Set(object target, string field, object value)
    {
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }

    private static object Get(object target, string property) => target.GetType().GetProperty(property).GetValue(target);
    private static object Call(object target, string method, params object[] args)
        => target.GetType().GetMethod(method).Invoke(target, args);

    private Component Board()
    {
        // Keep Awake deferred while the test configures and exercises initialization.
        return Object("Board under test", false).AddComponent(RuntimeType("BoardManager"));
    }

    private Component Template(string name)
    {
        GameObject root = Object(name);
        Component tile = root.AddComponent(RuntimeType("Tile"));
        GameObject highlight = new GameObject("Highlight");
        highlight.transform.SetParent(root.transform, false);
        Set(tile, "highlight", highlight);
        return tile;
    }

    private ScriptableObject TileDefinition(string type, Component template)
    {
        ScriptableObject data = Asset("TileData");
        Set(data, "tileType", Enum.Parse(RuntimeType("TileType"), type));
        Set(data, "tilePrefab", template);
        return data;
    }

    private ScriptableObject Stage(int width, int height, params ScriptableObject[] definitions)
    {
        ScriptableObject stage = Asset("StageData");
        Set(stage, "width", width); Set(stage, "height", height);
        Array tiles = Array.CreateInstance(RuntimeType("TileData"), definitions.Length);
        for (int i = 0; i < definitions.Length; i++) tiles.SetValue(definitions[i], i);
        Set(stage, "tiles", tiles);
        return stage;
    }

    private static void Generate(object board, object stage)
    {
        object[] args = { stage, null };
        Assert.That(Call(board, "TryCreateBoard", args), Is.True, args[1] as string);
    }

    private static Component Tile(object board, int x, int z)
        => (Component)Call(board, "GetTile", new Vector2Int(x, z));

    [TearDown]
    public void TearDown()
    {
        for (int i = objects.Count - 1; i >= 0; i--)
            if (objects[i] != null) UnityEngine.Object.DestroyImmediate(objects[i]);
        foreach (ScriptableObject asset in assets) UnityEngine.Object.DestroyImmediate(asset);
        objects.Clear(); assets.Clear();
    }

    [Test]
    public void MixedRectangularBoardUsesDocumentNorthAndEachTilePrefab()
    {
        Component board = Board();
        board.transform.position = new Vector3(10f, 2f, -4f);
        board.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        var grass = TileDefinition("Grass", Template("Grass template"));
        var concrete = TileDefinition("Concrete", Template("Concrete template"));
        var asphalt = TileDefinition("Asphalt", Template("Asphalt template"));
        var stage = Stage(2, 3, grass, concrete, concrete, asphalt, asphalt, grass);
        Generate(board, stage);

        Assert.That(Get(board, "Width"), Is.EqualTo(2));
        Assert.That(Get(board, "Height"), Is.EqualTo(3));
        Assert.That(Get(board, "CurrentStage"), Is.SameAs(stage));
        Component northWest = Tile(board, 0, 2);
        Assert.That(Get(northWest, "Data"), Is.SameAs(grass));
        Assert.That(northWest.name, Does.StartWith("Grass template"));
        Assert.That(northWest.transform.position, Is.EqualTo(new Vector3(10f, 2f, -2f)));
        Assert.That(Get(Tile(board, 1, 1), "Data"), Is.SameAs(asphalt));
        Assert.That(Get(Tile(board, 1, 0), "Data"), Is.SameAs(grass));
        Assert.That(Call(board, "GetTile", new Vector2Int(2, 0)), Is.Null);
        Assert.That(Call(board, "GetTile", new Vector2Int(0, 3)), Is.Null);
        Assert.That(Call(board, "GetTile", new Vector2Int(-1, 0)), Is.Null);
    }

    [Test]
    public void InvalidDimensionsLeaveExistingBoardIntact()
    {
        Component board = Board();
        var tile = TileDefinition("Grass", Template("Template"));
        var valid = Stage(1, 1, tile);
        Generate(board, valid);
        Component existing = Tile(board, 0, 0);
        object[] args = { Stage(2, 2, tile), null };
        Assert.That(Call(board, "TryCreateBoard", args), Is.False);
        Assert.That(args[1], Is.Not.Null);
        Assert.That(Tile(board, 0, 0), Is.SameAs(existing));
        Assert.That(Get(board, "CurrentStage"), Is.SameAs(valid));
        Assert.That(board.transform.childCount, Is.EqualTo(1));
    }

    [Test]
    public void MissingPrefabAndNullStageDoNotReplaceExistingBoard()
    {
        Component board = Board();
        var validTile = TileDefinition("Grass", Template("Template"));
        Generate(board, Stage(1, 1, validTile));
        Component existing = Tile(board, 0, 0);
        var missing = TileDefinition("Concrete", null);
        Assert.That(Call(board, "TryCreateBoard", Stage(1, 1, missing), null), Is.False);
        Assert.That(Call(board, "TryCreateBoard", null, null), Is.False);
        Assert.That(Tile(board, 0, 0), Is.SameAs(existing));
        Assert.That(board.transform.childCount, Is.EqualTo(1));
    }

    [Test]
    public void RebuildingRemovesOldTilesAndExternalBuildingsButKeepsOtherChildren()
    {
        Component board = Board();
        GameObject unrelated = Object("Unrelated child");
        unrelated.transform.SetParent(board.transform, false);
        var tileData = TileDefinition("Grass", Template("Template"));
        Generate(board, Stage(1, 1, tileData));
        Component oldTile = Tile(board, 0, 0);
        Component oldBuilding = Object("External building").AddComponent(RuntimeType("BuildingInstance"));
        Assert.That(Call(oldTile, "TrySetBuilding", oldBuilding), Is.True);
        Generate(board, Stage(2, 1, tileData, tileData));

        Assert.That(oldTile == null, Is.True);
        Assert.That(oldBuilding == null, Is.True);
        Assert.That(unrelated != null, Is.True);
        Assert.That(board.transform.childCount, Is.EqualTo(2));
        Assert.That(Get(board, "Width"), Is.EqualTo(2));
    }

    [Test]
    public void ResetClearsOccupancyAndHighlightsWithoutRemovingTiles()
    {
        Component board = Board();
        var data = TileDefinition("Grass", Template("Template"));
        Generate(board, Stage(1, 1, data));
        Component tile = Tile(board, 0, 0);
        Component building = Object("Building").AddComponent(RuntimeType("BuildingInstance"));
        Call(tile, "TrySetBuilding", building);
        Call(tile, "SetHighlight", true);
        Call(board, "ResetBoard");

        Assert.That(Get(tile, "IsOccupied"), Is.False);
        Assert.That(building == null, Is.True);
        Assert.That(Tile(board, 0, 0), Is.SameAs(tile));
        Assert.That(Get(tile, "Data"), Is.SameAs(data));
        var highlight = (GameObject)tile.GetType().GetField("highlight", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tile);
        Assert.That(highlight.activeSelf, Is.False);
        Assert.That(Get(board, "Width"), Is.EqualTo(1));
    }

    [Test]
    public void OccupiedTileRejectsReplacementAndAllowsExplicitDetach()
    {
        Component tile = Template("Template");
        Component first = Object("First").AddComponent(RuntimeType("BuildingInstance"));
        Component second = Object("Second").AddComponent(RuntimeType("BuildingInstance"));
        Assert.That(Call(tile, "TrySetBuilding", first), Is.True);
        Assert.That(Call(tile, "TrySetBuilding", second), Is.False);
        Assert.That(Get(tile, "Building"), Is.SameAs(first));
        Assert.That(Call(tile, "TrySetBuilding", first), Is.True);
        Assert.That(Call(tile, "TrySetBuilding", new object[] { null }), Is.True);
        Assert.That(first != null, Is.True);
        Assert.That(Call(tile, "TrySetBuilding", second), Is.True);
    }

    [Test]
    public void PlacementChecksTileTypeOccupancyAndCoordinates()
    {
        Component board = Board();
        var grass = TileDefinition("Grass", Template("Grass"));
        var asphalt = TileDefinition("Asphalt", Template("Asphalt"));
        Generate(board, Stage(2, 1, grass, asphalt));
        ScriptableObject building = Asset("BuildingData");
        Array allowed = Array.CreateInstance(RuntimeType("TileType"), 1);
        allowed.SetValue(Enum.Parse(RuntimeType("TileType"), "Grass"), 0);
        Set(building, "allowedTileTypes", allowed);

        Assert.That(Call(board, "CanPlaceBuilding", building, new Vector2Int(0, 0)), Is.True);
        Assert.That(Call(board, "CanPlaceBuilding", building, new Vector2Int(1, 0)), Is.False);
        Assert.That(Call(board, "CanPlaceBuilding", building, new Vector2Int(2, 0)), Is.False);
        Assert.That(Call(board, "CanPlaceBuilding", null, new Vector2Int(0, 0)), Is.False);
        Call(Tile(board, 0, 0), "TrySetBuilding", Object("Building").AddComponent(RuntimeType("BuildingInstance")));
        Assert.That(Call(board, "CanPlaceBuilding", building, new Vector2Int(0, 0)), Is.False);
    }

    private static List<Vector2Int> Neighbors(object board, Vector2Int coordinate)
    {
        var result = new List<Vector2Int>();
        foreach (object tile in (IEnumerable)Call(board, "GetAdjacentTiles", coordinate))
            result.Add((Vector2Int)Get(tile, "Coordinate"));
        return result;
    }

    [Test]
    public void NeighborsFollowLeftDownRightUpAndSkipOutsideTheBoard()
    {
        Component board = Board();
        var data = TileDefinition("Grass", Template("Template"));
        Generate(board, Stage(3, 3, data, data, data, data, data, data, data, data, data));
        Assert.That(Neighbors(board, new Vector2Int(1, 1)), Is.EqualTo(new[]
        {
            new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(2, 1), new Vector2Int(1, 2)
        }));
        Assert.That(Neighbors(board, Vector2Int.zero), Is.EqualTo(new[] { new Vector2Int(1, 0), new Vector2Int(0, 1) }));
        Assert.That(Neighbors(board, new Vector2Int(-1, 0)), Is.Empty);
    }

    [Test]
    public void PrototypeInitializationKeepsEightByEightAndSafeEmptyQueries()
    {
        Component board = Board();
        Assert.That(Call(board, "GetTile", Vector2Int.zero), Is.Null);
        Call(board, "ResetBoard");
        Set(board, "tilePrefab", Template("Legacy template"));
        board.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(board, null);
        Assert.That(Get(board, "Width"), Is.EqualTo(8));
        Assert.That(Get(board, "Height"), Is.EqualTo(8));
        Assert.That(Get(Tile(board, 7, 7), "Data"), Is.Null);
        Assert.That(Tile(board, 7, 7).transform.position, Is.EqualTo(new Vector3(7f, 0f, 7f)));
        Call(board, "ClearBoard");
        Assert.That(Get(board, "HasBoard"), Is.False);
        Assert.That(Get(board, "Width"), Is.EqualTo(0));
        Assert.That(Call(board, "GetTile", Vector2Int.zero), Is.Null);
    }
}
