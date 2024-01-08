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
    public ValueTask<byte[]> Encrypt<T>(T algorithm, byte[] key, byte[] data, CryptoKeyHash? keyHash = null) where T : ICryptoAlgorithmParams
        => js.CryptoEncrypt(algorithm, key, data, keyHash);
    /// <summary>
    /// The Encrypt method of the Crypto interface that encrypts data.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/encrypt">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/encrypt</see>
    /// </summary>
    public ValueTask<byte[]> Encrypt(CryptoAlgorithm algorithm, byte[]key, byte[]data, byte[]? iv = null, CryptoKeyHash? keyHash = null)
        => js.CryptoEncrypt(algorithm, key, data, iv, keyHash);

    /// <summary>
    /// The Decrypt method of the Crypto interface that decrypts data.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/decrypt">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/decrypt</see>
    /// </summary>
    public ValueTask<byte[]> Decrypt<T>(T algorithm, byte[] key, byte[] data, CryptoKeyHash? keyHash = null) where T : ICryptoAlgorithmParams
        => js.CryptoDecrypt(algorithm, key, data, keyHash);
    /// <summary>
    /// The Decrypt method of the Crypto interface that decrypts data.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/decrypt">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/decrypt</see>
    /// </summary>
    public ValueTask<byte[]> Decrypt(CryptoAlgorithm algorithm, byte[] key, byte[] data, byte[]? iv = null, CryptoKeyHash? keyHash = null)
        => js.CryptoDecrypt(algorithm, key, data, iv, keyHash);
}
