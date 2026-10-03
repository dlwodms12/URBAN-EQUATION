using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Presentation only. Rewards have already been committed by GameSessionManager.
public class ComboPopupUI : MonoBehaviour
{
    [SerializeField] private TMP_Text comboNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text rewardsText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private ResourceStripUI rewardsView;
    [SerializeField, Min(0.1f)] private float displayDuration = 2f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.25f;
    [SerializeField] private Vector2 screenOffset = new Vector2(0, 80);
    private ComboManager manager;
    private ComboManager subscribed;
    private Camera boardCamera;
    private float elapsed;
    private bool persistent;
    public ComboResult CurrentResult { get; private set; }
    public bool IsPersistent => persistent && CurrentResult != null;

    public bool TryConfigureTiming(float duration, float fade)
    {
        if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0
            || float.IsNaN(fade) || float.IsInfinity(fade) || fade < 0 || fade > duration) return false;
        displayDuration = duration; fadeDuration = fade; return true;
    }
    public void ShowPersistent(ComboResult result, Camera camera)
    {
        Unsubscribe(); Hide(); manager = null; boardCamera = camera;
        if (result == null) return;
        persistent = true; Show(result);
    }
    public void DismissPersistent() { if (persistent) Hide(); }

    public void Bind(ComboManager combos, Camera camera)
    {
        Unsubscribe(); Hide(); manager = combos; boardCamera = camera;
        if (isActiveAndEnabled) Subscribe();
    }

    private void OnEnable() => Subscribe();
    private void OnDisable() { Unsubscribe(); Hide(); }
    private void OnDestroy() => Unsubscribe();
    private void Update() => Tick(Time.unscaledDeltaTime);

    private void Subscribe()
    {
        if (subscribed == manager) return;
        Unsubscribe(); subscribed = manager;
        if (subscribed == null) return;
        subscribed.OnPresentationRequested += Show;
        subscribed.OnPresentationCleared += Hide;
        if (subscribed.CurrentPresentation != null) Show(subscribed.CurrentPresentation);
    }
    private void Unsubscribe()
    {
        if (subscribed != null)
        { subscribed.OnPresentationRequested -= Show; subscribed.OnPresentationCleared -= Hide; }
        subscribed = null;
    }
    private void Show(ComboResult result)
    {
        if (!isActiveAndEnabled || result == null) return;
        CurrentResult = result; elapsed = 0;
        if (comboNameText != null) comboNameText.text = result.ComboName;
        if (descriptionText != null) descriptionText.text = result.Description;
        if (rewardsText != null) rewardsText.text = FormatRewards(result.Rewards);
        if (rewardsView != null) rewardsView.Show(result.Rewards, true);
        if (canvasGroup != null)
        { canvasGroup.alpha = 1; canvasGroup.interactable = false; canvasGroup.blocksRaycasts = false; }
        Position();
    }
    private void Hide()
    {
        CurrentResult = null; elapsed = 0; persistent = false;
        if (canvasGroup != null) canvasGroup.alpha = 0;
        if (comboNameText != null) comboNameText.text = string.Empty;
        if (descriptionText != null) descriptionText.text = string.Empty;
        if (rewardsText != null) rewardsText.text = string.Empty;
        if (rewardsView != null) rewardsView.Clear();
    }

    public void Tick(float delta)
    {
        if (!isActiveAndEnabled || float.IsNaN(delta) || float.IsInfinity(delta) || delta < 0) return;
        if (persistent) { if (canvasGroup != null) canvasGroup.alpha = 1; Position(); return; }
        ComboResult current = manager == null ? null : manager.CurrentPresentation;
        if (current != CurrentResult) { if (current == null) Hide(); else Show(current); }
        if (CurrentResult == null) return;
        elapsed += delta;
        float duration = Mathf.Max(0.1f, displayDuration);
        float fade = Mathf.Clamp(fadeDuration, 0, duration);
        if (elapsed >= duration) { TryCompleteCurrent(); return; }
        if (canvasGroup != null) canvasGroup.alpha = fade <= 0 ? 1 : Mathf.Clamp01((duration - elapsed) / fade);
        Position();
    }

    public bool TryCompleteCurrent()
    {
        if (!isActiveAndEnabled || persistent || manager == null || CurrentResult == null) return false;
        ComboResult expected = CurrentResult;
        Hide();
        bool completed = manager.TryCompletePresentation(expected);
        if (manager.CurrentPresentation != null && CurrentResult == null) Show(manager.CurrentPresentation);
        return completed;
    }

    private void Position()
    {
        if (CurrentResult == null || boardCamera == null || !(transform.parent is RectTransform parent)) return;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        Vector3 point = boardCamera.WorldToScreenPoint(CurrentResult.PresentationPosition);
        if (point.z <= 0) { if (canvasGroup != null) canvasGroup.alpha = 0; return; }
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, (Vector2)point + screenOffset,
            uiCamera, out Vector2 position)) ((RectTransform)transform).anchoredPosition = position;
    }

    public static string FormatRewards(IReadOnlyList<ResourceAmount> rewards)
    {
        if (rewards == null) return string.Empty;
        var lines = new List<string>();
        foreach (ResourceAmount reward in rewards)
        {
            string name;
            switch (reward.Resource)
            {
                case ResourceType.Population: name = "인구"; break;
                case ResourceType.Jobs: name = "일자리"; break;
                case ResourceType.Money: name = "자금"; break;
                case ResourceType.Logistics: name = "물류"; break;
                case ResourceType.Tourism: name = "관광"; break;
                default: name = reward.Resource.ToString(); break;
            }
            string color = reward.Amount < 0 ? "F04D55" : "00B858";
            lines.Add("<color=#" + color + ">" + name + " " + (reward.Amount > 0 ? "+" : "") + reward.Amount + "</color>");
        }
        return string.Join("\n", lines);
    }
}
