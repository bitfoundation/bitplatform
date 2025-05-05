namespace Bit.Butil;

public class WebAuthnVerifyOptions
{
    public required string Challenge { get; set; }
    public string? Attestation { get; set; }
    public WebAuthnVerifyRp? Rp { get; set; }
    public WebAuthnVerifyUser? User { get; set; }
    public WebAuthnVerifyAuthenticatorSelection? AuthenticatorSelection { get; set; }
    public WebAuthnVerifyPubKeyCredParam[]? PubKeyCredParams { get; set; }
    public WebAuthnVerifyAllowCredential[]? AllowCredentials { get; set; }
}
