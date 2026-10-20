namespace Bit.Bsqlite.Data;

/// <summary>The tables a completed write touched.</summary>
public sealed class BsqliteTablesChangedEventArgs(IReadOnlyCollection<string> tables) : EventArgs
{
    public IReadOnlySet<string> Tables { get; } = tables as IReadOnlySet<string>
        ?? new HashSet<string>(tables, StringComparer.OrdinalIgnoreCase);
}
