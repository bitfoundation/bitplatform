namespace Bit.Bmage.Codecs;

/// <summary>CRC-32 (IEEE 802.3, as used by PNG and zlib).</summary>
internal static class Crc32
{
    private static readonly uint[] Table = BuildTable();

    private static uint[] BuildTable()
    {
        var t = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            t[n] = c;
        }
        return t;
    }

    /// <summary>Continues a CRC computation. Pass 0 as the initial value; the returned value is the final CRC.</summary>
    public static uint Update(uint crc, ReadOnlySpan<byte> data)
    {
        var c = crc ^ 0xFFFFFFFFu;
        foreach (var b in data) c = Table[(c ^ b) & 0xFF] ^ (c >> 8);
        return c ^ 0xFFFFFFFFu;
    }

    public static uint Compute(ReadOnlySpan<byte> data) => Update(0, data);
}
