namespace Boilerplate.Shared.Features.Identity.Dtos;

/// <summary>
/// Data sealed to a <see cref="DevicePinLockDto.PublicKey"/> by AccountPinCipher.Seal, which only that lock's PIN
/// opens. A locked user session keeps its tokens in one (See <see cref="DeviceUserSessionDto.SealedTokens"/>).
/// </summary>
public partial class DeviceSealedDataDto
{
    /// <summary>The one-time ECDH P-256 public key (SPKI) whose shared secret with the lock's key encrypts <see cref="Data"/>.</summary>
    public byte[] PublicKey { get; set; } = [];

    /// <summary>The AES-GCM nonce <see cref="Data"/> is encrypted with.</summary>
    public byte[] Iv { get; set; } = [];

    /// <summary>The AES-GCM ciphertext, tag appended.</summary>
    public byte[] Data { get; set; } = [];
}
