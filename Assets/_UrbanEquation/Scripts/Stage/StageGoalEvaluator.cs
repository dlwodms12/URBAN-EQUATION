using System;
using System.Collections.Generic;
using UnityEngine;

// Evaluates copied runtime state without paying resources or changing any manager.
public static class StageGoalEvaluator
{
    public static bool TryValidateGoals(StageData stage, out string error)
    {
        error = null;
        if (stage == null || stage.StageNumber < 1 || stage.RequiredGoal == null
            || stage.AdditionalGoals.Count != StageData.AdditionalGoalCount)
        { error = "Stage requires a positive number, a required goal and exactly two additional goals."; return false; }
        var errors = new List<string>();
        stage.RequiredGoal.Validate(errors);
        foreach (StageGoalData goal in stage.AdditionalGoals)
        {
            if (goal == null) errors.Add("Additional goal is missing.");
            else goal.Validate(errors);
        }
        error = errors.Count == 0 ? null : string.Join("; ", errors);
        return errors.Count == 0;
    }

    public static bool TryEvaluateStage(StageData stage, IReadOnlyList<BuildingStateSnapshot> buildings,
        IReadOnlyList<ResourceAmount> resources, int remainingCards,
        out StageProgressState progress, out string error)
    {
        return TryEvaluateStageWithInteractions(stage, buildings, resources, remainingCards,
            new ComboResult[0], false, false, out progress, out error);
    }

    public static bool TryEvaluateStageWithInteractions(StageData stage,
        IReadOnlyList<BuildingStateSnapshot> buildings, IReadOnlyList<ResourceAmount> resources,
        int remainingCards, IReadOnlyList<ComboResult> interactions, bool comboReviewed, bool complaintReviewed,
        out StageProgressState progress, out string error)
    {
        progress = null;
        if (!TryValidateGoals(stage, out error)
            || !TryReadContext(buildings, resources, remainingCards, out var amounts, out error)) return false;
        if (interactions == null) { error = "Interaction results are required."; return false; }
        foreach (ComboResult result in interactions)
            if (result == null) { error = "Interaction results contain a missing entry."; return false; }
        var achieved = new bool[3];
        achieved[0] = Evaluate(stage.RequiredGoal, buildings, amounts, remainingCards,
            stage, interactions, comboReviewed, complaintReviewed);
        for (int i = 0; i < StageData.AdditionalGoalCount; i++)
            achieved[i + 1] = Evaluate(stage.AdditionalGoals[i], buildings, amounts, remainingCards,
                stage, interactions, comboReviewed, complaintReviewed);
        int rank = achieved[0] ? 1 + (achieved[1] ? 1 : 0) + (achieved[2] ? 1 : 0) : 0;
        progress = new StageProgressState(false, achieved, rank, achieved[0], comboReviewed, complaintReviewed);
        return true;
    }

    public static bool TryEvaluateGoal(StageGoalData goal, IReadOnlyList<BuildingStateSnapshot> buildings,
        IReadOnlyList<ResourceAmount> resources, int remainingCards, out bool achieved, out string error)
    {
        achieved = false;
        error = null;
        if (goal == null) { error = "Goal is missing."; return false; }
        var errors = new List<string>();
        goal.Validate(errors);
        if (errors.Count > 0) { error = string.Join("; ", errors); return false; }
        if (!TryReadContext(buildings, resources, remainingCards, out var amounts, out error)) return false;
        achieved = Evaluate(goal, buildings, amounts, remainingCards);
        return true;
    }

    private static bool TryReadContext(IReadOnlyList<BuildingStateSnapshot> buildings,
        IReadOnlyList<ResourceAmount> resources, int remainingCards,
        out Dictionary<ResourceType, int> amounts, out string error)
    {
        amounts = null;
        error = null;
        if (buildings == null || resources == null || resources.Count != 5 || remainingCards < 0)
        { error = "Goal evaluation requires placements, all five resources and a nonnegative card count."; return false; }
        var next = new Dictionary<ResourceType, int>();
        foreach (ResourceAmount amount in resources)
        {
            if (!Enum.IsDefined(typeof(ResourceType), amount.Resource) || next.ContainsKey(amount.Resource))
            { error = "Goal resources contain an unknown or duplicate resource."; return false; }
            // Signed runtime values from legacy combos are valid; targets remain nonnegative.
            next.Add(amount.Resource, amount.Amount);
        }
        var coordinates = new HashSet<Vector2Int>();
        foreach (BuildingStateSnapshot building in buildings)
        {
            if (building == null || building.Building == null || building.BuildingCode <= 0
                || building.BuildingCode != building.Building.BuildingCode
                || building.Coordinate.x < 0 || building.Coordinate.y < 0
                || !coordinates.Add(building.Coordinate))
            { error = "Goal placements contain an invalid, changed or duplicate building."; return false; }
        }
        amounts = next;
        return true;
    }

