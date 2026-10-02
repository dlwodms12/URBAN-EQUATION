public static class BuildingPlacementValidator
{
    public static bool TryValidate(BoardManager board, ResourceManager resources,
        BuildingHandManager hand, int cardId, UnityEngine.Vector2Int coordinate,
        out BuildingCardState card, out Tile tile, out string error)
    {
        card = null;
        tile = null;
        error = null;
        if (board == null || resources == null || hand == null)
        { error = "Building requires BoardManager, ResourceManager and BuildingHandManager."; return false; }
        if (hand.Resources != resources)
        { error = "Hand and session must use the same ResourceManager."; return false; }
        if (!hand.TryGetCard(cardId, out card) || card.Building == null)
        { error = "The selected card is no longer in the hand."; return false; }
        tile = board.GetTile(coordinate);
        if (tile == null)
        { error = "The target is outside the current board."; return false; }
        if (tile.IsOccupied)
        { error = "The target tile is occupied."; return false; }
        if (!tile.CanAcceptBuilding(card.Building))
        { error = "The building is not allowed on this tile type."; return false; }
        if (!resources.CanAffordBuilding(card.Building))
        { error = "Required resources are not available."; return false; }
        return true;
    }
}
