using System;
using System.Collections.Generic;
using UnityEngine;

// Owns commands/screens, not scene assets. State changes are published after input is gated.
public class SceneFlowManager : MonoBehaviour
{
    private StageCatalog catalog;
    private SaveManager saves;
    private GameSessionManager session;
    private ComboDatabase database;
    private StageManager subscribedStage;
    private bool busy;
    private bool resultSaved;
    private delegate bool Command(out string error);

    public GameFlowState State { get; private set; } = GameFlowState.Unconfigured;
    public GameFlowScene Scene => IsLobbyState(State) ? GameFlowScene.Lobby : GameFlowScene.Game;
    public int StageCount => catalog == null ? 0 : catalog.Count;
    public int SelectedStageNumber { get; private set; }
    public StageData SelectedStage => catalog != null
        && catalog.TryGetStage(SelectedStageNumber, out StageData stage) ? stage : null;
    public StageResult Result { get; private set; }
    public bool CanContinue => State == GameFlowState.Lobby && saves != null && saves.CanContinue;
    public bool CanNextStage => State == GameFlowState.StageClear && resultSaved
        && saves.PendingResult == null && SelectedStageNumber < StageCount
        && saves.Progress.IsStageUnlocked(SelectedStageNumber + 1);
    public bool CanLeaveClear => State == GameFlowState.StageClear && resultSaved && saves.PendingResult == null;
    public bool QuitRequested { get; private set; }
    public string LastError { get; private set; }
    public string SaveError { get; private set; }
    public event Action OnStateChanged;
    public event Action<GameFlowScene> OnSceneRequested;
    public event Action OnQuitRequested;

    public bool TryConfigure(StageCatalog stages, SaveManager saveManager,
        GameSessionManager gameSession, ComboDatabase combos, out string error)
    {
        error = null;
        if (!isActiveAndEnabled || busy || (State != GameFlowState.Unconfigured && State != GameFlowState.Lobby
            && State != GameFlowState.StageLoadFailed) || (session != null && session.IsBusy)
            || gameSession == null || gameSession.IsBusy || stages == null || combos == null
            || saveManager == null || saveManager.IsBusy || !saveManager.IsConfigured || saveManager.PendingResult != null
            || saveManager.Progress.StageCount != stages.Count)
        { error = "Flow requires an active, idle session, stage catalog, combos and matching configured saves."; return false; }
        var errors = new List<string>();
        stages.Validate(errors);
        combos.Validate(errors);
        if (errors.Count > 0) { error = string.Join("; ", errors); return false; }
        if (!stages.TryGetStage(1, out StageData first)
            || !StageSessionInitializer.TryValidate(gameSession, first, combos, out error)) return false;
        busy = true;
        try
        {
            if (session != null && !session.TrySetGameplayEnabled(false, out error)) return false;
            Unsubscribe();
            catalog = stages;
            saves = saveManager;
            session = gameSession;
            database = combos;
            Result = null;
            SaveError = null;
            SelectedStageNumber = 0;
            resultSaved = false;
            QuitRequested = false;
            // A corrupt/unsupported file leaves Lobby usable for an explicitly confirmed new game.
            if (!saves.IsLoaded) saves.TryLoad(out _);
            LastError = saves.LastError;
            Subscribe();
            ChangeState(GameFlowState.Lobby);
            return true;
        }
        finally { busy = false; }
    }

    public bool IsStageUnlocked(int number) => catalog != null && catalog.TryGetStage(number, out _)
        && saves != null && saves.CanContinue && saves.Progress.IsStageUnlocked(number);
    public int GetBestRank(int number) => saves == null || !IsStageUnlocked(number)
        ? 0 : saves.Progress.GetBestRank(number);

    public bool TryPlay(out string error) => Execute(Play, out error);
    public bool TryConfirmNewGame(out string error) => Execute(ConfirmNewGame, out error);
    public bool TryContinue(out string error) => Execute(Continue, out error);
    public bool TrySelectStage(int number, out string error) =>
        Execute((out string problem) => SelectStage(number, out problem), out error);
    public bool TryDismissIntro(out string error) => Execute(DismissIntro, out error);
    public bool TryCompleteStage(out string error) => Execute(CompleteStage, out error);
    public bool TryRequestPause(out string error) => Execute(RequestPause, out error);
    public bool TryConfirmPause(out string error) => Execute(ConfirmPause, out error);
    public bool TryCancel(out string error) => Execute(Cancel, out error);
    public bool TryHandleEscape(out string error) => Execute(Escape, out error);
    public bool TryRetry(out string error) => Execute(Retry, out error);
    public bool TryNextStage(out string error) => Execute(NextStage, out error);
    public bool TryRetrySave(out string error) => Execute(RetrySave, out error);
    public bool TryReturnToStageSelect(out string error) => Execute(ReturnToSelect, out error);
    public bool TryRequestExit(out string error) => Execute(RequestExit, out error);
    public bool TryConfirmExit(out string error) => Execute(ConfirmExit, out error);

