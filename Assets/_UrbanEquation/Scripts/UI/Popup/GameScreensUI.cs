using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameScreensUI : MonoBehaviour
{
    [SerializeField] private GameObject lobbyBackground;
    [SerializeField] private GameObject lobby;
    [SerializeField] private GameObject modalBlocker;
    [SerializeField] private GameObject newGame;
    [SerializeField] private GameObject exit;
    [SerializeField] private GameObject stageSelect;
    [SerializeField] private GameObject intro;
    [SerializeField] private GameObject pause;
    [SerializeField] private GameObject clear;
    [SerializeField] private GameObject loadFailure;
    [SerializeField] private GameObject transition;
    [SerializeField] private TMP_Text introTitle;
    [SerializeField] private TMP_Text introDescription;
    [SerializeField] private TMP_Text clearTitle;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private TMP_Text saveErrorText;
    [SerializeField] private StageGoalRowUI[] clearGoals = new StageGoalRowUI[3];
    [SerializeField] private Image[] clearStars = new Image[3];
    [SerializeField] private StageSelectRowUI stageRowPrefab;
    [SerializeField] private Transform stageRowContent;
    [SerializeField] private Button playButton, continueButton, exitButton, newButton, resumeButton, newCancelButton;
    [SerializeField] private Button exitYesButton, exitNoButton, selectBackButton, introOkButton, pauseYesButton, pauseNoButton;
    [SerializeField] private Button retryButton, nextButton, clearSelectButton, saveRetryButton, loadSelectButton, loadRetryButton;
    private readonly List<StageSelectRowUI> rows = new List<StageSelectRowUI>();
    private SceneFlowManager flow;
    private GameSceneRouter router;
    private bool subscribed;
    private delegate bool Command(out string error);
    public int StageRowCount => rows.Count;
    public bool CanInteract => isActiveAndEnabled && flow != null && flow.isActiveAndEnabled && !flow.QuitRequested
        && (router == null || router.IsReady);

    public void Bind(SceneFlowManager manager, GameSceneRouter routes)
    { Unsubscribe(); flow=manager; router=routes; if(isActiveAndEnabled) Subscribe(); Refresh(); }
    public bool TryPlay() => flow != null && Run(flow.TryPlay);
    public bool TryContinue() => flow != null && Run(flow.TryContinue);
    public bool TryConfirmNewGame() => flow != null && Run(flow.TryConfirmNewGame);
    public bool TryRequestExit() => flow != null && Run(flow.TryRequestExit);
    public bool TryConfirmExit() => flow != null && Run(flow.TryConfirmExit);
    public bool TryCancel() => flow != null && Run(flow.TryCancel);
    public bool TryDismissIntro() => flow != null && Run(flow.TryDismissIntro);
    public bool TryConfirmPause() => flow != null && Run(flow.TryConfirmPause);
    public bool TryRetry() => flow != null && Run(flow.TryRetry);
    public bool TryNextStage() => flow != null && Run(flow.TryNextStage);
    public bool TryReturnToStageSelect() => flow != null && Run(flow.TryReturnToStageSelect);
    public bool TryRetrySave() => flow != null && Run(flow.TryRetrySave);
    public bool TrySelectStage(int number)
    {
        if (!CanInteract || flow == null) return false;
        bool result=flow.TrySelectStage(number,out _); ShowCommandError(result); return result;
    }
    public bool TryRetryLoad()
    {
        if(!isActiveAndEnabled || router==null) return false;
        bool result=router.TryRetryLoad(out _); ShowCommandError(result); return result;
    }
    private bool Run(Command command)
    {
        if (!CanInteract || command==null) return false;
        bool result=command(out _); ShowCommandError(result); return result;
    }
    private void ShowCommandError(bool success)
    { Refresh(); if(!success && errorText!=null) errorText.text="요청을 처리하지 못했습니다. 다시 시도해주세요."; }
    public void Refresh()
    {
        bool configured=flow!=null && flow.State!=GameFlowState.Unconfigured;
        GameFlowState state=configured?flow.State:GameFlowState.Unconfigured;
        bool routeError=router!=null && router.LastError!=null;
        bool loading=router!=null && router.IsLoading;
        bool interactive=CanInteract;
        Active(lobbyBackground,configured && flow.Scene==GameFlowScene.Lobby);
        Active(lobby,configured && flow.Scene==GameFlowScene.Lobby && state!=GameFlowState.StageSelect);
        Active(newGame,state==GameFlowState.NewGameConfirmation); Active(exit,state==GameFlowState.ExitConfirmation);
        Active(stageSelect,state==GameFlowState.StageSelect); Active(intro,state==GameFlowState.StageIntro);
        Active(pause,state==GameFlowState.PauseConfirmation); Active(clear,state==GameFlowState.StageClear);
        Active(loadFailure,state==GameFlowState.StageLoadFailed || routeError); Active(transition,loading);
        Active(modalBlocker,loading || routeError || (configured && state!=GameFlowState.Lobby && state!=GameFlowState.Playing));
        Set(playButton,interactive && state==GameFlowState.Lobby); Set(continueButton,interactive && flow!=null && flow.CanContinue);
        Set(exitButton,interactive && state==GameFlowState.Lobby);
        foreach(var button in new[]{newButton,newCancelButton,exitYesButton,exitNoButton,selectBackButton,introOkButton,pauseYesButton,pauseNoButton}) Set(button,interactive);
        Set(resumeButton,interactive && flow!=null && flow.CanResumeSavedGame);
        Set(retryButton,interactive && flow!=null && flow.CanLeaveClear); Set(clearSelectButton,interactive && flow!=null && flow.CanLeaveClear);
        Set(nextButton,interactive && flow!=null && flow.CanNextStage);
        bool saveFailed=state==GameFlowState.StageClear && !string.IsNullOrEmpty(flow.SaveError);
        Active(saveRetryButton==null?null:saveRetryButton.gameObject,saveFailed); Set(saveRetryButton,interactive && saveFailed);
        if(saveErrorText!=null) saveErrorText.text=saveFailed?"진행도를 저장하지 못했습니다. 저장을 다시 시도해주세요.":string.Empty;
        Active(loadRetryButton==null?null:loadRetryButton.gameObject,routeError); Set(loadRetryButton,routeError && !loading);
        Active(loadSelectButton==null?null:loadSelectButton.gameObject,state==GameFlowState.StageLoadFailed && !routeError); Set(loadSelectButton,interactive);
        if(errorText!=null) errorText.text=routeError?"화면을 불러오지 못했습니다. 다시 시도해주세요.":string.Empty;
        StageData stage=flow==null?null:flow.SelectedStage;
        if(introTitle!=null) introTitle.text=stage==null?string.Empty:"Stage "+stage.StageNumber
            +(string.IsNullOrEmpty(stage.StageName) || stage.StageName=="Stage "+stage.StageNumber?string.Empty:" · "+stage.StageName);
        if(introDescription!=null) introDescription.text=stage==null?string.Empty:stage.Introduction+"\n\n필수 목표\n★ "+stage.RequiredGoal.Description
            +"\n\n추가 목표\n★ "+stage.AdditionalGoals[0].Description+"\n★ "+stage.AdditionalGoals[1].Description;
        var result=flow==null?null:flow.Result;
        if(clearTitle!=null) clearTitle.text=result==null?"Stage Clear!":"Stage "+result.StageNumber+" Clear!";
        if(clearStars!=null) for(int i=0;i<clearStars.Length;i++) if(clearStars[i]!=null) clearStars[i].enabled=result!=null && i<result.Rank;
        if(clearGoals!=null) for(int i=0;i<clearGoals.Length;i++) if(clearGoals[i]!=null)
            clearGoals[i].Show(result==null || i>=result.GoalDescriptions.Count?string.Empty:result.GoalDescriptions[i],
                result!=null && i<result.GoalStates.Count && result.GoalStates[i]);
        RefreshStageRows(interactive);
    }
    private void RefreshStageRows(bool interactive)
    {
        if(flow==null || stageRowPrefab==null || stageRowContent==null) return;
        while(rows.Count<flow.StageCount)
        { var row=Instantiate(stageRowPrefab,stageRowContent); row.gameObject.SetActive(true); rows.Add(row); }
        for(int i=0;i<rows.Count;i++)
        { bool exists=i<flow.StageCount; rows[i].gameObject.SetActive(exists); if(exists) rows[i].Show(i+1,flow.GetBestRank(i+1),flow.IsStageUnlocked(i+1),interactive,SelectStage); }
    }
    private void SelectStage(int number) => TrySelectStage(number);
    private void Play() => TryPlay(); private void Continue() => TryContinue(); private void New() => TryConfirmNewGame();
    private void Exit() => TryRequestExit(); private void ExitYes() => TryConfirmExit(); private void Cancel() => TryCancel();
    private void IntroOk() => TryDismissIntro(); private void PauseYes() => TryConfirmPause();
    private void Retry() => TryRetry(); private void Next() => TryNextStage(); private void StageSelect() => TryReturnToStageSelect();
    private void SaveRetry() => TryRetrySave(); private void LoadRetry() => TryRetryLoad();
    private void OnEnable() { Subscribe(); Refresh(); }
    private void OnDisable() => Unsubscribe(); private void OnDestroy() => Unsubscribe();
    private void Subscribe()
    {
        if(subscribed || flow==null || !isActiveAndEnabled) return; subscribed=true;
        flow.OnStateChanged+=Refresh; if(router!=null) router.OnChanged+=Refresh;
        Wire(true);
    }
    private void Unsubscribe()
    {
        if(!subscribed) return; subscribed=false; if(flow!=null) flow.OnStateChanged-=Refresh;
        if(router!=null) router.OnChanged-=Refresh; Wire(false);
    }
    private void Wire(bool add)
    {
        Listen(playButton,Play,add); Listen(continueButton,Continue,add); Listen(exitButton,Exit,add);
        Listen(newButton,New,add); Listen(resumeButton,Continue,add); Listen(newCancelButton,Cancel,add);
        Listen(exitYesButton,ExitYes,add); Listen(exitNoButton,Cancel,add); Listen(selectBackButton,Cancel,add);
        Listen(introOkButton,IntroOk,add); Listen(pauseYesButton,PauseYes,add); Listen(pauseNoButton,Cancel,add);
        Listen(retryButton,Retry,add); Listen(nextButton,Next,add); Listen(clearSelectButton,StageSelect,add);
        Listen(saveRetryButton,SaveRetry,add); Listen(loadSelectButton,StageSelect,add); Listen(loadRetryButton,LoadRetry,add);
    }
    private static void Listen(Button button,UnityEngine.Events.UnityAction action,bool add)
    { if(button==null) return; if(add) button.onClick.AddListener(action); else button.onClick.RemoveListener(action); }
    private static void Active(GameObject root,bool active) { if(root!=null) root.SetActive(active); }
    private static void Set(Button button,bool active) { if(button!=null) button.interactable=active; }
}
