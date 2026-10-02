public enum GameFlowState
{
    Unconfigured, Lobby, NewGameConfirmation, StageSelect, StageIntro,
    Playing, StageClear, PauseConfirmation, ExitConfirmation, StageLoadFailed
}

// Logical destination. Actual Lobby/Game scenes and their loader are integrated in Phase 4.
public enum GameFlowScene { Lobby, Game }
