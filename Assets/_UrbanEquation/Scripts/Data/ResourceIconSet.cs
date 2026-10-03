using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ResourceIcons", menuName = "Urban Equation/Resource Icons")]
public class ResourceIconSet : ScriptableObject
{
    [SerializeField] private Sprite[] neutral = new Sprite[5];
    [SerializeField] private Sprite[] gained = new Sprite[5];
    [SerializeField] private Sprite[] spent = new Sprite[5];

    public Sprite GetIcon(ResourceType resource, int sign = 0)
    {
        int index = (int)resource;
        Sprite[] icons = sign > 0 ? gained : sign < 0 ? spent : neutral;
        return icons != null && index >= 0 && index < icons.Length ? icons[index] : null;
    }
    public void Validate(List<string> errors)
    {
        foreach (Sprite[] icons in new[] { neutral, gained, spent })
        {
            if (icons == null || icons.Length != 5) { errors.Add("Resource icons require five entries per color."); continue; }
            for (int i = 0; i < icons.Length; i++) if (icons[i] == null) errors.Add("Resource icon is missing: " + i);
        }
    }
}
