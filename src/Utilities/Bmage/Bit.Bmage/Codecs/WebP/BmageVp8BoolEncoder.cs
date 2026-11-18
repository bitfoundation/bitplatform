namespace Bit.Bmage;

/// <summary>The VP8 boolean entropy encoder (RFC 6386 section 7.3).</summary>
internal sealed class BmageVp8BoolEncoder
{
    private byte[] _buffer;
    private int _length;
    private uint _range = 255;
    private uint _bottom;
    private int _bitCount = 24;

    public BmageVp8BoolEncoder(int capacity) => _buffer = new byte[capacity];

    public void PutBit(int probability, bool bit)
    {
        var split = 1 + (((_range - 1) * (uint)probability) >> 8);
        if (bit)
        {
            _bottom += split;
            _range -= split;
        }
        else _range = split;
        while (_range < 128)
        {
            _range <<= 1;
            if ((_bottom & (1u << 31)) != 0) AddOne();
            _bottom <<= 1;
            if (--_bitCount == 0)
            {
                Emit((byte)(_bottom >> 24));
                _bottom &= (1 << 24) - 1;
                _bitCount = 8;
            }
        }
    }

    public void PutFlag(bool value) => PutBit(128, value);

    public void PutLiteral(int value, int bits)
    {
        while (bits-- > 0) PutBit(128, ((value >> bits) & 1) != 0);
    }

    private void AddOne()
    {
        // A carry into bytes already written; the arithmetic guarantees it never runs off the front.
        var i = _length - 1;
        while (_buffer[i] == 255) _buffer[i--] = 0;
        _buffer[i]++;
    }

    private void Emit(byte b)
    {
        if (_length == _buffer.Length) Array.Resize(ref _buffer, _buffer.Length * 2);
        _buffer[_length++] = b;
    }

    public byte[] Finish()
    {
        var c = _bitCount;
        var v = _bottom;
        if ((v & (1u << (32 - c))) != 0) AddOne();
        v <<= c & 7;
        c >>= 3;
        while (--c >= 0) v <<= 8;
        for (var i = 0; i < 4; i++)
        {
            Emit((byte)(v >> 24));
            v <<= 8;
        }
        return _buffer.AsSpan(0, _length).ToArray();
    }
}
