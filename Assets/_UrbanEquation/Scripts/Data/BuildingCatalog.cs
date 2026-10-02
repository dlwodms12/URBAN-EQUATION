using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingCatalog", menuName = "Urban Equation/Building Catalog")]
public class BuildingCatalog : ScriptableObject
{
    [SerializeField] private List<BuildingData> buildings = new List<BuildingData>();
    public IReadOnlyList<BuildingData> Buildings => buildings.AsReadOnly();

    public bool TryGetBuilding(int buildingCode, out BuildingData building)
    {
        building = null;
        foreach (BuildingData candidate in buildings)
        {
            if (candidate == null || candidate.BuildingCode != buildingCode) continue;
            // Ambiguous definitions must not silently select the first entry.
            if (building != null) { building = null; return false; }
            building = candidate;
        }
        return building != null;
    }

    public void Validate(List<string> errors)
    {
        var codes = new HashSet<int>();
        foreach (BuildingData building in buildings)
        {
            if (building == null) { errors.Add("Building catalog contains a missing entry."); continue; }
            if (!codes.Add(building.BuildingCode)) errors.Add($"Duplicate building: {building.BuildingCode}");
            building.Validate(errors);
        }
    }
}
