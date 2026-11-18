using System.Numerics;
using System.Runtime.InteropServices;

namespace Bit.Bmage;

/// <summary>The VP8 boolean entropy decoder (RFC 6386 section 7), reading ahead up to eight bytes at a time.</summary>
internal sealed class BmageVp8BoolDecoder
{
    private readonly byte[] _data;
    private int _pos;
    private readonly int _end;
    private ulong _value; // unread bits, most significant first
    private int _bits;    // how many of _value's bits are real data
    private uint _range = 255;

    public BmageVp8BoolDecoder(ReadOnlyMemory<byte> data)
    {
        // The caller's array is read in place; only memory no array backs is copied.
        var segment = MemoryMarshal.TryGetArray(data, out var array) ? array : new ArraySegment<byte>(data.ToArray());
        _data = segment.Array!;
        _pos = segment.Offset;
        _end = segment.Offset + segment.Count;
        Fill();
    }

    /// <summary>True once decoding needed bits past the end of the data, which means the stream is truncated.</summary>
    public bool IsOverrun { get; private set; }

    private void Fill()
    {
        while (_bits <= 56 && _pos < _end)
        {
            _value |= (ulong)_data[_pos++] << (56 - _bits);
            _bits += 8;
        }
        if (_bits < 8)
        {
            // Past the end the stream reads as zeros; needing them means the data was cut short.
            IsOverrun = true;
            _bits = 64;
        }
    }

    public bool GetBit(int probability)
    {
        if (_bits < 8) Fill();
        var split = 1 + (((_range - 1) * (uint)probability) >> 8);
        var bigSplit = (ulong)split << 56;
        bool bit;
        if (_value >= bigSplit)
        {
            _range -= split;
            _value -= bigSplit;
            bit = true;
        }
        else
        {
            _range = split;
            bit = false;
        }
        var shift = BitOperations.LeadingZeroCount(_range) - 24;
        _range <<= shift;
        _value <<= shift;
        _bits -= shift;
        return bit;
    }

    public bool ReadFlag() => GetBit(128);

    public int ReadLiteral(int bits)
    {
        var v = 0;
        while (bits-- > 0) v = (v << 1) | (GetBit(128) ? 1 : 0);
        return v;
    }

    /// <summary>A magnitude of <paramref name="bits"/> bits followed by a sign bit, as the frame header codes deltas.</summary>
    public int ReadSigned(int bits)
    {
        var v = ReadLiteral(bits);
        return ReadFlag() ? -v : v;
    }
}
