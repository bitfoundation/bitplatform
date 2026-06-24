namespace Bit.Butil;

/// <summary>Elliptic curve key pair returned by <see cref="Crypto.GenerateEcdsaKeyPair"/>.</summary>
public class EcKeyPair
{
    /// <summary>Public key in SubjectPublicKeyInfo (SPKI) DER format.</summary>
    public byte[] PublicKey { get; set; } = [];

    /// <summary>Private key in PKCS8 DER format.</summary>
    public byte[] PrivateKey { get; set; } = [];

    /// <summary>The named curve (P-256, P-384, P-521).</summary>
    public string Curve { get; set; } = "P-256";
}
