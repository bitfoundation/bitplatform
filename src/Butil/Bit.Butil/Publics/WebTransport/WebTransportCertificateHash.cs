namespace Bit.Butil;

/// <summary>
/// A hash of the exact server certificate to accept, which is how a WebTransport connection reaches
/// a server whose certificate no public CA signed - a development or peer-to-peer endpoint.
/// </summary>
/// <remarks>
/// The browser accepts this only for a short-lived certificate (14 days in Chromium) using an
/// ECDSA P-256 key, and only over HTTP/3. It is not a way around certificate validation for an
/// ordinary server.
/// </remarks>
public class WebTransportCertificateHash
{
    /// <summary>The hash algorithm. Only <c>"sha-256"</c> is defined.</summary>
    public string Algorithm { get; set; } = "sha-256";

    /// <summary>The certificate's hash, as raw bytes.</summary>
    public byte[] Value { get; set; } = [];
}
