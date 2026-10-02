using System;
using UnityEngine;

[Serializable]
public class StageBuildingCardData
{
    [SerializeField] private BuildingData building;
    [SerializeField, Min(1)] private int count = 1;

    public BuildingData Building => building;
    public int Count => count;
}
