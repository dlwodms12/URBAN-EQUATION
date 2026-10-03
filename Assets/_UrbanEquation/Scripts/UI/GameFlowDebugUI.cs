using System.Collections.Generic;
using UnityEngine;

// Opt-in temporary controls. No scene creation, disk access or configuration on component addition.
public class GameFlowDebugUI : MonoBehaviour
{
    [SerializeField] private SceneFlowManager flow;
    [SerializeField] private GameSessionManager session;
    private string error;
    private Vector2 scroll;
    private bool manualPlacement;
    private bool hideWhilePlaying;
    private Vector2Int selectedCoordinate;
    public void SetManualPlacement(bool enabled) => manualPlacement = enabled;
    public void SetGameplayPanelVisible(bool visible) => hideWhilePlaying = !visible;
    private delegate bool Command(out string error);

    public void Configure(SceneFlowManager manager, GameSessionManager gameSession)
    {
        flow = manager;
        session = gameSession;
        error = null;
    }

    private void Run(Command command)
    {
        if (command(out string message)) error = null;
        else error = message;
    }

    private void OnGUI()
    {
        if (flow == null || !flow.isActiveAndEnabled || flow.State == GameFlowState.Unconfigured) return;
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            Run(flow.TryHandleEscape);
            Event.current.Use();
        }
        if (hideWhilePlaying && flow.State == GameFlowState.Playing) return;
        GUILayout.BeginArea(new Rect(12, 12, 340, Screen.height - 24), GUI.skin.box);
        scroll = GUILayout.BeginScrollView(scroll);
        GUILayout.Label("URBAN EQUATION / " + flow.State);
        StageData stage = flow.SelectedStage;
        switch (flow.State)
        {
            case GameFlowState.Lobby:
                if (GUILayout.Button("Play")) Run(flow.TryPlay);
                GUI.enabled = flow.CanContinue;
                if (GUILayout.Button("Continue")) Run(flow.TryContinue);
                GUI.enabled = true;
                if (GUILayout.Button("Exit")) Run(flow.TryRequestExit);
                break;
            case GameFlowState.NewGameConfirmation:
                GUILayout.Label("지난 게임을 이어 할까요?");
                if (GUILayout.Button("새로 시작하기")) Run(flow.TryConfirmNewGame);
                if (GUILayout.Button("이어하기")) Run(flow.TryContinue);
                if (GUILayout.Button("취소")) Run(flow.TryCancel);
                break;
            case GameFlowState.StageSelect:
                for (int number = 1; number <= flow.StageCount; number++)
                {
                    GUI.enabled = flow.IsStageUnlocked(number);
                    if (GUILayout.Button("Stage " + number + " / Rank " + flow.GetBestRank(number)))
                        Run((out string problem) => flow.TrySelectStage(number, out problem));
                }
                GUI.enabled = true;
                if (GUILayout.Button("Lobby")) Run(flow.TryCancel);
                break;
            case GameFlowState.StageIntro:
                GUILayout.Label("Stage " + flow.SelectedStageNumber + " / " + stage.StageName);
                GUILayout.Label(stage.Introduction);
                GUILayout.Label("필수: " + stage.RequiredGoal.Description);
                foreach (StageGoalData goal in stage.AdditionalGoals) GUILayout.Label("추가: " + goal.Description);
                if (GUILayout.Button("OK")) Run(flow.TryDismissIntro);
                break;
            case GameFlowState.Playing:
                if (session != null)
                {
                    GUILayout.Label("Stage " + flow.SelectedStageNumber);
                    if (manualPlacement)
                    {
                        GUILayout.Label("타일 선택 (위쪽 = 북쪽 +Z): " + selectedCoordinate);
                        for (int y = session.Board.Height - 1; y >= 0; y--)
                        {
                            GUILayout.BeginHorizontal();
                            for (int x = 0; x < session.Board.Width; x++)
                                if (GUILayout.Button(x + "," + y)) selectedCoordinate = new Vector2Int(x, y);
                            GUILayout.EndHorizontal();
                        }
                    }
                    foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
                        GUILayout.Label(type + ": " + session.Resources.GetResource(type));
                    var cards = new List<BuildingCardState>(session.Hand.Cards);
                    foreach (BuildingCardState card in cards)
                    {
                        GUI.enabled = session.Hand.IsCardAvailable(card.CardId);
                        if (GUILayout.Button("건설 " + card.CardId + ": " + card.Building.BuildingName))
                        {
                            if (manualPlacement)
                                session.TryCommitBuild(card.CardId, selectedCoordinate, out _, out error);
                            else BuildFirstAvailable(card.CardId);
                        }
                    }
                    GUI.enabled = session.History.CanUndo;
                    if (GUILayout.Button("Undo")) Run(session.History.TryUndo);
                    GUI.enabled = session.Stage.NextStageAvailable;
                    if (GUILayout.Button("NEXT STAGE")) Run(flow.TryCompleteStage);
                    GUI.enabled = true;
                    ComboResult presentation = session.Combos == null ? null : session.Combos.CurrentPresentation;
                    if (presentation != null)
                    {
                        GUILayout.Label("콤보: " + presentation.ComboName + " / " + presentation.Description);
                        if (GUILayout.Button("다음 콤보 표시")) session.Combos.TryCompletePresentation(presentation);
                    }
                }
                if (GUILayout.Button("중단 / ESC")) Run(flow.TryRequestPause);
                break;
            case GameFlowState.StageClear:
                GUILayout.Label("Stage " + flow.SelectedStageNumber + " Clear / Rank " + flow.Result.Rank);
                for (int i = 0; i < flow.Result.GoalStates.Count; i++)
                    GUILayout.Label("목표 " + (i + 1) + ": " + flow.Result.GoalStates[i]);
                if (!string.IsNullOrEmpty(flow.SaveError))
                {
                    GUILayout.Label(flow.SaveError);
                    if (GUILayout.Button("저장 재시도")) Run(flow.TryRetrySave);
                }
                GUI.enabled = flow.CanLeaveClear;
                if (GUILayout.Button("Retry")) Run(flow.TryRetry);
                if (GUILayout.Button("Stage Select")) Run(flow.TryReturnToStageSelect);
                GUI.enabled = flow.CanNextStage;
                if (GUILayout.Button("Next Stage")) Run(flow.TryNextStage);
                GUI.enabled = true;
                break;
            case GameFlowState.PauseConfirmation:
                GUILayout.Label("현재 스테이지를 중단하고 Lobby로 돌아갈까요?");
                if (GUILayout.Button("예 (현재 진행 폐기)")) Run(flow.TryConfirmPause);
                if (GUILayout.Button("아니오")) Run(flow.TryCancel);
                break;
            case GameFlowState.ExitConfirmation:
                GUILayout.Label("게임을 종료할까요?");
                // Quit is an event for the eventual application bootstrap; never quit the Editor here.
                if (GUILayout.Button("예")) Run(flow.TryConfirmExit);
                if (GUILayout.Button("아니오")) Run(flow.TryCancel);
                break;
            case GameFlowState.StageLoadFailed:
                GUILayout.Label("스테이지 초기화에 실패했습니다.");
                if (GUILayout.Button("Stage Select로 돌아가기")) Run(flow.TryReturnToStageSelect);
                break;
        }
        if (!string.IsNullOrEmpty(error ?? flow.LastError)) GUILayout.Label(error ?? flow.LastError);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void BuildFirstAvailable(int cardId)
    {
        for (int y = 0; y < session.Board.Height; y++)
            for (int x = 0; x < session.Board.Width; x++)
            {
                var coordinate = new Vector2Int(x, y);
                if (!session.CanBuild(cardId, coordinate, out _)) continue;
                session.TryCommitBuild(cardId, coordinate, out _, out error);
                return;
            }
        error = "이 카드가 들어갈 수 있는 빈 타일이 없습니다.";
    }
}
