namespace Bit.BbScanner;

/// <summary>
/// Binarises scan lines with a threshold that follows the lighting along the row.
/// </summary>
/// <remarks>
/// <para>
/// A single black point per row, as <see cref="BbGlobalHistogramBinarizer"/> picks, fails as soon
/// as the row crosses a shadow or a highlight: the paper on the dark side ends up below the
/// threshold and the bars merge. Phone cameras produce exactly that kind of frame, with the
/// phone's own shadow across the label.
/// </para>
/// <para>
/// This binariser splits the row into 16 pixel segments, records the darkest and brightest
/// sample of each, and thresholds every pixel halfway between the extremes of the five
/// segments around it. Segments whose neighbourhood has no contrast are treated as background,
/// which keeps sensor noise on blank paper from turning into phantom bars. It costs about the
/// same as the histogram approach and needs no per-row allocation.
/// </para>
/// </remarks>
public sealed class BbAdaptiveRowBinarizer : IBbBinarizer
{
    private const int SegmentShift = 4;
    private const int SegmentSize = 1 << SegmentShift;
    private const int Reach = 2;
    /// <summary>
    /// Contrast below which a neighbourhood is treated as blank. Sensor noise on white paper
    /// spans a few tens of levels, and clips at 255, so this sits well above it.
    /// </summary>
    private const int MinDynamicRange = 40;

    private readonly BbGlobalHistogramBinarizer _matrixFallback = new();
    private int[] _minimums = [];
    private int[] _maximums = [];

    /// <inheritdoc />
    public BbBitMatrix? GetBlackMatrix(in BbLuminanceView source) => _matrixFallback.GetBlackMatrix(source);

    /// <inheritdoc />
    public bool TryGetBlackRow(in BbLuminanceView source, int y, BbBitRow row) => TryGetBlackRow(source, y, row, inverted: false);

    /// <summary>
    /// Binarises a single row, optionally treating light pixels as ink for a light-on-dark
    /// symbol. Inverting the bits afterwards would not do: blank neighbourhoods must read as
    /// background either way, and inverting turns them into ink.
    /// </summary>
    /// <param name="source">Grayscale source.</param>
    /// <param name="y">Row index inside the view.</param>
    /// <param name="row">Row to fill; it is reset by the callee.</param>
    /// <param name="inverted">When <see langword="true"/>, pixels above the threshold are set.</param>
    public bool TryGetBlackRow(in BbLuminanceView source, int y, BbBitRow row, bool inverted)
    {
        var width = source.Width;
        row.Reset(width);
        if (width < 3)
        {
            return false;
        }

        var pixels = source.GetRow(y);
        var segments = (width + SegmentSize - 1) >> SegmentShift;
        if (_minimums.Length < segments)
        {
            _minimums = new int[segments];
            _maximums = new int[segments];
        }

        var minimums = _minimums;
        var maximums = _maximums;

        var anyContrast = false;
        for (var s = 0; s < segments; s++)
        {
            var start = s << SegmentShift;
            var end = Math.Min(width, start + SegmentSize);
            var min = 255;
            var max = 0;
            for (var x = start; x < end; x++)
            {
                int v = pixels[x];
                if (v < min) min = v;
                if (v > max) max = v;
            }

            minimums[s] = min;
            maximums[s] = max;
            anyContrast |= max - min >= MinDynamicRange;
        }

        if (!anyContrast)
        {
            return false;
        }

        var left = pixels[0];
        var center = pixels[1];
        var lastSegment = segments - 1;
        for (var s = 0; s < segments; s++)
        {
            var from = Math.Max(0, s - Reach);
            var to = Math.Min(lastSegment, s + Reach);
            var min = 255;
            var max = 0;
            for (var i = from; i <= to; i++)
            {
                if (minimums[i] < min) min = minimums[i];
                if (maximums[i] > max) max = maximums[i];
            }

            var start = Math.Max(1, s << SegmentShift);
            var end = Math.Min(width - 1, (s + 1) << SegmentShift);
            if (max - min < MinDynamicRange)
            {
                // Flat neighbourhood: background, whatever its brightness.
                for (var x = start; x < end; x++)
                {
                    left = center;
                    center = pixels[x + 1];
                }

                continue;
            }

            var threshold = (min + max) >> 1;
            for (var x = start; x < end; x++)
            {
                var right = pixels[x + 1];

                // Compare against the neighbours as well: this removes the single pixel speckle
                // that a hard threshold produces on noisy sensors.
                var dark = (((center * 4) - left - right) / 2) < threshold;
                if (dark != inverted)
                {
                    row[x] = true;
                }

                left = center;
                center = right;
            }
        }

        return true;
    }
}
