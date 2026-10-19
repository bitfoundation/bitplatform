using Microsoft.AspNetCore.SignalR.Client;

namespace Bit.Bcrdt.SignalR;

/// <summary>
/// Creates a <see cref="BcrdtSignalRTransport"/> per document against one hub.
/// </summary>
/// <remarks>
/// <para>
/// One connection per document, rather than one shared connection multiplexing all of them. It costs
/// more sockets, and buys a failure domain per document: a document whose hub call faults, whose
/// authorization is refused or whose reconnection is backing off does not stall the others, and the
/// state machine above stays a simple one-connection-one-document machine instead of having to track
/// which documents on a shared connection are currently synced.
/// </para>
/// <para>
/// Registered as the application's <see cref="IBcrdtTransportFactory"/>, this is the only place that
/// knows the collaboration stack is speaking SignalR at all.
/// </para>
/// </remarks>
public sealed class BcrdtSignalRTransportFactory : IBcrdtTransportFactory
{
    private readonly Func<string, IBcrdtTransport> _create;

    /// <summary>Creates transports that connect to a hub URL.</summary>
    /// <param name="hubUrl">
    /// Where the hub is mapped, as an absolute URL. The .NET SignalR client cannot resolve a relative
    /// one; in Blazor WebAssembly, resolve it against <c>HostEnvironment.BaseAddress</c>.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="hubUrl"/> is not an absolute URL.</exception>
    /// <param name="configure">Applied to each connection's builder, for access tokens or a binary hub protocol.</param>
    public BcrdtSignalRTransportFactory(string hubUrl, Action<IHubConnectionBuilder>? configure = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(hubUrl);
        BcrdtSignalRTransport.RequireAbsolute(hubUrl);
        _create = documentId => new BcrdtSignalRTransport(hubUrl, documentId, configure);
    }

    /// <summary>Creates transports over caller-built connections.</summary>
    /// <remarks>
    /// The document identifier is passed to the factory so that a caller can vary the connection per
    /// document - a per-document access token being the usual reason.
    /// </remarks>
    /// <param name="connectionFactory">Produces an unstarted connection for a document.</param>
    public BcrdtSignalRTransportFactory(Func<string, HubConnection> connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _create = documentId => new BcrdtSignalRTransport(() => connectionFactory(documentId), documentId);
    }

    /// <inheritdoc/>
    public IBcrdtTransport Create(string documentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);
        return _create(documentId);
    }
}
