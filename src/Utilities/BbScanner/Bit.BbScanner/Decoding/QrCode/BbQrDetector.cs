namespace Bit.BbScanner;

/// <summary>
/// Finds a QR symbol in a binarised image and samples its module grid.
/// </summary>
public sealed class BbQrDetector
{
    private readonly BbQrFinderPatternFinder _finder = new();
    private BbBitMatrix _image;

    /// <summary>The module size, in pixels, below which a grid the binarised image does not decode is sampled from luminance.</summary>
    private const float MaxLuminanceModule = 3.5f;

    /// <summary>Creates a detector over a binarised image.</summary>
    /// <param name="image">The binarised image.</param>
    public BbQrDetector(BbBitMatrix image)
    {
        ArgumentNullException.ThrowIfNull(image);
        _image = image;
    }

    /// <summary>
    /// Points an existing detector at another image, so that continuous scanning reuses one
    /// instance and its finder rather than allocating a pair per frame.
    /// </summary>
    /// <param name="image">The binarised image.</param>
    public void Reset(BbBitMatrix image)
    {
        ArgumentNullException.ThrowIfNull(image);
        _image = image;
    }

    /// <summary>The finder pattern centres the last <see cref="Detect(bool, Func{BbDetectorResult, bool})"/> found, a Micro QR symbol's among them.</summary>
    internal ReadOnlySpan<BbQrFinderPattern> FinderCenters => _finder.Centers;

    /// <summary>Searches the image for finder patterns without trying them as a QR symbol's; see <see cref="FinderCenters"/>.</summary>
    internal ReadOnlySpan<BbQrFinderPattern> ScanFinders(bool tryHarder)
    {
        _finder.Reset(_image);
        _finder.FindCandidates(tryHarder);
        return _finder.Centers;
    }

    /// <summary>Detects and samples a symbol.</summary>
    /// <param name="tryHarder">When <see langword="true"/> the finder pattern search scans every row.</param>
    /// <returns>The sampled grid and its corner points, or <see langword="null"/> when no symbol is found.</returns>
    public BbDetectorResult? Detect(bool tryHarder) => Detect(tryHarder, static _ => true);

    /// <summary>
    /// Detects and samples a symbol, offering each candidate grid to <paramref name="accept"/>
    /// until one is taken.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two of the three inputs to the sampling grid are estimates, and both are wrong often
    /// enough to matter on real camera frames. The alignment pattern search can lock onto an
    /// isolated data module, which shears the whole grid; and the dimension, derived from the
    /// measured module size, drifts by a few modules on large symbols where a two per cent
    /// error is worth four modules.
    /// </para>
    /// <para>
    /// Rather than commit to one guess, the detector produces the candidates in order of
    /// likelihood: with the alignment pattern, without it, and at the dimension the symbol's own
    /// version information declares. A grid whose timing patterns do not alternate is discarded
    /// before the caller ever sees it, which costs one pass over two lines of modules and
    /// removes almost every wrong grid.
    /// </para>
    /// <para>
    /// The finder patterns themselves are a guess too. The finder returns its triples ranked by
    /// how well they fit the corners of a symbol, and a triple that includes a false centre
    /// usually fails the dimension estimate or the timing check, so the detector moves on to the
    /// next triple rather than giving up.
    /// </para>
    /// </remarks>
    /// <param name="tryHarder">When <see langword="true"/> the finder pattern search scans every row.</param>
    /// <param name="accept">Called with each candidate; returning <see langword="true"/> ends the search.</param>
    /// <returns>The accepted result, or <see langword="null"/>. Rejected candidates are disposed.</returns>
    public BbDetectorResult? Detect(bool tryHarder, Func<BbDetectorResult, bool> accept) => Detect(tryHarder, accept, default, inverted: false);

