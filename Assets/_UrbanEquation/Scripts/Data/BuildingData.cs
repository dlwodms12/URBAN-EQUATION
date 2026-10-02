using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BuildingData",
    menuName = "Urban Equation/Building Data"
)]

// 건물 데이터를 저장
public class BuildingData : ScriptableObject
{
    [Header("Building Information")]
    [SerializeField]
    private int buildingCode;

    [SerializeField]
    private string buildingName;

    [Header("Building Visual")]
    [SerializeField]
    private GameObject visualPrefab;

    [SerializeField] private Sprite cardImage;

    [Header("Production Data")]
    [Tooltip("Enable for new design data. Leave disabled for the four prototype assets.")]
    [SerializeField] private bool useResourceLists;
    [Tooltip("Positive quantities; these resources are spent before rewards are granted.")]
    [SerializeField] private ResourceAmount[] requiredResources = new ResourceAmount[0];
    [SerializeField] private ResourceAmount[] gainedResources = new ResourceAmount[0];
    [Tooltip("Any listed tile is allowed. List all three types for unrestricted buildings.")]
    [SerializeField] private TileType[] allowedTileTypes = new TileType[0];

    [Header("Resource")]
    [SerializeField]
    private ResourceType produceResource;

    [SerializeField]
    private int produceAmount;

    [SerializeField]
    private ResourceType consumeResource;

    [SerializeField]
    private int consumeAmount;

    // 외부 접근용 프로퍼티
    public int BuildingCode => buildingCode;
    public string BuildingName => buildingName;
    public string Code => $"B{buildingCode:00000}";
    public Sprite CardImage => cardImage;
    public bool UsesResourceLists => useResourceLists;

    // The serialized legacy fields below stay intact until prototype callers are replaced.
    public IReadOnlyList<ResourceAmount> RequiredResources => useResourceLists
        ? Array.AsReadOnly(requiredResources)
        : Array.AsReadOnly(consumeAmount == 0 ? new ResourceAmount[0]
            : new[] { new ResourceAmount(consumeResource, consumeAmount) });

    public IReadOnlyList<ResourceAmount> GainedResources => useResourceLists
        ? Array.AsReadOnly(gainedResources)
        : Array.AsReadOnly(produceAmount == 0 ? new ResourceAmount[0]
            : new[] { new ResourceAmount(produceResource, produceAmount) });

    public IReadOnlyList<TileType> AllowedTileTypes => Array.AsReadOnly(allowedTileTypes);

    public bool CanBuildOn(TileType type)
    {
        foreach (TileType allowedType in allowedTileTypes)
            if (type == allowedType) return true;
        return false;
    }

    public void Validate(List<string> errors)
    {
        if (buildingCode <= 0) errors.Add("Building code must be positive.");
        DataValidation.ValidateResources(RequiredResources, true, errors, Code + " costs");
        DataValidation.ValidateResources(GainedResources, true, errors, Code + " gains");
        // Prototype assets did not define tile constraints.
        if (!useResourceLists) return;
        if (allowedTileTypes.Length == 0) errors.Add($"{Code}: no allowed tile types.");
        var types = new HashSet<TileType>();
        foreach (TileType type in allowedTileTypes)
        {
            if (!Enum.IsDefined(typeof(TileType), type)) errors.Add($"{Code}: unknown tile type.");
            if (!types.Add(type)) errors.Add($"{Code}: duplicate tile type {type}.");
        }
    }

    // 건물 외형 프리팹
    public GameObject VisualPrefab => visualPrefab;

    // ResourceManager에 정의되어 있는 ResourceType에 접근하기 위한 프로퍼티
    public ResourceType ProduceResource => produceResource;
    public int ProduceAmount => produceAmount;

    public ResourceType ConsumeResource => consumeResource;
    public int ConsumeAmount => consumeAmount;
}
