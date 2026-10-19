namespace Bit.BbScanner;

/// <summary>
/// Finds the connected patches of ink in a binarised image, at a coarse cell resolution.
/// </summary>
/// <remarks>
/// <para>
/// Symbologies without a distinctive finder pattern, Data Matrix above all, have to be located
/// before they can be read. Growing a rectangle out of the image centre only works when the
/// user has aimed precisely; this finder instead looks at the whole frame at once, so a symbol
/// anywhere in view is a candidate, and several symbols yield several candidates.
/// </para>
/// <para>
/// The image is reduced to 4 by 4 pixel cells, each marked dark when it contains any ink. The
/// dark cells are dilated so that the one module gaps in a timing pattern do not split a symbol
/// into pieces, and then labelled into connected components. The whole pass costs a few tens of
/// microseconds on a 640 by 480 frame and allocates nothing after the first call.
/// </para>
/// </remarks>
public sealed class BbDarkRegionFinder
{
    /// <summary>Edge length of a cell, in pixels. Chosen so that a cell never straddles a 32 bit word.</summary>
    public const int CellSize = 4;

    private const int CellShift = 2;

    /// <summary>How far dark cells are dilated before labelling, in cells.</summary>
    private const int DilationRadius = 3;

    /// <summary>How many of a cell's sixteen pixels must be ink for it to count as dark.</summary>
    private const int MinInkPerCell = 4;

    private byte[] _dark = [];
    private byte[] _dilated = [];
    private int[] _labels = [];
    private int[] _parent = [];
    private int[] _minX = [];
    private int[] _minY = [];
    private int[] _maxX = [];
    private int[] _maxY = [];
    private int[] _cellCounts = [];
    private int[] _prefix = [];
    private readonly List<BbDarkRegion> _regions = [];

    /// <summary>Locates every patch of ink at least <paramref name="minCells"/> cells large.</summary>
    /// <param name="image">The binarised image.</param>
    /// <param name="minCells">Smallest number of dark cells worth reporting.</param>
    /// <returns>The regions found, in no particular order. The list is reused by the next call.</returns>
    public IReadOnlyList<BbDarkRegion> Find(BbBitMatrix image, int minCells)
    {
        ArgumentNullException.ThrowIfNull(image);
        _regions.Clear();

        var width = image.Width;
        var height = image.Height;
        var columns = (width + CellSize - 1) >> CellShift;
        var rows = (height + CellSize - 1) >> CellShift;
        var cellCount = columns * rows;
        if (cellCount == 0)
        {
            return _regions;
        }

        EnsureCapacity(cellCount);
        MarkDarkCells(image, columns, rows);
        Dilate(columns, rows);
        Label(columns, rows);
        Collect(columns, rows, width, height, minCells);

        return _regions;
    }

    private void EnsureCapacity(int cellCount)
    {
        if (_dark.Length >= cellCount)
        {
            return;
        }

        // Labels are one based, so the per label arrays carry one spare slot.
        _dark = new byte[cellCount];
        _dilated = new byte[cellCount];
        _labels = new int[cellCount];
        _parent = new int[cellCount + 1];
        _minX = new int[cellCount + 1];
        _minY = new int[cellCount + 1];
        _maxX = new int[cellCount + 1];
        _maxY = new int[cellCount + 1];
        _cellCounts = new int[cellCount + 1];
        _prefix = new int[cellCount + 1];
    }

    /// <summary>
    /// Marks a cell dark when enough of its sixteen pixels are ink.
    /// </summary>
    /// <remarks>
    /// Counting rather than testing for any ink at all is what makes the finder usable on a
    /// noisy frame: a sensor speckle sets one pixel, and treating that as a dark cell would,
    /// after dilation, merge the whole image into a single region.
    /// </remarks>
    private void MarkDarkCells(BbBitMatrix image, int columns, int rows)
    {
        var dark = _dark;
        var counts = _cellCounts;
        var cellCount = columns * rows;
        Array.Clear(dark, 0, cellCount);
        Array.Clear(counts, 0, cellCount);

        var height = image.Height;
        for (var y = 0; y < height; y++)
        {
            var words = image.GetRowWords(y);
            var cellRow = (y >> CellShift) * columns;
            for (var cx = 0; cx < columns; cx++)
            {
                var x = cx << CellShift;
                var nibble = (words[x >> 5] >> (x & 31)) & 0xF;
                if (nibble != 0)
                {
                    counts[cellRow + cx] += System.Numerics.BitOperations.PopCount((uint)nibble);
                }
            }
        }

        for (var i = 0; i < cellCount; i++)
        {
            if (counts[i] >= MinInkPerCell)
            {
                dark[i] = 1;
            }
        }
    }

