using System;
using UnityEngine;

public static class ProgressSaveCodec
{
    // Explicit fields keep runtime state and Unity object references out of JSON.
    [Serializable]
    private sealed class Document
    {
        public int version;
        public int stageCount;
        public int highestUnlockedStage;
        public int[] bestRanks;
    }

    public static bool TrySerialize(ProgressSaveData data, out string json, out string error)
    {
        json = null;
        error = null;
        if (data == null) { error = "Progress is missing."; return false; }
        json = JsonUtility.ToJson(new Document
        {
            version = ProgressSaveData.CurrentVersion,
            stageCount = data.StageCount,
            highestUnlockedStage = data.HighestUnlockedStage,
            bestRanks = data.CopyRanks()
        }, true);
        return true;
    }

    public static bool TryDeserialize(string json, int expectedStageCount,
        out ProgressSaveData data, out string error)
    {
        data = null;
        error = null;
        if (expectedStageCount < 1 || string.IsNullOrWhiteSpace(json))
        { error = "Save JSON and configured stage count are required."; return false; }
        string source = json.Trim();
        if (!source.StartsWith("{", StringComparison.Ordinal) || !source.EndsWith("}", StringComparison.Ordinal))
        { error = "Save must contain a JSON object."; return false; }
        Document document;
        try { document = JsonUtility.FromJson<Document>(source); }
        catch (ArgumentException exception)
        { error = "Could not decode save: " + exception.Message; return false; }
        if (document == null || document.version != ProgressSaveData.CurrentVersion)
        { error = "Save version is missing or unsupported."; return false; }
        if (document.stageCount != expectedStageCount)
        { error = "Save stage count does not match this game configuration."; return false; }
        return ProgressSaveData.TryRestore(document.stageCount, document.highestUnlockedStage,
            document.bestRanks, out data, out error);
    }
}
