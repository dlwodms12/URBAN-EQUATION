using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Optional runtime attachment; the Phase4-A card asset is kept intact.
public class BuildingCardHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private BuildingCardUI card;
    private BuildingPlacementController placement;
    private BuildingTooltipUI tooltip;
    private bool hovered;
    private Vector2 pointer;
    public void Configure(BuildingCardUI view, BuildingPlacementController controller, BuildingTooltipUI details)
    {
        if (card == view && placement == controller && tooltip == details) return;
        if (tooltip != null) tooltip.Hide(this); card = view; placement = controller; tooltip = details; hovered = false;
    }
    public void OnPointerEnter(PointerEventData data) { hovered = true; pointer = data.position; Refresh(); }
    public void OnPointerExit(PointerEventData data) { hovered = false; if (tooltip != null) tooltip.Hide(this); }
    private void Update()
    {
        if (Mouse.current != null) pointer = Mouse.current.position.ReadValue();
        Refresh();
    }
    private void Refresh()
    {
        bool dragging = placement != null && placement.IsDragging && placement.SelectedCardId == (card == null ? 0 : card.CardId);
        if (tooltip != null && card != null && card.Card != null && (hovered || dragging))
            tooltip.Show(card.Card.Building, this, pointer);
        else if (tooltip != null) tooltip.Hide(this);
    }
    private void OnDisable() { hovered = false; if (tooltip != null) tooltip.Hide(this); }
}