    private static bool Evaluate(StageGoalData goal, IReadOnlyList<BuildingStateSnapshot> buildings,
        Dictionary<ResourceType, int> resources, int remainingCards, StageData stage = null,
        IReadOnlyList<ComboResult> interactions = null, bool comboReviewed = false, bool complaintReviewed = false)
    {
        switch (goal.GoalType)
        {
            case StageGoalType.ResourceAtLeast:
                return resources[goal.ResourceTarget.Resource] >= goal.ResourceTarget.Amount;
            case StageGoalType.AllCardsUsed:
                return remainingCards == 0;
            case StageGoalType.AdjacentBuildings:
                int a = goal.BuildingA.BuildingCode, b = goal.BuildingB.BuildingCode;
                for (int i = 0; i < buildings.Count; i++)
                    for (int j = i + 1; j < buildings.Count; j++)
                    {
                        BuildingStateSnapshot first = buildings[i], second = buildings[j];
                        if (!((first.BuildingCode == a && second.BuildingCode == b)
                            || (first.BuildingCode == b && second.BuildingCode == a))) continue;
                        long dx = (long)first.Coordinate.x - second.Coordinate.x;
                        long dy = (long)first.Coordinate.y - second.Coordinate.y;
                        if (Math.Abs(dx) + Math.Abs(dy) == 1) return true;
                    }
                return false;
            case StageGoalType.EachTileTypeBuilt:
                if (stage == null) return false;
                var tileTypes = new HashSet<TileType>();
                foreach (BuildingStateSnapshot building in buildings)
                    if (stage.TryGetTile(building.Coordinate, out TileData tile)) tileTypes.Add(tile.TileType);
                return tileTypes.Contains(TileType.Asphalt) && tileTypes.Contains(TileType.Concrete)
                    && tileTypes.Contains(TileType.Grass);
            case StageGoalType.BuildingCountAtMost:
                return buildings.Count <= goal.TargetCount;
            case StageGoalType.ComboCountAtLeast:
                return CountInteractions(buildings, interactions, false) >= goal.TargetCount;
            case StageGoalType.ComplaintCountAtLeast:
                return CountInteractions(buildings, interactions, true) >= goal.TargetCount;
            case StageGoalType.ComboReviewed: return comboReviewed;
            case StageGoalType.ComplaintReviewed: return complaintReviewed;
            default: return false;
        }
    }
    private static int CountInteractions(IReadOnlyList<BuildingStateSnapshot> buildings,
        IReadOnlyList<ComboResult> interactions, bool complaint)
    {
        if (interactions == null) return 0;
        var placements = new Dictionary<Vector2Int, int>();
        foreach (BuildingStateSnapshot building in buildings) placements.Add(building.Coordinate, building.BuildingCode);
        var pairs = new HashSet<string>();
        foreach (ComboResult result in interactions)
        {
            if (result == null || result.IsComplaint != complaint
                || !placements.TryGetValue(result.SourceCoordinate, out int a) || a != result.BuildingCodeA
                || !placements.TryGetValue(result.AdjacentCoordinate, out int b) || b != result.BuildingCodeB) continue;
            Vector2Int first = result.SourceCoordinate, second = result.AdjacentCoordinate;
            if (System.Math.Abs((long)first.x - second.x) + System.Math.Abs((long)first.y - second.y) != 1) continue;
            if (first.x > second.x || (first.x == second.x && first.y > second.y))
            { Vector2Int swap = first; first = second; second = swap; }
            pairs.Add($"{first.x},{first.y}:{second.x},{second.y}");
        }
        return pairs.Count;
    }

}
