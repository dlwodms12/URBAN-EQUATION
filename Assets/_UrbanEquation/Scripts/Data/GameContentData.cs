using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameContent", menuName = "Urban Equation/Game Content")]
public class GameContentData : ScriptableObject
{
    [SerializeField] private BuildingCatalog buildingCatalog;
    [SerializeField] private ComboDatabase comboDatabase;
    [SerializeField] private StageCatalog stageCatalog;
    [SerializeField] private BuildingInstance buildingPrefab;

    public BuildingCatalog Buildings => buildingCatalog;
    public ComboDatabase Combos => comboDatabase;
    public StageCatalog Stages => stageCatalog;
    public BuildingInstance BuildingPrefab => buildingPrefab;

    public void Validate(List<string> errors)
    {
        if (buildingCatalog == null || comboDatabase == null || stageCatalog == null || buildingPrefab == null)
        { errors.Add("Game content requires building, combo, stage catalogs and a building prefab."); return; }
        buildingCatalog.Validate(errors);
        comboDatabase.Validate(errors);
        stageCatalog.Validate(errors);
        foreach (ComboDefinition combo in comboDatabase.Combos)
        {
            if (combo == null) continue;
            if (!buildingCatalog.TryGetBuilding(combo.BuildingCodeA, out _)
                || !buildingCatalog.TryGetBuilding(combo.BuildingCodeB, out _))
                errors.Add("A combo references a building outside the catalog.");
        }
        foreach (StageData stage in stageCatalog.Stages)
        {
            if (stage == null) continue;
            foreach (TileData tile in stage.Tiles)
                if (tile != null && (tile.TilePrefab == null || !Enum.IsDefined(typeof(TileType), tile.TileType)))
                    errors.Add("Stage tiles require a valid type and prefab.");
            long count = 0;
            foreach (StageBuildingCardData card in stage.BuildingCards)
            {
                if (card == null || card.Building == null) continue;
                CheckBuilding(card.Building, errors);
                if (card.Building.VisualPrefab == null) errors.Add("A playable stage building has no visual prefab.");
                count += card.Count;
            }
            if (count > int.MaxValue) errors.Add("Stage card count exceeds the supported range.");
            CheckGoal(stage.RequiredGoal, errors);
            foreach (StageGoalData goal in stage.AdditionalGoals) CheckGoal(goal, errors);
        }
    }

    private void CheckGoal(StageGoalData goal, List<string> errors)
    {
        if (goal == null || goal.GoalType != StageGoalType.AdjacentBuildings) return;
        CheckBuilding(goal.BuildingA, errors);
        CheckBuilding(goal.BuildingB, errors);
    }

    private void CheckBuilding(BuildingData building, List<string> errors)
    {
        if (building == null || !building.UsesResourceLists
            || !buildingCatalog.TryGetBuilding(building.BuildingCode, out BuildingData canonical)
            || canonical != building) errors.Add("Stages must reference production definitions from the building catalog.");
    }
}
