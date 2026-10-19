namespace Bit.BbScanner;

/// <summary>
/// Locates the three finder patterns of a QR symbol in a binarised image.
/// </summary>
/// <remarks>
/// <para>
/// The search looks for the 1:1:3:1:1 dark/light ratio that a horizontal line through a finder
/// pattern always produces, whatever the rotation of the symbol. A horizontal hit is then
/// confirmed vertically and diagonally before it is accepted, which is what keeps the false
/// positive rate low enough to run this on every camera frame.
/// </para>
/// <para>
/// Rows are sampled with a stride rather than exhaustively. The stride starts at roughly one
/// third of the smallest readable finder pattern and drops once a first candidate is confirmed,
/// so a frame containing no symbol costs a fraction of a frame that contains one.
/// </para>
/// <para>
/// The data region of a large symbol is full of 1:1:3:1:1 coincidences, and some of them survive
/// every cross-check. The scan therefore only stops early once three confirmed centres also sit
/// where the corners of a symbol could sit, and the triples that come out are ranked so that a
/// caller can fall back to the next one when the first does not sample into a readable grid.
/// </para>
/// </remarks>
public sealed class BbQrFinderPatternFinder
{
    private const int MinSkip = 3;
    private const int MaxModules = 97;

    /// <summary>How many ranked triples are kept; the rest are discarded.</summary>
    public const int MaxCandidates = 4;

    /// <summary>
    /// The centres of a version 1 symbol are fourteen modules apart along each edge, so no two
    /// finder patterns of one symbol can be closer than that.
    /// </summary>
    private const float MinCenterSpacingModules = 14.0f;

    /// <summary>
    /// The largest distortion, relative to the squared longest side, at which three confirmed
    /// centres are trusted enough to stop the scan. Rendered symbols score close to zero; a
    /// corner placed on a lattice coincidence scores around 0.4 and up.
    /// </summary>
    private const float MaxEarlyStopDistortion = 0.25f;

    /// <summary>
    /// The smallest triangle area, as a fraction of the squared longest side, that is not
    /// treated as three collinear points. A right isosceles triangle scores 0.5.
    /// </summary>
    private const float MinTriangleArea = 0.05f;

    private static readonly Comparison<BbQrFinderPattern> ByModuleSize =
        static (a, b) => a.EstimatedModuleSize.CompareTo(b.EstimatedModuleSize);

    private readonly List<BbQrFinderPattern> _possibleCenters = new(8);
    private readonly int[] _crossCheckStateCount = new int[5];
    private readonly Triple[] _candidates = new Triple[MaxCandidates];

    private BbBitMatrix? _image;
    private bool _hasSkipped;
    private int _candidateCount;

    /// <summary>
    /// Three finder patterns that could be the corners of one symbol, already in the order the
    /// perspective transform expects.
    /// </summary>
    public readonly struct Triple
    {
        internal Triple(BbQrFinderPattern bottomLeft, BbQrFinderPattern topLeft, BbQrFinderPattern topRight, float distortion)
        {
            BottomLeft = bottomLeft;
            TopLeft = topLeft;
            TopRight = topRight;
            Distortion = distortion;
        }

        /// <summary>The pattern below the top-left one.</summary>
        public BbQrFinderPattern BottomLeft { get; }

        /// <summary>The pattern at the right-angled corner.</summary>
        public BbQrFinderPattern TopLeft { get; }

        /// <summary>The pattern to the right of the top-left one.</summary>
        public BbQrFinderPattern TopRight { get; }

        /// <summary>
        /// How far the triangle departs from an isosceles right triangle, in squared pixels;
        /// zero is a perfect, upright symbol and smaller is more likely.
        /// </summary>
        public float Distortion { get; }
    }

    /// <summary>Creates a finder without an image; call <see cref="Reset"/> before searching.</summary>
    public BbQrFinderPatternFinder()
    {
    }

    /// <summary>Creates a finder over a binarised image.</summary>
    /// <param name="image">The binarised image.</param>
    public BbQrFinderPatternFinder(BbBitMatrix image)
    {
        ArgumentNullException.ThrowIfNull(image);
        _image = image;
    }

