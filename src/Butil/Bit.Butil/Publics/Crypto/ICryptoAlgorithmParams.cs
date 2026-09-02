namespace Bit.Butil;

/// <summary>
/// The shape every algorithm parameter object shares: the algorithm's own name, plus whatever
/// extra fields that algorithm needs (an IV, a counter, additional data). Implemented by
/// <see cref="AesCbcCryptoAlgorithmParams"/>, <see cref="AesCtrCryptoAlgorithmParams"/>,
/// <see cref="AesGcmCryptoAlgorithmParams"/> and <see cref="RsaOaepCryptoAlgorithmParams"/>.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/encrypt">SubtleCrypto.encrypt()</see>
/// </summary>
public interface ICryptoAlgorithmParams
{
    /// <summary>The Web Crypto algorithm identifier - e.g. <c>"AES-GCM"</c>.</summary>
    string Name { get; }
}
