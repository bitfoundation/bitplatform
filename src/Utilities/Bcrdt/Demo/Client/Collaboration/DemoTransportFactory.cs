namespace Bit.Bcrdt.Demo.Client.Collaboration;

/// <summary>
/// Hands one replica its own transport onto a shared in-memory network.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="BcrdtCollaborationService"/> asks its <see cref="IBcrdtTransportFactory"/> for a connection
/// when it opens a document, so giving every replica its own factory is what makes every replica an
/// independently connectable peer. They all join the same room - the room name is the document id -
/// so messages one sends reach the others, exactly as they would over a WebSocket to a server.
/// </para>
/// <para>
/// The created transport is kept so the demo can reach past the tidy API and pull the plug with
/// <see cref="BcrdtInMemoryTransport.SimulateDrop"/>, which is not something an application would do but
/// is precisely what a demo of offline behaviour needs.
/// </para>
/// </remarks>
public sealed class DemoTransportFactory(BcrdtInMemoryNetwork network) : IBcrdtTransportFactory
{
    /// <summary>The transport this factory last created, if any.</summary>
    public BcrdtInMemoryTransport? Transport { get; private set; }

    /// <inheritdoc/>
    public IBcrdtTransport Create(string documentId) => Transport = network.CreateTransport(documentId);
}
