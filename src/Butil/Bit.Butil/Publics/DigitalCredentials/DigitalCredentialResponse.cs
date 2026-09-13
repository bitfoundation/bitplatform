using System.Text.Json;

namespace Bit.Butil;

/// <summary>
/// What the wallet returned - a presentation of the requested claims, in the protocol's own shape.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/DigitalCredential">DigitalCredential</see>
/// </summary>
public class DigitalCredentialResponse
{
    /// <summary>The protocol the wallet answered in, from the request that matched.</summary>
    public string Protocol { get; set; } = string.Empty;

    /// <summary>
    /// The protocol's response object as raw JSON - typically a signed verifiable presentation.
    /// Verify it on the server: its signature, its issuer, and the nonce you put in the request.
    /// Reading the claims out of it in the browser proves nothing about them.
    /// </summary>
    public JsonElement Data { get; set; }
}
