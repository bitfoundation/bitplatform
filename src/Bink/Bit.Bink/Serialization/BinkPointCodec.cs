using System.Buffers.Binary;

namespace Bit.Bink;

/// <summary>
/// Compact binary encoding of stroke points. Coordinates are quantized to a fixed grid and delta-encoded as
/// zigzag varints, which typically stores a handwriting point in 3 to 5 bytes instead of the 30 or more a JSON
/// number array would use.
/// </summary>
/// <remarks>
/// <para>Layout: <c>[version:1][flags:1][quantization:varint][count:varint][channel blocks]</c>.</para>
/// <para>
/// Channel blocks in order: X, Y (zigzag varint deltas of quantized values), pressure (one byte, 0..255), time
/// (varint deltas in milliseconds), then tilt X/Y (one signed byte of degrees each) and twist (one byte of 1.42-degree
/// steps) when the corresponding flag is set. The encoding is deterministic: the same points always produce the
/// same bytes, which makes documents diffable and comparable.
/// </para>
/// </remarks>
public static class BinkPointCodec
{
    /// <summary>Current codec version.</summary>
    public const byte Version = 1;

    /// <summary>Default quantization: 64 steps per document unit (about 0.016 units of precision).</summary>
    public const int DefaultQuantization = 64;

    [Flags]
    private enum Flags : byte
    {
        None = 0,
        Tilt = 1,
        Twist = 2,
    }

    /// <summary>Encodes points into a byte array.</summary>
    /// <param name="points">Points to encode.</param>
    /// <param name="quantization">Steps per document unit. Higher keeps more precision and uses more space.</param>
    public static byte[] Encode(BinkStrokePoints points, int quantization = DefaultQuantization)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (quantization < 1) quantization = 1;
        int n = points.Count;
        var buffer = new ArrayBufferWriter(Math.Max(16, n * 6));

        Flags flags = Flags.None;
        if (!points.TiltX.IsEmpty) flags |= Flags.Tilt;
        if (!points.Twist.IsEmpty) flags |= Flags.Twist;

        buffer.WriteByte(Version);
        buffer.WriteByte((byte)flags);
        buffer.WriteVarInt((uint)quantization);
        buffer.WriteVarInt((uint)n);
        if (n == 0) return buffer.ToArray();

        WriteQuantizedDeltas(buffer, points.X, quantization);
        WriteQuantizedDeltas(buffer, points.Y, quantization);

        ReadOnlySpan<float> pressure = points.Pressure;
        for (int i = 0; i < n; i++) buffer.WriteByte((byte)Math.Clamp(MathF.Round(pressure[i] * 255f), 0, 255));

        ReadOnlySpan<float> time = points.Time;
        long previous = 0;
        for (int i = 0; i < n; i++)
        {
            long t = (long)MathF.Round(time[i]);
            buffer.WriteVarInt(ZigZag(t - previous));
            previous = t;
        }

