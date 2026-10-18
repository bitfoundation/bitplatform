using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Bit.BlazeDb.Wal;

/// <summary>
/// CRC-32 (IEEE 802.3, reflected polynomial 0xEDB88320) used to detect torn WAL writes.
/// Slicing-by-8: eight lookup tables let the loop consume eight bytes per iteration, which is
/// several times faster than a byte-at-a-time table on the snapshot-sized buffers a checkpoint
/// checksums, with no dependency and no allocation.
/// </summary>
internal static class BlazeDbCrc32
{
    private const uint Polynomial = 0xEDB88320u;

    // Table[k * 256 + b]: the CRC contribution of byte b when it sits k bytes further from the end.
    private static readonly uint[] Table = BuildTables();

    private static uint[] BuildTables()
    {
        var table = new uint[8 * 256];
        for (uint i = 0; i < 256; i++)
        {
            var crc = i;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ Polynomial : crc >> 1;
            }
            table[i] = crc;
        }
        for (var k = 1; k < 8; k++)
        {
            for (var i = 0; i < 256; i++)
            {
                var previous = table[(k - 1) * 256 + i];
                table[k * 256 + i] = (previous >> 8) ^ table[previous & 0xFF];
            }
        }
        return table;
    }

    public static uint Compute(ReadOnlySpan<byte> data) => Finish(Update(Seed, data));

    /// <summary>A checksum over two runs of bytes that are not contiguous in memory.</summary>
    public static uint Compute(ReadOnlySpan<byte> first, ReadOnlySpan<byte> second) =>
        Finish(Update(Update(Seed, first), second));

    /// <summary>Starting value for an incremental checksum; feed it through <see cref="Update"/>.</summary>
    public const uint Seed = 0xFFFFFFFFu;

    public static uint Update(uint crc, ReadOnlySpan<byte> data)
    {
        var table = Table;
        var words = MemoryMarshal.Cast<byte, ulong>(data.Slice(0, data.Length & ~7));
        foreach (var raw in words)
        {
            var word = BitConverter.IsLittleEndian ? raw : BinaryPrimitives.ReverseEndianness(raw);
            var low = (uint)word ^ crc;
            var high = (uint)(word >> 32);
            crc = table[7 * 256 + (low & 0xFF)]
                ^ table[6 * 256 + ((low >> 8) & 0xFF)]
                ^ table[5 * 256 + ((low >> 16) & 0xFF)]
                ^ table[4 * 256 + (low >> 24)]
                ^ table[3 * 256 + (high & 0xFF)]
                ^ table[2 * 256 + ((high >> 8) & 0xFF)]
                ^ table[1 * 256 + ((high >> 16) & 0xFF)]
                ^ table[high >> 24];
        }
        foreach (var b in data.Slice(words.Length * 8))
        {
            crc = (crc >> 8) ^ table[(crc ^ b) & 0xFF];
        }
        return crc;
    }

    public static uint Finish(uint crc) => crc ^ 0xFFFFFFFFu;
}