    private bool Execute(Command command, out string error)
    {
        error = null;
        if (!isActiveAndEnabled || busy || QuitRequested || catalog == null || session == null
            || session.IsBusy || session.Combos == null || session.Combos.IsResolving
            || session.Stage == null || session.Stage.IsBusy || saves.IsBusy)
        { error = "Flow is unconfigured, disabled or a transaction is in progress."; return false; }
        busy = true;
        try
        {
            LastError = null;
            bool success = command(out error);
            LastError = success ? null : error;
            if (!success) OnStateChanged?.Invoke();
            return success;
        }
        finally { busy = false; }
    }

    private bool Require(GameFlowState expected, out string error)
    {
        error = State == expected ? null : "This command is not available on the current screen.";
        return error == null;
    }

    private bool Play(out string error)
    {
        if (!Require(GameFlowState.Lobby, out error)) return false;
        if (saves.RequiresNewGameConfirmation) { ChangeState(GameFlowState.NewGameConfirmation); return true; }
        return StartNewGame(out error);
    }
    private bool ConfirmNewGame(out string error)
    {
        return Require(GameFlowState.NewGameConfirmation, out error) && StartNewGame(out error);
    }
    private bool StartNewGame(out string error)
    {
        if (!catalog.TryGetStage(1, out StageData first)) { error = "Stage 1 is missing."; return false; }
        // Do not erase existing progress because of a missing prefab/invalid stage definition.
        return StageSessionInitializer.TryValidate(session, first, database, out error)
            && saves.TryStartNewGame(out error) && EnterStage(1, out error);
    }
    private bool Continue(out string error)
    {
        error = null;
        if ((State != GameFlowState.Lobby && State != GameFlowState.NewGameConfirmation) || !saves.CanContinue)
        { error = "Continue requires valid saved progress on the Lobby screen."; return false; }
        ChangeState(GameFlowState.StageSelect);
        return true;
    }
    private bool SelectStage(int number, out string error)
    {
        if (!Require(GameFlowState.StageSelect, out error)) return false;
        if (!IsStageUnlocked(number)) { error = "The requested stage is locked or missing."; return false; }
        return EnterStage(number, out error);
    }
    private bool EnterStage(int number, out string error)
    {
        if (!catalog.TryGetStage(number, out StageData stage)) { error = "The requested stage is missing."; return false; }
        if (!StageSessionInitializer.TryValidate(session, stage, database, out error)) return false;
        if (!StageSessionInitializer.TryInitialize(session, stage, database, out error)
            || !saves.TryBindStage(session.Stage, out error))
        {
            SelectedStageNumber = number;
            ChangeState(GameFlowState.StageLoadFailed);
            return false;
        }
        SelectedStageNumber = number;
        Result = null;
        SaveError = null;
        resultSaved = false;
        // Subscribe after the save handler; the explicit write also covers an inactive SaveManager.
        Unsubscribe();
        Subscribe();
        ChangeState(GameFlowState.StageIntro);
        return true;
    }
    private bool DismissIntro(out string error)
    {
        if (!Require(GameFlowState.StageIntro, out error)) return false;
        ChangeState(GameFlowState.Playing);
        return true;
    }
    private bool CompleteStage(out string error)
    {
        return Require(GameFlowState.Playing, out error) && session.Stage.TryCompleteStage(out _, out error);
    }
    private void HandleStageCompleted(StageResult result)
    {
        if (State != GameFlowState.Playing || result == null || result.StageNumber != SelectedStageNumber) return;
        bool previousBusy = busy;
        busy = true;
        try
        {
            Result = result;
            string error = saves.LastError;
            resultSaved = saves.PendingResult != result && saves.TryRecordStageResult(result, out error);
            SaveError = resultSaved ? null : error;
            ChangeState(GameFlowState.StageClear);
        }
        finally { busy = previousBusy; }
    }
    private bool RequestPause(out string error)
    {
        if (!Require(GameFlowState.Playing, out error)) return false;
        ChangeState(GameFlowState.PauseConfirmation);
        return true;
    }
    private bool ConfirmPause(out string error)
    {
        if (!Require(GameFlowState.PauseConfirmation, out error)) return false;
        if (!StageSessionInitializer.TryDiscard(session, database, out error)) return false;
        ChangeState(GameFlowState.Lobby);
        return true;
    }
    private bool Cancel(out string error)
    {
        error = null;
        switch (State)
        {
            case GameFlowState.PauseConfirmation: ChangeState(GameFlowState.Playing); return true;
            case GameFlowState.NewGameConfirmation:
            case GameFlowState.ExitConfirmation:
            case GameFlowState.StageSelect: ChangeState(GameFlowState.Lobby); return true;
            default: error = "There is no cancelable screen."; return false;
        }
    }
    private bool Escape(out string error)
    {
        return State == GameFlowState.Playing ? RequestPause(out error) : Cancel(out error);
    }
    private bool RequireSavedClear(out string error)
    {
        if (!Require(GameFlowState.StageClear, out error)) return false;
        if (!CanLeaveClear) { error = "Save the completed result before leaving the clear screen."; return false; }
        return true;
    }
    private bool Retry(out string error) => RequireSavedClear(out error) && EnterStage(SelectedStageNumber, out error);
    private bool NextStage(out string error)
    {
        if (!RequireSavedClear(out error)) return false;
        if (!CanNextStage) { error = "There is no unlocked next stage."; return false; }
        return EnterStage(SelectedStageNumber + 1, out error);
    }
    private bool RetrySave(out string error)
    {
        if (!Require(GameFlowState.StageClear, out error)) return false;
        resultSaved = saves.TryRecordStageResult(Result, out error);
        SaveError = resultSaved ? null : error;
        OnStateChanged?.Invoke();
        return resultSaved;
    }
    private bool ReturnToSelect(out string error)
    {
        error = null;
        if (State == GameFlowState.StageClear)
        {
            if (!RequireSavedClear(out error) || !StageSessionInitializer.TryDiscard(session, database, out error)) return false;
        }
        else if (State == GameFlowState.StageLoadFailed)
        {
            if (!saves.CanContinue) { error = "Valid progress is required for stage selection."; return false; }
            session.Board.ResetBoard();
            session.History.TryClearHistory();
        }
        else { error = "Stage selection is unavailable from this screen."; return false; }
        Result = null;
        ChangeState(GameFlowState.StageSelect);
        return true;
    }
    private bool RequestExit(out string error)
    {
        if (!Require(GameFlowState.Lobby, out error)) return false;
        ChangeState(GameFlowState.ExitConfirmation);
        return true;
    }
    private bool ConfirmExit(out string error)
    {
        if (!Require(GameFlowState.ExitConfirmation, out error)) return false;
        QuitRequested = true;
        OnQuitRequested?.Invoke();
        return true;
    }

