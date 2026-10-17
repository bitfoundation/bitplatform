using System.Buffers.Binary;
using System.Text;

namespace Bit.BlazeDb.Serialization;

/// <summary>
/// Span-based binary reader mirroring <see cref="BlazeDbBufferWriter"/>. A ref struct so rows can be
/// decoded with zero intermediate allocations.
/// </summary>
public ref struct BlazeDbBufferReader
{
    private readonly ReadOnlySpan<byte> _data;
    private int _pos;

    public BlazeDbBufferReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _pos = 0;
    }

    public int Position => _pos;

    public int Remaining => _data.Length - _pos;

    public byte ReadByte()
    {
        CheckAvailable(1);
        return _data[_pos++];
    }

    public ReadOnlySpan<byte> ReadRaw(int count)
    {
        CheckAvailable(count);
        var span = _data.Slice(_pos, count);
        _pos += count;
        return span;
    }

    public ulong ReadVarUInt()
    {
        ulong result = 0;
        var shift = 0;
        while (true)
        {
            CheckAvailable(1);
            var b = _data[_pos++];
            result |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                return result;
            }
            shift += 7;
            if (shift >= 64)
            {
                throw new InvalidDataException("Malformed varint.");
            }
        }
    }

    public long ReadVarInt()
    {
        var value = ReadVarUInt();
        return (long)(value >> 1) ^ -(long)(value & 1);
    }

    public uint ReadFixed32()
    {
        CheckAvailable(4);
        var value = BinaryPrimitives.ReadUInt32LittleEndian(_data.Slice(_pos));
        _pos += 4;
        return value;
    }

    public ulong ReadFixed64()
    {
        CheckAvailable(8);
        var value = BinaryPrimitives.ReadUInt64LittleEndian(_data.Slice(_pos));
        _pos += 8;
        return value;
    }

    public bool ReadBool() => ReadVarUInt() != 0;

    public float ReadSingle() => BitConverter.UInt32BitsToSingle(ReadFixed32());

    public double ReadDouble() => BitConverter.UInt64BitsToDouble(ReadFixed64());

    public string ReadString()
    {
        var length = ReadLength();
        return Encoding.UTF8.GetString(ReadRaw(length));
    }

    /// <summary>Reads a length-delimited byte block.</summary>
    public ReadOnlySpan<byte> ReadBytes()
    {
        var length = ReadLength();
        return ReadRaw(length);
    }

    public Guid ReadGuid() => new(ReadFixedBlock(16));

    public decimal ReadDecimal()
    {
        var span = ReadFixedBlock(16);
        Span<int> bits = stackalloc int[4];
        for (var i = 0; i < 4; i++)
        {
            bits[i] = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(i * 4));
        }
        try
        {
            return new decimal(bits);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidDataException("Malformed decimal.", ex);
        }
    }

    public DateTime ReadDateTime()
    {
        var binary = (long)ReadFixed64();
        try
        {
            return DateTime.FromBinary(binary);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidDataException("Malformed DateTime.", ex);
        }
    }

    public DateTimeOffset ReadDateTimeOffset()
    {
        var span = ReadFixedBlock(16);
        var ticks = BinaryPrimitives.ReadInt64LittleEndian(span);
        var offsetTicks = BinaryPrimitives.ReadInt64LittleEndian(span.Slice(8));
        try
        {
            return new DateTimeOffset(ticks, new TimeSpan(offsetTicks));
        }
        catch (ArgumentException ex)
        {
            throw new InvalidDataException("Malformed DateTimeOffset.", ex);
        }
    }

    public TimeSpan ReadTimeSpan() => new((long)ReadFixed64());

    /// <summary>A length prefix; anything longer than what remains is malformed, which also rules out overflow.</summary>
    private int ReadLength()
    {
        var length = ReadVarUInt();
        if (length > (ulong)Remaining)
        {
            throw new InvalidDataException("Length prefix exceeds the remaining buffer.");
        }
        return (int)length;
    }

    public (int FieldNumber, BlazeDbWireType WireType) ReadTag()
    {
        var tag = ReadVarUInt();
        return ((int)(tag >> 3), (BlazeDbWireType)(tag & 0x7));
    }

    /// <summary>Skips a field value of the given wire type (unknown field tolerance).</summary>
    public void SkipField(BlazeDbWireType wireType)
    {
        switch (wireType)
        {
            case BlazeDbWireType.VarInt:
                ReadVarUInt();
                break;
            case BlazeDbWireType.Fixed64:
                ReadRaw(8);
                break;
            case BlazeDbWireType.LengthDelimited:
                ReadRaw(ReadLength());
                break;
            case BlazeDbWireType.Fixed32:
                ReadRaw(4);
                break;
            default:
                throw new InvalidDataException($"Unknown wire type {wireType}.");
        }
    }

    /// <summary>
    /// Reads a length-delimited block that must be exactly <paramref name="expected"/> bytes, so a
    /// corrupt length surfaces as <see cref="InvalidDataException"/> like every other decode error
    /// rather than as an argument error from the value constructor.
    /// </summary>
    private ReadOnlySpan<byte> ReadFixedBlock(int expected)
    {
        var block = ReadBytes();
        if (block.Length != expected)
        {
            throw new InvalidDataException($"Expected a {expected}-byte value but the block is {block.Length} bytes.");
        }
        return block;
    }

    private readonly void CheckAvailable(int count)
    {
        if (_pos + count > _data.Length)
        {
            throw new InvalidDataException("Unexpected end of buffer.");
        }
    }
}