    /// <summary>
    /// Points the finder at another image and forgets the previous search, so that continuous
    /// scanning reuses one instance and its candidate list.
    /// </summary>
    /// <param name="image">The binarised image.</param>
    public void Reset(BbBitMatrix image)
    {
        ArgumentNullException.ThrowIfNull(image);
        _image = image;
        _possibleCenters.Clear();
        _hasSkipped = false;
        _candidateCount = 0;
    }

    /// <summary>
    /// Searches for three finder patterns and returns the most likely triple.
    /// </summary>
    /// <param name="tryHarder">When <see langword="true"/> every row is scanned instead of a strided subset.</param>
    /// <param name="patterns">
    /// Receives the patterns ordered bottom-left, top-left, top-right, which is the order the
    /// perspective transform expects.
    /// </param>
    public bool TryFind(bool tryHarder, out BbQrFinderPattern[] patterns)
    {
        var candidates = FindCandidates(tryHarder);
        if (candidates.IsEmpty)
        {
            patterns = [];
            return false;
        }

        var best = candidates[0];
        patterns = [best.BottomLeft, best.TopLeft, best.TopRight];
        return true;
    }

    /// <summary>
    /// Searches for finder patterns and returns every plausible triple, most likely first.
    /// </summary>
    /// <remarks>
    /// The span is a view over scratch storage owned by this finder and is only valid until the
    /// next search or <see cref="Reset"/>. At most <see cref="MaxCandidates"/> triples are kept.
    /// </remarks>
    /// <param name="tryHarder">When <see langword="true"/> every row is scanned instead of a strided subset.</param>
    public ReadOnlySpan<Triple> FindCandidates(bool tryHarder)
    {
        _possibleCenters.Clear();
        _hasSkipped = false;
        _candidateCount = 0;

        Scan(tryHarder);
        RankCandidates();

        return _candidates.AsSpan(0, _candidateCount);
    }

    private void Scan(bool tryHarder)
    {
        var maxI = _image!.Height;
        var maxJ = _image!.Width;

        var iSkip = (3 * maxI) / (4 * MaxModules);
        if (iSkip < MinSkip || tryHarder)
        {
            iSkip = MinSkip;
        }

        Span<int> stateCount = stackalloc int[5];
        var done = false;

        for (var i = iSkip - 1; i < maxI && !done; i += iSkip)
        {
            stateCount.Clear();
            var currentState = 0;

            for (var j = 0; j < maxJ; j++)
            {
                if (_image![j, i])
                {
                    // Black.
                    if ((currentState & 1) == 1)
                    {
                        currentState++;
                    }

                    stateCount[currentState]++;
                }
                else
                {
                    // White.
                    if ((currentState & 1) == 1)
                    {
                        stateCount[currentState]++;
                        continue;
                    }

                    if (currentState != 4)
                    {
                        stateCount[++currentState]++;
                        continue;
                    }

                    if (!FoundPatternCross(stateCount))
                    {
                        ShiftCountsBy2(stateCount);
                        currentState = 3;
                        continue;
                    }

                    if (!HandlePossibleCenter(stateCount, i, j))
                    {
                        ShiftCountsBy2(stateCount);
                        currentState = 3;
                        continue;
                    }

                    // A confirmed centre lets the scan tighten its stride and, the first time,
                    // jump straight down to where the next finder pattern must be.
                    iSkip = 2;
                    if (_hasSkipped)
                    {
                        done = HaveMultiplyConfirmedCenters();
                    }
                    else
                    {
                        var rowSkip = FindRowSkip();
                        if (rowSkip > stateCount[2])
                        {
                            i += rowSkip - stateCount[2] - iSkip;
                            j = maxJ - 1;
                        }
                    }

                    currentState = 0;
                    stateCount.Clear();
                }
            }

            if (FoundPatternCross(stateCount) &&
                HandlePossibleCenter(stateCount, i, maxJ) &&
                _hasSkipped)
            {
                done = HaveMultiplyConfirmedCenters();
            }
        }
    }

