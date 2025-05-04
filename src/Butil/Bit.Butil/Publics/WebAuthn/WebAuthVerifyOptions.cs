namespace Bit.Butil;

public class WebAuthVerifyOptions
{
    public required string Challenge { get; set; }
    public WebAuthVerifyRp? Rp { get; set; }
    public WebAuthVerifyUser? User { get; set; }
    public WebAuthVerifyAuthenticatorSelection? AuthenticatorSelection { get; set; }
    public WebAuthVerifyPubKeyCredParam[]? PubKeyCredParams { get; set; }
}
