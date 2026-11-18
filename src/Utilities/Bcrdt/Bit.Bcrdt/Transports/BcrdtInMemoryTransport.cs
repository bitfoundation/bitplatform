namespace Bit.Bcrdt;

/// <summary>A transport that carries messages through a <see cref="BcrdtInMemoryNetwork"/>.</summary>
public sealed class BcrdtInMemoryTransport(BcrdtInMemoryNetwork network, string room) : IBcrdtTransport
{
    private readonly object _stateGate = new();
    private int _state = (int)BcrdtTransportState.Disconnected;
    private volatile bool _disposed;

    /// <inheritdoc/>
    public BcrdtTransportState State => (BcrdtTransportState)Volatile.Read(ref _state);

    /// <inheritdoc/>
    public string Description => $"in-memory:{room}";

    /// <summary>The room this transport joins.</summary>
    public string Room => room;

    /// <inheritdoc/>
    public event Action<ReadOnlyMemory<byte>>? MessageReceived;

    /// <inheritdoc/>
    public event Action<BcrdtTransportStateChange>? StateChanged;

    /// <inheritdoc/>
    public ValueTask ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        if (State == BcrdtTransportState.Connected) return ValueTask.CompletedTask;

        SetState(BcrdtTransportState.Connecting, null);
        network.Join(room, this);
        SetState(BcrdtTransportState.Connected, null);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask SendAsync(ReadOnlyMemory<byte> message, CancellationToken cancellationToken = default)
    {
        if (State != BcrdtTransportState.Connected) return ValueTask.CompletedTask;
        network.Broadcast(room, this, message);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (State == BcrdtTransportState.Disconnected) return ValueTask.CompletedTask;

        network.Leave(room, this);
        SetState(BcrdtTransportState.Disconnected, null);
        return ValueTask.CompletedTask;
    }

    /// <summary>Simulates the connection dropping, so reconnection behaviour can be exercised.</summary>
    public void SimulateDrop(Exception? error = null)
    {
        network.Leave(room, this);
        SetState(BcrdtTransportState.Faulted, error ?? new IOException("Simulated connection drop."));
    }

    internal void Deliver(ReadOnlyMemory<byte> message)
    {
        if (State != BcrdtTransportState.Connected) return;
        MessageReceived?.Invoke(message);
    }

    private void SetState(BcrdtTransportState state, Exception? error)
    {
        // The comparison and the assignment happen together so a transition is announced exactly
        // once; the handlers run outside the lock because they may re-enter this transport.
        lock (_stateGate)
        {
            if (State == state) return;
            Volatile.Write(ref _state, (int)state);
        }

        StateChanged?.Invoke(new BcrdtTransportStateChange(state, error));
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await DisconnectAsync().ConfigureAwait(false);
        MessageReceived = null;
        StateChanged = null;
    }
}
