using System;
using System.Collections.Generic;

// Runtime state only. Goal/rank evaluation is introduced in Phase 3-G.
public sealed class StageProgressState
{
    public static StageProgressState Empty { get; }
        = new StageProgressState(false, new bool[0], 0, false);
    public bool IsCleared { get; }
    public IReadOnlyList<bool> GoalStates { get; }
    public int Rank { get; }
    public bool NextStageAvailable { get; }

    public StageProgressState(bool isCleared, IReadOnlyList<bool> goals, int rank, bool nextStageAvailable)
    {
        if (goals == null) throw new ArgumentNullException(nameof(goals));
        if (rank < 0 || rank > 3) throw new ArgumentOutOfRangeException(nameof(rank));
        IsCleared = isCleared;
        Rank = rank;
        NextStageAvailable = nextStageAvailable;
        var copy = new bool[goals.Count];
        for (int i = 0; i < copy.Length; i++) copy[i] = goals[i];
        GoalStates = Array.AsReadOnly(copy);
    }
}
