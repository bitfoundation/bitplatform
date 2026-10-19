using System.Buffers.Binary;
using System.Text;

namespace Bit.Belemetry;

/// <summary>
/// A minimal protocol-buffers writer - just the wire types OTLP uses - so Belemetry needs neither Google.Protobuf
/// nor generated code.
/// </summary>
/// <remarks>
/// Nested messages are length-prefixed. Instead of computing sizes in a first pass, <see cref="BeginMessage"/>
/// reserves the largest length prefix a message can need, and <see cref="EndMessage"/> writes the real length and
/// shifts the body back over the unused prefix bytes. That keeps it a single pass with one buffer.
/// </remarks>
internal sealed class ProtobufWriter
{
    private const int WireVarint = 0;
    private const int WireFixed64 = 1;
    private const int WireLengthDelimited = 2;
    private const int WireFixed32 = 5;

    // A length prefix is a varint; 5 bytes encode any length below 2^35, far past any request size.
    private const int ReservedLengthBytes = 5;

    private byte[] _buffer;
    private int _position;

    public ProtobufWriter(int initialCapacity = 4096)
    {
        _buffer = new byte[Math.Max(64, initialCapacity)];
    }

    public int Length => _position;

    public byte[] ToArray() => _buffer.AsSpan(0, _position).ToArray();

    public void WriteString(int field, string? value)
    {
        value ??= string.Empty;
        WriteTag(field, WireLengthDelimited);
        var byteCount = Encoding.UTF8.GetByteCount(value);
        WriteVarint((ulong)byteCount);
        Ensure(byteCount);
        _position += Encoding.UTF8.GetBytes(value, _buffer.AsSpan(_position));
    }

    /// <summary>Writes the field only when <paramref name="value"/> is not empty (the proto3 default is omitted).</summary>
    public void WriteStringIfNotEmpty(int field, string? value)
    {
        if (string.IsNullOrEmpty(value) is false) WriteString(field, value);
    }

    public void WriteBytes(int field, ReadOnlySpan<byte> value)
    {
        WriteTag(field, WireLengthDelimited);
        WriteVarint((ulong)value.Length);
        Ensure(value.Length);
        value.CopyTo(_buffer.AsSpan(_position));
        _position += value.Length;
    }

    /// <summary>Writes a hex id (trace or span) as raw bytes; skipped when it is missing or malformed.</summary>
    public void WriteHexId(int field, string? hex, int byteLength)
    {
        if (hex is null || hex.Length != byteLength * 2) return;

        Span<byte> bytes = stackalloc byte[byteLength];
        if (BelemetryHex.TryDecode(hex, bytes)) WriteBytes(field, bytes);
    }

    public void WriteVarintField(int field, ulong value)
    {
        WriteTag(field, WireVarint);
        WriteVarint(value);
    }

    /// <summary>An <c>int64</c>/<c>int32</c>/enum field: negative values are written as 10-byte two's complement, per the spec.</summary>
    public void WriteInt64(int field, long value) => WriteVarintField(field, unchecked((ulong)value));

    public void WriteBool(int field, bool value) => WriteVarintField(field, value ? 1UL : 0UL);

    public void WriteFixed64(int field, ulong value)
    {
        WriteTag(field, WireFixed64);
        Ensure(8);
        BinaryPrimitives.WriteUInt64LittleEndian(_buffer.AsSpan(_position), value);
        _position += 8;
    }

    /// <summary>An <c>sfixed64</c> field.</summary>
    public void WriteSFixed64(int field, long value) => WriteFixed64(field, unchecked((ulong)value));

    public void WriteFixed32(int field, uint value)
    {
        WriteTag(field, WireFixed32);
        Ensure(4);
        BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(_position), value);
        _position += 4;
    }

    public void WriteDouble(int field, double value) => WriteFixed64(field, unchecked((ulong)BitConverter.DoubleToInt64Bits(value)));

    /// <summary>A packed <c>repeated fixed64</c> field.</summary>
    public void WritePackedFixed64(int field, ReadOnlySpan<long> values)
    {
        if (values.IsEmpty) return;

        WriteTag(field, WireLengthDelimited);
        WriteVarint((ulong)(values.Length * 8));
        Ensure(values.Length * 8);
        foreach (var value in values)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(_buffer.AsSpan(_position), unchecked((ulong)value));
            _position += 8;
        }
    }

    /// <summary>A packed <c>repeated double</c> field.</summary>
    public void WritePackedDouble(int field, ReadOnlySpan<double> values)
    {
        if (values.IsEmpty) return;

        WriteTag(field, WireLengthDelimited);
        WriteVarint((ulong)(values.Length * 8));
        Ensure(values.Length * 8);
        foreach (var value in values)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(_buffer.AsSpan(_position), unchecked((ulong)BitConverter.DoubleToInt64Bits(value)));
            _position += 8;
        }
    }

    /// <summary>Starts an embedded message; pass the returned token to <see cref="EndMessage"/>.</summary>
    public int BeginMessage(int field)
    {
        WriteTag(field, WireLengthDelimited);
        Ensure(ReservedLengthBytes);
        var start = _position;
        _position += ReservedLengthBytes;
        return start;
    }

    public void EndMessage(int token)
    {
        var bodyStart = token + ReservedLengthBytes;
        var bodyLength = _position - bodyStart;

        Span<byte> prefix = stackalloc byte[ReservedLengthBytes];
        var prefixLength = EncodeVarint((ulong)bodyLength, prefix);

        if (prefixLength < ReservedLengthBytes)
        {
            // Shift the body left over the unused reserved bytes.
            Buffer.BlockCopy(_buffer, bodyStart, _buffer, token + prefixLength, bodyLength);
        }

        prefix[..prefixLength].CopyTo(_buffer.AsSpan(token));
        _position = token + prefixLength + bodyLength;
    }

    private void WriteTag(int field, int wireType) => WriteVarint(((ulong)field << 3) | (uint)wireType);

    private void WriteVarint(ulong value)
    {
        Ensure(10);
        _position += EncodeVarint(value, _buffer.AsSpan(_position));
    }

    private static int EncodeVarint(ulong value, Span<byte> destination)
    {
        var i = 0;
        while (value >= 0x80)
        {
            destination[i++] = (byte)(value | 0x80);
            value >>= 7;
        }
        destination[i++] = (byte)value;
        return i;
    }

    private void Ensure(int additional)
    {
        if (_position + additional <= _buffer.Length) return;

        var size = Math.Max(_buffer.Length * 2, _position + additional);
        Array.Resize(ref _buffer, size);
    }
}
