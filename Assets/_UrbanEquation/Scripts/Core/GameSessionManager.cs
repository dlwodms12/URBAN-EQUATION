using System;
using System.Collections.Generic;
using UnityEngine;

// Building/combo completion and turn restore boundary. Goal evaluation is Phase 3-G.
public class GameSessionManager : MonoBehaviour
{
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private BuildingHandManager buildingHand;
    [SerializeField] private BuildingInstance buildingPrefab;
    [SerializeField] private ComboManager comboManager;
    [SerializeField] private TurnHistoryManager turnHistory;
    [SerializeField] private StageManager stageManager;
    private bool committing;

    public BoardManager Board => boardManager;
    public ResourceManager Resources => resourceManager;
    public BuildingHandManager Hand => buildingHand;
    public BuildingInstance BuildingPrefab => buildingPrefab;
    public ComboManager Combos => comboManager;
    public TurnHistoryManager History => turnHistory;
    public bool IsBusy => committing;
    public long ConfigurationVersion { get; private set; }
    public event Action OnStateRestoring;
    public event Action OnStateRestored;
    public IReadOnlyList<ComboResult> LastComboResults { get; private set; }
        = Array.AsReadOnly(new ComboResult[0]);
    public event Action<BuildingInstance, BuildingCardState> OnBuildingCommitted;
    public event Action<BuildingInstance, IReadOnlyList<ComboResult>> OnBuildResolved;

    public void ConfigureCombos(ComboManager manager)
    {
        if (committing) throw new InvalidOperationException("Cannot reconfigure during a build.");
        comboManager = manager;
        ConfigurationVersion++;
        if (turnHistory != null) turnHistory.NotifyConfigurationChanged();
    }

    public void Configure(BoardManager board, ResourceManager resources,
        BuildingHandManager hand, BuildingInstance prefab)
    {
        if (committing) throw new InvalidOperationException("Cannot reconfigure during a build.");
        boardManager = board;
        resourceManager = resources;
        buildingHand = hand;
        buildingPrefab = prefab;
        ConfigurationVersion++;
        if (turnHistory != null) turnHistory.NotifyConfigurationChanged();
    }

    internal void AttachHistory(TurnHistoryManager history, StageManager stage)
    {
        turnHistory = history;
        stageManager = stage;
        ConfigurationVersion++;
    }

    internal void DetachHistory(TurnHistoryManager history)
    {
        if (turnHistory == history) { turnHistory = null; ConfigurationVersion++; }
    }

    public bool CanBuild(int cardId, Vector2Int coordinate, out string error)
    {
        if (committing) { error = "A building transaction is already in progress."; return false; }
        if (turnHistory != null && !turnHistory.CanRecordFor(this, out error)) return false;
        if (comboManager != null && (comboManager.Board != boardManager
            || comboManager.Resources != resourceManager || comboManager.IsResolving))
        { error = "Combo manager is busy or uses different board/resources."; return false; }
        if (buildingPrefab == null) { error = "Building root prefab is not configured."; return false; }
        if (!BuildingPlacementValidator.TryValidate(boardManager, resourceManager,
            buildingHand, cardId, coordinate, out BuildingCardState card, out _, out error)) return false;
        if (card.Building.VisualPrefab == null)
        { error = "The selected building has no visual prefab."; return false; }
        return true;
    }

