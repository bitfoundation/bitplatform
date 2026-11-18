
namespace Bit.Bink;

/// <summary>How stroke points are written.</summary>
public enum BinkPointEncoding : byte
{
    /// <summary>Quantized binary, base64 encoded. Compact and deterministic; the default.</summary>
    Binary = 0,
    /// <summary>Plain JSON number arrays. Larger, but readable and easy to consume from other languages.</summary>
    Json = 1,
}