    /// <summary>
    /// Detects and samples a symbol as <see cref="Detect(bool, Func{BbDetectorResult, bool})"/> does, and samples a grid
    /// the binarised image does not decode again from the luminance plane it was made from, when that is not empty and
    /// the symbol's modules are small enough for binarisation to have lost some; see
    /// <see cref="BbGridSampler.SampleLuminance"/>.
    /// </summary>
    /// <param name="tryHarder">When <see langword="true"/> the finder pattern search scans every row.</param>
    /// <param name="accept">Called with each candidate; returning <see langword="true"/> ends the search.</param>
    /// <param name="luminance">The luminance plane the image was binarised from, or an empty view.</param>
    /// <param name="inverted">Whether the symbol sought is light on dark.</param>
    internal BbDetectorResult? Detect(bool tryHarder, Func<BbDetectorResult, bool> accept, in BbLuminanceView luminance, bool inverted)
    {
        ArgumentNullException.ThrowIfNull(accept);

        _finder.Reset(_image);
        var triples = _finder.FindCandidates(tryHarder);
        foreach (var triple in triples)
        {
            if (DetectFrom(triple.TopLeft, triple.TopRight, triple.BottomLeft, accept, luminance, inverted) is { } result)
            {
                return result;
            }
        }

        return null;
    }

    private BbDetectorResult? DetectFrom(
        BbQrFinderPattern topLeft,
        BbQrFinderPattern topRight,
        BbQrFinderPattern bottomLeft,
        Func<BbDetectorResult, bool> accept,
        in BbLuminanceView luminance,
        bool inverted)
    {
        // Written this way so that a NaN, which every comparison answers false to, is rejected
        // rather than carried into the dimension estimate.
        var moduleSize = CalculateModuleSize(topLeft, topRight, bottomLeft);
        if (!(moduleSize >= 1.0f))
        {
            return null;
        }

        // Every dimension near the estimate is a candidate, nearest first, and the timing check
        // in TryCandidate turns the wrong ones away for the price of two lines of modules.
        Span<int> dimensions = stackalloc int[MaxDimensionCandidates];
        var count = CandidateDimensions(EstimateDimension(topLeft, topRight, bottomLeft, moduleSize), dimensions);

        BbQrFinderPattern? alignmentPattern = null;
        var alignmentSearched = false;
        for (var i = 0; i < count; i++)
        {
            var dimension = dimensions[i];
            var version = BbQrVersion.GetProvisionalVersionForDimension(dimension);
            if (version is null)
            {
                continue;
            }

            var hasAlignment = version.AlignmentPatternCenters.Length > 0;

            // Searched for once: where the bottom right alignment pattern sits moves by a fraction
            // of a pixel between neighbouring dimensions, far less than the search allows for.
            if (hasAlignment && !alignmentSearched)
            {
                alignmentPattern = FindAlignment(topLeft, topRight, bottomLeft, version, moduleSize);
                alignmentSearched = true;
            }

            // Preferred first: the alignment pattern corrects for perspective when it is real.
            if (hasAlignment && alignmentPattern is not null &&
                TryCandidate(topLeft, topRight, bottomLeft, alignmentPattern, dimension, accept, luminance, inverted) is { } withAlignment)
            {
                return withAlignment;
            }

            // The affine grid, which ignores the alignment pattern entirely. This is the one that
            // works when the alignment search found noise.
            if (TryCandidate(topLeft, topRight, bottomLeft, null, dimension, accept, luminance, inverted) is { } affine)
            {
                return affine;
            }
        }

        return null;
    }

    /// <summary>The most dimensions tried for one finder pattern triple: the estimate's and two either side.</summary>
    private const int MaxDimensionCandidates = 5;

    /// <summary>
    /// The symbol's dimension as the finder pattern spacing gives it: the centres are seven modules
    /// in from the corners between them, so the spacing in modules plus seven. Unrounded, so that
    /// the legal dimension it falls nearest can be told apart from the one it merely rounds to.
    /// </summary>
    private static float EstimateDimension(
        BbQrFinderPattern topLeft,
        BbQrFinderPattern topRight,
        BbQrFinderPattern bottomLeft,
        float moduleSize)
    {
        var tlTr = BbMathUtils.Distance(topLeft.X, topLeft.Y, topRight.X, topRight.Y) / moduleSize;
        var tlBl = BbMathUtils.Distance(topLeft.X, topLeft.Y, bottomLeft.X, bottomLeft.Y) / moduleSize;
        return ((tlTr + tlBl) / 2) + 7;
    }

