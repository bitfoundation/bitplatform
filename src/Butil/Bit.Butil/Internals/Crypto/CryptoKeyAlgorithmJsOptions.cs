namespace Bit.Butil;

/// <summary>
/// <see cref="CryptoKeyAlgorithm"/> in the shape Web Crypto's algorithm dictionaries take: the
/// enums resolved to their identifier strings on this side of the boundary, so the JavaScript never
/// has to know what a <see cref="CryptoKeyAlgorithmName"/> is.
/// </summary>
internal class CryptoKeyAlgorithmJsOptions(CryptoKeyAlgorithm algorithm)
{
    public string name { get; set; } = algorithm.AlgorithmName();

    public string? hash { get; set; } = algorithm.Hash is null ? null : CryptoHashName.Resolve(algorithm.Hash.Value);

    public string? namedCurve { get; set; } = algorithm.NamedCurve;

    public int? length { get; set; } = algorithm.LengthBits;
}
