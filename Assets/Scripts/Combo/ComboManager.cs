using System.Collections.Generic;
using UnityEngine;

public class ComboManager : MonoBehaviour
{
    [System.Serializable]
    private class ComboData
    {
        [SerializeField]
        private int comboCode;

        [SerializeField]
        private int buildingCodeA;

        [SerializeField]
        private int buildingCodeB;

        [SerializeField]
        private ResourceType rewardResource;

        [SerializeField]
        private int rewardAmount;

        public int ComboCode => comboCode;

        public int BuildingCodeA => buildingCodeA;

        public int BuildingCodeB => buildingCodeB;

        public ResourceType RewardResource => rewardResource;

        public int RewardAmount => rewardAmount;
    }

    [SerializeField]
    private BoardManager boardManager;

    [SerializeField]
    private ResourceManager resourceManager;

    [SerializeField]
    private List<ComboData> combos = new List<ComboData>();

    public void CheckCombos(BuildingInstance building)
    {
        if (building == null)
        {
            return;
        }

        Vector2Int coordinate =
            building.Coordinate;

        CheckAdjacentTile(
            coordinate + Vector2Int.up,
            building
        );

        CheckAdjacentTile(
            coordinate + Vector2Int.down,
            building
        );

        CheckAdjacentTile(
            coordinate + Vector2Int.left,
            building
        );

        CheckAdjacentTile(
            coordinate + Vector2Int.right,
            building
        );
    }

    private void CheckAdjacentTile(
        Vector2Int coordinate,
        BuildingInstance building)
    {
        Tile tile =
            boardManager.GetTile(coordinate);

        if (tile == null)
        {
            return;
        }

        if (!tile.IsOccupied)
        {
            return;
        }

        BuildingInstance adjacentBuilding =
            tile.Building;

        if (adjacentBuilding == null)
        {
            return;
        }

        CheckCombo(
            building,
            adjacentBuilding
        );
    }

    private void CheckCombo(
        BuildingInstance buildingA,
        BuildingInstance buildingB)
    {
        int buildingCodeA =
            buildingA.Data.BuildingCode;

        int buildingCodeB =
            buildingB.Data.BuildingCode;

        foreach (ComboData combo in combos)
        {
            bool isMatched =
                IsBuildingPairMatched(
                    buildingCodeA,
                    buildingCodeB,
                    combo
                );

            if (!isMatched)
            {
                continue;
            }

            ApplyComboReward(combo);
        }
    }

    private bool IsBuildingPairMatched(
        int buildingCodeA,
        int buildingCodeB,
        ComboData combo)
    {
        bool normalOrder =
            buildingCodeA == combo.BuildingCodeA &&
            buildingCodeB == combo.BuildingCodeB;

        bool reverseOrder =
            buildingCodeA == combo.BuildingCodeB &&
            buildingCodeB == combo.BuildingCodeA;

        return normalOrder || reverseOrder;
    }

    private void ApplyComboReward(
        ComboData combo)
    {
        resourceManager.Add(
            combo.RewardResource,
            combo.RewardAmount
        );

        Debug.Log(
            $"ÄÞº¸ ¹ßµ¿: " +
            $"{combo.ComboCode} / " +
            $"{combo.RewardResource} " +
            $"{FormatAmount(combo.RewardAmount)}"
        );
    }

    private string FormatAmount(int amount)
    {
        if (amount > 0)
        {
            return $"+{amount}";
        }

        return amount.ToString();
    }
}