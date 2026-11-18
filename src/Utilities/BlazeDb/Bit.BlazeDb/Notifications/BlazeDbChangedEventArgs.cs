namespace Bit.BlazeDb;

/// <summary>One commit's changes across every table, for <see cref="BlazeDbDatabase.Changed"/>.</summary>
public sealed class BlazeDbChangedEventArgs : EventArgs
{
    internal BlazeDbChangedEventArgs(IReadOnlyList<BlazeDbRowChange> changes, bool isReset)
    {
        Changes = changes;
        IsReset = isReset;
    }

    /// <summary>The net change to each row the commit touched, table by table.</summary>
    public IReadOnlyList<BlazeDbRowChange> Changes { get; }

    /// <summary>True when the whole database was reloaded from storage; <see cref="Changes"/> is empty then.</summary>
    public bool IsReset { get; }
}
