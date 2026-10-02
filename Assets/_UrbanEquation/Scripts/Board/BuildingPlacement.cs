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

    private BuildingInstance previewBuilding;

    private Tile currentTile;

    public event Action<int, int> OnBuildingCountChanged;
    public event Action OnBuildingAvailabilityChanged;

    private void OnEnable()
    {
        if (resourceManager != null) resourceManager.OnResourcesChanged += HandleResourcesChanged;
    }

    private void OnDisable()
    {
        if (resourceManager != null) resourceManager.OnResourcesChanged -= HandleResourcesChanged;
        selectedBuilding = null;
        DestroyPreview();
    }

    private void HandleResourcesChanged() => OnBuildingAvailabilityChanged?.Invoke();

    public bool CanSelectBuilding(int buildingCode)
    {
        if (buildingDatabase == null || resourceManager == null || GetRemainingCount(buildingCode) <= 0)
            return false;
        BuildingData building = buildingDatabase.GetBuilding(buildingCode);
        return building != null && resourceManager.CanAffordBuilding(building);
    }

    private readonly Dictionary<int, int> maxBuildingCounts =
    new Dictionary<int, int>
    {
        { 1001, 2 },
        { 2001, 2 },
        { 3001, 2 },
        { 4001, 2 }
    };

    private readonly Dictionary<int, int> buildingCounts =
        new Dictionary<int, int>
        {
            { 1001, 2 },
            { 2001, 2 },
            { 3001, 2 },
            { 4001, 2 }
        };

    private void Update()
    {
        UpdateDragPreview();
    }

    public void StartBuildingDrag(int buildingCode)
    {
        if (!CanSelectBuilding(buildingCode)) return;
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
            $"드래그 시작: " +
            $"{building.BuildingCode} / " +
            $"{building.BuildingName}"
        );

        CreatePreview();
    }

    private void UpdateDragPreview()
    {
        if (previewBuilding != null)
        {
            UpdatePreviewPosition();
            UpdateCurrentTile();
        }

        if (Input.GetMouseButtonUp(0))
        {
            FinishBuildingDrag();
        }
    }

    private void CreatePreview()
    {
        if (previewBuilding != null)
        {
            return;
        }

        if (buildingPrefab == null)
        {
            Debug.LogError(
                "BuildingPlacement에 " +
                "BuildingPrefab이 연결되지 않았습니다."
            );

            return;
        }

        previewBuilding =
            Instantiate(
                buildingPrefab
            );

        previewBuilding.Initialize(
            selectedBuilding,
            Vector2Int.zero
        );

        SetPreviewMode(
            previewBuilding.gameObject
        );
    }

    private void UpdatePreviewPosition()
    {
        Ray ray =
            mainCamera.ScreenPointToRay(
                Input.mousePosition
            );

        Plane groundPlane =
            new Plane(
                Vector3.up,
                Vector3.zero
            );

        if (!groundPlane.Raycast(
                ray,
                out float distance))
        {
            return;
        }

        Vector3 worldPosition =
            ray.GetPoint(distance);

        previewBuilding.transform.position =
            worldPosition;
    }

    private void UpdateCurrentTile()
    {
        Ray ray =
            mainCamera.ScreenPointToRay(
                Input.mousePosition
            );

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit))
        {
            ClearCurrentTile();

            return;
        }

        Tile tile =
            hit.collider.GetComponent<Tile>();

        if (tile == null)
        {
            ClearCurrentTile();

            return;
        }

        if (tile == currentTile)
        {
            return;
        }

        ClearCurrentTile();

        currentTile = tile;

        if (!currentTile.IsOccupied)
        {
            currentTile.SetHighlight(true);
        }
    }

    private void ClearCurrentTile()
    {
        if (currentTile == null)
        {
            return;
        }

        currentTile.SetHighlight(false);

        currentTile = null;
    }

    private void DestroyPreview()
    {
        ClearCurrentTile();

        if (previewBuilding == null)
        {
            return;
        }

        DestroyGenerated(previewBuilding.gameObject);

        previewBuilding = null;
    }

    private void FinishBuildingDrag()
    {
        if (previewBuilding == null)
        {
            return;
        }

        Tile targetTile = currentTile;

        if (targetTile != null &&
            !targetTile.IsOccupied)
        {
            PlaceBuilding(targetTile);
        }

        DestroyPreview();

        selectedBuilding = null;
    }

    private void SetPreviewMode(
        GameObject previewObject)
    {
        Collider[] colliders =
            previewObject.GetComponentsInChildren<Collider>();

        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
    }

    private void PlaceBuilding(Tile tile)
    {
        if (GetRemainingCount(
                selectedBuilding.BuildingCode) <= 0)
        {
            Debug.Log(
                $"{selectedBuilding.BuildingName}은(는) " +
                "더 이상 건설할 수 없습니다."
            );

            return;
        }

        if (tile.IsOccupied)
        {
            Debug.Log(
                "이미 건물이 존재하는 타일입니다."
            );

            return;
        }

        // Legacy tiles have no TileData. Production data must satisfy tile constraints.
        if ((tile.Data != null && !tile.CanAcceptBuilding(selectedBuilding))
            || (tile.Data == null && selectedBuilding.UsesResourceLists)) return;

        if (!resourceManager.CanAffordBuilding(selectedBuilding))
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
                position,
                Quaternion.identity
            );

        building.Initialize(
            selectedBuilding,
            tile.Coordinate
        );

        if (!tile.TrySetBuilding(building))
        {
            DestroyGenerated(building.gameObject);
            return;
        }

        if (!resourceManager.TryApplyBuildingResourcesDeferred(selectedBuilding, out Action publishResources))
        {
            tile.ClearBuilding();
            return;
        }

        int buildingCode =
            selectedBuilding.BuildingCode;

        buildingCounts[buildingCode]--;
        publishResources();

        OnBuildingCountChanged?.Invoke(
            buildingCode,
            buildingCounts[buildingCode]
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

    public int GetRemainingCount(
        int buildingCode)
    {
        if (!buildingCounts.ContainsKey(
                buildingCode))
        {
            return 0;
        }

        return buildingCounts[buildingCode];
    }

    public int GetMaxCount(int buildingCode)
    {
        if (!maxBuildingCounts.ContainsKey(buildingCode))
        {
            return 0;
        }

        return maxBuildingCounts[buildingCode];
    }

    public void ResetBuildings()
    {
        foreach (int buildingCode in maxBuildingCounts.Keys)
        {
            buildingCounts[buildingCode] =
                maxBuildingCounts[buildingCode];

            OnBuildingCountChanged?.Invoke(
                buildingCode,
                buildingCounts[buildingCode]
            );
        }

        selectedBuilding = null;

        DestroyPreview();
    }

    private static void DestroyGenerated(GameObject value)
    {
        value.SetActive(false);
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }
}
