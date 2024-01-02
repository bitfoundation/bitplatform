namespace Bit.Butil;

/// <summary>
/// Represents the object that should be passed as the algorithm parameter into Crypto APIs when using the RSA_OAEP algorithm.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/RsaOaepParams">https://developer.mozilla.org/en-US/docs/Web/API/RsaOaepParams</see>
/// </summary>
public class RsaOaepCryptoAlgorithmParams : ICryptoAlgorithmParams
{
    public string Name => "RSA-OAEP";

    /// <summary>
    /// An array of bytes that does not itself need to be encrypted but which should be bound to the ciphertext. 
    /// A digest of the label is part of the input to the encryption operation.
    /// </summary>
    public byte[] Label { get; set; } = default!;
}
