using Bit.Bsqlite.Data;

namespace Bit.Bsqlite.Interop;

/// <summary>
/// An opened database: the connection the application uses, the transport that owns the worker,
/// and the resolution that explains why this backend was chosen.
/// </summary>
/// <remarks>
/// The session owns the transport. The connection's <c>Close</c> is bookkeeping only - see
/// <see cref="BsqliteConnection.Close"/> - so disposing the session is what actually tears
/// the worker down.
/// </remarks>
public sealed class BsqliteSession : IAsyncDisposable
{
    internal BsqliteSession(
        BsqliteConnection connection,
        IBsqliteTransport transport,
        BsqliteStorageResolution resolution)
    {
        Connection = connection;
        Transport = transport;
        Resolution = resolution;
    }

    /// <summary>The ADO.NET connection. Already open.</summary>
    public BsqliteConnection Connection { get; }

    /// <summary>The transport serving <see cref="Connection"/>. Owned by this session.</summary>
    public IBsqliteTransport Transport { get; }

    /// <summary>Why this backend was chosen, including every candidate that was not.</summary>
    public BsqliteStorageResolution Resolution { get; }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync().ConfigureAwait(false);
        await Transport.DisposeAsync().ConfigureAwait(false);
    }
}
