using System;
using System.Collections.Generic;
using UnityEngine;

public enum StageGoalType
{
    ResourceAtLeast = 0,
    AllCardsUsed = 1,
    AdjacentBuildings = 2
}

[Serializable]
public class StageGoalData
{
    [SerializeField] private StageGoalType goalType;
    [SerializeField, TextArea] private string description;
    [SerializeField] private ResourceAmount resourceTarget;
    [SerializeField] private BuildingData buildingA;
    [SerializeField] private BuildingData buildingB;

    public StageGoalType GoalType => goalType;
    public string Description => description;
    public ResourceAmount ResourceTarget => resourceTarget;
    public BuildingData BuildingA => buildingA;
    public BuildingData BuildingB => buildingB;

    public void Validate(List<string> errors)
    {
        if (!Enum.IsDefined(typeof(StageGoalType), goalType))
            errors.Add("Unknown stage goal type.");
        if (goalType == StageGoalType.ResourceAtLeast)
            DataValidation.ValidateResources(new[] { resourceTarget }, true, errors, "Stage goal");
        if (goalType == StageGoalType.AdjacentBuildings && (buildingA == null || buildingB == null))
            errors.Add("An adjacency goal requires two building definitions.");
    }
}
