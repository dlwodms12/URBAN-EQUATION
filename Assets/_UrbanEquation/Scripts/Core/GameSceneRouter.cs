using System;
using UnityEngine;

// Scene IO is injected. Flow/session remain owned by the persistent application.
public class GameSceneRouter : MonoBehaviour
{
    private SceneFlowManager flow;
    private GameSessionManager session;
    private GameApplicationSettings settings;
    private Func<string, bool> canLoad;
    private Action<string> load;
    private Action quit;
    private bool subscribed;
    private bool sceneReady;
    private GameFlowScene currentScene;
    private GameFlowScene loadingScene;
    public bool IsLoading { get; private set; }
    public bool IsReady => sceneReady && !IsLoading && LastError == null && flow != null && currentScene == flow.Scene;
    public string LastError { get; private set; }
    public event Action OnChanged;

    public bool TryConfigure(SceneFlowManager screens, GameSessionManager game, GameApplicationSettings data,
        Func<string, bool> available, Action<string> loader, Action quitter, out string error)
    {
        error = null;
        if (!isActiveAndEnabled || IsLoading || screens == null || game == null || data == null
            || available == null || loader == null || quitter == null)
        { error = "Scene routing requires an active idle router, flow, session, settings and IO callbacks."; return false; }
        if (session != null && session != game) session.TrySetGameplayEnabled(false, out _);
        Unsubscribe(); flow = screens; session = game; settings = data;
        canLoad = available; load = loader; quit = quitter; sceneReady = false; LastError = null;
        Subscribe(); Gate(); return true;
    }
    public void NotifySceneReady(GameFlowScene scene)
    {
        if (flow == null || !isActiveAndEnabled || (IsLoading && scene != loadingScene)) return;
        currentScene = scene; sceneReady = true; IsLoading = false; LastError = null;
        RequestCurrentScene();
    }
    public bool TryRetryLoad(out string error)
    {
        error = null;
        if (flow == null || IsLoading || !isActiveAndEnabled || flow.QuitRequested)
        { error = "Scene loading cannot be retried now."; return false; }
        LastError = null; RequestCurrentScene(); error = LastError; return error == null;
    }
    private void RequestScene(GameFlowScene ignored) => RequestCurrentScene();
    private void RequestCurrentScene()
    {
        if (flow == null || !isActiveAndEnabled) return;
        if (IsLoading) { Gate(); OnChanged?.Invoke(); return; }
        if (sceneReady && currentScene == flow.Scene) { Gate(); OnChanged?.Invoke(); return; }
        string path = settings.ScenePath(flow.Scene);
        try
        {
            if (!canLoad(path)) throw new InvalidOperationException("Requested scene is absent from the build scene list: " + path);
            loadingScene = flow.Scene; IsLoading = true; LastError = null;
            Gate(); OnChanged?.Invoke(); load(path);
        }
        catch (Exception exception)
        { IsLoading = false; LastError = exception.Message; Gate(); OnChanged?.Invoke(); }
    }
    private void StateChanged() { Gate(); OnChanged?.Invoke(); }
    private void Gate()
    {
        if (session != null) session.TrySetGameplayEnabled(isActiveAndEnabled && IsReady
            && flow.State == GameFlowState.Playing && !flow.QuitRequested, out _);
    }
    private void QuitRequested()
    {
        Gate();
        try { quit(); }
        catch (Exception exception) { LastError = exception.Message; OnChanged?.Invoke(); }
    }
    private void OnEnable() { Subscribe(); if (sceneReady) RequestCurrentScene(); else Gate(); }
    private void OnDisable() { Unsubscribe(); if (session != null) session.TrySetGameplayEnabled(false, out _); }
    private void OnDestroy() => Unsubscribe();
    private void Subscribe()
    {
        if (subscribed || flow == null || !isActiveAndEnabled) return;
        flow.OnSceneRequested += RequestScene; flow.OnStateChanged += StateChanged; flow.OnQuitRequested += QuitRequested;
        subscribed = true;
    }
    private void Unsubscribe()
    {
        if (!subscribed) return; subscribed = false;
        if (flow != null) { flow.OnSceneRequested -= RequestScene; flow.OnStateChanged -= StateChanged; flow.OnQuitRequested -= QuitRequested; }
    }
}
