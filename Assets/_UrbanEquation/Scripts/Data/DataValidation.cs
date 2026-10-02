using System;
using System.Collections.Generic;

// Validation reports authoring mistakes without silently changing design data.
public static class DataValidation
{
    public static void ValidateResources(IReadOnlyList<ResourceAmount> values,
        bool requireNonNegative, List<string> errors, string context)
    {
        var seen = new HashSet<ResourceType>();
        foreach (ResourceAmount value in values)
        {
            if (!Enum.IsDefined(typeof(ResourceType), value.Resource))
                errors.Add($"{context}: unknown resource type.");
            if (!seen.Add(value.Resource))
                errors.Add($"{context}: duplicate resource {value.Resource}.");
            if (requireNonNegative && value.Amount < 0)
                errors.Add($"{context}: negative quantity for {value.Resource}.");
        }
    }
}
