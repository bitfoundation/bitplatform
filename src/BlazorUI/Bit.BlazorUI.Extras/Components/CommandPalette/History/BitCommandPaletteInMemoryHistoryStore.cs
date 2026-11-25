namespace Bit.BlazorUI;

/// <summary>Keeps usage in memory for the lifetime of the scope (circuit / tab).</summary>
public sealed class BitCommandPaletteInMemoryHistoryStore : IBitCommandPaletteHistoryStore
{
    private Dictionary<string, BitCommandPaletteCommandUsage> _data = new(StringComparer.Ordinal);

    public ValueTask<IDictionary<string, BitCommandPaletteCommandUsage>> LoadAsync(CancellationToken cancellationToken = default)
        => new(new Dictionary<string, BitCommandPaletteCommandUsage>(_data, StringComparer.Ordinal));

    public ValueTask SaveAsync(IReadOnlyDictionary<string, BitCommandPaletteCommandUsage> usage, CancellationToken cancellationToken = default)
    {
        _data = new Dictionary<string, BitCommandPaletteCommandUsage>(usage, StringComparer.Ordinal);
        return ValueTask.CompletedTask;
    }
}