    /// <summary>
    /// Widens every dark cell by <see cref="DilationRadius"/> in both directions, so that the one
    /// module gaps of a timing pattern do not split a symbol into separate regions.
    /// </summary>
    /// <remarks>
    /// Both passes run over a prefix sum rather than a sliding window of comparisons, so the cost
    /// is one add and one subtract per cell whatever the radius is. This runs on every frame of a
    /// continuous scan, so the difference from the naive form is worth having.
    /// </remarks>
    private void Dilate(int columns, int rows)
    {
        var dark = _dark;
        var dilated = _dilated;
        var horizontal = _labels;
        var prefix = _prefix;

        for (var y = 0; y < rows; y++)
        {
            var row = y * columns;
            prefix[0] = 0;
            for (var x = 0; x < columns; x++)
            {
                prefix[x + 1] = prefix[x] + dark[row + x];
            }

            for (var x = 0; x < columns; x++)
            {
                var from = Math.Max(0, x - DilationRadius);
                var to = Math.Min(columns, x + DilationRadius + 1);
                horizontal[row + x] = prefix[to] - prefix[from] > 0 ? 1 : 0;
            }
        }

        for (var x = 0; x < columns; x++)
        {
            prefix[0] = 0;
            for (var y = 0; y < rows; y++)
            {
                prefix[y + 1] = prefix[y] + horizontal[(y * columns) + x];
            }

            for (var y = 0; y < rows; y++)
            {
                var from = Math.Max(0, y - DilationRadius);
                var to = Math.Min(rows, y + DilationRadius + 1);
                dilated[(y * columns) + x] = (byte)(prefix[to] - prefix[from] > 0 ? 1 : 0);
            }
        }
    }

    private void Label(int columns, int rows)
    {
        var dilated = _dilated;
        var labels = _labels;
        var parent = _parent;
        var next = 1;

        // Classic two pass labelling with union-find, 8-connected.
        for (var y = 0; y < rows; y++)
        {
            var row = y * columns;
            for (var x = 0; x < columns; x++)
            {
                var index = row + x;
                if (dilated[index] == 0)
                {
                    labels[index] = 0;
                    continue;
                }

                var label = 0;
                if (x > 0)
                {
                    label = labels[index - 1];
                }

                if (y > 0)
                {
                    var above = index - columns;
                    label = Merge(parent, label, labels[above]);
                    if (x > 0)
                    {
                        label = Merge(parent, label, labels[above - 1]);
                    }

                    if (x < columns - 1)
                    {
                        label = Merge(parent, label, labels[above + 1]);
                    }
                }

                if (label == 0)
                {
                    label = next++;
                    parent[label] = label;
                }

                labels[index] = label;
            }
        }

        for (var i = 0; i < columns * rows; i++)
        {
            if (labels[i] != 0)
            {
                labels[i] = Find(parent, labels[i]);
            }
        }
    }

    private static int Merge(int[] parent, int a, int b)
    {
        if (a == 0)
        {
            return b;
        }

        if (b == 0)
        {
            return a;
        }

        var rootA = Find(parent, a);
        var rootB = Find(parent, b);
        if (rootA == rootB)
        {
            return rootA;
        }

        if (rootA < rootB)
        {
            parent[rootB] = rootA;
            return rootA;
        }

        parent[rootA] = rootB;
        return rootB;
    }

    private static int Find(int[] parent, int label)
    {
        while (parent[label] != label)
        {
            parent[label] = parent[parent[label]];
            label = parent[label];
        }

        return label;
    }

    private void Collect(int columns, int rows, int width, int height, int minCells)
    {
        var dark = _dark;
        var labels = _labels;
        var count = (columns * rows) + 1;

        Array.Clear(_cellCounts, 0, count);
        Array.Fill(_minX, int.MaxValue, 0, count);
        Array.Fill(_minY, int.MaxValue, 0, count);
        Array.Fill(_maxX, -1, 0, count);
        Array.Fill(_maxY, -1, 0, count);

        // Bounding boxes come from the original dark cells, not the dilated ones, so a region
        // is exactly as large as the ink it contains.
        for (var y = 0; y < rows; y++)
        {
            var row = y * columns;
            for (var x = 0; x < columns; x++)
            {
                if (dark[row + x] == 0)
                {
                    continue;
                }

                var label = labels[row + x];
                _cellCounts[label]++;
                if (x < _minX[label]) _minX[label] = x;
                if (x > _maxX[label]) _maxX[label] = x;
                if (y < _minY[label]) _minY[label] = y;
                if (y > _maxY[label]) _maxY[label] = y;
            }
        }

        for (var label = 1; label < count; label++)
        {
            var cells = _cellCounts[label];
            if (cells < minCells)
            {
                continue;
            }

            _regions.Add(new BbDarkRegion(
                _minX[label] << CellShift,
                _minY[label] << CellShift,
                Math.Min(width - 1, ((_maxX[label] + 1) << CellShift) - 1),
                Math.Min(height - 1, ((_maxY[label] + 1) << CellShift) - 1),
                cells));
        }
    }
}

/// <summary>A connected patch of ink, as a pixel bounding box.</summary>
/// <param name="Left">Left edge, inclusive.</param>
/// <param name="Top">Top edge, inclusive.</param>
/// <param name="Right">Right edge, inclusive.</param>
/// <param name="Bottom">Bottom edge, inclusive.</param>
/// <param name="Cells">Number of dark cells the patch contains.</param>
public readonly record struct BbDarkRegion(int Left, int Top, int Right, int Bottom, int Cells)
{
    /// <summary>Width in pixels.</summary>
    public int Width => Right - Left + 1;

    /// <summary>Height in pixels.</summary>
    public int Height => Bottom - Top + 1;

    /// <summary>Horizontal centre in pixels.</summary>
    public float CenterX => (Left + Right) / 2.0f;

    /// <summary>Vertical centre in pixels.</summary>
    public float CenterY => (Top + Bottom) / 2.0f;
}
