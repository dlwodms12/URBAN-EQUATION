using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameApplication : MonoBehaviour
{
    public static GameApplication Instance { get; private set; }
    public GameApplicationSettings Settings { get; private set; }
    public GameBootstrap Bootstrap { get; private set; }
    public GameSceneRouter Router { get; private set; }
    public bool IsInitialized => Bootstrap != null && Bootstrap.IsInitialized && Router != null;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => Instance = null;
    public static string SavePath(bool temporary) => Path.Combine(temporary ? Application.temporaryCachePath : Application.persistentDataPath,
        temporary ? "UrbanEquationPhase4CPreview" : "URBAN-EQUATION", "progress.json");
    public static bool TryGetOrCreate(GameApplicationSettings data, bool temporary, out GameApplication application, out string error)
    {
        application = Instance; error = null;
        if (application != null)
        {
            if (!application.IsInitialized || application.Settings != data)
            { error = "Scene application settings do not match the active application."; return false; }
            return true;
        }
        var root = new GameObject("Urban Equation Application");
        application = root.AddComponent<GameApplication>();
        if (application.TryInitialize(data, SavePath(temporary), Application.CanStreamedLevelBeLoaded, Load, Quit, out error))
        { if (Application.isPlaying) DontDestroyOnLoad(root); return true; }
        if (Application.isPlaying) Destroy(root); else DestroyImmediate(root);
        application = null; return false;
    }
    public bool TryInitialize(GameApplicationSettings data, string savePath, Func<string, bool> available,
        Action<string> loader, Action quitter, out string error)
    {
        error = null;
        if (!isActiveAndEnabled || IsInitialized || (Instance != null && Instance != this) || data == null
            || string.IsNullOrWhiteSpace(savePath) || available == null || loader == null || quitter == null)
        { error = "Application initialization requires settings, an explicit save path and IO callbacks."; return false; }
        var errors = new List<string>(); data.Validate(errors);
        if (errors.Count > 0) { error = string.Join("; ", errors); return false; }
        var bootstrap = gameObject.AddComponent<GameBootstrap>();
        if (!bootstrap.TryInitialize(data.Content, savePath, false, out error))
        { DisposeComponent(bootstrap); return false; }
        var router = gameObject.AddComponent<GameSceneRouter>();
        if (!router.TryConfigure(bootstrap.Flow, bootstrap.Session, data, available, loader, quitter, out error))
        {
            // Only a successfully owned runtime is published as the singleton.
            bootstrap.gameObject.SetActive(false); DisposeComponent(router); DisposeComponent(bootstrap); return false;
        }
        Settings = data; Bootstrap = bootstrap; Router = router; Instance = this; return true;
    }
    private static void Load(string path)
    {
        if (SceneManager.LoadSceneAsync(path, LoadSceneMode.Single) == null)
            throw new InvalidOperationException("Scene loading did not start: " + path);
    }
    private static void Quit()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    private static void DisposeComponent(Component value)
    { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
