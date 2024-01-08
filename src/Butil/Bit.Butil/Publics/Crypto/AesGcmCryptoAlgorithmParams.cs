namespace Bit.Butil;

/// <summary>
/// Represents the object that should be passed as the algorithm parameter into Crypto APIs when using the AES-GCM algorithm.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AesCbcParams">https://developer.mozilla.org/en-US/docs/Web/API/AesCbcParams</see>
/// </summary>
public class AesGcmCryptoAlgorithmParams : ICryptoAlgorithmParams
{
    public string Name => "AES-GCM";

    /// <summary>
    /// The initialization vector. This must be unique for every encryption operation carried out with a given key. 
    /// Put another way: never reuse an IV with the same key.
    /// The AES-GCM specification recommends that the IV should be 96 bits long,
    /// and typically contains bits from a random number generator.
    /// </summary>
    public byte[] Iv { get; set; } = default!;

    /// <summary>
    /// This contains additional data that will not be encrypted but will be authenticated along with the encrypted data.
    /// </summary>
    public byte[] AdditionalData { get; set; } = default!;

    /// <summary>
    /// This determines the size in bits of the authentication tag generated in the encryption operation and 
    /// used for authentication in the corresponding decryption.
    /// </summary>
    public AesGcmTagLength TagLength { get; set; } = AesGcmTagLength.Sixteen;
}
