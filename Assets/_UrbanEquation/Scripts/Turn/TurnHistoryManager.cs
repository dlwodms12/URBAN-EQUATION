using System;
using System.Collections.Generic;
using UnityEngine;

public class TurnHistoryManager : MonoBehaviour
{
    [SerializeField] private GameSessionManager session;
    [SerializeField] private StageManager stageManager;
    private readonly List<GameStateSnapshot> snapshots = new List<GameStateSnapshot>();
    private object ownerToken = new object();
    private BoardManager subscribedBoard;
    private BuildingHandManager subscribedHand;
    private ComboManager subscribedCombo;
    private StageManager subscribedStage;
    private long observedConfiguration;
    private long observedBoardReset;
    private long observedHandInitialization;
    private long observedComboConfiguration;
    private long observedStageConfiguration;
    private bool configured;
    private bool restoring;

    public GameSessionManager Session => session;
    public int Count { get { EnsureScope(); return snapshots.Count; } }
    public bool CanUndo { get { EnsureScope(); return configured && session != null
        && session.GameplayEnabled && snapshots.Count > 1; } }
    public GameStateSnapshot CurrentSnapshot
        { get { EnsureScope(); return snapshots.Count == 0 ? null : snapshots[snapshots.Count - 1]; } }
    public IReadOnlyList<GameStateSnapshot> Snapshots { get { EnsureScope(); return snapshots.AsReadOnly(); } }
    public event Action OnHistoryChanged;

    private void OnEnable() => EnsureScope();
    private void OnDisable() => Unsubscribe();
    private void Start()
    {
        if (!configured && session != null && !TryConfigure(session, stageManager, out string error))
            Debug.LogWarning(error, this);
    }

    public bool TryConfigure(GameSessionManager gameSession, StageManager stage, out string error)
    {
        error = null;
        if (restoring || (session != null && session.IsBusy)
            || gameSession == null || gameSession.IsBusy)
        { error = "Cannot configure history while a session is busy or missing."; return false; }
        if (gameSession.History != null && gameSession.History != this)
        { error = "The session already has another history manager."; return false; }
        if (!ValidateConnections(gameSession, stage, out error)) return false;
        Unsubscribe();
        if (session != null && session != gameSession) session.DetachHistory(this);
        session = gameSession;
        stageManager = stage;
        configured = true;
        session.AttachHistory(this, stage);
        ObserveScope();
        InvalidateHistory();
        EnsureScope();
        return true;
    }

    public bool TryUndo(out string error)
    {
        EnsureScope();
        error = null;
        if (!configured || restoring || session == null || session.IsBusy)
        { error = "Undo requires an idle, configured session."; return false; }
        if (!session.GameplayEnabled)
        { error = "Undo is disabled for this screen."; return false; }
        if (snapshots.Count < 2)
        { error = "There is no earlier completed turn. The first build cannot be undone."; return false; }
        if (!ValidateConnections(session, stageManager, out error)) return false;
        GameStateSnapshot target = snapshots[snapshots.Count - 2];
        if (target.OwnerToken != ownerToken)
        { error = "The target snapshot belongs to a different history scope."; return false; }
        restoring = true;
        try
        {
            return session.TryRestoreTurn(target,
                () => snapshots.RemoveAt(snapshots.Count - 1),
                () => OnHistoryChanged?.Invoke(), out error);
        }
        finally { restoring = false; }
    }

    public bool TryClearHistory()
    {
        EnsureScope();
        if (restoring || (session != null && session.IsBusy)) return false;
        InvalidateHistory();
        return true;
    }

    internal bool CanRecordFor(GameSessionManager gameSession, out string error)
    {
        EnsureScope();
        if (!configured || session != gameSession || session.History != this || restoring)
        { error = "Turn history is not configured for this session."; return false; }
        if (!ValidateConnections(session, stageManager, out error)) return false;
        if (snapshots.Count == int.MaxValue)
        { error = "Turn history exceeds the supported integer range."; return false; }
        if (!session.Board.TryCaptureBuildings(out BuildingStateSnapshot[] placements, out error)) return false;
        foreach (BuildingStateSnapshot building in placements)
        {
            if (building.Building.VisualPrefab == null)
            { error = "Existing buildings require visuals for Undo reconstruction."; return false; }
            var errors = new List<string>();
            building.Building.Validate(errors);
            if (errors.Count > 0) { error = string.Join("; ", errors); return false; }
        }
        return true;
    }

    internal void RecordCompletedTurn(GameSessionManager gameSession)
    {
        if (gameSession != session || !configured)
            throw new InvalidOperationException("Completed turn belongs to a different session.");
        if (!session.Board.TryCaptureBuildings(out BuildingStateSnapshot[] buildings, out string error))
            throw new InvalidOperationException(error);
        snapshots.Add(new GameStateSnapshot(ownerToken, snapshots.Count + 1,
            buildings, session.Resources.CaptureResourceState(), session.Hand.CaptureCards(),
            session.Combos == null ? new ComboResult[0] : session.Combos.CaptureResults(),
            session.LastComboResults, stageManager == null ? null : stageManager.CaptureProgressState()));
        OnHistoryChanged?.Invoke();
    }

