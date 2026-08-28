using System;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Header("Manager References")]
    [SerializeField]
    private ResourceManager resourceManager;

    private bool isCleared;

    public bool IsCleared
    {
        get { return isCleared; }
    }

    public event Action OnStageCleared;

    private void Start()
    {
        isCleared = false;

        Debug.Log("Stage 1 시작");
        Debug.Log("클리어 목표: 인구 수 4");
    }

    public void CheckStageClear()
    {
        if (isCleared)
        {
            return;
        }

        int population =
            resourceManager.GetResource(
                ResourceType.Population
            );

        if (population < 4)
        {
            return;
        }

        ClearStage();
    }

    private void ClearStage()
    {
        isCleared = true;

        Debug.Log("Stage 1 Clear!");

        OnStageCleared?.Invoke();
    }
}