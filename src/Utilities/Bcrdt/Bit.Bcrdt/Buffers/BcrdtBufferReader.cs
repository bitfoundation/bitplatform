using System.Buffers.Binary;
using System.Text;

namespace Bit.Bcrdt;

/// <summary>
/// A bounds-checked binary reader over an immutable payload.
/// </summary>
/// <remarks>
/// <para>
/// Every read validates that the requested bytes exist and raises
/// <see cref="BcrdtFormatException"/> otherwise. That guarantee is what allows the sync
/// layer to accept updates from untrusted peers: a truncated or hostile payload fails loudly at the
/// decoder boundary rather than corrupting a document halfway through integration.
/// </para>
/// <para>
/// It wraps <see cref="ReadOnlyMemory{T}"/> rather than <see cref="ReadOnlySpan{T}"/> so decoder
/// classes can hold one as a field. It is a mutable struct - pass it by <see langword="ref"/>.
/// </para>
/// </remarks>
public struct BcrdtBufferReader
{
    private readonly ReadOnlyMemory<byte> _data;
    private int _position;

    /// <summary>Creates a reader positioned at the start of <paramref name="data"/>.</summary>
    public BcrdtBufferReader(ReadOnlyMemory<byte> data)
    {
        _data = data;
        _position = 0;
    }

    /// <summary>Creates a reader over an array.</summary>
    public BcrdtBufferReader(byte[] data) : this(data.AsMemory()) { }

    /// <summary>Current read offset.</summary>
    public readonly int Position => _position;

    /// <summary>Total payload length.</summary>
    public readonly int Length => _data.Length;

    /// <summary>Bytes not yet consumed.</summary>
    public readonly int Remaining => _data.Length - _position;

    /// <summary>Whether every byte has been consumed.</summary>
    public readonly bool IsAtEnd => _position >= _data.Length;

    /// <summary>The whole underlying payload.</summary>
    public readonly ReadOnlyMemory<byte> Buffer => _data;

    /// <summary>Reads one byte.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte()
    {
        if (_position >= _data.Length) throw BcrdtFormatException.Truncated("byte", 1, 0);
        return _data.Span[_position++];
    }

    /// <summary>Returns the next byte without consuming it.</summary>
    public readonly byte PeekByte()
    {
        if (_position >= _data.Length) throw BcrdtFormatException.Truncated("byte", 1, 0);
        return _data.Span[_position];
    }

    /// <summary>Reads <paramref name="count"/> raw bytes without copying.</summary>
    public ReadOnlyMemory<byte> ReadRaw(int count)
    {
        if (count < 0 || count > Remaining)
        {
            throw BcrdtFormatException.Truncated("payload", count, Remaining);
        }
        ReadOnlyMemory<byte> slice = _data.Slice(_position, count);
        _position += count;
        return slice;
    }

    /// <summary>Reads an unsigned LEB128 integer.</summary>
    /// <exception cref="BcrdtFormatException">
    /// The encoding runs past the end of the buffer, exceeds ten bytes, or carries bits beyond the
    /// 64th. All three checks matter for hostile input: without the length cap a crafted payload of
    /// continuation bytes would spin forever, and without the last one two different encodings would
    /// silently decode to the same value.
    /// </exception>
    public ulong ReadVarUInt()
    {
        ReadOnlySpan<byte> span = _data.Span;
        ulong result = 0;
        int shift = 0;
        int index = _position;

        while (true)
        {
            if (index >= span.Length) throw BcrdtFormatException.Truncated("varuint", 1, 0);
            byte b = span[index++];

            // The tenth byte contributes only the 64th bit; anything above it cannot be represented.
            if (shift == 63 && (b & 0x7E) != 0) throw new BcrdtFormatException("Varuint exceeds 64 bits.");

            result |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0) break;
            shift += 7;
            if (shift > 63) throw new BcrdtFormatException("Varuint exceeds 64 bits.");
        }

        _position = index;
        return result;
    }

    /// <summary>Reads a signed zigzag-LEB128 integer.</summary>
    public long ReadVarInt() => Unzigzag(ReadVarUInt());

    /// <summary>Reverses <see cref="BcrdtBufferWriter.Zigzag"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Unzigzag(ulong value) => (long)(value >> 1) ^ -(long)(value & 1);

    /// <summary>Reads an unsigned LEB128 integer that must fit in a non-negative <see cref="int"/>.</summary>
    public int ReadVarUInt32()
    {
        ulong value = ReadVarUInt();
        if (value > int.MaxValue) throw new BcrdtFormatException($"Length {value} exceeds Int32 range.");
        return (int)value;
    }

    /// <summary>Reads a length-prefixed UTF-8 string.</summary>
    public string ReadString()
    {
        int byteCount = ReadVarUInt32();
        if (byteCount > Remaining)
        {
            throw BcrdtFormatException.Truncated("string", byteCount, Remaining);
        }
        if (byteCount == 0) return string.Empty;
        string result = BcrdtWtf8.Decode(_data.Span.Slice(_position, byteCount));
        _position += byteCount;
        return result;
    }

    /// <summary>Reads a length-prefixed byte array.</summary>
    public byte[] ReadBytes()
    {
        int count = ReadVarUInt32();
        return ReadRaw(count).ToArray();
    }

    /// <summary>Reads a little-endian IEEE-754 double.</summary>
    public double ReadDouble()
    {
        ReadOnlyMemory<byte> slice = ReadRaw(8);
        return BinaryPrimitives.ReadDoubleLittleEndian(slice.Span);
    }

    /// <summary>Reads a little-endian IEEE-754 float.</summary>
    public float ReadSingle()
    {
        ReadOnlyMemory<byte> slice = ReadRaw(4);
        return BinaryPrimitives.ReadSingleLittleEndian(slice.Span);
    }

    /// <summary>Reads a fixed-width little-endian unsigned 32-bit integer.</summary>
    public uint ReadUInt32()
    {
        ReadOnlyMemory<byte> slice = ReadRaw(4);
        return BinaryPrimitives.ReadUInt32LittleEndian(slice.Span);
    }

    /// <summary>
    /// Validates a collection length against the bytes actually remaining, before anything is
    /// allocated for it.
    /// </summary>
    /// <remarks>
    /// A hostile payload can claim "this update contains two billion blocks" in five bytes. Without
    /// this check the decoder would try to size a list for it and exhaust memory. Every element
    /// costs at least one byte on the wire, so remaining-bytes is a sound upper bound.
    /// </remarks>
    public readonly int ValidateCount(int count, string what)
    {
        if (count < 0 || count > Remaining)
        {
            throw new BcrdtFormatException(
                $"Declared {what} count of {count} is impossible with {Remaining} byte(s) remaining.");
        }
        return count;
    }

    /// <summary>
    /// How much room to reserve for a collection of <paramref name="count"/> elements before any of
    /// them has been read.
    /// </summary>
    /// <remarks>
    /// <see cref="ValidateCount"/> bounds one count by the bytes left, but nested collections are each
    /// checked against the same remaining bytes, so a payload of a megabyte nested sixty levels deep
    /// could otherwise make the decoder reserve room for sixty megabytes' worth of elements before it
    /// noticed the lie. Reserving a bounded amount up front and growing as elements actually arrive
    /// keeps what a payload can make the decoder allocate proportional to its size.
    /// </remarks>
    public static int InitialCapacity(int count) => Math.Min(count, 256);
}
