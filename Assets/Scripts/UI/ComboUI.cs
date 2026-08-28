using System.Collections;
using TMPro;
using UnityEngine;

public class ComboUI : MonoBehaviour
{
    [SerializeField]
    private ComboManager comboManager;

    [SerializeField]
    private TMP_Text comboText;

    [SerializeField]
    private float displayDuration = 2f;

    private Coroutine displayCoroutine;

    private void Start()
    {
        comboManager.OnComboTriggered += ShowCombo;

        comboText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (comboManager != null)
        {
            comboManager.OnComboTriggered -= ShowCombo;
        }
    }

    private void ShowCombo(
        int comboCode,
        int buildingCodeA,
        int buildingCodeB,
        ResourceType rewardResource,
        int rewardAmount)
    {
        string amountText;

        if (rewardAmount > 0)
        {
            amountText = $"+{rewardAmount}";
        }
        else
        {
            amountText = rewardAmount.ToString();
        }

        comboText.text =
            $"COMBO {comboCode}\n" +
            $"{buildingCodeA} + {buildingCodeB}\n" +
            $"{GetResourceName(rewardResource)} {amountText}";

        comboText.gameObject.SetActive(true);

        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
        }

        displayCoroutine =
            StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(
            displayDuration
        );

        comboText.gameObject.SetActive(false);

        displayCoroutine = null;
    }

    private string GetResourceName(
        ResourceType resourceType)
    {
        switch (resourceType)
        {
            case ResourceType.Population:
                return "인구";

            case ResourceType.Jobs:
                return "일자리";

            case ResourceType.Goods:
                return "재화";

            case ResourceType.Logistics:
                return "물류";

            default:
                return resourceType.ToString();
        }
    }
}