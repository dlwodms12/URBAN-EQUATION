using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildingPlacement : MonoBehaviour
{
    [SerializeField]
    private Camera mainCamera;

    [SerializeField]
    private BoardManager boardManager;

    [SerializeField]
    private BuildingDatabase buildingDatabase;

    [SerializeField]
    private BuildingInstance buildingPrefab;

    [SerializeField]
    private ResourceManager resourceManager;

    [SerializeField]
    private ComboManager comboManager;

    [SerializeField]
    private StageManager stageManager;

    private BuildingData selectedBuilding;

    public event Action<int, int> OnBuildingCountChanged;

    private readonly Dictionary<int, int> buildingCounts =
        new Dictionary<int, int>
        {
            { 1001, 1 },
            { 2001, 2 },
            { 3001, 2 },
            { 4001, 2 }
        };

    private void Update()
    {
        SelectBuildingWithKeyboard();
        TryPlaceBuilding();
    }

    private void SelectBuildingWithKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectBuilding(1001);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectBuilding(2001);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SelectBuilding(3001);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SelectBuilding(4001);
        }
    }

    public void SelectBuilding(int buildingCode)
    {
        BuildingData building =
            buildingDatabase.GetBuilding(buildingCode);

        if (building == null)
        {
            return;
        }

        if (GetRemainingCount(buildingCode) <= 0)
        {
            Debug.Log(
                $"{building.BuildingName}은(는) " +
                "더 이상 건설할 수 없습니다."
            );

            return;
        }

        selectedBuilding = building;

        Debug.Log(
            $"건물 선택: " +
            $"{building.BuildingCode} / " +
            $"{building.BuildingName}"
        );
    }

    private void TryPlaceBuilding()
    {
        if (selectedBuilding == null)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        Ray ray =
            mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            return;
        }

        Tile tile =
            hit.collider.GetComponent<Tile>();

        if (tile == null)
        {
            return;
        }

        PlaceBuilding(tile);
    }

    private void PlaceBuilding(Tile tile)
    {
        if (GetRemainingCount(selectedBuilding.BuildingCode) <= 0)
        {
            Debug.Log(
                $"{selectedBuilding.BuildingName}은(는) " +
                "더 이상 건설할 수 없습니다."
            );

            return;
        }

        if (tile.IsOccupied)
        {
            Debug.Log("이미 건물이 존재하는 타일입니다.");
            return;
        }

        if (!resourceManager.CanConsume(
                selectedBuilding.ConsumeResource,
                selectedBuilding.ConsumeAmount))
        {
            Debug.Log(
                $"자원이 부족하여 " +
                $"{selectedBuilding.BuildingName}을(를) " +
                $"건설할 수 없습니다."
            );

            return;
        }

        Vector3 position =
            tile.transform.position;

        BuildingInstance building =
            Instantiate(
                buildingPrefab,
                position + Vector3.up * 0.5f,
                Quaternion.identity
            );

        building.Initialize(
            selectedBuilding,
            tile.Coordinate
        );

        tile.SetBuilding(building);

        int buildingCode =
            selectedBuilding.BuildingCode;

        buildingCounts[buildingCode]--;

        OnBuildingCountChanged?.Invoke(
            buildingCode,
            buildingCounts[buildingCode]
        );

        resourceManager.ApplyBuildingResource(
            selectedBuilding
        );

        comboManager.CheckCombos(
            building
        );

        stageManager.CheckStageClear();

        Debug.Log(
            $"건물 배치: " +
            $"{selectedBuilding.BuildingCode} " +
            $"at {tile.Coordinate}"
        );
    }

    public int GetRemainingCount(int buildingCode)
    {
        if (!buildingCounts.ContainsKey(buildingCode))
        {
            return 0;
        }

        return buildingCounts[buildingCode];
    }

    public int GetMaxCount(int buildingCode)
    {
        if (!buildingCounts.ContainsKey(buildingCode))
        {
            return 0;
        }

        return buildingCounts[buildingCode];
    }
}