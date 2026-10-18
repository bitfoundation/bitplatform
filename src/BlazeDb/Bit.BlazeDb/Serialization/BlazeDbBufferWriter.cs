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

    private void Ensure(int count)
    {
        if (_pos + count > _buffer.Length)
        {
            var newSize = Math.Max(_buffer.Length * 2, _pos + count);
            Array.Resize(ref _buffer, newSize);
        }
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

    // Shifted as an unsigned 64-bit value: field numbers run to 2^29 - 1, and shifting those in an
    // int would overflow into the sign bit and encode a different field.
    public void WriteTag(int fieldNumber, BlazeDbWireType wireType) =>
        WriteVarUInt(((ulong)(uint)fieldNumber << 3) | (uint)wireType);
}
