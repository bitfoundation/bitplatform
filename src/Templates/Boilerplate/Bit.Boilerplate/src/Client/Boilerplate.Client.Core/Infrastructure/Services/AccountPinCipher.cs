using System.Text;
using System.Security.Cryptography;

namespace Boilerplate.Client.Core.Infrastructure.Services;

public partial class AccountPinCipher
{
    public const int PinLength = 8;

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
