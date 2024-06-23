namespace Bit.Butil;

/// <summary>
/// Represents the object that should be passed as the algorithm parameter into Crypto APIs when using the AES-CTR algorithm.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/AesCtrParams">https://developer.mozilla.org/en-US/docs/Web/API/AesCtrParams</see>
/// </summary>
public class AesCtrCryptoAlgorithmParams : ICryptoAlgorithmParams
{
    public string Name => "AES-CTR";

    /// <summary>
    /// The initial value of the counter block. This must be 16 bytes (128 bits) long (the AES block size).
    /// </summary>
    public byte[] Counter { get; set; } = default!;

    /// <summary>
    /// The number of bits in the counter block that are used for the actual counter.
    /// </summary>
    public int Length { get; set; }
}
