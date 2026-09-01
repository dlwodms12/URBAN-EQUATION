using UnityEngine;

public class BuildingInstance : MonoBehaviour
{
    public BuildingData Data { get; private set; }

    public Vector2Int Coordinate { get; private set; }

    private GameObject currentVisual;

    public void Initialize(
        BuildingData data,
        Vector2Int coordinate)
    {
        if (data == null)
        {
            Debug.LogError(
                "BuildingInstance.Initialize()에 " +
                "BuildingData가 전달되지 않았습니다."
            );

            return;
        }

        Data = data;
        Coordinate = coordinate;

        gameObject.name =
            $"{data.BuildingCode}_{data.BuildingName}";

        CreateVisual();
    }

    private void CreateVisual()
    {
        if (currentVisual != null)
        {
            Destroy(currentVisual);
        }

        GameObject visualPrefab =
            Data.VisualPrefab;

        if (visualPrefab == null)
        {
            Debug.LogWarning(
                $"건물 코드 {Data.BuildingCode}에 " +
                "연결된 Visual Prefab이 없습니다."
            );

            return;
        }

        currentVisual =
            Instantiate(
                visualPrefab,
                transform
            );

        currentVisual.transform.localPosition =
            Vector3.zero;

        currentVisual.transform.localRotation =
            Quaternion.Euler(0f, 180f, 0f);

        currentVisual.transform.localScale =
            Vector3.one;
    }
}