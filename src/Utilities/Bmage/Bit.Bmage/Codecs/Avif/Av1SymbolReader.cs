using System.Numerics;

namespace Bit.Bmage.Codecs.Avif;

/// <summary>
/// The AV1 arithmetic decoder (spec section 8.2). The spec feeds the value one bit at a time; this keeps a 64-bit window
/// of the inverted stream instead, so the value is its top bits above <see cref="_cnt"/>, which decodes identically.
/// Reads past the end of the tile see zero bits, as the spec requires.
/// </summary>
internal sealed class Av1SymbolReader
{
    private byte[] _data = [];
    private int _pos;
    private int _end;
    private ulong _dif;
    private int _cnt;
    private uint _rng;
    private bool _disableUpdate;

    public void Init(byte[] data, int start, int size, bool disableCdfUpdate)
    {
        _data = data;
        _pos = start;
        _end = start + size;
        _dif = 0;
        _cnt = -15;
        _rng = 1 << 15;
        _disableUpdate = disableCdfUpdate;
        Refill();
    }

    private void Refill()
    {
        while (_cnt < 40)
        {
            uint b = _pos < _end ? _data[_pos] : 0u;
            _pos++;
            _dif = (_dif << 8) | (~b & 0xFF);
            _cnt += 8;
        }
    }

    /// <summary>read_symbol: decodes one of <paramref name="n"/> symbols with the CDF at <paramref name="offset"/> and adapts it.</summary>
    public int Read(ushort[] cdf, int offset, int n)
    {
        var value = (uint)(_dif >> _cnt);
        var r8 = _rng >> 8;
        uint cur = _rng, prev;
        var symbol = -1;
        do
        {
            symbol++;
            prev = cur;
            cur = ((r8 * ((32768u - cdf[offset + symbol]) >> 6)) >> 1) + (uint)(4 * (n - symbol - 1));
        }
        while (value < cur);
        Renormalize(prev - cur, cur);

        if (!_disableUpdate)
        {
            int count = cdf[offset + n];
            var rate = 3 + (count > 15 ? 1 : 0) + (count > 31 ? 1 : 0) + Math.Min(BitOperations.Log2((uint)n), 2);
            for (var i = 0; i < n - 1; i++)
            {
                if (i < symbol) cdf[offset + i] -= (ushort)(cdf[offset + i] >> rate);
                else cdf[offset + i] += (ushort)((32768 - cdf[offset + i]) >> rate);
            }
            if (count < 32) cdf[offset + n] = (ushort)(count + 1);
        }
        return symbol;
    }

    /// <summary>A symbol from a CDF built on the fly (never adapted), as split_or_horz and split_or_vert use.</summary>
    public int ReadBoolWithProbability(uint cdf0)
    {
        var value = (uint)(_dif >> _cnt);
        var cur = (((_rng >> 8) * ((32768u - cdf0) >> 6)) >> 1) + 4;
        if (value < cur)
        {
            Renormalize(cur, 0);
            return 1;
        }
        Renormalize(_rng - cur, cur);
        return 0;
    }

    /// <summary>read_bool: an equiprobable bit.</summary>
    public int ReadBool()
    {
        var value = (uint)(_dif >> _cnt);
        var cur = ((_rng >> 8) << 7) + 4;
        if (value < cur)
        {
            Renormalize(cur, 0);
            return 1;
        }
        Renormalize(_rng - cur, cur);
        return 0;
    }

    /// <summary>L(n): an n-bit literal of equiprobable bits, high bit first.</summary>
    public int ReadLiteral(int n)
    {
        var x = 0;
        for (var i = 0; i < n; i++) x = (x << 1) | ReadBool();
        return x;
    }

    /// <summary>NS(n): a value in 0..n-1 coded with arithmetic-coded bits.</summary>
    public int ReadNs(int n)
    {
        if (n <= 1) return 0;
        var w = BitOperations.Log2((uint)n) + 1;
        var m = (1 << w) - n;
        var v = ReadLiteral(w - 1);
        if (v < m) return v;
        return (v << 1) - m + ReadBool();
    }

    private void Renormalize(uint rng, uint cur)
    {
        _dif -= (ulong)cur << _cnt;
        var bits = BitOperations.LeadingZeroCount(rng) - 16;
        _rng = rng << bits;
        _cnt -= bits;
        if (_cnt < 16) Refill();
    }
}
