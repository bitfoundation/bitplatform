namespace Bit.Butil;

/// <summary>
/// The options one WebAuthn call is made with. The same object serves both directions: creating a
/// passkey reads <see cref="Rp"/>, <see cref="User"/>, <see cref="AuthenticatorSelection"/> and
/// <see cref="PubKeyCredParams"/>, while asserting an existing one reads
/// <see cref="AllowCredentials"/>. <see cref="Challenge"/> is required either way.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PublicKeyCredentialCreationOptions">PublicKeyCredentialCreationOptions</see>
/// </summary>
public class WebAuthnVerifyOptions
{
    /// <summary>
    /// The server-generated random challenge, base64url-encoded. It is what makes an assertion
    /// unrepeatable, so it has to come from the server and be verified there.
    /// </summary>
    public required string Challenge { get; set; }

    /// <summary>
    /// How much attestation the relying party wants about the authenticator itself -
    /// <c>"none"</c> (the default in practice), <c>"indirect"</c>, <c>"direct"</c> or
    /// <c>"enterprise"</c>.
    /// </summary>
    public string? Attestation { get; set; }

    /// <summary>The relying party - the site the credential belongs to. Creation only.</summary>
    public WebAuthnVerifyRp? Rp { get; set; }

    /// <summary>The account the credential is being created for. Creation only.</summary>
    public WebAuthnVerifyUser? User { get; set; }

    /// <summary>Which kind of authenticator is acceptable. Creation only.</summary>
    public WebAuthnVerifyAuthenticatorSelection? AuthenticatorSelection { get; set; }

    /// <summary>
    /// The signature algorithms the relying party accepts, most preferred first. Creation only.
    /// </summary>
    public WebAuthnVerifyPubKeyCredParam[]? PubKeyCredParams { get; set; }

    /// <summary>
    /// The credentials this assertion may be satisfied by. Assertion only; leaving it empty asks
    /// the browser to offer whatever discoverable credential it holds for the origin.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PublicKeyCredentialRequestOptions#allowcredentials">PublicKeyCredentialRequestOptions.allowCredentials</see>
    /// </summary>
    public WebAuthnVerifyAllowCredential[]? AllowCredentials { get; set; }
}
