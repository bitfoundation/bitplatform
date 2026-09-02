namespace Bit.Butil;

/// <summary>
/// The algorithms this wrapper can encrypt and decrypt with. Which one a call uses is decided by
/// the <see cref="ICryptoAlgorithmParams"/> instance passed to it; this enum names the same set for
/// code that has to choose one by value.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/encrypt">SubtleCrypto.encrypt()</see>
/// </summary>
public enum CryptoAlgorithm
{
    /// <summary>RSA-OAEP - public-key encryption, limited to payloads smaller than the key.</summary>
    RsaOaem,

    /// <summary>AES-CTR - AES in counter mode, no built-in authentication.</summary>
    AesCtr,

    /// <summary>AES-CBC - AES in cipher-block-chaining mode, no built-in authentication.</summary>
    AesCbc,

    /// <summary>AES-GCM - AES with authentication built in; the sensible default of the four.</summary>
    AesGcm
}