        if ((flags & Flags.Tilt) != 0)
        {
            ReadOnlySpan<float> tx = points.TiltX;
            ReadOnlySpan<float> ty = points.TiltY;
            for (int i = 0; i < n; i++)
            {
                buffer.WriteByte((byte)(sbyte)Math.Clamp(MathF.Round(tx[i]), -90, 90));
                buffer.WriteByte((byte)(sbyte)Math.Clamp(MathF.Round(ty[i]), -90, 90));
            }
        }
        if ((flags & Flags.Twist) != 0)
        {
            ReadOnlySpan<float> tw = points.Twist;
            for (int i = 0; i < n; i++) buffer.WriteByte((byte)Math.Clamp(MathF.Round(tw[i] / 360f * 255f), 0, 255));
        }
        return buffer.ToArray();
    }

    private static void WriteQuantizedDeltas(ArrayBufferWriter buffer, ReadOnlySpan<float> values, int quantization)
    {
        long previous = 0;
        for (int i = 0; i < values.Length; i++)
        {
            long q = (long)MathF.Round(values[i] * quantization);
            buffer.WriteVarInt(ZigZag(q - previous));
            previous = q;
        }
    }

    /// <summary>Decodes points previously written by <see cref="Encode"/>.</summary>
    /// <param name="data">The encoded bytes.</param>
    /// <param name="maxPoints">Rejects inputs claiming more points than this, so a malformed document cannot exhaust memory.</param>
    /// <exception cref="FormatException">The data is malformed or exceeds <paramref name="maxPoints"/>.</exception>
    public static BinkStrokePoints Decode(ReadOnlySpan<byte> data, int maxPoints = 1_000_000)
    {
        int offset = 0;
        if (data.Length < 2) throw new FormatException("Point data is truncated.");
        byte version = data[offset++];
        if (version != Version) throw new FormatException($"Unsupported point encoding version {version}.");
        var flags = (Flags)data[offset++];
        uint quantization = ReadVarInt(data, ref offset);
        if (quantization == 0) throw new FormatException("Point quantization must be positive.");
        uint count = ReadVarInt(data, ref offset);
        if (count > maxPoints) throw new FormatException($"Point data declares {count} points; the limit is {maxPoints}.");
        int n = (int)count;
        if (n == 0) return BinkStrokePoints.Empty;

        // Every point costs at least one byte per varint channel plus the fixed pressure byte, so a payload
        // shorter than this cannot contain the declared count. Checking first stops a small hostile document
        // from making the decoder allocate megabytes before it fails.
        long minimumBytes = (long)n * 4 + offset;
        if (data.Length < minimumBytes) throw new FormatException($"Point data declares {n} points but is only {data.Length} bytes.");

        float scale = 1f / quantization;
        float[] xs = new float[n], ys = new float[n], ps = new float[n], ts = new float[n];
        ReadQuantizedDeltas(data, ref offset, xs, scale);
        ReadQuantizedDeltas(data, ref offset, ys, scale);

        if (offset + n > data.Length) throw new FormatException("Point data is truncated (pressure).");
        for (int i = 0; i < n; i++) ps[i] = data[offset++] / 255f;

        long time = 0;
        for (int i = 0; i < n; i++)
        {
            time += UnZigZag(ReadVarInt(data, ref offset));
            ts[i] = time;
        }

        float[]? tiltX = null, tiltY = null, twist = null;
        if ((flags & Flags.Tilt) != 0)
        {
            if (offset + n * 2 > data.Length) throw new FormatException("Point data is truncated (tilt).");
            tiltX = new float[n];
            tiltY = new float[n];
            for (int i = 0; i < n; i++)
            {
                tiltX[i] = (sbyte)data[offset++];
                tiltY[i] = (sbyte)data[offset++];
            }
        }
        if ((flags & Flags.Twist) != 0)
        {
            if (offset + n > data.Length) throw new FormatException("Point data is truncated (twist).");
            twist = new float[n];
            for (int i = 0; i < n; i++) twist[i] = data[offset++] / 255f * 360f;
        }

        // The arrays above were built for exactly this stroke, so hand them over instead of copying again.
        return BinkStrokePoints.FromOwnedArrays(xs, ys, ps, ts, tiltX, tiltY, twist);
    }

    private static void ReadQuantizedDeltas(ReadOnlySpan<byte> data, ref int offset, float[] target, float scale)
    {
        long value = 0;
        for (int i = 0; i < target.Length; i++)
        {
            value += UnZigZag(ReadVarInt(data, ref offset));
            target[i] = value * scale;
        }
    }

    private static uint ZigZag(long value)
    {
        long z = (value << 1) ^ (value >> 63);
        if (z is < 0 or > uint.MaxValue) throw new FormatException("A coordinate delta is out of range.");
        return (uint)z;
    }

    // The XOR must happen in signed 64-bit arithmetic: doing it on uint turns every negative delta into a
    // large positive number, which would corrupt any stroke that moves left or up.
    private static long UnZigZag(uint value) => (long)(value >> 1) ^ -(long)(value & 1);

    private static uint ReadVarInt(ReadOnlySpan<byte> data, ref int offset)
    {
        uint result = 0;
        int shift = 0;
        while (true)
        {
            if (offset >= data.Length) throw new FormatException("Point data is truncated.");
            byte b = data[offset++];
            result |= (uint)(b & 0x7F) << shift;
            if ((b & 0x80) == 0) return result;
            shift += 7;
            if (shift > 28) throw new FormatException("Malformed variable-length integer in point data.");
        }
    }

    private sealed class ArrayBufferWriter(int capacity)
    {
        private byte[] _buffer = new byte[Math.Max(capacity, 16)];
        private int _count;

        public void WriteByte(byte value)
        {
            EnsureCapacity(1);
            _buffer[_count++] = value;
        }

        public void WriteVarInt(uint value)
        {
            EnsureCapacity(5);
            while (value >= 0x80)
            {
                _buffer[_count++] = (byte)(value | 0x80);
                value >>= 7;
            }
            _buffer[_count++] = (byte)value;
        }

        public byte[] ToArray() => _buffer.AsSpan(0, _count).ToArray();

        private void EnsureCapacity(int extra)
        {
            if (_count + extra <= _buffer.Length) return;
            Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, _count + extra));
        }
    }

    /// <summary>Estimated encoded size in bytes, used to pre-size buffers.</summary>
    public static int EstimateSize(int pointCount, bool hasTilt, bool hasTwist)
        => 8 + pointCount * (2 + 2 + 1 + 1 + (hasTilt ? 2 : 0) + (hasTwist ? 1 : 0));

    /// <summary>Reads a big-endian 32-bit integer (used by the container format).</summary>
    internal static int ReadInt32BigEndian(ReadOnlySpan<byte> data) => BinaryPrimitives.ReadInt32BigEndian(data);
}
