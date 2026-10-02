using System;
using UnityEngine;

[Serializable]
public struct ResourceAmount
{
    [SerializeField] private ResourceType resource;
    [SerializeField] private int amount;

    public ResourceType Resource => resource;
    public int Amount => amount;

    public ResourceAmount(ResourceType resource, int amount)
    {
        this.resource = resource;
        this.amount = amount;
    }
}
