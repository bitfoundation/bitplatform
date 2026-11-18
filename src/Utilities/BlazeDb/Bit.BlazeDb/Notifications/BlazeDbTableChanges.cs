namespace Bit.BlazeDb;

/// <summary>One table's share of a commit: per key, the row before the commit and the row after it.</summary>
internal sealed class BlazeDbTableChanges<TKey, TRow> : IBlazeDbTableChanges
    where TKey : notnull
{
    private readonly BlazeDbTable<TKey, TRow> _table;
    private readonly Dictionary<TKey, int> _positions = new();
    private readonly List<(TKey Key, bool HadOld, TRow? Old, bool HasNew, TRow? New)> _entries = [];

    public BlazeDbTableChanges(BlazeDbTable<TKey, TRow> table) => _table = table;

    /// <summary>A write that left <paramref name="row"/> under <paramref name="key"/>.</summary>
    public void Set(TKey key, bool hadOld, TRow? old, TRow row)
    {
        if (_positions.TryGetValue(key, out var position))
        {
            var entry = _entries[position];
            _entries[position] = (key, entry.HadOld, entry.Old, true, row);
        }
        else
        {
            _positions[key] = _entries.Count;
            _entries.Add((key, hadOld, old, true, row));
        }
    }

    /// <summary>A write that removed the row under <paramref name="key"/>, which held <paramref name="old"/>.</summary>
    public void Delete(TKey key, TRow old)
    {
        if (_positions.TryGetValue(key, out var position))
        {
            var entry = _entries[position];
            _entries[position] = (key, entry.HadOld, entry.Old, false, default);
        }
        else
        {
            _positions[key] = _entries.Count;
            _entries.Add((key, true, old, false, default));
        }
    }

    public void Deliver(List<BlazeDbRowChange>? all, ref List<Exception>? errors)
    {
        var changes = new List<BlazeDbRowChange<TKey, TRow>>(_entries.Count);
        foreach (var (key, hadOld, old, hasNew, row) in _entries)
        {
            // A row inserted and deleted again inside one commit never existed as far as anyone outside
            // the commit could tell, so it is not reported at all.
            if (hadOld || hasNew)
            {
                var kind = !hadOld ? BlazeDbChangeKind.Inserted : hasNew ? BlazeDbChangeKind.Updated : BlazeDbChangeKind.Deleted;
                changes.Add(new BlazeDbRowChange<TKey, TRow>(kind, key, old, row));
                all?.Add(new BlazeDbRowChange(_table.Name, kind, key, hadOld ? old : null, hasNew ? row : null));
            }
        }
        if (changes.Count == 0)
        {
            return;
        }
        _table.RaiseChanged(new BlazeDbTableChangedEventArgs<TKey, TRow>(changes, isReset: false), ref errors);
    }
}
