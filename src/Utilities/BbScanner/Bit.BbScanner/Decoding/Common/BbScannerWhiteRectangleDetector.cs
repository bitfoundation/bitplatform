namespace Bit.BbScanner;

/// <summary>
/// Finds a quadrilateral of dark content by growing a small rectangle outwards from the centre
/// of the image until every edge is clear of ink.
/// </summary>
/// <remarks>
/// Data Matrix and PDF417 have no finder pattern that can be recognised from a single scan
/// line, so their detectors start by isolating the region that contains the symbol at all. This
/// grow-until-white search is cheap, tolerant of rotation, and makes no assumption about the
/// shape of the symbol inside.
/// </remarks>
public sealed class BbScannerWhiteRectangleDetector
{
    private const int InitSize = 10;
    private const int Corr = 1;

    private readonly BbScannerBitMatrix _image;
    private readonly int _height;
    private readonly int _width;
    private readonly int _leftInit;
    private readonly int _rightInit;
    private readonly int _downInit;
    private readonly int _upInit;

    private BbScannerWhiteRectangleDetector(BbScannerBitMatrix image, int initSize, int x, int y)
    {
        _image = image;
        _height = image.Height;
        _width = image.Width;

        var halfSize = initSize / 2;
        _leftInit = x - halfSize;
        _rightInit = x + halfSize;
        _upInit = y - halfSize;
        _downInit = y + halfSize;
    }

    /// <summary>Creates a detector centred on the image, or <see langword="null"/> when the image is too small.</summary>
    /// <param name="image">The binarised image.</param>
    public static BbScannerWhiteRectangleDetector? Create(BbScannerBitMatrix image)
    {
        ArgumentNullException.ThrowIfNull(image);
        return Create(image, InitSize, image.Width / 2, image.Height / 2);
    }

    /// <summary>Creates a detector centred on a given point.</summary>
    /// <param name="image">The binarised image.</param>
    /// <param name="initSize">Edge length of the starting rectangle.</param>
    /// <param name="x">Centre x.</param>
    /// <param name="y">Centre y.</param>
    public static BbScannerWhiteRectangleDetector? Create(BbScannerBitMatrix image, int initSize, int x, int y)
    {
        ArgumentNullException.ThrowIfNull(image);

        // Checked before allocating: this is called on every frame that finds no symbol, and on
        // a small image it fails every time.
        var halfSize = initSize / 2;
        if (x - halfSize < 0 || y - halfSize < 0 ||
            y + halfSize >= image.Height || x + halfSize >= image.Width)
        {
            return null;
        }

        return new BbScannerWhiteRectangleDetector(image, initSize, x, y);
    }

