using System;
using System.Collections.Generic;

// A completed result is a value snapshot. Save/scene flow consume it in later phases.
public sealed class StageResult
{
    public int StageNumber { get; }
    public string StageName { get; }
    public int Rank { get; }
    public IReadOnlyList<bool> GoalStates { get; }
    public IReadOnlyList<string> GoalDescriptions { get; }
    public IReadOnlyList<ResourceAmount> Resources { get; }

    internal StageResult(StageData stage, StageProgressState progress, IReadOnlyList<ResourceAmount> resources)
    {
        StageNumber = stage.StageNumber;
        StageName = stage.StageName;
        Rank = progress.Rank;
        var flags = new bool[progress.GoalStates.Count];
        for (int i = 0; i < flags.Length; i++) flags[i] = progress.GoalStates[i];
        GoalStates = Array.AsReadOnly(flags);
        GoalDescriptions = Array.AsReadOnly(new[] { stage.RequiredGoal.Description,
            stage.AdditionalGoals[0].Description, stage.AdditionalGoals[1].Description });
        var values = new ResourceAmount[resources.Count];
        for (int i = 0; i < values.Length; i++) values[i] = resources[i];
        Resources = Array.AsReadOnly(values);
    }
}
