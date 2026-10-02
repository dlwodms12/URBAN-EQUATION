using UnityEngine;
using UnityEngine.UI;

public class UndoButtonUI : MonoBehaviour
{
    [SerializeField] private TurnHistoryManager history;
    [SerializeField] private Button undoButton;
    private TurnHistoryManager subscribedHistory;
    private Button subscribedButton;

    public void Configure(TurnHistoryManager manager, Button button)
    {
        Unsubscribe();
        history = manager;
        undoButton = button;
        if (isActiveAndEnabled) Subscribe();
        Refresh();
    }

    private void OnEnable() { Subscribe(); Refresh(); }
    private void Start() { Subscribe(); Refresh(); }
    private void OnDisable() => Unsubscribe();
    private void OnDestroy() => Unsubscribe();

    private void Subscribe()
    {
        if (subscribedHistory == history && subscribedButton == undoButton) return;
        Unsubscribe();
        subscribedHistory = history;
        subscribedButton = undoButton;
        if (subscribedHistory != null) subscribedHistory.OnHistoryChanged += Refresh;
        if (subscribedButton != null) subscribedButton.onClick.AddListener(Undo);
    }

    private void Unsubscribe()
    {
        if (subscribedHistory != null) subscribedHistory.OnHistoryChanged -= Refresh;
        if (subscribedButton != null) subscribedButton.onClick.RemoveListener(Undo);
        subscribedHistory = null;
        subscribedButton = null;
    }

    private void Refresh()
    {
        if (undoButton != null) undoButton.interactable = history != null && history.CanUndo;
    }

    private void Undo()
    {
        if (history != null) history.TryUndo(out _);
        Refresh();
    }
}
