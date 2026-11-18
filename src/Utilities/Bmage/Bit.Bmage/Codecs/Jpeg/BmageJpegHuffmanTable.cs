namespace Bit.Bmage;

internal sealed class BmageJpegHuffmanTable
{
    // Fast lookup for codes up to 9 bits: (length << 8) | value, 0 when the code is longer.
    public readonly ushort[] Lookup = new ushort[512];
    public readonly int[] MaxCode = new int[18];
    public readonly int[] ValPtr = new int[17];
    public readonly int[] MinCode = new int[17];
    public readonly byte[] Values;

    public BmageJpegHuffmanTable(ReadOnlySpan<byte> bits, ReadOnlySpan<byte> values)
    {
        Values = values.ToArray();
        var code = 0;
        var k = 0;
        for (var len = 1; len <= 16; len++)
        {
            var n = bits[len - 1];
            ValPtr[len] = k;
            MinCode[len] = code;
            if (n == 0) { MaxCode[len] = -1; }
            else
            {
                // More codes than this length can hold means the table is not a prefix code; trusting it would index
                // past the lookup table.
                if (code + n > 1 << len) throw new BmageImageDecodeException("Invalid JPEG Huffman table.");
                for (var i = 0; i < n; i++)
                {
                    if (len <= 9)
                    {
                        var shift = 9 - len;
                        var start = code << shift;
                        for (var j = 0; j < (1 << shift); j++)
                            Lookup[start + j] = (ushort)((len << 8) | Values[k]);
                    }
                    code++; k++;
                }
                MaxCode[len] = code - 1;
            }
            code <<= 1;
        }
        MaxCode[17] = int.MaxValue;
    }
}
