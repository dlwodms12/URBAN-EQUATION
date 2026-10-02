using System;
using System.Collections.Generic;

public sealed class ComboPresentationQueue
{
    private readonly Queue<ComboResult> waiting = new Queue<ComboResult>();
    public ComboResult Current { get; private set; }
    public int PendingCount => waiting.Count;
    // Null means presentation was cleared or the final item was completed.
    public event Action<ComboResult> OnCurrentChanged;

    public void EnqueueResults(IReadOnlyList<ComboResult> results)
    {
        if (results == null) throw new ArgumentNullException(nameof(results));
        foreach (ComboResult result in results)
            if (result == null) throw new ArgumentException("Presentation result cannot be null.");
        foreach (ComboResult result in results) waiting.Enqueue(result);
        if (Current == null && waiting.Count > 0) Advance();
    }

    public bool TryComplete(ComboResult expected)
    {
        // Object identity rejects stale acknowledgements after Undo/reset, even if IDs repeat.
        if (expected == null || expected != Current) return false;
        Advance();
        return true;
    }

    public void Clear()
    {
        ClearSilently();
        OnCurrentChanged?.Invoke(null);
    }

    internal void ClearSilently()
    {
        waiting.Clear();
        Current = null;
    }

    private void Advance()
    {
        Current = waiting.Count == 0 ? null : waiting.Dequeue();
        OnCurrentChanged?.Invoke(Current);
    }
}
