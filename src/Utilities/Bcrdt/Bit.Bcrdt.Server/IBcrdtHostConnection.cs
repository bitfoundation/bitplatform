namespace Bit.Bcrdt;

/// <summary>One client connection as a <see cref="BcrdtDocumentHost"/> sees it.</summary>
/// <remarks>
/// Implemented by the relays - <see cref="BcrdtHub"/> and the WebSocket endpoint - and by anything else
/// that wants to put a hosted document behind a transport of its own.
/// </remarks>
public interface IBcrdtHostConnection
{
    /// <summary>A stable identifier for the connection, for logs. Never shown to other clients.</summary>
    string Id { get; }

    /// <summary>
    /// What the connection may do now: what it was granted when it joined, or read-only once its grant was
    /// narrowed (<see cref="BcrdtRelayConnections.MakeReadOnlyAsync(string, Func{IBcrdtHostConnection, bool})"/>).
    /// The host reads it on every message.
    /// </summary>
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
