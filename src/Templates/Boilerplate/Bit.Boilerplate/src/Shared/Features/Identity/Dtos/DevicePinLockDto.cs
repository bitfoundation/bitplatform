namespace Boilerplate.Shared.Features.Identity.Dtos;

/// <summary>
/// The PIN of one account on this device, made by AccountPinCipher.CreateLock. The PIN itself is not stored: only it
/// opens <see cref="PrivateKey"/>, while <see cref="PublicKey"/> stays readable so data can be sealed without it.
/// </summary>
public partial class DevicePinLockDto
{
    /// <summary>The PBKDF2 salt the key that encrypts <see cref="PrivateKey"/> is derived from the PIN with.</summary>
    public byte[] Salt { get; set; } = [];

    /// <summary>The AES-GCM nonce <see cref="PrivateKey"/> is encrypted with.</summary>
    public byte[] Iv { get; set; } = [];

    /// <summary>The ECDH P-256 private key (PKCS#8), AES-GCM encrypted under the PIN's key, tag appended.</summary>
    public byte[] PrivateKey { get; set; } = [];

    /// <summary>The ECDH P-256 public key (SPKI) that <see cref="DeviceSealedDataDto"/> is sealed to.</summary>
    public byte[] PublicKey { get; set; } = [];
}
