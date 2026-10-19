using Bit.Bcrdt.SignalR;
using Microsoft.AspNetCore.Components;

namespace Bit.Bcrdt.Demo.Client.Collaboration;

/// <summary>The two relays the demo server hosts for the live page.</summary>
public enum LiveRelayKind
{
    /// <summary>The <c>BcrdtHub</c> SignalR hub at <c>/hub/bcrdt</c>, through <c>Bit.Bcrdt.SignalR</c>.</summary>
    SignalR,

    /// <summary>The raw WebSocket endpoint at <c>/ws/bcrdt/{documentId}</c>, through the built-in WebSocket transport.</summary>
    WebSocket,
}

/// <summary>
/// The transport factory of the application's own <see cref="IBcrdtCollaborationService"/>: the live page's
/// browser tabs reach each other through the demo server, over whichever relay <see cref="Kind"/> names.
/// </summary>
/// <remarks>
/// Every other page builds its replicas by hand over the in-memory transport, so they can sit side by side in one
/// tab. The live page is the only one that goes through <c>AddBitBcrdtServices</c> and
/// <see cref="BcrdtCollaborationScope"/> - exactly the way an application would - and this is the one piece it
/// configures. The addresses are resolved when a transport is created rather than at registration, because only
/// then is the page's base address known.
/// </remarks>
public sealed class LiveRelay(NavigationManager navigation) : IBcrdtTransportFactory
{
    /// <summary>The relay transports created from now on connect through.</summary>
    public LiveRelayKind Kind { get; set; } = LiveRelayKind.SignalR;

    /// <inheritdoc/>
    public IBcrdtTransport Create(string documentId)
    {
        var baseUri = new Uri(navigation.BaseUri);

        return Kind switch
        {
            // An http(s) address is rewritten to ws(s) by the factory.
            LiveRelayKind.WebSocket => new BcrdtWebSocketTransportFactory(new Uri(baseUri, "ws/bcrdt/" + BcrdtWebSocketTransportFactory.DocumentIdPlaceholder).OriginalString).Create(documentId),
            // The .NET SignalR client needs an absolute hub address.
            _ => new BcrdtSignalRTransportFactory(new Uri(baseUri, "hub/bcrdt").ToString()).Create(documentId),
        };
    }
}
