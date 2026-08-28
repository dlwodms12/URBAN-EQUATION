using System;
using System.Collections.Generic;
using UnityEngine;

public enum ResourceType
{
    Population,
    Jobs,
    Goods,
    Logistics
}

public class ResourceManager : MonoBehaviour
{
    [Header("Initial Resources")]
    [SerializeField]
    private int initialPopulation = 2;

    [SerializeField]
    private int initialJobs = 2;

    [SerializeField]
    private int initialGoods = 2;

    [SerializeField]
    private int initialLogistics = 2;

    private Dictionary<ResourceType, int> resources;

    public event Action<ResourceType, int> OnResourceChanged;

    private void Awake()
    {
        InitializeResources();
    }

    private void InitializeResources()
    {
        resources = new Dictionary<ResourceType, int>
        {
            { ResourceType.Population, initialPopulation },
            { ResourceType.Jobs, initialJobs },
            { ResourceType.Goods, initialGoods },
            { ResourceType.Logistics, initialLogistics }
        };
    }

    public int GetResource(ResourceType resourceType)
    {
        if (!resources.ContainsKey(resourceType))
        {
            Debug.LogWarning(
                $"존재하지 않는 자원입니다: {resourceType}"
            );

            return 0;
        }

        return resources[resourceType];
    }

    // 건물 배치 가능 여부를 판단
    public bool CanConsume(
        ResourceType resourceType,
        int amount)
    {
        return GetResource(resourceType) >= amount;
    }

    public bool Consume(
        ResourceType resourceType,
        int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (!CanConsume(resourceType, amount))
        {
            return false;
        }

        resources[resourceType] -= amount;

        NotifyResourceChanged(resourceType);

        return true;
    }

    public void Add(
        ResourceType resourceType,
        int amount)
    {
        if (amount == 0)
        {
            return;
        }

        resources[resourceType] += amount;

        NotifyResourceChanged(resourceType);
    }

    private void NotifyResourceChanged(
        ResourceType resourceType)
    {
        OnResourceChanged?.Invoke(
            resourceType,
            resources[resourceType]
        );
    }

    // 건물 배치 시 자원 소비 및 생산을 적용하는 메서드
    public void ApplyBuildingResource(
        BuildingData buildingData)
    {
        if (buildingData == null)
        {
            return;
        }

        Consume(
            buildingData.ConsumeResource,
            buildingData.ConsumeAmount
        );

        Add(
            buildingData.ProduceResource,
            buildingData.ProduceAmount
        );
    }
}