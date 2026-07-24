// Abstract byte source for the lexer and parser.

namespace Bit.BlazorUI;

/// <summary>
/// Abstract byte source consumed by the lexer and parser: a movable read
/// position bounded by
/// <see cref="Start"/>..<see cref="End"/>, with single-byte and range reads.
/// Decoded streams (Flate, etc.) and the cross-reference reader all build on it.
/// </summary>
public abstract class BitPdfBaseStream
{
    /// <summary>Inclusive lower bound of the readable window.</summary>
    public int Start { get; protected set; }

    /// <summary>Exclusive upper bound of the readable window.</summary>
    public int End { get; protected set; }

    /// <summary>Current read position.</summary>
    public int Pos { get; set; }

    /// <summary>The stream dictionary, when this stream is a PDF stream object.</summary>
    public BitPdfDict? Dict { get; set; }

    /// <summary>Number of bytes between <see cref="Start"/> and <see cref="End"/>.</summary>
    public int Length => End - Start;

    /// <summary><c>true</c> when the readable window is empty.</summary>
    public bool IsEmpty => Length == 0;

    /// <summary>Reads the next byte, advancing <see cref="Pos"/>; returns -1 at end.</summary>
    public abstract int GetByte();

    /// <summary>
    /// Reads up to <paramref name="length"/> bytes (or to the end when
    /// <paramref name="length"/> &lt;= 0), advancing <see cref="Pos"/>.
    /// </summary>
    public abstract byte[] GetBytes(int length = 0);

    /// <summary>Returns the bytes in the absolute range [begin, end) without moving <see cref="Pos"/>.</summary>
    public abstract byte[] GetByteRange(int begin, int end);

    /// <summary>Reads the next byte without advancing <see cref="Pos"/>; returns -1 at end.</summary>
    public virtual int PeekByte()
    {
        int b = GetByte();
        if (b != -1)
        {
            Pos--;
        }
        return b;
    }

    /// <summary>Reads up to <paramref name="length"/> bytes without advancing <see cref="Pos"/>.</summary>
    public virtual byte[] PeekBytes(int length)
    {
        var bytes = GetBytes(length);
        Pos -= bytes.Length;
        return bytes;
    }

    /// <summary>Reads a big-endian unsigned 16-bit integer, or -1 at end.</summary>
    public int GetUint16()
    {
        int b0 = GetByte();
        int b1 = GetByte();
        if (b0 == -1 || b1 == -1)
        {
            return -1;
        }
        return (b0 << 8) + b1;
    }

    /// <summary>Reads a big-endian signed 32-bit integer, or -1 at end.</summary>
    public int GetInt32()
    {
        int b0 = GetByte(), b1 = GetByte(), b2 = GetByte(), b3 = GetByte();
        if (b0 == -1 || b1 == -1 || b2 == -1 || b3 == -1)
        {
            return -1;
        }
        return (b0 << 24) + (b1 << 16) + (b2 << 8) + b3;
    }

    /// <summary>Advances <see cref="Pos"/> by <paramref name="n"/> (default 1).</summary>
    public virtual void Skip(int n = 1) => Pos += n;

    /// <summary>Resets <see cref="Pos"/> back to <see cref="Start"/>.</summary>
    public abstract void Reset();

    /// <summary>Moves <see cref="Start"/> to the current <see cref="Pos"/>.</summary>
    public abstract void MoveStart();

    /// <summary>Creates a view over a sub-range, optionally carrying a stream dictionary.</summary>
    public abstract BitPdfBaseStream MakeSubStream(int start, int length, BitPdfDict? dict = null);
}
