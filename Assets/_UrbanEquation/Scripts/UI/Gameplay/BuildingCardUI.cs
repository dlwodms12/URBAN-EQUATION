using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildingCardUI : MonoBehaviour, IPointerDownHandler, IDragHandler,
    IPointerUpHandler, IEndDragHandler
{
    [SerializeField] private TMP_Text buildingNameText;
    [SerializeField] private Image cardImage;
    [SerializeField] private Image disabledOverlay;
    [SerializeField] private Button button;
    private BuildingHandManager hand;
    private BuildingPlacementController placement;

    public BuildingCardState Card { get; private set; }
    public int CardId => Card == null ? 0 : Card.CardId;
    public bool IsAvailable => Card != null && hand != null && hand.IsCardAvailable(CardId);

    public void Bind(BuildingCardState card, BuildingHandManager manager, BuildingPlacementController controller)
    {
        Card = card;
        hand = manager;
        placement = controller;
        if (button == null) button = GetComponent<Button>();
        if (buildingNameText != null)
            buildingNameText.text = Card == null ? string.Empty : Card.Building.BuildingName;
        if (cardImage != null) cardImage.sprite = Card == null ? null : Card.Building.CardImage;
        RefreshAvailability();
    }

    public void RefreshAvailability()
    {
        bool available = IsAvailable;
        if (button != null) button.interactable = available;
        if (disabledOverlay != null)
        {
            disabledOverlay.color = new Color(0f, 0f, 0f, 0.5f);
            disabledOverlay.raycastTarget = false;
            disabledOverlay.gameObject.SetActive(!available);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !IsAvailable || placement == null) return;
        if (placement.TryBeginDrag(CardId)) placement.UpdatePointer(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (placement != null && placement.SelectedCardId == CardId)
            placement.UpdatePointer(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left
            && placement != null && placement.SelectedCardId == CardId)
            placement.TryFinishDrag(eventData.position, out _);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (placement != null && placement.SelectedCardId == CardId) placement.CancelDrag();
    }
}

