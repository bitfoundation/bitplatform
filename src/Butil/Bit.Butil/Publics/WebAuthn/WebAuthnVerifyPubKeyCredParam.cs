namespace Bit.Butil;

public class WebAuthnVerifyPubKeyCredParam
{
    public required int Alg { get; set; }
    public required string Type { get; set; }
}
