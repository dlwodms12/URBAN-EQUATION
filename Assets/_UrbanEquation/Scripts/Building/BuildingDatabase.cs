using System.Collections.Generic;
using UnityEngine;

public class BuildingDatabase : MonoBehaviour
{
    [SerializeField]
    private List<BuildingData> buildings = new List<BuildingData>();

    private Dictionary<int, BuildingData> buildingDictionary;

    private void Awake()
    {
        InitializeDictionary();
    }

    // 건물 데이터를 Dictionary로 초기화하는 코드
    private void InitializeDictionary()
    {
        buildingDictionary = new Dictionary<int, BuildingData>();

        foreach (BuildingData building in buildings)
        {
            if (building == null)
            {
                continue;
            }

            if (buildingDictionary.ContainsKey(building.BuildingCode))
            {
                Debug.LogError(
                    $"중복된 건물 코드가 존재합니다: {building.BuildingCode}"
                );

                continue;
            }

            buildingDictionary.Add(
                building.BuildingCode,
                building
            );
        }
    }

    // 건물 데이터를 반환하는 코드
    public BuildingData GetBuilding(int buildingCode)
    {
        if (buildingDictionary.TryGetValue(
                buildingCode,
                out BuildingData building))
        {
            return building;
        }

        Debug.LogWarning(
            $"건물 코드를 찾을 수 없습니다: {buildingCode}"
        );

        return null;
    }
}