    private void ChangeState(GameFlowState next)
    {
        GameFlowScene previousScene = Scene;
        bool wasUnconfigured = State == GameFlowState.Unconfigured;
        State = next;
        session.TrySetGameplayEnabled(next == GameFlowState.Playing, out _);
        OnStateChanged?.Invoke();
        if (wasUnconfigured || previousScene != Scene) OnSceneRequested?.Invoke(Scene);
    }
    private static bool IsLobbyState(GameFlowState value) => value == GameFlowState.Unconfigured
        || value == GameFlowState.Lobby || value == GameFlowState.NewGameConfirmation
        || value == GameFlowState.StageSelect || value == GameFlowState.ExitConfirmation;
    private void OnEnable()
    {
        Subscribe();
        if (session != null) session.TrySetGameplayEnabled(State == GameFlowState.Playing, out _);
    }
    private void OnDisable()
    {
        Unsubscribe();
        if (session != null) session.TrySetGameplayEnabled(false, out _);
    }
    private void Subscribe()
    {
        if (!isActiveAndEnabled || session == null || subscribedStage == session.Stage) return;
        Unsubscribe();
        subscribedStage = session.Stage;
        subscribedStage.OnStageCompleted += HandleStageCompleted;
    }
    private void Unsubscribe()
    {
        if (subscribedStage != null) subscribedStage.OnStageCompleted -= HandleStageCompleted;
        subscribedStage = null;
    }
}
