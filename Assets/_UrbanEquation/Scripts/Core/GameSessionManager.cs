using System;
using UnityEngine;

// Building commit boundary. Combo/goals/history are attached in subsequent phases.
public class GameSessionManager : MonoBehaviour
{
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private BuildingHandManager buildingHand;
    [SerializeField] private BuildingInstance buildingPrefab;
    private bool committing;

    public BoardManager Board => boardManager;
    public ResourceManager Resources => resourceManager;
    public BuildingHandManager Hand => buildingHand;
    public BuildingInstance BuildingPrefab => buildingPrefab;
    public event Action<BuildingInstance, BuildingCardState> OnBuildingCommitted;

    public void Configure(BoardManager board, ResourceManager resources,
        BuildingHandManager hand, BuildingInstance prefab)
    {
        if (committing) throw new InvalidOperationException("Cannot reconfigure during a build.");
        boardManager = board;
        resourceManager = resources;
        buildingHand = hand;
        buildingPrefab = prefab;
    }

    public bool CanBuild(int cardId, Vector2Int coordinate, out string error)
    {
        if (committing) { error = "A building transaction is already in progress."; return false; }
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
            if (!resourceManager.TryApplyBuildingResourcesDeferred(card.Building, out Action publishResources))
            {
                buildingHand.RestoreRemovedCard(index, removed);
                tile.TrySetBuilding(null);
                error = "Building resource transaction was rejected.";
                return false;
            }

            candidate.transform.SetParent(tile.transform, true);
            candidate.gameObject.SetActive(true);
            building = candidate;
            publishResources();
            buildingHand.PublishHandChanged();
            // Later phases process combos, goals and completed-turn snapshots here.
            OnBuildingCommitted?.Invoke(building, removed);
            return true;
        }
        finally
        {
            // Success reparented the candidate; failure destroys the entire candidate subtree.
            if (stagingRoot != null) DestroyGenerated(stagingRoot);
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
