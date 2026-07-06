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
/// <remarks>
/// <b>Security note:</b> the key-handling methods on this type marshal raw key material across the
/// JavaScript&#8596;.NET interop boundary. Generated keys are created as <i>extractable</i> and their
/// bytes (symmetric <c>raw</c> keys, private <c>pkcs8</c> keys, and PBKDF2-derived bits) are exported
/// back to .NET, where they are transferred as base64 over the interop channel and may therefore
/// appear in interop logs, traces, or memory dumps. They are <b>not</b> retained inside the browser's
/// non-extractable key store. Treat returned key bytes as sensitive: avoid logging them, zero/clear
/// buffers when done where practical, and prefer server-side key custody when the threat model
/// requires keys never to leave a hardware/secure boundary.
/// </remarks>
public class Crypto(IJSRuntime js)
{
    /// <summary>
    /// Returns a cryptographically strong random Guid (v4 UUID).
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Crypto/randomUUID">https://developer.mozilla.org/en-US/docs/Web/API/Crypto/randomUUID</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async ValueTask<Guid> RandomUuid()
    {
        var raw = await js.Invoke<string>("BitButil.crypto.randomUUID");
        // During prerender/SSR the invoke returns a safe default (empty string), and a genuine
        // call always yields a parseable UUID. Guid.Parse(null/"") would throw, contradicting the
        // documented "returns default rather than throwing" prerender contract - so guard it.
        return Guid.TryParse(raw, out var uuid) ? uuid : default;
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
        return js.Invoke<byte[]>("BitButil.crypto.digest", HashAlgorithmName(algorithm), data);
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
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> VerifyHmac(CryptoKeyHash algorithm, byte[] key, byte[] signature, byte[] data)
    {
        var algo = HashAlgorithmName(algorithm);
        return js.Invoke<bool>("BitButil.crypto.verifyHmac", algo, key, signature, data);
    }

    private static string HashAlgorithmName(CryptoKeyHash algorithm) => algorithm switch
    {
        CryptoKeyHash.Sha256 => "SHA-256",
        CryptoKeyHash.Sha384 => "SHA-384",
        CryptoKeyHash.Sha512 => "SHA-512",
        // An out-of-range value (only reachable by casting an invalid int to the enum) is a caller
        // bug. For crypto, fail loudly rather than silently substituting SHA-256.
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Unsupported hash algorithm."),
    };

    // ─── Key generation / import / export ──────────────────────────────────────

    /// <summary>
    /// Generates a fresh AES key as raw bytes.
    /// </summary>
    /// <param name="bits">Key length in bits - 128, 192, or 256.</param>
    /// <remarks>The key is returned as extractable raw bytes - see the security note on <see cref="Crypto"/>.</remarks>
    public ValueTask<byte[]> GenerateAesKey(int bits = 256)
        => js.Invoke<byte[]>("BitButil.crypto.generateAesKey", bits);

    /// <summary>
    /// Generates an HMAC key of the requested length and hash.
    /// </summary>
    /// <remarks>The key is returned as extractable raw bytes - see the security note on <see cref="Crypto"/>.</remarks>
    public ValueTask<byte[]> GenerateHmacKey(CryptoKeyHash algorithm = CryptoKeyHash.Sha256, int? lengthBits = null)
        => js.Invoke<byte[]>("BitButil.crypto.generateHmacKey", HashAlgorithmName(algorithm), lengthBits);

    /// <summary>
    /// Generates an RSA key pair (RSA-OAEP). Returns spki/pkcs8 DER bytes for public/private.
    /// </summary>
    /// <remarks>The private key is returned as extractable pkcs8 bytes - see the security note on <see cref="Crypto"/>.</remarks>
    public ValueTask<RsaKeyPair> GenerateRsaKeyPair(int modulusLengthBits = 2048,
                                                    CryptoKeyHash algorithm = CryptoKeyHash.Sha256)
        => js.Invoke<RsaKeyPair>("BitButil.crypto.generateRsaKeyPair", modulusLengthBits, HashAlgorithmName(algorithm));

    /// <summary>
    /// Generates an ECDSA key pair on the named curve.
    /// </summary>
    /// <param name="curve">One of <c>"P-256"</c>, <c>"P-384"</c>, <c>"P-521"</c>.</param>
    /// <remarks>The private key is returned as extractable pkcs8 bytes - see the security note on <see cref="Crypto"/>.</remarks>
    public ValueTask<EcKeyPair> GenerateEcdsaKeyPair(string curve = "P-256")
        => js.Invoke<EcKeyPair>("BitButil.crypto.generateEcdsaKeyPair", curve);

    // ─── Derivation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Derives raw bytes from a password using PBKDF2.
    /// </summary>
    /// <remarks>The derived bits are returned as raw bytes - see the security note on <see cref="Crypto"/>.</remarks>
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
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
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
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
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
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AesCtrCryptoAlgorithmParams))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AesCbcCryptoAlgorithmParams))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AesGcmCryptoAlgorithmParams))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(RsaOaepCryptoAlgorithmParams))]
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
