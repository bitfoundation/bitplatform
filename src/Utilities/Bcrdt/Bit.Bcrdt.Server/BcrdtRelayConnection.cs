namespace Bit.Bcrdt;

/// <summary>
/// A connection to one document as a relay keeps it, with or without a <see cref="BcrdtDocumentHost"/>: the grant the
/// relay enforces on every message, and the refusal that ends it.
/// </summary>
/// <remarks>
/// The same object is what a host sees and what <see cref="BcrdtRelayConnections"/> lists, so a refusal from either
/// one - a rejected change, a revocation - is the same refusal, and a narrowed grant is the grant both of them read.
/// </remarks>
internal abstract class BcrdtRelayConnection : IBcrdtHostConnection
{
    private int _access;
    private int _refused;
    private BcrdtRelayConnections? _registry;

    // The messages the relay is passing on for this connection, counted by when they began: a wait for those begun
    // before it retires the current generation and starts a new one, so messages that keep overlapping it - a
    // chatty client on a hub that runs several of its invocations at once - cannot keep it waiting.
    private RelayGeneration _relaying = new();

    protected BcrdtRelayConnection(string documentId, string id, BcrdtDocumentAccess access, string? userId, string relay)
    {
        DocumentId = documentId;
        Id = id;
        _access = (int)access;
        UserId = userId;
        Relay = relay;
    }

    /// <summary>The document this connection joined.</summary>
    public string DocumentId { get; }

    public string Id { get; }

    /// <summary>What the connection may do now: what it was granted on joining, or read-only once narrowed.</summary>
    public BcrdtDocumentAccess Access => (BcrdtDocumentAccess)Volatile.Read(ref _access);

    public string? UserId { get; }

    public string Relay { get; }

    /// <summary>The host keeping a replica of the document for this connection, if any. Set before the host is joined.</summary>
    public BcrdtDocumentHost? Host { get; set; }

    /// <summary>Whether the connection was refused - by the host or by a revocation; set before the refusal is even sent.</summary>
    public bool Refused => Volatile.Read(ref _refused) != 0;

    public abstract void Send(byte[] message);

    public void Refuse(string reason) => TryRefuse(reason);

    /// <summary>Refuses the connection, reporting whether this call is the one that did.</summary>
    public bool TryRefuse(string reason)
    {
        if (Interlocked.Exchange(ref _refused, 1) != 0) return false;

        Volatile.Read(ref _registry)?.Remove(this);
        OnRefused(reason);
        return true;
    }

    /// <summary>Tells the client it was refused and closes the connection. Must not block.</summary>
    protected abstract void OnRefused(string reason);

    /// <summary>Lowers a read-write grant to read-only, reporting whether this call is the one that did.</summary>
    public bool TryMakeReadOnly() =>
        !Refused &&
        Interlocked.CompareExchange(ref _access, (int)BcrdtDocumentAccess.ReadOnly, (int)BcrdtDocumentAccess.ReadWrite) == (int)BcrdtDocumentAccess.ReadWrite;

    /// <summary>
    /// Starts passing on a message the client sent, or reports that it must be dropped - <see langword="null"/>: the
    /// connection was refused, or the message is a change and the connection may not write. Call
    /// <see cref="RelayGeneration.End"/> on what it returns once the message has gone wherever it was going.
    /// </summary>
    /// <remarks>
    /// Counted before the grant is read, and a grant is changed before the count is read, so either this sees the
    /// change or <see cref="SettledAsync"/> waits for this message.
    /// </remarks>
    public RelayGeneration? TryBeginRelay(byte[] message)
    {
        RelayGeneration generation;
        do generation = Volatile.Read(ref _relaying);
        while (!generation.TryEnter());

        if (!Refused && (Access == BcrdtDocumentAccess.ReadWrite || !BcrdtSyncProtocol.IsWrite(message))) return generation;

        generation.End();
        return null;
    }

    /// <summary>Completes once no message the relay began passing on before this call is still on its way.</summary>
    /// <remarks>Messages begun after it are not waited for, however many of them overlap.</remarks>
    public Task SettledAsync()
    {
        RelayGeneration retired = Interlocked.Exchange(ref _relaying, new RelayGeneration());
        retired.End();
        return retired.Ended;
    }

    /// <summary>Remembers the list this connection is on, so a refusal takes it off.</summary>
    public void ListIn(BcrdtRelayConnections registry) => Volatile.Write(ref _registry, registry);

    /// <summary>Takes the connection off the list it is on, if any: it left or disconnected.</summary>
    public void Unlist() => Volatile.Read(ref _registry)?.Remove(this);

    /// <summary>
    /// The messages a connection began passing on between two waits for them, and when the last of them is done.
    /// </summary>
    /// <remarks>
    /// The connection holds one count of its current generation, released when the generation is retired, so the
    /// count reaches zero - and stays there - only once it is retired and every message in it has ended.
    /// </remarks>
    internal sealed class RelayGeneration
    {
        private readonly TaskCompletionSource _ended = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _count = 1;

        /// <summary>Completes once the generation is retired and every message in it has ended.</summary>
        public Task Ended => _ended.Task;

        /// <summary>Counts a message in, unless the generation is over and a newer one is taking messages.</summary>
        public bool TryEnter()
        {
            int count = Volatile.Read(ref _count);
            while (count != 0)
            {
                int seen = Interlocked.CompareExchange(ref _count, count + 1, count);
                if (seen == count) return true;
                count = seen;
            }
            return false;
        }

        /// <summary>Counts a message out: it has gone wherever it was going.</summary>
        public void End()
        {
            if (Interlocked.Decrement(ref _count) == 0) _ended.TrySetResult();
        }
    }
}
