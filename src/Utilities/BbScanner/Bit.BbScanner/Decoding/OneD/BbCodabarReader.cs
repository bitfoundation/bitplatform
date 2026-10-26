using System.Text;

namespace Bit.BbScanner;

/// <summary>
/// Decodes Codabar (also called NW-7, Code 2 of 7 or USD-4), as used by libraries, blood banks
/// and courier waybills.
/// </summary>
/// <remarks>
/// <para>
/// A Codabar character is seven elements, four bars and three spaces, each narrow or wide, and
/// characters are separated by a narrow gap. Sixteen characters carry data - the digits and
/// <c>- $ : / . +</c> - and four, <c>A</c> to <c>D</c>, only ever start or stop a symbol (some
/// printers label the same four patterns <c>T</c>, <c>N</c>, <c>*</c> and <c>E</c>).
/// </para>
/// <para>
/// Codabar has no check character, so nothing in the symbol catches a misread, and its simple
/// structure is easy to assemble by accident out of print and packaging. Every character is
/// therefore matched against all twenty patterns at once, taking a narrow and a wide width that
/// fit it best, and only an unambiguous fit is accepted; the widths have to agree from character
/// to character; a symbol has to open and close with a start/stop character, carry at least
/// three data characters and sit between quiet zones; and <see cref="BbBarcodeDecoder"/> only
/// reports it once a second scan line has read the same value.
/// </para>
/// </remarks>
public sealed class BbCodabarReader : IBbRowDecoder, IBbImageHolder
{
    private const string Alphabet = "0123456789-$:/.+ABCD";

    /// <summary>The first start/stop character in <see cref="Alphabet"/>.</summary>
    private const int FirstStartStop = 16;

    /// <summary>Fewest data characters a symbol may carry; shorter is too easy to come across by chance.</summary>
    private const int MinDataCharacters = 3;

    /// <summary>
    /// The largest root mean square misfit a character may have, in units of its wide minus its
    /// narrow width.
    /// </summary>
    private const float MaxFitError = 0.22f;

    /// <summary>
    /// The seven element widths of every character, in units of the narrow element, bar first,
    /// with wide shown as two; indexed as <see cref="Alphabet"/>.
    /// </summary>
    private static readonly int[][] Patterns =
    [
        [1, 1, 1, 1, 1, 2, 2], [1, 1, 1, 1, 2, 2, 1], [1, 1, 1, 2, 1, 1, 2], [2, 2, 1, 1, 1, 1, 1], [1, 1, 2, 1, 1, 2, 1], // 0-4
        [2, 1, 1, 1, 1, 2, 1], [1, 2, 1, 1, 1, 1, 2], [1, 2, 1, 1, 2, 1, 1], [1, 2, 2, 1, 1, 1, 1], [2, 1, 1, 2, 1, 1, 1], // 5-9
        [1, 1, 1, 2, 2, 1, 1], [1, 1, 2, 2, 1, 1, 1], [2, 1, 1, 1, 2, 1, 2], [2, 1, 2, 1, 1, 1, 2], [2, 1, 2, 1, 2, 1, 1], // - $ : / .
        [1, 1, 2, 1, 2, 1, 2], [1, 1, 2, 2, 1, 2, 1], [1, 2, 1, 2, 1, 1, 2], [1, 1, 1, 2, 1, 2, 2], [1, 1, 1, 2, 2, 2, 1], // + A B C D
    ];

    private readonly bool _keepStartStop;
    private readonly List<int> _runs = new(256);
    private readonly List<int> _runStarts = new(256);

    /// <inheritdoc />
    void IBbImageHolder.ForgetImage()
    {
        BbLinearPatterns.Forget(_runs);
        BbLinearPatterns.Forget(_runStarts);
    }
    private readonly List<(int Value, float Narrow, float Wide)> _characters = new(32);
    private readonly StringBuilder _builder = new(32);

    /// <summary>Creates a reader.</summary>
    /// <param name="keepStartStop">
    /// When <see langword="true"/> the start and stop characters are kept in the decoded text,
    /// as the letters <c>A</c> to <c>D</c>; otherwise only the data between them is reported.
    /// </param>
    public BbCodabarReader(bool keepStartStop = false)
    {
        _keepStartStop = keepStartStop;
    }

    /// <inheritdoc />
    public BbBarcodeFormat Formats => BbBarcodeFormat.Codabar;

