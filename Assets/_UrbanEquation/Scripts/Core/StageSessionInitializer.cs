using System;
using System.Collections.Generic;

// Validate definitions before touching live managers. Unexpected initialization failures fail closed.
public static class StageSessionInitializer
{
    public static bool TryValidate(GameSessionManager session, StageData stage,
        ComboDatabase database, out string error)
    {
        error = null;
        if (session == null || session.IsBusy || session.Board == null || session.Resources == null
            || session.Hand == null || session.BuildingPrefab == null || session.Combos == null
            || session.Combos.IsResolving || session.Stage == null || session.Stage.IsBusy || session.History == null
            || session.History.Session != session || stage == null || database == null)
        { error = "Stage initialization requires an idle session and all configured managers/data."; return false; }
        var errors = new List<string>();
        stage.Validate(errors);
        database.Validate(errors);
        foreach (TileData tile in stage.Tiles)
        {
            if (tile != null && tile.TilePrefab == null) errors.Add("A stage tile has no prefab.");
            if (tile != null && !Enum.IsDefined(typeof(TileType), tile.TileType))
                errors.Add("A stage tile has an unknown type.");
        }
        long cardCount = 0;
        var buildings = new Dictionary<int, BuildingData>();
        foreach (StageBuildingCardData entry in stage.BuildingCards)
        {
            if (entry == null || entry.Building == null) continue;
            entry.Building.Validate(errors);
            if (entry.Building.VisualPrefab == null) errors.Add("A stage building has no visual prefab.");
            if (buildings.TryGetValue(entry.Building.BuildingCode, out BuildingData previous)
                && previous != entry.Building) errors.Add("Stage contains conflicting building definitions.");
            buildings[entry.Building.BuildingCode] = entry.Building;
            cardCount += entry.Count;
        }
        if (cardCount > int.MaxValue) errors.Add("Stage card count exceeds the supported integer range.");
        error = errors.Count == 0 ? null : string.Join("; ", errors);
        return errors.Count == 0;
    }

    public static bool TryInitialize(GameSessionManager session, StageData stage,
        ComboDatabase database, out string error)
    {
        if (!TryValidate(session, stage, database, out error)) return false;
        if (!session.TrySetGameplayEnabled(false, out error)) return false;
        try
        {
            if (!session.Board.TryCreateBoard(stage, out error)) return false;
            session.ClearResolvedBuildResults();
            if (!session.Resources.TryInitializeFromStage(stage))
            { error = "Could not initialize stage resources."; return false; }
            return session.Hand.TryInitializeFromStage(stage, session.Resources, out error)
                && session.Combos.TryConfigure(session.Board, session.Resources, database, out error)
                && session.Stage.TryConfigure(stage, session.Board, session.Resources, session.Hand, out error)
                && session.History.TryConfigure(session, session.Stage, out error);
        }
        catch (Exception exception)
        {
            error = "Could not initialize stage session: " + exception.Message;
            return false;
        }
    }

    public static bool TryDiscard(GameSessionManager session, ComboDatabase database, out string error)
    {
        StageData stage = session == null || session.Stage == null ? null : session.Stage.CurrentStage;
        if (!TryValidate(session, stage, database, out error)) return false;
        if (!session.TrySetGameplayEnabled(false, out error)) return false;
        // Keep the empty tile layout until the next entry; no live turn is retained.
        session.ClearResolvedBuildResults();
        session.Board.ResetBoard();
        if (!session.Resources.TryInitializeFromStage(stage))
        { error = "Could not reset stage resources."; return false; }
        session.Hand.ResetCards();
        if (!session.Combos.TryConfigure(session.Board, session.Resources, database, out error)) return false;
        session.Stage.ResetStage();
        if (session.History.TryClearHistory()) return true;
        error = "Could not discard turn history.";
        return false;
    }
}