    /// <summary>
    /// Writes the legal dimensions near an estimate into <paramref name="candidates"/>, nearest
    /// first, and returns how many there are.
    /// </summary>
    /// <remarks>
    /// Legal dimensions are four apart, and the estimate is a ratio of measured distances, so its
    /// error grows with the symbol: a module size two per cent off is a fraction of a module on
    /// a version 1 symbol and two and a half modules on a version 25 one. An estimate two modules
    /// from either neighbour is the ordinary case on a rotated large symbol, not a sign the triple
    /// is false, so both neighbours are tried, and then the ones beyond them. Small symbols are
    /// searched within four modules, large ones - where the estimate drifts furthest - within
    /// eight.
    /// </remarks>
    private static int CandidateDimensions(float estimate, Span<int> candidates)
    {
        if (!float.IsFinite(estimate))
        {
            return 0;
        }

        var reach = estimate < 45 ? 4.5f : 8.5f;
        var count = 0;
        var first = Math.Max(21, (int)MathF.Ceiling((estimate - reach - 17) / 4) * 4 + 17);
        for (var dimension = first; dimension <= 177 && dimension <= estimate + reach && count < candidates.Length; dimension += 4)
        {
            // Insertion sort by distance from the estimate keeps the list ordered as it grows.
            var at = count++;
            while (at > 0 && MathF.Abs(candidates[at - 1] - estimate) > MathF.Abs(dimension - estimate))
            {
                candidates[at] = candidates[at - 1];
                at--;
            }

            candidates[at] = dimension;
        }

        return count;
    }

    private BbDetectorResult? TryCandidate(
        BbQrFinderPattern topLeft,
        BbQrFinderPattern topRight,
        BbQrFinderPattern bottomLeft,
        BbQrFinderPattern? alignmentPattern,
        int dimension,
        Func<BbDetectorResult, bool> accept,
        in BbLuminanceView luminance,
        bool inverted)
    {
        var transform = CreateTransform(topLeft, topRight, bottomLeft, alignmentPattern, dimension);
        if (TryGrid(BbGridSampler.Sample(_image, dimension, dimension, transform), transform, dimension, accept) is { } result)
        {
            return result;
        }

        // At two or three pixels a module a turned symbol's binarised image has lost modules its luminance still tells
        // apart, so a grid that did not decode is sampled again from that.
        var module = BbScanPoint.Distance(transform.Transform(new BbScanPoint(0, 0)), transform.Transform(new BbScanPoint(dimension, 0))) / dimension;
        return !luminance.IsEmpty && module < MaxLuminanceModule
            ? TryGrid(BbGridSampler.SampleLuminance(luminance, dimension, dimension, transform, inverted), transform, dimension, accept)
            : null;
    }

    /// <summary>Checks a sampled grid's timing patterns and hands it to <paramref name="accept"/> with its corners.</summary>
    private static BbDetectorResult? TryGrid(BbBitMatrix? bits, BbPerspectiveTransform transform, int dimension, Func<BbDetectorResult, bool> accept)
    {
        if (bits is null)
        {
            return null;
        }

        if (!HasPlausibleTimingPatterns(bits))
        {
            bits.Dispose();
            return null;
        }

        // The symbol's own corners, clockwise from its top-left: where the module grid the
        // transform samples begins and ends. The finder pattern centres the transform was built
        // from sit three and a half modules inside them, and a box drawn through those would
        // cut the symbol's outer modules off.
        BbScanPoint[] points =
        [
            transform.Transform(new BbScanPoint(0, 0)),
            transform.Transform(new BbScanPoint(dimension, 0)),
            transform.Transform(new BbScanPoint(dimension, dimension)),
            transform.Transform(new BbScanPoint(0, dimension)),
        ];

        var result = new BbDetectorResult(bits, points);
        if (accept(result))
        {
            return result;
        }

        result.Dispose();
        return null;
    }

    /// <summary>
    /// Checks the two timing patterns of a sampled grid, the alternating row and column at
    /// index 6 between the finder patterns.
    /// </summary>
    /// <remarks>
    /// A grid sampled through a wrong transform is essentially random there, while a correctly
    /// sampled one is exact. Allowing a fifth of the modules to be wrong keeps damaged and
    /// blurred symbols while rejecting sheared grids, which typically get half of them wrong.
    /// </remarks>
    private static bool HasPlausibleTimingPatterns(BbBitMatrix bits)
    {
        var dimension = bits.Width;
        if (dimension < 21)
        {
            return false;
        }

        var errors = 0;
        var checked_ = 0;
        for (var i = 8; i < dimension - 8; i++)
        {
            var expected = (i & 1) == 0;
            checked_ += 2;
            if (bits[i, 6] != expected)
            {
                errors++;
            }

            if (bits[6, i] != expected)
            {
                errors++;
            }
        }

        // A fifth of the modules may be wrong, but never fewer than four in absolute terms: the
        // timing pattern is the finest detail in the symbol and degrades before the data does,
        // and a version 1 symbol only offers ten modules to check.
        return checked_ > 0 && (errors * 5 <= checked_ || errors <= 4);
    }