    /// <inheritdoc />
    public BbSymbolDecodeResult? DecodeRow(int rowNumber, BbBitRow row, BbBarcodeFormat enabledFormats)
    {
        // Read once, so every candidate start is tried against the same runs. A start character,
        // three data characters and a stop are 39 runs at the very least.
        var runs = _runs;
        if ((enabledFormats & BbBarcodeFormat.Codabar) == 0 || !BbLinearPatterns.ReadRuns(row, runs, _runStarts) || runs.Count < 39)
        {
            return null;
        }


        // Runs alternate colour, so the bars are every other run from the first one.
        var firstBar = row[_runStarts[0]] ? 0 : 1;
        for (var i = firstBar; i + 7 <= runs.Count; i += 2)
        {
            // A start character with a quiet zone before it, of at least half a character, inside
            // the row: a symbol cut off by the edge of the image cannot be trusted.
            if (i == 0)
            {
                continue;
            }

            // The four start/stop patterns are tried on their own first: runs that fit none of
            // them well enough cannot classify as one against all twenty either, and nearly every
            // bar on a line through anything else is turned away here for a fifth of the work.
            var characterWidth = Width(i, 7);
            if (runs[i - 1] * 2 < characterWidth || !CouldBeStartStop(i) || Classify(i, out var start) < FirstStartStop)
            {
                continue;
            }

            var result = TryDecodeFrom(rowNumber, row, i, start);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    private BbSymbolDecodeResult? TryDecodeFrom(int rowNumber, BbBitRow row, int startRun, (float Narrow, float Wide) startFit)
    {
        var runs = _runs;
        var characters = _characters;
        characters.Clear();
        characters.Add((Classify(startRun, out _), startFit.Narrow, startFit.Wide));

        var run = startRun + 7;
        while (true)
        {
            // The gap between characters is a narrow space; a wide one is the end of the symbol,
            // or of a fragment that only looked like one.
            if (run + 8 > runs.Count || runs[run] > startFit.Wide * 1.5f)
            {
                return null;
            }

            var value = Classify(run + 1, out var fit);
            if (value < 0)
            {
                return null;
            }

            characters.Add((value, fit.Narrow, fit.Wide));
            run += 8;

            if (value >= FirstStartStop)
            {
                break;
            }

            if (characters.Count > 64)
            {
                return null;
            }
        }

        var dataCount = characters.Count - 2;
        if (dataCount < MinDataCharacters)
        {
            return null;
        }

        // A quiet zone after the stop character, as before the start.
        var stopWidth = Width(run - 7, 7);
        if (run < runs.Count && runs[run] * 2 < stopWidth)
        {
            return null;
        }

        if (run >= runs.Count && _runStarts[run - 1] + runs[run - 1] >= row.Size)
        {
            // The stop character ends at the very edge of the row: no quiet zone at all.
            return null;
        }

        if (!WidthsAgree())
        {
            return null;
        }

        var text = _builder;
        text.Clear();
        var from = _keepStartStop ? 0 : 1;
        var to = _keepStartStop ? characters.Count : characters.Count - 1;
        for (var i = from; i < to; i++)
        {
            text.Append(Alphabet[characters[i].Value]);
        }

        var decoded = text.ToString();
        var payload = new BbDecoderResult(Encoding.ASCII.GetBytes(decoded), decoded);
        var left = _runStarts[startRun];
        var right = _runStarts[run - 1] + runs[run - 1];
        return new BbSymbolDecodeResult(
            payload,
            BbBarcodeFormat.Codabar,
            [new BbScanPoint(left, rowNumber), new BbScanPoint(right, rowNumber)]);
    }

    /// <summary>
    /// Every character's narrow and wide widths within half again of the symbol's median: one
    /// print, one module size. A character far off it is something else the line crossed.
    /// </summary>
    private bool WidthsAgree()
    {
        var characters = _characters;
        Span<float> narrow = stackalloc float[characters.Count];
        Span<float> wide = stackalloc float[characters.Count];
        for (var i = 0; i < characters.Count; i++)
        {
            narrow[i] = characters[i].Narrow;
            wide[i] = characters[i].Wide;
        }

        narrow.Sort();
        wide.Sort();
        var medianNarrow = narrow[narrow.Length / 2];
        var medianWide = wide[wide.Length / 2];
        foreach (var character in characters)
        {
            if (character.Narrow > medianNarrow * 1.5f || character.Narrow * 1.5f < medianNarrow ||
                character.Wide > medianWide * 1.5f || character.Wide * 1.5f < medianWide)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Matches the seven runs from <paramref name="run"/> against every character: for each, the
    /// narrow and wide widths are the means of the runs it calls narrow and wide, and the
    /// character whose pattern those two widths reproduce most closely wins - provided it does so
    /// clearly, with a wide to narrow ratio a printer could have meant, and clearly better than
    /// the next best.
    /// </summary>
    /// <returns>The character's index in <see cref="Alphabet"/>, or -1.</returns>
    private int Classify(int run, out (float Narrow, float Wide) fit)
    {
        fit = default;
        var total = Width(run, 7);
        if (total < 9)
        {
            return -1;
        }

        var best = -1;
        var bestError = float.MaxValue;
        var secondError = float.MaxValue;
        for (var c = 0; c < Patterns.Length; c++)
        {
            var error = FitError(run, Patterns[c], out var narrow, out var wide);
            if (error < bestError)
            {
                secondError = bestError;
                bestError = error;
                best = c;
                fit = (narrow, wide);
            }
            else if (error < secondError)
            {
                secondError = error;
            }
        }

        // Unambiguous: a clean fit, and the runner up clearly further off.
        return best >= 0 && bestError < MaxFitError && secondError > bestError * 1.5f ? best : -1;
    }

    /// <summary>Whether the seven runs from <paramref name="run"/> fit a start/stop pattern closely enough to be one.</summary>
    private bool CouldBeStartStop(int run)
    {
        for (var c = FirstStartStop; c < Patterns.Length; c++)
        {
            if (FitError(run, Patterns[c], out _, out _) < MaxFitError)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// How far the seven runs from <paramref name="run"/> are from <paramref name="pattern"/>, taking the narrow and
    /// wide widths, and how much wider than they are drawn the bars come out, as the best fit to the runs; or
    /// <see cref="float.MaxValue"/> when those widths cannot be a Codabar character's.
    /// </summary>
    /// <remarks>
    /// Ink spread, a thermal printer's dot gain and the exposure of a photograph all draw every bar the same amount
    /// wider - or narrower - and every space the same amount narrower. Fitted as one narrow and one wide width, that
    /// bias reads as misfit, and at a two to one ratio a pixel of it was enough to lose the symbol; fitted as what it
    /// is, it costs nothing. A uniform bias cannot turn one character into another, so this loses no protection.
    /// </remarks>
    private float FitError(int run, int[] pattern, out float narrow, out float wide)
    {
        var runs = _runs;

        // Least squares for run = narrow or wide, plus the bias on a bar and minus it on a space: the normal equations
        // of three unknowns. Bars are the even elements, a character starting on one.
        float nn = 0, ww = 0, nb = 0, wb = 0, bb = 0, nr = 0, wr = 0, br = 0;
        for (var e = 0; e < 7; e++)
        {
            float isNarrow = pattern[e] == 1 ? 1 : 0;
            var isWide = 1 - isNarrow;
            float sign = (e & 1) == 0 ? 1 : -1;
            float r = runs[run + e];
            nn += isNarrow;
            ww += isWide;
            nb += isNarrow * sign;
            wb += isWide * sign;
            bb += 1;
            nr += isNarrow * r;
            wr += isWide * r;
            br += sign * r;
        }

        // | nn  0  nb | |narrow|   |nr|
        // |  0 ww  wb | | wide | = |wr|
        // | nb wb  bb | | bias |   |br|
        var determinant = (nn * ((ww * bb) - (wb * wb))) - (nb * (ww * nb));
        float bias;
        if (MathF.Abs(determinant) < 1e-3f)
        {
            // A pattern whose bars and spaces cannot be told apart from its narrow and wide elements: no bias.
            bias = 0;
            narrow = nr / nn;
            wide = wr / ww;
        }
        else
        {
            narrow = Solve(0);
            wide = Solve(1);
            bias = Solve(2);
        }

        // More bias than half the narrow width would leave no narrow space at all.
        if (MathF.Abs(bias) > narrow * 0.5f)
        {
            return float.MaxValue;
        }

        // The specification allows wide elements of two to three times the narrow; blur
        // squeezes that, but a ratio near one is no two-width code at all.
        if (wide < narrow * 1.6f || wide > narrow * 4.5f)
        {
            return float.MaxValue;
        }

        // How far the runs are from the pattern, in units of the difference between wide and
        // narrow: the scale on which telling one character from another actually happens, and
        // one that does not punish a small print for its whole pixels.
        var squares = 0f;
        for (var e = 0; e < 7; e++)
        {
            var expected = (pattern[e] == 1 ? narrow : wide) + ((e & 1) == 0 ? bias : -bias);
            var difference = runs[run + e] - expected;
            squares += difference * difference;
        }

        return MathF.Sqrt(squares / 7) / (wide - narrow);

        // Cramer's rule on the system above, for unknown number column.
        float Solve(int column)
        {
            float a00 = nn, a01 = 0, a02 = nb, a10 = 0, a11 = ww, a12 = wb, a20 = nb, a21 = wb, a22 = bb;
            switch (column)
            {
                case 0: a00 = nr; a10 = wr; a20 = br; break;
                case 1: a01 = nr; a11 = wr; a21 = br; break;
                default: a02 = nr; a12 = wr; a22 = br; break;
            }

            return ((a00 * ((a11 * a22) - (a12 * a21))) - (a01 * ((a10 * a22) - (a12 * a20))) + (a02 * ((a10 * a21) - (a11 * a20)))) / determinant;
        }
    }

    private int Width(int run, int count)
    {
        var width = 0;
        for (var i = run; i < run + count; i++)
        {
            width += _runs[i];
        }

        return width;
    }
}