    /// <summary>Checks the 1:1:3:1:1 ratio that a line through a finder pattern produces.</summary>
    /// <param name="stateCount">Five alternating run lengths, dark first.</param>
    public static bool FoundPatternCross(ReadOnlySpan<int> stateCount)
    {
        var totalModuleSize = 0;
        for (var i = 0; i < 5; i++)
        {
            var count = stateCount[i];
            if (count == 0)
            {
                return false;
            }

            totalModuleSize += count;
        }

        if (totalModuleSize < 7)
        {
            return false;
        }

        var moduleSize = totalModuleSize / 7.0f;
        var maxVariance = moduleSize / 2.0f;

        return Math.Abs(moduleSize - stateCount[0]) < maxVariance
            && Math.Abs(moduleSize - stateCount[1]) < maxVariance
            && Math.Abs((3.0f * moduleSize) - stateCount[2]) < 3 * maxVariance
            && Math.Abs(moduleSize - stateCount[3]) < maxVariance
            && Math.Abs(moduleSize - stateCount[4]) < maxVariance;
    }

    private static float CenterFromEnd(ReadOnlySpan<int> stateCount, int end) =>
        (end - stateCount[4] - stateCount[3]) - (stateCount[2] / 2.0f);

    private static void ShiftCountsBy2(Span<int> stateCount)
    {
        stateCount[0] = stateCount[2];
        stateCount[1] = stateCount[3];
        stateCount[2] = stateCount[4];
        stateCount[3] = 1;
        stateCount[4] = 0;
    }

    private bool HandlePossibleCenter(ReadOnlySpan<int> stateCount, int i, int j)
    {
        var stateCountTotal = 0;
        for (var k = 0; k < 5; k++)
        {
            stateCountTotal += stateCount[k];
        }

        var centerJ = CenterFromEnd(stateCount, j);
        var centerI = CrossCheckVertical(i, (int)centerJ, stateCount[2], stateCountTotal);
        if (float.IsNaN(centerI))
        {
            return false;
        }

        centerJ = CrossCheckHorizontal((int)centerJ, (int)centerI, stateCount[2], stateCountTotal);
        if (float.IsNaN(centerJ) || !CrossCheckDiagonal((int)centerI, (int)centerJ))
        {
            return false;
        }

        var estimatedModuleSize = stateCountTotal / 7.0f;
        for (var index = 0; index < _possibleCenters.Count; index++)
        {
            var center = _possibleCenters[index];
            if (center.AboutEquals(estimatedModuleSize, centerI, centerJ))
            {
                _possibleCenters[index] = center.CombineEstimate(centerI, centerJ, estimatedModuleSize);
                return true;
            }
        }

        _possibleCenters.Add(new BbQrFinderPattern(centerJ, centerI, estimatedModuleSize));
        return true;
    }

    private float CrossCheckVertical(int startI, int centerJ, int maxCount, int originalStateCountTotal)
    {
        var maxI = _image!.Height;
        var stateCount = _crossCheckStateCount;
        Array.Clear(stateCount);

        var i = startI;
        while (i >= 0 && _image![centerJ, i])
        {
            stateCount[2]++;
            i--;
        }

        if (i < 0)
        {
            return float.NaN;
        }

        while (i >= 0 && !_image![centerJ, i] && stateCount[1] <= maxCount)
        {
            stateCount[1]++;
            i--;
        }

        if (i < 0 || stateCount[1] > maxCount)
        {
            return float.NaN;
        }

        while (i >= 0 && _image![centerJ, i] && stateCount[0] <= maxCount)
        {
            stateCount[0]++;
            i--;
        }

        if (stateCount[0] > maxCount)
        {
            return float.NaN;
        }

        i = startI + 1;
        while (i < maxI && _image![centerJ, i])
        {
            stateCount[2]++;
            i++;
        }

        if (i == maxI)
        {
            return float.NaN;
        }

        while (i < maxI && !_image![centerJ, i] && stateCount[3] < maxCount)
        {
            stateCount[3]++;
            i++;
        }

        if (i == maxI || stateCount[3] >= maxCount)
        {
            return float.NaN;
        }

        while (i < maxI && _image![centerJ, i] && stateCount[4] < maxCount)
        {
            stateCount[4]++;
            i++;
        }

        if (stateCount[4] >= maxCount)
        {
            return float.NaN;
        }

        var stateCountTotal = stateCount[0] + stateCount[1] + stateCount[2] + stateCount[3] + stateCount[4];
        if (5 * Math.Abs(stateCountTotal - originalStateCountTotal) >= 2 * originalStateCountTotal)
        {
            return float.NaN;
        }

        return FoundPatternCross(stateCount) ? CenterFromEnd(stateCount, i) : float.NaN;
    }

