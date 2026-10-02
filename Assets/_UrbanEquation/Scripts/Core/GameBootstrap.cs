using System;
using System.Collections.Generic;
using UnityEngine;

// Explicit startup only: adding this component does not create a session or access disk.
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private GameContentData startupContent;
    [SerializeField] private string startupSavePath;
    [SerializeField] private bool initializeOnStart;
    [SerializeField] private bool showDebugUI;
    private bool initializing;
    private GameObject runtimeRoot;

    public bool IsInitialized => runtimeRoot != null;
    public GameContentData Content { get; private set; }
    public GameSessionManager Session { get; private set; }
    public SceneFlowManager Flow { get; private set; }
    public SaveManager Saves { get; private set; }

    public void ConfigureForStartup(GameContentData content, string savePath, bool debugUI)
    {
        if (initializing || IsInitialized) throw new InvalidOperationException("Bootstrap is already initialized.");
        startupContent = content;
        startupSavePath = savePath;
        showDebugUI = debugUI;
        initializeOnStart = true;
    }

    private void Start()
    {
        if (initializeOnStart && !IsInitialized
            && !TryInitialize(startupContent, startupSavePath, showDebugUI, out string error))
            Debug.LogError("Urban Equation startup failed: " + error, this);
    }

    public bool TryInitialize(GameContentData content, string savePath, bool debugUI, out string error)
    {
        error = null;
        if (!isActiveAndEnabled || initializing || IsInitialized)
        { error = "Bootstrap must be active, idle and uninitialized."; return false; }
        if (content == null || string.IsNullOrWhiteSpace(savePath))
        { error = "Startup requires content and an explicit save path."; return false; }
        var errors = new List<string>();
        content.Validate(errors);
        if (errors.Count != 0) { error = string.Join("; ", errors); return false; }
        initializing = true;
        GameObject candidate = null;
        try
        {
            candidate = new GameObject("Urban Equation Session");
            candidate.SetActive(false);
            candidate.transform.SetParent(transform, false);
            var board = candidate.AddComponent<BoardManager>();
            var resources = candidate.AddComponent<ResourceManager>();
            var hand = candidate.AddComponent<BuildingHandManager>();
            var combos = candidate.AddComponent<ComboManager>();
            var stage = candidate.AddComponent<StageManager>();
            var session = candidate.AddComponent<GameSessionManager>();
            var history = candidate.AddComponent<TurnHistoryManager>();
            var saves = candidate.AddComponent<SaveManager>();
            var flow = candidate.AddComponent<SceneFlowManager>();
            StageData first = content.Stages.Stages[0];
            if (!board.TryCreateBoard(first, out error)) return false;
            if (!resources.TryInitializeFromStage(first)) { error = "Could not initialize resources."; return false; }
            if (!hand.TryInitializeFromStage(first, resources, out error)
                || !combos.TryConfigure(board, resources, content.Combos, out error)) return false;
            session.Configure(board, resources, hand, content.BuildingPrefab);
            session.ConfigureCombos(combos);
            if (!stage.TryConfigure(first, board, resources, hand, out error)
                || !session.TryConfigureStage(stage, out error)
                || !history.TryConfigure(session, stage, out error)
                || !session.TrySetGameplayEnabled(false, out error)
                || !saves.TryConfigure(content.Stages.Count, savePath, out error)) return false;
            candidate.SetActive(true);
            if (!flow.TryConfigure(content.Stages, saves, session, content.Combos, out error)) return false;
            if (debugUI)
            {
                var ui = candidate.AddComponent<GameFlowDebugUI>();
                ui.Configure(flow, session);
                ui.SetManualPlacement(true);
            }
            runtimeRoot = candidate;
            Content = content;
            Session = session;
            Saves = saves;
            Flow = flow;
            return true;
        }
        catch (Exception exception) { error = "Could not start game: " + exception.Message; return false; }
        finally
        {
            initializing = false;
            if (candidate != null && candidate != runtimeRoot)
            {
                candidate.SetActive(false);
                if (Application.isPlaying) Destroy(candidate); else DestroyImmediate(candidate);
            }
        }
    }
}
