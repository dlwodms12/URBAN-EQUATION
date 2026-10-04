using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ComboResult
{
    internal object OwnerToken { get; }
    public int ResultId { get; }
    public int ComboCode { get; }
    public string Code => $"C{ComboCode:00000}";
    public string ComboName { get; }
    public string Description { get; }
    public int BuildingCodeA { get; }
    public int BuildingCodeB { get; }
    public Vector2Int SourceCoordinate { get; }
    public Vector2Int AdjacentCoordinate { get; }
    public Vector2Int Direction => AdjacentCoordinate - SourceCoordinate;
    public Vector3 PresentationPosition { get; }
    public IReadOnlyList<ResourceAmount> Rewards { get; }
    public bool IsComplaint
    {
        get { foreach (ResourceAmount reward in Rewards) if (reward.Amount < 0) return true; return false; }
    }

    internal ComboResult(object owner, int resultId, int comboCode, string name, string description,
        int buildingCodeA, int buildingCodeB, Vector2Int source, Vector2Int adjacent,
        Vector3 presentationPosition, IReadOnlyList<ResourceAmount> rewards)
    {
        OwnerToken = owner;
        ResultId = resultId;
        ComboCode = comboCode;
        ComboName = name;
        Description = description;
        BuildingCodeA = buildingCodeA;
        BuildingCodeB = buildingCodeB;
        SourceCoordinate = source;
        AdjacentCoordinate = adjacent;
        PresentationPosition = presentationPosition;
        var copy = new ResourceAmount[rewards.Count];
        for (int i = 0; i < copy.Length; i++) copy[i] = rewards[i];
        Rewards = Array.AsReadOnly(copy);
    }
}
