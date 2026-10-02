using System;
using System.Collections.Generic;
using UnityEngine;

public class ComboManager : MonoBehaviour
{
    // Preserve the prototype scene's serialized rows and field names.
    [Serializable]
    private class ComboData
    {
        [SerializeField] private int comboCode;
        [SerializeField] private int buildingCodeA;
        [SerializeField] private int buildingCodeB;
        [SerializeField] private ResourceType rewardResource;
        [SerializeField] private int rewardAmount;
        public int ComboCode => comboCode;
        public int BuildingCodeA => buildingCodeA;
        public int BuildingCodeB => buildingCodeB;
        public ResourceType RewardResource => rewardResource;
        public int RewardAmount => rewardAmount;
    }

    [SerializeField] private BoardManager boardManager;
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private List<ComboData> combos = new List<ComboData>();
    [SerializeField] private ComboDatabase comboDatabase;

    private readonly List<ComboResult> results = new List<ComboResult>();
    private readonly Dictionary<string, ComboResult> appliedPairs = new Dictionary<string, ComboResult>();
    private readonly ComboPresentationQueue presentations = new ComboPresentationQueue();
    private object ownerToken = new object();
    private BoardManager stateBoard;
    private BoardManager subscribedBoard;
    private long observedResetVersion;
    private bool presentationEventsBound;
    private bool resolving;

    public BoardManager Board => boardManager;
    public ResourceManager Resources => resourceManager;
    public bool IsResolving => resolving;
    public long ConfigurationVersion { get; private set; }
    public IReadOnlyList<ComboResult> Results { get { EnsureBindings(); return results.AsReadOnly(); } }
    public ComboResult CurrentPresentation { get { EnsureBindings(); return presentations.Current; } }
    public int PendingPresentationCount { get { EnsureBindings(); return presentations.PendingCount; } }

    public event Action<int, int, int, ResourceType, int> OnComboTriggered;
    public event Action<ComboResult> OnComboResolved;
    public event Action OnResultsChanged;
    public event Action<ComboResult> OnPresentationRequested;
    public event Action OnPresentationCleared;

    private void OnEnable() => EnsureBindings();
    private void OnDisable() => UnsubscribeBoard();

    public bool TryConfigure(BoardManager board, ResourceManager resources, ComboDatabase database, out string error)
    {
        error = null;
        if (resolving) { error = "Combo resolution is already in progress."; return false; }
        if (board == null || resources == null) { error = "Combos require board and resources."; return false; }
        if (!ValidateSource(database, out error)) return false;
        UnsubscribeBoard();
        boardManager = board;
        resourceManager = resources;
        comboDatabase = database;
        stateBoard = board;
        observedResetVersion = board.ResetVersion;
        ownerToken = new object();
        ConfigurationVersion++;
        ClearComboState();
        EnsureBindings();
        return true;
    }

    // Compatibility entry point used by the existing BuildingPlacement.
    public void CheckCombos(BuildingInstance building)
    {
        if (building == null) return;
        if (!TryResolveCombos(building, out _, out string error)) Debug.LogWarning(error, this);
    }

    public bool TryResolveCombos(BuildingInstance building, out ComboResult[] resolved, out string error)
    {
        resolved = new ComboResult[0];
        error = null;
        if (resolving) { error = "Combo resolution is already in progress."; return false; }
        resolving = true;
        try
        {
            if (!TryPrepareCombos(building, out ComboResult[] prepared, out error)) return false;
            if (!resourceManager.TryApplyRewardsDeferred(RewardLists(prepared), out Action publishResources))
            { error = "Combo resource transaction was rejected."; return false; }
            Action publishResults = RecordPreparedResults(prepared);
            resolved = prepared;
            publishResources();
            publishResults();
            return true;
        }
        finally { resolving = false; }
    }

