namespace Bit.Bink.Samples.Core.Shared;

/// <summary>
/// An in-memory transport that connects several collaboration sessions on the same page, so the collaboration sample
/// runs without a server. A real application swaps it for SignalR or WebSockets; nothing else changes.
/// </summary>
/// <remarks>
/// A <see cref="BinkDocument"/> is not thread-safe and belongs to the renderer's dispatcher. Every message is handed
/// over through <c>dispatch</c> (the component's <c>InvokeAsync</c>), the way a real transport has to marshal what
/// arrives on a thread-pool thread. On WebAssembly that is a no-op; on Blazor Server and in MAUI it keeps every edit
/// on the thread that owns the document.
/// </remarks>
public sealed class LoopbackHub(Func<Func<Task>, Task> dispatch)
{
    private readonly List<LoopbackTransport> _clients = [];

    /// <summary>Creates a transport endpoint attached to this hub.</summary>
    public LoopbackTransport CreateClient()
    {
        var client = new LoopbackTransport(this);
        _clients.Add(client);
        return client;
    }

    internal async Task BroadcastAsync(LoopbackTransport sender, BinkCollaborationMessage message)
    {
        foreach (LoopbackTransport client in _clients.ToArray())
        {
            if (ReferenceEquals(client, sender)) continue;
            await dispatch(() => client.ReceiveAsync(message));
        }
    }
}

/// <summary>One endpoint of a <see cref="LoopbackHub"/>. While it is offline the hub keeps what was sent to it.</summary>
public sealed class LoopbackTransport(LoopbackHub hub) : IBinkCollaborationTransport
{
    private readonly Queue<BinkCollaborationMessage> _inbox = new();
    private bool _connected = true;

    /// <inheritdoc />
    public bool IsConnected => _connected;

    /// <inheritdoc />
    public event Func<BinkCollaborationMessage, Task>? MessageReceived;

    /// <inheritdoc />
    public event Action<bool>? ConnectionChanged;

    /// <summary>Number of messages this endpoint has sent.</summary>
    public int SentCount { get; private set; }

    /// <summary>Simulates going offline and coming back, to show the queue and reconnect behaviour.</summary>
    public async Task SetConnectedAsync(bool connected)
    {
        if (_connected == connected) return;
        _connected = connected;
        if (connected)
        {
            // Catch up on what arrived while offline before sending our own queue, as a reconnecting client would.
            while (_inbox.TryDequeue(out BinkCollaborationMessage? message))
            {
                if (MessageReceived is not null) await MessageReceived(message);
            }
        }
        ConnectionChanged?.Invoke(connected);
    }

    /// <inheritdoc />
    public async Task SendAsync(BinkCollaborationMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (_connected is false) throw new InvalidOperationException("The transport is offline.");
        SentCount++;
        await hub.BroadcastAsync(this, message);
    }

    internal Task ReceiveAsync(BinkCollaborationMessage message)
    {
        if (_connected is false)
        {
            // A cursor position from a minute ago is noise, not news: presence is not kept for later.
            if (message.Kind != BinkCollaborationMessageKind.Presence) _inbox.Enqueue(message);
            return Task.CompletedTask;
        }
        return MessageReceived?.Invoke(message) ?? Task.CompletedTask;
    }
}
