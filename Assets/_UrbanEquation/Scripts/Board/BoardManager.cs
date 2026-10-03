using System;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    // Compatibility constant for the prototype. Production queries Width/Height.
    public const int BoardSize = 8;

    [Header("Stage Board")]
    [SerializeField] private StageData initialStage;

    [Header("Prototype Compatibility")]
    [SerializeField] private Tile tilePrefab;

    private Tile[,] tiles;
    private Transform generatedRoot;

    public int Width => tiles == null ? 0 : tiles.GetLength(0);
    public int Height => tiles == null ? 0 : tiles.GetLength(1);
    public StageData CurrentStage { get; private set; }
    public bool HasBoard => tiles != null;
    public long ResetVersion { get; private set; }
    public event Action OnBoardReset;

    private void Awake()
    {
        if (initialStage != null)
        {
            if (!TryCreateBoard(initialStage, out string error)) Debug.LogError(error, this);
        }
        else if (tilePrefab != null)
        {
            // Existing scenes keep their serialized tilePrefab reference.
            CreatePrototypeBoard();
        }
        // A session manager may initialize an unconfigured board later.
    }

    public bool TryCreateBoard(StageData stage, out string error)
    {
        error = null;
        if (stage == null) { error = "Board requires StageData."; return false; }
        if (stage.Width < 1 || stage.Height < 1
            || (long)stage.Width * stage.Height != stage.Tiles.Count)
        {
            error = "Stage tile array must match Width x Height.";
            return false;
        }

        // Validate the entire board before replacing the currently playable board.
        // Resource/card/goal validation belongs to session initialization, not this layer.
        foreach (TileData data in stage.Tiles)
        {
            if (data == null || data.TilePrefab == null)
            {
                error = "Every stage tile requires TileData and a Tile prefab.";
                return false;
            }
            if (!Enum.IsDefined(typeof(TileType), data.TileType))
            {
                error = "Stage contains an unknown tile type.";
                return false;
            }
        }

        Transform candidateRoot = CreateRoot(true);
        Tile[,] candidateTiles;
        try
        {
            candidateTiles = new Tile[stage.Width, stage.Height];
            for (int z = 0; z < stage.Height; z++)
            {
                for (int x = 0; x < stage.Width; x++)
                {
                    var coordinate = new Vector2Int(x, z);
                    if (!stage.TryGetTile(coordinate, out TileData data))
                        throw new InvalidOperationException("Stage tile lookup failed.");
                    candidateTiles[x, z] = SpawnTile(data.TilePrefab, coordinate, data, candidateRoot);
                }
            }
        }
        catch (Exception exception)
        {
            DestroyObject(candidateRoot.gameObject);
            error = "Could not create stage board: " + exception.Message;
            return false;
        }

        ReplaceBoard(candidateTiles, candidateRoot, stage);
        return true;
    }

    private void CreatePrototypeBoard()
    {
        Transform root = CreateRoot(false);
        var prototypeTiles = new Tile[BoardSize, BoardSize];
        for (int z = 0; z < BoardSize; z++)
            for (int x = 0; x < BoardSize; x++)
                prototypeTiles[x, z] = SpawnTile(tilePrefab, new Vector2Int(x, z), null, root);
        ReplaceBoard(prototypeTiles, root, null);
    }

    private Transform CreateRoot(bool alignNorth)
    {
        var root = new GameObject("GeneratedBoard");
        root.SetActive(false);
        root.transform.SetParent(transform, false);
        // Production north stays world +Z; legacy visuals retain the parent rotation.
        if (alignNorth) root.transform.rotation = Quaternion.identity;
        return root.transform;
    }

    private Tile SpawnTile(Tile prefab, Vector2Int coordinate, TileData data, Transform parent)
    {
        Tile tile = Instantiate(prefab, parent);
        Vector3 origin = data == null ? Vector3.zero : transform.position;
        tile.transform.position = origin + new Vector3(coordinate.x, 0f, coordinate.y);
        tile.Initialize(coordinate, data);
        return tile;
    }

    private void ReplaceBoard(Tile[,] nextTiles, Transform nextRoot, StageData stage)
    {
        ClearBoard();
        tiles = nextTiles;
        generatedRoot = nextRoot;
        CurrentStage = stage;
        generatedRoot.gameObject.SetActive(true);
    }

    public Tile GetTile(Vector2Int coordinate)
    {
        if (tiles == null || coordinate.x < 0 || coordinate.x >= Width
            || coordinate.y < 0 || coordinate.y >= Height) return null;
        return tiles[coordinate.x, coordinate.y];
    }

    public bool CanPlaceBuilding(BuildingData building, Vector2Int coordinate)
    {
        Tile tile = GetTile(coordinate);
        return tile != null && tile.CanAcceptBuilding(building);
    }

    public IEnumerable<Tile> GetAdjacentTiles(Vector2Int coordinate)
    {
        if (GetTile(coordinate) == null) yield break;
        // Left, down, right, up: the design's combo inspection order.
        Vector2Int[] offsets = { Vector2Int.left, Vector2Int.down, Vector2Int.right, Vector2Int.up };
        foreach (Vector2Int offset in offsets)
        {
            Tile tile = GetTile(coordinate + offset);
            if (tile != null) yield return tile;
        }
    }

    public bool TryCaptureBuildings(out BuildingStateSnapshot[] placements, out string error)
    {
        placements = new BuildingStateSnapshot[0];
        error = null;
        if (tiles == null) { error = "Cannot capture an uninitialized board."; return false; }
        var captured = new List<BuildingStateSnapshot>();
        foreach (Tile tile in tiles)
        {
            if (tile == null) { error = "Board contains a missing tile."; return false; }
            if (!tile.IsOccupied) continue;
            BuildingInstance building = tile.Building;
            if (building.Data == null || building.Coordinate != tile.Coordinate)
            { error = "Board contains an invalid building."; return false; }
            captured.Add(new BuildingStateSnapshot(tile.Coordinate, building.Data));
        }
        placements = captured.ToArray();
        return true;
    }

    internal sealed class PreparedBuildingRestore : IDisposable
    {
        private readonly GameObject stagingRoot;
        private readonly Action apply;
        private bool applied;
        internal PreparedBuildingRestore(GameObject root, Action commit) { stagingRoot = root; apply = commit; }
        internal void Apply()
        {
            if (applied) throw new InvalidOperationException("Board restore was already applied.");
            apply();
            applied = true;
        }
        public void Dispose()
        {
            if (stagingRoot != null) DestroyObject(stagingRoot);
        }
    }

    internal bool TryPrepareBuildingRestore(IReadOnlyList<BuildingStateSnapshot> placements,
        BuildingInstance prefab, out PreparedBuildingRestore operation, out string error)
    {
        operation = null;
        error = null;
        if (tiles == null || placements == null || prefab == null)
        { error = "Building restore requires a board, placements and root prefab."; return false; }
        foreach (Tile tile in tiles)
            if (tile == null) { error = "Board contains a missing tile."; return false; }
        var seen = new HashSet<Vector2Int>();
        var targetTiles = new List<Tile>();
        foreach (BuildingStateSnapshot state in placements)
        {
            if (state == null || state.Building == null || state.Building.VisualPrefab == null
                || state.BuildingCode != state.Building.BuildingCode || !seen.Add(state.Coordinate))
            { error = "Snapshot contains a missing, changed or duplicate building definition."; return false; }
            Tile tile = GetTile(state.Coordinate);
            if (tile == null || (tile.Data != null && !state.Building.CanBuildOn(tile.Data.TileType))
                || (tile.Data == null && state.Building.UsesResourceLists))
            { error = "Snapshot building does not match the board's tiles."; return false; }
            var errors = new List<string>();
            state.Building.Validate(errors);
            if (errors.Count > 0) { error = string.Join("; ", errors); return false; }
            targetTiles.Add(tile);
        }

        var root = new GameObject("UndoBuildingCandidates");
        root.SetActive(false);
        root.transform.SetParent(transform, true);
        var buildings = new List<BuildingInstance>();
        try
        {
            for (int i = 0; i < placements.Count; i++)
            {
                BuildingInstance building = Instantiate(prefab, root.transform);
                building.transform.position = targetTiles[i].transform.position;
                building.transform.rotation = Quaternion.identity;
                building.Initialize(placements[i].Building, placements[i].Coordinate);
                buildings.Add(building);
            }
        }
        catch (Exception exception)
        {
            DestroyObject(root);
            error = "Could not prepare restored buildings: " + exception.Message;
            return false;
        }
        operation = new PreparedBuildingRestore(root, () =>
        {
            // This is a restore, not a retry: keep the combo/history ownership scope.
            foreach (Tile tile in tiles) { tile.ClearBuilding(); tile.SetHighlight(false); }
            for (int i = 0; i < buildings.Count; i++)
            {
                BuildingInstance building = buildings[i];
                Tile tile = targetTiles[i];
                building.transform.SetParent(tile.transform, true);
                tile.TrySetBuilding(building);
                building.gameObject.SetActive(true);
            }
        });
        return true;
    }

    public void ResetBoard()
    {
        if (tiles == null) return;
        foreach (Tile tile in tiles)
        {
            if (tile == null) continue;
            tile.ClearBuilding();
            tile.SetHighlight(false);
        }
        ResetVersion++;
        OnBoardReset?.Invoke();
    }

    public void ClearBoard()
    {
        ResetBoard();
        if (generatedRoot != null)
        {
            generatedRoot.gameObject.SetActive(false);
            DestroyObject(generatedRoot.gameObject);
        }
        generatedRoot = null;
        tiles = null;
        CurrentStage = null;
    }

    private static void DestroyObject(GameObject value)
    {
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }
}
