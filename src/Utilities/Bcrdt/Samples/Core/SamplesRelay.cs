using System.Net;
using Bit.Bcrdt.SignalR;

namespace Bit.Bcrdt.Samples.Core;

/// <summary>
/// Connects every document to the <c>BcrdtHub</c> each web host maps at <c>/hub/bcrdt</c>, so every tab that opens a
/// sample page meets the others there, whichever render mode it runs in.
/// </summary>
/// <remarks>
/// <para>
/// The .NET SignalR client needs an absolute hub address, and a web host learns its own only once it runs, so the
/// address is resolved when a transport is created rather than at registration.
/// </para>
/// <para>
/// In the browser it is the page's own address. Under Server rendering the replica lives in the server process and
/// connects to the hub like any other client, so a circuit's replica and a WebAssembly tab's replica - which Auto
/// produces side by side - edit one document. There the address is one the server listens on
/// (<see cref="LocalHubAddress"/>), never the page's: a circuit's <c>NavigationManager.BaseUri</c> is whatever the
/// browser reported when the circuit started, and a server that connected wherever it pointed would send requests to
/// any host a client named. An address of its own also keeps working behind a TLS-terminating proxy or in a
/// container, where the public address is not one the server can reach.
/// </para>
/// </remarks>
/// <param name="hubAddress">Where the hub is, asked each time a document is opened.</param>
public sealed class SamplesRelay(Func<Uri> hubAddress) : IBcrdtTransportFactory
{
    /// <inheritdoc/>
    public IBcrdtTransport Create(string documentId) => new BcrdtSignalRTransport(hubAddress().ToString(), documentId);

    /// <summary>
    /// The hub as the server reaches it from its own process: at an address it listens on, plain HTTP first - its own
    /// traffic needs no TLS, and then nothing depends on whether it trusts the development certificate - and at
    /// <c>localhost</c> when it listens on every interface.
    /// </summary>
    /// <remarks>
    /// A server that listens on HTTPS alone connects there, so its certificate has to be valid for <c>localhost</c>,
    /// as the development certificate is.
    /// </remarks>
    /// <param name="listeningAddresses">The server's bindings, from its <c>IServerAddressesFeature</c>.</param>
    /// <exception cref="InvalidOperationException">The server is not listening on anything yet.</exception>
    public static Uri LocalHubAddress(IEnumerable<string> listeningAddresses)
    {
        ArgumentNullException.ThrowIfNull(listeningAddresses);

        string address = listeningAddresses
            .OrderBy(address => address.StartsWith("https:", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault() ?? throw new InvalidOperationException("The server is not listening on any address yet.");

        // A wildcard binding (http://+:80, http://*:80, http://[::]:80, http://0.0.0.0:80) names every interface,
        // not one to connect to.
        var uri = new UriBuilder(address.Replace("://+:", "://localhost:").Replace("://*:", "://localhost:"));
        if (IPAddress.TryParse(uri.Host.Trim('[', ']'), out IPAddress? ip) && (ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any)))
        {
            uri.Host = "localhost";
        }

        return new Uri(uri.Uri, "hub/bcrdt");
    }
}
