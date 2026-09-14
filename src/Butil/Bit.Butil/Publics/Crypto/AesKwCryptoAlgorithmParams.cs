namespace Bit.Butil;

/// <summary>
/// Represents the object that should be passed as the algorithm parameter into
/// <see cref="Crypto.WrapKey{T}"/> and <see cref="Crypto.UnwrapKey{T}"/> when using the AES-KW algorithm.
/// <br/>
/// AES-KW takes no parameters of its own - it is deterministic and carries its own integrity check -
/// so this type exists to name the algorithm, not to configure it.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/wrapKey">https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/wrapKey</see>
/// </summary>
/// <remarks>
/// AES-KW only wraps key material whose length is a multiple of 8 bytes, which every AES and most
/// HMAC keys satisfy; wrapping a PKCS#8 private key that is not padded to that boundary fails with
/// an <c>OperationError</c>. Use AES-GCM as the wrapping algorithm when the wrapped key can be any
/// length.
/// </remarks>
public class AesKwCryptoAlgorithmParams : ICryptoAlgorithmParams
{
    /// <summary>The Web Crypto algorithm identifier - always <c>"AES-KW"</c>.</summary>
    public string Name => "AES-KW";
}
