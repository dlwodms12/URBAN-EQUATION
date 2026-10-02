using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageCatalog", menuName = "Urban Equation/Stage Catalog")]
public class StageCatalog : ScriptableObject
{
    [SerializeField] private StageData[] stages = new StageData[0];
    public int Count => stages == null ? 0 : stages.Length;
    public IReadOnlyList<StageData> Stages => Array.AsReadOnly(stages ?? new StageData[0]);

    public bool TryGetStage(int number, out StageData stage)
    {
        stage = null;
        if (number < 1 || number > Count || stages[number - 1] == null
            || stages[number - 1].StageNumber != number) return false;
        stage = stages[number - 1];
        return true;
    }

    public void Validate(List<string> errors)
    {
        if (Count == 0) errors.Add("Stage catalog is empty.");
        for (int i = 0; i < Count; i++)
        {
            if (stages[i] == null) { errors.Add("Stage catalog contains a missing stage."); continue; }
            if (stages[i].StageNumber != i + 1)
                errors.Add("Stage catalog must contain consecutive stages in number order, starting at 1.");
            stages[i].Validate(errors);
        }
    }
}
