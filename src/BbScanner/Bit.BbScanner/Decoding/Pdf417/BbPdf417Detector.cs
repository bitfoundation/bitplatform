namespace Bit.BbScanner;

/// <summary>
/// Locates a PDF417 symbol and reads its codewords row by row.
/// </summary>
/// <remarks>
/// <para>
/// PDF417 is a stacked linear symbology: every row is a self-contained sequence of seventeen
/// module characters bracketed by a start and a stop pattern. That makes detection a row-wise
/// search for those two patterns rather than a two dimensional finder search, and it makes
/// sampling exact: a codeword is eight consecutive runs, whatever the module width happens to be
/// at that point in the row.
/// </para>
/// <para>
/// Every image row that yields a codeword sequence is assigned to a symbol row by its row
/// indicator, which carries the row number: comparing neighbouring image rows for equality would
/// merge two different symbol rows that happen to carry the same codewords, and would split one
/// symbol row in two at the first scan line a speck of dust corrupts. The scan lines of a symbol
/// row then vote on every codeword, and a codeword no scan line could read is reported as an
/// erasure for the error correction stage rather than discarding the row.
/// </para>
/// </remarks>
public sealed class BbPdf417Detector
{
    /// <summary>The start pattern: a wide bar followed by six narrow elements and a three module space.</summary>
    private static readonly int[] StartPattern = [8, 1, 1, 1, 1, 1, 1, 3];

    /// <summary>The stop pattern, which is eighteen modules rather than seventeen.</summary>
    private static readonly int[] StopPattern = [7, 1, 1, 3, 1, 1, 1, 2, 1];

    private const float MaxAvgVariance = 0.42f;
    private const float MaxIndividualVariance = 0.8f;

    /// <summary>Marks a codeword position no scan line could read.</summary>
    public const int Erasure = -1;

    /// <summary>One decoded symbol row.</summary>
    /// <param name="RowNumber">The zero based row number carried by the row indicators.</param>
    /// <param name="Codewords">Every codeword of the row, including both row indicators; <see cref="Erasure"/> where unreadable.</param>
    /// <param name="Clusters">The cluster number each codeword belongs to, which is fixed by the row number.</param>
    /// <param name="Top">First image row this symbol row was seen on.</param>
    /// <param name="Bottom">Last image row this symbol row was seen on.</param>
    /// <param name="Left">X of the first module of the start pattern.</param>
    /// <param name="Right">X just past the last module of the stop pattern.</param>
    public sealed record SymbolRow(int RowNumber, int[] Codewords, int[] Clusters, int Top, int Bottom, int Left, int Right);

    /// <summary>The scan lines gathered for one symbol row before they vote.</summary>
    private sealed class RowAccumulator(int top, int left, int right)
    {
        public List<int[]> ScanLines { get; } = [];

        public int Top { get; } = top;

        public int Bottom { get; set; } = top;

        public int Left { get; set; } = left;

        public int Right { get; set; } = right;
    }

    /// <summary>
    /// Scans the image and returns the symbol rows it found, in row number order.
    /// </summary>
    /// <param name="image">The binarised image.</param>
    /// <param name="table">The symbol character table to decode with.</param>
    /// <returns>The rows, or an empty list when no symbol was found.</returns>
    public IReadOnlyList<SymbolRow> Detect(BbBitMatrix image, BbPdf417SymbolTable table)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(table);

        var accumulators = new SortedDictionary<int, RowAccumulator>();

        Span<int> counters = stackalloc int[9];

        for (var y = 0; y < image.Height; y++)
        {
            if (!TryFindPattern(image, y, 0, StartPattern, counters, out var startBegin, out var startEnd))
            {
                continue;
            }

            if (!TryFindPattern(image, y, startEnd, StopPattern, counters, out var stopBegin, out var stopEnd))
            {
                continue;
            }

            var codewords = TryReadRow(image, y, startEnd, stopBegin, table, out var clusters);
            if (codewords is null)
            {
                continue;
            }

            var rowNumber = RowNumberOf(codewords, clusters!);
            if (rowNumber < 0)
            {
                continue;
            }

            if (!accumulators.TryGetValue(rowNumber, out var accumulator))
            {
                accumulator = new RowAccumulator(y, startBegin, stopEnd);
                accumulators.Add(rowNumber, accumulator);
            }

            accumulator.ScanLines.Add(codewords);
            accumulator.Bottom = y;
            accumulator.Left = Math.Min(accumulator.Left, startBegin);
            accumulator.Right = Math.Max(accumulator.Right, stopEnd);
        }

