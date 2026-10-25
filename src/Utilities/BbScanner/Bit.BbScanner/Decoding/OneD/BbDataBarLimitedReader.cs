using System.Globalization;
using System.Text;

namespace Bit.BbScanner;

/// <summary>
/// Decodes GS1 DataBar Limited (ISO/IEC 24724): a GTIN-14 that starts with 0 or 1, in a symbol 74 modules wide and as
/// little as ten tall, for items too small for any other linear symbol - pharmaceuticals, cosmetics, jewellery.
/// </summary>
/// <remarks>
/// <para>
/// A symbol is a guard - a module of light and a module of bar - two data characters with a check character between
/// them, and a guard of a module of light, a module of bar and five modules of light. Each data character is fourteen
/// bars and spaces adding up to 26 modules, a space first, and stands for a number below 2 013 571 through the same
/// combinatorics as the other DataBar symbols; the GTIN, without its check digit, is the left one times 2 013 571
/// plus the right one.
/// </para>
/// <para>
/// One check guards the data: the check character, twelve elements of eight modules and a final module of light and
/// one of bar, carries the element widths of the two data characters, weighted and summed modulo 89. And the symbol has
/// no finder pattern to say where it is: any 45 runs of a line - a bar, 42 runs, a space and a bar - might be one, so
/// the reader asks a lot of a candidate before it rounds a single element. The guard's bar and the three single-module
/// elements at the right end have to measure a module, the light after the last bar five, the light before the first
/// one, and each character the modules it should; the check character has to be one of the 89 of its 441 possible
/// arrangements the specification uses; and, as for every DataBar symbol, the pipeline reports one only once a second
/// line has read the same value.
/// </para>
/// <para>
/// The text is the GS1 element string the symbol stands for, <c>01</c> and the GTIN-14, and
/// <see cref="BbBarcodeResultMetadata.IsGs1"/> is set. A symbol whose linkage flag says it is the linear part of a
/// GS1 Composite symbol is not read: the two-dimensional part above it, which carries the rest of its data, is not.
/// </para>
/// </remarks>
public sealed class BbDataBarLimitedReader : IBbRowDecoder, IBbImageHolder
{
    /// <summary>
    /// The data characters by group: the largest value, the group's first value, the modules of the odd and of the even
    /// elements, the widest odd and widest even element, and how many arrangements the odd and the even elements have.
    /// </summary>
    private static readonly int[][] Groups =
    [
        [183063, 0, 17, 9, 6, 3, 6538, 28],
        [820063, 183064, 13, 13, 5, 4, 875, 728],
        [1000775, 820064, 9, 17, 3, 6, 28, 6454],
        [1491020, 1000776, 15, 11, 5, 4, 2415, 203],
        [1979844, 1491021, 11, 15, 4, 5, 203, 2408],
        [1996938, 1979845, 19, 7, 8, 1, 17094, 1],
        [2013570, 1996939, 7, 19, 1, 8, 1, 16632],
    ];

    /// <summary>The weight of every element width in the checksum: the left data character's, then the right one's.</summary>
    private static readonly int[] CheckWeights =
    [
        1, 3, 9, 27, 81, 65, 17, 51, 64, 14, 42, 37, 22, 66,
        20, 60, 2, 6, 18, 54, 73, 41, 34, 13, 39, 28, 84, 74,
    ];

    /// <summary>
    /// The checksum each arrangement of the check character stands for, by its number - 21 times its spaces' number
    /// plus its bars' - and -1 for the 352 arrangements the specification does not use.
    /// </summary>
    private static readonly int[] ChecksumOfArrangement = BuildChecksums();

    /// <summary>The runs from the left guard's bar to the right guard's bar.</summary>
    private const int SymbolRuns = 45;

    /// <summary>The modules from the left guard's bar to the right guard's bar.</summary>
    private const int SymbolModules = 73;

    /// <summary>
    /// The light, in modules, there has to be before the left guard's bar: the guard's own module of light, less what
    /// blur takes from a single module.
    /// </summary>
    private const float MinLeadingLight = 0.75f;

    /// <summary>The light, in modules, there has to be after the right guard's bar: the guard's own five, less blur's share.</summary>
    private const float MinTrailingLight = 4.5f;

