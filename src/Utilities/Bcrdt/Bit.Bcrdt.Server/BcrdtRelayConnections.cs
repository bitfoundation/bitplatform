using System.Collections.Concurrent;

namespace Bit.Bcrdt.Server;

/// <summary>A connection a relay carries, as <see cref="BcrdtRelayConnections"/> lists it.</summary>
/// <remarks>A snapshot: it holds nothing of the document, and does not change when the connection does.</remarks>
/// <param name="DocumentId">The document the connection works on.</param>
/// <param name="ConnectionId">
/// The relay's identifier for the connection: SignalR's connection identifier, or the HTTP connection's for a WebSocket.
/// </param>
/// <param name="UserId">The authenticated user behind the connection, or <see langword="null"/> for an anonymous one.</param>
/// <param name="Access">What the connection may do with the document when the snapshot was taken.</param>
/// <param name="Relay">Which relay carries it. See <see cref="IBcrdtHostConnection.Relay"/>.</param>
public sealed record BcrdtRelayConnectionInfo(string DocumentId, string ConnectionId, string? UserId, BcrdtDocumentAccess Access, string Relay);

/// <summary>
/// The connections this process's relays carry - <see cref="BcrdtHub"/> and every endpoint mapped with
/// <see cref="BcrdtWebSocketEndpoint.MapBitBcrdtWebSocket"/> - so that a grant can be ended or narrowed while the
/// connection is open, with or without a <see cref="BcrdtDocumentHost"/>.
/// </summary>
/// <remarks>
/// <para>
/// A relay checks a grant once, when a client joins a document, and a connection that is in use can stay open for
/// days. Without a list of who is connected, taking a document away from somebody would mean waiting for them to
/// reconnect - which a client that is working never does. The relays add a connection here once it has joined a
/// document and take it off when it leaves, disconnects or is refused, so the list holds live connections only, and
/// nothing of any document's content.
/// </para>
/// <para>
/// Registered by <c>AddBitBcrdtHub()</c>, and by <c>AddBitBcrdtRelayConnections()</c> for an application that maps
/// only the WebSocket endpoint; resolve it from the container. The WebSocket endpoint uses the registered one unless
/// <see cref="BcrdtWebSocketEndpointOptions.Connections"/> names another.
/// </para>
/// <para>
/// <b>Per process.</b> A connection is listed by the server instance it is connected to. Behind a SignalR backplane,
/// or with a WebSocket endpoint on several instances, revoke on every instance - publish the revocation on a bus
/// every instance listens to - and change the authorization hook first, so that nobody gets back in by reconnecting
/// to another instance.
/// </para>
/// </remarks>
public sealed class BcrdtRelayConnections
{
    // One flat list rather than one per document: revoking is rare enough that a scan costs nothing worth saving,
    // and a flat list has no per-document entry to retire - nothing to leak, and no retirement race.
    private readonly ConcurrentDictionary<BcrdtRelayConnection, byte> _connections = new();

    /// <summary>The documents that have at least one connection.</summary>
    public IReadOnlyCollection<string> Documents => [.. _connections.Keys.Select(connection => connection.DocumentId).Distinct(StringComparer.Ordinal)];

    /// <summary>Who is connected to a document: one entry per connection.</summary>
    /// <param name="documentId">The document.</param>
    public IReadOnlyList<BcrdtRelayConnectionInfo> GetConnections(string documentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);

