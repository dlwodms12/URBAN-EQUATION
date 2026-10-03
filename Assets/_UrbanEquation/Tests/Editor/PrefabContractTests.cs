using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UObject = UnityEngine.Object;

public class PrefabContractTests
{
    private readonly List<UObject> objects = new List<UObject>();
    private string directory;
    private static Type Runtime(string name) => Type.GetType(name+", Assembly-CSharp",true);
    private static Type Editor(string name) => Type.GetType(name+", Assembly-CSharp-Editor",true);
    private static object Get(object target,string name) => target.GetType().GetProperty(name).GetValue(target);
    private static object Field(object target,string name) => target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
    private static void Set(object target,string name,object value) => target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    private static object Call(object target,string name,params object[] args) => target.GetType().GetMethod(name).Invoke(target,args);
    private static object Factory(string method,params object[] args) => Editor("UrbanEquationPrefabFactory").GetMethod(method).Invoke(null,args);
    private static void Life(object target,string name) => target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
    private static object[] Items(object value) => ((IEnumerable)value).Cast<object>().ToArray();
    private T Own<T>(T obj) where T:UObject { objects.Add(obj);return obj; }
    private GameObject Root(string name) => Own(new GameObject(name));
    private Sprite Sprite()
    { var texture=Own(new Texture2D(8,8));return Own(UnityEngine.Sprite.Create(texture,new Rect(0,0,8,8),new Vector2(.5f,.5f))); }
    private static UObject Font()
    {
        Type type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("TMPro.TMP_FontAsset",false)).First(x=>x!=null);
        return AssetDatabase.LoadAssetAtPath("Assets/_UrbanEquation/Fonts/NotoSansKR-Medium SDF.asset",type);
    }
    private Component Card()
    { return Own((GameObject)Factory("CreateCard",Sprite(),Font())).GetComponent(Runtime("BuildingCardUI")); }
    private Component Popup()
    { return Own((GameObject)Factory("CreateCombo",Sprite(),Sprite(),Font())).GetComponent(Runtime("ComboPopupUI")); }
    private Component Bootstrap(bool threeCards=false)
    {
        var component=Root("Prefab contract session").AddComponent(Runtime("GameBootstrap"));
        var content=AssetDatabase.LoadAssetAtPath("Assets/_UrbanEquation/Data/GameContent.asset",Runtime("GameContentData"));
        if(threeCards)
        {
            content=Own(UObject.Instantiate(content));
            var catalog=Own(UObject.Instantiate((UObject)Get(content,"Stages")));
            var first=Own(UObject.Instantiate((UObject)Items(Get(catalog,"Stages"))[0]));
            var entries=Array.CreateInstance(Runtime("StageBuildingCardData"),1);
            var entry=Activator.CreateInstance(Runtime("StageBuildingCardData"));
            Set(entry,"building",Get(Items(Get(first,"BuildingCards"))[0],"Building"));Set(entry,"count",3);entries.SetValue(entry,0);
            Set(first,"buildingCards",entries);
            var amounts=Array.CreateInstance(Runtime("ResourceAmount"),5);
            for(int i=0;i<5;i++)amounts.SetValue(Activator.CreateInstance(Runtime("ResourceAmount"),Enum.ToObject(Runtime("ResourceType"),i),i==1?3:0),i);
            Set(first,"initialResources",amounts);
            var stages=Array.CreateInstance(Runtime("StageData"),2);stages.SetValue(first,0);stages.SetValue(Items(Get(catalog,"Stages"))[1],1);
            Set(catalog,"stages",stages);Set(content,"stageCatalog",catalog);
        }
        object[] args={content,Path.Combine(directory,Guid.NewGuid().ToString("N")+".json"),false,null};
        Assert.That(Call(component,"TryInitialize",args),Is.True,args[3] as string);
        Command(Get(component,"Flow"),"TryPlay");Command(Get(component,"Flow"),"TryDismissIntro");return component;
    }
    private static void Command(object target,string method)
    { object[] args={null};Assert.That(Call(target,method,args),Is.True,args[0] as string); }
    private static void Build(object session,int id,int x,int y)
    { object[] args={id,new Vector2Int(x,y),null,null};Assert.That(Call(session,"TryCommitBuild",args),Is.True,args[3] as string); }
    private static object Combo(object bootstrap)
    {
        object session=Get(bootstrap,"Session");Build(session,1,0,0);Build(session,2,1,0);
        return Get(session,"Combos");
    }
    private GameObject TileTemplate()
    {
        var template=Root("Tile template");template.AddComponent<BoxCollider>();
        var tile=template.AddComponent(Runtime("Tile"));var highlight=Root("Highlight");
        highlight.transform.SetParent(template.transform,false);Set(tile,"highlight",highlight);
        var baseObject=Own(GameObject.CreatePrimitive(PrimitiveType.Cube));baseObject.name="Road base";
        baseObject.transform.SetParent(template.transform,false);baseObject.transform.localScale=new Vector3(1,.1f,1);
        return template;
    }
    private GameObject Surface()
    {
        var surface=Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
        surface.transform.localScale=new Vector3(10,1,10);return surface;
    }
    [SetUp] public void SetUp() => directory=Path.Combine(Path.GetTempPath(),"UE-Prefabs-"+Guid.NewGuid().ToString("N"));
    [TearDown] public void TearDown()
    {
        for(int i=objects.Count-1;i>=0;i--)if(objects[i]!=null)UObject.DestroyImmediate(objects[i]);objects.Clear();
        if(Directory.Exists(directory))Directory.Delete(directory,true);
    }

    [Test] public void CardFactoryWiresAllFourReferences()
    {
        var card=Card();foreach(string name in new[]{"buildingNameText","cardImage","disabledOverlay","button"})Assert.That(Field(card,name),Is.Not.Null);
        Assert.That(((RectTransform)card.transform).sizeDelta,Is.EqualTo(new Vector2(110,140)));
    }
    [Test] public void CardOverlayIsLastSiblingAndHalfBlackWithoutRaycasts()
    {
        var card=Card();var overlay=(Image)Field(card,"disabledOverlay");
        Assert.That(overlay.color,Is.EqualTo(new Color(0,0,0,.5f)));Assert.That(overlay.raycastTarget,Is.False);
        Assert.That(overlay.transform.GetSiblingIndex(),Is.EqualTo(card.transform.childCount-1));
    }
    [Test] public void CardDecorationDoesNotInterceptPointerEvents()
    {
        var card=Card();Assert.That(((Image)Field(card,"cardImage")).raycastTarget,Is.False);
        Assert.That(Get(Field(card,"buildingNameText"),"raycastTarget"),Is.False);
        Assert.That(card.GetComponent<Image>().raycastTarget,Is.True);
    }
    [Test] public void CardBindUsesShippedBuildingNameAndImage()
    {
        var bootstrap=Bootstrap();var session=Get(bootstrap,"Session");var hand=Get(session,"Hand");var state=Items(Get(hand,"Cards"))[0];
        var card=Card();Call(card,"Bind",state,hand,null);
        Assert.That(Get(Field(card,"buildingNameText"),"text"),Is.EqualTo(Get(Get(state,"Building"),"BuildingName")));
        Assert.That(((Image)Field(card,"cardImage")).sprite,Is.EqualTo(Get(Get(state,"Building"),"CardImage")));
        Assert.That(card.GetComponent<Button>().interactable,Is.True);
    }
    [Test] public void ConsumedCardCannotRespondAndDisplaysOverlay()
    {
        var bootstrap=Bootstrap();var session=Get(bootstrap,"Session");var hand=Get(session,"Hand");
        var card=Card();Call(card,"Bind",Items(Get(hand,"Cards"))[0],hand,null);Build(session,1,0,0);Call(card,"RefreshAvailability");
        Assert.That(Get(card,"IsAvailable"),Is.False);Assert.That(card.GetComponent<Button>().interactable,Is.False);
        Assert.That(((Image)Field(card,"disabledOverlay")).gameObject.activeSelf,Is.True);
    }
    [Test] public void ResourceChangeRefreshesOverlayForRemainingCard()
    {
        var bootstrap=Bootstrap();var session=Get(bootstrap,"Session");var hand=Get(session,"Hand");
        var card=Card();Call(card,"Bind",Items(Get(hand,"Cards"))[1],hand,null);
        var resources=Get(session,"Resources");Call(resources,"Consume",Enum.ToObject(Runtime("ResourceType"),1),2);Call(card,"RefreshAvailability");
        Assert.That(card.GetComponent<Button>().interactable,Is.False);
        Call(resources,"Add",Enum.ToObject(Runtime("ResourceType"),1),1);Call(card,"RefreshAvailability");
        Assert.That(card.GetComponent<Button>().interactable,Is.True);Assert.That(((Image)Field(card,"disabledOverlay")).gameObject.activeSelf,Is.False);
    }
    [Test] public void CardFactoryRejectsMissingRequiredArt()
    {
        Assert.Throws<TargetInvocationException>(()=>Factory("CreateCard",null,Font()));
        Assert.Throws<TargetInvocationException>(()=>Factory("CreateCard",Sprite(),null));
    }
    [Test] public void TileFactoryPreservesRoadHighlightAndCollider()
    {
        var source=TileTemplate();var surface=Surface();var tile=Own((GameObject)Factory("CreateTile",source,surface,"Grass"));
        Assert.That(tile.GetComponent(Runtime("Tile")),Is.Not.Null);Assert.That(tile.GetComponent<BoxCollider>(),Is.Not.Null);
        Assert.That(tile.transform.Find("Road base"),Is.Not.Null);Assert.That(tile.transform.Find("Highlight").gameObject.activeSelf,Is.False);
        Assert.That(source.transform.childCount,Is.EqualTo(2));
    }
    [Test] public void TileSurfaceFitsInsideOneBoardCellAndDoesNotInterceptRaycasts()
    {
        var tile=Own((GameObject)Factory("CreateTile",TileTemplate(),Surface(),"Concrete"));var surface=tile.transform.Find("Surface");
        Assert.That(surface.GetComponentsInChildren<Collider>(true),Is.Empty);
        var bounds=surface.GetComponent<Renderer>().bounds;Assert.That(bounds.size.x,Is.EqualTo(.72f).Within(.00001f));
        Assert.That(bounds.size.z,Is.EqualTo(.72f).Within(.00001f));Assert.That(bounds.center.x,Is.EqualTo(0).Within(.00001f));
        Assert.That(bounds.min.y,Is.GreaterThan(.05f));
    }
    [Test] public void TileFactoryRejectsMissingTemplateOrSurface()
    {
        Assert.Throws<TargetInvocationException>(()=>Factory("CreateTile",null,Surface(),"Tile"));
        Assert.Throws<TargetInvocationException>(()=>Factory("CreateTile",TileTemplate(),null,"Tile"));
    }
    [Test] public void BuildingRootLiftDoesNotChangePrototypeTemplate()
    {
        var template=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_UrbanEquation/Prefabs/Gameplay/Buildings/Building.prefab");
        var root=Own((GameObject)Factory("CreateBuildingRoot",template,.15f));
        var offset=(Vector3)Field(root.GetComponent(Runtime("BuildingInstance")),"visualOffset");
        Assert.That(offset.y*root.transform.lossyScale.y,Is.EqualTo(.15f).Within(.00001f));
        Assert.That(Field(template.GetComponent(Runtime("BuildingInstance")),"visualOffset"),Is.EqualTo(Vector3.zero));
        Assert.That(root.transform.localScale,Is.EqualTo(template.transform.localScale));
    }
    [Test] public void BuildingVisualInitializesAtAuthoredOffset()
    {
        var template=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_UrbanEquation/Prefabs/Gameplay/Buildings/Building.prefab");
        var root=Own((GameObject)Factory("CreateBuildingRoot",template,.15f));var instance=root.GetComponent(Runtime("BuildingInstance"));
        var content=AssetDatabase.LoadAssetAtPath("Assets/_UrbanEquation/Data/GameContent.asset",Runtime("GameContentData"));
        object building=Items(Get(Get(content,"Buildings"),"Buildings"))[0];Call(instance,"Initialize",building,Vector2Int.zero);
        Assert.That(root.transform.GetChild(root.transform.childCount-1).localPosition,Is.EqualTo(Field(instance,"visualOffset")));
    }
    [Test] public void PopupFactoryWiresTextAndNeverBlocksBoardInput()
    {
        var popup=Popup();foreach(string name in new[]{"comboNameText","descriptionText","rewardsText","canvasGroup"})Assert.That(Field(popup,name),Is.Not.Null);
        var group=popup.GetComponent<CanvasGroup>();Assert.That(group.blocksRaycasts,Is.False);Assert.That(group.interactable,Is.False);
        Assert.That(popup.GetComponentsInChildren<Graphic>(true).All(g=>!g.raycastTarget),Is.True);
    }
    [Test] public void EmptyPopupBindingHidesWithoutTouchingResources()
    {
        var bootstrap=Bootstrap();var session=Get(bootstrap,"Session");var resources=Call(Get(session,"Resources"),"CaptureResourceState");
        var popup=Popup();Call(popup,"Bind",Get(session,"Combos"),null);
        Assert.That(Get(popup,"CurrentResult"),Is.Null);Assert.That(popup.GetComponent<CanvasGroup>().alpha,Is.Zero);
        Assert.That(Call(Get(session,"Resources"),"CaptureResourceState"),Is.EqualTo(resources));
    }
    [Test] public void PopupBindingDisplaysAnAlreadyQueuedCombo()
    {
        var manager=Combo(Bootstrap());var popup=Popup();Call(popup,"Bind",manager,null);
        Assert.That(Get(popup,"CurrentResult"),Is.SameAs(Get(manager,"CurrentPresentation")));
        Assert.That(Get(Field(popup,"comboNameText"),"text"),Is.EqualTo("마을의 탄생"));
        Assert.That(Get(Field(popup,"rewardsText"),"text").ToString(),Does.Contain("인구 +1"));
    }
    [Test] public void PopupFadeUsesExplicitTimeBeforeCompletion()
    {
        var manager=Combo(Bootstrap());var popup=Popup();Call(popup,"Bind",manager,null);Call(popup,"Tick",1.875f);
        Assert.That(popup.GetComponent<CanvasGroup>().alpha,Is.EqualTo(.5f).Within(.00001f));
        Assert.That(Get(manager,"CurrentPresentation"),Is.Not.Null);
    }
    [Test] public void PopupCompletionAdvancesPresentationWithoutApplyingRewardAgain()
    {
        var bootstrap=Bootstrap();var manager=Combo(bootstrap);var resources=Get(Get(bootstrap,"Session"),"Resources");
        var before=Call(resources,"CaptureResourceState");var popup=Popup();Call(popup,"Bind",manager,null);Call(popup,"Tick",2f);
        Assert.That(Get(manager,"CurrentPresentation"),Is.Null);Assert.That(Get(popup,"CurrentResult"),Is.Null);
        Assert.That(Call(resources,"CaptureResourceState"),Is.EqualTo(before));Assert.That(Items(Get(manager,"Results")).Length,Is.EqualTo(1));
    }
    [Test] public void ClearingPresentationsImmediatelyHidesPopup()
    {
        var manager=Combo(Bootstrap());var popup=Popup();Call(popup,"Bind",manager,null);Call(manager,"ClearPresentations");
        Assert.That(Get(popup,"CurrentResult"),Is.Null);Assert.That(popup.GetComponent<CanvasGroup>().alpha,Is.Zero);
    }
    [Test] public void DisablingPopupPreservesQueueAndReenablingResumesIt()
    {
        var manager=Combo(Bootstrap());var popup=Popup();Call(popup,"Bind",manager,null);var expected=Get(manager,"CurrentPresentation");
        popup.gameObject.SetActive(false);Life(popup,"OnDisable");Assert.That(Get(popup,"CurrentResult"),Is.Null);
        Assert.That(Get(manager,"CurrentPresentation"),Is.SameAs(expected));Call(popup,"Tick",3f);
        Assert.That(Get(manager,"CurrentPresentation"),Is.SameAs(expected));
        popup.gameObject.SetActive(true);Life(popup,"OnEnable");Assert.That(Get(popup,"CurrentResult"),Is.SameAs(expected));
    }
    [Test] public void InvalidElapsedTimeDoesNotDrainQueue()
    {
        var manager=Combo(Bootstrap());var popup=Popup();Call(popup,"Bind",manager,null);var expected=Get(manager,"CurrentPresentation");
        foreach(float delta in new[]{-1f,float.NaN,float.PositiveInfinity})Call(popup,"Tick",delta);
        Assert.That(Get(manager,"CurrentPresentation"),Is.SameAs(expected));
    }
    [Test] public void RebindingPopupUnsubscribesPreviousManager()
    {
        var first=Bootstrap();var a=Combo(first);var second=Bootstrap();
        var popup=Popup();Call(popup,"Bind",a,null);Call(popup,"Bind",Get(Get(second,"Session"),"Combos"),null);
        var oldSession=Get(first,"Session");Command(Get(oldSession,"History"),"TryUndo");Build(oldSession,2,1,0);
        Assert.That(Get(popup,"CurrentResult"),Is.Null);
    }
    [Test] public void MultipleResultsAdvanceInOrderWithoutRepeatingRewards()
    {
        var bootstrap=Bootstrap(true);var session=Get(bootstrap,"Session");var manager=Get(session,"Combos");
        var popup=Popup();Call(popup,"Bind",manager,null);
        Build(session,1,0,0);Build(session,2,1,1);Build(session,3,1,0);
        var results=Items(Get(manager,"Results"));Assert.That(results.Length,Is.EqualTo(2));
        Assert.That(Get(results[0],"AdjacentCoordinate"),Is.EqualTo(new Vector2Int(0,0)));
        Assert.That(Get(results[1],"AdjacentCoordinate"),Is.EqualTo(new Vector2Int(1,1)));
        Assert.That(Get(popup,"CurrentResult"),Is.SameAs(results[0]));
        var before=Call(Get(session,"Resources"),"CaptureResourceState");
        Call(popup,"Tick",2f);Assert.That(Get(popup,"CurrentResult"),Is.SameAs(results[1]));
        Call(popup,"Tick",2f);Assert.That(Get(popup,"CurrentResult"),Is.Null);
        Assert.That(Call(Get(session,"Resources"),"CaptureResourceState"),Is.EqualTo(before));
    }
    [Test] public void RewardTextPreservesSignedAmountsAndColors()
    {
        var rewards=Array.CreateInstance(Runtime("ResourceAmount"),2);
        rewards.SetValue(Activator.CreateInstance(Runtime("ResourceAmount"),Enum.ToObject(Runtime("ResourceType"),2),3),0);
        rewards.SetValue(Activator.CreateInstance(Runtime("ResourceAmount"),Enum.ToObject(Runtime("ResourceType"),3),-1),1);
        var text=(string)Runtime("ComboPopupUI").GetMethod("FormatRewards").Invoke(null,new object[]{rewards});
        Assert.That(text,Does.Contain("<color=#00B858>자금 +3</color>"));
        Assert.That(text,Does.Contain("<color=#F04D55>물류 -1</color>"));
    }
    [Test] public void MissingPrefabSetReferencesAreReported()
    {
        var set=Own(ScriptableObject.CreateInstance(Runtime("GameplayPrefabSet")));var errors=new List<string>();Call(set,"Validate",errors);
        Assert.That(errors.Count,Is.EqualTo(2));
    }
    [Test] public void ValidPrefabSetReferencesAreAccepted()
    {
        var set=Own(ScriptableObject.CreateInstance(Runtime("GameplayPrefabSet")));Set(set,"buildingCardPrefab",Card());Set(set,"comboPopupPrefab",Popup());
        var errors=new List<string>();Call(set,"Validate",errors);Assert.That(errors,Is.Empty);
    }
    [Test] public void PopupFactoryRejectsMissingHeaderOrFont()
    {
        Assert.Throws<TargetInvocationException>(()=>Factory("CreateCombo",Sprite(),null,Font()));
        Assert.Throws<TargetInvocationException>(()=>Factory("CreateCombo",Sprite(),Sprite(),null));
    }

    [Test] public void LatestLargeOfficeVisualAcceptsSkyscraperWithoutChangingShippedData()
    {
        var source=AssetDatabase.LoadAssetAtPath("Assets/_UrbanEquation/Data/Buildings/B23001_LargeOffice.asset",Runtime("BuildingData"));
        var original=Get(source,"VisualPrefab");var building=Own(UObject.Instantiate(source));
        var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/polyperfect/Low Poly Ultimate Pack/_T/Prefabs_T/Buildings_T/Building_Skyscraper.prefab");
        Assert.That(model,Is.Not.Null);Set(building,"visualPrefab",model);
        var errors=new List<string>();Editor("UrbanEquationPrefabSetup").GetMethod("ValidateBuildingVisual").Invoke(null,new object[]{building,errors});
        Assert.That(errors,Is.Empty);Assert.That(Get(source,"VisualPrefab"),Is.SameAs(original));
    }

    [Test] public void LargeOfficeVisualRejectsLegacyHouseEvenWhenReferenceIsPresent()
    {
        var source=AssetDatabase.LoadAssetAtPath("Assets/_UrbanEquation/Data/Buildings/B23001_LargeOffice.asset",Runtime("BuildingData"));
        var building=Own(UObject.Instantiate(source));
        var house=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/polyperfect/Low Poly Ultimate Pack/_T/Prefabs_T/Buildings_T/Building_House_Block.prefab");
        Assert.That(house,Is.Not.Null);Set(building,"visualPrefab",house);
        var errors=new List<string>();Editor("UrbanEquationPrefabSetup").GetMethod("ValidateBuildingVisual").Invoke(null,new object[]{building,errors});
        Assert.That(errors.Count,Is.EqualTo(1));Assert.That(errors[0],Does.Contain("B23001"));
        Assert.That(errors[0],Does.Contain("Building_Skyscraper.prefab"));Assert.That(errors[0],Does.Contain("Building_House_Block.prefab"));
    }

    [Test] public void MissingLargeOfficeVisualReportsLatestModelPath()
    {
        var source=AssetDatabase.LoadAssetAtPath("Assets/_UrbanEquation/Data/Buildings/B23001_LargeOffice.asset",Runtime("BuildingData"));
        var building=Own(UObject.Instantiate(source));Set(building,"visualPrefab",null);
        var errors=new List<string>();Editor("UrbanEquationPrefabSetup").GetMethod("ValidateBuildingVisual").Invoke(null,new object[]{building,errors});
        Assert.That(errors.Count,Is.EqualTo(1));Assert.That(errors[0],Does.Contain("Building_Skyscraper.prefab"));
        Assert.That(errors[0],Does.Contain("<none>"));
    }
}