    /// <summary>How much wider or narrower than its modules, in modules, every bar may measure.</summary>
    private const float MaxBarBias = 0.6f;

    /// <summary>A GTIN-14 that starts with 0 or 1, without its check digit, is below this.</summary>
    private const long MaxValue = 2_000_000_000_000L;

    private readonly List<int> _runs = new(256);
    private readonly List<int> _runStarts = new(256);
    private readonly int[] _widths = new int[40];
    private int[] _prefix = new int[257];

    /// <inheritdoc />
    public BbBarcodeFormat Formats => BbBarcodeFormat.DataBarLimited;

    /// <inheritdoc />
    void IBbImageHolder.ForgetImage()
    {
        BbLinearPatterns.Forget(_runs);
        BbLinearPatterns.Forget(_runStarts);
        Array.Clear(_prefix);
        Array.Clear(_widths);
    }

    /// <inheritdoc />
    public BbSymbolDecodeResult? DecodeRow(int rowNumber, BbBitRow row, BbBarcodeFormat enabledFormats)
    {
        // A symbol and the light either side of it.
        var runs = _runs;
        if ((enabledFormats & BbBarcodeFormat.DataBarLimited) == 0 || !BbLinearPatterns.ReadRuns(row, runs, _runStarts) ||
            runs.Count < SymbolRuns + 2)
        {
            return null;
        }

        var prefix = Prefix(runs);

        // The left guard's bar is a bar with a run of light before it.
        for (var guard = row[_runStarts[0]] ? 2 : 1; guard + SymbolRuns < runs.Count; guard += 2)
        {
            if (TryRead(rowNumber, guard, prefix) is { } result)
            {
                return result;
            }
        }

        return null;
    }

    private BbSymbolDecodeResult? TryRead(int rowNumber, int guard, int[] prefix)
    {
        var runs = _runs;
        var end = guard + SymbolRuns - 1;
        var module = (prefix[end + 1] - prefix[guard]) / (float)SymbolModules;
        if (module < 0.8f ||
            !IsModule(runs[guard], module) || !IsModule(runs[end - 1], module) || !IsModule(runs[end], module) ||
            !IsModule(runs[guard + 27], module) || !IsModule(runs[guard + 28], module) ||
            runs[guard - 1] < module * MinLeadingLight || runs[end + 1] < module * MinTrailingLight ||
            !Near(prefix[guard + 15] - prefix[guard + 1], 26, module) ||
            !Near(prefix[guard + 29] - prefix[guard + 15], 18, module) ||
            !Near(prefix[guard + 43] - prefix[guard + 29], 26, module))
        {
            return null;
        }

        // How much wider than its modules every bar measures, from the five single-module elements: the guard's bar, and
        // the check character's last space and bar and the right guard's, the spaces' sign turned.
        var bias = ((runs[guard] - module) - (runs[guard + 27] - module) + (runs[guard + 28] - module) -
                    (runs[end - 1] - module) + (runs[end] - module)) / 5;
        if (MathF.Abs(bias) > module * MaxBarBias)
        {
            return null;
        }

        // The data characters' odd elements, the spaces, add up to an odd number of modules, and so do their bars; the
        // check character's twelve elements, both sets, to eight.
        var widths = _widths.AsSpan();
        var left = widths[..14];
        var check = widths[14..26];
        var right = widths[26..40];
        if (!BbDataBarElements.Quantize(runs, guard + 1, 14, 26, 8, 1, 1, oddFirst: true, firstIsBar: false, bias, left) ||
            !BbDataBarElements.Quantize(runs, guard + 15, 12, 16, 3, 0, 0, oddFirst: true, firstIsBar: false, bias, check) ||
            !BbDataBarElements.Quantize(runs, guard + 29, 14, 26, 8, 1, 1, oddFirst: true, firstIsBar: false, bias, right))
        {
            return null;
        }

        var checksum = 0;
        for (var i = 0; i < 14; i++)
        {
            checksum += (left[i] * CheckWeights[i]) + (right[i] * CheckWeights[14 + i]);
        }

        Span<int> spaces = stackalloc int[6];
        Span<int> bars = stackalloc int[6];
        var (spaceModules, barModules) = (0, 0);
        for (var i = 0; i < 6; i++)
        {
            (spaces[i], bars[i]) = (check[2 * i], check[(2 * i) + 1]);
            (spaceModules, barModules) = (spaceModules + spaces[i], barModules + bars[i]);
        }

        if (spaceModules != 8 || barModules != 8)
        {
            return null;
        }

        var arrangement = (21 * BbDataBarElements.RssValue(spaces, 3, noNarrow: false)) + BbDataBarElements.RssValue(bars, 3, noNarrow: false);
        if (arrangement < 0 || arrangement >= ChecksumOfArrangement.Length || ChecksumOfArrangement[arrangement] != checksum % 89 ||
            Character(left) is not { } d1 || Character(right) is not { } d2)
        {
            return null;
        }

        // A linked symbol's value is raised by 2 015 133 531 096, which takes it past every GTIN this symbol can hold.
        var value = (2013571L * d1) + d2;
        if (value >= MaxValue)
        {
            return null;
        }

        var digits = value.ToString("D13", CultureInfo.InvariantCulture);
        var text = $"01{digits}{BbDataBarElements.CheckDigit(digits)}";
        var payload = new BbDecoderResult(Encoding.ASCII.GetBytes(text), text) { IsGs1 = true };
        return new BbSymbolDecodeResult(
            payload,
            BbBarcodeFormat.DataBarLimited,
            [new BbScanPoint(_runStarts[guard], rowNumber), new BbScanPoint(_runStarts[end] + runs[end], rowNumber)]);

        static bool IsModule(int run, float module) => MathF.Abs(run - module) <= (module * 0.6f) + 0.5f;

        // Within a quarter of the width that many modules have.
        static bool Near(int width, int modules, float module) => MathF.Abs(width - (modules * module)) * 4 <= modules * module;
    }

