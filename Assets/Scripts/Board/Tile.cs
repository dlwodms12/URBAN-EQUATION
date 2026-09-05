using UnityEngine;

public class Tile : MonoBehaviour
{
    [Header("Tile Highlight")]
    [SerializeField]
    private GameObject highlight;

    public Vector2Int Coordinate { get; private set; }

    public BuildingInstance Building { get; private set; }

    public bool IsOccupied => Building != null;

    public void Initialize(Vector2Int coordinate)
    {
        Coordinate = coordinate;

        SetHighlight(false);
    }

    public void SetBuilding(BuildingInstance building)
    {
        Building = building;
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