using UnityEngine;

[CreateAssetMenu(fileName = "ResourceData", menuName = "Urban Equation/Resource Data")]
public class ResourceData : ScriptableObject
{
    [SerializeField] private ResourceType resourceType;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite normalIcon;
    [SerializeField] private Sprite gainIcon;
    [SerializeField] private Sprite costIcon;

    public ResourceType ResourceType => resourceType;
    public string ResourceCode => $"R{(int)resourceType + 1:00000}";
    public string DisplayName => displayName;
    public Sprite NormalIcon => normalIcon;
    public Sprite GainIcon => gainIcon;
    public Sprite CostIcon => costIcon;
}
