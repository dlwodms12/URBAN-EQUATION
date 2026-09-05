using UnityEngine;

public class ResetManager : MonoBehaviour
{
    [Header("Manager References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private BuildingPlacement buildingPlacement;
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private StageManager stageManager;

    [Header("UI References")]
    [SerializeField] private ClearUI clearUI;
    [SerializeField] private ComboUI comboUI;

    public void ResetGame()
    {
        if (boardManager != null)
        {
            boardManager.ResetBoard();
        }

        if (buildingPlacement != null)
        {
            buildingPlacement.ResetBuildings();
        }

        if (resourceManager != null)
        {
            resourceManager.ResetResources();
        }

        if (stageManager != null)
        {
            stageManager.ResetStage();
        }

        if (clearUI != null)
        {
            clearUI.ResetUI();
        }

        if (comboUI != null)
        {
            comboUI.ResetUI();
        }

        Debug.Log("게임이 초기 상태로 리셋되었습니다.");
    }
}