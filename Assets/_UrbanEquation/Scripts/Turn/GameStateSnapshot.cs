using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BuildingStateSnapshot
{
    public Vector2Int Coordinate { get; }
    public BuildingData Building { get; }
    public int BuildingCode { get; }

    public BuildingStateSnapshot(Vector2Int coordinate, BuildingData building)
    {
        if (building == null) throw new ArgumentNullException(nameof(building));
        Coordinate = coordinate;
        Building = building;
        BuildingCode = building.BuildingCode;
    }
}

// Definitions/cards are runtime references; scene objects and visual instances are not retained.
public sealed class GameStateSnapshot
{
    internal object OwnerToken { get; }
    public int TurnNumber { get; }
    public IReadOnlyList<BuildingStateSnapshot> Buildings { get; }
    public IReadOnlyList<ResourceAmount> Resources { get; }
    public IReadOnlyList<BuildingCardState> Cards { get; }
    public IReadOnlyList<ComboResult> Combos { get; }
    public IReadOnlyList<ComboResult> LastBuildCombos { get; }
    public StageProgressState StageProgress { get; }

    internal GameStateSnapshot(object owner, int turnNumber,
        IReadOnlyList<BuildingStateSnapshot> buildings, IReadOnlyList<ResourceAmount> resources,
        IReadOnlyList<BuildingCardState> cards, IReadOnlyList<ComboResult> combos,
        IReadOnlyList<ComboResult> lastBuildCombos, StageProgressState stageProgress)
    {
        OwnerToken = owner;
        TurnNumber = turnNumber;
        Buildings = Copy(buildings);
        Resources = Copy(resources);
        Cards = Copy(cards);
        Combos = Copy(combos);
        LastBuildCombos = Copy(lastBuildCombos);
        StageProgress = stageProgress;
    }

    private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> source)
    {
        var copy = new T[source.Count];
        for (int i = 0; i < copy.Length; i++) copy[i] = source[i];
        return Array.AsReadOnly(copy);
    }
}
