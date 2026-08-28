using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildingSlotUI : MonoBehaviour
{
    [SerializeField]
    private BuildingPlacement buildingPlacement;

    [SerializeField]
    private BuildingDatabase buildingDatabase;

    [SerializeField]
    private int buildingCode;

    [SerializeField]
    private TMP_Text buildingNameText;

    [SerializeField]
    private TMP_Text buildingCodeText;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (button == null)
        {
            Debug.LogError(
                $"{gameObject.name}에 Button 컴포넌트가 없습니다."
            );

            return;
        }

        button.onClick.AddListener(
            SelectBuilding
        );
    }

    private void Start()
    {
        UpdateUI();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(
                SelectBuilding
            );
        }
    }

    private void SelectBuilding()
    {
        Debug.Log(
            $"클릭된 슬롯: {gameObject.name} / " +
            $"건물 코드: {buildingCode}"
        );

        if (buildingPlacement == null)
        {
            Debug.LogError(
                $"{gameObject.name}의 " +
                "BuildingPlacement가 연결되지 않았습니다."
            );

            return;
        }

        buildingPlacement.SelectBuilding(
            buildingCode
        );
    }

    private void UpdateUI()
    {
        if (buildingDatabase == null)
        {
            Debug.LogError(
                $"{gameObject.name}의 " +
                "BuildingDatabase가 연결되지 않았습니다."
            );

            return;
        }

        BuildingData building =
            buildingDatabase.GetBuilding(
                buildingCode
            );

        if (building == null)
        {
            Debug.LogError(
                $"{gameObject.name}에서 " +
                $"건물 코드를 찾을 수 없습니다: {buildingCode}"
            );

            return;
        }

        if (buildingNameText != null)
        {
            buildingNameText.text =
                building.BuildingName;
        }

        if (buildingCodeText != null)
        {
            buildingCodeText.text =
                building.BuildingCode.ToString();
        }
    }
}