    private float CrossCheckHorizontal(int startJ, int centerI, int maxCount, int originalStateCountTotal)
    {
        var maxJ = _image!.Width;
        var stateCount = _crossCheckStateCount;
        Array.Clear(stateCount);

        var j = startJ;
        while (j >= 0 && _image![j, centerI])
        {
            stateCount[2]++;
            j--;
        }

        if (j < 0)
        {
            return float.NaN;
        }

        while (j >= 0 && !_image![j, centerI] && stateCount[1] <= maxCount)
        {
            stateCount[1]++;
            j--;
        }

        if (j < 0 || stateCount[1] > maxCount)
        {
            return float.NaN;
        }

        while (j >= 0 && _image![j, centerI] && stateCount[0] <= maxCount)
        {
            stateCount[0]++;
            j--;
        }

        if (stateCount[0] > maxCount)
        {
            return float.NaN;
        }

        j = startJ + 1;
        while (j < maxJ && _image![j, centerI])
        {
            stateCount[2]++;
            j++;
        }

        if (j == maxJ)
        {
            return float.NaN;
        }

        while (j < maxJ && !_image![j, centerI] && stateCount[3] < maxCount)
        {
            stateCount[3]++;
            j++;
        }

        if (j == maxJ || stateCount[3] >= maxCount)
        {
            return float.NaN;
        }

        while (j < maxJ && _image![j, centerI] && stateCount[4] < maxCount)
        {
            stateCount[4]++;
            j++;
        }

        if (stateCount[4] >= maxCount)
        {
            return float.NaN;
        }

        var stateCountTotal = stateCount[0] + stateCount[1] + stateCount[2] + stateCount[3] + stateCount[4];
        if (5 * Math.Abs(stateCountTotal - originalStateCountTotal) >= originalStateCountTotal)
        {
            return float.NaN;
        }

        return FoundPatternCross(stateCount) ? CenterFromEnd(stateCount, j) : float.NaN;
    }

    /// <summary>
    /// Confirms a candidate along its main diagonal. This is what rejects the many 1:1:3:1:1
    /// horizontal and vertical coincidences produced by text and packaging artwork.
    /// </summary>
    private bool CrossCheckDiagonal(int centerI, int centerJ)
    {
        Span<int> stateCount = stackalloc int[5];

        var i = 0;
        while (centerI >= i && centerJ >= i && _image![centerJ - i, centerI - i])
        {
            stateCount[2]++;
            i++;
        }

        if (centerI < i || centerJ < i)
        {
            return false;
        }

        while (centerI >= i && centerJ >= i && !_image![centerJ - i, centerI - i] && stateCount[1] <= centerI)
        {
            stateCount[1]++;
            i++;
        }

        if (centerI < i || centerJ < i || stateCount[1] > centerI)
        {
            return false;
        }

        while (centerI >= i && centerJ >= i && _image![centerJ - i, centerI - i])
        {
            stateCount[0]++;
            i++;
        }

        var maxI = _image!.Height;
        var maxJ = _image!.Width;

        i = 1;
        while (centerI + i < maxI && centerJ + i < maxJ && _image![centerJ + i, centerI + i])
        {
            stateCount[2]++;
            i++;
        }

        if (centerI + i >= maxI || centerJ + i >= maxJ)
        {
            return false;
        }

        while (centerI + i < maxI && centerJ + i < maxJ && !_image![centerJ + i, centerI + i])
        {
            stateCount[3]++;
            i++;
        }

        if (centerI + i >= maxI || centerJ + i >= maxJ)
        {
            return false;
        }

        while (centerI + i < maxI && centerJ + i < maxJ && _image![centerJ + i, centerI + i])
        {
            stateCount[4]++;
            i++;
        }

        return FoundPatternCross(stateCount);
    }

