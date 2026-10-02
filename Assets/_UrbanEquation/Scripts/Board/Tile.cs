using UnityEngine;

public class Tile : MonoBehaviour
{
    [Header("Tile Highlight")]
    [SerializeField]
    private GameObject highlight;

    public Vector2Int Coordinate { get; private set; }

    public TileData Data { get; private set; }

    public BuildingInstance Building { get; private set; }

    public bool IsOccupied => Building != null;

    public void Initialize(Vector2Int coordinate)
    {
        Initialize(coordinate, null);
    }

    public void Initialize(Vector2Int coordinate, TileData data)
    {
        Coordinate = coordinate;
        Data = data;

        SetHighlight(false);
    }

    public void SetBuilding(BuildingInstance building)
    {
        if (!TrySetBuilding(building))
            Debug.LogWarning("Cannot replace an occupied tile's building without clearing it.", this);
    }

    public bool TrySetBuilding(BuildingInstance building)
    {
        if (Building != null && building != null && Building != building) return false;
        Building = building;
        return true;
    }

    public bool CanAcceptBuilding(BuildingData building)
    {
        return !IsOccupied && Data != null && building != null && building.CanBuildOn(Data.TileType);
    }

    public void ClearBuilding()
    {
        BuildingInstance previous = Building;
        Building = null;
        if (previous == null) return;
        // Disable immediately so deferred Destroy cannot leave a clickable old building.
        previous.gameObject.SetActive(false);
        if (Application.isPlaying) Destroy(previous.gameObject);
        else DestroyImmediate(previous.gameObject);
    }

    public void SetHighlight(bool active)
    {
        if (highlight == null)
        {
            return;
        }

        highlight.SetActive(active);
    }
}
