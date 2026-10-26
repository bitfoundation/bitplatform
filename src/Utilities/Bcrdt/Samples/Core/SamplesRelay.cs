using Bit.Bcrdt.SignalR;
using Microsoft.AspNetCore.Components;

namespace Bit.Bcrdt.Samples.Core;

/// <summary>
/// Connects every document to the <c>BcrdtHub</c> of the server that served the page, which each web host maps at
/// <c>/hub/bcrdt</c> - so every tab that opens a sample page meets the others there, whichever render mode it runs in.
/// </summary>
/// <remarks>
/// <para>
/// The .NET SignalR client needs an absolute hub address, and a web host learns its own only once a page is requested,
/// so the address is resolved against the page's base address when a transport is created rather than at
/// registration. That is also why it is registered per scope: in Blazor Server every circuit has its own
/// <see cref="NavigationManager"/>.
/// </para>
/// <para>
/// Under Server rendering the replica lives in the server process and connects to the hub like any other client, so
/// a circuit's replica and a WebAssembly tab's replica - which Auto produces side by side - edit one document. A
/// server that cannot reach its own public address (behind a TLS-terminating proxy, say) resolves the hub against an
/// internal address instead.
/// </para>
/// </remarks>
public sealed class SamplesRelay(NavigationManager navigation) : IBcrdtTransportFactory
{
    /// <inheritdoc/>
    public IBcrdtTransport Create(string documentId) =>
        new BcrdtSignalRTransportFactory(new Uri(new Uri(navigation.BaseUri), "hub/bcrdt").ToString()).Create(documentId);
}
