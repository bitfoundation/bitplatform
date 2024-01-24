using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// The Crypto interface represents basic cryptography features available in the current context. 
/// It allows access to a cryptographically strong random number generator and to cryptographic primitives.
/// <br />
/// More info: <see href="https://developer.mozilla.org/en-US/docs/Web/API/Crypto">https://developer.mozilla.org/en-US/docs/Web/API/Crypto</see>
/// </summary>
public class Crypto(IJSRuntime js)
{
    /// <summary>
    /// The Encrypt method of the Crypto interface that encrypts data.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/encrypt">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/encrypt</see>
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AesCtrCryptoAlgorithmParams))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AesCbcCryptoAlgorithmParams))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AesGcmCryptoAlgorithmParams))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RsaOaepCryptoAlgorithmParams))]
    public ValueTask<byte[]> Encrypt<T>(T algorithm, byte[] key, byte[] data, CryptoKeyHash? keyHash = null) where T : ICryptoAlgorithmParams
    {
        if (algorithm.GetType() == typeof(RsaOaepCryptoAlgorithmParams))
        {
            var keyHashString = keyHash switch
            {
                CryptoKeyHash.Sha384 => "SHA-384",
                CryptoKeyHash.Sha512 => "SHA-512",
                _ => "SHA-256",
            };

            return js.InvokeAsync<byte[]>("BitButil.crypto.encryptRsaOaep", algorithm, key, data, keyHashString);
        }

        if (algorithm.GetType() == typeof(AesCtrCryptoAlgorithmParams))
        {
            return js.InvokeAsync<byte[]>("BitButil.crypto.encryptAesCtr", algorithm, key, data);
        }

        if (algorithm.GetType() == typeof(AesCbcCryptoAlgorithmParams))
        {
            return js.InvokeAsync<byte[]>("BitButil.crypto.encryptAesCbc", algorithm, key, data);
        }


        return js.InvokeAsync<byte[]>("BitButil.crypto.encryptAesGcm", algorithm, key, data);
    }

    /// <summary>
    /// The Encrypt method of the Crypto interface that encrypts data.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/encrypt">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/encrypt</see>
    /// </summary>
    public ValueTask<byte[]> Encrypt(CryptoAlgorithm algorithm, byte[] key, byte[] data, byte[]? iv = null, CryptoKeyHash? keyHash = null)
        => algorithm switch
        {
            CryptoAlgorithm.AesCtr => Encrypt(new AesCtrCryptoAlgorithmParams { Counter = iv }, key, data, null),
            CryptoAlgorithm.AesCbc => Encrypt(new AesCbcCryptoAlgorithmParams { Iv = iv }, key, data, null),
            CryptoAlgorithm.AesGcm => Encrypt(new AesGcmCryptoAlgorithmParams { Iv = iv }, key, data, null),
            _ => Encrypt(new RsaOaepCryptoAlgorithmParams(), key, data, keyHash),
        };

    /// <summary>
    /// The Decrypt method of the Crypto interface that decrypts data.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/decrypt">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/decrypt</see>
    /// </summary>
    public ValueTask<byte[]> Decrypt<T>(T algorithm, byte[] key, byte[] data, CryptoKeyHash? keyHash = null) where T : ICryptoAlgorithmParams
    {
        if (algorithm.GetType() == typeof(RsaOaepCryptoAlgorithmParams))
        {
            var keyHashString = keyHash switch
            {
                CryptoKeyHash.Sha384 => "SHA-384",
                CryptoKeyHash.Sha512 => "SHA-512",
                _ => "SHA-256",
            };

            return js.InvokeAsync<byte[]>("BitButil.crypto.decryptRsaOaep", algorithm, key, data, keyHashString);
        }

        if (algorithm.GetType() == typeof(AesCtrCryptoAlgorithmParams))
        {
            return js.InvokeAsync<byte[]>("BitButil.crypto.decryptAesCtr", algorithm, key, data);
        }

        if (algorithm.GetType() == typeof(AesCbcCryptoAlgorithmParams))
        {
            return js.InvokeAsync<byte[]>("BitButil.crypto.decryptAesCbc", algorithm, key, data);
        }

        return js.InvokeAsync<byte[]>("BitButil.crypto.decryptAesGcm", algorithm, key, data);
    }
    /// <summary>
    /// The Decrypt method of the Crypto interface that decrypts data.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/decrypt">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/decrypt</see>
    /// </summary>
    public ValueTask<byte[]> Decrypt(CryptoAlgorithm algorithm, byte[] key, byte[] data, byte[]? iv = null, CryptoKeyHash? keyHash = null)
        => algorithm switch
        {
            CryptoAlgorithm.AesCtr => Decrypt(new AesCtrCryptoAlgorithmParams { Counter = iv }, key, data, null),
            CryptoAlgorithm.AesCbc => Decrypt(new AesCbcCryptoAlgorithmParams { Iv = iv }, key, data, null),
            CryptoAlgorithm.AesGcm => Decrypt(new AesGcmCryptoAlgorithmParams { Iv = iv }, key, data, null),
            _ => Decrypt(new RsaOaepCryptoAlgorithmParams(), key, data, keyHash),
        };
}
