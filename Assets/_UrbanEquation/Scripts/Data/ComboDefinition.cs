using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ComboDefinition
{
    [SerializeField] private int comboCode;
    [SerializeField] private string comboName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private int buildingCodeA;
    [SerializeField] private int buildingCodeB;
    [SerializeField] private ResourceAmount[] rewards = new ResourceAmount[0];

    public int ComboCode => comboCode;
    public string Code => $"C{comboCode:00000}";
    public string ComboName => comboName;
    public string Description => description;
    public int BuildingCodeA => buildingCodeA;
    public int BuildingCodeB => buildingCodeB;
    public IReadOnlyList<ResourceAmount> Rewards => Array.AsReadOnly(rewards);

    // Construction order is irrelevant; equal building codes are valid pairs.
    public bool Matches(int a, int b)
    {
        return (a == buildingCodeA && b == buildingCodeB)
            || (a == buildingCodeB && b == buildingCodeA);
    }

    public void Validate(List<string> errors)
    {
        if (comboCode <= 0 || buildingCodeA <= 0 || buildingCodeB <= 0)
            errors.Add("Combo and building codes must be positive.");
        if (rewards.Length == 0) errors.Add($"Combo {Code} has no reward.");
        DataValidation.ValidateResources(rewards, false, errors, Code);
    }
}
