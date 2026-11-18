using System.Runtime.InteropServices;

namespace Bit.Bmage;

/// <summary>Reads the least significant bit first, as the WebP lossless format packs its bits.</summary>
internal sealed class BmageVp8LBitReader
{
    private readonly byte[] _data;
    private int _pos;
    private readonly int _end;
    private readonly int _length;
    private ulong _buffer;
    private int _bits;
    private long _consumed;

    public BmageVp8LBitReader(ReadOnlyMemory<byte> data)
    {
        // The caller's array is read in place; only memory no array backs is copied.
        var segment = MemoryMarshal.TryGetArray(data, out var array) ? array : new ArraySegment<byte>(data.ToArray());
        _data = segment.Array!;
        _pos = segment.Offset;
        _end = segment.Offset + segment.Count;
        _length = segment.Count;
        Fill();
    }

    /// <summary>True once more bits were read than the data holds; they read as zeros.</summary>
    public bool IsOverrun => _consumed > (long)_length * 8;

    public long RemainingBits => (long)_length * 8 - _consumed;

    private void Fill()
    {
        while (_bits <= 56)
        {
            var b = _pos < _end ? _data[_pos] : (byte)0;
            _pos++;
            _buffer |= (ulong)b << _bits;
            _bits += 8;
        }
    }

    public uint ReadBits(int count)
    {
        if (count == 0) return 0;
        if (_bits < count) Fill();
        var value = (uint)(_buffer & ((1UL << count) - 1));
        _buffer >>= count;
        _bits -= count;
        _consumed += count;
        return value;
    }

    /// <summary>The next bits without consuming them; at least 32 are always available.</summary>
    public uint Peek()
    {
        if (_bits < 32) Fill();
        return (uint)_buffer;
    }

    public void Skip(int count)
    {
        _buffer >>= count;
        _bits -= count;
        _consumed += count;
    }
}
