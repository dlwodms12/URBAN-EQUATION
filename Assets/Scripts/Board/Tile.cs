using UnityEngine;


// 타일의 위치를 저장
public class Tile : MonoBehaviour
{
    public Vector2Int Coordinate { get; private set; }

    public void Initialize(Vector2Int coordinate)
    {
        Coordinate = coordinate;
    }
}