namespace Bit.Bcrdt;

/// <summary>Where a transport currently stands.</summary>
public enum BcrdtTransportState
{
    /// <summary>Not connected, and not trying to be.</summary>
    Disconnected,
    /// <summary>Attempting to establish a connection.</summary>
    Connecting,
    /// <summary>Connected and able to carry messages.</summary>
    Connected,
    /// <summary>Failed in a way the transport itself cannot recover from.</summary>
    Faulted,
}

/// <summary>Reports a transport state change.</summary>
/// <param name="State">The new state.</param>
/// <param name="Error">What went wrong, when the change was caused by a failure.</param>
public readonly record struct BcrdtTransportStateChange(BcrdtTransportState State, Exception? Error);

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

/// <summary>
/// Thrown by <see cref="IBcrdtTransport.ConnectAsync"/> when the peer refused the connection.
/// </summary>
/// <remarks>
/// Distinct from an ordinary connection failure because retrying cannot help: the same credentials
/// will be refused again. The <c>BcrdtSyncProvider</c> treats it as final, stops reconnecting and raises
/// its <c>AuthorizationDenied</c> event.
/// </remarks>
public sealed class BcrdtTransportAuthorizationException : Exception
{
    /// <summary>Creates an exception with a default message.</summary>
    public BcrdtTransportAuthorizationException()
        : base("The peer refused the connection.")
    {
    }

    /// <summary>Creates an exception with a message.</summary>
    public BcrdtTransportAuthorizationException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception with a message and the failure that revealed the refusal.</summary>
    public BcrdtTransportAuthorizationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// How long to wait between reconnection attempts.
/// </summary>
/// <remarks>
/// <para>
/// Uses full jitter - a delay drawn uniformly from zero to the exponential ceiling - rather than a
/// fixed backoff. When a server restarts, every client it was serving tries to reconnect at once;
/// a deterministic schedule means they all retry in lockstep and knock it over again. Randomizing
/// the whole interval spreads them out, which is the difference between a server recovering and a
/// server being held down by its own clients.
/// </para>
/// <para>
/// A policy is safe to share between providers and threads: one instance commonly serves every
/// document an application opens, and those documents reconnect concurrently after an outage.
/// </para>
/// </remarks>
public sealed class BcrdtReconnectPolicy
{
    /// <summary>The starting delay, doubled on each successive failure.</summary>
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>The ceiling the exponential growth stops at.</summary>
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>How many times to try before giving up. Defaults to forever.</summary>
    public int MaxAttempts { get; init; } = int.MaxValue;

    /// <summary>Whether reconnection is attempted at all.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>A policy that never reconnects, for tests and one-shot syncs.</summary>
    public static BcrdtReconnectPolicy None { get; } = new() { Enabled = false, MaxAttempts = 0 };

    /// <summary>The delay to wait before attempt number <paramref name="attempt"/>.</summary>
    public TimeSpan NextDelay(int attempt)
    {
        if (attempt <= 0) return TimeSpan.Zero;

        // Cap the exponent before it is applied, so a long outage cannot overflow the multiplication.
        int exponent = Math.Min(attempt - 1, 20);
        double ceiling = Math.Min(BaseDelay.TotalMilliseconds * Math.Pow(2, exponent), MaxDelay.TotalMilliseconds);

        // Random.Shared rather than a per-policy instance: System.Random is not thread-safe, and one
        // policy is shared by every provider that reconnects after the same outage.
        return TimeSpan.FromMilliseconds(Random.Shared.NextDouble() * Math.Max(ceiling, 0));
    }
}
