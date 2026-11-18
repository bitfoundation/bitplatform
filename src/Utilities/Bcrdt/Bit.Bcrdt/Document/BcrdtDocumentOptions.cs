namespace Bit.Bcrdt;

/// <summary>Configuration for a new <see cref="BcrdtDocument"/>.</summary>
public sealed class BcrdtDocumentOptions
{
    /// <summary>An application-level identifier for the document. Not used by the engine.</summary>
    public string? Id { get; init; }

    /// <summary>An explicit client identifier. Leave unset to have one generated.</summary>
    /// <remarks>Set this only in tests; two live sessions sharing an identifier will not converge.</remarks>
    public ulong? ClientId { get; init; }

    /// <summary>Where generated client identifiers come from.</summary>
    public IBcrdtClientIdProvider ClientIdProvider { get; init; } = BcrdtRandomClientIdProvider.Instance;

    /// <summary>
    /// Whether deleted content is reclaimed automatically at the end of each transaction.
    /// </summary>
    /// <remarks>
    /// Leave this on unless the application needs to reconstruct historical states from tombstones.
    /// Turning it off makes documents grow without bound in the presence of deletions.
    /// </remarks>
    public bool GarbageCollect { get; init; } = true;

    /// <summary>The default for <see cref="MaxPendingSize"/>: 32 MiB.</summary>
    public const long DefaultMaxPendingSize = 32L * 1024 * 1024;

    /// <summary>
    /// Roughly how many bytes of received changes the document holds while they wait for something
    /// they build on, before it discards them. Zero or less removes the bound.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A change that builds on content the replica has not received yet cannot be placed, so it is
    /// kept until that content arrives (<see cref="BcrdtDocument.HasPendingUpdates"/>). Without a bound
    /// a peer could grow a replica's memory without limit just by sending such changes on purpose.
    /// </para>
    /// <para>
    /// Discarding them loses nothing for good: nothing in them was applied, so the document's state
    /// vector still says it lacks them, and the next handshake brings them - and the deletions that were
    /// waiting with them - back. <see cref="BcrdtDocument.PendingUpdatesDiscarded"/> reports it, and a
    /// <see cref="BcrdtSyncProvider"/> answers it with that handshake.
    /// </para>
    /// </remarks>
    public long MaxPendingSize { get; init; } = DefaultMaxPendingSize;
}
