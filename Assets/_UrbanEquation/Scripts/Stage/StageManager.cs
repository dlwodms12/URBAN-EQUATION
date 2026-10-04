using System;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private StageData stageData;
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private BuildingHandManager buildingHand;
    private bool isCleared;
    private bool configured;
    private bool changing;
    private GameSessionManager ownerSession;
    private StageProgressState progress = StageProgressState.Empty;

    public ResourceManager Resources => resourceManager;
    public BoardManager Board => boardManager;
    public BuildingHandManager Hand => buildingHand;
    public StageData CurrentStage => configured ? stageData : null;
    public bool IsConfigured => configured;
    public bool IsBusy => changing;
    public bool IsCleared => isCleared;
    public int Rank => progress.Rank;
    public bool NextStageAvailable => progress.NextStageAvailable;
    public bool GameplayEnabled => ownerSession == null || ownerSession.GameplayEnabled;
    public IReadOnlyList<bool> GoalStates => progress.GoalStates;
    public StageResult CurrentResult { get; private set; }
    public long ConfigurationVersion { get; private set; }
    public event Action OnStateChanged;
    public event Action OnConfigurationChanged;
    public event Action OnStageCleared;
    public event Action<StageResult> OnStageCompleted;
    internal void NotifyGameplayStateChanged() => OnStateChanged?.Invoke();

    public StageProgressState CaptureProgressState() => new StageProgressState(
        isCleared, progress.GoalStates, progress.Rank, progress.NextStageAvailable, progress.ComboReviewed, progress.ComplaintReviewed);

    public bool TryConfigure(StageData stage, BoardManager board, ResourceManager resources,
        BuildingHandManager hand, out string error)
    {
        error = null;
        if (changing || (ownerSession != null && ownerSession.IsBusy))
        { error = "Cannot configure stage during a transaction."; return false; }
        if (!TryEvaluateContext(stage, board, resources, hand, out StageProgressState initial, out error, null, false, false))
            return false;
        if (ownerSession != null && (ownerSession.Board != board || ownerSession.Resources != resources
            || ownerSession.Hand != hand))
        { error = "Stage and session must use the same managers."; return false; }
        stageData = stage;
        boardManager = board;
        resourceManager = resources;
        buildingHand = hand;
        configured = true;
        progress = initial;
        isCleared = false;
        CurrentResult = null;
        ConfigurationVersion++;
        OnConfigurationChanged?.Invoke();
        OnStateChanged?.Invoke();
        return true;
    }

    internal bool CanUseWith(GameSessionManager session, out string error)
    {
        error = null;
        if (session == null || changing || (ownerSession != null && ownerSession != session)
            || resourceManager != session.Resources)
        { error = "Stage is busy or belongs to a different session/resources."; return false; }
        if (!configured)
        {
            if (stageData == null) return true; // Existing Prototype compatibility.
            error = "Stage data has not been configured.";
            return false;
        }
        if (boardManager != session.Board || buildingHand != session.Hand)
        { error = "Stage and session board/hand must match."; return false; }
        return TryEvaluateContext(stageData, boardManager, resourceManager, buildingHand, out _, out error);
    }

    internal void BindSession(GameSessionManager session) => ownerSession = session;
    internal void UnbindSession(GameSessionManager session)
    {
        if (ownerSession == session) ownerSession = null;
    }

    public bool TryRefreshGoals(out string error)
    {
        error = null;
        if (!configured || changing || (ownerSession != null && ownerSession.IsBusy))
        { error = "Goals require an idle, configured stage."; return false; }
        if (isCleared) return true;
        if (!TryPrepareGoalEvaluation(out Action apply, out Action publish, out error)) return false;
        apply();
        publish();
        return true;
    }

    internal bool TryPrepareGoalEvaluation(out Action apply, out Action publish, out string error)
    {
        return TryPrepareBuildGoalEvaluation(new ComboResult[0], out apply, out publish, out error);
    }

    internal bool TryPrepareBuildGoalEvaluation(IReadOnlyList<ComboResult> pending,
        out Action apply, out Action publish, out string error)
    {
        apply = null;
        publish = null;
        if (!configured || isCleared)
        { error = "Cannot evaluate this stage."; return false; }
        if (!TryEvaluateContext(stageData, boardManager, resourceManager, buildingHand,
            out StageProgressState next, out error, pending)) return false;
        if (TryPrepareProgressRestore(next, out apply, out publish)) return true;
        error = "Evaluated progress is invalid.";
        return false;
    }

    public bool TryCompleteStage(out StageResult result, out string error)
    {
        result = null;
        error = null;
        if (!configured || changing || (ownerSession != null && ownerSession.IsBusy))
        { error = "Completion requires an idle, configured stage."; return false; }
        if (!GameplayEnabled)
        { error = "Completion is disabled for this screen."; return false; }
        if (isCleared) { result = CurrentResult; return result != null; }
        if (!TryEvaluateContext(stageData, boardManager, resourceManager, buildingHand,
            out StageProgressState latest, out error)) return false;
        changing = true;
        try
        {
            if (!latest.NextStageAvailable)
            {
                progress = latest;
                OnStateChanged?.Invoke();
                error = "The required goal is not achieved.";
                return false;
            }
            progress = new StageProgressState(true, latest.GoalStates, latest.Rank, false, latest.ComboReviewed, latest.ComplaintReviewed);
            isCleared = true;
            CurrentResult = new StageResult(stageData, progress, resourceManager.CaptureResourceState());
            result = CurrentResult;
            OnStateChanged?.Invoke();
            OnStageCleared?.Invoke();
            OnStageCompleted?.Invoke(result);
            return true;
        }
        finally { changing = false; }
    }

    public bool TryApplyProgressState(StageProgressState state)
    {
        if (configured && (changing || (ownerSession != null && ownerSession.IsBusy))) return false;
        if (!TryPrepareProgressRestore(state, out Action apply, out Action publish)) return false;
        apply();
        publish();
        return true;
    }

    internal bool TryPrepareProgressRestore(StageProgressState state, out Action apply, out Action publish)
    {
        apply = null;
        publish = null;
        if (state == null || (configured && !IsValidProgress(state))) return false;
        bool becameCleared = !isCleared && state.IsCleared;
        bool changed = !SameProgress(progress, state) || isCleared != state.IsCleared;
        apply = () =>
        {
            progress = state;
            isCleared = state.IsCleared;
            CurrentResult = configured && isCleared
                ? new StageResult(stageData, state, resourceManager.CaptureResourceState()) : null;
        };
        publish = () =>
        {
            if (changed || !configured) OnStateChanged?.Invoke();
            if (becameCleared) OnStageCleared?.Invoke();
            // Restoring a snapshot never publishes a new completion result.
        };
        return true;
    }

    private static bool IsValidProgress(StageProgressState state)
    {
        if (state.GoalStates.Count != 3) return false;
        bool required = state.GoalStates[0];
        int rank = required ? 1 + (state.GoalStates[1] ? 1 : 0) + (state.GoalStates[2] ? 1 : 0) : 0;
        return state.Rank == rank && (!state.IsCleared || required)
            && state.NextStageAvailable == (required && !state.IsCleared);
    }

    private static bool SameProgress(StageProgressState a, StageProgressState b)
    {
        if (a.IsCleared != b.IsCleared || a.Rank != b.Rank || a.NextStageAvailable != b.NextStageAvailable
            || a.GoalStates.Count != b.GoalStates.Count || a.ComboReviewed != b.ComboReviewed
            || a.ComplaintReviewed != b.ComplaintReviewed) return false;
        for (int i = 0; i < a.GoalStates.Count; i++)
            if (a.GoalStates[i] != b.GoalStates[i]) return false;
        return true;
    }

    private bool TryEvaluateContext(StageData stage, BoardManager board, ResourceManager resources,
        BuildingHandManager hand, out StageProgressState evaluated, out string error,
        IReadOnlyList<ComboResult> pending = null, bool? comboReviewed = null, bool? complaintReviewed = null)
    {
        evaluated = null;
        error = null;
        if (stage == null || board == null || !board.HasBoard || board.CurrentStage != stage
            || board.Width != stage.Width || board.Height != stage.Height || resources == null
            || hand == null || !hand.IsInitialized || hand.Resources != resources)
        { error = "Goals require the current initialized board, resources and hand."; return false; }
        if (!board.TryCaptureBuildings(out BuildingStateSnapshot[] buildings, out error)) return false;
        var interactions = new List<ComboResult>();
        if (ownerSession != null && ownerSession.Combos != null)
            interactions.AddRange(ownerSession.Combos.CaptureResults());
        if (pending != null) interactions.AddRange(pending);
        return StageGoalEvaluator.TryEvaluateStageWithInteractions(stage, buildings, resources.CaptureResourceState(),
            hand.Cards.Count, interactions, comboReviewed ?? progress.ComboReviewed,
            complaintReviewed ?? progress.ComplaintReviewed, out evaluated, out error);
    }

    // Only a manual two-building lookup can satisfy this tutorial goal.
    public bool TryConfirmInteraction(ComboResult result, out string error)
    {
        error = null;
        if (!configured || isCleared || changing || ownerSession == null || ownerSession.IsBusy
            || !GameplayEnabled || result == null || ownerSession.Combos == null
            || !ownerSession.Combos.TryGetAppliedCombo(result.SourceCoordinate, result.AdjacentCoordinate,
                out ComboResult current) || current != result)
        { error = "Review requires a current interaction in an idle playing stage."; return false; }
        bool reviewedCombo = progress.ComboReviewed || !result.IsComplaint;
        bool reviewedComplaint = progress.ComplaintReviewed || result.IsComplaint;
        if (!TryEvaluateContext(stageData, boardManager, resourceManager, buildingHand,
            out StageProgressState next, out error, null, reviewedCombo, reviewedComplaint)) return false;
        if (!TryPrepareProgressRestore(next, out Action apply, out Action publish))
        { error = "Reviewed progress is invalid."; return false; }
        changing = true;
        try
        {
            apply();
            if (ownerSession.History != null) ownerSession.History.UpdateCurrentProgress();
            publish();
            return true;
        }
        finally { changing = false; }
    }

    private void Start()
    {
        if (stageData != null)
        {
            if (!configured && !TryConfigure(stageData, boardManager, resourceManager, buildingHand, out string error))
                Debug.LogWarning(error, this);
            return;
        }
        isCleared = false;
        progress = StageProgressState.Empty;
        Debug.Log("Stage 1 시작");
        Debug.Log("클리어 목표: 인구 수 4");
    }

    public void CheckStageClear()
    {
        if (configured) { TryRefreshGoals(out _); return; }
        if (isCleared || resourceManager == null || resourceManager.GetResource(ResourceType.Population) < 4) return;
        isCleared = true;
        OnStateChanged?.Invoke();
        Debug.Log("Stage 1 Clear!");
        OnStageCleared?.Invoke();
    }

    public void ResetStage()
    {
        if (changing || (ownerSession != null && ownerSession.IsBusy)) return;
        StageProgressState reset = StageProgressState.Empty;
        if (configured && !TryEvaluateContext(stageData, boardManager, resourceManager, buildingHand,
            out reset, out string error, null, false, false))
        { Debug.LogWarning(error, this); return; }
        isCleared = false;
        progress = reset;
        CurrentResult = null;
        ConfigurationVersion++;
        OnConfigurationChanged?.Invoke();
        OnStateChanged?.Invoke();
        if (!configured)
        {
            Debug.Log("Stage 1 재시작");
            Debug.Log("클리어 목표: 인구 수 4");
        }
    }
}
