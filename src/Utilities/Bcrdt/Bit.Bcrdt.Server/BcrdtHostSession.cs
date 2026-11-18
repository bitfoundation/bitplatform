namespace Bit.Bcrdt;

/// <summary>One connection's membership of a hosted document.</summary>
/// <remarks>Created by <see cref="BcrdtDocumentHost.JoinAsync"/>; dispose it when the connection leaves.</remarks>
public sealed class BcrdtHostSession : IDisposable
{
    private readonly BcrdtDocumentHost _host;
    private readonly BcrdtDocumentHost.HostedDocument _document;
    private readonly IBcrdtHostConnection _connection;
    private int _disposed;

    internal BcrdtHostSession(BcrdtDocumentHost host, BcrdtDocumentHost.HostedDocument document, IBcrdtHostConnection connection)
    {
        _host = host;
        _document = document;
        _connection = connection;
    }

    /// <summary>The document this session belongs to.</summary>
    public string DocumentId => _document.Id;

    /// <summary>
    /// Handles a message the connection sent, and says whether the relay should pass it on to the
    /// document's other connections.
    /// </summary>
    /// <remarks>
    /// <see langword="false"/> for anything the other clients must not see: a malformed message, an
    /// <see cref="BcrdtSyncMessageKind.Auth"/> verdict, a change from a read-only connection, a change the
    /// validator rejected. Messages from one connection must be passed in the order they arrived.
    /// </remarks>
    public ValueTask<bool> ReceiveAsync(ReadOnlyMemory<byte> message)
    {
        if (Volatile.Read(ref _disposed) != 0) return ValueTask.FromResult(false);
        return _host.ReceiveAsync(_document, _connection, message);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _host.Leave(_document, _connection);
    }
}
