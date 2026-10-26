using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bit.Bcrdt.Server;

/// <summary>One client connection as a <see cref="BcrdtDocumentHost"/> sees it.</summary>
/// <remarks>
/// Implemented by the relays - <see cref="BcrdtHub"/> and the WebSocket endpoint - and by anything else
/// that wants to put a hosted document behind a transport of its own.
/// </remarks>
public interface IBcrdtHostConnection
{
    /// <summary>A stable identifier for the connection, for logs. Never shown to other clients.</summary>
    string Id { get; }

    /// <summary>What the connection was granted when it joined.</summary>
    BcrdtDocumentAccess Access { get; }

    /// <summary>
    /// The authenticated user behind the connection, or <see langword="null"/> for an anonymous one.
    /// </summary>
    /// <remarks>
    /// Lets a user's new connection take over the presence identity their dropped one held at once,
    /// rather than after the old one has been silent for a while.
    /// </remarks>
    string? UserId { get; }

    /// <summary>
    /// Which relay carries messages between this connection and its peers - one value per hub type or
    /// WebSocket mapping.
    /// </summary>
    /// <remarks>
    /// A relay passes a message only to its own clients. When one host serves several relays, it passes
    /// what a client sends on to the clients of the other relays itself, so they all edit one document.
    /// </remarks>
    string Relay { get; }

    /// <summary>
    /// Queues a protocol message for this connection. Must not block and must not throw: a connection
    /// that cannot take it is the connection's problem, and the protocol repairs a lost message on the
    /// next handshake.
    /// </summary>
    void Send(byte[] message);

    /// <summary>
    /// Refuses the connection for good: tells the client why with an
    /// <see cref="BcrdtSyncMessageKind.Auth"/> refusal, then closes it. Must not block: it is called
    /// while the document is locked.
    /// </summary>
    void Refuse(string reason);
}

/// <summary>A change a client is about to make to a hosted document, offered for validation.</summary>
/// <remarks>Valid only for the duration of the callback it is passed to.</remarks>
public sealed class BcrdtHostedUpdateContext
{
    internal BcrdtHostedUpdateContext(string documentId, IBcrdtHostConnection connection, BcrdtDocument document, ReadOnlyMemory<byte> update)
    {
        DocumentId = documentId;
        Connection = connection;
        Document = document;
        Update = update;
    }

    /// <summary>The document the change is for.</summary>
    public string DocumentId { get; }

    /// <summary>The connection that sent it.</summary>
    public IBcrdtHostConnection Connection { get; }

    /// <summary>
    /// The hosted replica as it stands before the change. Read it; do not change it - the callback runs
    /// under the document's lock and a change made here would be broadcast as the server's own.
    /// </summary>
    public BcrdtDocument Document { get; }

    /// <summary>
    /// The update, in the engine's wire format. <see cref="BcrdtUpdateOperations"/> decodes it without a
    /// document - its state vector, blocks and deletions - and applying it to a copy of
    /// <see cref="Document"/> shows what it would do.
    /// </summary>
    public ReadOnlyMemory<byte> Update { get; }
}

/// <summary>Settings for a <see cref="BcrdtDocumentHost"/>.</summary>
public sealed class BcrdtDocumentHostOptions
{
    /// <summary>
    /// Where hosted documents are kept. Without one, documents live in process memory: they outlast
    /// every client leaving, but not the process.
    /// </summary>
    public IBcrdtDocumentStore? Store { get; init; }

    /// <summary>How hosted documents are written to <see cref="Store"/>.</summary>
    /// <remarks>
    /// <see cref="BcrdtDocumentPersistenceOptions.PersistRemoteUpdates"/> is ignored: a host exists to keep
    /// what its clients write, so it always persists their changes.
    /// </remarks>
    public BcrdtDocumentPersistenceOptions Persistence { get; init; } = new();

    /// <summary>
    /// How long a document stays loaded after its last connection leaves, so a page reload or a brief
    /// network drop does not cost a reload from the store.
    /// </summary>
    public TimeSpan UnloadDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Whether hosted documents reclaim deleted content. See <see cref="BcrdtDocumentOptions.GarbageCollect"/>.</summary>
    public bool GarbageCollect { get; init; } = true;

    /// <summary>
    /// Whether the connections to one document can be spread over several server instances - a SignalR
    /// backplane, say - so that no single host sees all of them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Off by default, and then the host answers every handshake on its own: it holds everything every
    /// connection has sent, so a client joining or returning is brought up to date - and its offline work
    /// collected - by the host alone, and the other clients never hear the handshake at all. Without a
    /// host, every peer in a room answers every joiner with its whole document; with one, a join costs
    /// one answer instead of one per peer.
    /// </para>
    /// <para>
    /// Turn it on when an instance may not see every connection to a document. The host then still
    /// answers, but handshakes are also relayed so that clients on other instances answer too, as they
    /// do without a host. The WebSocket endpoint keeps a document's room on one instance, so it never
    /// needs this.
    /// </para>
    /// </remarks>
    public bool ScaleOut { get; init; }

