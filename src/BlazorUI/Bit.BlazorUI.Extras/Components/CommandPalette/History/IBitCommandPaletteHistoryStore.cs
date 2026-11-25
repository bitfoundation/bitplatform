namespace Bit.BlazorUI;

/// <summary>Persists command usage. Implement to store in a database or user profile.</summary>
public interface IBitCommandPaletteHistoryStore
{
    ValueTask<IDictionary<string, BitCommandPaletteCommandUsage>> LoadAsync(CancellationToken cancellationToken = default);

    ValueTask SaveAsync(IReadOnlyDictionary<string, BitCommandPaletteCommandUsage> usage, CancellationToken cancellationToken = default);
}
