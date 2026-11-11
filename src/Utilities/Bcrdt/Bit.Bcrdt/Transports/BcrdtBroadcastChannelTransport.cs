using Microsoft.JSInterop;

namespace Bit.Bcrdt;

/// <summary>
/// Carries the synchronization protocol between the tabs of one browser, over a <c>BroadcastChannel</c>.
/// </summary>
/// <remarks>
/// <para>
/// Every tab that has a document open is a replica of its own, and on its own a tab hears what another tab of the
/// same browser did only through the server - not at all while the browser is offline. A <c>BroadcastChannel</c>
/// reaches every other tab of the same origin directly, online or not, so the tabs of one browser can keep each
/// other up to date while the network is down and see each other's edits without the round trip. Like the
/// in-memory transport it is a room: every message one tab posts reaches every other tab on the same channel, and
/// never the tab that posted it.
/// </para>
/// <para>
/// There is no connection to lose: the channel is open from <see cref="ConnectAsync"/> until
/// <see cref="DisconnectAsync"/>. A tab alone on its channel is answered by nobody, and its provider counts as synced
/// once its handshake times out, as the first replica in an empty room does.
/// </para>
/// <para>
/// The channel is reached through JavaScript interop, the module the components already use, so the transport is
/// only connected once interactive. It works over any <see cref="IJSRuntime"/>, but sharing a channel only makes
/// sense where the replica runs in the browser: <see cref="BcrdtCollaborationBuilder.UseCrossTabSync"/>, the usual
/// way in, is for WebAssembly and does nothing elsewhere. Messages cross the interop boundary as byte arrays - a
/// <c>Uint8Array</c> on the JavaScript side - which the channel copies to every other tab.
/// </para>
/// </remarks>
public sealed class BcrdtBroadcastChannelTransport : IBcrdtTransport
{
    /// <summary>What a channel's name begins with when no other prefix is given.</summary>
    public const string DefaultChannelPrefix = "bit-bcrdt";

    private readonly IJSRuntime _js;
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private readonly object _stateGate = new();

    private IJSObjectReference? _module;
    private IJSObjectReference? _channel;
    private DotNetObjectReference<Receiver>? _receiver;
    private int _state = (int)BcrdtTransportState.Disconnected;
    private volatile bool _disposed;

    /// <summary>Creates a transport over a channel of the browser this application runs in.</summary>
    /// <param name="js">The JavaScript runtime of the browser.</param>
    /// <param name="channelName">
    /// The channel to join. Every tab of the same origin that joins a channel of this name receives what this one
    /// posts, so the name says which document it carries - and, for applications sharing an origin that must not
    /// see each other's documents, whose. <see cref="BcrdtBroadcastChannelTransportFactory"/> names one per document.
    /// </param>
    public BcrdtBroadcastChannelTransport(IJSRuntime js, string channelName)
    {
        ArgumentNullException.ThrowIfNull(js);
        ArgumentException.ThrowIfNullOrEmpty(channelName);

        _js = js;
        ChannelName = channelName;
    }

    /// <summary>The channel this transport joins.</summary>
    public string ChannelName { get; }

    /// <inheritdoc/>
    public BcrdtTransportState State => (BcrdtTransportState)Volatile.Read(ref _state);

    /// <inheritdoc/>
    public string Description => $"broadcast-channel:{ChannelName}";

    /// <inheritdoc/>
    public event Action<ReadOnlyMemory<byte>>? MessageReceived;

    /// <inheritdoc/>
    public event Action<BcrdtTransportStateChange>? StateChanged;

    /// <summary>Joins the channel.</summary>
    /// <exception cref="ObjectDisposedException">The transport has been disposed.</exception>
    /// <exception cref="JSException">
    /// The browser could not open the channel - one without <c>BroadcastChannel</c>, say. The provider reports it and
    /// tries again on its reconnection schedule, which costs nothing but the attempt.
    /// </exception>
    public async ValueTask ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (State == BcrdtTransportState.Connected) return;

            _module ??= await _js.InvokeAsync<IJSObjectReference>("import", cancellationToken, "./_content/Bit.Bcrdt/bit-bcrdt.js").ConfigureAwait(false);

            // The channel reports to a receiver of its own rather than to this transport, so a message that is still
            // on its way when the channel closes reaches nothing - and a channel opened again gets a fresh one.
            var receiver = DotNetObjectReference.Create(new Receiver(this));
            try
            {
                _channel = await _module.InvokeAsync<IJSObjectReference>("openChannel", cancellationToken, ChannelName, receiver).ConfigureAwait(false);
            }
            catch
            {
                receiver.Dispose();
                throw;
            }

