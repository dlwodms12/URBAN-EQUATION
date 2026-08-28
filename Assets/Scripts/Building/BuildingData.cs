using Mono.Cecil;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BuildingData",
    menuName = "Urban Equation/Building Data"
)]

//건물 데이터를 저장
public class BuildingData : ScriptableObject
{
    [Header("Building Information")]
    [SerializeField]
    private int buildingCode;

    [SerializeField]
    private string buildingName;

    [Header("Resource")]
    [SerializeField]
    private ResourceType produceResource;

    [SerializeField]
    private int produceAmount;

    [SerializeField]
    private ResourceType consumeResource;

    [SerializeField]
    private int consumeAmount;

    //외부 접근용 프로퍼티
    public int BuildingCode => buildingCode;
    public string BuildingName => buildingName;

    // ResourceManager에 정의되어 있는 ResourceType에 접근하기 위한 프로퍼티
    public ResourceType ProduceResource => produceResource;
    public int ProduceAmount => produceAmount;

    public ResourceType ConsumeResource => consumeResource;
    public int ConsumeAmount => consumeAmount;
}