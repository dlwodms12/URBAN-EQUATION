using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int Coordinate { get; private set; }

    public BuildingInstance Building { get; private set; }

    public bool IsOccupied => Building != null;

    public void Initialize(Vector2Int coordinate)
    {
        Coordinate = coordinate;
    }

    public void SetBuilding(BuildingInstance building)
    {
        Building = building;
    }
}