    private BbQrFinderPattern? FindAlignment(
        BbQrFinderPattern topLeft,
        BbQrFinderPattern topRight,
        BbQrFinderPattern bottomLeft,
        BbQrVersion provisionalVersion,
        float moduleSize)
    {
        var modulesBetweenCenters = provisionalVersion.DimensionForVersion - 7;

        // The fourth corner of the parallelogram formed by the three finder patterns is where
        // the bottom-right of the symbol falls; the alignment pattern sits three modules in.
        var bottomRightX = topRight.X - topLeft.X + bottomLeft.X;
        var bottomRightY = topRight.Y - topLeft.Y + bottomLeft.Y;
        var correction = 1.0f - (3.0f / modulesBetweenCenters);
        var estimatedX = (int)(topLeft.X + (correction * (bottomRightX - topLeft.X)));
        var estimatedY = (int)(topLeft.Y + (correction * (bottomRightY - topLeft.Y)));

        // Widen the search until it succeeds; a tight region is much cheaper when it works.
        for (var allowance = 4; allowance <= 16; allowance <<= 1)
        {
            var found = FindAlignmentInRegion(moduleSize, estimatedX, estimatedY, allowance);
            if (found is null)
            {
                continue;
            }

            // A pattern further from the estimate than the search that found it is a false
            // sighting; using it would shear the grid far worse than ignoring it.
            var drift = BbMathUtils.Distance(found.X, found.Y, estimatedX, estimatedY);
            return drift <= Math.Max(4f * moduleSize, allowance * moduleSize * 0.75f) ? found : null;
        }

        return null;
    }

    private static BbPerspectiveTransform CreateTransform(
        BbQrFinderPattern topLeft,
        BbQrFinderPattern topRight,
        BbQrFinderPattern bottomLeft,
        BbQrFinderPattern? alignmentPattern,
        int dimension)
    {
        var dimMinusThree = dimension - 3.5f;

        float bottomRightX, bottomRightY, sourceBottomRightX, sourceBottomRightY;
        if (alignmentPattern is not null)
        {
            bottomRightX = alignmentPattern.X;
            bottomRightY = alignmentPattern.Y;
            sourceBottomRightX = dimMinusThree - 3.0f;
            sourceBottomRightY = sourceBottomRightX;
        }
        else
        {
            bottomRightX = topRight.X - topLeft.X + bottomLeft.X;
            bottomRightY = topRight.Y - topLeft.Y + bottomLeft.Y;
            sourceBottomRightX = dimMinusThree;
            sourceBottomRightY = dimMinusThree;
        }

        return BbPerspectiveTransform.QuadrilateralToQuadrilateral(
            3.5f, 3.5f,
            dimMinusThree, 3.5f,
            sourceBottomRightX, sourceBottomRightY,
            3.5f, dimMinusThree,
            topLeft.X, topLeft.Y,
            topRight.X, topRight.Y,
            bottomRightX, bottomRightY,
            bottomLeft.X, bottomLeft.Y);
    }

    private float CalculateModuleSize(BbQrFinderPattern topLeft, BbQrFinderPattern topRight, BbQrFinderPattern bottomLeft) =>
        (CalculateModuleSizeOneWay(topLeft, topRight) + CalculateModuleSizeOneWay(topLeft, bottomLeft)) / 2.0f;

