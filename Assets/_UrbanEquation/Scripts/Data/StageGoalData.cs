using System;
using System.Collections.Generic;
using UnityEngine;

public enum StageGoalType
{
    ResourceAtLeast = 0,
    AllCardsUsed = 1,
    AdjacentBuildings = 2,
    EachTileTypeBuilt = 3,
    BuildingCountAtMost = 4,
    ComboCountAtLeast = 5,
    ComplaintCountAtLeast = 6,
    ComboReviewed = 7,
    ComplaintReviewed = 8
}

[Serializable]
public class StageGoalData
{
    [SerializeField] private StageGoalType goalType;
    [SerializeField, TextArea] private string description;
    [SerializeField] private ResourceAmount resourceTarget;
    [SerializeField] private BuildingData buildingA;
    [SerializeField] private BuildingData buildingB;
    [SerializeField, Min(1)] private int targetCount = 1;

    public StageGoalType GoalType => goalType;
    public string Description => description;
    public ResourceAmount ResourceTarget => resourceTarget;
    public BuildingData BuildingA => buildingA;
    public BuildingData BuildingB => buildingB;
    public int TargetCount => targetCount;

    public void Validate(List<string> errors)
    {
        if (!Enum.IsDefined(typeof(StageGoalType), goalType))
            errors.Add("Unknown stage goal type.");
        if ((goalType == StageGoalType.BuildingCountAtMost || goalType == StageGoalType.ComboCountAtLeast
            || goalType == StageGoalType.ComplaintCountAtLeast) && targetCount < 1)
            errors.Add("A count goal requires a positive target.");
        if (goalType == StageGoalType.ResourceAtLeast)
            DataValidation.ValidateResources(new[] { resourceTarget }, true, errors, "Stage goal");
        if (goalType == StageGoalType.AdjacentBuildings && (buildingA == null || buildingB == null))
            errors.Add("An adjacency goal requires two building definitions.");
        else if (goalType == StageGoalType.AdjacentBuildings
            && (buildingA.BuildingCode <= 0 || buildingB.BuildingCode <= 0))
            errors.Add("Adjacency goal building codes must be positive.");
    }
}
