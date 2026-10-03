using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UrbanEquationScreensSetup
{
    private const string Root="Assets/_UrbanEquation/";
    private const string Gui="Assets/Space_Exploration_GUI_Kit/";
    public const string ScreensPath=Root+"Prefabs/UI/Popup/Pfb_Flow_Screens_001.prefab";
    public const string SettingsPath=Root+"Data/Presentation/GameApplication.asset";
    public const string LobbyPath=Root+"Scenes/Lobby.unity";
    public const string GamePath=Root+"Scenes/Game.unity";
    public const string ClosePath=Root+"Sprite/UI/Spr_Ui_XButton_001.png";
    public static readonly string[] ArtPaths={
        Gui+"Background_Images/extra large/background-1-extra-large.png",
        Gui+"Background_Images/extra large/background-overlay-extra-large.png",
        Gui+"Containers/Large/setting-container-large.png",
        Gui+"Containers/Extra Large/shop-item-container-extra-large.png",
        Gui+"Containers/Extra Large/pause-container-extra-large.png",
        Gui+"Settings_&_Menu_Components/Extra Large/check-box-empty-extra-large.png",
        Gui+"Icons/star-512.png",
        Gui+"Button_Images/Source_Image_Sprites/extra large/large-blue-extra-large.png",
        Gui+"Button_Images/Source_Image_Sprites/extra large/large-yellow-extra-large.png",
        Gui+"Button_Images/Source_Image_Sprites/extra large/large-pink-extra-large.png",
        Gui+"Button_Images/Source_Image_Sprites/large/medium-blue-large.png",
        Gui+"Button_Images/Source_Image_Sprites/large/medium-yellow-large.png",
        Gui+"Button_Images/Source_Image_Sprites/extra large/square-blue-extra-large.png",
        Gui+"Button_Images/Source_Image_Sprites/extra large/square-pink-extra-large.png",
        Gui+"Button_Images/Disabled_Sprites/Extra_Large/square-disabled-extra-large.png",
        Gui+"Picto_Icons/Dark_Purple/play-256.png",
        Gui+"Picto_Icons/Dark_Purple/lock-256.png",
        ClosePath,
        Gui+"Button_Images/Source_Image_Sprites/large/medium-blue-large.png",
        Gui+"Button_Images/Source_Image_Sprites/large/medium-pink-large.png"
    };
    [MenuItem("Tools/Urban Equation/Phase 4C/1 Create Missing Screens and Scenes")]
    private static void CreateMenu()
    {
        if(TryCreateMissing(out string error)) Debug.Log("Phase4-C 화면·씬 생성/연결 완료. 메뉴2 Validate Screens and Scenes를 실행해주세요.");
        else Debug.LogError(error);
    }
    [MenuItem("Tools/Urban Equation/Phase 4C/1 Create Missing Screens and Scenes",true)]
    [MenuItem("Tools/Urban Equation/Phase 4C/3 Open Lobby Scene",true)]
    [MenuItem("Tools/Urban Equation/Phase 4C/4 Preview with Temporary Save",true)]
    private static bool CanEdit() => !EditorApplication.isPlayingOrWillChangePlaymode;
    public static bool TryCreateMissing(out string error)
    {
        error=null;
        if(!CanEdit()) { error="Stop Play before screen setup."; return false; }
        var errors=UrbanEquationHudSetup.CollectValidationErrors();
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"Fonts/NotoSansKR-Medium SDF.asset");
        if(font==null) errors.Add("NotoSansKR-Medium SDF font is missing.");
        var sprites=ArtPaths.Select(SpriteAt).ToArray();
        for(int i=0;i<sprites.Length;i++) if(i!=17 && sprites[i]==null) errors.Add("Required GUI sprite is missing: "+ArtPaths[i]);
        if(!(AssetImporter.GetAtPath(ClosePath) is TextureImporter)) errors.Add("Close-button image is missing: "+ClosePath);
        CheckDestination(SettingsPath,typeof(GameApplicationSettings),errors);
        var existing=AssetDatabase.LoadMainAssetAtPath(ScreensPath);
        if(existing!=null && (!(existing is GameObject go) || go.GetComponent<GameScreensUI>()==null)) errors.Add("Screen prefab destination contains another asset type.");
        foreach(string path in new[]{LobbyPath,GamePath}) CheckDestination(path,typeof(SceneAsset),errors);
        if(errors.Count>0) { error=string.Join("\n",errors); return false; }
        try
        {
            EnsureFolder(Root+"Prefabs/UI/Popup"); sprites[17]=ImportClose();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ScreensPath);
            if(prefab==null)
            {
                var created=UrbanEquationScreensFactory.CreateScreens(font,sprites);
                try
                { prefab=PrefabUtility.SaveAsPrefabAsset(created,ScreensPath,out bool success); if(!success || prefab==null) throw new InvalidOperationException("Could not save screen prefab."); }
                finally { UnityEngine.Object.DestroyImmediate(created); }
            }
            var settings=AssetDatabase.LoadAssetAtPath<GameApplicationSettings>(SettingsPath);
            if(settings==null) { settings=ScriptableObject.CreateInstance<GameApplicationSettings>(); AssetDatabase.CreateAsset(settings,SettingsPath); }
            Ref(settings,"content",AssetDatabase.LoadAssetAtPath<GameContentData>(Root+"Data/GameContent.asset"));
            Ref(settings,"hud",AssetDatabase.LoadAssetAtPath<GameplayHudSet>(UrbanEquationHudSetup.SetPath));
            Ref(settings,"screensPrefab",prefab.GetComponent<GameScreensUI>());
            var serialized=new SerializedObject(settings); serialized.FindProperty("lobbyScenePath").stringValue=LobbyPath;
            serialized.FindProperty("gameScenePath").stringValue=GamePath; serialized.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssetIfDirty(settings);
            CreateSceneIfMissing(LobbyPath,GameFlowScene.Lobby,settings); CreateSceneIfMissing(GamePath,GameFlowScene.Game,settings);
            EditorBuildSettings.scenes=BuildScenes(EditorBuildSettings.scenes);
            errors=CollectValidationErrors(); if(errors.Count>0) { error=string.Join("\n",errors); return false; } return true;
        }
        catch(Exception exception)
        { error="Screen setup failed: "+exception.Message+"\nCompleted assets were retained. Resolve the error and run menu1 again."; return false; }
    }
    public static EditorBuildSettingsScene[] BuildScenes(EditorBuildSettingsScene[] existing)
    {
        var result=new List<EditorBuildSettingsScene>{new EditorBuildSettingsScene(LobbyPath,true),new EditorBuildSettingsScene(GamePath,true)};
        if(existing!=null) result.AddRange(existing.Where(x=>x.path!=LobbyPath && x.path!=GamePath));
        return result.ToArray();
    }
    private static void CreateSceneIfMissing(string path,GameFlowScene destination,GameApplicationSettings settings)
    {
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)!=null) return;
        var previous=SceneManager.GetActiveScene(); var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        { PopulateScene(settings,destination,false); if(!EditorSceneManager.SaveScene(scene,path)) throw new InvalidOperationException("Could not save scene: "+path); }
        finally { if(previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true); }
    }
    private static void PopulateScene(GameApplicationSettings settings,GameFlowScene destination,bool temporary)
    {
        var camera=new GameObject("Main Camera").AddComponent<Camera>(); camera.tag="MainCamera";
        camera.orthographic=true; camera.orthographicSize=4; camera.backgroundColor=new Color(.12f,.15f,.18f); camera.clearFlags=CameraClearFlags.SolidColor;
        camera.gameObject.AddComponent<AudioListener>();
        if(destination==GameFlowScene.Game)
        { var light=new GameObject("Directional Light").AddComponent<Light>(); light.type=LightType.Directional; light.transform.rotation=Quaternion.Euler(50,-30,0); }
        var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        new GameObject("Scene Entry").AddComponent<GameSceneEntry>().Configure(settings,destination,camera,temporary);
    }
    [MenuItem("Tools/Urban Equation/Phase 4C/2 Validate Screens and Scenes")]
    private static void ValidateMenu()
    { var errors=CollectValidationErrors(); if(errors.Count==0) Debug.Log("Phase4-C Screens Validation OK (Lobby / Game / menus / popups / build scenes)."); else Debug.LogError(string.Join("\n",errors)); }
    public static List<string> CollectValidationErrors()
    {
        var errors=UrbanEquationHudSetup.CollectValidationErrors();
        var settings=AssetDatabase.LoadAssetAtPath<GameApplicationSettings>(SettingsPath);
        if(settings==null) errors.Add("GameApplication.asset is missing."); else settings.Validate(errors);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ScreensPath); var screens=prefab==null?null:prefab.GetComponent<GameScreensUI>();
        if(screens==null) errors.Add("Pfb_Flow_Screens_001.prefab is missing.");
        else
        {
            CheckFields(screens,new[]{"lobbyBackground","lobby","modalBlocker","newGame","exit","stageSelect","intro","pause","clear","loadFailure","transition",
                "introTitle","introDescription","clearTitle","errorText","saveErrorText","stageRowPrefab","stageRowContent",
                "playButton","continueButton","exitButton","newButton","resumeButton","newCancelButton","exitYesButton","exitNoButton","selectBackButton","introOkButton",
                "pauseYesButton","pauseNoButton","retryButton","nextButton","clearSelectButton","saveRetryButton","loadSelectButton","loadRetryButton"},errors);
            CheckArray(screens,"clearGoals",3,errors); CheckArray(screens,"clearStars",3,errors);
            foreach(var row in prefab.GetComponentsInChildren<StageSelectRowUI>(true))
            { CheckFields(row,new[]{"title","selectButton","selectIcon","playIcon","lockIcon","unlockedSprite","lockedSprite"},errors); CheckArray(row,"stars",3,errors); }
            foreach(var row in prefab.GetComponentsInChildren<StageGoalRowUI>(true)) CheckFields(row,new[]{"description","background","achievedSprite","pendingSprite"},errors);
            var canvas=prefab.GetComponent<Canvas>();
            if(canvas==null || canvas.renderMode!=RenderMode.ScreenSpaceOverlay || canvas.sortingOrder<=0 || prefab.GetComponent<GraphicRaycaster>()==null)
                errors.Add("Screen Canvas must render above the HUD and receive UI raycasts.");
            if(settings!=null && settings.ScreensPrefab!=screens) errors.Add("GameApplication screen prefab reference is inconsistent.");
        }
        if(settings!=null && (settings.Content!=AssetDatabase.LoadAssetAtPath<GameContentData>(Root+"Data/GameContent.asset")
            || settings.Hud!=AssetDatabase.LoadAssetAtPath<GameplayHudSet>(UrbanEquationHudSetup.SetPath)
            || settings.ScenePath(GameFlowScene.Lobby)!=LobbyPath || settings.ScenePath(GameFlowScene.Game)!=GamePath)) errors.Add("GameApplication references/scene paths are inconsistent.");
        var build=EditorBuildSettings.scenes;
        if(build.Length<2 || build[0].path!=LobbyPath || !build[0].enabled || build[1].path!=GamePath || !build[1].enabled)
            errors.Add("Build scene list must start with enabled Lobby and Game scenes. Run menu1 or update Build Profiles Scene List.");
        if(!EditorApplication.isPlayingOrWillChangePlaymode)
        { ValidateScene(LobbyPath,GameFlowScene.Lobby,settings,errors); ValidateScene(GamePath,GameFlowScene.Game,settings,errors); }
        return errors;
    }
    private static void ValidateScene(string path,GameFlowScene destination,GameApplicationSettings settings,List<string> errors)
    {
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)==null) { errors.Add("Scene has not been generated: "+path); return; }
        var previous=SceneManager.GetActiveScene(); var scene=SceneManager.GetSceneByPath(path); bool opened=!scene.IsValid() || !scene.isLoaded;
        if(opened) scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var entries=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<GameSceneEntry>(true)).ToArray();
            if(entries.Length!=1) errors.Add(path+" requires exactly one GameSceneEntry.");
            else
            { var s=new SerializedObject(entries[0]); if(s.FindProperty("settings").objectReferenceValue!=settings || s.FindProperty("destination").enumValueIndex!=(int)destination
                || s.FindProperty("boardCamera").objectReferenceValue==null || s.FindProperty("temporarySave").boolValue) errors.Add(path+" has inconsistent entry/settings/camera/save mode."); }
            var events=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<EventSystem>(true)).ToArray();
            if(events.Length!=1 || events[0].GetComponent<InputSystemUIInputModule>()==null) errors.Add(path+" requires one Input System EventSystem.");
        }
        finally { if(previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); if(opened) EditorSceneManager.CloseScene(scene,true); }
    }
    [MenuItem("Tools/Urban Equation/Phase 4C/3 Open Lobby Scene")]
    private static void OpenLobby()
    { if(!ValidateForOpen() || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return; EditorSceneManager.OpenScene(LobbyPath); }
    [MenuItem("Tools/Urban Equation/Phase 4C/4 Preview with Temporary Save")]
    private static void Preview()
    {
        if(!ValidateForOpen() || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        PopulateScene(AssetDatabase.LoadAssetAtPath<GameApplicationSettings>(SettingsPath),GameFlowScene.Lobby,true);
        EditorApplication.isPlaying=true;
    }
    private static bool ValidateForOpen()
    { var errors=CollectValidationErrors(); if(errors.Count==0) return true; Debug.LogError(string.Join("\n",errors)); return false; }
    private static Sprite SpriteAt(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    private static Sprite ImportClose()
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(ClosePath);
        if(importer.textureType!=TextureImporterType.Sprite || importer.spriteImportMode!=SpriteImportMode.Single)
        { importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single; importer.mipmapEnabled=false; importer.alphaIsTransparency=true; importer.SaveAndReimport(); }
        return SpriteAt(ClosePath) ?? throw new InvalidOperationException("Close-button Sprite import failed.");
    }
    private static void EnsureFolder(string path)
    { if(AssetDatabase.IsValidFolder(path)) return; string parent=System.IO.Path.GetDirectoryName(path).Replace('\\','/'); EnsureFolder(parent); AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(path)); }
    private static void CheckDestination(string path,Type type,List<string> errors)
    { var asset=AssetDatabase.LoadMainAssetAtPath(path); if(asset!=null && !type.IsInstanceOfType(asset)) errors.Add("Destination contains another asset type: "+path); }
    private static void CheckFields(UnityEngine.Object target,string[] fields,List<string> errors)
    { var s=new SerializedObject(target); foreach(string name in fields) if(s.FindProperty(name)?.objectReferenceValue==null) errors.Add(target.name+" missing reference: "+name); }
    private static void CheckArray(UnityEngine.Object target,string name,int count,List<string> errors)
    { var p=new SerializedObject(target).FindProperty(name); if(p==null || p.arraySize!=count) { errors.Add(name+" requires "+count+" entries."); return; } for(int i=0;i<count;i++) if(p.GetArrayElementAtIndex(i).objectReferenceValue==null) errors.Add(name+" entry is missing: "+i); }
    private static void Ref(UnityEngine.Object target,string name,UnityEngine.Object value) => UrbanEquationPrefabFactory.Reference(target,name,value);
}
