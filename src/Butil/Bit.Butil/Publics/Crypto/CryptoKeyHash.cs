namespace Bit.Butil;

/// <summary>
/// The digest an RSA key is imported with. It is a property of the key, not of the message, so the
/// same value has to be used to import a key that was exported under it.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/importKey">SubtleCrypto.importKey()</see>
/// </summary>
public enum CryptoKeyHash
{
    /// <summary>SHA-256.</summary>
    Sha256,

    /// <summary>SHA-384.</summary>
    Sha384,

    /// <summary>SHA-512.</summary>
    Sha512,
}
