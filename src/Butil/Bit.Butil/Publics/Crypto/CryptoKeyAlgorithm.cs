using System;

namespace Bit.Butil;

/// <summary>
/// Describes the key an import, export, derivation or unwrap produces - the algorithm it belongs to
/// plus whatever that algorithm needs to identify it (a hash for HMAC and the RSA family, a named
/// curve for the EC family, a length for AES).
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/importKey">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/importKey</see>
/// </summary>
/// <remarks>
/// The static factories cover the shapes the browser accepts; constructing one by hand is fine too,
/// but leaving out the field an algorithm requires makes the browser reject the key rather than
/// guess (an ECDH key with no curve, an HMAC key with no hash).
/// </remarks>
public class CryptoKeyAlgorithm
{
    /// <summary>The algorithm the key belongs to.</summary>
    public CryptoKeyAlgorithmName Name { get; set; }

    /// <summary>The digest bound into the key. Required for HMAC, RSA-OAEP, RSA-PSS, HKDF; ignored elsewhere.</summary>
    public CryptoKeyHash? Hash { get; set; }

    /// <summary>The named curve (<c>"P-256"</c>, <c>"P-384"</c>, <c>"P-521"</c>). Required for ECDSA and ECDH.</summary>
    public string? NamedCurve { get; set; }

    /// <summary>The key length in bits. Required when deriving or unwrapping an AES or HMAC key.</summary>
    public int? LengthBits { get; set; }

    /// <summary>An AES-GCM key of <paramref name="bits"/> bits (128, 192 or 256).</summary>
    public static CryptoKeyAlgorithm AesGcm(int bits = 256)
        => new() { Name = CryptoKeyAlgorithmName.AesGcm, LengthBits = bits };

    /// <summary>An AES-CBC key of <paramref name="bits"/> bits (128, 192 or 256).</summary>
    public static CryptoKeyAlgorithm AesCbc(int bits = 256)
        => new() { Name = CryptoKeyAlgorithmName.AesCbc, LengthBits = bits };

    /// <summary>An AES-CTR key of <paramref name="bits"/> bits (128, 192 or 256).</summary>
    public static CryptoKeyAlgorithm AesCtr(int bits = 256)
        => new() { Name = CryptoKeyAlgorithmName.AesCtr, LengthBits = bits };

    /// <summary>An AES-KW key-wrapping key of <paramref name="bits"/> bits (128, 192 or 256).</summary>
    public static CryptoKeyAlgorithm AesKw(int bits = 256)
        => new() { Name = CryptoKeyAlgorithmName.AesKw, LengthBits = bits };

    /// <summary>An HMAC key bound to <paramref name="hash"/>.</summary>
    public static CryptoKeyAlgorithm Hmac(CryptoKeyHash hash = CryptoKeyHash.Sha256, int? lengthBits = null)
        => new() { Name = CryptoKeyAlgorithmName.Hmac, Hash = hash, LengthBits = lengthBits };

    /// <summary>An RSA-OAEP key bound to <paramref name="hash"/>.</summary>
    public static CryptoKeyAlgorithm RsaOaep(CryptoKeyHash hash = CryptoKeyHash.Sha256)
        => new() { Name = CryptoKeyAlgorithmName.RsaOaep, Hash = hash };

    /// <summary>An RSA-PSS key bound to <paramref name="hash"/>.</summary>
    public static CryptoKeyAlgorithm RsaPss(CryptoKeyHash hash = CryptoKeyHash.Sha256)
        => new() { Name = CryptoKeyAlgorithmName.RsaPss, Hash = hash };

    /// <summary>An ECDSA key on <paramref name="curve"/>.</summary>
    public static CryptoKeyAlgorithm Ecdsa(string curve = "P-256")
        => new() { Name = CryptoKeyAlgorithmName.Ecdsa, NamedCurve = curve };

    /// <summary>An ECDH key on <paramref name="curve"/>.</summary>
    public static CryptoKeyAlgorithm Ecdh(string curve = "P-256")
        => new() { Name = CryptoKeyAlgorithmName.Ecdh, NamedCurve = curve };

    /// <summary>The Web Crypto identifier for <see cref="Name"/> - <c>"AES-GCM"</c>, <c>"RSA-OAEP"</c>, and so on.</summary>
    public string AlgorithmName() => Name switch
    {
        CryptoKeyAlgorithmName.AesGcm => "AES-GCM",
        CryptoKeyAlgorithmName.AesCbc => "AES-CBC",
        CryptoKeyAlgorithmName.AesCtr => "AES-CTR",
        CryptoKeyAlgorithmName.AesKw => "AES-KW",
        CryptoKeyAlgorithmName.Hmac => "HMAC",
        CryptoKeyAlgorithmName.RsaOaep => "RSA-OAEP",
        CryptoKeyAlgorithmName.RsaPss => "RSA-PSS",
        CryptoKeyAlgorithmName.Ecdsa => "ECDSA",
        CryptoKeyAlgorithmName.Ecdh => "ECDH",
        CryptoKeyAlgorithmName.Hkdf => "HKDF",
        CryptoKeyAlgorithmName.Pbkdf2 => "PBKDF2",
        // Only reachable by casting an invalid int to the enum. For crypto, fail loudly rather
        // than silently substituting something that would encrypt under the wrong algorithm.
        _ => throw new ArgumentOutOfRangeException(nameof(Name), Name, "Unsupported key algorithm."),
    };
}
