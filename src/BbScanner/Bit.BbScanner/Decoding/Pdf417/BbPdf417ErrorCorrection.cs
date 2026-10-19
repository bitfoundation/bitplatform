namespace Bit.BbScanner;

/// <summary>
/// Reed-Solomon error correction over GF(929), as used by PDF417.
/// </summary>
/// <remarks>
/// The structure mirrors the byte oriented decoder: syndromes, the Euclidean algorithm to find
/// the error locator and evaluator, then Forney's formula for the magnitudes. The differences
/// are all consequences of the field being prime rather than binary: subtraction is a real
/// operation, and magnitudes are found through the formal derivative of the locator.
/// </remarks>
public sealed class BbPdf417ErrorCorrection
{
    private readonly BbModulusGF _field = BbModulusGF.Pdf417;

    /// <summary>
    /// Repairs <paramref name="received"/> in place.
    /// </summary>
    /// <param name="received">Data codewords followed by error correction codewords.</param>
    /// <param name="numEcCodewords">Number of error correction codewords.</param>
    /// <param name="errorsCorrected">Number of codewords that had to be changed.</param>
    /// <returns><see langword="false"/> when the block carries more errors than it can correct.</returns>
    public bool TryDecode(int[] received, int numEcCodewords, out int errorsCorrected) =>
        TryDecode(received, numEcCodewords, erasures: null, out errorsCorrected);

    /// <summary>
    /// Repairs <paramref name="received"/> in place, given the positions the detector could not
    /// read at all.
    /// </summary>
    /// <param name="received">Data codewords followed by error correction codewords; an erased position may hold any value.</param>
    /// <param name="numEcCodewords">Number of error correction codewords.</param>
    /// <param name="erasures">Indices into <paramref name="received"/> whose value is unknown, or <see langword="null"/>.</param>
    /// <param name="errorsCorrected">Number of codewords that had to be changed, erasures included.</param>
    /// <returns><see langword="false"/> when the block carries more damage than it can correct.</returns>
    /// <remarks>
    /// Erasures are corrected as ordinary errors, after being zeroed so that they hold a known
    /// value: that spends two correction codewords on each instead of one, but it keeps a single
    /// decoding path, and a symbol that needs the full erasure capacity is one the detector has
    /// already lost most of.
    /// </remarks>
    public bool TryDecode(int[] received, int numEcCodewords, IReadOnlyList<int>? erasures, out int errorsCorrected)
    {
        ArgumentNullException.ThrowIfNull(received);
        errorsCorrected = 0;

        if (erasures is not null)
        {
            if (erasures.Count > numEcCodewords / 2)
            {
                return false;
            }

            foreach (var position in erasures)
            {
                if ((uint)position >= (uint)received.Length)
                {
                    return false;
                }

                received[position] = 0;
            }
        }

        foreach (var codeword in received)
        {
            if ((uint)codeword >= (uint)_field.Modulus)
            {
                return false;
            }
        }

        var poly = new BbModulusPoly(_field, received);
        var syndromeCoefficients = new int[numEcCodewords];
        var hasError = false;

        for (var i = numEcCodewords; i > 0; i--)
        {
            var evaluation = poly.EvaluateAt(_field.Exp(i));
            syndromeCoefficients[numEcCodewords - i] = evaluation;
            if (evaluation != 0)
            {
                hasError = true;
            }
        }

        if (!hasError)
        {
            return true;
        }

        var syndrome = new BbModulusPoly(_field, syndromeCoefficients);
        if (!TryRunEuclideanAlgorithm(_field.BuildMonomial(numEcCodewords, 1), syndrome, numEcCodewords, out var sigma, out var omega))
        {
            return false;
        }

        // A locator of degree zero names no positions while the syndromes are non-zero: the
        // damage could not be placed, so the block is rejected rather than passed on as is.
        if (sigma.Degree == 0 || !TryFindErrorLocations(sigma, out var errorLocations))
        {
            return false;
        }

        var errorMagnitudes = FindErrorMagnitudes(omega, sigma, errorLocations);
        for (var i = 0; i < errorLocations.Length; i++)
        {
            var position = received.Length - 1 - _field.Log(errorLocations[i]);
            if (position < 0)
            {
                return false;
            }

            received[position] = _field.Subtract(received[position], errorMagnitudes[i]);
        }

        // A block damaged beyond capacity can come out of the algorithm "corrected" into a
        // different, wrong codeword; checking the syndromes again catches that.
        poly = new BbModulusPoly(_field, received);
        for (var i = numEcCodewords; i > 0; i--)
        {
            if (poly.EvaluateAt(_field.Exp(i)) != 0)
            {
                return false;
            }
        }

        errorsCorrected = errorLocations.Length;
        return true;
    }

