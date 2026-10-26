using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Bit.BbScanner;

/// <summary>
/// Adaptive, block local binariser used for matrix symbologies.
/// </summary>
/// <remarks>
/// <para>
/// The image is divided into 8 by 8 pixel blocks. Each block gets its own threshold, taken as
/// the mean luminance of the 5 by 5 neighbourhood of blocks around it. This tracks the gradual
/// illumination gradients that dominate phone camera captures, which a single global threshold
/// cannot follow.
/// </para>
/// <para>
/// Blocks whose own dynamic range is small carry no edge, so they are almost certainly all
/// paper or all ink. Those fall back to a value derived from the neighbourhood minimum, which
/// stops flat paper from being shredded into noise.
/// </para>
/// <para>
/// What counts as "small" is measured from the frame rather than fixed. Sensor noise raises the
/// range of every blank block, and a blank block that is thresholded at its own mean comes out
/// half black: one noisy frame then produces a field of speckle that no detector can work with.
/// Estimating the noise floor from the quietest blocks and requiring a real edge to stand clear
/// of it keeps a noisy frame as readable as a clean one, without giving up the faint edges of a
/// blurred or low contrast symbol, which a fixed higher threshold would discard.
/// </para>
/// <para>
/// The scratch arrays are cached on the instance and only regrown when the frame size changes,
/// so continuous scanning does not allocate here.
/// </para>
/// </remarks>
public sealed class BbHybridBinarizer : IBbBinarizer
{
    private const int BlockSizePower = 3;
    private const int BlockSize = 1 << BlockSizePower;
    private const int BlockSizeMask = BlockSize - 1;

    /// <summary>Smallest luminance span that can hold an edge, however clean the frame is.</summary>
    private const int MinDynamicRange = 24;

    /// <summary>Largest the noise derived threshold is allowed to become.</summary>
    private const int MaxDynamicRange = 96;

    /// <summary>Fraction of blocks, in sixteenths, taken to be blank when estimating the noise floor.</summary>
    /// <remarks>
    /// Deliberately small. A quarter of the blocks would be a fair estimate only while the frame
    /// is mostly background; once a symbol fills a good part of the view, that percentile is
    /// measuring the symbol's own edges, and the threshold derived from it then classifies those
    /// edges as blank and erases the symbol. The quietest sixteenth is background in either case,
    /// because every conformant symbol carries a quiet zone and the frame around it.
    /// </remarks>
    private const int QuietBlockFraction = 1;

    private readonly BbGlobalHistogramBinarizer _rowFallback = new();

    private int[] _blockAverages = [];
    private int[] _blockMinimums = [];
    private int[] _blockRanges = [];
    private int[] _edgeSums = [];
    private int[] _edgeCounts = [];
    private int[] _allSums = [];
    private int[] _thresholds = [];
    private Vector128<byte>[] _limits = [];
    private readonly int[] _rangeHistogram = new int[256];
    private int _blockColumns;
    private int _blockRows;

    /// <summary>
    /// Zeroes the block statistics of the last image - eight by eight pixel means, minimums and ranges, which together
    /// are a small picture of it.
    /// </summary>
    internal void ForgetImage()
    {
        Array.Clear(_blockAverages);
        Array.Clear(_blockMinimums);
        Array.Clear(_blockRanges);
        Array.Clear(_edgeSums);
        Array.Clear(_edgeCounts);
        Array.Clear(_allSums);
        Array.Clear(_thresholds);
        Array.Clear(_limits);
        Array.Clear(_rangeHistogram);
    }

    /// <inheritdoc />
    public bool TryGetBlackRow(in BbLuminanceView source, int y, BbBitRow row) =>
        _rowFallback.TryGetBlackRow(source, y, row);

    /// <inheritdoc />
    public BbBitMatrix? GetBlackMatrix(in BbLuminanceView source)
    {
        var width = source.Width;
        var height = source.Height;

        // Very small images have too few blocks for the neighbourhood average to mean anything.
        if (width < BlockSize * 5 || height < BlockSize * 5)
        {
            return _rowFallback.GetBlackMatrix(source);
        }

        var columns = (width + BlockSizeMask) >> BlockSizePower;
        var rows = (height + BlockSizeMask) >> BlockSizePower;

        EnsureCapacity(columns, rows);
        MeasureBlocks(source, columns, rows);
        AdjustUniformBlocks(columns, rows, EstimateMinimumRange(columns * rows));

        var matrix = new BbBitMatrix(width, height);
        ApplyThresholds(source, matrix, columns, rows);
        return matrix;
    }

