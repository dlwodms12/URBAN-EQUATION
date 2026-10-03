using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BoardDetailsUI : MonoBehaviour
{
    private GameSessionManager session;
    private BuildingPlacementController placement;
    private Camera boardCamera;
    private BuildingTooltipUI tooltip;
    private ComboPopupUI replay;
    private GameSessionManager subscribed;
    public BuildingInstance SelectedBuilding { get; private set; }

    public void Bind(GameSessionManager game, BuildingPlacementController controller, Camera camera,
        BuildingTooltipUI details, ComboPopupUI historyPopup)
    {
        Unsubscribe(); ClearSelection(); session = game; placement = controller;
        boardCamera = camera; tooltip = details; replay = historyPopup;
        if (isActiveAndEnabled) Subscribe();
    }
    public bool SelectBuilding(BuildingInstance building)
    {
        if (replay != null) replay.DismissPersistent();
        if (session == null || !session.GameplayEnabled || session.IsBusy
            || (placement != null && placement.IsDragging) || !IsCurrent(building))
        { SelectedBuilding = null; return false; }
        BuildingInstance previous = SelectedBuilding; SelectedBuilding = building;
        if (IsCurrent(previous) && previous != building && session.Combos != null
            && session.Combos.TryGetAppliedCombo(previous.Coordinate, building.Coordinate, out ComboResult result))
        { if (replay != null) replay.ShowPersistent(result, boardCamera); return true; }
        return false;
    }
    public void ClearSelection()
    {
        SelectedBuilding = null; if (tooltip != null) tooltip.Hide(this);
        if (replay != null) replay.DismissPersistent();
    }
    private bool IsCurrent(BuildingInstance building) => building != null && session != null && session.Board != null
        && session.Board.GetTile(building.Coordinate)?.Building == building;
    private void Update()
    {
        if (session == null || !session.GameplayEnabled || session.IsBusy || (placement != null && placement.IsDragging))
        { ClearSelection(); return; }
        if (Mouse.current == null || boardCamera == null) return;
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        BuildingInstance building = null;
        Vector2 point = Mouse.current.position.ReadValue();
        if (!overUI && Physics.Raycast(boardCamera.ScreenPointToRay(point), out RaycastHit hit))
        {
            building = hit.collider.GetComponentInParent<BuildingInstance>();
            if (building == null) building = hit.collider.GetComponentInParent<Tile>()?.Building;
            if (!IsCurrent(building)) building = null;
        }
        if (building != null && tooltip != null) tooltip.Show(building.Data, this, point);
        else if (tooltip != null) tooltip.Hide(this);
        if (Mouse.current.leftButton.wasPressedThisFrame) SelectBuilding(building);
    }
    private void HandleState() { if (session == null || !session.GameplayEnabled) ClearSelection(); }
    private void OnEnable() => Subscribe();
    private void OnDisable() { Unsubscribe(); ClearSelection(); }
    private void OnDestroy() => Unsubscribe();
    private void Subscribe()
    {
        if (subscribed == session) return; Unsubscribe(); subscribed = session;
        if (subscribed == null) return;
        subscribed.OnStateRestoring += ClearSelection; subscribed.OnGameplayStateChanged += HandleState;
    }
    private void Unsubscribe()
    {
        if (subscribed != null) { subscribed.OnStateRestoring -= ClearSelection; subscribed.OnGameplayStateChanged -= HandleState; }
        subscribed = null;
    }
}
