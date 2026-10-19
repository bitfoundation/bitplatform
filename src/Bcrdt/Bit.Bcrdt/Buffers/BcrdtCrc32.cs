namespace Bit.Bcrdt;

/// <summary>
/// The standard CRC-32 checksum, used to detect torn records in append-only storage.
/// </summary>
/// <remarks>
/// Implemented here rather than taken from a package so that the whole stack keeps zero external
/// dependencies. It guards against a crash mid-append leaving a half-written record that would
/// otherwise be decoded as garbage on the next load; it is not a security primitive.
/// </remarks>
public static class BcrdtCrc32
{
    private const uint Polynomial = 0xEDB88320;
    private static readonly uint[] Table = BuildTable();

    private static uint[] BuildTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint value = i;
            for (int bit = 0; bit < 8; bit++)
            {
                value = (value & 1) != 0 ? (value >> 1) ^ Polynomial : value >> 1;
            }
            table[i] = value;
        }
        return table;
    }

    /// <summary>Computes the checksum of a payload.</summary>
    public static uint Compute(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in data)
        {
            crc = (crc >> 8) ^ Table[(crc ^ b) & 0xFF];
        }
        return ~crc;
    }
}
