// PDF function evaluation (Types 0, 2, 3, 4).


namespace Bit.BlazorUI;

/// <summary>Type 0: sampled function with multilinear interpolation (m inputs, n outputs).</summary>
internal sealed class BitPdfSampledFunction : BitPdfFunction
{
    private readonly byte[] _samples;
    private readonly int _bps;
    private readonly int[] _size;
    private readonly int _inCount;
    private readonly int _outCount;
    private readonly double[] _domain;
    private readonly double[] _encode;
    private readonly double[] _decode;
    private readonly double[] _range;

    private BitPdfSampledFunction(byte[] samples, int bps, int[] size, int inCount, int outCount,
        double[] domain, double[] encode, double[] decode, double[] range)
    {
        _samples = samples;
        _bps = bps;
        _size = size;
        _inCount = inCount;
        _outCount = outCount;
        _domain = domain;
        _encode = encode;
        _decode = decode;
        _range = range;
    }

    public static BitPdfSampledFunction? Build(BitPdfStream stream, IBitPdfXRef? xref = null)
    {
        BitPdfDict dict = stream.Dict!;
        double[] domain = ReadNumbers(dict.Get("Domain"), xref);
        double[] range = ReadNumbers(dict.Get("Range"), xref);
        double[] sizeArr = ReadNumbers(dict.Get("Size"), xref);
        int bps = ToInt(dict.Get("BitsPerSample"), 8);
        if (domain.Length < 2 || range.Length < 2 || sizeArr.Length < 1)
        {
            return null;
        }

        int inCount = domain.Length / 2;
        int outCount = range.Length / 2;
        var size = new int[inCount];
        for (int i = 0; i < inCount; i++)
        {
            size[i] = i < sizeArr.Length ? Math.Max(1, (int)sizeArr[i]) : 1;
        }

        // Encode defaults to [0 size0-1 0 size1-1 ...]; missing entries in a
        // partial Encode array fall back to those per-axis defaults rather than
        // discarding the values that were supplied.
        double[] providedEncode = ReadNumbers(dict.Get("Encode"), xref);
        var encode = new double[inCount * 2];
        for (int i = 0; i < inCount; i++)
        {
            encode[i * 2] = i * 2 < providedEncode.Length ? providedEncode[i * 2] : 0;
            encode[i * 2 + 1] = i * 2 + 1 < providedEncode.Length ? providedEncode[i * 2 + 1] : size[i] - 1;
        }

        // Decode defaults to Range; a partial Decode falls back to Range per entry.
        double[] providedDecode = ReadNumbers(dict.Get("Decode"), xref);
        var decode = new double[range.Length];
        for (int i = 0; i < range.Length; i++)
        {
            decode[i] = i < providedDecode.Length ? providedDecode[i] : range[i];
        }

        byte[] samples = BitPdfStreamDecoder.Decode(stream);
        return new BitPdfSampledFunction(samples, bps, size, inCount, outCount, domain, encode, decode, range);
    }

    public override double[] Eval(double[] input)
    {
        // Encode each input to a (fractional) sample coordinate within its axis.
        var e = new double[_inCount];
        var i0 = new int[_inCount];
        var frac = new double[_inCount];
        for (int k = 0; k < _inCount; k++)
        {
            double x = Clamp(k < input.Length ? input[k] : 0, _domain[k * 2], _domain[k * 2 + 1]);
            double enc = Interp(x, _domain[k * 2], _domain[k * 2 + 1], _encode[k * 2], _encode[k * 2 + 1]);
            enc = Clamp(enc, 0, _size[k] - 1);
            i0[k] = (int)Math.Floor(enc);
            if (i0[k] >= _size[k] - 1)
            {
                i0[k] = Math.Max(0, _size[k] - 1);
                frac[k] = 0;
            }
            else
            {
                frac[k] = enc - i0[k];
            }
            e[k] = enc;
        }

        double maxVal = Math.Pow(2, _bps) - 1;
        var output = new double[_outCount];

        // Multilinear interpolation over the 2^m surrounding sample corners.
        int corners = 1 << _inCount;
        for (int corner = 0; corner < corners; corner++)
        {
            double weight = 1;
            long flatIndex = 0;
            long stride = 1;
            for (int k = 0; k < _inCount; k++)
            {
                bool upper = (corner & (1 << k)) != 0;
                int idx = i0[k] + (upper ? 1 : 0);
                if (idx > _size[k] - 1)
                {
                    idx = _size[k] - 1;
                }
                weight *= upper ? frac[k] : 1 - frac[k];
                flatIndex += idx * stride;
                stride *= _size[k];
            }
            if (weight == 0)
            {
                continue;
            }
            for (int c = 0; c < _outCount; c++)
            {
                output[c] += weight * (ReadSample(flatIndex * _outCount + c) / maxVal);
            }
        }

        // Apply the Decode array to map [0,1] sample space onto the output range,
        // then clamp to /Range (PDF 32000-1 §7.10.2).
        for (int c = 0; c < _outCount; c++)
        {
            double d0 = _decode.Length > c * 2 ? _decode[c * 2] : 0;
            double d1 = _decode.Length > c * 2 + 1 ? _decode[c * 2 + 1] : 1;
            output[c] = d0 + output[c] * (d1 - d0);
            if (_range.Length > c * 2 + 1)
            {
                output[c] = Clamp(output[c], _range[c * 2], _range[c * 2 + 1]);
            }
        }
        return output;
    }

    private double ReadSample(long index)
    {
        long bitPos = index * _bps;
        long bytePos = bitPos / 8;
        int bitOffset = (int)(bitPos % 8);
        int value = 0;
        int bitsRead = 0;
        while (bitsRead < _bps && bytePos < _samples.Length)
        {
            int available = 8 - bitOffset;
            int take = Math.Min(available, _bps - bitsRead);
            int b = _samples[bytePos];
            int shifted = (b >> (available - take)) & ((1 << take) - 1);
            value = (value << take) | shifted;
            bitsRead += take;
            bitOffset += take;
            if (bitOffset >= 8)
            {
                bitOffset = 0;
                bytePos++;
            }
        }
        return value;
    }

    private static double Interp(double x, double xmin, double xmax, double ymin, double ymax)
        => xmax > xmin ? ymin + (x - xmin) * (ymax - ymin) / (xmax - xmin) : ymin;
}
