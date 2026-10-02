using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class UrbanEquationDebugMenu
{
    [MenuItem("Tools/Urban Equation/Stage 1-2 Debug")]
    private static void Open()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var content = AssetDatabase.LoadAssetAtPath<GameContentData>("Assets/_UrbanEquation/Data/GameContent.asset");
        var errors = new List<string>();
        if (content == null) errors.Add("GameContent.asset is missing."); else content.Validate(errors);
        if (errors.Count != 0) { Debug.LogError(string.Join("; ", errors)); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("Urban Equation Debug");
        root.AddComponent<GameBootstrap>().ConfigureForStartup(content,
            Path.Combine(Application.temporaryCachePath, "UrbanEquationDebug", "phase3j-progress.json"), true);
        var cameraObject = new GameObject("Debug Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(6, 8, -5);
        camera.transform.LookAt(new Vector3(1, 0, 1));
        camera.orthographic = true;
        camera.orthographicSize = 4;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.15f, 0.18f);
        var light = new GameObject("Debug Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/Urban Equation/Stage 1-2 Debug", true)]
    private static bool CanOpen() => !EditorApplication.isPlayingOrWillChangePlaymode;
}
