namespace Bit.BbScanner;

/// <summary>
/// Locates the alignment pattern nearest the bottom-right corner of a QR symbol.
/// </summary>
/// <remarks>
/// The alignment pattern is a small 1:1:1 concentric square. On its own it is far too common a
/// shape to search for across a whole image, so this finder is only ever pointed at the small
/// region where the geometry of the three finder patterns says it must be. Finding it is what
/// upgrades the sampling grid from an affine approximation to a true perspective mapping, and
/// therefore what makes larger symbols readable at an angle.
/// </remarks>
public sealed class BbQrAlignmentPatternFinder
{
    private readonly BbBitMatrix _image;
    private readonly int _startX;
    private readonly int _startY;
    private readonly int _width;
    private readonly int _height;
    private readonly float _moduleSize;
    private readonly List<BbQrFinderPattern> _possibleCenters = new(4);

    /// <summary>Creates a finder restricted to one region of the image.</summary>
    /// <param name="image">The binarised image.</param>
    /// <param name="startX">Left edge of the search region.</param>
    /// <param name="startY">Top edge of the search region.</param>
    /// <param name="width">Width of the search region.</param>
    /// <param name="height">Height of the search region.</param>
    /// <param name="moduleSize">Estimated module width in pixels.</param>
    public BbQrAlignmentPatternFinder(BbBitMatrix image, int startX, int startY, int width, int height, float moduleSize)
    {
        ArgumentNullException.ThrowIfNull(image);
        _image = image;
        _startX = startX;
        _startY = startY;
        _width = width;
        _height = height;
        _moduleSize = moduleSize;
    }

    /// <summary>Searches the region, returning the centre or <see langword="null"/>.</summary>
    public BbQrFinderPattern? Find()
    {
        var maxJ = _startX + _width;
        var middleI = _startY + (_height / 2);
        Span<int> stateCount = stackalloc int[3];

        // Rows are visited outwards from the middle of the region, because the estimate that
        // produced the region is centred on the true position.
        for (var iGen = 0; iGen < _height; iGen++)
        {
            var i = middleI + (((iGen & 0x01) == 0) ? (iGen + 1) / 2 : -((iGen + 1) / 2));
            if (i < 0 || i >= _image.Height)
            {
                continue;
            }

            stateCount.Clear();
            var j = _startX;
            while (j < maxJ && !_image[j, i])
            {
                j++;
            }

            var currentState = 0;
            while (j < maxJ)
            {
                if (_image[j, i])
                {
                    if (currentState == 1)
                    {
                        stateCount[1]++;
                    }
                    else if (currentState == 2)
                    {
                        if (FoundPatternCross(stateCount, _moduleSize))
                        {
                            var confirmed = HandlePossibleCenter(stateCount, i, j);
                            if (confirmed is not null)
                            {
                                return confirmed;
                            }
                        }

                        stateCount[0] = stateCount[2];
                        stateCount[1] = 1;
                        stateCount[2] = 0;
                        currentState = 1;
                    }
                    else
                    {
                        stateCount[++currentState]++;
                    }
                }
                else
                {
                    if (currentState == 1)
                    {
                        currentState++;
                    }

                    stateCount[currentState]++;
                }

                j++;
            }

            if (FoundPatternCross(stateCount, _moduleSize))
            {
                var confirmed = HandlePossibleCenter(stateCount, i, maxJ);
                if (confirmed is not null)
                {
                    return confirmed;
                }
            }
        }

        // A single unconfirmed sighting is as likely to be an isolated data module as the real
        // pattern, and a wrong alignment centre shears the entire grid. The detector copes with a
        // missing pattern by falling back to the affine transform, so reporting nothing is
        // strictly better than reporting a guess.
        return null;
    }

    private static bool FoundPatternCross(ReadOnlySpan<int> stateCount, float moduleSize)
    {
        var maxVariance = moduleSize / 2.0f;
        for (var i = 0; i < 3; i++)
        {
            if (Math.Abs(moduleSize - stateCount[i]) >= maxVariance)
            {
                return false;
            }
        }

        return true;
    }

    private static float CenterFromEnd(ReadOnlySpan<int> stateCount, int end) =>
        (end - stateCount[2]) - (stateCount[1] / 2.0f);

    private BbQrFinderPattern? HandlePossibleCenter(ReadOnlySpan<int> stateCount, int i, int j)
    {
        var stateCountTotal = stateCount[0] + stateCount[1] + stateCount[2];
        var centerJ = CenterFromEnd(stateCount, j);
        var centerI = CrossCheckVertical(i, (int)centerJ, 2 * stateCount[1], stateCountTotal);
        if (float.IsNaN(centerI))
        {
            return null;
        }

        var estimatedModuleSize = stateCountTotal / 3.0f;
        for (var index = 0; index < _possibleCenters.Count; index++)
        {
            var center = _possibleCenters[index];
            if (center.AboutEquals(estimatedModuleSize, centerI, centerJ))
            {
                return center.CombineEstimate(centerI, centerJ, estimatedModuleSize);
            }
        }

        _possibleCenters.Add(new BbQrFinderPattern(centerJ, centerI, estimatedModuleSize));
        return null;
    }

    private float CrossCheckVertical(int startI, int centerJ, int maxCount, int originalStateCountTotal)
    {
        var maxI = _image.Height;
        Span<int> stateCount = stackalloc int[3];

        var i = startI;
        while (i >= 0 && _image[centerJ, i] && stateCount[1] <= maxCount)
        {
            stateCount[1]++;
            i--;
        }

        if (i < 0 || stateCount[1] > maxCount)
        {
            return float.NaN;
        }

        while (i >= 0 && !_image[centerJ, i] && stateCount[0] <= maxCount)
        {
            stateCount[0]++;
            i--;
        }

        if (stateCount[0] > maxCount)
        {
            return float.NaN;
        }

        i = startI + 1;
        while (i < maxI && _image[centerJ, i] && stateCount[1] <= maxCount)
        {
            stateCount[1]++;
            i++;
        }

        if (i == maxI || stateCount[1] > maxCount)
        {
            return float.NaN;
        }

        while (i < maxI && !_image[centerJ, i] && stateCount[2] <= maxCount)
        {
            stateCount[2]++;
            i++;
        }

        if (stateCount[2] > maxCount)
        {
            return float.NaN;
        }

        var stateCountTotal = stateCount[0] + stateCount[1] + stateCount[2];
        if (5 * Math.Abs(stateCountTotal - originalStateCountTotal) >= 2 * originalStateCountTotal)
        {
            return float.NaN;
        }

        return FoundPatternCross(stateCount, _moduleSize) ? CenterFromEnd(stateCount, i) : float.NaN;
    }
}
