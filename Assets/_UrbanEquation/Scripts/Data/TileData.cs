using UnityEngine;

[CreateAssetMenu(fileName = "TileData", menuName = "Urban Equation/Tile Data")]
public class TileData : ScriptableObject
{
    [SerializeField] private TileType tileType;
    [SerializeField] private string displayName;
    [SerializeField] private Tile tilePrefab;

    public TileType TileType => tileType;
    public string TileCode => $"T{(int)tileType + 1:00000}";
    public string DisplayName => displayName;
    public Tile TilePrefab => tilePrefab;
}
