
namespace Bit.BbScanner.TestKit;

/// <summary>
/// Generates Reed-Solomon check codewords, the inverse of
/// <see cref="BbScannerReedSolomonDecoder"/>.
/// </summary>
/// <remarks>
/// The library only needs to decode, so the encoder lives with the tests. Having both sides
/// means every symbology can be validated by round trip rather than by fixture images.
/// </remarks>
public sealed class ReedSolomonEncoder
{
    private readonly BbScannerGenericGF _field;
    private readonly List<BbScannerGfPoly> _cachedGenerators = [];

    /// <summary>Creates an encoder over a field.</summary>
    /// <param name="field">The field the codewords live in.</param>
    public ReedSolomonEncoder(BbScannerGenericGF field)
    {
        _field = field;
        _cachedGenerators.Add(new BbScannerGfPoly(field, [1]));
    }

    private BbScannerGfPoly BuildGenerator(int degree)
    {
        if (degree >= _cachedGenerators.Count)
        {
            var last = _cachedGenerators[^1];
            for (var d = _cachedGenerators.Count; d <= degree; d++)
            {
                var next = last.Multiply(new BbScannerGfPoly(_field, [1, _field.Exp(d - 1 + _field.GeneratorBase)]));
                _cachedGenerators.Add(next);
                last = next;
            }
        }

        return _cachedGenerators[degree];
    }

    /// <summary>
    /// Appends <paramref name="ecBytes"/> check codewords to <paramref name="toEncode"/>, which
    /// must already be sized to hold them.
    /// </summary>
    /// <param name="toEncode">Data codewords followed by <paramref name="ecBytes"/> free slots.</param>
    /// <param name="ecBytes">Number of check codewords to generate.</param>
    public void Encode(int[] toEncode, int ecBytes)
    {
        ArgumentNullException.ThrowIfNull(toEncode);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ecBytes);

        var dataBytes = toEncode.Length - ecBytes;
        if (dataBytes <= 0)
        {
            throw new ArgumentException("There is no data to encode.", nameof(toEncode));
        }

        var generator = BuildGenerator(ecBytes);
        var infoCoefficients = toEncode[..dataBytes];
        var info = new BbScannerGfPoly(_field, infoCoefficients).MultiplyByMonomial(ecBytes, 1);
        var (_, remainder) = info.Divide(generator);

        var coefficients = remainder.Coefficients;
        var numZeroCoefficients = ecBytes - coefficients.Length;
        for (var i = 0; i < numZeroCoefficients; i++)
        {
            toEncode[dataBytes + i] = 0;
        }

        Array.Copy(coefficients, 0, toEncode, dataBytes + numZeroCoefficients, coefficients.Length);
    }
}
