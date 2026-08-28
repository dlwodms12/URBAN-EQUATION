using UnityEngine;

public class BuildingInstance : MonoBehaviour
{
    public BuildingData Data { get; private set; }

    public Vector2Int Coordinate { get; private set; }

    public void Initialize(
        BuildingData data,
        Vector2Int coordinate)
    {
        Data = data;
        Coordinate = coordinate;

        gameObject.name =
            $"{data.BuildingCode}_{data.BuildingName}";
    }
}