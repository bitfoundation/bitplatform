namespace Bit.Butil;

/// <summary>
/// One credential an assertion is allowed to be satisfied by.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PublicKeyCredentialRequestOptions#allowcredentials">PublicKeyCredentialRequestOptions.allowCredentials</see>
/// </summary>
public class WebAuthnVerifyAllowCredential
{
    /// <summary>The credential id, base64url-encoded, as it was returned when the passkey was created.</summary>
    public required string Id { get; set; }

    /// <summary>Always <c>"public-key"</c>; it is the only credential type WebAuthn defines.</summary>
    public required string Type { get; set; }
}
