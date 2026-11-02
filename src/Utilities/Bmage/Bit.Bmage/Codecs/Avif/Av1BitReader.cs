using System.Numerics;

namespace Bit.Bmage.Codecs.Avif;

/// <summary>Reads the MSB-first bit fields of AV1 headers: the spec's f(n), su(n), ns(n) and uvlc, and leb128 sizes.</summary>
internal ref struct Av1BitReader
{
    private readonly ReadOnlySpan<byte> _data;
    private long _bitPos;

    public Av1BitReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _bitPos = 0;
    }

    /// <summary>The position in bits from the start of the data.</summary>
    public readonly long BitPosition => _bitPos;

    public int ReadBit()
    {
        var byteIndex = _bitPos >> 3;
        if (byteIndex >= _data.Length) throw new ImageDecodeException("Truncated AV1 header.");
        var bit = (_data[(int)byteIndex] >> (7 - (int)(_bitPos & 7))) & 1;
        _bitPos++;
        return bit;
    }

    public bool ReadFlag() => ReadBit() != 0;

    /// <summary>f(n) for n up to 32.</summary>
    public uint ReadBits(int n)
    {
        uint x = 0;
        for (var i = 0; i < n; i++) x = (x << 1) | (uint)ReadBit();
        return x;
    }

    public int Read(int n) => (int)ReadBits(n);

    public int ReadSigned(int n)
    {
        var value = (int)ReadBits(n);
        var signMask = 1 << (n - 1);
        return (value & signMask) != 0 ? value - 2 * signMask : value;
    }

    public int ReadNs(int n)
    {
        if (n <= 1) return 0;
        var w = BitOperations.Log2((uint)n) + 1;
        var m = (1 << w) - n;
        var v = Read(w - 1);
        if (v < m) return v;
        return (v << 1) - m + ReadBit();
    }

    public uint ReadUvlc()
    {
        var leadingZeros = 0;
        while (ReadBit() == 0)
        {
            leadingZeros++;
            if (leadingZeros > 40) throw new ImageDecodeException("Invalid AV1 uvlc value.");
        }
        if (leadingZeros >= 32) return uint.MaxValue;
        return ReadBits(leadingZeros) + (uint)((1L << leadingZeros) - 1);
    }

    public void ByteAlign() => _bitPos = (_bitPos + 7) & ~7L;

    /// <summary>Reads a leb128 value from a byte position, returning the number of bytes it took or 0 when malformed.</summary>
    public static int ReadLeb128(ReadOnlySpan<byte> data, int pos, out ulong value)
    {
        value = 0;
        for (var i = 0; i < 8; i++)
        {
            if (pos + i >= data.Length) return 0;
            var b = data[pos + i];
            value |= (ulong)(b & 0x7F) << (i * 7);
            if ((b & 0x80) == 0) return i + 1;
        }
        return 0;
    }
}