    private void EnsureCapacity(int columns, int rows)
    {
        var required = columns * rows;
        if (_blockAverages.Length < required)
        {
            _blockAverages = new int[required];
            _blockMinimums = new int[required];
            _blockRanges = new int[required];
            _edgeSums = new int[required];
            _edgeCounts = new int[required];
            _allSums = new int[required];
            _thresholds = new int[required];
        }

        _blockColumns = columns;
        _blockRows = rows;
    }

    /// <summary>First pass: the mean, minimum and luminance span of every block.</summary>
    /// <remarks>
    /// Two blocks side by side are sixteen pixels a row, one vector: eight rows of minimum, maximum and widened sum take
    /// a dozen vector operations where the pixel loop takes hundreds. That is a few times faster compiled, and the
    /// difference between unusable and usable on the WebAssembly interpreter, which runs a vector operation as one
    /// instruction rather than sixteen. A block whose origin is pulled in from the image's edge, and any machine
    /// without vectors, takes the pixel loop; both give the same results.
    /// </remarks>
    private void MeasureBlocks(in BbLuminanceView source, int columns, int rows)
    {
        var width = source.Width;
        var height = source.Height;
        var averages = _blockAverages;
        var minimums = _blockMinimums;
        var ranges = _blockRanges;
        var paired = Vector128.IsHardwareAccelerated ? (width >> BlockSizePower) & ~1 : 0;
        Span<byte> lanes = stackalloc byte[16];

        for (var by = 0; by < rows; by++)
        {
            var yOffset = ClampBlockOrigin(by, rows, height);
            var bx = 0;
            for (; bx < paired; bx += 2)
            {
                var x = bx << BlockSizePower;
                var low = Vector128<byte>.AllBitsSet;
                var high = Vector128<byte>.Zero;
                var leftSum = Vector128<ushort>.Zero;
                var rightSum = Vector128<ushort>.Zero;
                for (var yy = 0; yy < BlockSize; yy++)
                {
                    var pixels = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(source.GetRow(yOffset + yy)), (nuint)x);
                    low = Vector128.Min(low, pixels);
                    high = Vector128.Max(high, pixels);
                    var (left, right) = Vector128.Widen(pixels);
                    leftSum += left;
                    rightSum += right;
                }

                var index = (by * columns) + bx;
                low.CopyTo(lanes);
                var (leftMin, rightMin) = (Lowest(lanes[..8]), Lowest(lanes[8..]));
                high.CopyTo(lanes);
                var (leftMax, rightMax) = (Highest(lanes[..8]), Highest(lanes[8..]));
                averages[index] = Vector128.Sum(leftSum) >> (BlockSizePower * 2);
                minimums[index] = leftMin;
                ranges[index] = leftMax - leftMin;
                averages[index + 1] = Vector128.Sum(rightSum) >> (BlockSizePower * 2);
                minimums[index + 1] = rightMin;
                ranges[index + 1] = rightMax - rightMin;
            }

            for (; bx < columns; bx++)
            {
                var xOffset = ClampBlockOrigin(bx, columns, width);

                var sum = 0;
                var min = 255;
                var max = 0;
                for (var yy = 0; yy < BlockSize; yy++)
                {
                    var pixels = source.GetRow(yOffset + yy).Slice(xOffset, BlockSize);
                    for (var xx = 0; xx < BlockSize; xx++)
                    {
                        int pixel = pixels[xx];
                        sum += pixel;
                        if (pixel < min) min = pixel;
                        if (pixel > max) max = pixel;
                    }
                }

                var index = (by * columns) + bx;
                averages[index] = sum >> (BlockSizePower * 2);
                minimums[index] = min;
                ranges[index] = max - min;
            }
        }
    }

    /// <summary>
    /// Estimates the luminance span below which a block is treated as carrying no edge.
    /// </summary>
    /// <remarks>
    /// Most blocks of a typical frame are blank, so a low percentile of the block spans measures
    /// the sensor noise rather than the content. A clean frame yields nearly zero and the fixed
    /// floor applies; a noisy one raises the bar so that blank paper is still recognised as
    /// blank.
    /// </remarks>
    private int EstimateMinimumRange(int blockCount)
    {
        var histogram = _rangeHistogram;
        Array.Clear(histogram);

        var ranges = _blockRanges;
        for (var i = 0; i < blockCount; i++)
        {
            histogram[Math.Min(255, ranges[i])]++;
        }

        var target = Math.Max(1, blockCount * QuietBlockFraction / 16);
        var seen = 0;
        var noiseFloor = 0;
        for (var i = 0; i < histogram.Length; i++)
        {
            seen += histogram[i];
            if (seen > target)
            {
                noiseFloor = i;
                break;
            }
        }

        return Math.Clamp(noiseFloor * 2, MinDynamicRange, MaxDynamicRange);
    }

    /// <summary>
    /// Second pass: replaces the mean of every edgeless block with a value that will threshold
    /// the pixels around it correctly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A block with no edge in it is either all paper or all ink, and the two need opposite
    /// treatment. Paper has to be biased downwards, or a flat area thresholded against its own
    /// mean comes out half black. Ink must not be, or a module wider than a block drags the
    /// local threshold below the ink itself and the symbol disappears.
    /// </para>
    /// <para>
    /// The two are told apart by the blocks nearby that do contain an edge. Such a block spans
    /// ink and paper, so its mean sits between them and is a local, illumination-tracking
    /// midpoint: an edgeless block below it is ink, one above it is paper. Where no edge is in
    /// range there is no symbol nearby either, and the paper bias applies unconditionally, which
    /// is what keeps a blank frame, however unevenly lit, from being shredded.
    /// </para>
    /// </remarks>
    private void AdjustUniformBlocks(int columns, int rows, int minimumRange)
    {
        var averages = _blockAverages;
        var minimums = _blockMinimums;
        var ranges = _blockRanges;
        var sums = _edgeSums;
        var counts = _edgeCounts;
        var allSums = _allSums;

        // Summed area tables over the blocks that do contain an edge, so the neighbourhood
        // midpoint below costs a constant number of lookups however wide the window is.
        for (var by = 0; by < rows; by++)
        {
            var rowSum = 0;
            var rowCount = 0;
            var rowAll = 0;
            for (var bx = 0; bx < columns; bx++)
            {
                var index = (by * columns) + bx;
                if (ranges[index] > minimumRange)
                {
                    rowSum += averages[index];
                    rowCount++;
                }

                rowAll += averages[index];

                var above = by > 0 ? (by - 1) * columns + bx : -1;
                sums[index] = rowSum + (above >= 0 ? sums[above] : 0);
                counts[index] = rowCount + (above >= 0 ? counts[above] : 0);
                allSums[index] = rowAll + (above >= 0 ? allSums[above] : 0);
            }
        }

        const int Reach = 2;
        for (var by = 0; by < rows; by++)
        {
            for (var bx = 0; bx < columns; bx++)
            {
                var index = (by * columns) + bx;
                if (ranges[index] > minimumRange)
                {
                    continue;
                }

                var min = minimums[index];
                var average = min / 2;

                var top = Math.Max(0, by - Reach);
                var left = Math.Max(0, bx - Reach);
                var bottom = Math.Min(rows - 1, by + Reach);
                var right = Math.Min(columns - 1, bx + Reach);

                var count = Area(counts, columns, top, left, bottom, right);
                if (count > 0)
                {
                    var midpoint = Area(sums, columns, top, left, bottom, right) / count;
                    if (averages[index] < midpoint)
                    {
                        // Inside ink: contribute the local midpoint so that the threshold around
                        // this block stays above the ink rather than sinking below it.
                        average = midpoint;
                    }
                }
                else
                {
                    // No edge anywhere in range. That is usually blank paper, but it is also what
                    // a symbol whose modules happen to line up with the block grid looks like, so
                    // a block clearly darker than everything around it is still treated as ink.
                    var area = (bottom - top + 1) * (right - left + 1);
                    var neighbourhood = Area(allSums, columns, top, left, bottom, right) / area;
                    if (averages[index] < neighbourhood - (minimumRange / 2))
                    {
                        average = neighbourhood;
                    }
                }

                averages[index] = average;
            }
        }
    }

    /// <summary>Sum over an inclusive block rectangle, from a summed area table.</summary>
    private static int Area(int[] table, int columns, int top, int left, int bottom, int right)
    {
        var total = table[(bottom * columns) + right];
        if (top > 0)
        {
            total -= table[((top - 1) * columns) + right];
        }

        if (left > 0)
        {
            total -= table[(bottom * columns) + left - 1];
        }

        if (top > 0 && left > 0)
        {
            total += table[((top - 1) * columns) + left - 1];
        }

        return total;
    }

    /// <summary>
    /// Third pass: every pixel against its block's threshold, the mean of the 5 by 5 blocks around it. As in
    /// <see cref="MeasureBlocks"/>, two blocks are compared at once where vectors are available, the sixteen results
    /// going into the bit row as one mask; blocks overlap only at the clamped edges, where bits are OR'd together
    /// exactly as the pixel loop does.
    /// </summary>
    private void ApplyThresholds(in BbLuminanceView source, BbBitMatrix matrix, int columns, int rows)
    {
        var width = source.Width;
        var height = source.Height;
        var averages = _blockAverages;
        var thresholds = _thresholds;

        for (var by = 0; by < rows; by++)
        {
            var top = Clamp(by, rows - 3);
            for (var bx = 0; bx < columns; bx++)
            {
                var left = Clamp(bx, columns - 3);
                var sum = 0;
                for (var dy = -2; dy <= 2; dy++)
                {
                    var rowStart = (top + dy) * columns;
                    sum += averages[rowStart + left - 2] +
                           averages[rowStart + left - 1] +
                           averages[rowStart + left] +
                           averages[rowStart + left + 1] +
                           averages[rowStart + left + 2];
                }

                thresholds[(by * columns) + bx] = sum / 25;
            }
        }

        var paired = Vector128.IsHardwareAccelerated ? (width >> BlockSizePower) & ~1 : 0;
        if (_limits.Length < paired / 2)
        {
            _limits = new Vector128<byte>[paired / 2];
        }

        var limits = _limits;
        for (var by = 0; by < rows; by++)
        {
            var yOffset = ClampBlockOrigin(by, rows, height);
            var blockThresholds = thresholds.AsSpan(by * columns, columns);
            for (var pair = 0; pair < paired / 2; pair++)
            {
                limits[pair] = Vector128.Create(
                    Vector64.Create((byte)blockThresholds[pair * 2]),
                    Vector64.Create((byte)blockThresholds[(pair * 2) + 1]));
            }

            for (var yy = 0; yy < BlockSize; yy++)
            {
                var y = yOffset + yy;
                var pixels = source.GetRow(y);
                var target = matrix.GetRowWords(y);
                var bx = 0;
                for (; bx < paired; bx += 2)
                {
                    // Sixteen pixels from a multiple of sixteen: the mask fills the low or the high half of a word.
                    var x = bx << BlockSizePower;
                    var row = Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(pixels), (nuint)x);
                    var dark = Vector128.LessThanOrEqual(row, limits[bx >> 1]).ExtractMostSignificantBits();
                    if (dark != 0)
                    {
                        target[x >> 5] |= dark << (x & 31);
                    }
                }

                for (; bx < columns; bx++)
                {
                    var xOffset = ClampBlockOrigin(bx, columns, width);
                    var threshold = blockThresholds[bx];
                    for (var xx = 0; xx < BlockSize; xx++)
                    {
                        var x = xOffset + xx;
                        if (pixels[x] <= threshold)
                        {
                            target[x >> 5] |= 1u << (x & 31);
                        }
                    }
                }
            }
        }
    }

    private static int Lowest(ReadOnlySpan<byte> lanes)
    {
        var lowest = 255;
        foreach (var lane in lanes)
        {
            if (lane < lowest)
            {
                lowest = lane;
            }
        }

        return lowest;
    }

    private static int Highest(ReadOnlySpan<byte> lanes)
    {
        var highest = 0;
        foreach (var lane in lanes)
        {
            if (lane > highest)
            {
                highest = lane;
            }
        }

        return highest;
    }

    private static int ClampBlockOrigin(int blockIndex, int blockCount, int size)
    {
        var offset = blockIndex << BlockSizePower;
        var limit = size - BlockSize;
        return offset > limit ? limit : offset;
    }

    private static int Clamp(int value, int max) => value < 2 ? 2 : (value > max ? max : value);
}
