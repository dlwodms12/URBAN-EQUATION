using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    [Header("Initial Resources (Prototype)")]
    [SerializeField] private int initialPopulation = 2;
    [SerializeField] private int initialJobs = 2;
    // Keep the serialized name; value 2 now represents Money.
    [SerializeField] private int initialGoods = 2;
    [SerializeField] private int initialLogistics = 2;
    [SerializeField] private int initialTourism = 0;

    private static readonly ResourceType[] ResourceTypes =
    {
        ResourceType.Population, ResourceType.Jobs, ResourceType.Money,
        ResourceType.Logistics, ResourceType.Tourism
    };
    private static readonly ResourceAmount[] EmptyAmounts = new ResourceAmount[0];

    private Dictionary<ResourceType, int> resources;
    private Dictionary<ResourceType, int> initialState;
    private bool hasStageInitialState;

    public event Action<ResourceType, int> OnResourceChanged;
    public event Action OnResourcesChanged;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (resources != null) return;
        initialState = CreatePrototypeState();
        resources = new Dictionary<ResourceType, int>(initialState);
    }

    private Dictionary<ResourceType, int> CreatePrototypeState()
    {
        return new Dictionary<ResourceType, int>
        {
            { ResourceType.Population, initialPopulation },
            { ResourceType.Jobs, initialJobs },
            { ResourceType.Money, initialGoods },
            { ResourceType.Logistics, initialLogistics },
            { ResourceType.Tourism, initialTourism }
        };
    }

    public bool TryInitializeFromStage(StageData stage)
    {
        EnsureInitialized();
        if (stage == null
            || !TryReadAmounts(stage.InitialResources, false, true, out var next)) return false;
        initialState = new Dictionary<ResourceType, int>(next);
        hasStageInitialState = true;
        CommitState(next, true);
        return true;
    }

    public void ResetResources()
    {
        EnsureInitialized();
        if (!hasStageInitialState) initialState = CreatePrototypeState();
        CommitState(new Dictionary<ResourceType, int>(initialState), true);
    }

    public bool TryGetResource(ResourceType resourceType, out int amount)
    {
        EnsureInitialized();
        return resources.TryGetValue(resourceType, out amount);
    }

    public int GetResource(ResourceType resourceType)
    {
        if (TryGetResource(resourceType, out int amount)) return amount;
        Debug.LogWarning($"Unknown resource: {resourceType}", this);
        return 0;
    }

    public bool CanAffordResources(IReadOnlyList<ResourceAmount> costs)
    {
        return TryPrepareChange(costs, EmptyAmounts, false, out _);
    }

    public bool CanAffordBuilding(BuildingData building)
    {
        return building != null && CanAffordResources(building.RequiredResources);
    }

    public bool TryConsumeResources(IReadOnlyList<ResourceAmount> costs)
    {
        if (!TryPrepareChange(costs, EmptyAmounts, false, out var next)) return false;
        CommitState(next, false);
        return true;
    }

    public bool TryAddResources(IReadOnlyList<ResourceAmount> amounts)
    {
        // Signed deltas preserve the prototype's Add behavior, including negative combos.
        if (!TryPrepareChange(EmptyAmounts, amounts, true, out var next)) return false;
        CommitState(next, false);
        return true;
    }

    public bool TryApplyBuildingResources(BuildingData building)
    {
        if (!TryApplyBuildingResourcesDeferred(building, out Action publish)) return false;
        publish();
        return true;
    }

    // The session finishes tile/card/visual state before publishing resource events.
    internal bool TryApplyBuildingResourcesDeferred(BuildingData building, out Action publish)
    {
        publish = null;
        if (building == null || !TryPrepareChange(building.RequiredResources,
            building.GainedResources, false, out var next)) return false;
        publish = CommitStateDeferred(next, false);
        return true;
    }

    internal bool TryApplyBuildingAndRewardsDeferred(BuildingData building,
        IReadOnlyList<IReadOnlyList<ResourceAmount>> rewards, out Action publish)
    {
        publish = null;
        if (building == null || !TryPrepareChange(building.RequiredResources,
            building.GainedResources, false, out var next)
            || !TryPrepareRewards(next, rewards, out var final)) return false;
        publish = CommitStateDeferred(final, false);
        return true;
    }

    internal bool TryApplyRewardsDeferred(IReadOnlyList<IReadOnlyList<ResourceAmount>> rewards,
        out Action publish)
    {
        EnsureInitialized();
        publish = null;
        if (!TryPrepareRewards(resources, rewards, out var next)) return false;
        publish = CommitStateDeferred(next, false);
        return true;
    }

    private static bool TryPrepareRewards(Dictionary<ResourceType, int> start,
        IReadOnlyList<IReadOnlyList<ResourceAmount>> rewards,
        out Dictionary<ResourceType, int> next)
    {
        next = null;
        if (rewards == null) return false;
        var candidate = new Dictionary<ResourceType, int>(start);
        foreach (IReadOnlyList<ResourceAmount> reward in rewards)
        {
            if (!TryPrepareChangeFromState(candidate, EmptyAmounts, reward, true, out var updated))
                return false;
            candidate = updated;
        }
        next = candidate;
        return true;
    }

    public bool TryApplyComboResources(ComboDefinition combo)
    {
        return combo != null && combo.Rewards.Count > 0 && TryAddResources(combo.Rewards);
    }

    public bool CanConsume(ResourceType resourceType, int amount)
    {
        return CanAffordResources(new[] { new ResourceAmount(resourceType, amount) });
    }

    public bool Consume(ResourceType resourceType, int amount)
    {
        return TryConsumeResources(new[] { new ResourceAmount(resourceType, amount) });
    }

    public void Add(ResourceType resourceType, int amount)
    {
        if (!TryAddResources(new[] { new ResourceAmount(resourceType, amount) }))
            Debug.LogWarning("Invalid resource update was rejected.", this);
    }

    // Compatibility entry point used by the existing BuildingPlacement.
    public void ApplyBuildingResource(BuildingData buildingData)
    {
        if (buildingData == null) return;
        if (!TryApplyBuildingResources(buildingData))
            Debug.LogWarning("Building resource transaction was rejected.", this);
    }

    public ResourceAmount[] CaptureResourceState()
    {
        EnsureInitialized();
        var snapshot = new ResourceAmount[ResourceTypes.Length];
        for (int i = 0; i < ResourceTypes.Length; i++)
            snapshot[i] = new ResourceAmount(ResourceTypes[i], resources[ResourceTypes[i]]);
        return snapshot;
    }

    public bool TryRestoreResources(IReadOnlyList<ResourceAmount> snapshot)
    {
        EnsureInitialized();
        // A captured state may contain signed values from legacy Add.
        // Restore does not change the stage's retry baseline.
        if (!TryReadAmounts(snapshot, true, true, out var next)) return false;
        CommitState(next, true);
        return true;
    }

    private bool TryPrepareChange(IReadOnlyList<ResourceAmount> costs,
        IReadOnlyList<ResourceAmount> gains, bool allowSignedGains,
        out Dictionary<ResourceType, int> next)
    {
        EnsureInitialized();
        return TryPrepareChangeFromState(resources, costs, gains, allowSignedGains, out next);
    }

    private static bool TryPrepareChangeFromState(Dictionary<ResourceType, int> state,
        IReadOnlyList<ResourceAmount> costs, IReadOnlyList<ResourceAmount> gains,
        bool allowSignedGains, out Dictionary<ResourceType, int> next)
    {
        next = null;
        if (!TryReadAmounts(costs, false, false, out var costMap)
            || !TryReadAmounts(gains, allowSignedGains, false, out var gainMap)) return false;

        var candidate = new Dictionary<ResourceType, int>();
        foreach (ResourceType type in ResourceTypes)
        {
            costMap.TryGetValue(type, out int cost);
            gainMap.TryGetValue(type, out int gain);
            int current = state[type];
            // Rewards from this build cannot pay its own upfront costs.
            if (cost > 0 && current < cost) return false;
            long value = (long)current - cost + gain;
            if (value < int.MinValue || value > int.MaxValue) return false;
            candidate.Add(type, (int)value);
        }
        next = candidate;
        return true;
    }

    private static bool TryReadAmounts(IReadOnlyList<ResourceAmount> amounts,
        bool allowNegative, bool requireAll,
        out Dictionary<ResourceType, int> result)
    {
        result = null;
        if (amounts == null || (requireAll && amounts.Count != ResourceTypes.Length)) return false;
        var candidate = new Dictionary<ResourceType, int>();
        foreach (ResourceAmount amount in amounts)
        {
            int code = (int)amount.Resource;
            if (code < 0 || code >= ResourceTypes.Length
                || (!allowNegative && amount.Amount < 0)
                || candidate.ContainsKey(amount.Resource)) return false;
            candidate.Add(amount.Resource, amount.Amount);
        }
        result = candidate;
        return true;
    }

    private void CommitState(Dictionary<ResourceType, int> next, bool notifyAll)
    {
        CommitStateDeferred(next, notifyAll)();
    }

    private Action CommitStateDeferred(Dictionary<ResourceType, int> next, bool notifyAll)
    {
        var changed = new List<ResourceType>();
        foreach (ResourceType type in ResourceTypes)
            if (notifyAll || resources[type] != next[type]) changed.Add(type);

        // Publish only after all five resource values have been committed.
        resources = next;
        return () =>
        {
            if (changed.Count == 0) return;
            foreach (ResourceType type in changed)
                OnResourceChanged?.Invoke(type, next[type]);
            OnResourcesChanged?.Invoke();
        };
    }
}
