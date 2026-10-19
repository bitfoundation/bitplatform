using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace Bit.Bcrdt;

/// <summary>
/// A growable, pooled binary writer used by every encoder in the stack.
/// </summary>
/// <remarks>
/// <para>
/// This is a mutable <see langword="struct"/> that rents its backing array from
/// <see cref="ArrayPool{T}"/>. It exists because encoding is the hottest allocation path in the
/// system: a document that fans out one update per keystroke would otherwise churn a
/// <c>MemoryStream</c>, an internal buffer and a <c>ToArray</c> copy for every character typed.
/// </para>
/// <para>
/// Because it is a mutable struct, <b>never copy it</b> - pass it by <see langword="ref"/> and store
/// it in exactly one place. Copying produces two writers that believe they own the same rented
/// array. Call <see cref="Dispose"/> to return the array to the pool; failing to do so is not a
/// correctness bug, only a lost pooling opportunity.
/// </para>
/// </remarks>
public struct BcrdtBufferWriter : IDisposable
{
    private const int DefaultCapacity = 256;
    private const int MaxPooledSize = 1 << 20;

    private byte[]? _buffer;
    private int _length;
    private bool _rented;

    /// <summary>Creates a writer with an initial capacity hint.</summary>
    /// <param name="capacity">Expected payload size. A good hint avoids all regrowth.</param>
    public BcrdtBufferWriter(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        if (capacity == 0)
        {
            _buffer = null;
            _rented = false;
        }
        else
        {
            _buffer = ArrayPool<byte>.Shared.Rent(capacity);
            _rented = true;
        }
        _length = 0;
    }

    /// <summary>Number of bytes written so far.</summary>
    public readonly int Length => _length;

    /// <summary>The bytes written so far, without copying.</summary>
    public readonly ReadOnlySpan<byte> WrittenSpan =>
        _buffer is null ? ReadOnlySpan<byte>.Empty : _buffer.AsSpan(0, _length);

    /// <summary>Copies the written bytes into a new array.</summary>
    public readonly byte[] ToArray() => WrittenSpan.ToArray();

    /// <summary>Ensures at least <paramref name="count"/> more bytes can be written without regrowth.</summary>
    public void EnsureCapacity(int count)
    {
        if (_buffer is null || _length + count > _buffer.Length)
        {
            Grow(count);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int count)
    {
        int required = _length + count;
        int newSize = Math.Max(required, _buffer is null ? DefaultCapacity : _buffer.Length * 2);
        byte[] next = ArrayPool<byte>.Shared.Rent(newSize);
        if (_buffer is not null)
        {
            _buffer.AsSpan(0, _length).CopyTo(next);
            if (_rented) ArrayPool<byte>.Shared.Return(_buffer);
        }
        _buffer = next;
        _rented = true;
    }

    /// <summary>Writes a single raw byte.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value)
    {
        if (_buffer is null || _length >= _buffer.Length) Grow(1);
        _buffer![_length++] = value;
    }

    /// <summary>Writes raw bytes verbatim, with no length prefix.</summary>
    public void WriteRaw(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty) return;
        EnsureCapacity(value.Length);
        value.CopyTo(_buffer.AsSpan(_length));
        _length += value.Length;
    }

    /// <summary>
    /// Writes an unsigned integer in LEB128 form: seven payload bits per byte, high bit set on every
    /// byte but the last. Small values - which dominate, because clocks are delta-encoded - cost one byte.
    /// </summary>
    public void WriteVarUInt(ulong value)
    {
        EnsureCapacity(10);
        byte[] buffer = _buffer!;
        int index = _length;
        while (value >= 0x80)
        {
            buffer[index++] = (byte)(value | 0x80);
            value >>= 7;
        }
        buffer[index++] = (byte)value;
        _length = index;
    }

    /// <summary>
    /// Writes a signed integer as zigzag-then-LEB128, so that small magnitudes of either sign cost
    /// one byte rather than ten.
    /// </summary>
    public void WriteVarInt(long value) => WriteVarUInt(Zigzag(value));

    /// <summary>Encodes a signed value so that its magnitude, not its sign, determines its size.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Zigzag(long value) => (ulong)((value << 1) ^ (value >> 63));

    /// <summary>
    /// Writes a length-prefixed string, preserving unpaired surrogates exactly.
    /// </summary>
    /// <remarks>
    /// Measures before writing so the length prefix is exact; the second pass is far cheaper than
    /// reserving a speculative prefix and shifting the payload afterwards. See <see cref="BcrdtWtf8"/>
    /// for why strict UTF-8 is not sufficient here.
    /// </remarks>
    public void WriteString(string value)
    {
        int byteCount = BcrdtWtf8.GetByteCount(value, out bool isStrictUtf8);
        WriteVarUInt((ulong)byteCount);
        EnsureCapacity(byteCount);

        Span<byte> destination = _buffer.AsSpan(_length, byteCount);
        if (isStrictUtf8) Encoding.UTF8.GetBytes(value, destination);
        else BcrdtWtf8.Encode(value, destination);

        _length += byteCount;
    }

    /// <summary>Writes a length-prefixed byte array.</summary>
    public void WriteBytes(ReadOnlySpan<byte> value)
    {
        WriteVarUInt((ulong)value.Length);
        WriteRaw(value);
    }

    /// <summary>Writes an IEEE-754 double in little-endian order.</summary>
    public void WriteDouble(double value)
    {
        EnsureCapacity(8);
        BinaryPrimitives.WriteDoubleLittleEndian(_buffer.AsSpan(_length), value);
        _length += 8;
    }

    /// <summary>Writes an IEEE-754 float in little-endian order.</summary>
    public void WriteSingle(float value)
    {
        EnsureCapacity(4);
        BinaryPrimitives.WriteSingleLittleEndian(_buffer.AsSpan(_length), value);
        _length += 4;
    }

    /// <summary>Writes a fixed-width little-endian unsigned 32-bit integer.</summary>
    public void WriteUInt32(uint value)
    {
        EnsureCapacity(4);
        BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(_length), value);
        _length += 4;
    }

    /// <summary>Resets the writer for reuse without returning its buffer to the pool.</summary>
    public void Reset() => _length = 0;

    /// <summary>Returns the rented buffer to the pool.</summary>
    public void Dispose()
    {
        if (_rented && _buffer is not null && _buffer.Length <= MaxPooledSize)
        {
            ArrayPool<byte>.Shared.Return(_buffer);
        }
        _buffer = null;
        _length = 0;
        _rented = false;
    }
}
