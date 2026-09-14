namespace Bit.Butil;

/// <summary>
/// The key formats SubtleCrypto can read and write. Which ones a key accepts is decided by its
/// algorithm: a symmetric key is <see cref="Raw"/> or <see cref="Jwk"/>, a public key is
/// <see cref="Spki"/> or <see cref="Jwk"/>, and a private key is <see cref="Pkcs8"/> or
/// <see cref="Jwk"/>.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/importKey">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/importKey</see>
/// </summary>
public enum CryptoKeyFormat
{
    /// <summary>The unstructured bytes of a symmetric key (<c>"raw"</c>).</summary>
    Raw,

    /// <summary>A private key as PKCS#8 DER bytes (<c>"pkcs8"</c>).</summary>
    Pkcs8,

    /// <summary>A public key as SubjectPublicKeyInfo DER bytes (<c>"spki"</c>).</summary>
    Spki,

    /// <summary>A JSON Web Key (<c>"jwk"</c>) - see <see cref="CryptoJsonWebKey"/>.</summary>
    Jwk,
}