    internal void UpdateCurrentProgress()
    {
        EnsureScope();
        if (!configured || restoring || snapshots.Count == 0 || stageManager == null) return;
        GameStateSnapshot current = snapshots[snapshots.Count - 1];
        snapshots[snapshots.Count - 1] = new GameStateSnapshot(ownerToken, current.TurnNumber,
            current.Buildings, current.Resources, current.Cards, current.Combos,
            current.LastBuildCombos, stageManager.CaptureProgressState());
    }

    internal void NotifyConfigurationChanged() => EnsureScope();
    internal void NotifyGameplayStateChanged() => OnHistoryChanged?.Invoke();

    private static bool ValidateConnections(GameSessionManager gameSession, StageManager stage, out string error)
    {
        error = null;
        if (gameSession.Board == null || !gameSession.Board.HasBoard || gameSession.Resources == null
            || gameSession.Hand == null || !gameSession.Hand.IsInitialized || gameSession.BuildingPrefab == null
            || gameSession.Hand.Resources != gameSession.Resources
            || (stage != null && stage.Resources != gameSession.Resources)
            || (gameSession.Combos != null && (gameSession.Combos.Board != gameSession.Board
                || gameSession.Combos.Resources != gameSession.Resources || gameSession.Combos.IsResolving)))
        { error = "History requires initialized board/hand and matching resource/combination/stage references."; return false; }
        if (stage != null && !stage.CanUseWith(gameSession, out error)) return false;
        return true;
    }

    private void ObserveScope()
    {
        observedConfiguration = session.ConfigurationVersion;
        observedBoardReset = session.Board == null ? 0 : session.Board.ResetVersion;
        observedHandInitialization = session.Hand == null ? 0 : session.Hand.InitializationVersion;
        observedComboConfiguration = session.Combos == null ? 0 : session.Combos.ConfigurationVersion;
        observedStageConfiguration = stageManager == null ? 0 : stageManager.ConfigurationVersion;
    }

    private void EnsureScope()
    {
        if (!configured || session == null) return;
        long boardVersion = session.Board == null ? 0 : session.Board.ResetVersion;
        long handVersion = session.Hand == null ? 0 : session.Hand.InitializationVersion;
        long comboVersion = session.Combos == null ? 0 : session.Combos.ConfigurationVersion;
        long stageVersion = stageManager == null ? 0 : stageManager.ConfigurationVersion;
        if (observedConfiguration != session.ConfigurationVersion || observedBoardReset != boardVersion
            || observedHandInitialization != handVersion || observedComboConfiguration != comboVersion
            || observedStageConfiguration != stageVersion)
        {
            ObserveScope();
            InvalidateHistory();
        }
        if (isActiveAndEnabled && subscribedBoard != session.Board)
        {
            Unsubscribe();
            subscribedBoard = session.Board;
            if (subscribedBoard != null) subscribedBoard.OnBoardReset += HandleBoardReset;
        }
        if (isActiveAndEnabled && subscribedHand != session.Hand)
        {
            if (subscribedHand != null) subscribedHand.OnHandChanged -= EnsureScope;
            subscribedHand = session.Hand;
            if (subscribedHand != null) subscribedHand.OnHandChanged += EnsureScope;
        }
        if (isActiveAndEnabled && subscribedCombo != session.Combos)
        {
            if (subscribedCombo != null) subscribedCombo.OnResultsChanged -= EnsureScope;
            subscribedCombo = session.Combos;
            if (subscribedCombo != null) subscribedCombo.OnResultsChanged += EnsureScope;
        }
        if (isActiveAndEnabled && subscribedStage != stageManager)
        {
            if (subscribedStage != null) subscribedStage.OnConfigurationChanged -= EnsureScope;
            subscribedStage = stageManager;
            if (subscribedStage != null) subscribedStage.OnConfigurationChanged += EnsureScope;
        }
    }

    private void HandleBoardReset() => EnsureScope();
    private void InvalidateHistory()
    {
        snapshots.Clear();
        ownerToken = new object();
        OnHistoryChanged?.Invoke();
    }
    private void Unsubscribe()
    {
        if (subscribedBoard != null) subscribedBoard.OnBoardReset -= HandleBoardReset;
        if (subscribedHand != null) subscribedHand.OnHandChanged -= EnsureScope;
        if (subscribedCombo != null) subscribedCombo.OnResultsChanged -= EnsureScope;
        if (subscribedStage != null) subscribedStage.OnConfigurationChanged -= EnsureScope;
        subscribedBoard = null;
        subscribedHand = null;
        subscribedCombo = null;
        subscribedStage = null;
    }
}
