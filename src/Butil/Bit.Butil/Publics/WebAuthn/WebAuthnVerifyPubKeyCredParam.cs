namespace Bit.Butil;

/// <summary>
/// One signature algorithm the relying party is willing to verify.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PublicKeyCredentialCreationOptions#pubkeycredparams">PublicKeyCredentialCreationOptions.pubKeyCredParams</see>
/// </summary>
public class WebAuthnVerifyPubKeyCredParam
{
    /// <summary>
    /// The COSE algorithm identifier - <c>-7</c> for ES256 and <c>-257</c> for RS256, the two every
    /// authenticator supports.
    /// </summary>
    public required int Alg { get; set; }

    /// <summary>Always <c>"public-key"</c>; it is the only credential type WebAuthn defines.</summary>
    public required string Type { get; set; }
}
