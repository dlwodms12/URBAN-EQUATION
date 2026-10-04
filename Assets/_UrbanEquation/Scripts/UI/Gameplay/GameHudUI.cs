using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHudUI : MonoBehaviour
{
    [SerializeField] private GameObject gameplayRoot;
    [SerializeField] private ResourceStripUI cityResources;
    [SerializeField] private TMP_Text stageTitle;
    [SerializeField] private StageGoalRowUI[] goals = new StageGoalRowUI[3];
    [SerializeField] private BuildingHandUI hand;
    [SerializeField] private Transform cardContent;
    [SerializeField] private GameplayPrefabSet commonPrefabs;
    [SerializeField] private UndoButtonUI undoView;
    [SerializeField] private Button undoButton;
    [SerializeField] private NextStageButtonUI nextView;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private TMP_Text placementFeedback;
    [SerializeField] private BuildingTooltipUI tooltip;
    [SerializeField] private ComboPopupUI automaticCombo;
    [SerializeField] private ComboPopupUI replayCombo;
    [SerializeField] private BoardDetailsUI boardDetails;
    [SerializeField] private GameUiUpdateSettings uiUpdate;
    private GameSessionManager session;
    private SceneFlowManager flow;
    private BuildingPlacementController placement;
    private bool subscribed;
    private bool screenBackdropVisible;
    public bool GameplayVisible => gameplayRoot != null && gameplayRoot.activeSelf;
    public void SetScreenBackdropVisible(bool visible) { screenBackdropVisible = visible; RefreshAll(); }

    public void Bind(GameSessionManager game, SceneFlowManager screens, BuildingPlacementController controller, Camera camera)
    {
        Unsubscribe(); session = game; flow = screens; placement = controller;
        if (undoView != null) undoView.Configure(session == null ? null : session.History, undoButton);
        if (nextView != null) nextView.Configure(session == null ? null : session.Stage, nextButton);
        if (hand != null)
        {
            hand.SetTooltip(tooltip);
            hand.Bind(session == null ? null : session.Hand, placement,
                commonPrefabs == null ? null : commonPrefabs.BuildingCardPrefab, cardContent);
        }
        if (uiUpdate != null)
        {
            if (hand != null && hand.TryGetComponent<ScrollRect>(out var scroll))
            {
                var buttons = hand.GetComponent<BuildingHandScrollUI>();
                if (buttons == null) buttons = hand.gameObject.AddComponent<BuildingHandScrollUI>();
                buttons.Configure(scroll, uiUpdate.LeftScrollButton, uiUpdate.RightScrollButton, session);
            }
            if (automaticCombo != null) automaticCombo.ConfigureComplaintSprites(uiUpdate.ComplaintBackground, uiUpdate.ComplaintHeader);
            if (replayCombo != null) replayCombo.ConfigureComplaintSprites(uiUpdate.ComplaintBackground, uiUpdate.ComplaintHeader);
        }
        if (automaticCombo != null) automaticCombo.Bind(session == null ? null : session.Combos, camera);
        if (boardDetails != null) boardDetails.Bind(session, placement, camera, tooltip, replayCombo);
        if (isActiveAndEnabled) Subscribe(); RefreshAll();
    }
    private void OnEnable() { Subscribe(); RefreshAll(); }
    private void OnDisable() { Unsubscribe(); HideDetails(); if (gameplayRoot != null) gameplayRoot.SetActive(false); }
    private void OnDestroy() => Unsubscribe();
    private void LateUpdate() => RefreshPlacement(placement == null ? null : placement.PreviewTile,
        placement != null && placement.PreviewCanPlace);
    private void Subscribe()
    {
        if (subscribed || session == null || flow == null || session.Resources == null || session.Stage == null) return;
        subscribed = true;
        session.Resources.OnResourcesChanged += RefreshResources;
        session.Stage.OnStateChanged += RefreshGoals;
        session.OnStateRestored += RefreshAll;
        flow.OnStateChanged += RefreshAll;
        if (placement != null) placement.OnPreviewChanged += RefreshPlacement;
        if (pauseButton != null) pauseButton.onClick.AddListener(Pause);
    }
    private void Unsubscribe()
    {
        if (!subscribed) return; subscribed = false;
        if (session != null)
        {
            if (session.Resources != null) session.Resources.OnResourcesChanged -= RefreshResources;
            if (session.Stage != null) session.Stage.OnStateChanged -= RefreshGoals;
            session.OnStateRestored -= RefreshAll;
        }
        if (flow != null) flow.OnStateChanged -= RefreshAll;
        if (placement != null) placement.OnPreviewChanged -= RefreshPlacement;
        if (pauseButton != null) pauseButton.onClick.RemoveListener(Pause);
    }
    public void RefreshAll()
    {
        bool playing = isActiveAndEnabled && session != null && flow != null && flow.State == GameFlowState.Playing
            && session.GameplayEnabled;
        bool backdrop = screenBackdropVisible && isActiveAndEnabled && session != null && flow != null
            && flow.Scene == GameFlowScene.Game && flow.State != GameFlowState.Unconfigured;
        if (gameplayRoot != null) gameplayRoot.SetActive(playing || backdrop);
        if (pauseButton != null) pauseButton.interactable = playing;
        if (!playing) HideDetails();
        RefreshResources(); RefreshGoals();
        RefreshPlacement(placement == null ? null : placement.PreviewTile, placement != null && placement.PreviewCanPlace);
    }
    private void HideDetails()
    { if (tooltip != null) tooltip.HideAll(); if (boardDetails != null) boardDetails.ClearSelection(); }
    private void RefreshResources()
    { if (cityResources != null) cityResources.Show(session == null || session.Resources == null ? null : session.Resources.CaptureResourceState(), false); }
    private void RefreshGoals()
    {
        StageManager stage = session == null ? null : session.Stage;
        StageData data = stage == null ? null : stage.CurrentStage;
        if (stageTitle != null) stageTitle.text = data == null ? string.Empty : "Stage " + data.StageNumber;
        if (goals == null) return;
        for (int i = 0; i < goals.Length; i++)
        {
            if (goals[i] == null) continue;
            StageGoalData goal = data == null ? null : i == 0 ? data.RequiredGoal : data.AdditionalGoals[i - 1];
            goals[i].Show(goal == null ? string.Empty : goal.Description,
                stage != null && i < stage.GoalStates.Count && stage.GoalStates[i]);
        }
    }
    private void RefreshPlacement(Tile tile, bool allowed)
    {
        if (placementFeedback == null) return;
        bool dragging = placement != null && placement.IsDragging;
        placementFeedback.text = !dragging ? string.Empty : allowed ? "건설 가능" : "이 위치에는 건설할 수 없습니다";
        placementFeedback.color = allowed ? ResourceStripUI.GainColor : ResourceStripUI.SpendColor;
    }
    private void Pause() { if (flow != null) flow.TryRequestPause(out _); }
}