    /// <summary>
    /// The largest message the host sends a client, in bytes; a larger answer or change goes out as
    /// several. Defaults to <see cref="BcrdtSyncProtocol.DefaultMaxMessageSize"/>; zero sends every message
    /// whole.
    /// </summary>
    public int MaxMessageSize { get; init; } = BcrdtSyncProtocol.DefaultMaxMessageSize;

    /// <summary>
    /// How long a presence identity's connection must have been silent before another connection may
    /// publish under that identity: 15 seconds by default.
    /// </summary>
    /// <remarks>
    /// A client that loses its connection reconnects on a new one, usually before the server has noticed
    /// the old one is gone, and goes on publishing the same identity. A new connection of the same
    /// signed-in user takes it over at once; an anonymous one once the old connection has gone quiet for
    /// this long. Keep it above the clients' presence heartbeat (10 seconds by default), so a live owner
    /// always refreshes its entry first.
    /// </remarks>
    public TimeSpan PresenceTakeoverAfter { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Decides whether a change a client sends is accepted. Omitted means every well-formed change from
    /// a read-write connection is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is where server-side business rules go: CRDT convergence says nothing about whether a
    /// change is <i>allowed</i>, and a modified client can produce perfectly valid updates that break
    /// an application's invariants. Runs before the change is applied or relayed, under the document's
    /// lock.
    /// </para>
    /// <para>
    /// A rejected change cannot simply be dropped: the client already holds it, every later change it
    /// makes builds on it, and it would offer it again on every reconnection. So a rejection refuses the
    /// connection for good - the client is told, stops reconnecting, and has to be reopened from the
    /// server's copy. Reject what a well-behaved client never sends, and validate on the client too so
    /// that a person is told before they type something the server will refuse.
    /// </para>
    /// </remarks>
    public Func<BcrdtHostedUpdateContext, ValueTask<bool>>? ValidateUpdate { get; init; }
}

/// <summary>
/// Keeps a server-side replica of every document its relays carry: always present, always answering
/// the handshake, and persisted.
/// </summary>
/// <remarks>
/// <para>
/// A relay on its own holds nothing, so a document exists only while somebody has it open: the first
/// person into an empty room sees an empty document unless their own device kept a copy, and an edit
/// made by someone who then closes the tab before anyone else connects is gone. A host closes that gap.
/// It joins every room as one more peer - the same <see cref="BcrdtSyncExchange"/> decisions every
/// client runs - loads the document from its store when the first connection arrives, writes every
/// change any client makes back to the store, and answers every handshake with the full document.
/// </para>
/// <para>
/// The relays keep relaying with a host attached, so clients still answer each other too: the host
/// adds a replica, it does not take a role away. On top of that it is the place to enforce what a relay
/// alone cannot: it drops malformed messages instead of passing them on, and it can validate a change
/// against server-side rules before anybody else receives it (<see cref="BcrdtDocumentHostOptions.ValidateUpdate"/>).
/// </para>
/// <para>
/// Server code can read and change a hosted document with <see cref="ReadAsync"/> and
/// <see cref="EditAsync"/>; a change made there reaches every connected client and the store, exactly
/// like a client's.
/// </para>
/// <para>
/// <b>Scale-out.</b> A host keeps one replica per document per process. Behind a SignalR backplane each
/// instance hosts its own replica of a document and sees only the messages of its own connections. Every
/// change is still persisted - by the instance its author is connected to - and a replica that receives
/// a change depending on something only another instance saw asks the sender for it, so nothing is lost;
/// but the instances' in-memory replicas are not kept in step with each other, so give them a shared
/// store, and prefer routing a document's connections to one instance where the load allows it.
/// </para>
/// </remarks>
public sealed class BcrdtDocumentHost : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, HostedDocument> _documents = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private readonly BcrdtDocumentHostOptions _options;
    private readonly BcrdtDocumentPersistenceOptions _persistence;
    private readonly IBcrdtDocumentStore _store;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _shutdown = new();

    // Taken from the source once, so a message a relay delivers after disposal tests a cancelled token
    // instead of reading the token of a disposed source, which throws.
    private readonly CancellationToken _stopping;

    private static readonly UpDownCounter<long> LoadedDocumentsMetric = BcrdtDiagnostics.Meter.CreateUpDownCounter<long>(
        "bcrdt.host.documents", "{document}", "Documents a document host has loaded.");

    private static readonly UpDownCounter<long> ConnectionsMetric = BcrdtDiagnostics.Meter.CreateUpDownCounter<long>(
        "bcrdt.host.connections", "{connection}", "Connections to documents a document host has loaded.");