    /// <summary>
    /// Grows the rectangle until no edge touches ink, then finds one point of the enclosed shape
    /// on each of the four diagonals.
    /// </summary>
    /// <returns>Four points ordered as the corners of the enclosed quadrilateral, or <see langword="null"/>.</returns>
    public BbScannerScanPoint[]? Detect()
    {
        var left = _leftInit;
        var right = _rightInit;
        var up = _upInit;
        var down = _downInit;

        var sizeExceeded = false;
        var aBlackPointFoundOnBorder = true;

        var atLeastOneBlackPointFoundOnRight = false;
        var atLeastOneBlackPointFoundOnBottom = false;
        var atLeastOneBlackPointFoundOnLeft = false;
        var atLeastOneBlackPointFoundOnTop = false;

        while (aBlackPointFoundOnBorder)
        {
            aBlackPointFoundOnBorder = false;

            var rightBorderNotWhite = true;
            while ((rightBorderNotWhite || !atLeastOneBlackPointFoundOnRight) && right < _width)
            {
                rightBorderNotWhite = ContainsBlackPoint(up, down, right, horizontal: false);
                if (rightBorderNotWhite)
                {
                    right++;
                    aBlackPointFoundOnBorder = true;
                    atLeastOneBlackPointFoundOnRight = true;
                }
                else if (!atLeastOneBlackPointFoundOnRight)
                {
                    right++;
                }
            }

            if (right >= _width)
            {
                sizeExceeded = true;
                break;
            }

            var bottomBorderNotWhite = true;
            while ((bottomBorderNotWhite || !atLeastOneBlackPointFoundOnBottom) && down < _height)
            {
                bottomBorderNotWhite = ContainsBlackPoint(left, right, down, horizontal: true);
                if (bottomBorderNotWhite)
                {
                    down++;
                    aBlackPointFoundOnBorder = true;
                    atLeastOneBlackPointFoundOnBottom = true;
                }
                else if (!atLeastOneBlackPointFoundOnBottom)
                {
                    down++;
                }
            }

            if (down >= _height)
            {
                sizeExceeded = true;
                break;
            }

            var leftBorderNotWhite = true;
            while ((leftBorderNotWhite || !atLeastOneBlackPointFoundOnLeft) && left >= 0)
            {
                leftBorderNotWhite = ContainsBlackPoint(up, down, left, horizontal: false);
                if (leftBorderNotWhite)
                {
                    left--;
                    aBlackPointFoundOnBorder = true;
                    atLeastOneBlackPointFoundOnLeft = true;
                }
                else if (!atLeastOneBlackPointFoundOnLeft)
                {
                    left--;
                }
            }

            if (left < 0)
            {
                sizeExceeded = true;
                break;
            }

            var topBorderNotWhite = true;
            while ((topBorderNotWhite || !atLeastOneBlackPointFoundOnTop) && up >= 0)
            {
                topBorderNotWhite = ContainsBlackPoint(left, right, up, horizontal: true);
                if (topBorderNotWhite)
                {
                    up--;
                    aBlackPointFoundOnBorder = true;
                    atLeastOneBlackPointFoundOnTop = true;
                }
                else if (!atLeastOneBlackPointFoundOnTop)
                {
                    up--;
                }
            }

            if (up < 0)
            {
                sizeExceeded = true;
                break;
            }
        }

        if (sizeExceeded ||
            !atLeastOneBlackPointFoundOnRight || !atLeastOneBlackPointFoundOnBottom ||
            !atLeastOneBlackPointFoundOnLeft || !atLeastOneBlackPointFoundOnTop)
        {
            return null;
        }

        var maxSize = right - left;

        // Walk each corner diagonal inwards until it first touches the shape.
        BbScannerScanPoint? z = null;
        for (var i = 1; z is null && i < maxSize; i++)
        {
            z = GetBlackPointOnSegment(left, down - i, left + i, down);
        }

        if (z is null)
        {
            return null;
        }

        BbScannerScanPoint? t = null;
        for (var i = 1; t is null && i < maxSize; i++)
        {
            t = GetBlackPointOnSegment(left, up + i, left + i, up);
        }

        if (t is null)
        {
            return null;
        }

        BbScannerScanPoint? x = null;
        for (var i = 1; x is null && i < maxSize; i++)
        {
            x = GetBlackPointOnSegment(right, up + i, right - i, up);
        }

        if (x is null)
        {
            return null;
        }

        BbScannerScanPoint? y = null;
        for (var i = 1; y is null && i < maxSize; i++)
        {
            y = GetBlackPointOnSegment(right, down - i, right - i, down);
        }

        return y is null ? null : CenterEdges(y.Value, z.Value, x.Value, t.Value);
    }

    private BbScannerScanPoint? GetBlackPointOnSegment(float aX, float aY, float bX, float bY)
    {
        var dist = BbScannerMathUtils.Round(BbScannerMathUtils.Distance(aX, aY, bX, bY));
        if (dist == 0)
        {
            return null;
        }

        var xStep = (bX - aX) / dist;
        var yStep = (bY - aY) / dist;

        for (var i = 0; i <= dist; i++)
        {
            var x = BbScannerMathUtils.Round(aX + (i * xStep));
            var y = BbScannerMathUtils.Round(aY + (i * yStep));
            if (_image.GetSafe(x, y))
            {
                return new BbScannerScanPoint(x, y);
            }
        }

        return null;
    }

    /// <summary>
    /// Nudges the four found points half a module outwards so that they sit on the outside of
    /// the symbol rather than on its first dark module.
    /// </summary>
    private BbScannerScanPoint[] CenterEdges(BbScannerScanPoint y, BbScannerScanPoint z, BbScannerScanPoint x, BbScannerScanPoint t)
    {
        var yi = y.X;
        var yj = y.Y;
        var zi = z.X;
        var zj = z.Y;
        var xi = x.X;
        var xj = x.Y;
        var ti = t.X;
        var tj = t.Y;

        if (yi < _width / 2.0f)
        {
            return
            [
                new BbScannerScanPoint(ti - Corr, tj + Corr),
                new BbScannerScanPoint(zi + Corr, zj + Corr),
                new BbScannerScanPoint(xi - Corr, xj - Corr),
                new BbScannerScanPoint(yi + Corr, yj - Corr),
            ];
        }

        return
        [
            new BbScannerScanPoint(ti + Corr, tj + Corr),
            new BbScannerScanPoint(zi + Corr, zj - Corr),
            new BbScannerScanPoint(xi - Corr, xj + Corr),
            new BbScannerScanPoint(yi - Corr, yj - Corr),
        ];
    }

    private bool ContainsBlackPoint(int a, int b, int fixedCoordinate, bool horizontal)
    {
        if (horizontal)
        {
            for (var x = a; x <= b; x++)
            {
                if (_image.GetSafe(x, fixedCoordinate))
                {
                    return true;
                }
            }

            return false;
        }

        for (var y = a; y <= b; y++)
        {
            if (_image.GetSafe(fixedCoordinate, y))
            {
                return true;
            }
        }

        return false;
    }
}
