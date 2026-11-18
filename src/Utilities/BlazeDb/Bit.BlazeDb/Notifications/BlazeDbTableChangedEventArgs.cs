namespace Bit.BlazeDb;

/// <summary>
/// One commit's changes to one table, for <see cref="BlazeDbTable{TKey, TRow}.Changed"/>.
/// </summary>
public sealed class BlazeDbTableChangedEventArgs<TKey, TRow> : EventArgs
{
    internal BlazeDbTableChangedEventArgs(IReadOnlyList<BlazeDbRowChange<TKey, TRow>> changes, bool isReset)
    {
        Changes = changes;
        IsReset = isReset;
    }

    /// <summary>The net change to each row the commit touched, in the order the rows were first written.</summary>
    public IReadOnlyList<BlazeDbRowChange<TKey, TRow>> Changes { get; }

    /// <summary>
    /// True when the table was replaced wholesale - a read-only replica reloading from storage - rather than
    /// changed row by row. <see cref="Changes"/> is empty then; re-read whatever depends on the table.
    /// </summary>
    public bool IsReset { get; }
}
