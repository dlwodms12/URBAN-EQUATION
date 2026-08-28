using TMPro;
using UnityEngine;

public class ResourceUI : MonoBehaviour
{
    [SerializeField]
    private ResourceManager resourceManager;

    [SerializeField]
    private TMP_Text populationText;

    [SerializeField]
    private TMP_Text jobsText;

    [SerializeField]
    private TMP_Text goodsText;

    [SerializeField]
    private TMP_Text logisticsText;

    private void Start()
    {
        resourceManager.OnResourceChanged += UpdateResource;

        UpdateAllResources();
    }

    private void OnDestroy()
    {
        if (resourceManager != null)
        {
            resourceManager.OnResourceChanged -= UpdateResource;
        }
    }

    private void UpdateResource(
        ResourceType resourceType,
        int value)
    {
        switch (resourceType)
        {
            case ResourceType.Population:
                populationText.text =
                    $"인구: {value}";
                break;

            case ResourceType.Jobs:
                jobsText.text =
                    $"일자리: {value}";
                break;

            case ResourceType.Goods:
                goodsText.text =
                    $"재화: {value}";
                break;

            case ResourceType.Logistics:
                logisticsText.text =
                    $"물류: {value}";
                break;
        }
    }

    private void UpdateAllResources()
    {
        populationText.text =
            $"인구: {resourceManager.GetResource(ResourceType.Population)}";

        jobsText.text =
            $"일자리: {resourceManager.GetResource(ResourceType.Jobs)}";

        goodsText.text =
            $"재화: {resourceManager.GetResource(ResourceType.Goods)}";

        logisticsText.text =
            $"물류: {resourceManager.GetResource(ResourceType.Logistics)}";
    }
}