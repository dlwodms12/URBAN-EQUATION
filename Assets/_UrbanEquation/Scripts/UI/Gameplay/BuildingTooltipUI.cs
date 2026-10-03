using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BuildingTooltipUI : MonoBehaviour
{
    [SerializeField] private TMP_Text buildingName;
    [SerializeField] private TMP_Text allowedTiles;
    [SerializeField] private ResourceStripUI gained;
    [SerializeField] private ResourceStripUI spent;
    [SerializeField] private CanvasGroup canvasGroup;
    private object owner;
    public BuildingData CurrentBuilding { get; private set; }
    public bool IsVisible => CurrentBuilding != null && canvasGroup != null && canvasGroup.alpha > 0;

    private void OnEnable() => HideAll();
    private void OnDisable() => HideAll();
    public void Show(BuildingData building, object source, Vector2 screenPoint)
    {
        if (building == null || source == null) { HideAll(); return; }
        if (CurrentBuilding != building)
        {
            CurrentBuilding = building;
            if (buildingName != null) buildingName.text = building.BuildingName;
            if (allowedTiles != null) allowedTiles.text = FormatTiles(building);
            if (gained != null) gained.Show(building.GainedResources, true);
            var costs = new List<ResourceAmount>();
            foreach (ResourceAmount cost in building.RequiredResources) costs.Add(new ResourceAmount(cost.Resource, -cost.Amount));
            if (spent != null) spent.Show(costs, true);
        }
        owner = source;
        if (canvasGroup != null) { canvasGroup.alpha = 1; canvasGroup.interactable = false; canvasGroup.blocksRaycasts = false; }
        Place(screenPoint);
    }
    public void Hide(object source) { if (owner == source) HideAll(); }
    public void HideAll()
    {
        owner = null; CurrentBuilding = null;
        if (canvasGroup != null) canvasGroup.alpha = 0;
        if (gained != null) gained.Clear(); if (spent != null) spent.Clear();
    }
    public static string FormatTiles(BuildingData building)
    {
        if (building == null) return string.Empty;
        var labels = new List<string>();
        foreach (TileType tile in building.AllowedTileTypes)
        {
            string color = tile == TileType.Grass ? "4EA84B" : tile == TileType.Concrete ? "999999" : "555555";
            labels.Add("<color=#" + color + ">●</color> " + tile);
        }
        return "건설 가능한 타일  " + string.Join("  ", labels);
    }
    private void Place(Vector2 point)
    {
        var parent = transform.parent as RectTransform; if (parent == null) return;
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var rect = (RectTransform)transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, point, camera, out Vector2 position)) return;
        position += new Vector2(0, 125);
        Vector2 half = rect.rect.size * .5f;
        position.x = Mathf.Clamp(position.x, parent.rect.xMin + half.x, parent.rect.xMax - half.x);
        position.y = Mathf.Clamp(position.y, parent.rect.yMin + half.y, parent.rect.yMax - half.y);
        rect.anchoredPosition = position;
    }
}