    internal bool TryPrepareCombos(BuildingInstance building, out ComboResult[] prepared, out string error)
    {
        EnsureBindings();
        prepared = new ComboResult[0];
        error = null;
        if (boardManager == null || resourceManager == null || building == null || building.Data == null
            || boardManager.GetTile(building.Coordinate) == null
            || boardManager.GetTile(building.Coordinate).Building != building)
        { error = "The source building must occupy a tile on the configured board."; return false; }
        if (!ValidateSource(comboDatabase, out error)) return false;

        var next = new List<ComboResult>();
        // BoardManager provides left -> down -> right -> up; diagonals are excluded.
        foreach (Tile tile in boardManager.GetAdjacentTiles(building.Coordinate))
        {
            if (!tile.IsOccupied) continue;
            BuildingInstance adjacent = tile.Building;
            if (adjacent.Data == null || adjacent.Coordinate != tile.Coordinate)
            { error = "Adjacent building data/coordinate is invalid."; return false; }
            string key = PairKey(building.Coordinate, adjacent.Coordinate);
            if (appliedPairs.ContainsKey(key)) continue;

            int code = 0;
            string name = null, description = null;
            IReadOnlyList<ResourceAmount> rewards = null;
            if (comboDatabase != null)
            {
                if (!comboDatabase.TryGetCombo(building.Data.BuildingCode,
                    adjacent.Data.BuildingCode, out ComboDefinition definition)) continue;
                code = definition.ComboCode;
                name = definition.ComboName;
                description = definition.Description;
                rewards = definition.Rewards;
            }
            else
            {
                foreach (ComboData definition in combos)
                {
                    if (!Matches(building.Data.BuildingCode, adjacent.Data.BuildingCode,
                        definition.BuildingCodeA, definition.BuildingCodeB)) continue;
                    code = definition.ComboCode;
                    rewards = new[] { new ResourceAmount(definition.RewardResource, definition.RewardAmount) };
                    break;
                }
                if (rewards == null) continue;
            }
            long id = (long)results.Count + next.Count + 1;
            if (id > int.MaxValue) { error = "Combo result count exceeds the integer range."; return false; }
            next.Add(new ComboResult(ownerToken, (int)id, code, name, description,
                building.Data.BuildingCode, adjacent.Data.BuildingCode,
                building.Coordinate, adjacent.Coordinate,
                (building.transform.position + adjacent.transform.position) * 0.5f, rewards));
        }
        prepared = next.ToArray();
        return true;
    }

    internal static IReadOnlyList<IReadOnlyList<ResourceAmount>> RewardLists(IReadOnlyList<ComboResult> batch)
    {
        var list = new List<IReadOnlyList<ResourceAmount>>();
        foreach (ComboResult result in batch) list.Add(result.Rewards);
        return list;
    }

    internal Action RecordPreparedResults(IReadOnlyList<ComboResult> batch)
    {
        var copy = new ComboResult[batch.Count];
        for (int i = 0; i < batch.Count; i++)
        {
            ComboResult result = batch[i];
            results.Add(result);
            appliedPairs.Add(PairKey(result.SourceCoordinate, result.AdjacentCoordinate), result);
            copy[i] = result;
        }
        return () =>
        {
            bool wasResolving = resolving;
            resolving = true;
            try
            {
                foreach (ComboResult result in copy)
                {
                    OnComboResolved?.Invoke(result);
                    foreach (ResourceAmount reward in result.Rewards)
                        OnComboTriggered?.Invoke(result.ComboCode, result.BuildingCodeA,
                            result.BuildingCodeB, reward.Resource, reward.Amount);
                }
                if (copy.Length > 0) OnResultsChanged?.Invoke();
                presentations.EnqueueResults(copy);
            }
            finally { resolving = wasResolving; }
        };
    }

    public bool TryGetAppliedCombo(Vector2Int a, Vector2Int b, out ComboResult result)
    {
        EnsureBindings();
        result = null;
        if (!appliedPairs.TryGetValue(PairKey(a, b), out ComboResult candidate)
            || !IsResultOnBoard(candidate)) return false;
        result = candidate;
        return true;
    }

    public ComboResult[] CaptureResults() { EnsureBindings(); return results.ToArray(); }

    public bool TryRestoreResults(IReadOnlyList<ComboResult> snapshot, out string error)
    {
        EnsureBindings();
        if (!TryPrepareResultsRestore(snapshot, IsResultOnBoard,
            out Action apply, out Action publish, out error)) return false;
        apply();
        publish();
        return true;
    }

    internal bool TryPrepareResultsRestore(IReadOnlyList<ComboResult> snapshot,
        IReadOnlyList<BuildingStateSnapshot> restoredBuildings,
        out Action apply, out Action publish, out string error)
    {
        EnsureBindings();
        var buildings = new Dictionary<Vector2Int, BuildingStateSnapshot>();
        foreach (BuildingStateSnapshot building in restoredBuildings)
            buildings.Add(building.Coordinate, building);
        return TryPrepareResultsRestore(snapshot, result =>
        {
            Vector2Int direction = result.AdjacentCoordinate - result.SourceCoordinate;
            return Math.Abs(direction.x) + Math.Abs(direction.y) == 1
                && buildings.TryGetValue(result.SourceCoordinate, out BuildingStateSnapshot a)
                && buildings.TryGetValue(result.AdjacentCoordinate, out BuildingStateSnapshot b)
                && a.BuildingCode == result.BuildingCodeA && b.BuildingCode == result.BuildingCodeB;
        }, out apply, out publish, out error);
    }