    /// <summary>
    /// The value of a data character from its fourteen widths as drawn, odd elements first; the group their modules add
    /// up to says how the odd and even arrangements combine.
    /// </summary>
    private static int? Character(ReadOnlySpan<int> widths)
    {
        Span<int> odd = stackalloc int[7];
        Span<int> even = stackalloc int[7];
        var oddSum = 0;
        for (var i = 0; i < 7; i++)
        {
            (odd[i], even[i]) = (widths[2 * i], widths[(2 * i) + 1]);
            oddSum += odd[i];
        }

        foreach (var group in Groups)
        {
            if (group[2] != oddSum)
            {
                continue;
            }

            // The even elements may not all be wide.
            var vOdd = BbDataBarElements.RssValue(odd, group[4], noNarrow: false);
            var vEven = BbDataBarElements.RssValue(even, group[5], noNarrow: true);
            if (vOdd < 0 || vEven < 0 || vOdd >= group[6] || vEven >= group[7])
            {
                return null;
            }

            var value = group[1] + (vOdd * group[7]) + vEven;
            return value <= group[0] ? value : null;
        }

        return null;
    }

    /// <summary>Running totals of the runs, so that the width of any span of them is one subtraction.</summary>
    private int[] Prefix(List<int> runs)
    {
        if (_prefix.Length <= runs.Count)
        {
            _prefix = new int[runs.Count * 2];
        }

        var prefix = _prefix;
        prefix[0] = 0;
        for (var i = 0; i < runs.Count; i++)
        {
            prefix[i + 1] = prefix[i] + runs[i];
        }

        return prefix;
    }

    /// <summary>
    /// The specification's sequence of the check character's arrangements, in the order of the checksums they stand
    /// for, turned round into a lookup.
    /// </summary>
    private static int[] BuildChecksums()
    {
        int[] sequence =
        [
            .. Enumerable.Range(0, 44), 45, 52, 57, 63, 64, 65, 66, 73, 74, 75, 76, 77, 78, 79, 82,
            126, 127, 128, 129, 130, 132, 141, 142, 143, 144, 145, 146, 210, 211, 212, 213, 214, 215, 216, 217, 220,
            316, 317, 318, 319, 320, 322, 323, 326, 337,
        ];
        var checksums = new int[21 * 21];
        Array.Fill(checksums, -1);
        for (var checksum = 0; checksum < sequence.Length; checksum++)
        {
            checksums[sequence[checksum]] = checksum;
        }

        return checksums;
    }
}
