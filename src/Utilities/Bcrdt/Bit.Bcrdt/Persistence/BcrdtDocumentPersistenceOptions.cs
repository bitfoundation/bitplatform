namespace Bit.Bcrdt;

/// <summary>Settings for binding a document to a store.</summary>
public sealed class BcrdtDocumentPersistenceOptions
{
    /// <summary>How many records may accumulate before the log is compacted.</summary>
    public int CompactionThreshold { get; init; } = 500;

    /// <summary>
    /// A tag identifying writes this component made, so they are not written straight back out.
    /// </summary>
    public static object LoadOrigin { get; } = new BcrdtDocumentPersistenceOrigin();

    /// <summary>Whether updates that arrived from peers are persisted too.</summary>
    /// <remarks>
    /// Normally yes: a client that persists only its own edits cannot show the document offline.
    /// Turn it off only on a server that already has the authoritative copy elsewhere.
    /// </remarks>
    public bool PersistRemoteUpdates { get; init; } = true;

    /// <summary>How long to wait before retrying a failed write. Doubles on each consecutive failure.</summary>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMilliseconds(250);

    /// <summary>The longest wait between retries of a failed write.</summary>
    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(30);
}
