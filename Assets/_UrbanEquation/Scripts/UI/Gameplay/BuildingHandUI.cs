using System.Collections.Generic;
using UnityEngine;

public class BuildingHandUI : MonoBehaviour
{
    [SerializeField] private BuildingHandManager hand;
    [SerializeField] private BuildingPlacementController placement;
    [SerializeField] private BuildingCardUI cardPrefab;
    [SerializeField] private Transform content;
    private BuildingHandManager subscribedHand;
    private readonly Dictionary<int, BuildingCardUI> views = new Dictionary<int, BuildingCardUI>();

    public void Bind(BuildingHandManager manager, BuildingPlacementController controller,
        BuildingCardUI prefab, Transform parent)
    {
        Unsubscribe();
        ClearViews();
        hand = manager;
        placement = controller;
        cardPrefab = prefab;
        content = parent;
        if (isActiveAndEnabled) Subscribe();
        Rebuild();
    }

    private void OnEnable()
    {
        Subscribe();
        Rebuild();
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (placement != null) placement.CancelDrag();
    }

    private void OnDestroy() => ClearViews();

    private void Subscribe()
    {
        if (subscribedHand == hand) return;
        Unsubscribe();
        subscribedHand = hand;
        if (subscribedHand == null) return;
        subscribedHand.OnHandChanged += Rebuild;
        subscribedHand.OnCardAvailabilityChanged += RefreshAvailability;
    }

    private void Unsubscribe()
    {
        if (subscribedHand != null)
        {
            subscribedHand.OnHandChanged -= Rebuild;
            subscribedHand.OnCardAvailabilityChanged -= RefreshAvailability;
        }
        subscribedHand = null;
    }

    private void Rebuild()
    {
        if (hand == null || cardPrefab == null || content == null) { ClearViews(); return; }
        var present = new HashSet<int>();
        foreach (BuildingCardState card in hand.Cards) present.Add(card.CardId);
        var removed = new List<int>();
        foreach (var pair in views)
            if (!present.Contains(pair.Key)) removed.Add(pair.Key);
        foreach (int cardId in removed)
        {
            DestroyView(views[cardId]);
            views.Remove(cardId);
        }

        for (int i = 0; i < hand.Cards.Count; i++)
        {
            BuildingCardState card = hand.Cards[i];
            if (!views.TryGetValue(card.CardId, out BuildingCardUI view) || view == null)
            {
                view = Instantiate(cardPrefab, content);
                views[card.CardId] = view;
            }
            view.Bind(card, hand, placement);
            view.transform.SetSiblingIndex(i);
            view.gameObject.SetActive(true);
        }
    }

    private void RefreshAvailability()
    {
        foreach (BuildingCardUI view in views.Values)
            if (view != null) view.RefreshAvailability();
    }

    private void ClearViews()
    {
        foreach (BuildingCardUI view in views.Values) DestroyView(view);
        views.Clear();
    }

    private static void DestroyView(BuildingCardUI view)
    {
        if (view == null) return;
        view.gameObject.SetActive(false);
        // Remove from layout immediately even though runtime Destroy is deferred.
        view.transform.SetParent(null, false);
        if (Application.isPlaying) Destroy(view.gameObject);
        else DestroyImmediate(view.gameObject);
    }
}

