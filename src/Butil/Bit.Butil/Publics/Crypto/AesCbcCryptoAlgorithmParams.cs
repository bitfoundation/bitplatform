namespace Bit.Butil;

/// <summary>
/// Represents the object that should be passed as the algorithm parameter into Crypto APIs when using the AES-CBC algorithm.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AesCbcParams">https://developer.mozilla.org/en-US/docs/Web/API/AesCbcParams</see>
/// </summary>
public class AesCbcCryptoAlgorithmParams : ICryptoAlgorithmParams
{
    public string Name => "AES-CBC";

    /// <summary>
    /// The initialization vector. Must be 16 bytes (128 bits), unpredictable, and preferably cryptographically random.
    /// </summary>
    public byte[] Iv { get; set; } = default!;
}