    public bool TryCommitBuild(int cardId, Vector2Int coordinate,
        out BuildingInstance building, out string error)
    {
        building = null;
        if (!CanBuild(cardId, coordinate, out error)) return false;
        if (!BuildingPlacementValidator.TryValidate(boardManager, resourceManager,
            buildingHand, cardId, coordinate, out BuildingCardState card, out Tile tile, out error))
            return false;
        if (card.Building.VisualPrefab == null)
        { error = "The selected building has no visual prefab."; return false; }

        committing = true;
        GameObject stagingRoot = null;
        BuildingInstance candidate = null;
        try
        {
            try
            {
                stagingRoot = new GameObject("BuildingCandidate");
                stagingRoot.SetActive(false);
                stagingRoot.transform.SetParent(tile.transform, false);
                candidate = Instantiate(buildingPrefab, stagingRoot.transform);
                candidate.transform.position = tile.transform.position;
                candidate.transform.rotation = Quaternion.identity;
                candidate.Initialize(card.Building, coordinate);
            }
            catch (Exception exception)
            {
                error = "Could not create building: " + exception.Message;
                return false;
            }

            // The hand/tile are ready before resource events are published.
            if (!tile.TrySetBuilding(candidate))
            { error = "The target tile became occupied."; return false; }
            if (!buildingHand.TryRemoveCard(cardId, out BuildingCardState removed, out int index))
            {
                tile.TrySetBuilding(null);
                error = "The selected card is no longer in the hand.";
                return false;
            }
            ComboResult[] comboResults = new ComboResult[0];
            if (comboManager != null && !comboManager.TryPrepareCombos(candidate, out comboResults, out error))
            {
                buildingHand.RestoreRemovedCard(index, removed);
                tile.TrySetBuilding(null);
                return false;
            }
            if (!resourceManager.TryApplyBuildingAndRewardsDeferred(card.Building,
                ComboManager.RewardLists(comboResults), out Action publishResources))
            {
                buildingHand.RestoreRemovedCard(index, removed);
                tile.TrySetBuilding(null);
                error = "Building/combo resource transaction was rejected.";
                return false;
            }

            Action publishCombos = comboManager == null ? (Action)(() => { })
                : comboManager.RecordPreparedResults(comboResults);
            LastComboResults = Array.AsReadOnly((ComboResult[])comboResults.Clone());
            candidate.transform.SetParent(tile.transform, true);
            candidate.gameObject.SetActive(true);
            building = candidate;
            publishResources();
            buildingHand.PublishHandChanged();
            publishCombos();
            OnBuildingCommitted?.Invoke(building, removed);
            // Phase 3-G evaluates goals before this completed-turn capture.
            if (turnHistory != null) turnHistory.RecordCompletedTurn(this);
            OnBuildResolved?.Invoke(building, LastComboResults);
            return true;
        }
        finally
        {
            // Success reparented the candidate; failure destroys the entire candidate subtree.
            if (stagingRoot != null) DestroyGenerated(stagingRoot);
            committing = false;
        }
    }

    internal bool TryRestoreTurn(GameStateSnapshot snapshot, Action applyHistory,
        Action publishHistory, out string error)
    {
        error = null;
        if (committing || snapshot == null || boardManager == null || resourceManager == null || buildingHand == null)
        { error = "Cannot restore a turn in this session."; return false; }
        committing = true;
        BoardManager.PreparedBuildingRestore boardRestore = null;
        try
        {
            if (!boardManager.TryPrepareBuildingRestore(snapshot.Buildings, buildingPrefab, out boardRestore, out error))
                return false;
            if (!resourceManager.TryPrepareResourceRestore(snapshot.Resources, out Action applyResources, out Action publishResources))
            { error = "Snapshot resource state is invalid."; return false; }
            if (!buildingHand.TryPrepareCardsRestore(snapshot.Cards, out Action applyCards, out Action publishCards, out error))
                return false;
            Action applyCombos = () => { }, publishCombos = () => { };
            if (comboManager != null)
            {
                if (!comboManager.TryPrepareResultsRestore(snapshot.Combos, snapshot.Buildings,
                    out applyCombos, out publishCombos, out error)) return false;
            }
            else if (snapshot.Combos.Count != 0)
            { error = "Snapshot requires a combo manager."; return false; }
            var recorded = new HashSet<ComboResult>(snapshot.Combos);
            foreach (ComboResult result in snapshot.LastBuildCombos)
                if (!recorded.Contains(result))
                { error = "Last-build combo results are missing from the snapshot ledger."; return false; }
            Action applyStage = () => { }, publishStage = () => { };
            if (stageManager != null)
            {
                if (!stageManager.TryPrepareProgressRestore(snapshot.StageProgress, out applyStage, out publishStage))
                { error = "Snapshot stage state is invalid."; return false; }
            }
            else if (snapshot.StageProgress != null)
            { error = "Snapshot requires a stage manager."; return false; }

            OnStateRestoring?.Invoke();
            boardRestore.Apply();
            applyResources();
            applyCards();
            applyCombos();
            applyStage();
            var last = new ComboResult[snapshot.LastBuildCombos.Count];
            for (int i = 0; i < last.Length; i++) last[i] = snapshot.LastBuildCombos[i];
            LastComboResults = Array.AsReadOnly(last);
            applyHistory();
            publishResources();
            publishCards();
            publishCombos();
            publishStage();
            publishHistory();
            OnStateRestored?.Invoke();
            return true;
        }
        finally
        {
            if (boardRestore != null) boardRestore.Dispose();
            committing = false;
        }
    }

    private static void DestroyGenerated(GameObject value)
    {
        value.SetActive(false);
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }
}
