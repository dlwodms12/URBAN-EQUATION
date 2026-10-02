using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ComboDatabase", menuName = "Urban Equation/Combo Database")]
public class ComboDatabase : ScriptableObject
{
    // The same ComboCode can occur on multiple rows in the design table.
    // The unordered building pair identifies an individual row.
    [SerializeField] private List<ComboDefinition> combos = new List<ComboDefinition>();
    public IReadOnlyList<ComboDefinition> Combos => combos.AsReadOnly();

    public bool TryGetCombo(int a, int b, out ComboDefinition combo)
    {
        combo = null;
        foreach (ComboDefinition candidate in combos)
        {
            if (candidate == null || !candidate.Matches(a, b)) continue;
            if (combo != null) { combo = null; return false; }
            combo = candidate;
        }
        return combo != null;
    }

    public void Validate(List<string> errors)
    {
        var pairs = new HashSet<string>();
        foreach (ComboDefinition combo in combos)
        {
            if (combo == null) { errors.Add("Combo database contains a missing entry."); continue; }
            combo.Validate(errors);
            int low = Mathf.Min(combo.BuildingCodeA, combo.BuildingCodeB);
            int high = Mathf.Max(combo.BuildingCodeA, combo.BuildingCodeB);
            if (!pairs.Add($"{low}:{high}")) errors.Add($"Duplicate combo pair: {low}, {high}");
        }
    }
}
