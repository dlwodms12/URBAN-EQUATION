using System;
using System.IO;
using UnityEngine;

// Configure/load explicitly from game flow. Adding the component never touches player files.
public class SaveManager : MonoBehaviour
{
    private ProgressFileStore store;
    private StageManager stage;
    private StageManager subscribedStage;
    private bool busy;
    private bool loadBlocked;
    public ProgressSaveData Progress { get; private set; }
    public StageResult PendingResult { get; private set; }
    public bool IsConfigured => store != null;
    public bool IsBusy => busy;
    public bool IsLoaded { get; private set; }
    public bool HasProgress { get; private set; }
    public bool CanContinue => IsLoaded && !loadBlocked && HasProgress;
    public bool CanPlay => IsConfigured;
    public bool RequiresNewGameConfirmation => HasProgress || loadBlocked || (IsConfigured && !IsLoaded);
    public string FilePath => store == null ? null : store.FilePath;
    public string LastError { get; private set; }
    public event Action OnProgressChanged;
    public event Action<string> OnSaveFailed;

    public bool TryConfigureDefault(int stageCount, out string error) =>
        TryConfigure(stageCount, Path.Combine(Application.persistentDataPath, "URBAN-EQUATION", "progress.json"), out error);

    public bool TryConfigure(int stageCount, string filePath, out string error)
    {
        error = null;
        if (busy) { error = "Progress is busy."; return false; }
        if (!ProgressSaveData.TryCreate(stageCount, out ProgressSaveData fresh, out error)) return false;
        if (stage != null && stage.CurrentStage != null && stage.CurrentStage.StageNumber > stageCount)
        { error = "Bound stage is outside this progress configuration."; return false; }
        ProgressFileStore next;
        try { next = new ProgressFileStore(filePath); }
        catch (Exception exception) when (ProgressFileStore.IsStorageException(exception))
        { error = exception.Message; return false; }
        store = next;
        Progress = fresh;
        IsLoaded = false;
        HasProgress = false;
        loadBlocked = false;
        PendingResult = null;
        LastError = null;
        return true;
    }

    public bool TryLoad(out string error)
    {
        if (PendingResult != null)
        { error = "Save the pending result or explicitly start a new game before loading."; return false; }
        if (!Begin(out error)) return false;
        try
        {
            if (!store.TryRead(out string json, out bool exists, out error))
            { loadBlocked = true; return Fail(error); }
            ProgressSaveData next;
            if (exists)
            {
                if (!ProgressSaveCodec.TryDeserialize(json, Progress.StageCount, out next, out error))
                { loadBlocked = true; return Fail(error); }
            }
            else ProgressSaveData.TryCreate(Progress.StageCount, out next, out _);
            Progress = next;
            IsLoaded = true;
            HasProgress = exists;
            loadBlocked = false;
            LastError = null;
            PendingResult = null;
            OnProgressChanged?.Invoke();
            return true;
        }
        finally { busy = false; }
    }

    // Caller obtains the existing-progress confirmation before invoking this command.
    public bool TryStartNewGame(out string error)
    {
        if (!Begin(out error)) return false;
        try
        {
            ProgressSaveData.TryCreate(Progress.StageCount, out ProgressSaveData fresh, out _);
            if (!Write(fresh, out error)) return Fail(error);
            Commit(fresh);
            PendingResult = null;
            OnProgressChanged?.Invoke();
            return true;
        }
        finally { busy = false; }
    }

    public bool TryRecordClear(int stageNumber, int rank, out string error)
    {
        error = null;
        if (!Begin(out error)) return false;
        try
        {
            if (!IsLoaded || loadBlocked)
            { error = "Load valid progress or start a new game first."; return Fail(error); }
            if (!Progress.TryRecordClear(stageNumber, rank, out ProgressSaveData next, out error)) return Fail(error);
            if (HasProgress && SameProgress(Progress, next))
            {
                LastError = null;
                ClearSatisfiedPendingResult();
                return true;
            }
            if (!Write(next, out error)) return Fail(error);
            Commit(next);
            ClearSatisfiedPendingResult();
            OnProgressChanged?.Invoke();
            return true;
        }
        finally { busy = false; }
    }

    public bool TryRecordStageResult(StageResult result, out string error)
    {
        error = null;
        if (result == null || result.GoalStates.Count != 3 || !result.GoalStates[0]
            || result.Rank != 1 + (result.GoalStates[1] ? 1 : 0) + (result.GoalStates[2] ? 1 : 0))
        { error = "A completed result requires the mandatory goal and matching rank."; return false; }
        if (!TryRecordClear(result.StageNumber, result.Rank, out error)) return false;
        if (PendingResult == result) PendingResult = null;
        return true;
    }

    public bool TrySavePendingResult(out string error) => TryRecordStageResult(PendingResult, out error);

    public bool TryBindStage(StageManager manager, out string error)
    {
        error = null;
        if (busy || !IsLoaded || loadBlocked || manager == null || !manager.IsConfigured
            || !Progress.IsStageUnlocked(manager.CurrentStage.StageNumber))
        { error = "Save binding requires valid progress and an initialized, unlocked stage."; return false; }
        Unsubscribe();
        stage = manager;
        if (isActiveAndEnabled) Subscribe();
        return true;
    }

    private bool Begin(out string error)
    {
        error = null;
        if (!IsConfigured || busy) { error = "Configure an idle save manager first."; return false; }
        busy = true;
        return true;
    }
    private bool Write(ProgressSaveData data, out string error)
    {
        if (!ProgressSaveCodec.TrySerialize(data, out string json, out error)) return false;
        return store.TryWrite(json, out error);
    }
    private void Commit(ProgressSaveData next)
    {
        Progress = next;
        HasProgress = true;
        IsLoaded = true;
        loadBlocked = false;
        LastError = null;
    }
    private void ClearSatisfiedPendingResult()
    {
        if (PendingResult != null && Progress.GetBestRank(PendingResult.StageNumber) >= PendingResult.Rank)
            PendingResult = null;
    }
    private bool Fail(string error)
    {
        LastError = error;
        OnSaveFailed?.Invoke(error);
        return false;
    }
    private static bool SameProgress(ProgressSaveData a, ProgressSaveData b)
    {
        if (a.StageCount != b.StageCount || a.HighestUnlockedStage != b.HighestUnlockedStage) return false;
        for (int i = 0; i < a.StageCount; i++)
            if (a.BestRanks[i] != b.BestRanks[i]) return false;
        return true;
    }
    private void OnEnable() => Subscribe();
    private void OnDisable() => Unsubscribe();
    private void OnDestroy() => Unsubscribe();
    private void Subscribe()
    {
        if (subscribedStage == stage) return;
        Unsubscribe();
        subscribedStage = stage;
        if (subscribedStage != null) subscribedStage.OnStageCompleted += SaveCompleted;
    }
    private void Unsubscribe()
    {
        if (subscribedStage != null) subscribedStage.OnStageCompleted -= SaveCompleted;
        subscribedStage = null;
    }
    private void SaveCompleted(StageResult result)
    {
        PendingResult = result;
        TryRecordStageResult(result, out _);
    }
}
