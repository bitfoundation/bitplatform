namespace Bit.Butil;

/// <summary>
/// The account a passkey is created for. The browser stores these three values with the credential
/// and shows them when the user later picks between passkeys.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PublicKeyCredentialCreationOptions#user">PublicKeyCredentialCreationOptions.user</see>
/// </summary>
public class WebAuthnVerifyUser
{
    /// <summary>
    /// An opaque, base64url-encoded account handle - at most 64 bytes decoded. It must not be an
    /// email address or anything else that identifies the person, since it is stored on the
    /// authenticator.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>The account name, usually the sign-in identifier the user recognises.</summary>
    public string? Name { get; set; }

    /// <summary>A friendlier name for the same account, for the browser's own picker.</summary>
    public string? DisplayName { get; set; }
}
