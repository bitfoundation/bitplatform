namespace Bit.Balette;

/// <summary>Keeps usage in memory for the lifetime of the scope (circuit / tab).</summary>
public sealed class BaletteInMemoryHistoryStore : IBaletteHistoryStore
{
    private Dictionary<string, BaletteCommandUsage> _data = new(StringComparer.Ordinal);

    public ValueTask<IDictionary<string, BaletteCommandUsage>> LoadAsync(CancellationToken cancellationToken = default)
        => new(new Dictionary<string, BaletteCommandUsage>(_data, StringComparer.Ordinal));

    public ValueTask SaveAsync(IReadOnlyDictionary<string, BaletteCommandUsage> usage, CancellationToken cancellationToken = default)
    {
        _data = new Dictionary<string, BaletteCommandUsage>(usage, StringComparer.Ordinal);
        return ValueTask.CompletedTask;
    }
}
