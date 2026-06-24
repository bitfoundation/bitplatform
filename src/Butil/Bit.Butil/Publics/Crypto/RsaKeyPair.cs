namespace Bit.Butil;

/// <summary>RSA key pair returned by <see cref="Crypto.GenerateRsaKeyPair"/>.</summary>
public class RsaKeyPair
{
    /// <summary>Public key in SubjectPublicKeyInfo (SPKI) DER format.</summary>
    public byte[] PublicKey { get; set; } = [];

    /// <summary>Private key in PKCS8 DER format.</summary>
    public byte[] PrivateKey { get; set; } = [];
}
