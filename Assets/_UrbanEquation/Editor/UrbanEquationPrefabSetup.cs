using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class UrbanEquationPrefabSetup
{
    public const string Root = "Assets/_UrbanEquation/";
    public const string PrefabSetPath = Root + "Data/Presentation/GameplayPrefabs.asset";
    public const string CardPath = Root + "Prefabs/UI/Cards/BuildingCard.prefab";
    public const string ComboPath = Root + "Prefabs/UI/Gameplay/ComboPopup.prefab";
    public const string BuildingRootPath = Root + "Prefabs/Gameplay/Buildings/BuildingRoot.prefab";
    private const string Models = "Assets/polyperfect/Low Poly Ultimate Pack/_T/Prefabs_T/";
    private const string Gui = "Assets/Space_Exploration_GUI_Kit/Containers/";
    private static readonly string[] TileNames = { "Asphalt", "Concrete", "Grass" };
    // B23001 follows the document filename, explicitly confirmed by the user.
    private static readonly string[,] BuildingRows = {
        { "11001", "SmallHouse", "Building_House_Block", "SHouse" },
        { "12001", "MediumHouse", "Building_House_Family_Small", "MHouse" },
        { "13001", "LargeHouse", "Building_House_Middle", "BHouse" },
        { "21001", "SmallOffice", "Building_Office_Rounded", "SOffice" },
        { "22001", "MediumOffice", "Building_Office", "MOffice" },
        { "23001", "LargeOffice", "Building_House_Block", "BOffice" },
        { "31001", "Restaurant", "Building_Restaurant", "SRestaurant" },
        { "32001", "Cafe", "Building_Cafe", "MCafe" },
        { "33001", "Casino", "Building_Casino", "BCasino" },
        { "41001", "SmallFactory", "Industry_Storage", "SFactory" },
        { "42001", "MediumFactory", "Industry_Factory_Old", "MFactory" },
        { "43001", "LargeFactory", "Incineration_Plant", "BFactory" },
        { "51001", "Lighthouse", "Lighthouse", "SLighthouse" },
        { "52001", "Temple", "Building_Temple_China", "MTemple" },
        { "53001", "Stadium", "Building_Stadium", "BStadium" }
    };

    [MenuItem("Tools/Urban Equation/Phase 4A/1 Create Missing Prefabs and Bind Data")]
    private static void Create()
    {
        if (TryCreateMissing(out string error)) Debug.Log("Phase4-A 생성·연결 완료. 2 Validate Prefab Setup을 실행해 주세요.");
        else Debug.LogError(error);
    }
    [MenuItem("Tools/Urban Equation/Phase 4A/1 Create Missing Prefabs and Bind Data", true)]
    private static bool CanCreate() => !EditorApplication.isPlayingOrWillChangePlaymode;

    public static bool TryCreateMissing(out string error)
    {
        error = null;
        if (EditorApplication.isPlayingOrWillChangePlaymode) { error = "Stop Play before prefab setup."; return false; }
        var content = Load<GameContentData>(Root + "Data/GameContent.asset");
        var font = Load<TMP_FontAsset>(Root + "Fonts/NotoSansKR-Medium SDF.asset");
        var tileTemplate = Load<GameObject>(Root + "Prefabs/Gameplay/Tiles/Tile.prefab");
        var buildingTemplate = Load<GameObject>(Root + "Prefabs/Gameplay/Buildings/Building.prefab");
        var errors = new List<string>();
        if (content == null || font == null || tileTemplate == null || buildingTemplate == null)
            errors.Add("Required GameContent/font/legacy template assets are missing.");
        if (content != null) content.Validate(errors);
        var surfaces = new GameObject[3]; var tiles = new TileData[3];
        var models = new GameObject[15]; var buildings = new BuildingData[15];
        for (int i = 0; i < 3; i++)
        {
            surfaces[i] = Load<GameObject>(Models + "Tiles_T/Tile_Plain_" + TileNames[i] + ".prefab");
            tiles[i] = Load<TileData>(Root + "Data/Tiles/T" + (i+1).ToString("00000") + "_" + TileNames[i] + ".asset");
            if (surfaces[i] == null || tiles[i] == null) errors.Add("Missing tile input: " + TileNames[i]);
            CheckExisting(TilePath(i), typeof(Tile), errors);
        }
        for (int i = 0; i < 15; i++)
        {
            models[i] = Load<GameObject>(Models + "Buildings_T/" + BuildingRows[i,2] + ".prefab");
            buildings[i] = Load<BuildingData>(Root + "Data/Buildings/B" + BuildingRows[i,0] + "_" + BuildingRows[i,1] + ".asset");
            if (models[i] == null || buildings[i] == null) errors.Add("Missing building input: " + BuildingRows[i,0]);
            else if (content != null && content.Buildings != null && (!content.Buildings.TryGetBuilding(int.Parse(BuildingRows[i,0]), out BuildingData canonical)
                || canonical != buildings[i])) errors.Add("Building asset is outside the catalog: " + BuildingRows[i,0]);
            if (!(AssetImporter.GetAtPath(BuildingImage(i)) is TextureImporter)) errors.Add("Missing building image: " + BuildingImage(i));
        }
        CheckExisting(CardPath,typeof(BuildingCardUI),errors);
        CheckExisting(ComboPath,typeof(ComboPopupUI),errors);
        CheckExisting(BuildingRootPath,typeof(BuildingInstance),errors);
        if (AssetDatabase.LoadMainAssetAtPath(PrefabSetPath) != null && Load<GameplayPrefabSet>(PrefabSetPath) == null)
            errors.Add("Prefab set destination contains another asset type.");
        Sprite cardBackground = FirstSprite(Gui + "Extra Large/loot-container-extra-large.png");
        Sprite comboBackground = FirstSprite(Gui + "Large/loot-container-large.png");
        string comboImage = Root + "Sprite/UI/Spr_Ui_Combo_001.png";
        if (cardBackground == null || comboBackground == null) errors.Add("GUI kit background sprites are missing.");
        if (!(AssetImporter.GetAtPath(comboImage) is TextureImporter)) errors.Add("Combo image is missing.");
        if (errors.Count != 0) { error = string.Join("\n",errors); return false; }
        try
        {
            // Change only these sixteen project-owned image importers; never the external pack.
            var images = new Sprite[15];
            for (int i = 0; i < 15; i++) images[i] = ImportSprite(BuildingImage(i));
            Sprite comboHeader = ImportSprite(comboImage);
            float buildingHeight = 0;
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var prefab = GetOrCreate(TilePath(i), () => UrbanEquationPrefabFactory.CreateTile(tileTemplate,surfaces[index],"Pfb_Tile_"+TileNames[index]+"_001"));
                BindAsset(tiles[i],"tilePrefab",prefab.GetComponent<Tile>());
                buildingHeight = Mathf.Max(buildingHeight,MeasureSurfaceTop(prefab));
            }
            var buildingRoot = GetOrCreate(BuildingRootPath, () => UrbanEquationPrefabFactory.CreateBuildingRoot(buildingTemplate,buildingHeight));
            var card = GetOrCreate(CardPath, () => UrbanEquationPrefabFactory.CreateCard(cardBackground,font));
            var combo = GetOrCreate(ComboPath, () => UrbanEquationPrefabFactory.CreateCombo(comboBackground,comboHeader,font));
            var set = Load<GameplayPrefabSet>(PrefabSetPath);
            if (set == null)
            { Folder("Assets/_UrbanEquation/Data/Presentation"); set = ScriptableObject.CreateInstance<GameplayPrefabSet>(); AssetDatabase.CreateAsset(set,PrefabSetPath); }
            BindAsset(set,"buildingCardPrefab",card.GetComponent<BuildingCardUI>());
            BindAsset(set,"comboPopupPrefab",combo.GetComponent<ComboPopupUI>());
            BindAsset(content,"buildingPrefab",buildingRoot.GetComponent<BuildingInstance>());
            for (int i = 0; i < 15; i++)
            { BindAsset(buildings[i],"visualPrefab",models[i]); BindAsset(buildings[i],"cardImage",images[i]); }
            errors = CollectValidationErrors();
            if (errors.Count != 0) { error = string.Join("\n",errors); return false; }
            return true;
        }
        catch (Exception exception)
        { error = "Prefab setup failed: " + exception.Message + "\nCompleted assets were retained; resolve the error and run Create Missing again."; return false; }
    }

    [MenuItem("Tools/Urban Equation/Phase 4A/2 Validate Prefab Setup")]
    private static void ValidateMenu()
    {
        var errors = CollectValidationErrors();
        if (errors.Count == 0) Debug.Log("Phase4-A Prefab Validation OK (3 tiles / building root / 15 visuals+cards / card+combo views).");
        else Debug.LogError(string.Join("\n",errors));
    }
    public static List<string> CollectValidationErrors()
    {
        var errors = new List<string>(); var content = Load<GameContentData>(Root + "Data/GameContent.asset");
        if (content == null) errors.Add("GameContent is missing."); else content.Validate(errors);
        var set = Load<GameplayPrefabSet>(PrefabSetPath);
        if (set == null) errors.Add("GameplayPrefabs.asset has not been generated."); else set.Validate(errors);
        var unique = new HashSet<Tile>();
        for (int i = 0; i < 3; i++)
        {
            var tile = Load<TileData>(Root + "Data/Tiles/T" + (i+1).ToString("00000") + "_" + TileNames[i] + ".asset");
            var prefab = Load<GameObject>(TilePath(i));
            if (tile == null || prefab == null || tile.TileType != (TileType)i || tile.TilePrefab != prefab.GetComponent<Tile>())
                errors.Add("Tile data/prefab binding is missing or inconsistent: " + TileNames[i]);
            else
            {
                if (!unique.Add(tile.TilePrefab)) errors.Add("Tile types must reference different prefab assets.");
                Collider collider = prefab.GetComponent<Collider>();
                if (collider == null || !collider.enabled || collider.isTrigger || prefab.transform.Find("Surface") == null)
                    errors.Add("Tile requires its collider and Surface: " + TileNames[i]);
                CheckView(TilePath(i),typeof(Tile),new[]{"highlight"},errors);
            }
        }
        if (content != null)
        {
            if (content.BuildingPrefab == null || AssetDatabase.GetAssetPath(content.BuildingPrefab) != BuildingRootPath)
                errors.Add("GameContent must reference the new BuildingRoot prefab.");
            if (content.Buildings != null) foreach (BuildingData building in content.Buildings.Buildings)
                if (building != null && (building.VisualPrefab == null || building.CardImage == null))
                    errors.Add("Visual/card image is missing: " + building.Code);
        }
        CheckView(CardPath,typeof(BuildingCardUI),new[]{"buildingNameText","cardImage","disabledOverlay","button"},errors);
        CheckView(ComboPath,typeof(ComboPopupUI),new[]{"comboNameText","descriptionText","rewardsText","canvasGroup"},errors);
        return errors;
    }

    public static string TilePath(int index) => Root + "Prefabs/Gameplay/Tiles/Pfb_Tile_" + TileNames[index] + "_001.prefab";
    private static string BuildingImage(int i) => Root + "Sprite/Building_Image/Spr_Ui_" + BuildingRows[i,3] + "_001.png";
    private static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);
    private static Sprite FirstSprite(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    private static Sprite ImportSprite(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.SaveAndReimport();
        }
        Sprite sprite = FirstSprite(path);
        if (sprite == null) throw new InvalidOperationException("Sprite import failed: " + path);
        return sprite;
    }
    private static void CheckExisting(string path, Type component, List<string> errors)
    {
        var existing = AssetDatabase.LoadMainAssetAtPath(path);
        if (existing != null && (!(existing is GameObject prefab) || prefab.GetComponent(component) == null))
            errors.Add("Destination asset has the wrong type: " + path);
    }
    private static void CheckView(string path, Type component, string[] fields, List<string> errors)
    {
        var root = Load<GameObject>(path); var view = root == null ? null : root.GetComponent(component);
        if (view == null) { errors.Add("View prefab is missing: " + path); return; }
        var serialized = new SerializedObject(view);
        foreach (string field in fields) if (serialized.FindProperty(field)?.objectReferenceValue == null)
            errors.Add(path + " has a missing reference: " + field);
    }
    private static void BindAsset(UnityEngine.Object target, string field, UnityEngine.Object reference)
    {
        UrbanEquationPrefabFactory.Reference(target,field,reference);
        EditorUtility.SetDirty(target); AssetDatabase.SaveAssetIfDirty(target);
    }
    private static float MeasureSurfaceTop(GameObject prefab)
    {
        var instance = UnityEngine.Object.Instantiate(prefab);
        try
        {
            Transform surface = instance.transform.Find("Surface");
            if (surface == null) throw new InvalidOperationException("Tile prefab has no Surface.");
            float top = 0;
            foreach (Renderer renderer in surface.GetComponentsInChildren<Renderer>(true))
                top = Mathf.Max(top,renderer.bounds.max.y - instance.transform.position.y);
            return top;
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
    }
    private static GameObject GetOrCreate(string path, Func<GameObject> construct)
    {
        var existing = Load<GameObject>(path); if (existing != null) return existing;
        Folder(System.IO.Path.GetDirectoryName(path).Replace('\\','/'));
        var root = construct();
        try
        {
            if (PrefabUtility.IsPartOfPrefabInstance(root))
                PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root,path,out bool success);
            if (!success || prefab == null) throw new InvalidOperationException("Could not save prefab: " + path);
            return prefab;
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    private static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\','/'); Folder(parent);
        AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(path));
    }
}