    /// <summary>
    /// Once two centres are known, the third finder pattern must lie a predictable distance away,
    /// so the scan can jump most of the way there instead of walking.
    /// </summary>
    private int FindRowSkip()
    {
        if (_possibleCenters.Count <= 1)
        {
            return 0;
        }

        BbQrFinderPattern? firstConfirmed = null;
        foreach (var center in _possibleCenters)
        {
            if (center.Count < 2)
            {
                continue;
            }

            if (firstConfirmed is null)
            {
                firstConfirmed = center;
                continue;
            }

            _hasSkipped = true;
            return (int)((Math.Abs(firstConfirmed.X - center.X) - Math.Abs(firstConfirmed.Y - center.Y)) / 2);
        }

        return 0;
    }

    /// <summary>
    /// Decides whether the scan can stop: three confirmed centres whose module sizes agree and
    /// which are laid out like the corners of a symbol.
    /// </summary>
    /// <remarks>
    /// Agreement in module size alone is not enough. The padding region of a large symbol holds
    /// lattices of 1:1:3:1:1 coincidences that pass every cross-check and get confirmed once the
    /// row stride drops to two, and three of those, or two real corners plus one of those, would
    /// otherwise end the scan before it reaches the real third corner.
    /// </remarks>
    private bool HaveMultiplyConfirmedCenters()
    {
        var confirmedCount = 0;
        var totalModuleSize = 0.0f;

        foreach (var pattern in _possibleCenters)
        {
            if (pattern.Count < 2)
            {
                continue;
            }

            confirmedCount++;
            totalModuleSize += pattern.EstimatedModuleSize;
        }

        if (confirmedCount < 3)
        {
            return false;
        }

        // Three confirmed centres are only trustworthy when their module sizes agree; a large
        // spread means at least one of them belongs to something that is not this symbol.
        var average = totalModuleSize / confirmedCount;
        var totalDeviation = 0.0f;
        foreach (var pattern in _possibleCenters)
        {
            if (pattern.Count >= 2)
            {
                totalDeviation += Math.Abs(pattern.EstimatedModuleSize - average);
            }
        }

        if (totalDeviation > 0.05f * totalModuleSize)
        {
            return false;
        }

        var count = _possibleCenters.Count;
        for (var i = 0; i < count - 2; i++)
        {
            var fpi = _possibleCenters[i];
            if (fpi.Count < 2)
            {
                continue;
            }

            for (var j = i + 1; j < count - 1; j++)
            {
                var fpj = _possibleCenters[j];
                if (fpj.Count < 2)
                {
                    continue;
                }

                for (var k = j + 1; k < count; k++)
                {
                    var fpk = _possibleCenters[k];
                    if (fpk.Count < 2)
                    {
                        continue;
                    }

                    var minModuleSize = Math.Min(fpi.EstimatedModuleSize, Math.Min(fpj.EstimatedModuleSize, fpk.EstimatedModuleSize));
                    if (TryMeasure(fpi, fpj, fpk, minModuleSize, out var distortion, out var longestSideSquared) &&
                        distortion <= MaxEarlyStopDistortion * longestSideSquared &&
                        ImpliesLegalDimension(fpi, fpj, fpk) &&
                        HasSolidCenter(fpi) && HasSolidCenter(fpj) && HasSolidCenter(fpk))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks that a candidate still shows the 1:1:3:1:1 ratio one module off its centre, both
    /// horizontally and vertically.
    /// </summary>
    /// <remarks>
    /// The cross-checks that admit a candidate all pass through its centre, and a plus-shaped
    /// coincidence in a data region satisfies every one of them. A real finder pattern has a
    /// solid three by three module centre, so the ratio also holds on the lines a module away,
    /// where such a coincidence falls apart. This is only consulted before stopping the scan
    /// early: a blurred or damaged corner that fails it still gets found by the full scan.
    /// </remarks>
    private bool HasSolidCenter(BbQrFinderPattern pattern)
    {
        var moduleSize = pattern.EstimatedModuleSize;
        var maxCount = BbMathUtils.Round(3.0f * moduleSize);
        var total = BbMathUtils.Round(7.0f * moduleSize);
        var centerX = (int)pattern.X;
        var centerY = (int)pattern.Y;
        var offset = Math.Max(1, (int)moduleSize);

        return IsInside(centerX, centerY - offset) && IsInside(centerX, centerY + offset)
            && IsInside(centerX - offset, centerY) && IsInside(centerX + offset, centerY)
            && !float.IsNaN(CrossCheckHorizontal(centerX, centerY - offset, maxCount, total))
            && !float.IsNaN(CrossCheckHorizontal(centerX, centerY + offset, maxCount, total))
            && !float.IsNaN(CrossCheckVertical(centerY, centerX - offset, maxCount, total))
            && !float.IsNaN(CrossCheckVertical(centerY, centerX + offset, maxCount, total));
    }

    private bool IsInside(int x, int y) =>
        (uint)x < (uint)_image!.Width && (uint)y < (uint)_image!.Height;

    /// <summary>
    /// Checks that the spacing of three centres, in their own modules, is one a QR version has.
    /// </summary>
    /// <remarks>
    /// A lattice of coincidences inside a symbol's data region can form a perfect right isosceles
    /// triangle, but its spacing follows the codeword layout rather than a symbol size: twelve
    /// modules implies a nineteen module symbol, which does not exist. The rule mirrors the
    /// detector's: a legal dimension is one more than a multiple of four, and an estimate one
    /// module off is snapped rather than rejected, since the module size here is only an
    /// estimate from the finder patterns themselves.
    /// </remarks>
    private static bool ImpliesLegalDimension(BbQrFinderPattern a, BbQrFinderPattern b, BbQrFinderPattern c)
    {
        Order(a, b, c, out var bottomLeft, out var topLeft, out var topRight);

        var moduleSize = (a.EstimatedModuleSize + b.EstimatedModuleSize + c.EstimatedModuleSize) / 3.0f;
        var topEdge = BbMathUtils.Round(BbMathUtils.Distance(topLeft.X, topLeft.Y, topRight.X, topRight.Y) / moduleSize);
        var leftEdge = BbMathUtils.Round(BbMathUtils.Distance(topLeft.X, topLeft.Y, bottomLeft.X, bottomLeft.Y) / moduleSize);
        var dimension = ((topEdge + leftEdge) / 2) + 7;

        switch (dimension & 0x03)
        {
            case 0:
                dimension++;
                break;
            case 2:
                dimension--;
                break;
            case 3:
                return false;
        }

        return dimension is >= 21 and <= 177;
    }

    /// <summary>
    /// Ranks every plausible triple of candidates by distortion and keeps the best few.
    /// </summary>
    private void RankCandidates()
    {
        _candidateCount = 0;
        var count = _possibleCenters.Count;
        if (count < 3)
        {
            return;
        }

        _possibleCenters.Sort(ByModuleSize);

        for (var i = 0; i < count - 2; i++)
        {
            var fpi = _possibleCenters[i];
            var minModuleSize = fpi.EstimatedModuleSize;

            for (var j = i + 1; j < count - 1; j++)
            {
                var fpj = _possibleCenters[j];

                for (var k = j + 1; k < count; k++)
                {
                    var fpk = _possibleCenters[k];

                    // The list is sorted by module size, so once one is too large the rest are.
                    if (fpk.EstimatedModuleSize > minModuleSize * 1.4f)
                    {
                        break;
                    }

                    if (TryMeasure(fpi, fpj, fpk, minModuleSize, out var distortion, out _))
                    {
                        InsertCandidate(fpi, fpj, fpk, distortion);
                    }
                }
            }
        }
    }

    /// <summary>Inserts a triple into the ranked candidates, dropping the worst once full.</summary>
    private void InsertCandidate(BbQrFinderPattern a, BbQrFinderPattern b, BbQrFinderPattern c, float distortion)
    {
        var position = _candidateCount;
        while (position > 0 && _candidates[position - 1].Distortion > distortion)
        {
            position--;
        }

        if (position >= MaxCandidates)
        {
            return;
        }

        var last = Math.Min(_candidateCount, MaxCandidates - 1);
        for (var m = last; m > position; m--)
        {
            _candidates[m] = _candidates[m - 1];
        }

        Order(a, b, c, out var bottomLeft, out var topLeft, out var topRight);
        _candidates[position] = new Triple(bottomLeft, topLeft, topRight, distortion);

        if (_candidateCount < MaxCandidates)
        {
            _candidateCount++;
        }
    }

    /// <summary>
    /// Measures how well three centres fit the corners of one symbol.
    /// </summary>
    /// <remarks>
    /// The three centres of a QR symbol form an isosceles right triangle, so the longest side
    /// squared should be twice each of the others; the distortion is how far it is from that.
    /// Two things disqualify a triple outright: a longest side shorter than the centre spacing
    /// of the smallest symbol, and three centres in a line, which no corner set can be.
    /// </remarks>
    /// <returns><see langword="false"/> when the triple cannot be three corners of one symbol.</returns>
    private static bool TryMeasure(
        BbQrFinderPattern a,
        BbQrFinderPattern b,
        BbQrFinderPattern c,
        float minModuleSize,
        out float distortion,
        out float longestSideSquared)
    {
        var ab = SquaredDistance(a, b);
        var bc = SquaredDistance(b, c);
        var ac = SquaredDistance(a, c);

        // Sort the three sides without touching the heap.
        if (ab > bc)
        {
            (ab, bc) = (bc, ab);
        }

        if (bc > ac)
        {
            (bc, ac) = (ac, bc);
        }

        if (ab > bc)
        {
            (ab, bc) = (bc, ab);
        }

        longestSideSquared = ac;
        distortion = 0.0f;

        var minSpacing = MinCenterSpacingModules * minModuleSize;
        if (longestSideSquared < minSpacing * minSpacing)
        {
            return false;
        }

        // The cross product is twice the triangle's area; near zero means collinear.
        if (Math.Abs(CrossProductZ(a, b, c)) < MinTriangleArea * longestSideSquared)
        {
            return false;
        }

        distortion = Math.Abs(longestSideSquared - (2 * bc)) + Math.Abs(longestSideSquared - (2 * ab));
        return true;
    }

    private static float SquaredDistance(BbQrFinderPattern a, BbQrFinderPattern b) =>
        BbMathUtils.DistanceSquared(a.X, a.Y, b.X, b.Y);

    /// <summary>
    /// Puts three finder patterns into bottom-left, top-left, top-right order.
    /// </summary>
    /// <param name="patterns">The three patterns, reordered in place.</param>
    public static void OrderBestPatterns(BbQrFinderPattern[] patterns)
    {
        ArgumentNullException.ThrowIfNull(patterns);

        Order(patterns[0], patterns[1], patterns[2], out var bottomLeft, out var topLeft, out var topRight);
        patterns[0] = bottomLeft;
        patterns[1] = topLeft;
        patterns[2] = topRight;
    }

    private static void Order(
        BbQrFinderPattern first,
        BbQrFinderPattern second,
        BbQrFinderPattern third,
        out BbQrFinderPattern bottomLeft,
        out BbQrFinderPattern topLeft,
        out BbQrFinderPattern topRight)
    {
        var zeroOne = BbMathUtils.Distance(first.X, first.Y, second.X, second.Y);
        var oneTwo = BbMathUtils.Distance(second.X, second.Y, third.X, third.Y);
        var zeroTwo = BbMathUtils.Distance(first.X, first.Y, third.X, third.Y);

        // The pattern opposite the longest side is the corner one: the top-left.
        BbQrFinderPattern pointA, pointB, pointC;
        if (oneTwo >= zeroOne && oneTwo >= zeroTwo)
        {
            pointB = first;
            pointA = second;
            pointC = third;
        }
        else if (zeroTwo >= oneTwo && zeroTwo >= zeroOne)
        {
            pointB = second;
            pointA = first;
            pointC = third;
        }
        else
        {
            pointB = third;
            pointA = first;
            pointC = second;
        }

        // The sign of the cross product says whether A and C are the right way round.
        if (CrossProductZ(pointA, pointB, pointC) < 0.0f)
        {
            (pointA, pointC) = (pointC, pointA);
        }

        bottomLeft = pointA;
        topLeft = pointB;
        topRight = pointC;
    }

    private static float CrossProductZ(BbQrFinderPattern a, BbQrFinderPattern b, BbQrFinderPattern c) =>
        ((c.X - b.X) * (a.Y - b.Y)) - ((c.Y - b.Y) * (a.X - b.X));
}
