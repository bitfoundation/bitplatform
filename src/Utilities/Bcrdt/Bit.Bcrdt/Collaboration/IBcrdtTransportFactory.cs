namespace Bit.Bcrdt;

/// <summary>
/// Creates a transport for a document.
/// </summary>
/// <remarks>
/// A factory rather than a single shared transport, because documents are usually separate
/// conversations: one connection, room or topic per document, so that a client subscribed to one
/// document does not receive another's traffic.
/// </remarks>
public interface IBcrdtTransportFactory
{
    /// <summary>Creates a transport for a document.</summary>
    IBcrdtTransport Create(string documentId);
}
