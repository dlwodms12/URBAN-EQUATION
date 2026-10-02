public sealed class BuildingCardState
{
    public int CardId { get; }
    public BuildingData Building { get; }

    internal BuildingCardState(int cardId, BuildingData building)
    {
        CardId = cardId;
        Building = building;
    }
}

