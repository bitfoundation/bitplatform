using System.Net.WebSockets;

namespace Bit.Bcrdt;

/// <summary>Settings for an endpoint mapped with <see cref="BcrdtWebSocketEndpoint.MapBitBcrdtWebSocket"/>.</summary>
public sealed class BcrdtWebSocketEndpointOptions
{
    /// <summary>
    /// Decides what each request may do with a document. Omitted means read-write for every request the
    /// <c>authorize</c> callback admits.
    /// </summary>
    /// <remarks>
    /// Consulted after the <c>authorize</c> callback, when both are given. A read-only connection receives
    /// the document and every change and may publish presence, but every change it sends is dropped. The
    /// client is told what it was granted as soon as its socket opens.
    /// </remarks>
    public BcrdtWebSocketAccessAuthorization? AuthorizeAccess { get; init; }

    /// <summary>
    /// A host that keeps a persisted, always-present replica of every document this endpoint carries,
    /// or <see langword="null"/> to only relay. See <see cref="BcrdtDocumentHost"/>.
    /// </summary>
    /// <remarks>
    /// A host is keyed by document identifier alone, so two endpoints given the same host - this one and a
    /// <see cref="BcrdtHub"/>, say - share its documents, and the host passes changes between their
    /// clients. Give each its own host when their documents must stay apart.
    /// </remarks>
    public BcrdtDocumentHost? DocumentHost { get; init; }

    /// <summary>
    /// Where this endpoint lists the connections it admits, so that a grant can be revoked or narrowed while the
    /// socket is open. Omitted means the <see cref="BcrdtRelayConnections"/> registered in the container, if any -
    /// <c>AddBitBcrdtRelayConnections()</c> or <c>AddBitBcrdtHub()</c> registers one.
    /// </summary>
    public BcrdtRelayConnections? Connections { get; init; }

    /// <summary>The default largest message a client may send: 16 MB.</summary>
    public const int DefaultMaxMessageSize = BcrdtWebSocketFraming.DefaultMaxMessageSize;

    /// <summary>
    /// The largest message a client may send, in bytes. A larger one closes that client's socket with
    /// <see cref="WebSocketCloseStatus.MessageTooBig"/> rather than being buffered without bound.
    /// </summary>
    public int MaxMessageSize { get; init; } = DefaultMaxMessageSize;

    /// <summary>
    /// How many messages may be queued for one client before it is disconnected.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Generous enough to absorb an ordinary latency spike, and finite because an unbounded queue in
    /// front of a client that never drains simply relocates the problem into the server's memory.
    /// </para>
    /// <para>
    /// A <see cref="DocumentHost"/>'s answer to a client's own handshake does not count: it can be the
    /// whole document in many parts, all produced at once, and would disconnect the client it was meant
    /// for - again on every reconnection. It is queued as the client drains, within a budget of this
    /// many messages of its own, and the client's next message is not handled until it is - though it is read, up
    /// to <see cref="MaxMessageSize"/> bytes ahead, so that the client's answers to the keep-alive are heard meanwhile.
    /// </para>
    /// </remarks>
    public int OutboundQueueCapacity { get; init; } = 256;

    /// <summary>
    /// How many bytes may be queued for one client before it is disconnected: 64 MB by default.
    /// </summary>
    /// <remarks>
    /// The message count alone does not bound memory - 256 messages of up to
    /// <see cref="MaxMessageSize"/> each could pin gigabytes behind one client that has stopped
    /// reading. Must be at least <see cref="MaxMessageSize"/>, or a single large message would always
    /// disconnect its recipients. A host's handshake answer is paced within a budget of this many bytes
    /// of its own, as for <see cref="OutboundQueueCapacity"/>.
    /// </remarks>
    public long OutboundQueueByteLimit { get; init; } = 64L * 1024 * 1024;

    /// <summary>
    /// How long, once the host begins shutting down, clients are given to answer the close frame
    /// before their sockets are aborted.
    /// </summary>
    public TimeSpan ShutdownGracePeriod { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The longest document identifier, in characters, a client may ask for: 256 by default, as for
    /// <see cref="BcrdtHubOptions.MaxDocumentIdLength"/>.
    /// </summary>
    /// <remarks>
    /// A longer one is refused like an unauthorized request, before any authorization callback runs. Raise it for
    /// an application whose identifiers are long by design; an identifier is held in memory by the relay and the
    /// host, and named in every log line about its document.
    /// </remarks>
    public int MaxDocumentIdLength { get; init; } = BcrdtHubOptions.DefaultMaxDocumentIdLength;

    /// <summary>How often the server pings a client to find out whether it is still there: 15 seconds by default.</summary>
    /// <remarks>
    /// Zero turns the keep-alive off. See <see cref="KeepAliveTimeout"/>.
    /// </remarks>
    public TimeSpan KeepAliveInterval { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// How long a ping may go unanswered before the server gives the client up for gone: 30 seconds by default.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A client whose network vanished without a word - a laptop put to sleep, a NAT entry expiring - leaves a
    /// socket that looks open until TCP itself gives up, often many minutes later. Until the server notices, the
    /// dead connection keeps its place in the room and, with a <see cref="DocumentHost"/>, the presence identity
    /// it published - which the same client, back on a new connection, may not be able to take over in the
    /// meantime. Every browser and <see cref="System.Net.WebSockets.ClientWebSocket"/> answers a ping on its own,
    /// so a live client, a hidden tab included, is never dropped by this.
    /// </para>
    /// <para>
    /// An answer is only heard while the socket is being read, so the endpoint keeps reading while a
    /// <see cref="DocumentHost"/> handles a message or paces out a large answer, holding up to
    /// <see cref="MaxMessageSize"/> bytes of what the client sends meanwhile. A client that sends more than that
    /// before the host is done with it is no longer read until it is, and may be given up for gone.
    /// </para>
    /// <para>
    /// <see cref="Timeout.InfiniteTimeSpan"/> sends pings without waiting for their answers, as the server did
    /// before this setting existed.
    /// </para>
    /// </remarks>
    public TimeSpan KeepAliveTimeout { get; init; } = TimeSpan.FromSeconds(30);
}
