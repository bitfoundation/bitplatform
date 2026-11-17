using System.Text;
using System.Security.Cryptography;

namespace Boilerplate.Client.Core.Infrastructure.Services;

/// <summary>
/// Keeps a locked account's tokens on this device encrypted with a key that only its PIN opens. Nothing here reaches
/// the server.
/// <para>How it works:</para>
/// <list type="number">
/// <item><see cref="CreateLock"/> makes an ECDH P-256 key pair and encrypts its private key with AES-GCM under a key
/// derived from the PIN (PBKDF2, SHA-256, 600,000 rounds). The public key stays readable (See <see cref="DevicePinLockDto"/>).</item>
/// <item><see cref="Seal"/> encrypts to that public key without the PIN: a one-time key pair, ECDH with the lock's public
/// key, and SHA-256 of the shared secret as the AES-GCM key. That is how a locked account's refreshed tokens are stored
/// while another account is active (See <see cref="DeviceSealedDataDto"/>).</item>
/// <item><see cref="Open"/> derives the PIN's key, decrypts the private key and repeats the ECDH. A wrong PIN fails the
/// GCM tag and returns null.</item>
/// </list>
/// <para>
/// The rounds only slow down guessing a copy of the storage; <see cref="MaxFailedAttempts"/> limits the app's own prompt.
/// The browser runs these primitives with Butil's Crypto (See WebAccountPinCipher), every other platform with .NET's.
/// </para>
/// </summary>
public partial class AccountPinCipher
{
    public const int PinLength = 8;

    /// <summary>Wrong PINs in a row after which AuthManager removes the session from the device.</summary>
    public const int MaxFailedAttempts = 10;

    private const int PinIterations = 600_000;

    public virtual ValueTask<bool> IsSupported() => ValueTask.FromResult(AesGcm.IsSupported);

    public async Task<DevicePinLockDto> CreateLock(string pin)
    {
        var (publicKey, privateKey) = await GenerateKeyPair();
        var salt = RandomNumberGenerator.GetBytes(16);
        var iv = RandomNumberGenerator.GetBytes(12);

        return new()
        {
            Salt = salt,
            Iv = iv,
            PublicKey = publicKey,
            PrivateKey = await Encrypt(await DerivePinKey(Encoding.UTF8.GetBytes(pin), salt, PinIterations), iv, privateKey)
        };
    }

    public async Task<DeviceSealedDataDto> Seal(DevicePinLockDto pinLock, byte[] data)
    {
        var (publicKey, privateKey) = await GenerateKeyPair();
        var iv = RandomNumberGenerator.GetBytes(12);

        return new()
        {
            PublicKey = publicKey,
            Iv = iv,
            Data = await Encrypt(await DeriveSharedKey(privateKey, pinLock.PublicKey), iv, data)
        };
    }

    public async Task<byte[]?> Open(DevicePinLockDto pinLock, string pin, DeviceSealedDataDto sealedData)
    {
        var privateKey = await Decrypt(await DerivePinKey(Encoding.UTF8.GetBytes(pin), pinLock.Salt, PinIterations), pinLock.Iv, pinLock.PrivateKey);

        if (privateKey is null)
            return null;

        return await Decrypt(await DeriveSharedKey(privateKey, sealedData.PublicKey), sealedData.Iv, sealedData.Data);
    }

    protected virtual ValueTask<(byte[] PublicKey, byte[] PrivateKey)> GenerateKeyPair()
    {
        using var keyPair = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

        return ValueTask.FromResult((keyPair.ExportSubjectPublicKeyInfo(), keyPair.ExportPkcs8PrivateKey()));
    }

    protected virtual ValueTask<byte[]> DeriveSharedKey(byte[] privateKey, byte[] publicKey)
    {
        using var own = ECDiffieHellman.Create();
        own.ImportPkcs8PrivateKey(privateKey, out _);

        using var other = ECDiffieHellman.Create();
        other.ImportSubjectPublicKeyInfo(publicKey, out _);

        return ValueTask.FromResult(own.DeriveKeyFromHash(other.PublicKey, HashAlgorithmName.SHA256));
    }

    protected virtual ValueTask<byte[]> DerivePinKey(byte[] pin, byte[] salt, int iterations)
    {
        return new(Task.Run(() => Rfc2898DeriveBytes.Pbkdf2(pin, salt, iterations, HashAlgorithmName.SHA256, 32)));
    }

    protected virtual ValueTask<byte[]> Encrypt(byte[] key, byte[] iv, byte[] data)
    {
        using var aes = new AesGcm(key, tagSizeInBytes: 16);

        var result = new byte[data.Length + 16];
        aes.Encrypt(iv, data, result.AsSpan(0, data.Length), result.AsSpan(data.Length));

        return ValueTask.FromResult(result);
    }

    protected virtual ValueTask<byte[]?> Decrypt(byte[] key, byte[] iv, byte[] data)
    {
        if (data.Length < 16)
            return ValueTask.FromResult<byte[]?>(null);

        using var aes = new AesGcm(key, tagSizeInBytes: 16);

        var result = new byte[data.Length - 16];

        try
        {
            aes.Decrypt(iv, data.AsSpan(0, result.Length), data.AsSpan(result.Length), result);
        }
        catch (AuthenticationTagMismatchException)
        {
            return ValueTask.FromResult<byte[]?>(null);
        }

        return ValueTask.FromResult<byte[]?>(result);
    }
}
