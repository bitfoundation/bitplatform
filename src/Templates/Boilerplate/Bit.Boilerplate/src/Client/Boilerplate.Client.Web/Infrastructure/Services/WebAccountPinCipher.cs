using Bit.Butil;

namespace Boilerplate.Client.Web.Infrastructure.Services;

public partial class WebAccountPinCipher : AccountPinCipher
{
    [AutoInject] private Crypto crypto = default!;

    public override ValueTask<bool> IsSupported() => crypto.IsSupported();

    protected override async ValueTask<(byte[] PublicKey, byte[] PrivateKey)> GenerateKeyPair()
    {
        var keyPair = await crypto.GenerateEcdhKeyPair();

        return (keyPair.PublicKey, keyPair.PrivateKey);
    }

    protected override async ValueTask<byte[]> DeriveSharedKey(byte[] privateKey, byte[] publicKey)
    {
        return await crypto.Digest(CryptoKeyHash.Sha256, await crypto.DeriveEcdhBits(privateKey, publicKey, 256));
    }

    protected override ValueTask<byte[]> DerivePinKey(byte[] pin, byte[] salt, int iterations)
    {
        return crypto.DerivePbkdf2(pin, salt, iterations, 256);
    }

    protected override ValueTask<byte[]> Encrypt(byte[] key, byte[] iv, byte[] data)
    {
        return crypto.Encrypt(CryptoAlgorithm.AesGcm, key, data, iv);
    }

    protected override async ValueTask<byte[]?> Decrypt(byte[] key, byte[] iv, byte[] data)
    {
        try
        {
            return await crypto.Decrypt(CryptoAlgorithm.AesGcm, key, data, iv);
        }
        catch (JSException)
        {
            return null;
        }
    }
}
