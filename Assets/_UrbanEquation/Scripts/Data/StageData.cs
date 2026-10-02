using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageData", menuName = "Urban Equation/Stage Data")]
public class StageData : ScriptableObject
{
    public const int AdditionalGoalCount = 2;
    public const int MinimumClearRank = 1;
    public const int MaximumClearRank = 3;

    [SerializeField, Min(1)] private int stageNumber = 1;
    [SerializeField] private string stageName;
    [SerializeField, TextArea] private string introduction;
    [SerializeField, Min(1)] private int width = 2;
    [SerializeField, Min(1)] private int height = 2;
    [Tooltip("Document order: north row first, west to east within each row. Coordinate Y is world +Z.")]
    [SerializeField] private TileData[] tiles = new TileData[0];
    [SerializeField] private ResourceAmount[] initialResources = new ResourceAmount[0];
    [Tooltip("Entries are expanded in this order; each entry creates Count distinct cards.")]
    [SerializeField] private StageBuildingCardData[] buildingCards = new StageBuildingCardData[0];
    [SerializeField] private StageGoalData requiredGoal = new StageGoalData();
    [SerializeField] private StageGoalData[] additionalGoals = new StageGoalData[AdditionalGoalCount];

    public int StageNumber => stageNumber;
    public string StageName => stageName;
    public string Introduction => introduction;
    public int Width => width;
    public int Height => height;
    public IReadOnlyList<TileData> Tiles => Array.AsReadOnly(tiles);
    public IReadOnlyList<ResourceAmount> InitialResources => Array.AsReadOnly(initialResources);
    public IReadOnlyList<StageBuildingCardData> BuildingCards => Array.AsReadOnly(buildingCards);
    public StageGoalData RequiredGoal => requiredGoal;
    public IReadOnlyList<StageGoalData> AdditionalGoals => Array.AsReadOnly(additionalGoals);

    public bool TryGetTile(Vector2Int coordinate, out TileData tile)
    {
        tile = null;
        if (width <= 0 || height <= 0 || (long)width * height != tiles.Length
            || coordinate.x < 0 || coordinate.x >= width
            || coordinate.y < 0 || coordinate.y >= height) return false;

        int index = (height - 1 - coordinate.y) * width + coordinate.x;
        tile = tiles[index];
        return tile != null;
    }

    public void Validate(List<string> errors)
    {
        if (stageNumber < 1) errors.Add("Stage number must be positive.");
        if (width < 1 || height < 1 || (long)width * height != tiles.Length)
            errors.Add("Stage tiles must match Width x Height.");
        foreach (TileData tile in tiles)
            if (tile == null) errors.Add("Stage contains a missing tile definition.");
        DataValidation.ValidateResources(initialResources, true, errors, "Initial resources");
        if (initialResources.Length != 5) errors.Add("Stage requires all five initial resource values.");
        if (buildingCards.Length == 0) errors.Add("Stage has no building cards.");
        foreach (StageBuildingCardData card in buildingCards)
            if (card == null || card.Building == null || card.Count < 1)
                errors.Add("Stage card requires a building and a positive count.");
        if (requiredGoal == null) errors.Add("Stage requires a mandatory goal.");
        else requiredGoal.Validate(errors);
        if (additionalGoals.Length != AdditionalGoalCount)
            errors.Add("Stage requires exactly two additional goals.");
        foreach (StageGoalData goal in additionalGoals)
            if (goal == null) errors.Add("Stage has a missing additional goal.");
            else goal.Validate(errors);
    }
}
