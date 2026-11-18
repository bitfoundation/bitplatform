namespace Bit.Balette;

/// <summary>Persists command usage. Implement to store in a database or user profile.</summary>
public interface IBaletteHistoryStore
{
    ValueTask<IDictionary<string, BaletteCommandUsage>> LoadAsync(CancellationToken cancellationToken = default);

    ValueTask SaveAsync(IReadOnlyDictionary<string, BaletteCommandUsage> usage, CancellationToken cancellationToken = default);
}
