using System;
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
    /// Returns a cryptographically strong random Guid (v4 UUID).
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Crypto/randomUUID">https://developer.mozilla.org/en-US/docs/Web/API/Crypto/randomUUID</see>
    /// </summary>
    public async ValueTask<Guid> RandomUuid()
    {
        var raw = await js.Invoke<string>("BitButil.crypto.randomUUID");
        return Guid.Parse(raw);
    }

    /// <summary>
    /// Fills <paramref name="length"/> bytes with cryptographically strong random values.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Crypto/getRandomValues">https://developer.mozilla.org/en-US/docs/Web/API/Crypto/getRandomValues</see>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="length"/> is negative or above the
    /// browser's per-call limit (65 536).</exception>
    public ValueTask<byte[]> GetRandomValues(int length)
    {
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), "length must be non-negative.");
        if (length > 65536)
            throw new ArgumentOutOfRangeException(nameof(length), "Web Crypto rejects requests larger than 65 536 bytes.");

        return js.Invoke<byte[]>("BitButil.crypto.getRandomValues", length);
    }

    /// <summary>
    /// Computes a digest of <paramref name="data"/> using the requested algorithm.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/digest">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/digest</see>
    /// </summary>
    public ValueTask<byte[]> Digest(CryptoKeyHash algorithm, byte[] data)
    {
        var algo = algorithm switch
        {
            CryptoKeyHash.Sha384 => "SHA-384",
            CryptoKeyHash.Sha512 => "SHA-512",
            _ => "SHA-256",
        };
        return js.Invoke<byte[]>("BitButil.crypto.digest", algo, data);
    }

    /// <summary>
    /// Produces an HMAC tag for <paramref name="data"/> using the given symmetric key.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/sign">SubtleCrypto.sign()</see>
    /// </summary>
    public ValueTask<byte[]> SignHmac(CryptoKeyHash algorithm, byte[] key, byte[] data)
    {
        var algo = HashAlgorithmName(algorithm);
        return js.Invoke<byte[]>("BitButil.crypto.signHmac", algo, key, data);
    }

    /// <summary>
    /// Verifies an HMAC tag previously produced by <see cref="SignHmac"/> (or any compatible producer).
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/verify">SubtleCrypto.verify()</see>
    /// </summary>
    public ValueTask<bool> VerifyHmac(CryptoKeyHash algorithm, byte[] key, byte[] signature, byte[] data)
    {
        var algo = HashAlgorithmName(algorithm);
        return js.Invoke<bool>("BitButil.crypto.verifyHmac", algo, key, signature, data);
    }

    private static string HashAlgorithmName(CryptoKeyHash algorithm) => algorithm switch
    {
        CryptoKeyHash.Sha384 => "SHA-384",
        CryptoKeyHash.Sha512 => "SHA-512",
        _ => "SHA-256",
    };

    // ─── Key generation / import / export ──────────────────────────────────────

    /// <summary>
    /// Generates a fresh AES key as raw bytes.
    /// </summary>
    /// <param name="bits">Key length in bits — 128, 192, or 256.</param>
    public ValueTask<byte[]> GenerateAesKey(int bits = 256)
        => js.Invoke<byte[]>("BitButil.crypto.generateAesKey", bits);

    /// <summary>
    /// Generates an HMAC key of the requested length and hash.
    /// </summary>
    public ValueTask<byte[]> GenerateHmacKey(CryptoKeyHash algorithm = CryptoKeyHash.Sha256, int? lengthBits = null)
        => js.Invoke<byte[]>("BitButil.crypto.generateHmacKey", HashAlgorithmName(algorithm), lengthBits);

    /// <summary>
    /// Generates an RSA key pair (RSA-OAEP). Returns spki/pkcs8 DER bytes for public/private.
    /// </summary>
    public ValueTask<RsaKeyPair> GenerateRsaKeyPair(int modulusLengthBits = 2048,
                                                    CryptoKeyHash algorithm = CryptoKeyHash.Sha256)
        => js.Invoke<RsaKeyPair>("BitButil.crypto.generateRsaKeyPair", modulusLengthBits, HashAlgorithmName(algorithm));

    /// <summary>
    /// Generates an ECDSA key pair on the named curve.
    /// </summary>
    /// <param name="curve">One of <c>"P-256"</c>, <c>"P-384"</c>, <c>"P-521"</c>.</param>
    public ValueTask<EcKeyPair> GenerateEcdsaKeyPair(string curve = "P-256")
        => js.Invoke<EcKeyPair>("BitButil.crypto.generateEcdsaKeyPair", curve);

    // ─── Derivation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Derives raw bytes from a password using PBKDF2.
    /// </summary>
    public ValueTask<byte[]> DerivePbkdf2(byte[] password, byte[] salt, int iterations,
                                          int outputLengthBits, CryptoKeyHash algorithm = CryptoKeyHash.Sha256)
        => js.Invoke<byte[]>("BitButil.crypto.derivePbkdf2", password, salt, iterations, outputLengthBits, HashAlgorithmName(algorithm));

    // ─── RSA-PSS sign / verify ─────────────────────────────────────────────────

    /// <summary>
    /// Produces an RSA-PSS signature using a PKCS8 private key.
    /// </summary>
    public ValueTask<byte[]> SignRsaPss(byte[] privateKey, byte[] data, int saltLength = 32,
                                        CryptoKeyHash algorithm = CryptoKeyHash.Sha256)
        => js.Invoke<byte[]>("BitButil.crypto.signRsaPss", privateKey, data, saltLength, HashAlgorithmName(algorithm));

    /// <summary>
    /// Verifies an RSA-PSS signature using an SPKI public key.
    /// </summary>
    public ValueTask<bool> VerifyRsaPss(byte[] publicKey, byte[] signature, byte[] data, int saltLength = 32,
                                        CryptoKeyHash algorithm = CryptoKeyHash.Sha256)
        => js.Invoke<bool>("BitButil.crypto.verifyRsaPss", publicKey, signature, data, saltLength, HashAlgorithmName(algorithm));

    // ─── ECDSA sign / verify ───────────────────────────────────────────────────

    /// <summary>
    /// Produces an ECDSA signature using a PKCS8 private key.
    /// </summary>
    public ValueTask<byte[]> SignEcdsa(byte[] privateKey, byte[] data, string curve = "P-256",
                                       CryptoKeyHash algorithm = CryptoKeyHash.Sha256)
        => js.Invoke<byte[]>("BitButil.crypto.signEcdsa", privateKey, data, curve, HashAlgorithmName(algorithm));

    /// <summary>
    /// Verifies an ECDSA signature using an SPKI public key.
    /// </summary>
    public ValueTask<bool> VerifyEcdsa(byte[] publicKey, byte[] signature, byte[] data, string curve = "P-256",
                                       CryptoKeyHash algorithm = CryptoKeyHash.Sha256)
        => js.Invoke<bool>("BitButil.crypto.verifyEcdsa", publicKey, signature, data, curve, HashAlgorithmName(algorithm));

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

            return js.Invoke<byte[]>("BitButil.crypto.encryptRsaOaep", algorithm, key, data, keyHashString);
        }

        if (algorithm.GetType() == typeof(AesCtrCryptoAlgorithmParams))
        {
            return js.Invoke<byte[]>("BitButil.crypto.encryptAesCtr", algorithm, key, data);
        }

        if (algorithm.GetType() == typeof(AesCbcCryptoAlgorithmParams))
        {
            return js.Invoke<byte[]>("BitButil.crypto.encryptAesCbc", algorithm, key, data);
        }


        return js.Invoke<byte[]>("BitButil.crypto.encryptAesGcm", algorithm, key, data);
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

            return js.Invoke<byte[]>("BitButil.crypto.decryptRsaOaep", algorithm, key, data, keyHashString);
        }

        if (algorithm.GetType() == typeof(AesCtrCryptoAlgorithmParams))
        {
            return js.Invoke<byte[]>("BitButil.crypto.decryptAesCtr", algorithm, key, data);
        }

        if (algorithm.GetType() == typeof(AesCbcCryptoAlgorithmParams))
        {
            return js.Invoke<byte[]>("BitButil.crypto.decryptAesCbc", algorithm, key, data);
        }

        return js.Invoke<byte[]>("BitButil.crypto.decryptAesGcm", algorithm, key, data);
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
