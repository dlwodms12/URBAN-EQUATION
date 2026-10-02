using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildingHandManager : MonoBehaviour
{
    [SerializeField] private ResourceManager resourceManager;
    private ResourceManager subscribedResources;
    private readonly List<BuildingCardState> cards = new List<BuildingCardState>();
    private BuildingCardState[] initialCards = new BuildingCardState[0];
    private bool initialized;

    public IReadOnlyList<BuildingCardState> Cards => cards.AsReadOnly();
    public ResourceManager Resources => resourceManager;
    public bool IsInitialized => initialized;
    public long InitializationVersion { get; private set; }
    public event Action OnHandChanged;
    public event Action OnCardAvailabilityChanged;

    private void OnEnable() => Subscribe();
    private void OnDisable() => Unsubscribe();

    public bool TryInitializeFromStage(StageData stage, ResourceManager resources, out string error)
    {
        error = null;
        if (stage == null || resources == null)
        { error = "Hand requires StageData and ResourceManager."; return false; }
        if (stage.BuildingCards.Count == 0)
        { error = "Stage has no building cards."; return false; }

        long total = 0;
        var definitions = new Dictionary<int, BuildingData>();
        foreach (StageBuildingCardData entry in stage.BuildingCards)
        {
            if (entry == null || entry.Building == null || entry.Count < 1)
            { error = "Each stage card entry requires a building and positive count."; return false; }
            var errors = new List<string>();
            entry.Building.Validate(errors);
            if (errors.Count > 0) { error = string.Join("; ", errors); return false; }
            int code = entry.Building.BuildingCode;
            if (definitions.TryGetValue(code, out BuildingData previous) && previous != entry.Building)
            { error = "Stage contains conflicting definitions for the same building code."; return false; }
            definitions[code] = entry.Building;
            total += entry.Count;
            if (total > int.MaxValue)
            { error = "Stage card count exceeds the supported integer range."; return false; }
        }

        var next = new List<BuildingCardState>();
        foreach (StageBuildingCardData entry in stage.BuildingCards)
            for (int i = 0; i < entry.Count; i++)
                next.Add(new BuildingCardState(next.Count + 1, entry.Building));

        Unsubscribe();
        resourceManager = resources;
        initialCards = next.ToArray();
        initialized = true;
        InitializationVersion++;
        cards.Clear();
        cards.AddRange(next);
        if (isActiveAndEnabled) Subscribe();
        PublishHandChanged();
        return true;
    }

    public bool TryGetCard(int cardId, out BuildingCardState card)
    {
        card = null;
        foreach (BuildingCardState candidate in cards)
            if (candidate.CardId == cardId) { card = candidate; return true; }
        return false;
    }

    public bool IsCardAvailable(int cardId)
    {
        return resourceManager != null && TryGetCard(cardId, out BuildingCardState card)
            && resourceManager.CanAffordBuilding(card.Building);
    }

    public BuildingCardState[] CaptureCards() => cards.ToArray();

    public bool TryRestoreCards(IReadOnlyList<BuildingCardState> snapshot, out string error)
    {
        if (!TryPrepareCardsRestore(snapshot, out Action apply, out Action publish, out error)) return false;
        apply();
        publish();
        return true;
    }

    internal bool TryPrepareCardsRestore(IReadOnlyList<BuildingCardState> snapshot,
        out Action apply, out Action publish, out string error)
    {
        apply = null;
        publish = null;
        error = null;
        if (!initialized || snapshot == null)
        { error = "Card restore requires an initialized hand and a snapshot."; return false; }
        var seen = new HashSet<int>();
        var next = new List<BuildingCardState>();
        foreach (BuildingCardState card in snapshot)
        {
            if (card == null || card.CardId < 1 || card.CardId > initialCards.Length
                || initialCards[card.CardId - 1] != card || !seen.Add(card.CardId))
            { error = "Snapshot contains an unknown, foreign, or duplicate card."; return false; }
            next.Add(card);
        }
        apply = () => { cards.Clear(); cards.AddRange(next); };
        publish = PublishHandChanged;
        return true;
    }

    public void ResetCards()
    {
        if (!initialized) return;
        cards.Clear();
        cards.AddRange(initialCards);
        PublishHandChanged();
    }

    // Only the session consumes a card, without exposing a partially committed build.
    internal bool TryRemoveCard(int cardId, out BuildingCardState card, out int index)
    {
        card = null;
        index = -1;
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].CardId != cardId) continue;
            index = i;
            card = cards[i];
            cards.RemoveAt(i);
            return true;
        }
        return false;
    }

    internal void RestoreRemovedCard(int index, BuildingCardState card) => cards.Insert(index, card);
    internal void PublishHandChanged() => OnHandChanged?.Invoke();

    private void Subscribe()
    {
        if (subscribedResources == resourceManager) return;
        Unsubscribe();
        subscribedResources = resourceManager;
        if (subscribedResources != null)
            subscribedResources.OnResourcesChanged += HandleResourcesChanged;
    }

    private void Unsubscribe()
    {
        if (subscribedResources != null)
            subscribedResources.OnResourcesChanged -= HandleResourcesChanged;
        subscribedResources = null;
    }

    private void HandleResourcesChanged() => OnCardAvailabilityChanged?.Invoke();
}
