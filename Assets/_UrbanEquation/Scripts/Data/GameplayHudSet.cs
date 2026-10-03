using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameplayHud", menuName = "Urban Equation/Gameplay HUD")]
public class GameplayHudSet : ScriptableObject
{
    [SerializeField] private GameHudUI hudPrefab;
    [SerializeField] private ResourceIconSet resourceIcons;
    public GameHudUI HudPrefab => hudPrefab;
    public ResourceIconSet ResourceIcons => resourceIcons;
    public void Validate(List<string> errors)
    {
        if (hudPrefab == null) errors.Add("Game HUD prefab is missing.");
        if (resourceIcons == null) errors.Add("Resource icon set is missing."); else resourceIcons.Validate(errors);
    }
}
