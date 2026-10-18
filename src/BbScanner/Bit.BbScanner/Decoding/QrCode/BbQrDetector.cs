namespace Bit.BbScanner;

/// <summary>
/// Finds a QR symbol in a binarised image and samples its module grid.
/// </summary>
public sealed class BbQrDetector
{
    private readonly BbQrFinderPatternFinder _finder = new();
    private BbBitMatrix _image;

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
    public BbDetectorResult? Detect(bool tryHarder, Func<BbDetectorResult, bool> accept)
    {
        ArgumentNullException.ThrowIfNull(accept);

        _finder.Reset(_image);
        var triples = _finder.FindCandidates(tryHarder);
        foreach (var triple in triples)
        {
            if (DetectFrom(triple.TopLeft, triple.TopRight, triple.BottomLeft, accept) is { } result)
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
        Func<BbDetectorResult, bool> accept)
    {
        // Written this way so that a NaN, which every comparison answers false to, is rejected
        // rather than carried into the dimension estimate.
        var moduleSize = CalculateModuleSize(topLeft, topRight, bottomLeft);
        if (!(moduleSize >= 1.0f))
        {
            return null;
        }

        if (!TryComputeDimension(topLeft, topRight, bottomLeft, moduleSize, out var dimension))
        {
            return null;
        }

        var provisionalVersion = BbQrVersion.GetProvisionalVersionForDimension(dimension);
        if (provisionalVersion is null)
        {
            return null;
        }

        var alignmentPattern = provisionalVersion.AlignmentPatternCenters.Length > 0
            ? FindAlignment(topLeft, topRight, bottomLeft, provisionalVersion, moduleSize)
            : null;

        // Preferred first: the alignment pattern corrects for perspective when it is real.
        if (alignmentPattern is not null &&
            TryCandidate(topLeft, topRight, bottomLeft, alignmentPattern, dimension, accept) is { } withAlignment)
        {
            return withAlignment;
        }

        // The affine grid, which ignores the alignment pattern entirely. This is the one that
        // works when the alignment search found noise.
        if (TryCandidate(topLeft, topRight, bottomLeft, null, dimension, accept) is { } affine)
        {
            return affine;
        }

        // Finally, let the symbol say how big it is. Version information is only present from
        // version 7 up, which is exactly where the dimension estimate becomes unreliable.
        if (dimension < 45)
        {
            return null;
        }

        Span<int> candidates = stackalloc int[DimensionDeltas.Length];
        var count = NearbyDimensions(dimension, candidates);
        for (var i = 0; i < count; i++)
        {
            var candidateDimension = candidates[i];
            if (alignmentPattern is not null &&
                TryCandidate(topLeft, topRight, bottomLeft, alignmentPattern, candidateDimension, accept) is { } resized)
            {
                return resized;
            }

            if (TryCandidate(topLeft, topRight, bottomLeft, null, candidateDimension, accept) is { } resizedAffine)
            {
                return resizedAffine;
            }
        }

        return null;
    }

    /// <summary>Offsets from the estimated dimension to retry, nearest first.</summary>
    private static readonly int[] DimensionDeltas = [4, -4, 8, -8];

    /// <summary>
    /// Writes the legal dimensions within eight modules of the estimate into
    /// <paramref name="candidates"/>, nearest first, and returns how many there are.
    /// </summary>
    private static int NearbyDimensions(int dimension, Span<int> candidates)
    {
        var count = 0;
        foreach (var delta in DimensionDeltas)
        {
            var candidate = dimension + delta;
            if (candidate is >= 21 and <= 177 && BbQrVersion.GetProvisionalVersionForDimension(candidate) is not null)
            {
                candidates[count++] = candidate;
            }
        }

        return count;
    }

    private BbDetectorResult? TryCandidate(
        BbQrFinderPattern topLeft,
        BbQrFinderPattern topRight,
        BbQrFinderPattern bottomLeft,
        BbQrFinderPattern? alignmentPattern,
        int dimension,
        Func<BbDetectorResult, bool> accept)
    {
        var transform = CreateTransform(topLeft, topRight, bottomLeft, alignmentPattern, dimension);
        var bits = BbGridSampler.Sample(_image, dimension, dimension, transform);
        if (bits is null)
        {
            return null;
        }

        if (!HasPlausibleTimingPatterns(bits))
        {
            bits.Dispose();
            return null;
        }

        BbScanPoint[] points = alignmentPattern is null
            ? [bottomLeft.Point, topLeft.Point, topRight.Point]
            : [bottomLeft.Point, topLeft.Point, topRight.Point, alignmentPattern.Point];

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

    private static bool TryComputeDimension(
        BbQrFinderPattern topLeft,
        BbQrFinderPattern topRight,
        BbQrFinderPattern bottomLeft,
        float moduleSize,
        out int dimension)
    {
        var tlTr = BbMathUtils.Round(BbMathUtils.Distance(topLeft.X, topLeft.Y, topRight.X, topRight.Y) / moduleSize);
        var tlBl = BbMathUtils.Round(BbMathUtils.Distance(topLeft.X, topLeft.Y, bottomLeft.X, bottomLeft.Y) / moduleSize);
        dimension = ((tlTr + tlBl) / 2) + 7;

        // Every legal QR dimension is congruent to 1 modulo 4, so a result that is off by one can
        // be snapped; being off by two means the estimate is not trustworthy at all.
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
