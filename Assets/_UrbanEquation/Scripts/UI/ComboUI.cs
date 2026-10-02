using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ComboUI : MonoBehaviour
{
    [SerializeField] private ComboManager comboManager;
    [SerializeField] private TMP_Text comboText;
    // Preserve the prototype's existing serialized duration; final visual timing is Phase 4.
    [SerializeField] private float displayDuration = 2f;
    private Coroutine displayCoroutine;
    private ComboManager subscribedManager;

    private void OnEnable() => Subscribe();
    private void Start()
    {
        Subscribe();
        if (comboManager != null && comboManager.CurrentPresentation != null)
            ShowCombo(comboManager.CurrentPresentation);
        else ResetLocalDisplay();
    }
    private void OnDisable() { Unsubscribe(); ResetLocalDisplay(); }
    private void OnDestroy() { Unsubscribe(); ResetLocalDisplay(); }

    private void Subscribe()
    {
        if (subscribedManager == comboManager) return;
        Unsubscribe();
        subscribedManager = comboManager;
        if (subscribedManager == null) return;
        subscribedManager.OnPresentationRequested += ShowCombo;
        subscribedManager.OnPresentationCleared += ResetLocalDisplay;
        if (subscribedManager.CurrentPresentation != null) ShowCombo(subscribedManager.CurrentPresentation);
    }

    private void Unsubscribe()
    {
        if (subscribedManager != null)
        {
            subscribedManager.OnPresentationRequested -= ShowCombo;
            subscribedManager.OnPresentationCleared -= ResetLocalDisplay;
        }
        subscribedManager = null;
    }

    private void ShowCombo(ComboResult result)
    {
        if (comboText == null || !isActiveAndEnabled) return;
        ResetLocalDisplay();
        comboText.text = FormatComboResult(result);
        comboText.gameObject.SetActive(true);
        displayCoroutine = StartCoroutine(CompleteAfterDelay(result));
    }

    private IEnumerator CompleteAfterDelay(ComboResult result)
    {
        yield return new WaitForSeconds(displayDuration);
        if (comboText != null) comboText.gameObject.SetActive(false);
        displayCoroutine = null;
        if (comboManager != null) comboManager.TryCompletePresentation(result);
    }

    public static string FormatComboResult(ComboResult result)
    {
        if (result == null) return string.Empty;
        var lines = new List<string>();
        lines.Add(string.IsNullOrEmpty(result.ComboName) ? $"COMBO {result.ComboCode}" : result.ComboName);
        lines.Add(string.IsNullOrEmpty(result.Description)
            ? $"{result.BuildingCodeA} + {result.BuildingCodeB}" : result.Description);
        foreach (ResourceAmount reward in result.Rewards)
            lines.Add($"{GetResourceName(reward.Resource)} {(reward.Amount > 0 ? "+" : "")}{reward.Amount}");
        return string.Join("\n", lines);
    }

    private static string GetResourceName(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Population: return "인구";
            case ResourceType.Jobs: return "일자리";
            case ResourceType.Money: return "자금";
            case ResourceType.Logistics: return "물류";
            case ResourceType.Tourism: return "관광";
            default: return type.ToString();
        }
    }

    private void ResetLocalDisplay()
    {
        if (displayCoroutine != null) { StopCoroutine(displayCoroutine); displayCoroutine = null; }
        if (comboText != null) comboText.gameObject.SetActive(false);
    }

    public void ResetUI()
    {
        ResetLocalDisplay();
        if (comboManager != null) comboManager.ClearPresentations();
    }
}