            _receiver = receiver;

            // Announced only once the channel is listening, so the answers to the handshake this announcement starts
            // cannot arrive before anything hears them. A failure above is reported by throwing alone.
            SetState(BcrdtTransportState.Connected);
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    /// <inheritdoc/>
    /// <remarks>Dropped, as the contract asks, while the channel is closed - or the browser is gone.</remarks>
    public async ValueTask SendAsync(ReadOnlyMemory<byte> message, CancellationToken cancellationToken = default)
    {
        IJSObjectReference? channel = Volatile.Read(ref _channel);
        if (State != BcrdtTransportState.Connected || channel is null) return;

        try
        {
            await channel.InvokeVoidAsync("post", cancellationToken, message.ToArray()).ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
            // The browser went away with the channel; there is nobody left to post to.
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await CloseAsync().ConfigureAwait(false);
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    /// <summary>Leaves the channel, if this transport is on it, and announces it.</summary>
    private async ValueTask CloseAsync()
    {
        IJSObjectReference? channel = _channel;
        DotNetObjectReference<Receiver>? receiver = _receiver;
        _channel = null;
        _receiver = null;
        if (channel is null) return;

        // Disconnected before the channel closes, so nothing that arrives meanwhile is passed on.
        SetState(BcrdtTransportState.Disconnected);
        receiver?.Dispose();

        try
        {
            await channel.InvokeVoidAsync("close").ConfigureAwait(false);
            await channel.DisposeAsync().ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
            // The browser is gone, and the channel with it.
        }
    }

    private void Deliver(byte[] message)
    {
        if (State != BcrdtTransportState.Connected) return;
        MessageReceived?.Invoke(message);
    }

    private void SetState(BcrdtTransportState state)
    {
        lock (_stateGate)
        {
            if (State == state) return;
            Volatile.Write(ref _state, (int)state);
        }

        StateChanged?.Invoke(new BcrdtTransportStateChange(state, null));
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await _lifecycle.WaitAsync().ConfigureAwait(false);
        try
        {
            await CloseAsync().ConfigureAwait(false);

            IJSObjectReference? module = _module;
            _module = null;
            if (module is not null)
            {
                try
                {
                    await module.DisposeAsync().ConfigureAwait(false);
                }
                catch (JSDisconnectedException)
                {
                    // The browser is gone, and the module with it.
                }
            }
        }
        finally
        {
            _lifecycle.Release();
        }

        MessageReceived = null;
        StateChanged = null;
    }

    /// <summary>What the channel calls with each message another tab posted.</summary>
    internal sealed class Receiver(BcrdtBroadcastChannelTransport transport)
    {
        /// <summary>A message from another tab: the bytes it posted, copied by the channel and again by interop.</summary>
        [JSInvokable]
        public void OnChannelMessage(byte[] message) => transport.Deliver(message);
    }
}

/// <summary>
/// Creates a <see cref="BcrdtBroadcastChannelTransport"/> per document, on a channel named after it.
/// </summary>
/// <remarks>
/// The channel of a document is <c>{prefix}/{documentId}</c>: one per document, so a tab hears only about the
/// documents it has open, and prefixed, so applications on one origin can keep theirs apart the way they choose
/// different IndexedDB databases.
/// </remarks>
public sealed class BcrdtBroadcastChannelTransportFactory : IBcrdtTransportFactory
{
    private readonly IJSRuntime _js;
    private readonly string _prefix;

    /// <summary>Creates a factory over the browser this application runs in.</summary>
    /// <param name="js">The JavaScript runtime of the browser.</param>
    /// <param name="channelPrefix">What every channel's name begins with.</param>
    public BcrdtBroadcastChannelTransportFactory(IJSRuntime js, string channelPrefix = BcrdtBroadcastChannelTransport.DefaultChannelPrefix)
    {
        ArgumentNullException.ThrowIfNull(js);
        ArgumentException.ThrowIfNullOrEmpty(channelPrefix);

        _js = js;
        _prefix = channelPrefix;
    }

    /// <inheritdoc/>
    public IBcrdtTransport Create(string documentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);
        return new BcrdtBroadcastChannelTransport(_js, $"{_prefix}/{documentId}");
    }
}