    private bool TryRunEuclideanAlgorithm(BbModulusPoly a, BbModulusPoly b, int r, out BbModulusPoly sigma, out BbModulusPoly omega)
    {
        sigma = _field.Zero;
        omega = _field.Zero;

        if (a.Degree < b.Degree)
        {
            (a, b) = (b, a);
        }

        var rLast = a;
        var rCurrent = b;
        var tLast = _field.Zero;
        var tCurrent = _field.One;

        while (rCurrent.Degree >= r / 2)
        {
            var rLastLast = rLast;
            var tLastLast = tLast;
            rLast = rCurrent;
            tLast = tCurrent;

            if (rLast.IsZero)
            {
                return false;
            }

            rCurrent = rLastLast;
            var q = _field.Zero;
            var denominatorLeadingTerm = rLast.GetCoefficient(rLast.Degree);
            var dltInverse = _field.Inverse(denominatorLeadingTerm);

            while (rCurrent.Degree >= rLast.Degree && !rCurrent.IsZero)
            {
                var degreeDiff = rCurrent.Degree - rLast.Degree;
                var scale = _field.Multiply(rCurrent.GetCoefficient(rCurrent.Degree), dltInverse);
                q = q.Add(_field.BuildMonomial(degreeDiff, scale));
                rCurrent = rCurrent.Subtract(rLast.MultiplyByMonomial(degreeDiff, scale));
            }

            tCurrent = q.Multiply(tLast).Subtract(tLastLast).Negative();

            if (rCurrent.Degree >= rLast.Degree)
            {
                return false;
            }
        }

        var sigmaTildeAtZero = tCurrent.GetCoefficient(0);
        if (sigmaTildeAtZero == 0)
        {
            return false;
        }

        var inverse = _field.Inverse(sigmaTildeAtZero);
        sigma = tCurrent.Multiply(inverse);
        omega = rCurrent.Multiply(inverse);
        return true;
    }

    private bool TryFindErrorLocations(BbModulusPoly errorLocator, out int[] locations)
    {
        var errorCount = errorLocator.Degree;
        locations = new int[errorCount];
        var found = 0;

        for (var i = 1; i < _field.Modulus && found < errorCount; i++)
        {
            if (errorLocator.EvaluateAt(i) == 0)
            {
                locations[found++] = _field.Inverse(i);
            }
        }

        return found == errorCount;
    }

    private int[] FindErrorMagnitudes(BbModulusPoly errorEvaluator, BbModulusPoly errorLocator, int[] errorLocations)
    {
        var degree = errorLocator.Degree;
        if (degree < 1)
        {
            return [];
        }

        // The formal derivative of the error locator; in a prime field this is the natural way to
        // evaluate Forney's formula, and it is why this decoder needs the locator as well as the
        // evaluator.
        var derivativeCoefficients = new int[degree];
        for (var i = 1; i <= degree; i++)
        {
            derivativeCoefficients[degree - i] = _field.Multiply(i, errorLocator.GetCoefficient(i));
        }

        var derivative = new BbModulusPoly(_field, derivativeCoefficients);

        var result = new int[errorLocations.Length];
        for (var i = 0; i < result.Length; i++)
        {
            var xiInverse = _field.Inverse(errorLocations[i]);
            var numerator = _field.Subtract(0, errorEvaluator.EvaluateAt(xiInverse));
            var denominator = _field.Inverse(derivative.EvaluateAt(xiInverse));
            result[i] = _field.Multiply(numerator, denominator);
        }

        return result;
    }

    /// <summary>Number of error correction codewords a security level uses.</summary>
    /// <param name="errorCorrectionLevel">The level, 0 to 8.</param>
    public static int GetErrorCorrectionCodewordCount(int errorCorrectionLevel)
    {
        if (errorCorrectionLevel is < 0 or > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(errorCorrectionLevel), "PDF417 security levels run from 0 to 8.");
        }

        return 1 << (errorCorrectionLevel + 1);
    }
}
