namespace Bit.Butil;

/// <summary>
/// The algorithms a key can be imported, exported, derived or wrapped under. This names the key
/// rather than the operation: a key imported under the wrong algorithm is rejected by the browser
/// with a <c>DataError</c> even when its bytes are right.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/importKey">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/importKey</see>
/// </summary>
public enum CryptoKeyAlgorithmName
{
    /// <summary>AES-GCM - authenticated symmetric encryption.</summary>
    AesGcm,

    /// <summary>AES-CBC - symmetric encryption without authentication.</summary>
    AesCbc,

    /// <summary>AES-CTR - AES in counter mode.</summary>
    AesCtr,

    /// <summary>AES-KW - the key-wrapping-only variant of AES; it encrypts keys, never messages.</summary>
    AesKw,

    /// <summary>HMAC - keyed message authentication. Carries a hash.</summary>
    Hmac,

    /// <summary>RSA-OAEP - public-key encryption. Carries a hash.</summary>
    RsaOaep,

    /// <summary>RSA-PSS - public-key signatures. Carries a hash.</summary>
    RsaPss,

    /// <summary>ECDSA - elliptic-curve signatures. Carries a named curve.</summary>
    Ecdsa,

    /// <summary>ECDH - elliptic-curve key agreement. Carries a named curve.</summary>
    Ecdh,

    /// <summary>HKDF - the extract-and-expand key derivation function.</summary>
    Hkdf,

    /// <summary>PBKDF2 - password-based key derivation.</summary>
    Pbkdf2,
}