    private float CalculateModuleSizeOneWay(BbQrFinderPattern pattern, BbQrFinderPattern otherPattern)
    {
        var estimate1 = SizeOfBlackWhiteBlackRunBothWays(
            (int)pattern.X, (int)pattern.Y, (int)otherPattern.X, (int)otherPattern.Y);
        var estimate2 = SizeOfBlackWhiteBlackRunBothWays(
            (int)otherPattern.X, (int)otherPattern.Y, (int)pattern.X, (int)pattern.Y);

        if (float.IsNaN(estimate1))
        {
            return estimate2 / 7.0f;
        }

        if (float.IsNaN(estimate2))
        {
            return estimate1 / 7.0f;
        }

        // Seven modules span a finder pattern, and each estimate covers one of them twice.
        return (estimate1 + estimate2) / 14.0f;
    }

    private float SizeOfBlackWhiteBlackRunBothWays(int fromX, int fromY, int toX, int toY)
    {
        var result = SizeOfBlackWhiteBlackRun(fromX, fromY, toX, toY);

        // Measure the mirrored run as well, clipping the ray to the image if it leaves it.
        var scale = 1.0f;
        var otherToX = fromX - (toX - fromX);
        if (otherToX < 0)
        {
            scale = fromX / (float)(fromX - otherToX);
            otherToX = 0;
        }
        else if (otherToX >= _image.Width)
        {
            scale = (_image.Width - 1 - fromX) / (float)(otherToX - fromX);
            otherToX = _image.Width - 1;
        }

        var otherToY = (int)(fromY - ((toY - fromY) * scale));

        scale = 1.0f;
        if (otherToY < 0)
        {
            scale = fromY / (float)(fromY - otherToY);
            otherToY = 0;
        }
        else if (otherToY >= _image.Height)
        {
            scale = (_image.Height - 1 - fromY) / (float)(otherToY - fromY);
            otherToY = _image.Height - 1;
        }

        otherToX = (int)(fromX + ((otherToX - fromX) * scale));

        result += SizeOfBlackWhiteBlackRun(fromX, fromY, otherToX, otherToY);

        // The starting pixel was counted by both halves.
        return result - 1.0f;
    }

    /// <summary>
    /// Walks a Bresenham line outwards from a finder pattern centre and returns the distance at
    /// which the black, white, black transition sequence completes.
    /// </summary>
    private float SizeOfBlackWhiteBlackRun(int fromX, int fromY, int toX, int toY)
    {
        var steep = Math.Abs(toY - fromY) > Math.Abs(toX - fromX);
        if (steep)
        {
            (fromX, fromY) = (fromY, fromX);
            (toX, toY) = (toY, toX);
        }

        var dx = Math.Abs(toX - fromX);
        var dy = Math.Abs(toY - fromY);
        var error = -dx / 2;
        var xStep = fromX < toX ? 1 : -1;
        var yStep = fromY < toY ? 1 : -1;

        var state = 0;
        var x = fromX;
        var y = fromY;
        for (; x != toX + xStep; x += xStep)
        {
            var realX = steep ? y : x;
            var realY = steep ? x : y;

            if ((uint)realX >= (uint)_image.Width || (uint)realY >= (uint)_image.Height)
            {
                break;
            }

            if ((state == 1) == _image[realX, realY])
            {
                if (state == 2)
                {
                    return BbMathUtils.Distance(x, y, fromX, fromY);
                }

                state++;
            }

            error += dy;
            if (error > 0)
            {
                if (y == toY)
                {
                    break;
                }

                y += yStep;
                error -= dx;
            }
        }

        // Running off the end of the image while still inside the final black run is treated as
        // a measurement to the edge rather than a failure.
        return state == 2 ? BbMathUtils.Distance(toX + xStep, toY, fromX, fromY) : float.NaN;
    }

    private BbQrFinderPattern? FindAlignmentInRegion(float overallEstModuleSize, int estAlignmentX, int estAlignmentY, int allowanceFactor)
    {
        var allowance = (int)(allowanceFactor * overallEstModuleSize);
        var left = Math.Max(0, estAlignmentX - allowance);
        var right = Math.Min(_image.Width - 1, estAlignmentX + allowance);
        if (right - left < overallEstModuleSize * 3)
        {
            return null;
        }

        var top = Math.Max(0, estAlignmentY - allowance);
        var bottom = Math.Min(_image.Height - 1, estAlignmentY + allowance);
        if (bottom - top < overallEstModuleSize * 3)
        {
            return null;
        }

        return new BbQrAlignmentPatternFinder(_image, left, top, right - left, bottom - top, overallEstModuleSize).Find();
    }
}