        if (accumulators.Count == 0)
        {
            return [];
        }

        // A row indicator misread as another valid codeword lands a scan line on a row number
        // the symbol does not have. Such a row is seen on far fewer scan lines than a real one,
        // whose height is several pixels, so rows well below the typical count are dropped.
        var counts = accumulators.Values.Select(a => a.ScanLines.Count).Order().ToArray();
        var median = counts[counts.Length / 2];

        var rows = new List<SymbolRow>(accumulators.Count);
        foreach (var (rowNumber, accumulator) in accumulators)
        {
            if (accumulator.ScanLines.Count * 2 < median)
            {
                continue;
            }

            var voted = Vote(accumulator.ScanLines);
            var clusters = new int[voted.Length];
            Array.Fill(clusters, (rowNumber % 3) * 3);
            rows.Add(new SymbolRow(rowNumber, voted, clusters, accumulator.Top, accumulator.Bottom, accumulator.Left, accumulator.Right));
        }

        return rows;
    }

    /// <summary>
    /// Derives the row number from whichever row indicator could be read: the indicator's value
    /// divided by thirty gives the row number divided by three, and its cluster gives the row
    /// number modulo three.
    /// </summary>
    private static int RowNumberOf(int[] codewords, int[] clusters)
    {
        var left = codewords[0];
        if (left != Erasure && clusters[0] is 0 or 3 or 6)
        {
            return ((left / 30) * 3) + (clusters[0] / 3);
        }

        var right = codewords[^1];
        if (right != Erasure && clusters[^1] is 0 or 3 or 6)
        {
            return ((right / 30) * 3) + (clusters[^1] / 3);
        }

        return -1;
    }

    /// <summary>
    /// Combines the scan lines of one symbol row: the most common codeword count wins, and
    /// within it every position takes its most frequent readable value.
    /// </summary>
    private static int[] Vote(List<int[]> scanLines)
    {
        var length = scanLines
            .GroupBy(line => line.Length)
            .OrderByDescending(group => group.Count())
            .ThenByDescending(group => group.Key)
            .First()
            .Key;

        var voted = new int[length];
        var tally = new Dictionary<int, int>();
        for (var position = 0; position < length; position++)
        {
            tally.Clear();
            foreach (var line in scanLines)
            {
                if (line.Length != length || line[position] == Erasure)
                {
                    continue;
                }

                tally[line[position]] = tally.GetValueOrDefault(line[position]) + 1;
            }

            var best = Erasure;
            var bestCount = 0;
            foreach (var (value, count) in tally)
            {
                if (count > bestCount)
                {
                    best = value;
                    bestCount = count;
                }
            }

            voted[position] = best;
        }

        return voted;
    }

    /// <summary>
    /// Reads the codewords between the start and stop patterns of one image row. A character
    /// whose pattern is not in the table is recorded as <see cref="Erasure"/>; only a row whose
    /// run structure is broken is rejected outright.
    /// </summary>
    private static int[]? TryReadRow(
        BbBitMatrix image, int y, int from, int to, BbPdf417SymbolTable table, out int[]? clusters)
    {
        clusters = null;

        var codewords = new List<int>(16);
        var clusterList = new List<int>(16);

        Span<int> counters = stackalloc int[8];
        Span<int> widths = stackalloc int[8];

        var x = from;
        while (x < to)
        {
            if (!TryRecordRuns(image, y, ref x, counters, to))
            {
                return null;
            }

            if (!TryNormalize(counters, widths))
            {
                return null;
            }

            var cluster = BbPdf417SymbolTable.ClusterOf(widths);
            var pattern = BbPdf417SymbolTable.ToPattern(widths);
            if (!table.TryGetCodeword(cluster, pattern, out var codeword))
            {
                codeword = Erasure;
                cluster = -1;
            }

            codewords.Add(codeword);
            clusterList.Add(cluster);

            // A row can hold at most 30 columns plus two indicators.
            if (codewords.Count > 34)
            {
                return null;
            }
        }

        // A usable row has a left indicator, at least one data column and a right indicator.
        if (codewords.Count < 3)
        {
            return null;
        }

        clusters = [.. clusterList];
        return [.. codewords];
    }

    /// <summary>Measures eight alternating runs starting at a bar.</summary>
    private static bool TryRecordRuns(BbBitMatrix image, int y, ref int x, Span<int> counters, int limit)
    {
        counters.Clear();

        if (x >= limit || !image[x, y])
        {
            return false;
        }

        var position = 0;
        var isBlack = true;

        while (x < limit)
        {
            var pixel = image[x, y];
            if (pixel == isBlack)
            {
                counters[position]++;
                x++;
                continue;
            }

            if (++position == 8)
            {
                return true;
            }

            isBlack = !isBlack;
        }

        // The last run may end exactly at the stop pattern.
        return position == 7 && counters[7] > 0;
    }

    /// <summary>
    /// Converts measured pixel widths into module widths that sum to seventeen.
    /// </summary>
    /// <remarks>
    /// Rounding each element independently rarely sums to seventeen, so the remainder is
    /// distributed to the elements whose rounding error was largest. That recovers the correct
    /// pattern from a row whose module width is not an exact number of pixels.
    /// </remarks>
    private static bool TryNormalize(ReadOnlySpan<int> counters, Span<int> widths)
    {
        var total = 0;
        foreach (var counter in counters)
        {
            if (counter <= 0)
            {
                return false;
            }

            total += counter;
        }

        if (total < BbPdf417SymbolTable.ModulesPerCodeword)
        {
            return false;
        }

        var moduleWidth = (float)total / BbPdf417SymbolTable.ModulesPerCodeword;

        Span<float> errors = stackalloc float[8];
        var sum = 0;
        for (var i = 0; i < 8; i++)
        {
            var exact = counters[i] / moduleWidth;
            var rounded = Math.Clamp((int)MathF.Round(exact), 1, 6);
            widths[i] = rounded;
            errors[i] = exact - rounded;
            sum += rounded;
        }

        // Hand the remaining modules to the elements that were rounded down the most, and take
        // them back from the ones rounded up the most.
        while (sum != BbPdf417SymbolTable.ModulesPerCodeword)
        {
            var grow = sum < BbPdf417SymbolTable.ModulesPerCodeword;
            var bestIndex = -1;
            var bestError = grow ? float.NegativeInfinity : float.PositiveInfinity;

            for (var i = 0; i < 8; i++)
            {
                if (grow && widths[i] >= 6)
                {
                    continue;
                }

                if (!grow && widths[i] <= 1)
                {
                    continue;
                }

                if (grow ? errors[i] > bestError : errors[i] < bestError)
                {
                    bestError = errors[i];
                    bestIndex = i;
                }
            }

            if (bestIndex < 0)
            {
                return false;
            }

            widths[bestIndex] += grow ? 1 : -1;
            errors[bestIndex] += grow ? -1 : 1;
            sum += grow ? 1 : -1;
        }

        return true;
    }

    /// <summary>Finds a fixed pattern in one row of the image, starting at a bar.</summary>
    private static bool TryFindPattern(
        BbBitMatrix image, int y, int fromX, ReadOnlySpan<int> pattern, Span<int> counters, out int start, out int end)
    {
        start = 0;
        end = 0;

        var patternLength = pattern.Length;
        counters[..patternLength].Clear();

        var width = image.Width;
        var x = fromX;
        while (x < width && !image[x, y])
        {
            x++;
        }

        if (x >= width)
        {
            return false;
        }

        var patternStart = x;
        var counterPosition = 0;
        var isBlack = true;

        for (; x < width; x++)
        {
            if (image[x, y] == isBlack)
            {
                counters[counterPosition]++;
                continue;
            }

            if (counterPosition == patternLength - 1)
            {
                if (BbLinearPatterns.PatternMatchVariance(counters[..patternLength], pattern, MaxIndividualVariance) < MaxAvgVariance)
                {
                    start = patternStart;
                    end = x;
                    return true;
                }

                patternStart += counters[0] + counters[1];
                counters[2..patternLength].CopyTo(counters[..(patternLength - 2)]);
                counters[patternLength - 2] = 0;
                counters[patternLength - 1] = 0;
                counterPosition--;
            }
            else
            {
                counterPosition++;
            }

            counters[counterPosition] = 1;
            isBlack = !isBlack;
        }

        // The stop pattern ends with a bar, so a symbol flush against the right edge of the image
        // produces no closing transition. Evaluate the window the loop was still accumulating.
        if (counterPosition == patternLength - 1 &&
            BbLinearPatterns.PatternMatchVariance(counters[..patternLength], pattern, MaxIndividualVariance) < MaxAvgVariance)
        {
            start = patternStart;
            end = width;
            return true;
        }

        return false;
    }
}
