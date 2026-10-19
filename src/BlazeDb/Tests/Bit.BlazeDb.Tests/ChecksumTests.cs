using System.Text;
using Bit.BlazeDb.Wal;

namespace Bit.BlazeDb.Tests;

/// <summary>
/// The checksum is what tells a torn record from a whole one, so it has to be the standard CRC-32
/// - not merely self-consistent - and every way of feeding it bytes has to agree.
/// </summary>
[TestClass]
public class ChecksumTests
{
    [TestMethod]
    public void Matches_The_Ieee_Check_Value()
    {
        // The reference vector every CRC-32 implementation is checked against.
        Assert.AreEqual(0xCBF43926u, BlazeDbCrc32.Compute("123456789"u8));
        Assert.AreEqual(0x00000000u, BlazeDbCrc32.Compute(ReadOnlySpan<byte>.Empty));
        Assert.AreEqual(0x190A55ADu, BlazeDbCrc32.Compute(new byte[32]));
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(9)]
    [DataRow(15)]
    [DataRow(16)]
    [DataRow(17)]
    [DataRow(1000)]
    [DataRow(4097)]
    public void Wide_And_Narrow_Paths_Agree_On_Every_Length_And_Split(int length)
    {
        var data = new byte[length];
        new Random(length).NextBytes(data);
        var expected = Reference(data);

        Assert.AreEqual(expected, BlazeDbCrc32.Compute(data));
        for (var split = 0; split <= length; split += Math.Max(1, length / 7))
        {
            Assert.AreEqual(expected, BlazeDbCrc32.Compute(data.AsSpan(0, split), data.AsSpan(split)));
            var incremental = BlazeDbCrc32.Update(BlazeDbCrc32.Seed, data.AsSpan(0, split));
            incremental = BlazeDbCrc32.Update(incremental, data.AsSpan(split));
            Assert.AreEqual(expected, BlazeDbCrc32.Finish(incremental));
        }
    }

    [TestMethod]
    public void Unaligned_Input_Is_Handled()
    {
        var text = Encoding.ASCII.GetBytes("xx123456789");
        Assert.AreEqual(0xCBF43926u, BlazeDbCrc32.Compute(text.AsSpan(2)));
    }

    /// <summary>The textbook bitwise algorithm, slow and obviously right.</summary>
    private static uint Reference(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
        }
        return crc ^ 0xFFFFFFFFu;
    }
}
