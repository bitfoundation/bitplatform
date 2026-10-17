using System.Numerics;
using System.Runtime.CompilerServices;

namespace Bit.BbScanner;

/// <summary>
/// A packed, reusable row of bits where a set bit means "black".
/// </summary>
/// <remarks>
/// Linear symbologies are decoded row by row. A scanner instance keeps one
/// <see cref="BbBitRow"/> alive and calls <see cref="Reset(int)"/> between rows, so scanning a
/// frame for one dimensional codes performs no allocations at all after warm-up.
/// </remarks>
public sealed class BbBitRow
{
    private uint[] _bits;

    /// <summary>Creates a row able to hold <paramref name="size"/> bits.</summary>
    /// <param name="size">Number of bits.</param>
    public BbBitRow(int size)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        Size = size;
        _bits = new uint[(size + 31) >> 5];
    }

    /// <summary>Number of bits in the row.</summary>
    public int Size { get; private set; }

    /// <summary>Clears the row and, if needed, grows it to hold <paramref name="size"/> bits.</summary>
    /// <param name="size">Required number of bits.</param>
    public void Reset(int size)
    {
        var words = (size + 31) >> 5;
        if (_bits.Length < words)
        {
            _bits = new uint[words];
        }
        else
        {
            Array.Clear(_bits, 0, words);
        }

        Size = size;
    }

    /// <summary>Reads or writes a single bit.</summary>
    /// <param name="i">Bit index.</param>
    public bool this[int i]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ((_bits[i >> 5] >> (i & 31)) & 1) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            ref var word = ref _bits[i >> 5];
            var mask = 1u << (i & 31);
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

    /// <summary>Index of the first set bit at or after <paramref name="from"/>, or <see cref="Size"/> if there is none.</summary>
    /// <param name="from">Index to start searching from.</param>
    public int GetNextSet(int from)
    {
        if (from >= Size)
        {
            return Size;
        }

        var word = from >> 5;
        var current = _bits[word] & ~((1u << (from & 31)) - 1);
        while (current == 0)
        {
            if (++word == ((Size + 31) >> 5))
            {
                return Size;
            }

            current = _bits[word];
        }

        return Math.Min(Size, (word << 5) + BitOperations.TrailingZeroCount(current));
    }

    /// <summary>Index of the first clear bit at or after <paramref name="from"/>, or <see cref="Size"/> if there is none.</summary>
    /// <param name="from">Index to start searching from.</param>
    public int GetNextUnset(int from)
    {
        if (from >= Size)
        {
            return Size;
        }

        var word = from >> 5;
        var current = ~_bits[word] & ~((1u << (from & 31)) - 1);
        while (current == 0)
        {
            if (++word == ((Size + 31) >> 5))
            {
                return Size;
            }

            current = ~_bits[word];
        }

        return Math.Min(Size, (word << 5) + BitOperations.TrailingZeroCount(current));
    }

    /// <summary>Checks whether every bit in <c>[start, end)</c> equals <paramref name="value"/>.</summary>
    /// <param name="start">Inclusive start index.</param>
    /// <param name="end">Exclusive end index.</param>
    /// <param name="value">Expected bit value.</param>
    public bool IsRange(int start, int end, bool value)
    {
        if (end < start || start < 0 || end > Size)
        {
            throw new ArgumentOutOfRangeException(nameof(end));
        }

        for (var i = start; i < end; i++)
        {
            if (this[i] != value)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Inverts every bit, used to retry a light-on-dark linear symbol.</summary>
    public void Invert()
    {
        var words = (Size + 31) >> 5;
        for (var i = 0; i < words; i++)
        {
            _bits[i] = ~_bits[i];
        }

        var trailing = Size & 31;
        if (trailing != 0)
        {
            _bits[words - 1] &= (1u << trailing) - 1;
        }
    }

    /// <summary>
    /// Reverses the row in place, used to read a linear symbol that was scanned right to left.
    /// </summary>
    /// <remarks>
    /// The swap is done bit by bit rather than by building a new buffer, because linear decoding
    /// reverses rows constantly and an allocation per reversal would show up directly in the
    /// frame budget.
    /// </remarks>
    public void Reverse()
    {
        var low = 0;
        var high = Size - 1;
        while (low < high)
        {
            var a = this[low];
            var b = this[high];
            if (a != b)
            {
                this[low] = b;
                this[high] = a;
            }

            low++;
            high--;
        }
    }
}
