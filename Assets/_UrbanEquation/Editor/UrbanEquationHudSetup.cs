using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class UrbanEquationHudSetup
{
    private const string Root = "Assets/_UrbanEquation/";
    public const string HudPath = Root + "Prefabs/UI/Gameplay/Pfb_Main_GameHud_001.prefab";
    public const string IconsPath = Root + "Data/Presentation/ResourceIcons.asset";
    public const string SetPath = Root + "Data/Presentation/GameplayHud.asset";
    private const string Gui = "Assets/Space_Exploration_GUI_Kit/";
    private static readonly string[] IconNames = { "Population", "Job", "Funds", "Goods", "Tourism" };
    private static readonly string[] ArtPaths = {
        Gui+"Containers/Extra Large/pause-container-extra-large.png",
        Gui+"Containers/Large/setting-container-large.png",
        Gui+"Button_Images/Source_Image_Sprites/large/medium-pink-large.png",
        Gui+"Button_Images/Source_Image_Sprites/large/medium-blue-large.png",
        Gui+"Button_Images/Source_Image_Sprites/extra large/square-pink-extra-large.png",
        Gui+"Button_Images/Source_Image_Sprites/extra large/square-blue-extra-large.png",
        Gui+"Button_Images/Source_Image_Sprites/large/medium-pink-large.png",
        Gui+"Icons/star-64.png"
    };
    [MenuItem("Tools/Urban Equation/Phase 4B/1 Create Missing HUD and Bind Icons")]
    private static void CreateMenu()
    {
        if (TryCreateMissing(out string error)) Debug.Log("Phase4-B HUD 생성·연결 완료. 메뉴2 Validate HUD Setup을 실행해 주세요.");
        else Debug.LogError(error);
    }
    [MenuItem("Tools/Urban Equation/Phase 4B/1 Create Missing HUD and Bind Icons", true)]
    private static bool CanCreate() => !EditorApplication.isPlayingOrWillChangePlaymode;
    public static bool TryCreateMissing(out string error)
    {
        error = null;
        if (EditorApplication.isPlayingOrWillChangePlaymode) { error="Stop Play before HUD setup."; return false; }
        var errors=UrbanEquationPrefabSetup.CollectValidationErrors();
        var common=Load<GameplayPrefabSet>(UrbanEquationPrefabSetup.PrefabSetPath);
        var font=Load<TMP_FontAsset>(Root+"Fonts/NotoSansKR-Medium SDF.asset");
        if (font==null) errors.Add("NotoSansKR-Medium SDF font is missing.");
        var art=ArtPaths.Select(SpriteAt).ToArray();
        for(int i=0;i<art.Length;i++) if(art[i]==null) errors.Add("Required GUI sprite is missing: "+ArtPaths[i]);
        for(int i=0;i<5;i++) for(int color=1;color<=3;color++)
            if(!(AssetImporter.GetAtPath(IconPath(i,color)) is TextureImporter)) errors.Add("Resource image is missing: "+IconPath(i,color));
        CheckDestination(IconsPath,typeof(ResourceIconSet),errors); CheckDestination(SetPath,typeof(GameplayHudSet),errors);
        var existing=AssetDatabase.LoadMainAssetAtPath(HudPath);
        if(existing!=null && (!(existing is GameObject root) || root.GetComponent<GameHudUI>()==null)) errors.Add("HUD destination has another asset type.");
        if(errors.Count>0) { error=string.Join("\n",errors); return false; }
        try
        {
            var icons=Load<ResourceIconSet>(IconsPath);
            if(icons==null) { icons=ScriptableObject.CreateInstance<ResourceIconSet>(); AssetDatabase.CreateAsset(icons,IconsPath); }
            var serialized=new SerializedObject(icons);
            for(int color=1;color<=3;color++)
            {
                var property=serialized.FindProperty(color==1?"neutral":color==2?"gained":"spent"); property.arraySize=5;
                for(int i=0;i<5;i++) property.GetArrayElementAtIndex(i).objectReferenceValue=ImportSprite(IconPath(i,color));
            }
            serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(icons); AssetDatabase.SaveAssetIfDirty(icons);
            var hud=Load<GameObject>(HudPath);
            if(hud==null)
            {
                var created=UrbanEquationHudFactory.CreateHud(common,icons,font,art[0],art[1],art[2],art[3],art[4],art[5],art[6],art[7]);
                try
                {
                    hud=PrefabUtility.SaveAsPrefabAsset(created,HudPath,out bool success);
                    if(!success || hud==null) throw new InvalidOperationException("Could not save HUD prefab.");
                }
                finally { UnityEngine.Object.DestroyImmediate(created); }
            }
            var set=Load<GameplayHudSet>(SetPath);
            if(set==null) { set=ScriptableObject.CreateInstance<GameplayHudSet>(); AssetDatabase.CreateAsset(set,SetPath); }
            UrbanEquationPrefabFactory.Reference(set,"hudPrefab",hud.GetComponent<GameHudUI>());
            UrbanEquationPrefabFactory.Reference(set,"resourceIcons",icons); EditorUtility.SetDirty(set); AssetDatabase.SaveAssetIfDirty(set);
            errors=CollectValidationErrors(); if(errors.Count>0) { error=string.Join("\n",errors); return false; }
            return true;
        }
        catch(Exception exception)
        { error="HUD setup failed: "+exception.Message+"\nCompleted assets were retained; resolve the error and run menu1 again."; return false; }
    }
    [MenuItem("Tools/Urban Equation/Phase 4B/2 Validate HUD Setup")]
    private static void ValidateMenu()
    {
        var errors=CollectValidationErrors();
        if(errors.Count==0) Debug.Log("Phase4-B HUD Validation OK (resources / goals / hand / buttons / tooltip / combo views).");
        else Debug.LogError(string.Join("\n",errors));
    }
    public static List<string> CollectValidationErrors()
    {
        var errors=UrbanEquationPrefabSetup.CollectValidationErrors();
        var set=Load<GameplayHudSet>(SetPath);
        if(set==null) errors.Add("GameplayHud.asset has not been generated."); else set.Validate(errors);
        var root=Load<GameObject>(HudPath); var hud=root==null?null:root.GetComponent<GameHudUI>();
        if(hud==null) { errors.Add("Pfb_Main_GameHud_001.prefab is missing."); return errors; }
        var canvas=root.GetComponent<Canvas>();
        if(canvas==null || canvas.renderMode!=RenderMode.ScreenSpaceOverlay) errors.Add("HUD requires an overlay Canvas.");
        CheckFields(hud,new[]{"gameplayRoot","cityResources","stageTitle","hand","cardContent","commonPrefabs","undoView","undoButton",
            "nextView","nextButton","pauseButton","placementFeedback","tooltip","automaticCombo","replayCombo","boardDetails"},errors);
        var serialized=new SerializedObject(hud); var goals=serialized.FindProperty("goals");
        if(goals==null || goals.arraySize!=3) errors.Add("HUD requires three goal rows.");
        else for(int i=0;i<3;i++)
        {
            var row=goals.GetArrayElementAtIndex(i).objectReferenceValue as StageGoalRowUI;
            if(row==null) errors.Add("Goal row is missing: "+i); else CheckFields(row,new[]{"description","background","achievedSprite","pendingSprite"},errors);
        }
        foreach(var tooltip in root.GetComponentsInChildren<BuildingTooltipUI>(true))
            CheckFields(tooltip,new[]{"buildingName","allowedTiles","gained","spent","canvasGroup"},errors);
        foreach(var popup in root.GetComponentsInChildren<ComboPopupUI>(true))
            CheckFields(popup,new[]{"comboNameText","descriptionText","rewardsView","canvasGroup"},errors);
        foreach(var strip in root.GetComponentsInChildren<ResourceStripUI>(true)) CheckFields(strip,new[]{"icons","font"},errors);
        if(set!=null && (set.HudPrefab!=hud || AssetDatabase.GetAssetPath(set.ResourceIcons)!=IconsPath)) errors.Add("HUD set references are inconsistent.");
        return errors;
    }
    private static void CheckFields(UnityEngine.Object target,string[] names,List<string> errors)
    {
        var serialized=new SerializedObject(target);
        foreach(string name in names) if(serialized.FindProperty(name)?.objectReferenceValue==null) errors.Add(target.name+" has a missing reference: "+name);
    }
    private static void CheckDestination(string path,Type type,List<string> errors)
    { var asset=AssetDatabase.LoadMainAssetAtPath(path); if(asset!=null && !type.IsInstanceOfType(asset)) errors.Add("Destination has another asset type: "+path); }
    private static string IconPath(int resource,int color) => Root+"Sprite/Icon/Spr_Ui_I"+IconNames[resource]+"_00"+color+".png";
    private static T Load<T>(string path) where T:UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);
    private static Sprite SpriteAt(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    private static Sprite ImportSprite(string path)
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        if(importer.textureType!=TextureImporterType.Sprite || importer.spriteImportMode!=SpriteImportMode.Single)
        {
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.mipmapEnabled=false; importer.alphaIsTransparency=true; importer.SaveAndReimport();
        }
        var sprite=SpriteAt(path); if(sprite==null) throw new InvalidOperationException("Resource sprite import failed: "+path); return sprite;
    }
}
