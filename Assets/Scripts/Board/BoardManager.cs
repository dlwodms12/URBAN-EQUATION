using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public const int BoardSize = 8;

    [SerializeField]
    private Tile tilePrefab;

    private Tile[,] tiles; // 2차원 배열 선언

    private void Awake()
    {
        CreateBoard();
    }

    private void CreateBoard()
    {
        tiles = new Tile[BoardSize, BoardSize];

        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                Tile tile = Instantiate(tilePrefab, transform);

                tile.transform.position = new Vector3(x, 0f, y);

                tile.Initialize(new Vector2Int(x, y));

                tiles[x, y] = tile;
            }
        }
    }

    public Tile GetTile(Vector2Int coordinate)
    {
        if (coordinate.x < 0 || coordinate.x >= BoardSize)
        {
            return null;
        }

        if (coordinate.y < 0 || coordinate.y >= BoardSize)
        {
            return null;
        }

        return tiles[coordinate.x, coordinate.y];
    }
}