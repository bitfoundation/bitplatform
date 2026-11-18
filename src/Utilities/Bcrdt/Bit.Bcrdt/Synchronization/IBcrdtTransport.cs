namespace Bit.Bcrdt;

/// <summary>
/// A bidirectional channel that carries protocol messages between peers.
/// </summary>
/// <remarks>
/// <para>
/// This is the seam that keeps the collaboration engine independent of any particular networking
/// technology. SignalR, raw WebSockets, WebRTC, a message broker, a <c>BroadcastChannel</c> between
/// browser tabs, or an in-process loopback for tests are all just implementations of this interface.
/// </para>
/// <para>
/// The contract asks for very little, deliberately. A transport may lose messages, duplicate them,
/// reorder them or deliver them late; the synchronization layer above is built to tolerate all
/// four. What it must do is deliver messages whole - a message that arrives must be exactly the
/// bytes that were sent - and report honestly when it is not connected.
/// </para>
/// <para>
/// Four further rules keep the state machine above simple and correct:
/// </para>
/// <list type="bullet">
/// <item><description>
/// A <see cref="ConnectAsync"/> that fails reports the failure <b>only</b> by throwing; it must not
/// also raise <see cref="BcrdtTransportState.Disconnected"/> or <see cref="BcrdtTransportState.Faulted"/> for
/// the same attempt. A connection that was announced as <see cref="BcrdtTransportState.Connected"/> and is
/// then lost raises exactly one terminal transition.
/// </description></item>
/// <item><description>
/// A peer that refuses the connection is reported by throwing
/// <see cref="BcrdtTransportAuthorizationException"/>, so the layer above can stop retrying rather than
/// back off forever against an answer that will not change.
/// </description></item>
/// <item><description>
/// The memory handed to <see cref="MessageReceived"/> must stay valid and unmodified after the handler
/// returns: receivers may queue it for processing on another thread.
/// </description></item>
/// <item><description>
/// Events may be raised on any thread. The <c>BcrdtSyncProvider</c> marshals them onto its own context
/// before touching the document.
/// </description></item>
/// </list>
/// </remarks>
public interface IBcrdtTransport : IAsyncDisposable
{
    /// <summary>Where the transport currently stands.</summary>
    BcrdtTransportState State { get; }

    /// <summary>A description of the peer, for logging and diagnostics.</summary>
    string Description { get; }

    /// <summary>Raised for each message received from the peer.</summary>
    event Action<ReadOnlyMemory<byte>>? MessageReceived;

    /// <summary>Raised whenever <see cref="State"/> changes.</summary>
    event Action<BcrdtTransportStateChange>? StateChanged;

    /// <summary>Establishes the connection.</summary>
    /// <exception cref="BcrdtTransportAuthorizationException">The peer refused the connection.</exception>
    ValueTask ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message.
    /// </summary>
    /// <remarks>
    /// Implementations should drop the message rather than throw when not connected: the layer above
    /// repairs the gap on the next handshake, and treating a disconnection as an exception would
    /// turn an ordinary network event into an error path on every keystroke.
    /// </remarks>
    ValueTask SendAsync(ReadOnlyMemory<byte> message, CancellationToken cancellationToken = default);

    /// <summary>Closes the connection without disposing the transport.</summary>
    ValueTask DisconnectAsync(CancellationToken cancellationToken = default);
}
