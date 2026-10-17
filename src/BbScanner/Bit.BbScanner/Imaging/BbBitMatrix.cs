using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bit.BbScanner;

/// <summary>
/// A two dimensional bit plane where a set bit means "black".
/// </summary>
/// <remarks>
/// Bits are packed 32 to a <see cref="uint"/> word so a 1080p binarised frame costs 260 KB
/// instead of the 2 MB a <c>bool[]</c> would need. The packed layout also lets the detectors
/// skip whole runs of white with a single word comparison, which is what makes the finder
/// pattern search cheap enough to run on every frame.
/// </remarks>
public sealed class BbBitMatrix : IDisposable
{
    private uint[]? _bits;

    /// <summary>Creates a matrix with every bit clear.</summary>
    /// <param name="width">Width in bits.</param>
    /// <param name="height">Height in bits.</param>
    public BbBitMatrix(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        Width = width;
        Height = height;
        RowWords = (width + 31) >> 5;
        _bits = ArrayPool<uint>.Shared.Rent(RowWords * height);
        System.Array.Clear(_bits, 0, RowWords * height);
    }

    /// <summary>Width in bits.</summary>
    public int Width { get; }

    /// <summary>Height in bits.</summary>
    public int Height { get; }

    /// <summary>Number of 32 bit words per row.</summary>
    public int RowWords { get; }

    /// <summary>The packed backing store. Its length may exceed the matrix because it is pooled.</summary>
    public uint[] Bits
    {
        get
        {
            ObjectDisposedException.ThrowIf(_bits is null, this);
            return _bits;
        }
    }

    /// <summary>Reads the bit at the given coordinate.</summary>
    /// <param name="x">Column.</param>
    /// <param name="y">Row.</param>
    public bool this[int x, int y]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Debug.Assert((uint)x < (uint)Width && (uint)y < (uint)Height);
            return ((_bits![(y * RowWords) + (x >> 5)] >> (x & 31)) & 1) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            Debug.Assert((uint)x < (uint)Width && (uint)y < (uint)Height);
            ref var word = ref _bits![(y * RowWords) + (x >> 5)];
            var mask = 1u << (x & 31);
            if (value)
            {
                word |= mask;
            }
            else
            {
                word &= ~mask;
            }
        }
    }

    /// <summary>Reads a bit, returning <see langword="false"/> for coordinates outside the matrix.</summary>
    /// <param name="x">Column.</param>
    /// <param name="y">Row.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetSafe(int x, int y) =>
        (uint)x < (uint)Width && (uint)y < (uint)Height && this[x, y];

    /// <summary>Returns the packed words of one row.</summary>
    /// <param name="y">Row index.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<uint> GetRowWords(int y)
    {
        Debug.Assert((uint)y < (uint)Height);
        return _bits.AsSpan(y * RowWords, RowWords);
    }

    /// <summary>Sets every bit in a rectangle.</summary>
    /// <param name="left">Left edge, inclusive.</param>
    /// <param name="top">Top edge, inclusive.</param>
    /// <param name="width">Rectangle width.</param>
    /// <param name="height">Rectangle height.</param>
    public void SetRegion(int left, int top, int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(left);
        ArgumentOutOfRangeException.ThrowIfNegative(top);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (top + height > Height || left + width > Width)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "The region lies outside the matrix.");
        }

        for (var y = top; y < top + height; y++)
        {
            for (var x = left; x < left + width; x++)
            {
                this[x, y] = true;
            }
        }
    }

    /// <summary>Clears every bit.</summary>
    public void Clear() => System.Array.Clear(_bits!, 0, RowWords * Height);

    /// <summary>Inverts every bit inside the matrix bounds.</summary>
    public void Invert()
    {
        var trailing = Width & 31;
        var lastMask = trailing == 0 ? uint.MaxValue : (1u << trailing) - 1;
        for (var y = 0; y < Height; y++)
        {
            var row = GetRowWords(y);
            for (var w = 0; w < row.Length; w++)
            {
                row[w] = ~row[w];
            }

            row[^1] &= lastMask;
        }
    }

    /// <summary>Returns a copy of the sub-rectangle as a new matrix.</summary>
    /// <param name="left">Left edge, inclusive.</param>
    /// <param name="top">Top edge, inclusive.</param>
    /// <param name="width">Rectangle width.</param>
    /// <param name="height">Rectangle height.</param>
    public BbBitMatrix Crop(int left, int top, int width, int height)
    {
        var result = new BbBitMatrix(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (this[left + x, top + y])
                {
                    result[x, y] = true;
                }
            }
        }

        return result;
    }

    /// <summary>Returns a copy rotated 180 degrees, used to retry decoding an upside down symbol.</summary>
    public BbBitMatrix Rotate180()
    {
        var result = new BbBitMatrix(Width, Height);
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                if (this[x, y])
                {
                    result[Width - 1 - x, Height - 1 - y] = true;
                }
            }
        }

        return result;
    }

    /// <summary>Returns a copy rotated 90 degrees counter clockwise.</summary>
    public BbBitMatrix Rotate90()
    {
        var result = new BbBitMatrix(Height, Width);
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                if (this[x, y])
                {
                    result[y, Width - 1 - x] = true;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Finds the tightest bounding box of set bits as <c>[left, top, width, height]</c>,
    /// or <see langword="null"/> when the matrix is empty.
    /// </summary>
    public int[]? GetEnclosingRectangle()
    {
        int left = Width, top = Height, right = -1, bottom = -1;
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                if (!this[x, y])
                {
                    continue;
                }

                if (x < left) left = x;
                if (x > right) right = x;
                if (y < top) top = y;
                if (y > bottom) bottom = y;
            }
        }

        return right < left ? null : [left, top, right - left + 1, bottom - top + 1];
    }

    /// <summary>Renders the matrix as text, with <c>##</c> for set bits. Intended for tests and diagnostics.</summary>
    public override string ToString()
    {
        var sb = new StringBuilder((Width + 1) * Height * 2);
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                sb.Append(this[x, y] ? "##" : "  ");
            }

            sb.Append('\n');
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        var bits = _bits;
        _bits = null;
        if (bits is not null)
        {
            ArrayPool<uint>.Shared.Return(bits);
        }
    }
}
