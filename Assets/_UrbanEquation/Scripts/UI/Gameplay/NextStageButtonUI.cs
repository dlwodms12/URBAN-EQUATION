using UnityEngine;
using UnityEngine.UI;

// Main game NEXT STAGE confirms a result; SceneFlowManager opens the clear screen.
public class NextStageButtonUI : MonoBehaviour
{
    [SerializeField] private StageManager stage;
    [SerializeField] private Button nextButton;
    private StageManager subscribedStage;
    private Button subscribedButton;

    public void Configure(StageManager manager, Button button)
    {
        Unsubscribe();
        stage = manager;
        nextButton = button;
        if (isActiveAndEnabled) Subscribe();
        Refresh();
    }

    private void OnEnable() { Subscribe(); Refresh(); }
    private void Start() { Subscribe(); Refresh(); }
    private void OnDisable() => Unsubscribe();
    private void OnDestroy() => Unsubscribe();

    private void Subscribe()
    {
        if (subscribedStage == stage && subscribedButton == nextButton) return;
        Unsubscribe();
        subscribedStage = stage;
        subscribedButton = nextButton;
        if (subscribedStage != null) subscribedStage.OnStateChanged += Refresh;
        if (subscribedButton != null) subscribedButton.onClick.AddListener(CompleteStage);
    }

    private void Unsubscribe()
    {
        if (subscribedStage != null) subscribedStage.OnStateChanged -= Refresh;
        if (subscribedButton != null) subscribedButton.onClick.RemoveListener(CompleteStage);
        subscribedStage = null;
        subscribedButton = null;
    }

    private void Refresh()
    {
        if (nextButton != null) nextButton.interactable = stage != null
            && stage.GameplayEnabled && stage.NextStageAvailable;
    }

    private void CompleteStage()
    {
        if (stage != null) stage.TryCompleteStage(out _, out _);
        Refresh();
    }
}
