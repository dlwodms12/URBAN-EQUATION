using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

[InitializeOnLoad]
public static class UrbanEquationFinalQa
{
    private const string PreviewKey="UrbanEquation.Phase4D.MultiComboPreview";
    private static GameObject previewRoot;
    private static GameContentData previewContent;
    private static StageCatalog previewCatalog;
    private static StageData previewStage;
    private static string previewDirectory;
    static UrbanEquationFinalQa() => EditorApplication.playModeStateChanged+=HandlePlayMode;

    [MenuItem("Tools/Urban Equation/Phase 4D/1 Audit Final Setup")]
    private static void Audit()
    {
        var errors=CollectValidationErrors();
        if(errors.Count==0) Debug.Log("Phase4-D Final Setup Audit OK. This checks setup only; run all EditMode tests and the manual/Player QA checklist.");
        else Debug.LogError(string.Join("\n",errors));
    }
    [MenuItem("Tools/Urban Equation/Phase 4D/1 Audit Final Setup",true)]
    [MenuItem("Tools/Urban Equation/Phase 4D/2 Open Lobby for Final QA",true)]
    [MenuItem("Tools/Urban Equation/Phase 4D/3 Multi Combo QA Preview",true)]
    private static bool CanEdit() => !EditorApplication.isPlayingOrWillChangePlaymode;
    public static List<string> CollectValidationErrors()
    {
        var errors=UrbanEquationScreensSetup.CollectValidationErrors();
        var content=AssetDatabase.LoadAssetAtPath<GameContentData>("Assets/_UrbanEquation/Data/GameContent.asset");
        if(content!=null)
        {
            if(content.Buildings==null || content.Buildings.Buildings.Count!=15) errors.Add("Final content requires the existing fifteen building definitions.");
            if(content.Combos==null || content.Combos.Combos.Count!=30) errors.Add("Final content requires the existing thirty combo definitions.");
            ValidateStageCatalog(content.Stages,errors);
        }
        var paths=new[]{UrbanEquationHudSetup.HudPath,UrbanEquationScreensSetup.ScreensPath,
            "Assets/_UrbanEquation/Prefabs/UI/Cards/BuildingCard.prefab",
            "Assets/_UrbanEquation/Prefabs/UI/Gameplay/ComboPopup.prefab",
            "Assets/_UrbanEquation/Prefabs/Gameplay/Buildings/BuildingRoot.prefab",
            "Assets/_UrbanEquation/Prefabs/Gameplay/Tiles/Pfb_Tile_Asphalt_001.prefab",
            "Assets/_UrbanEquation/Prefabs/Gameplay/Tiles/Pfb_Tile_Concrete_001.prefab",
            "Assets/_UrbanEquation/Prefabs/Gameplay/Tiles/Pfb_Tile_Grass_001.prefab"};
        foreach(string path in paths)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(prefab==null) { errors.Add("Final prefab missing: "+path); continue; }
            foreach(var transform in prefab.GetComponentsInChildren<Transform>(true))
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)>0)
                    errors.Add("Missing script: "+path+" / "+transform.name);
        }
        foreach(string path in new[]{UrbanEquationScreensSetup.LobbyPath,UrbanEquationScreensSetup.GamePath})
            if(string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path))) errors.Add("Scene GUID is missing: "+path);
        return errors;
    }

    public static void ValidateStageCatalog(StageCatalog catalog,List<string> errors)
    {
        if(catalog==null || catalog.Count<2)
        { errors.Add("Final content requires the existing Stage1 and Stage2 definitions."); return; }
        // Validate every registered stage while preserving the shipped tutorial contracts.
        catalog.Validate(errors);
        if(!catalog.TryGetStage(1,out StageData first) || first.Width!=2 || first.Height!=2
            || first.BuildingCards.Sum(x=>x==null?0:x.Count)!=2) errors.Add("Stage1 must retain its 2x2 board and two building cards.");
        if(!catalog.TryGetStage(2,out StageData second) || second.Width!=3 || second.Height!=3
            || second.BuildingCards.Sum(x=>x==null?0:x.Count)!=5) errors.Add("Stage2 must retain its 3x3 board and five building cards.");
    }
    [MenuItem("Tools/Urban Equation/Phase 4D/2 Open Lobby for Final QA")]
    private static void OpenLobby()
    {
        if(!ValidateForPreview() || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(UrbanEquationScreensSetup.LobbyPath);
    }
    [MenuItem("Tools/Urban Equation/Phase 4D/3 Multi Combo QA Preview")]
    private static void OpenMultiCombo()
    {
        if(!ValidateForPreview() || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        SessionState.SetBool(PreviewKey,true); EditorApplication.isPlaying=true;
    }
    private static bool ValidateForPreview()
    { var errors=CollectValidationErrors(); if(errors.Count==0) return true; Debug.LogError(string.Join("\n",errors)); return false; }

    // Memory clones only. Caller owns/disposes all three returned objects.
    public static GameContentData CreateMultiComboContent(GameContentData source,out StageCatalog catalog,out StageData stage)
    {
        catalog=null; stage=null;
        if(source==null || source.Stages==null || !source.Stages.TryGetStage(1,out StageData first)
            || first.Width!=2 || first.Height!=2 || first.BuildingCards.Count!=1
            || first.BuildingCards[0]==null || first.BuildingCards[0].Building==null || first.BuildingCards[0].Building.BuildingCode!=11001)
            throw new ArgumentException("Multi-combo QA requires the authored Stage1 small-house definition.");
        GameContentData content=null;
        try
        {
            stage=UnityEngine.Object.Instantiate(first); stage.name="QA ONLY - Multi Combo Stage";
            var s=new SerializedObject(stage); s.FindProperty("stageName").stringValue="QA: 복수 콤보";
            s.FindProperty("introduction").stringValue="임시 검증용: 원본 Stage1 데이터는 변경하지 않습니다.";
            s.FindProperty("buildingCards").GetArrayElementAtIndex(0).FindPropertyRelative("count").intValue=3;
            var resources=s.FindProperty("initialResources");
            for(int i=0;i<resources.arraySize;i++)
            { var item=resources.GetArrayElementAtIndex(i); item.FindPropertyRelative("amount").intValue=item.FindPropertyRelative("resource").enumValueIndex==(int)ResourceType.Jobs?3:0; }
            s.ApplyModifiedPropertiesWithoutUndo();
            catalog=UnityEngine.Object.Instantiate(source.Stages); catalog.name="QA ONLY - Stage Catalog";
            var stages=new SerializedObject(catalog); stages.FindProperty("stages").GetArrayElementAtIndex(0).objectReferenceValue=stage; stages.ApplyModifiedPropertiesWithoutUndo();
            content=UnityEngine.Object.Instantiate(source); content.name="QA ONLY - Game Content";
            UrbanEquationPrefabFactory.Reference(content,"stageCatalog",catalog);
            var errors=new List<string>(); content.Validate(errors); if(errors.Count>0) throw new InvalidOperationException(string.Join("; ",errors));
            return content;
        }
        catch
        { if(content!=null) UnityEngine.Object.DestroyImmediate(content); if(catalog!=null) UnityEngine.Object.DestroyImmediate(catalog); if(stage!=null) UnityEngine.Object.DestroyImmediate(stage); catalog=null;stage=null;throw; }
    }
    private static void HandlePlayMode(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PreviewKey,false))
        {
            SessionState.SetBool(PreviewKey,false);
            try { StartPreview(); }
            catch(Exception exception) { Cleanup(); Debug.LogError("Multi-combo QA preview failed: "+exception.Message); }
        }
        if(state==PlayModeStateChange.ExitingPlayMode) Cleanup();
        if(state==PlayModeStateChange.EnteredEditMode) SessionState.SetBool(PreviewKey,false);
    }
    private static void StartPreview()
    {
        var source=AssetDatabase.LoadAssetAtPath<GameContentData>("Assets/_UrbanEquation/Data/GameContent.asset");
        var hudSet=AssetDatabase.LoadAssetAtPath<GameplayHudSet>(UrbanEquationHudSetup.SetPath);
        previewContent=CreateMultiComboContent(source,out previewCatalog,out previewStage);
        previewDirectory=Path.Combine(Application.temporaryCachePath,"UrbanEquationPhase4D",Guid.NewGuid().ToString("N"));
        previewRoot=new GameObject("QA ONLY - Multi Combo Preview");
        var bootstrap=previewRoot.AddComponent<GameBootstrap>();
        if(!bootstrap.TryInitialize(previewContent,Path.Combine(previewDirectory,"progress.json"),true,out string error)) throw new InvalidOperationException(error);
        var camera=new GameObject("QA Camera").AddComponent<Camera>();camera.transform.SetParent(previewRoot.transform,false);camera.tag="MainCamera";
        camera.transform.position=new Vector3(6,8,-5);camera.transform.LookAt(new Vector3(.5f,0,.5f));camera.orthographic=true;camera.orthographicSize=4;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.15f,.18f);
        var light=new GameObject("QA Light").AddComponent<Light>();light.transform.SetParent(previewRoot.transform,false);light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(50,-30,0);
        var events=new GameObject("QA EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.transform.SetParent(previewRoot.transform,false);
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        var placement=previewRoot.AddComponent<BuildingPlacementController>();placement.Configure(bootstrap.Session,camera);
        var hud=UnityEngine.Object.Instantiate(hudSet.HudPrefab,previewRoot.transform);hud.Bind(bootstrap.Session,bootstrap.Flow,placement,camera);
        var debug=bootstrap.GetComponentInChildren<GameFlowDebugUI>();debug.SetManualPlacement(false);debug.SetGameplayPanelVisible(false);
        if(!bootstrap.Flow.TryPlay(out error) || !bootstrap.Flow.TryDismissIntro(out error)) throw new InvalidOperationException(error);
        Debug.Log("Phase4-D multi-combo QA: 주택 (0,1) → (1,0) → (1,1). 마지막 건설에서 두 콤보가 순차 표시되며 인구5/일자리0입니다. Undo → 인구2/일자리1/카드1개; 다시 건설 → 두 콤보 재표시. Stop 후 원본 Stage1은 그대로입니다.");
    }
    private static void Cleanup()
    {
        if(previewRoot!=null) { previewRoot.SetActive(false); UnityEngine.Object.DestroyImmediate(previewRoot); }
        if(previewContent!=null) UnityEngine.Object.DestroyImmediate(previewContent);
        if(previewCatalog!=null) UnityEngine.Object.DestroyImmediate(previewCatalog);
        if(previewStage!=null) UnityEngine.Object.DestroyImmediate(previewStage);
        previewRoot=null;previewContent=null;previewCatalog=null;previewStage=null;
        if(!string.IsNullOrEmpty(previewDirectory))
        { try { if(Directory.Exists(previewDirectory)) Directory.Delete(previewDirectory,true); } catch(IOException) {} catch(UnauthorizedAccessException) {} }
        previewDirectory=null;
    }
}
