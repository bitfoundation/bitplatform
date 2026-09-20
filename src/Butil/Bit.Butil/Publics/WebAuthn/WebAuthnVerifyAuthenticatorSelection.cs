namespace Bit.Butil;

/// <summary>
/// Which authenticators the relying party will accept for a new credential.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PublicKeyCredentialCreationOptions#authenticatorselection">PublicKeyCredentialCreationOptions.authenticatorSelection</see>
/// </summary>
public class WebAuthnVerifyAuthenticatorSelection
{
    /// <summary>
    /// <c>"platform"</c> for the device's own authenticator (Touch ID, Windows Hello),
    /// <c>"cross-platform"</c> for a roaming one such as a security key or a phone.
    /// </summary>
    public required string AuthenticatorAttachment { get; set; }
}
