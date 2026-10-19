namespace Bit.BbScanner;

/// <summary>
/// Run-length helpers shared by every linear symbology decoder.
/// </summary>
/// <remarks>
/// All linear symbologies are read the same way: walk a scan line, measure the widths of
/// consecutive same-coloured runs, and compare those widths against the symbology character
/// table after normalising for the unknown module width. Keeping that shared machinery here
/// means a new linear symbology only has to contribute its character table.
/// </remarks>
public static class BbLinearPatterns
{
    /// <summary>Rejects a candidate whose average width error exceeds this fraction of a module.</summary>
    public const float MaxAvgVariance = 0.25f;

    /// <summary>Rejects a candidate where any single element is off by more than this fraction of a module.</summary>
    public const float MaxIndividualVariance = 0.7f;

    /// <summary>
    /// Measures <c>counters.Length</c> consecutive runs starting at <paramref name="start"/>.
    /// </summary>
    /// <param name="row">The binarised scan line.</param>
    /// <param name="start">Index to start measuring from.</param>
    /// <param name="counters">Receives the run widths.</param>
    /// <returns><see langword="false"/> when the row ends before every run was measured.</returns>
    public static bool RecordPattern(BbBitRow row, int start, Span<int> counters)
    {
        counters.Clear();

        var end = row.Size;
        if (start >= end)
        {
            return false;
        }

        var isWhite = !row[start];
        var counterPosition = 0;
        var i = start;
        while (i < end)
        {
            if (row[i] != isWhite)
            {
                counters[counterPosition]++;
            }
            else
            {
                if (++counterPosition == counters.Length)
                {
                    break;
                }

                counters[counterPosition] = 1;
                isWhite = !isWhite;
            }

            i++;
        }

        // Either every run was measured, or the last run runs off the end of the row, which is
        // still usable because the caller only needs its minimum width.
        return counterPosition == counters.Length || (counterPosition == counters.Length - 1 && i == end);
    }

    /// <summary>
    /// Scores how closely measured run widths match an expected module width pattern.
    /// </summary>
    /// <param name="counters">Measured run widths in pixels.</param>
    /// <param name="pattern">Expected widths in modules.</param>
    /// <param name="maxIndividualVariance">Per element rejection threshold, in modules.</param>
    /// <returns>The average per element error in modules, or <see cref="float.PositiveInfinity"/> on rejection.</returns>
    public static float PatternMatchVariance(ReadOnlySpan<int> counters, ReadOnlySpan<int> pattern, float maxIndividualVariance)
    {
        var numCounters = counters.Length;
        var total = 0;
        var patternLength = 0;
        for (var i = 0; i < numCounters; i++)
        {
            total += counters[i];
            patternLength += pattern[i];
        }

        // Fewer pixels than modules means the symbol is smaller than one pixel per module and
        // cannot be read; bail out before dividing.
        if (total < patternLength)
        {
            return float.PositiveInfinity;
        }

        var unitBarWidth = (float)total / patternLength;
        maxIndividualVariance *= unitBarWidth;

        var totalVariance = 0.0f;
        for (var i = 0; i < numCounters; i++)
        {
            var counter = counters[i];
            var scaledPattern = pattern[i] * unitBarWidth;
            var variance = counter > scaledPattern ? counter - scaledPattern : scaledPattern - counter;
            if (variance > maxIndividualVariance)
            {
                return float.PositiveInfinity;
            }

            totalVariance += variance;
        }

        return totalVariance / total;
    }

    /// <summary>
    /// Finds the next run of <c>pattern.Length</c> alternating runs that matches
    /// <paramref name="pattern"/>, starting the search at <paramref name="rowOffset"/>.
    /// </summary>
    /// <param name="row">The binarised scan line.</param>
    /// <param name="rowOffset">Index to start searching from.</param>
    /// <param name="whiteFirst">When <see langword="true"/> the first run of the pattern is white.</param>
    /// <param name="pattern">Expected widths in modules.</param>
    /// <param name="counters">Scratch space at least as long as <paramref name="pattern"/>.</param>
    /// <param name="start">Receives the index of the first pixel of the match.</param>
    /// <param name="end">Receives the index just past the last pixel of the match.</param>
    /// <param name="maxAvgVariance">Average width error the match is allowed to carry.</param>
    /// <param name="maxIndividualVariance">Per element width error the match is allowed to carry.</param>
    public static bool FindGuardPattern(
        BbBitRow row,
        int rowOffset,
        bool whiteFirst,
        ReadOnlySpan<int> pattern,
        Span<int> counters,
        out int start,
        out int end,
        float maxAvgVariance = MaxAvgVariance,
        float maxIndividualVariance = MaxIndividualVariance)
    {
        start = 0;
        end = 0;

        var patternLength = pattern.Length;
        counters[..patternLength].Clear();

        var width = row.Size;
        var isWhite = whiteFirst;
        rowOffset = whiteFirst ? row.GetNextUnset(rowOffset) : row.GetNextSet(rowOffset);

        var counterPosition = 0;
        var patternStart = rowOffset;
        for (var x = rowOffset; x < width; x++)
        {
            if (row[x] != isWhite)
            {
                counters[counterPosition]++;
                continue;
            }

            if (counterPosition == patternLength - 1)
            {
                if (PatternMatchVariance(counters[..patternLength], pattern, maxIndividualVariance) < maxAvgVariance)
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
            isWhite = !isWhite;
        }

        return false;
    }
}
