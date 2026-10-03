using System;
using System.Collections.Generic;

// Durable progress only. Running board, resources, cards and Undo are never retained.
public sealed class ProgressSaveData
{
    public const int CurrentVersion = 1;
    public int StageCount { get; }
    public int HighestUnlockedStage { get; }
    public IReadOnlyList<int> BestRanks { get; }

    private ProgressSaveData(int stageCount, int unlocked, int[] ranks)
    {
        StageCount = stageCount;
        HighestUnlockedStage = unlocked;
        BestRanks = Array.AsReadOnly((int[])ranks.Clone());
    }

    public bool IsStageUnlocked(int number) => number >= 1 && number <= HighestUnlockedStage;
    public int GetBestRank(int number) => number < 1 || number > StageCount ? 0 : BestRanks[number - 1];

    public static bool TryCreate(int stageCount, out ProgressSaveData data, out string error)
    {
        data = null;
        error = null;
        if (stageCount < 1) { error = "Stage count must be positive."; return false; }
        data = new ProgressSaveData(stageCount, 1, new int[stageCount]);
        return true;
    }

    public bool TryRecordClear(int stageNumber, int rank, out ProgressSaveData next, out string error)
    {
        next = null;
        error = null;
        if (!IsStageUnlocked(stageNumber) || rank < 1 || rank > 3)
        { error = "A clear requires an unlocked stage and rank 1 through 3."; return false; }
        int[] ranks = CopyRanks();
        ranks[stageNumber - 1] = Math.Max(ranks[stageNumber - 1], rank);
        int unlocked = Math.Max(HighestUnlockedStage, stageNumber < StageCount ? stageNumber + 1 : StageCount);
        next = new ProgressSaveData(StageCount, unlocked, ranks);
        return true;
    }

    internal int[] CopyRanks()
    {
        var ranks = new int[StageCount];
        for (int i = 0; i < ranks.Length; i++) ranks[i] = BestRanks[i];
        return ranks;
    }

    // Catalogs may append stages; existing numbers keep the same meaning.
    // Validate the stored progress before calling this migration (see the codec).
    public bool TryExpandStages(int stageCount, out ProgressSaveData data, out string error)
    {
        data = null;
        error = null;
        if (stageCount < StageCount)
        { error = "Saved progress contains more stages than this game configuration."; return false; }
        if (stageCount == StageCount) { data = this; return true; }

        var ranks = new int[stageCount];
        for (int i = 0; i < StageCount; i++) ranks[i] = BestRanks[i];
        int unlocked = HighestUnlockedStage;
        // The old final stage had no successor when its clear was recorded.
        if (unlocked == StageCount && ranks[StageCount - 1] > 0) unlocked++;
        return TryRestore(stageCount, unlocked, ranks, out data, out error);
    }

    internal static bool TryRestore(int count, int unlocked, int[] ranks,
        out ProgressSaveData data, out string error)
    {
        data = null;
        error = null;
        if (count < 1 || unlocked < 1 || unlocked > count || ranks == null || ranks.Length != count)
        { error = "Progress stage count, unlock range or ranks are invalid."; return false; }
        for (int i = 0; i < count; i++)
        {
            if (ranks[i] < 0 || ranks[i] > 3
                || (i < unlocked - 1 && ranks[i] == 0)
                || (i >= unlocked && ranks[i] != 0))
            { error = "Progress contains an invalid rank or skips a required earlier clear."; return false; }
        }
        if (unlocked < count && ranks[unlocked - 1] != 0)
        { error = "A cleared frontier stage must unlock its successor."; return false; }
        data = new ProgressSaveData(count, unlocked, ranks);
        return true;
    }
}
