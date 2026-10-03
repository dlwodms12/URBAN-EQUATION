using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameplayPrefabs", menuName = "Urban Equation/Gameplay Prefabs")]
public class GameplayPrefabSet : ScriptableObject
{
    [SerializeField] private BuildingCardUI buildingCardPrefab;
    [SerializeField] private ComboPopupUI comboPopupPrefab;
    public BuildingCardUI BuildingCardPrefab => buildingCardPrefab;
    public ComboPopupUI ComboPopupPrefab => comboPopupPrefab;
    public void Validate(List<string> errors)
    {
        if (buildingCardPrefab == null) errors.Add("Building card prefab is missing.");
        if (comboPopupPrefab == null) errors.Add("Combo popup prefab is missing.");
    }
}
