namespace Bit.BevTools;

public sealed class BevToolsStateProviderEntry
{
    internal BevToolsStateProviderEntry(IBevToolsStateProvider provider, string origin, int historySize)
    {
        Provider = provider;
        Origin = origin;
        History = new BevToolsRingBuffer<BevToolsStateChangeRecord>(historySize);
    }

    public IBevToolsStateProvider Provider { get; }

    public string Name => Provider.Name;

    public string? Description => Provider.Description;

    /// <summary>Where the provider came from: "DI", "extension", "runtime".</summary>
    public string Origin { get; }

    public int ChangeCount { get; internal set; }

    public DateTimeOffset? LastChangedAt { get; internal set; }

    internal BevToolsRingBuffer<BevToolsStateChangeRecord> History { get; }

    internal object SyncRoot { get; } = new();

    internal int Generation { get; set; }

    internal long NotificationSequence { get; set; }

    internal long NextCommitSequence { get; set; } = 1;

    internal Dictionary<long, BevToolsPendingStateChange> PendingChanges { get; } = [];

    internal Dictionary<string, string>? LastSnapshot { get; set; }

    public BevToolsStateChangeRecord[] Changes => History.ToArray();

    public string? Error { get; internal set; }
}