    private bool TryPrepareResultsRestore(IReadOnlyList<ComboResult> snapshot,
        Func<ComboResult, bool> isOnBoard, out Action apply, out Action publish, out string error)
    {
        apply = null;
        publish = null;
        error = null;
        if (resolving || snapshot == null) { error = "Cannot restore this combo snapshot."; return false; }
        var next = new List<ComboResult>();
        var pairs = new Dictionary<string, ComboResult>();
        foreach (ComboResult result in snapshot)
        {
            if (result == null || result.OwnerToken != ownerToken || result.ResultId != next.Count + 1
                || !isOnBoard(result))
            { error = "Snapshot contains a foreign/invalid/out-of-order combo result."; return false; }
            string key = PairKey(result.SourceCoordinate, result.AdjacentCoordinate);
            if (pairs.ContainsKey(key)) { error = "Snapshot contains a duplicate building pair."; return false; }
            next.Add(result);
            pairs.Add(key, result);
        }
        apply = () =>
        {
            results.Clear(); results.AddRange(next);
            appliedPairs.Clear();
            foreach (var pair in pairs) appliedPairs.Add(pair.Key, pair.Value);
            // Clear now, notify after the whole session has been restored.
            presentations.ClearSilently();
        };
        publish = () => { OnPresentationCleared?.Invoke(); OnResultsChanged?.Invoke(); };
        return true;
    }

    public bool TryCompletePresentation(ComboResult expected)
    {
        EnsureBindings();
        return presentations.TryComplete(expected);
    }

    public void ClearPresentations() { EnsureBindings(); presentations.Clear(); }

    public void ClearComboState()
    {
        results.Clear();
        appliedPairs.Clear();
        presentations.Clear();
        OnResultsChanged?.Invoke();
    }

    private bool IsResultOnBoard(ComboResult result)
    {
        if (boardManager == null) return false;
        Tile a = boardManager.GetTile(result.SourceCoordinate);
        Tile b = boardManager.GetTile(result.AdjacentCoordinate);
        return a != null && b != null && a.Building != null && b.Building != null
            && a.Building.Data != null && b.Building.Data != null
            && a.Building.Data.BuildingCode == result.BuildingCodeA
            && b.Building.Data.BuildingCode == result.BuildingCodeB;
    }

    private bool ValidateSource(ComboDatabase database, out string error)
    {
        var errors = new List<string>();
        if (database != null) database.Validate(errors);
        else
        {
            var pairs = new HashSet<string>();
            foreach (ComboData combo in combos)
            {
                if (combo == null) { errors.Add("Prototype combo row is missing."); continue; }
                if (combo.ComboCode <= 0 || combo.BuildingCodeA <= 0 || combo.BuildingCodeB <= 0
                    || !Enum.IsDefined(typeof(ResourceType), combo.RewardResource))
                    errors.Add("Prototype combo row contains an invalid code/resource.");
                string key = Math.Min(combo.BuildingCodeA, combo.BuildingCodeB)
                    + ":" + Math.Max(combo.BuildingCodeA, combo.BuildingCodeB);
                if (!pairs.Add(key)) errors.Add("Duplicate prototype combo pair.");
            }
        }
        error = errors.Count == 0 ? null : string.Join("; ", errors);
        return errors.Count == 0;
    }

    private void EnsureBindings()
    {
        if (!presentationEventsBound)
        {
            presentations.OnCurrentChanged += HandlePresentationChanged;
            presentationEventsBound = true;
        }
        if (stateBoard != boardManager)
        {
            stateBoard = boardManager;
            observedResetVersion = boardManager == null ? 0 : boardManager.ResetVersion;
            ownerToken = new object();
            ConfigurationVersion++;
            ClearComboState();
        }
        if (boardManager != null && observedResetVersion != boardManager.ResetVersion)
        {
            observedResetVersion = boardManager.ResetVersion;
            ClearComboState();
        }
        if (isActiveAndEnabled && subscribedBoard != boardManager)
        {
            UnsubscribeBoard();
            subscribedBoard = boardManager;
            if (subscribedBoard != null) subscribedBoard.OnBoardReset += HandleBoardReset;
        }
    }

    private void HandleBoardReset()
    {
        observedResetVersion = boardManager.ResetVersion;
        ClearComboState();
    }

    private void UnsubscribeBoard()
    {
        if (subscribedBoard != null) subscribedBoard.OnBoardReset -= HandleBoardReset;
        subscribedBoard = null;
    }

    private void HandlePresentationChanged(ComboResult result)
    {
        if (result == null) OnPresentationCleared?.Invoke();
        else OnPresentationRequested?.Invoke(result);
    }

    private static bool Matches(int a, int b, int x, int y) => (a == x && b == y) || (a == y && b == x);
    private static string PairKey(Vector2Int a, Vector2Int b)
    {
        if (a.x > b.x || (a.x == b.x && a.y > b.y)) { Vector2Int swap = a; a = b; b = swap; }
        return $"{a.x}:{a.y}|{b.x}:{b.y}";
    }
}
