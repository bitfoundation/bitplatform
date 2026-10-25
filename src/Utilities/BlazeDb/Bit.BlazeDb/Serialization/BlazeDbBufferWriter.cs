using System.Buffers.Binary;
using System.Text;

namespace Bit.BlazeDb.Serialization;

/// <summary>
/// Growable binary writer used for rows, keys, WAL records and snapshots.
/// Instances are reused (see <see cref="Reset"/>); not thread-safe.
/// </summary>
public sealed class BlazeDbBufferWriter
{
    private byte[] _buffer;
    private int _pos;

    public BlazeDbBufferWriter(int initialCapacity = 256)
    {
        _buffer = new byte[Math.Max(16, initialCapacity)];
    }

    public int Length => _pos;

    public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _pos);

    public ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _pos);

    public byte[] ToArray() => WrittenSpan.ToArray();

    public void Reset() => _pos = 0;

    /// <summary>Drops everything written after the first <paramref name="length"/> bytes.</summary>
    internal void Truncate(int length)
    {
        if ((uint)length > (uint)_pos)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }
        _pos = length;
    }

    /// <summary>Overwrites four already-written bytes at <paramref name="position"/>.</summary>
    internal void PatchFixed32(int position, uint value) =>
        BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(position, Math.Max(0, Math.Min(4, _pos - position))), value);

    /// <summary>Overwrites eight already-written bytes at <paramref name="position"/>.</summary>
    internal void PatchFixed64(int position, ulong value) =>
        BinaryPrimitives.WriteUInt64LittleEndian(_buffer.AsSpan(position, Math.Max(0, Math.Min(8, _pos - position))), value);

    private void Ensure(int count)
    {
        // Compared as a difference, so a buffer near the 2 GB limit cannot overflow the check.
        if (count > _buffer.Length - _pos)
        {
            Grow(count);
        }
    }

    /// <summary>
    /// Doubles the buffer, or grows it to exactly what is needed when doubling is not enough. Sizes are
    /// worked out in 64 bits: past 1 GB a doubling no longer fits an <see cref="int"/>, and past
    /// <see cref="Array.MaxLength"/> nothing does, which is refused in so many words rather than left
    /// to surface as a copy into a buffer that never grew.
    /// </summary>
    private void Grow(int count)
    {
        var required = (long)_pos + count;
        if (count < 0 || required > Array.MaxLength)
        {
            throw new BlazeDbException(
                $"A buffer cannot hold {required} bytes: the most a single .NET array can hold is {Array.MaxLength}. " +
                "Split the data being written - a transaction, a snapshot or a stored file - into smaller parts.");
        }
        var doubled = Math.Min((long)_buffer.Length * 2, Array.MaxLength);
        Array.Resize(ref _buffer, (int)Math.Max(doubled, required));
    }

    public void WriteByte(byte value)
    {
        Ensure(1);
        _buffer[_pos++] = value;
    }

    public void WriteRaw(ReadOnlySpan<byte> bytes)
    {
        Ensure(bytes.Length);
        bytes.CopyTo(_buffer.AsSpan(_pos));
        _pos += bytes.Length;
    }

    public void WriteVarUInt(ulong value)
    {
        Ensure(10);
        while (value >= 0x80)
        {
            _buffer[_pos++] = (byte)(value | 0x80);
            value >>= 7;
        }
        _buffer[_pos++] = (byte)value;
    }

    /// <summary>ZigZag-encoded signed varint (small negative numbers stay small).</summary>
    public void WriteVarInt(long value) => WriteVarUInt((ulong)((value << 1) ^ (value >> 63)));

    public void WriteFixed32(uint value)
    {
        Ensure(4);
        BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(_pos), value);
        _pos += 4;
    }

    public void WriteFixed64(ulong value)
    {
        Ensure(8);
        BinaryPrimitives.WriteUInt64LittleEndian(_buffer.AsSpan(_pos), value);
        _pos += 8;
    }

    public void WriteBool(bool value) => WriteVarUInt(value ? 1UL : 0UL);

    public void WriteSingle(float value) => WriteFixed32(BitConverter.SingleToUInt32Bits(value));

    public void WriteDouble(double value) => WriteFixed64(BitConverter.DoubleToUInt64Bits(value));

    /// <summary>Length-delimited UTF-8 string.</summary>
    public void WriteString(string value)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        WriteVarUInt((ulong)byteCount);
        Ensure(byteCount);
        Encoding.UTF8.GetBytes(value, _buffer.AsSpan(_pos));
        _pos += byteCount;
    }

    /// <summary>Length-delimited raw bytes.</summary>
    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        WriteVarUInt((ulong)bytes.Length);
        WriteRaw(bytes);
    }

    /// <summary>Length-delimited 16-byte GUID.</summary>
    public void WriteGuid(Guid value)
    {
        WriteVarUInt(16);
        Ensure(16);
        value.TryWriteBytes(_buffer.AsSpan(_pos));
        _pos += 16;
    }

    /// <summary>Length-delimited 16-byte decimal.</summary>
    public void WriteDecimal(decimal value)
    {
        WriteVarUInt(16);
        Ensure(16);
        Span<int> bits = stackalloc int[4];
        decimal.GetBits(value, bits);
        var span = _buffer.AsSpan(_pos);
        for (var i = 0; i < 4; i++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(i * 4), bits[i]);
        }
        _pos += 16;
    }

    /// <summary>Fixed64 via <see cref="DateTime.ToBinary"/> (preserves Kind).</summary>
    public void WriteDateTime(DateTime value) => WriteFixed64((ulong)value.ToBinary());

    /// <summary>Length-delimited 16 bytes: clock ticks (in the offset's local time) + offset ticks.</summary>
    public void WriteDateTimeOffset(DateTimeOffset value)
    {
        WriteVarUInt(16);
        Ensure(16);
        var span = _buffer.AsSpan(_pos);
        BinaryPrimitives.WriteInt64LittleEndian(span, value.Ticks);
        BinaryPrimitives.WriteInt64LittleEndian(span.Slice(8), value.Offset.Ticks);
        _pos += 16;
    }

    public void WriteTimeSpan(TimeSpan value) => WriteFixed64((ulong)value.Ticks);

    /// <summary>ZigZag varint of <see cref="DateOnly.DayNumber"/>.</summary>
    public void WriteDateOnly(DateOnly value) => WriteVarInt(value.DayNumber);

    /// <summary>ZigZag varint of <see cref="TimeOnly.Ticks"/>.</summary>
    public void WriteTimeOnly(TimeOnly value) => WriteVarInt(value.Ticks);

    /// <summary>Varint of the value's 16 IEEE 754 bits.</summary>
    public void WriteHalf(Half value) => WriteVarUInt(BitConverter.HalfToUInt16Bits(value));

    /// <summary>Length-delimited 16 bytes, little-endian two's complement.</summary>
    public void WriteInt128(Int128 value)
    {
        WriteVarUInt(16);
        Ensure(16);
        BinaryPrimitives.WriteInt128LittleEndian(_buffer.AsSpan(_pos), value);
        _pos += 16;
    }

    /// <summary>Length-delimited 16 bytes, little-endian.</summary>
    public void WriteUInt128(UInt128 value)
    {
        WriteVarUInt(16);
        Ensure(16);
        BinaryPrimitives.WriteUInt128LittleEndian(_buffer.AsSpan(_pos), value);
        _pos += 16;
    }

    // Shifted as an unsigned 64-bit value: field numbers run to 2^29 - 1, and shifting those in an
    // int would overflow into the sign bit and encode a different field.
    public void WriteTag(int fieldNumber, BlazeDbWireType wireType) =>
        WriteVarUInt(((ulong)(uint)fieldNumber << 3) | (uint)wireType);
}
