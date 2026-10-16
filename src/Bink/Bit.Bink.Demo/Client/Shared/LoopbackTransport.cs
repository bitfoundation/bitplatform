namespace Bit.Bink.Demo.Client.Shared;

/// <summary>
/// An in-memory transport that connects several sessions in the same browser tab, so the collaboration demo runs
/// without a server. A real application swaps this for SignalR or WebSockets; nothing else changes.
/// </summary>
public sealed class LoopbackHub
{
    private readonly List<LoopbackTransport> _clients = new();

    /// <summary>Extra delay applied to every message, used to show what latency does to the experience.</summary>
    public int LatencyMilliseconds { get; set; }

    /// <summary>Creates a transport endpoint attached to this hub.</summary>
    public LoopbackTransport CreateClient()
    {
        var client = new LoopbackTransport(this);
        _clients.Add(client);
        return client;
    }

    internal async Task BroadcastAsync(LoopbackTransport sender, BinkCollaborationMessage message)
    {
        if (LatencyMilliseconds > 0) await Task.Delay(LatencyMilliseconds).ConfigureAwait(false);
        foreach (LoopbackTransport client in _clients.ToArray())
        {
            if (ReferenceEquals(client, sender)) continue;
            await client.ReceiveAsync(message).ConfigureAwait(false);
        }
    }
}

/// <summary>One endpoint of a <see cref="LoopbackHub"/>.</summary>
/// <remarks>
/// While an endpoint is offline the hub keeps the operations sent to it, the way a relay server keeps a mailbox,
/// and delivers them when it reconnects. Without that, whatever the others drew in the meantime would never
/// arrive and the two documents would quietly drift apart. Presence is not kept: a cursor position from a
/// minute ago is noise, not news.
/// </remarks>
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

    /// <summary>Number of messages this endpoint has sent, shown in the demo.</summary>
    public int SentCount { get; private set; }

    /// <summary>Number of bytes this endpoint has sent, so the cost of an edit is visible.</summary>
    public long SentBytes { get; private set; }

    /// <summary>Simulates going offline and coming back, to show the queue and reconnect behaviour.</summary>
    public async Task SetConnectedAsync(bool connected)
    {
        if (_connected == connected) return;
        _connected = connected;
        if (connected)
        {
            // Catch up on what arrived while offline before sending our own queue, as a client reconnecting to
            // a server would.
            while (_inbox.TryDequeue(out BinkCollaborationMessage? message))
            {
                if (MessageReceived is not null) await MessageReceived(message).ConfigureAwait(false);
            }
        }
        ConnectionChanged?.Invoke(connected);
    }

    /// <summary>Number of messages waiting for this endpoint to come back online.</summary>
    public int InboxCount => _inbox.Count;

    /// <inheritdoc />
    public async Task SendAsync(BinkCollaborationMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!_connected) throw new InvalidOperationException("The transport is offline.");
        SentCount++;
        SentBytes += (message.Operation?.Length ?? 0) + (message.Snapshot?.Length ?? 0) + 64;
        await hub.BroadcastAsync(this, message).ConfigureAwait(false);
    }

    internal Task ReceiveAsync(BinkCollaborationMessage message)
    {
        if (!_connected)
        {
            if (message.Kind != BinkCollaborationMessageKind.Presence) _inbox.Enqueue(message);
            return Task.CompletedTask;
        }
        return MessageReceived is not null ? MessageReceived(message) : Task.CompletedTask;
    }
}
