using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// In-memory construction is separate from asset import/save so tests never modify project assets.
public static class UrbanEquationPrefabFactory
{
    public static GameObject CreateTile(GameObject template, GameObject surface, string name)
    {
        if (template == null || template.GetComponent<Tile>() == null || surface == null)
            throw new ArgumentException("Tile construction requires a Tile template and surface model.");
        var root = UnityEngine.Object.Instantiate(template);
        root.name = name;
        try
        {
            var visual = UnityEngine.Object.Instantiate(surface, root.transform);
            visual.name = "Surface";
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);
            var renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new ArgumentException("Tile surface has no renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float size = Mathf.Max(bounds.size.x, bounds.size.z);
            if (size <= 0) throw new ArgumentException("Tile surface has empty bounds.");
            // Preserve the template's road perimeter and highlight. Fit the building area inside it.
            visual.transform.localScale *= 0.72f / size;
            bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float top = root.transform.position.y;
            Transform highlight = root.transform.Find("Highlight");
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.transform.IsChildOf(visual.transform)
                    || (highlight != null && renderer.transform.IsChildOf(highlight))) continue;
                top = Mathf.Max(top, renderer.bounds.max.y);
            }
            visual.transform.position += new Vector3(root.transform.position.x - bounds.center.x,
                top + 0.002f - bounds.min.y, root.transform.position.z - bounds.center.z);
            root.GetComponent<Tile>().SetHighlight(false);
            return root;
        }
        catch { UnityEngine.Object.DestroyImmediate(root); throw; }
    }

    public static GameObject CreateBuildingRoot(GameObject template, float worldHeight)
    {
        if (template == null || template.GetComponent<BuildingInstance>() == null)
            throw new ArgumentException("Building root construction requires BuildingInstance.");
        if (float.IsNaN(worldHeight) || float.IsInfinity(worldHeight) || worldHeight < 0 || template.transform.lossyScale.y <= 0)
            throw new ArgumentException("Building visual height and root scale must be valid.");
        var root = UnityEngine.Object.Instantiate(template); root.name = "BuildingRoot";
        var serialized = new SerializedObject(root.GetComponent<BuildingInstance>());
        serialized.FindProperty("visualOffset").vector3Value = new Vector3(0,worldHeight / root.transform.lossyScale.y,0);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    public static GameObject CreateCard(Sprite background, TMP_FontAsset font)
    {
        if (background == null || font == null) throw new ArgumentException("Card requires a background and font.");
        var root = Rect("BuildingCard", null, new Vector2(110,140));
        var image = root.AddComponent<Image>(); image.sprite = background; image.type = Image.Type.Sliced;
        var button = root.AddComponent<Button>(); button.targetGraphic = image;
        // Availability is shown by the dedicated half-black overlay, not a second tint.
        button.transition = Selectable.Transition.None;
        var layout = root.AddComponent<LayoutElement>(); layout.preferredWidth = 110; layout.preferredHeight = 140;
        var art = Rect("BuildingImage", root.transform, new Vector2(86,90));
        ((RectTransform)art.transform).anchoredPosition = new Vector2(0,12);
        var artImage = art.AddComponent<Image>(); artImage.preserveAspect = true; artImage.raycastTarget = false;
        var title = Label("BuildingName", root.transform, font, new Vector2(102,28), new Vector2(0,-51), 15);
        var overlay = Rect("DisabledOverlay", root.transform, Vector2.zero);
        var overlayRect = (RectTransform)overlay.transform;
        overlayRect.anchorMin = Vector2.zero; overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero; overlayRect.offsetMax = Vector2.zero;
        var overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = new Color(0,0,0,0.5f); overlayImage.raycastTarget = false;
        var view = root.AddComponent<BuildingCardUI>();
        Reference(view, "buildingNameText", title); Reference(view,"cardImage", artImage);
        Reference(view,"disabledOverlay",overlayImage); Reference(view,"button",button);
        overlay.SetActive(false);
        return root;
    }

    public static GameObject CreateCombo(Sprite background, Sprite header, TMP_FontAsset font)
    {
        if (background == null || header == null || font == null)
            throw new ArgumentException("Combo popup requires background, header and font.");
        var root = Rect("ComboPopup", null, new Vector2(210,230));
        var image = root.AddComponent<Image>(); image.sprite = background; image.type = Image.Type.Sliced;
        image.raycastTarget = false;
        var group = root.AddComponent<CanvasGroup>(); group.interactable = false; group.blocksRaycasts = false;
        var logo = Rect("ComboHeader",root.transform,new Vector2(160,48));
        ((RectTransform)logo.transform).anchoredPosition = new Vector2(0,80);
        var logoImage = logo.AddComponent<Image>(); logoImage.sprite = header;
        logoImage.preserveAspect = true; logoImage.raycastTarget = false;
        var title = Label("ComboName",root.transform,font,new Vector2(190,35),new Vector2(0,36),20);
        var description = Label("Description",root.transform,font,new Vector2(190,60),new Vector2(0,-9),15);
        var rewards = Label("Rewards",root.transform,font,new Vector2(190,66),new Vector2(0,-77),19);
        title.text = "콤보 이름"; description.text = "콤보 설명"; rewards.text = "<color=#00B858>인구 +1</color>";
        var view = root.AddComponent<ComboPopupUI>();
        Reference(view,"comboNameText",title); Reference(view,"descriptionText",description);
        Reference(view,"rewardsText",rewards); Reference(view,"canvasGroup",group);
        return root;
    }

    private static GameObject Rect(string name, Transform parent, Vector2 size)
    {
        var root = new GameObject(name,typeof(RectTransform));
        root.transform.SetParent(parent,false); ((RectTransform)root.transform).sizeDelta = size;
        return root;
    }
    private static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, Vector2 size, Vector2 position, int fontSize)
    {
        var root = Rect(name,parent,size); ((RectTransform)root.transform).anchoredPosition = position;
        var text = root.AddComponent<TextMeshProUGUI>(); text.font = font;
        text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black; text.raycastTarget = false; text.richText = true;
        text.enableAutoSizing = true; text.fontSizeMin = 11; text.fontSizeMax = fontSize;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }
    public static void Reference(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(field);
        if (property == null) throw new ArgumentException("Missing serialized field: " + field);
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
