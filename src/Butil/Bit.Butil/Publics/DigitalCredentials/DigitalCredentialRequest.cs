namespace Bit.Butil;

/// <summary>
/// One request in a digital-credential exchange: the protocol to speak, and the request object that
/// protocol defines.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Digital_Credentials_API">Digital Credentials API</see>
/// </summary>
public class DigitalCredentialRequest
{
    /// <summary>
    /// The exchange protocol, e.g. <c>"openid4vp"</c> for OpenID for Verifiable Presentations.
    /// <see cref="DigitalCredentials.IsProtocolSupported"/> says whether the browser will speak it.
    /// </summary>
    public required string Protocol { get; set; }

    /// <summary>
    /// The protocol's own request object, serialized as it stands - for OpenID4VP, the presentation
    /// definition naming the claims being asked for. Build it on the server: it is signed, and what
    /// it asks for is what the wallet shows the user. See the note on
    /// <see cref="PaymentMethod.Data"/> about trimming.
    /// </summary>
    public required object Data { get; set; }
}
