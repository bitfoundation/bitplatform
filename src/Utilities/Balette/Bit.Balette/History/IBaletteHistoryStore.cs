namespace Bit.Balette;

/// <summary>Usage statistics for one command, used for recents, pinning and frecency ranking.</summary>
public sealed record BaletteCommandUsage(int Count, DateTimeOffset LastUsed, bool Pinned)
{
    /// <summary>
    /// When the command was pinned, which orders the pinned commands: the first pinned is listed first. Null when it is not
    /// pinned - or was pinned before this was recorded, which lists it ahead of the dated ones, by id.
    /// </summary>
    public DateTimeOffset? PinnedAt { get; init; }
}

/// <summary>Persists command usage. Implement to store in a database or user profile.</summary>
public interface IBaletteHistoryStore
{
    ValueTask<IDictionary<string, BaletteCommandUsage>> LoadAsync(CancellationToken cancellationToken = default);

    ValueTask SaveAsync(IReadOnlyDictionary<string, BaletteCommandUsage> usage, CancellationToken cancellationToken = default);
}

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