        return [.. Of(documentId).Select(connection => new BcrdtRelayConnectionInfo(connection.DocumentId, connection.Id, connection.UserId, connection.Access, connection.Relay))];
    }

    /// <summary>
    /// Takes a document away from the connections that no longer may have it - a user removed from a shared
    /// document, an account disabled - rather than waiting for them to reconnect.
    /// </summary>
    /// <param name="documentId">The document.</param>
    /// <param name="revoke">Which of the document's connections lose it; <see cref="IBcrdtHostConnection.UserId"/> is the usual test.</param>
    /// <param name="reason">What the clients are told. Defaults to <see cref="BcrdtDocumentHost.DefaultRevocationReason"/>.</param>
    /// <returns>How many connections were revoked.</returns>
    /// <remarks>
    /// <para>
    /// Each revoked connection is refused the way its relay refuses one: the client hears an
    /// <see cref="BcrdtSyncMessageKind.Auth"/> refusal and stops reconnecting, and the relay closes the connection.
    /// A message the relay was already passing on when this ran goes where it was going - this waits for it - but
    /// once this completes nothing the connection sends is relayed, and nothing more is relayed to it.
    /// </para>
    /// <para>
    /// A connection to a hosted document is revoked in the <see cref="BcrdtDocumentHost"/> too, so its presence is
    /// withdrawn from everyone else at once and the host neither applies nor sends anything more for it - as
    /// <see cref="BcrdtDocumentHost.RevokeAsync(string, Func{IBcrdtHostConnection, bool}, string?)"/> does. Without a
    /// host the relays hold no presence: a revoked client's entry leaves the others' participant lists once it times
    /// out, like that of any client whose connection dropped.
    /// </para>
    /// <para>
    /// Change what the relay's authorization hook answers <i>before</i> revoking: a client that was already past
    /// the hook when this ran joins with the grant the hook gave it.
    /// </para>
    /// </remarks>
    public ValueTask<int> RevokeAsync(string documentId, Func<IBcrdtHostConnection, bool> revoke, string? reason = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);
        ArgumentNullException.ThrowIfNull(revoke);

        return RevokeAsync([.. Of(documentId).Where(connection => revoke(connection))], reason ?? BcrdtDocumentHost.DefaultRevocationReason);
    }

    /// <summary>
    /// Takes every document from the connections that no longer may have it, whichever documents they have open - a
    /// user signed out everywhere, say.
    /// </summary>
    /// <param name="revoke">Which connections lose which documents, given the document identifier and the connection.</param>
    /// <param name="reason">What the clients are told. Defaults to <see cref="BcrdtDocumentHost.DefaultRevocationReason"/>.</param>
    /// <returns>How many connections were revoked - one per connection and document.</returns>
    /// <remarks>See <see cref="RevokeAsync(string, Func{IBcrdtHostConnection, bool}, string?)"/>.</remarks>
    public ValueTask<int> RevokeAsync(Func<string, IBcrdtHostConnection, bool> revoke, string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(revoke);

        return RevokeAsync([.. Live().Where(connection => revoke(connection.DocumentId, connection))], reason ?? BcrdtDocumentHost.DefaultRevocationReason);
    }

    /// <summary>
    /// Narrows read-write connections to a document to read-only, without disconnecting them.
    /// </summary>
    /// <param name="documentId">The document.</param>
    /// <param name="narrow">Which of the document's connections become read-only.</param>
    /// <returns>How many connections were narrowed; one that was read-only already does not count.</returns>
    /// <remarks>
    /// <para>
    /// Enforced where a grant always is, on the server: from the moment this is called the relay drops every
    /// <see cref="BcrdtSyncMessageKind.Update"/> and <see cref="BcrdtSyncMessageKind.SyncStep2"/> the connection
    /// sends, and so does a <see cref="BcrdtDocumentHost"/>, which reads <see cref="IBcrdtHostConnection.Access"/>
    /// on every message. A change the relay was already passing on goes where it was going - this waits for it.
    /// The client is told with an <see cref="BcrdtSyncMessageKind.Auth"/> grant, as on joining, and the built-in
    /// components stop offering edits; it goes on receiving every change and may still publish presence.
    /// </para>
    /// <para>
    /// Only ever narrows. Widening a grant is a matter of reconnecting: a client that was read-only holds nothing
    /// it was allowed to send, and the handshake of a new join is what would collect it. As with revoking, change
    /// the authorization hook first, or the next reconnection is granted read-write again.
    /// </para>
    /// </remarks>
    public ValueTask<int> MakeReadOnlyAsync(string documentId, Func<IBcrdtHostConnection, bool> narrow)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);
        ArgumentNullException.ThrowIfNull(narrow);

        return MakeReadOnlyAsync([.. Of(documentId).Where(connection => narrow(connection))]);
    }

    /// <summary>Narrows read-write connections to read-only, whichever documents they have open.</summary>
    /// <param name="narrow">Which connections become read-only on which documents, given the document identifier and the connection.</param>
    /// <returns>How many connections were narrowed - one per connection and document.</returns>
    /// <remarks>See <see cref="MakeReadOnlyAsync(string, Func{IBcrdtHostConnection, bool})"/>.</remarks>
    public ValueTask<int> MakeReadOnlyAsync(Func<string, IBcrdtHostConnection, bool> narrow)
    {
        ArgumentNullException.ThrowIfNull(narrow);

        return MakeReadOnlyAsync([.. Live().Where(connection => narrow(connection.DocumentId, connection))]);
    }

    /// <summary>Lists a connection that has joined a document. A relay calls this once the connection is admitted.</summary>
    internal void Add(BcrdtRelayConnection connection)
    {
        connection.ListIn(this);
        _connections.TryAdd(connection, 0);

        // Refused before it was listed, so its refusal found nothing to take off.
        if (connection.Refused) Remove(connection);
    }

    /// <summary>Takes a connection off the list: it left, disconnected or was refused.</summary>
    internal void Remove(BcrdtRelayConnection connection) => _connections.TryRemove(connection, out _);

    /// <summary>The connections not refused yet, as they stand now. Application predicates run over this, outside any lock.</summary>
    private IEnumerable<BcrdtRelayConnection> Live() => _connections.Keys.Where(connection => !connection.Refused);

    private IEnumerable<BcrdtRelayConnection> Of(string documentId) =>
        Live().Where(connection => string.Equals(connection.DocumentId, documentId, StringComparison.Ordinal));

    private static async ValueTask<int> RevokeAsync(List<BcrdtRelayConnection> chosen, string reason)
    {
        // Refused at the relay first: counted exactly once however many revocations race, and from here on the
        // relay passes nothing it sends to anybody.
        List<BcrdtRelayConnection> refused = [.. chosen.Where(connection => connection.TryRefuse(reason))];
        if (refused.Count == 0) return 0;

        // Then in the host, which withdraws their presence and drops whatever of theirs is waiting for the document
        // - one pass per document, however many of its connections go.
        foreach (IGrouping<(BcrdtDocumentHost Host, string DocumentId), BcrdtRelayConnection> hosted in refused
            .Where(connection => connection.Host is not null)
            .GroupBy(connection => (connection.Host!, connection.DocumentId)))
        {
            HashSet<IBcrdtHostConnection> members = new(hosted, ReferenceEqualityComparer.Instance);
            await hosted.Key.Host.RevokeAsync(hosted.Key.DocumentId, members.Contains, reason).ConfigureAwait(false);
        }

        foreach (BcrdtRelayConnection connection in refused) await connection.SettledAsync().ConfigureAwait(false);
        return refused.Count;
    }

    private static async ValueTask<int> MakeReadOnlyAsync(List<BcrdtRelayConnection> chosen)
    {
        List<BcrdtRelayConnection> narrowed = [.. chosen.Where(connection => connection.TryMakeReadOnly())];
        if (narrowed.Count == 0) return 0;

        byte[] grant = BcrdtSyncProtocol.EncodeAuth(BcrdtDocumentAccess.ReadOnly, null);
        foreach (BcrdtRelayConnection connection in narrowed) connection.Send(grant);

        foreach (BcrdtRelayConnection connection in narrowed) await connection.SettledAsync().ConfigureAwait(false);
        return narrowed.Count;
    }
}

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

    // Messages the relay is passing on for this connection right now, and whoever waits for them to be done.
    private int _relaying;
    private TaskCompletionSource? _settled;

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
    /// Starts passing on a message the client sent, or reports that it must be dropped: the connection was refused,
    /// or the message is a change and the connection may not write. Pair every <see langword="true"/> with
    /// <see cref="EndRelay"/>.
    /// </summary>
    /// <remarks>
    /// Counted before the grant is read, and a grant is changed before the count is read, so either this sees the
    /// change or <see cref="SettledAsync"/> waits for this message.
    /// </remarks>
    public bool TryBeginRelay(byte[] message)
    {
        Interlocked.Increment(ref _relaying);
        if (!Refused && (Access == BcrdtDocumentAccess.ReadWrite || !BcrdtSyncProtocol.IsWrite(message))) return true;

        EndRelay();
        return false;
    }

    /// <summary>Ends what <see cref="TryBeginRelay"/> started: the message has gone wherever it was going.</summary>
    public void EndRelay()
    {
        if (Interlocked.Decrement(ref _relaying) == 0) Interlocked.Exchange(ref _settled, null)?.TrySetResult();
    }

    /// <summary>Completes once no message the relay began passing on before this call is still on its way.</summary>
    public Task SettledAsync()
    {
        var waiter = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource settled = Interlocked.CompareExchange(ref _settled, waiter, null) ?? waiter;

        // Nothing in flight, or the last one ended before the waiter was in place to hear it.
        if (Volatile.Read(ref _relaying) == 0)
        {
            Interlocked.CompareExchange(ref _settled, null, settled);
            settled.TrySetResult();
        }

        return settled.Task;
    }

    /// <summary>Remembers the list this connection is on, so a refusal takes it off.</summary>
    public void ListIn(BcrdtRelayConnections registry) => Volatile.Write(ref _registry, registry);

    /// <summary>Takes the connection off the list it is on, if any: it left or disconnected.</summary>
    public void Unlist() => Volatile.Read(ref _registry)?.Remove(this);
}
