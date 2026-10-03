using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameApplication", menuName = "Urban Equation/Game Application")]
public class GameApplicationSettings : ScriptableObject
{
    [SerializeField] private GameContentData content;
    [SerializeField] private GameplayHudSet hud;
    [SerializeField] private GameScreensUI screensPrefab;
    [SerializeField] private string lobbyScenePath = "Assets/_UrbanEquation/Scenes/Lobby.unity";
    [SerializeField] private string gameScenePath = "Assets/_UrbanEquation/Scenes/Game.unity";
    public GameContentData Content => content;
    public GameplayHudSet Hud => hud;
    public GameScreensUI ScreensPrefab => screensPrefab;
    public string ScenePath(GameFlowScene scene) => scene == GameFlowScene.Lobby ? lobbyScenePath : gameScenePath;
    public void Validate(List<string> errors)
    {
        if (content == null) errors.Add("Application content is missing."); else content.Validate(errors);
        if (hud == null) errors.Add("Application HUD set is missing."); else hud.Validate(errors);
        if (screensPrefab == null) errors.Add("Application screen prefab is missing.");
        if (!ValidPath(lobbyScenePath) || !ValidPath(gameScenePath) || lobbyScenePath == gameScenePath)
            errors.Add("Application requires two distinct Assets scene paths ending in .unity.");
    }
    private static bool ValidPath(string path) => !string.IsNullOrWhiteSpace(path)
        && path.StartsWith("Assets/", System.StringComparison.Ordinal) && path.EndsWith(".unity", System.StringComparison.Ordinal)
        && !path.Contains("..") && !path.Contains("\\");
}
