using System;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Header("Manager References")]
    [SerializeField]
    private ResourceManager resourceManager;

    private bool isCleared;
    private StageProgressState progress = StageProgressState.Empty;
    public ResourceManager Resources => resourceManager;
    public event Action OnStateChanged;

    public StageProgressState CaptureProgressState() => new StageProgressState(
        isCleared, progress.GoalStates, progress.Rank, progress.NextStageAvailable);

    public bool TryApplyProgressState(StageProgressState state)
    {
        if (!TryPrepareProgressRestore(state, out Action apply, out Action publish)) return false;
        apply();
        publish();
        return true;
    }

    internal bool TryPrepareProgressRestore(StageProgressState state, out Action apply, out Action publish)
    {
        apply = null;
        publish = null;
        if (state == null) return false;
        bool becameCleared = !isCleared && state.IsCleared;
        apply = () => { progress = state; isCleared = state.IsCleared; };
        publish = () =>
        {
            OnStateChanged?.Invoke();
            if (becameCleared) OnStageCleared?.Invoke();
        };
        return true;
    }

    public bool IsCleared
    {
        get { return isCleared; }
    }

    public event Action OnStageCleared;

    private void Start()
    {
        isCleared = false;
        progress = StageProgressState.Empty;

        Debug.Log("Stage 1 시작");
        Debug.Log("클리어 목표: 인구 수 4");
    }

    public void CheckStageClear()
    {
        if (isCleared)
        {
            return;
        }

        int population =
            resourceManager.GetResource(
                ResourceType.Population
            );

        if (population < 4)
        {
            return;
        }

        ClearStage();
    }

    private void ClearStage()
    {
        isCleared = true;
        OnStateChanged?.Invoke();

        Debug.Log("Stage 1 Clear!");

        OnStageCleared?.Invoke();
    }

    public void ResetStage()
    {
        isCleared = false;
        progress = StageProgressState.Empty;
        OnStateChanged?.Invoke();

        Debug.Log("Stage 1 재시작");
        Debug.Log("클리어 목표: 인구 수 4");
    }
}
