namespace Bit.Bcrdt;

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
    /// Roughly how many bytes of client changes a hosted document holds while they wait for something
    /// they build on, before it discards them and asks the client that sent the last one for the rest.
    /// See <see cref="BcrdtDocumentOptions.MaxPendingSize"/>.
    /// </summary>
    public long MaxPendingSize { get; init; } = BcrdtDocumentOptions.DefaultMaxPendingSize;

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
    /// <para>
    /// An identity whose connection withdrew its entry is not taken over at all, but by the same signed-in user:
    /// a tab merely hidden withdraws its entry and stays connected, quiet until it is shown again, and anybody in
    /// the room - who knows its client identifier from the presence traffic - could otherwise claim it meanwhile
    /// and keep its owner out. The owner's connection releasing it, by leaving, frees it for anyone.
    /// </para>
    /// <para>
    /// A connection that withdrew its entry and then died without a goodbye - a hidden tab on a laptop put to
    /// sleep - holds its identity until its relay notices it is gone: within SignalR's
    /// <c>ClientTimeoutInterval</c> (30 seconds by default) on the hub, and within
    /// <see cref="BcrdtWebSocketEndpointOptions.KeepAliveInterval"/> plus
    /// <see cref="BcrdtWebSocketEndpointOptions.KeepAliveTimeout"/> (45 seconds by default) on the WebSocket
    /// endpoint. An anonymous client back on a new connection is shown again once that has happened.
    /// </para>
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
