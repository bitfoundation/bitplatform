namespace Bit.Butil;

/// <summary>
/// The relying party a credential is created for - the site itself. Its id is always the current
/// origin's domain and is filled in by the browser, so only the display name is stated here.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PublicKeyCredentialCreationOptions#rp">PublicKeyCredentialCreationOptions.rp</see>
/// </summary>
public class WebAuthnVerifyRp
{
    /// <summary>The site name the browser shows in its passkey prompt.</summary>
    public string? Name { get; set; }
}