    private static readonly Counter<long> DroppedMetric = BcrdtDiagnostics.Meter.CreateCounter<long>(
        "bcrdt.host.messages.dropped", "{message}", "Messages a document host refused to apply or relay.");

    /// <summary>Counts a refused message by why it was refused, and refuses it.</summary>
    private static bool Drop(string reason)
    {
        DroppedMetric.Add(1, new KeyValuePair<string, object?>("bcrdt.reason", reason));
        return false;
    }
    private bool _disposed;

    /// <summary>Creates a host.</summary>
    public BcrdtDocumentHost(BcrdtDocumentHostOptions? options = null, ILogger<BcrdtDocumentHost>? logger = null)
    {
        _options = options ?? new BcrdtDocumentHostOptions();
        if (_options.UnloadDelay < TimeSpan.Zero && _options.UnloadDelay != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(options), _options.UnloadDelay, "UnloadDelay must not be negative, except Timeout.InfiniteTimeSpan to keep documents loaded.");
        }

        _store = _options.Store ?? new BcrdtInMemoryDocumentStore();
        _logger = (ILogger?)logger ?? NullLogger.Instance;
        _stopping = _shutdown.Token;

        BcrdtDocumentPersistenceOptions given = _options.Persistence ?? new BcrdtDocumentPersistenceOptions();
        _persistence = new BcrdtDocumentPersistenceOptions
        {
            CompactionThreshold = given.CompactionThreshold,
            RetryDelay = given.RetryDelay,
            MaxRetryDelay = given.MaxRetryDelay,
            PersistRemoteUpdates = true,
        };
    }

    /// <summary>The identifiers of the documents currently loaded.</summary>
    public IReadOnlyCollection<string> LoadedDocuments => [.. _documents.Keys];

    /// <summary>The origin of changes made through <see cref="EditAsync"/>, as transactions and observers see it.</summary>
    public static object EditOrigin { get; } = new HostEditOrigin();

    /// <summary>
    /// Adds a connection to a document, loading the document from the store if it is not loaded yet.
    /// </summary>
    /// <remarks>
    /// Await this before relaying anything the connection sends, so that the host is ready to answer its
    /// handshake. Dispose the returned session when the connection leaves.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The host has been disposed.</exception>
    public async ValueTask<BcrdtHostSession> JoinAsync(
        string documentId,
        IBcrdtHostConnection connection,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);
        ArgumentNullException.ThrowIfNull(connection);

        HostedDocument document = await AcquireAsync(documentId, cancellationToken).ConfigureAwait(false);
        lock (document.Connections) document.Connections.Add(connection);
        ConnectionsMetric.Add(1);
        return new BcrdtHostSession(this, document, connection);
    }

    /// <summary>Reads a hosted document, loading it if necessary.</summary>
    /// <remarks>
    /// The callback runs under the document's lock, so it sees a state no client change is halfway
    /// through. Keep it short: every client of the document waits for it.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The host has been disposed.</exception>
    public async ValueTask<T> ReadAsync<T>(string documentId, Func<BcrdtDocument, T> read, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);
        ArgumentNullException.ThrowIfNull(read);

        HostedDocument document = await AcquireAsync(documentId, cancellationToken).ConfigureAwait(false);
        try
        {
            await document.Lock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // The host was disposed while this waited for the document.
                ObjectDisposedException.ThrowIf(document.Closed, this);
                return read(document.Crdt);
            }
            finally
            {
                document.Lock.Release();
            }
        }
        finally
        {
            Release(document);
        }
    }

    /// <summary>
    /// Changes a hosted document from the server, loading it if necessary.
    /// </summary>
    /// <remarks>
    /// Everything the callback does is one transaction with <see cref="EditOrigin"/> as its origin. It
    /// is sent to every connected client as an ordinary update and written to the store; a client that
    /// is offline receives it on its next handshake. The returned task completes once the change has been
    /// applied and handed to the connections - not once it is stored; the store is written in the
    /// background like any other change.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The host has been disposed.</exception>
    public async ValueTask EditAsync(string documentId, Action<BcrdtDocument> edit, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);
        ArgumentNullException.ThrowIfNull(edit);

        HostedDocument document = await AcquireAsync(documentId, cancellationToken).ConfigureAwait(false);
        try
        {
            await document.Lock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ObjectDisposedException.ThrowIf(document.Closed, this);
                document.Crdt.Transact(_ => edit(document.Crdt), EditOrigin);
            }
            finally
            {
                document.Lock.Release();
            }
        }
        finally
        {
            Release(document);
        }
    }

    /// <summary>Waits until every change to a loaded document has reached the store.</summary>
    /// <remarks>Completes at once for a document that is not loaded: unloading flushes it.</remarks>
    public Task FlushAsync(string documentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);
        if (!_documents.TryGetValue(documentId, out HostedDocument? document) || !document.Loading.IsCompletedSuccessfully)
        {
            return Task.CompletedTask;
        }

        return document.Persistence?.FlushAsync() ?? Task.CompletedTask;
    }

    internal async ValueTask<bool> ReceiveAsync(HostedDocument document, IBcrdtHostConnection connection, ReadOnlyMemory<byte> raw)
    {
        bool relay = await HandleAsync(document, connection, raw).ConfigureAwait(false);
        if (relay) ForwardAcrossRelays(document, connection, raw);
        return relay;
    }

    /// <summary>
    /// Passes a message the sender's relay is about to deliver on to the connections it cannot reach:
    /// those of the other relays this host serves.
    /// </summary>
    private static void ForwardAcrossRelays(HostedDocument document, IBcrdtHostConnection sender, ReadOnlyMemory<byte> raw)
    {
        List<IBcrdtHostConnection>? elsewhere = null;
        lock (document.Connections)
        {
            foreach (IBcrdtHostConnection other in document.Connections)
            {
                if (!string.Equals(other.Relay, sender.Relay, StringComparison.Ordinal)) (elsewhere ??= []).Add(other);
            }
        }

        if (elsewhere is null) return;
        byte[] message = raw.ToArray();
        foreach (IBcrdtHostConnection other in elsewhere) other.Send(message);
    }

    private async ValueTask<bool> HandleAsync(HostedDocument document, IBcrdtHostConnection connection, ReadOnlyMemory<byte> raw)
    {
        BcrdtSyncMessage message;
        try
        {
            message = BcrdtSyncProtocol.Decode(raw);
        }
        catch (BcrdtFormatException ex)
        {
            // Relaying a message this build cannot read would hand every other client the same problem.
            _logger.LogDebug(ex, "Dropped a malformed message on document {DocumentId} from connection {ConnectionId}.", document.LogId, connection.Id);
            return Drop("malformed");
        }

        switch (message.Kind)
        {
            case BcrdtSyncMessageKind.Auth:
                // Only the server decides who is admitted; a client's verdict is a forgery.
                return Drop("forged_auth");

            case BcrdtSyncMessageKind.Update:
            case BcrdtSyncMessageKind.SyncStep2:
                if (connection.Access != BcrdtDocumentAccess.ReadWrite) return Drop("read_only");
                return await ApplyAsync(document, connection, message).ConfigureAwait(false);

            case BcrdtSyncMessageKind.SyncStep1:
            case BcrdtSyncMessageKind.SyncStep1Reply:
                // The host holds everything every connection sent, so unless some connections are on
                // other instances its answer is the whole answer, and the room need not hear the question.
                if (!await AnswerAsync(document, connection, message).ConfigureAwait(false)) return Drop("malformed");
                return _options.ScaleOut;

            case BcrdtSyncMessageKind.AwarenessUpdate:
                return PoliceAwareness(document, connection, message.Payload, (long)_options.PresenceTakeoverAfter.TotalMilliseconds);

            default:
                // Presence queries belong to the clients; the host has no presence of its own.
                return true;
        }
    }

    /// <summary>
    /// Lets a connection speak only for itself in presence: a client's entry belongs to the first
    /// connection that published it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Presence carries no CRDT guarantees and no signatures, so on a bare relay any member can publish
    /// an entry under another participant's identity - a fake name on their cursor - or announce them
    /// gone. Here an entry with state is accepted only from the connection that owns that client, and
    /// a departure announced for somebody else's client only at a clock that client has already
    /// published, which is what a peer timing them out legitimately sends and which their own next
    /// update supersedes. A message breaking either rule is dropped whole.
    /// </para>
    /// <para>
    /// Owning an entry also lets the host announce its departure the moment the connection goes, rather
    /// than leaving everyone else to wait out the presence timeout.
    /// </para>
    /// </remarks>
    private static bool PoliceAwareness(HostedDocument document, IBcrdtHostConnection connection, ReadOnlyMemory<byte> payload, long takeoverAfterMilliseconds)
    {
        List<(ulong ClientId, long Clock, bool Online)> entries;
        try
        {
            entries = BcrdtAwarenessStore.ReadEntries(payload);
        }
        catch (BcrdtFormatException)
        {
            return Drop("malformed");
        }

        long now = Environment.TickCount64;
        lock (document.Connections)
        {
            int owned = 0;
            foreach (AwarenessClaim claim in document.AwarenessClaims.Values)
            {
                if (ReferenceEquals(claim.Owner, connection)) owned++;
            }

            HashSet<ulong>? claiming = null;
            foreach ((ulong clientId, long clock, bool online) in entries)
            {
                if (document.AwarenessClaims.TryGetValue(clientId, out AwarenessClaim? claim))
                {
                    if (ReferenceEquals(claim.Owner, connection)) continue;

                    // A client that lost its connection comes back on a new one before the server has
                    // noticed the old one is gone, publishing the same identity at a later clock.
                    if (online && clock > claim.Clock && CanTakeOver(claim, connection, now, takeoverAfterMilliseconds))
                    {
                        (claiming ??= []).Add(clientId);
                        continue;
                    }

                    if (online || clock > claim.Clock) return Drop("forged_presence");
                }
                else if (online)
                {
                    (claiming ??= []).Add(clientId);
                }
            }

            if (claiming is not null && owned + claiming.Count > MaxAwarenessClientsPerConnection) return Drop("presence_limit");

            foreach ((ulong clientId, long clock, bool online) in entries)
            {
                if (document.AwarenessClaims.TryGetValue(clientId, out AwarenessClaim? claim))
                {
                    if (!ReferenceEquals(claim.Owner, connection) && claiming?.Contains(clientId) != true) continue;

                    claim.Owner = connection;
                    claim.LastActive = now;
                    if (clock > claim.Clock) claim.Clock = clock;
                }
                else if (online)
                {
                    document.AwarenessClaims[clientId] = new AwarenessClaim(connection, clock) { LastActive = now };
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Whether <paramref name="connection"/> may take over a presence identity another connection holds:
    /// at once when both are the same signed-in user, otherwise once the holder has gone quiet.
    /// </summary>
    private static bool CanTakeOver(AwarenessClaim claim, IBcrdtHostConnection connection, long now, long takeoverAfterMilliseconds) =>
        (connection.UserId is { } user && string.Equals(claim.Owner.UserId, user, StringComparison.Ordinal)) ||
        now - claim.LastActive > takeoverAfterMilliseconds;

    /// <summary>A fingerprint of deletions waiting for their targets, zero when none are.</summary>
    /// <remarks>
    /// The waiting set is rebuilt on every update applied, so its identity says nothing about whether it
    /// grew; its content does.
    /// </remarks>
    private static ulong Digest(BcrdtDeleteSet? waiting)
    {
        if (waiting is null) return 0;
        BcrdtDeleteSet canonical = waiting.Clone();
        canonical.SortAndMerge();
        return BcrdtSyncProtocol.ComputeDeletionDigest(canonical);
    }

    /// <summary>
    /// How many presence identities one connection may publish. A client publishes one; a few more
    /// allow for a replica that changed identity mid-session.
    /// </summary>
    private const int MaxAwarenessClientsPerConnection = 4;

    /// <summary>
    /// Takes a document's lock for a message a connection sent, or reports that the document is closed:
    /// the host was disposed, and the relay delivering the message has not noticed yet.
    /// </summary>
    /// <remarks>
    /// A message arriving then is dropped quietly - its client reconnects to whatever serves the
    /// document next and repairs the loss in the handshake - rather than failing the relay's call.
    /// </remarks>
    private async ValueTask<bool> TryEnterAsync(HostedDocument document)
    {
        try
        {
            await document.Lock.WaitAsync(_stopping).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        if (!document.Closed) return true;

        document.Lock.Release();
        return false;
    }

    private async ValueTask<bool> ApplyAsync(HostedDocument document, IBcrdtHostConnection connection, BcrdtSyncMessage message)
    {
        if (!await TryEnterAsync(document).ConfigureAwait(false)) return Drop("closed");
        try
        {
            if (_options.ValidateUpdate is { } validate)
            {
                bool accepted;
                try
                {
                    accepted = await validate(new BcrdtHostedUpdateContext(document.Id, connection, document.Crdt, message.Payload)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // A validator that fails has not accepted anything.
                    _logger.LogError(ex, "Update validation threw for document {DocumentId}; the change was refused.", document.LogId);
                    accepted = false;
                }

                if (!accepted)
                {
                    _logger.LogInformation("Refused connection {ConnectionId} on document {DocumentId}: a change it sent was rejected.", connection.Id, document.LogId);
                    connection.Refuse("A change was rejected by the server.");
                    return Drop("rejected");
                }
            }

            BcrdtStructStore store = document.Crdt.Store;
            byte[]? pendingBefore = store.PendingUpdate;
            ulong pendingDeletionsBefore = Digest(store.PendingDeleteSet);

            List<byte[]> replies = [];
            try
            {
                BcrdtSyncExchange.Handle(document.Crdt, message, connection, replies, _options.MaxMessageSize);
            }
            catch (BcrdtFormatException ex)
            {
                _logger.LogDebug(ex, "Dropped a malformed update on document {DocumentId} from connection {ConnectionId}.", document.LogId, connection.Id);
                return Drop("malformed");
            }
            catch (InvalidOperationException ex)
            {
                // Well-formed, but part of it could not be placed. What could be was applied; what
                // could not would fail on every other replica in the same way, so it is not passed on.
                _logger.LogWarning(ex, "Part of an update on document {DocumentId} could not be integrated.", document.LogId);
                return Drop("unintegrable");
            }

            // The change built on something this replica has not seen - over a backplane, a change
            // another instance relayed. Ask the sender, who evidently has it, so the host can integrate
            // and store both rather than holding this one back indefinitely. Only for an incremental
            // change that added to what is waiting: a SyncStep2 is already the sender's answer to such a
            // question, and asking again whenever anything at all is waiting would trade handshakes
            // with every client for as long as something nobody has stays unresolved.
            bool newlyWaiting =
                !ReferenceEquals(pendingBefore, store.PendingUpdate) ||
                pendingDeletionsBefore != Digest(store.PendingDeleteSet);
            if (message.Kind == BcrdtSyncMessageKind.Update && newlyWaiting && document.Crdt.HasPendingUpdates)
            {
                connection.Send(BcrdtSyncExchange.CreateAnnouncement(document.Crdt));
            }

            return true;
        }
        finally
        {
            document.Lock.Release();
        }
    }

    /// <summary>Answers a handshake to the connection that sent it, and says whether it was well-formed.</summary>
    private async ValueTask<bool> AnswerAsync(HostedDocument document, IBcrdtHostConnection connection, BcrdtSyncMessage message)
    {
        List<byte[]> replies = [];

        // A closed document cannot answer, but that says nothing against the message.
        if (!await TryEnterAsync(document).ConfigureAwait(false)) return true;
        try
        {
            BcrdtSyncExchange.Handle(document.Crdt, message, connection, replies, _options.MaxMessageSize);
        }
        catch (BcrdtFormatException ex)
        {
            // Relaying it would hand every other client the same problem.
            _logger.LogDebug(ex, "Dropped a malformed handshake on document {DocumentId} from connection {ConnectionId}.", document.LogId, connection.Id);
            return false;
        }
        finally
        {
            document.Lock.Release();
        }

        // Answered to the asker alone: the others did not ask.
        foreach (byte[] reply in replies) connection.Send(reply);
        return true;
    }

    internal void Leave(HostedDocument document, IBcrdtHostConnection connection)
    {
        ConnectionsMetric.Add(-1);
        List<(ulong ClientId, long Clock)>? departed = null;
        IBcrdtHostConnection[] remaining;
        lock (document.Connections)
        {
            document.Connections.Remove(connection);
            foreach ((ulong clientId, AwarenessClaim claim) in document.AwarenessClaims)
            {
                if (ReferenceEquals(claim.Owner, connection)) (departed ??= []).Add((clientId, claim.Clock));
            }
            if (departed is not null)
            {
                foreach ((ulong clientId, long _) in departed) document.AwarenessClaims.Remove(clientId);
            }
            remaining = [.. document.Connections];
        }

        // A tab that crashed or lost its network never says goodbye. Saying it on its behalf takes it
        // off everyone's participant list now rather than after the presence timeout; a client that
        // did say goodbye has already moved past this clock, so for it this changes nothing.
        if (departed is not null && remaining.Length > 0)
        {
            byte[] removal = BcrdtSyncProtocol.EncodeAwarenessUpdate(BcrdtAwarenessStore.EncodeRemovals(departed));
            foreach (IBcrdtHostConnection other in remaining) other.Send(removal);
        }

        Release(document);
    }

    private async ValueTask<HostedDocument> AcquireAsync(string documentId, CancellationToken cancellationToken)
    {
        HostedDocument document;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            document = _documents.GetOrAdd(documentId, id => new HostedDocument(id, LoadAsync));
            document.Pins++;
            document.CancelUnload();
        }

        try
        {
            await document.Loading.WaitAsync(cancellationToken).ConfigureAwait(false);
            return document;
        }
        catch
        {
            if (document.Loading.IsFaulted)
            {
                // Forget the failure so the next connection tries again instead of inheriting it.
                lock (_gate) _documents.TryRemove(new KeyValuePair<string, HostedDocument>(documentId, document));
            }

            Release(document);
            throw;
        }
    }

    private void Release(HostedDocument document)
    {
        lock (_gate)
        {
            document.Pins--;
            if (document.Pins > 0 || _disposed) return;

            if (!document.Loading.IsCompletedSuccessfully)
            {
                // Everyone gave up while it was loading, or the load failed. Either way nobody will use
                // this instance again; one still loading is closed once it has finished, so its writer
                // does not outlive it.
                document.Retired = true;
                _documents.TryRemove(new KeyValuePair<string, HostedDocument>(document.Id, document));
                if (!document.Loading.IsCompleted) _ = CloseWhenLoadedAsync(document);
                return;
            }

            document.ScheduleUnload(_options.UnloadDelay, () => _ = UnloadAsync(document));
        }
    }

    private async Task UnloadAsync(HostedDocument document)
    {
        lock (_gate)
        {
            // Someone arrived while the timer was firing; the document stays.
            if (document.Pins > 0 || document.Retired || _disposed) return;
        }

        // Flushed while it is still registered, so somebody joining meanwhile gets this instance - with
        // every change in it - rather than a fresh one loaded from a store the last changes have not
        // reached yet. With every client gone the host is the only holder of those changes: if the store
        // will not take them, the document stays loaded and the unload is tried again later.
        try
        {
            if (document.Persistence is { } persistence)
            {
                await persistence.FlushAsync().WaitAsync(FlushBeforeUnload).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Document {DocumentId} could not be flushed; it stays loaded and is tried again later.", document.LogId);
            lock (_gate)
            {
                if (document.Pins == 0 && !document.Retired && !_disposed)
                {
                    document.ScheduleUnload(RetryUnloadAfter(_options.UnloadDelay), () => _ = UnloadAsync(document));
                }
            }
            return;
        }

        lock (_gate)
        {
            if (document.Pins > 0 || document.Retired || _disposed) return;
            document.Retired = true;
            _documents.TryRemove(new KeyValuePair<string, HostedDocument>(document.Id, document));
        }

        try
        {
            await CloseAsync(document).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document {DocumentId} did not unload cleanly.", document.LogId);
        }
    }

    /// <summary>How long an unload waits for the store before giving up for now.</summary>
    private static readonly TimeSpan FlushBeforeUnload = TimeSpan.FromSeconds(30);

    private static TimeSpan RetryUnloadAfter(TimeSpan unloadDelay) =>
        unloadDelay > TimeSpan.FromMinutes(1) || unloadDelay == Timeout.InfiniteTimeSpan ? unloadDelay : TimeSpan.FromMinutes(1);

    private async Task CloseWhenLoadedAsync(HostedDocument document)
    {
        try
        {
            await document.Loading.ConfigureAwait(false);
        }
        catch (Exception)
        {
            // A failed load holds nothing to close, and whoever was waiting for it was told.
            return;
        }

        try
        {
            await CloseAsync(document).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document {DocumentId} did not close cleanly.", document.LogId);
        }
    }

    private async Task LoadAsync(HostedDocument document)
    {
        var crdt = new BcrdtDocument(new BcrdtDocumentOptions { Id = document.Id, GarbageCollect = _options.GarbageCollect });
        try
        {
            document.Persistence = await BcrdtDocumentPersistence
                .AttachAsync(crdt, _store, document.Id, _persistence, _stopping)
                .ConfigureAwait(false);

            document.Persistence.WriteFailed += error =>
                _logger.LogError(error, "Writing document {DocumentId} to the store failed; the write is retried.", document.LogId);
        }
        catch
        {
            crdt.Dispose();
            throw;
        }

        crdt.Updated += args => OnDocumentUpdated(document, args, _options.MaxMessageSize);
        document.Crdt = crdt;
        LoadedDocumentsMetric.Add(1);
    }

    /// <summary>Sends a change made on the server to every connection.</summary>
    /// <remarks>
    /// A client's change is not sent from here: the relay already passes the message it arrived in to
    /// everybody else, and sending it again would double the traffic for nothing.
    /// </remarks>
    private static void OnDocumentUpdated(HostedDocument document, BcrdtUpdateEventArgs args, int maxMessageSize)
    {
        if (!ReferenceEquals(args.Origin, EditOrigin)) return;

        IReadOnlyList<byte[]> messages = BcrdtSyncProtocol.EncodeDelta(BcrdtSyncMessageKind.Update, args.Update, maxMessageSize);
        IBcrdtHostConnection[] connections;
        lock (document.Connections) connections = [.. document.Connections];
        foreach (IBcrdtHostConnection connection in connections)
        {
            foreach (byte[] message in messages) connection.Send(message);
        }
    }

    private static async Task CloseAsync(HostedDocument document)
    {
        // Waits out a change in progress, so the writer is closed after the last change it must store.
        await document.Lock.WaitAsync().ConfigureAwait(false);
        try
        {
            // First, so whatever is waiting for the lock finds the document gone rather than a replica
            // and a writer halfway through being disposed.
            document.Closed = true;
            if (document.Persistence is { } persistence) await persistence.DisposeAsync().ConfigureAwait(false);
            if (document.Crdt is { } crdt)
            {
                crdt.Dispose();
                LoadedDocumentsMetric.Add(-1);
            }
        }
        finally
        {
            document.Lock.Release();
        }
    }

    /// <inheritdoc/>
    /// <remarks>Every loaded document's pending writes are given a bounded chance to reach the store.</remarks>
    public async ValueTask DisposeAsync()
    {
        HostedDocument[] documents;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            documents = [.. _documents.Values];
            _documents.Clear();
            foreach (HostedDocument document in documents) document.CancelUnload();
        }

        foreach (HostedDocument document in documents)
        {
            try
            {
                await document.Loading.ConfigureAwait(false);
                await CloseAsync(document).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Document {DocumentId} did not close cleanly.", document.LogId);
            }
        }

        await _shutdown.CancelAsync().ConfigureAwait(false);
        _shutdown.Dispose();
    }

    private sealed class HostEditOrigin
    {
        public override string ToString() => "document-host";
    }

    /// <summary>A presence identity, the connection that publishes it, and the last clock it published at.</summary>
    internal sealed class AwarenessClaim(IBcrdtHostConnection owner, long clock)
    {
        public IBcrdtHostConnection Owner { get; set; } = owner;

        public long Clock { get; set; } = clock;

        /// <summary>When the owner last published this identity, by <see cref="Environment.TickCount64"/>.</summary>
        public long LastActive { get; set; }
    }

    /// <summary>One loaded document: the replica, its writer, and who is connected to it.</summary>
    internal sealed class HostedDocument
    {
        private CancellationTokenSource? _unload;

        public HostedDocument(string id, Func<HostedDocument, Task> load)
        {
            Id = id;
            LogId = id.Replace('\r', ' ').Replace('\n', ' ');

            // Started on the pool rather than inline: this runs under the host's gate, and a store's
            // load can do real work before its first await.
            Loading = Task.Run(() => load(this));
        }

        public string Id { get; }

        /// <summary>
        /// <see cref="Id"/> as it goes into log entries. The client names the document, so its line breaks
        /// are flattened to keep it from forging entries of its own.
        /// </summary>
        public string LogId { get; }

        /// <summary>Serializes every use of <see cref="Crdt"/>, which is not thread-safe.</summary>
        public SemaphoreSlim Lock { get; } = new(1, 1);

        public List<IBcrdtHostConnection> Connections { get; } = [];

        /// <summary>Which connection owns which presence identity. Guarded by <see cref="Connections"/>.</summary>
        public Dictionary<ulong, AwarenessClaim> AwarenessClaims { get; } = [];

        public Task Loading { get; }

        public BcrdtDocument Crdt { get; set; } = null!;

        public BcrdtDocumentPersistence? Persistence { get; set; }

        /// <summary>Sessions and operations holding the document loaded. Guarded by the host's gate.</summary>
        public int Pins { get; set; }

        /// <summary>Set once the document has been taken out of the host. Guarded by the host's gate.</summary>
        public bool Retired { get; set; }

        /// <summary>Set once the replica and its writer are disposed, or about to be. Guarded by <see cref="Lock"/>.</summary>
        public bool Closed { get; set; }

        public void ScheduleUnload(TimeSpan delay, Action unload)
        {
            CancelUnload();
            var cancellation = new CancellationTokenSource();
            _unload = cancellation;
            // Never inline: this is scheduled under the host's gate, and the unload takes that gate.
            _ = Task.Delay(delay, cancellation.Token).ContinueWith(
                static (task, state) =>
                {
                    if (!task.IsCanceled) ((Action)state!)();
                },
                unload,
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);
        }

        public void CancelUnload()
        {
            CancellationTokenSource? pending = _unload;
            _unload = null;
            if (pending is null) return;
            pending.Cancel();
            pending.Dispose();
        }
    }
}

/// <summary>One connection's membership of a hosted document.</summary>
/// <remarks>Created by <see cref="BcrdtDocumentHost.JoinAsync"/>; dispose it when the connection leaves.</remarks>
public sealed class BcrdtHostSession : IDisposable
{
    private readonly BcrdtDocumentHost _host;
    private readonly BcrdtDocumentHost.HostedDocument _document;
    private readonly IBcrdtHostConnection _connection;
    private int _disposed;

    internal BcrdtHostSession(BcrdtDocumentHost host, BcrdtDocumentHost.HostedDocument document, IBcrdtHostConnection connection)
    {
        _host = host;
        _document = document;
        _connection = connection;
    }

    /// <summary>The document this session belongs to.</summary>
    public string DocumentId => _document.Id;

    /// <summary>
    /// Handles a message the connection sent, and says whether the relay should pass it on to the
    /// document's other connections.
    /// </summary>
    /// <remarks>
    /// <see langword="false"/> for anything the other clients must not see: a malformed message, an
    /// <see cref="BcrdtSyncMessageKind.Auth"/> verdict, a change from a read-only connection, a change the
    /// validator rejected. Messages from one connection must be passed in the order they arrived.
    /// </remarks>
    public ValueTask<bool> ReceiveAsync(ReadOnlyMemory<byte> message)
    {
        if (Volatile.Read(ref _disposed) != 0) return ValueTask.FromResult(false);
        return _host.ReceiveAsync(_document, _connection, message);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _host.Leave(_document, _connection);
    }
}
