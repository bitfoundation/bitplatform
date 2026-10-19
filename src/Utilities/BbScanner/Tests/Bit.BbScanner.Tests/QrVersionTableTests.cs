namespace Bit.BbScanner.Tests;

/// <summary>
/// Validates the transcribed QR block tables against an independent source of truth: the total
/// number of codewords each version holds. Every error correction level of a version has to add
/// up to the same total, and that total has to match the published figure, which makes a
/// transcription slip in any of the 160 table entries impossible to miss.
/// </summary>
[TestClass]
public class QrVersionTableTests
{
    /// <summary>Total codewords per version, from ISO/IEC 18004 table 1.</summary>
    private static readonly int[] TotalCodewordsPerVersion =
    [
        26, 44, 70, 100, 134, 172, 196, 242, 292, 346,
        404, 466, 532, 581, 655, 733, 815, 901, 991, 1085,
        1156, 1258, 1364, 1474, 1588, 1706, 1828, 1921, 2051, 2185,
        2323, 2465, 2611, 2761, 2876, 3034, 3196, 3362, 3532, 3706,
    ];

    [TestMethod]
    public void EveryLevelOfEveryVersionMatchesThePublishedCodewordTotal()
    {
        for (var number = 1; number <= 40; number++)
        {
            var version = BbQrVersion.GetVersionForNumber(number);
            var expected = TotalCodewordsPerVersion[number - 1];

            Assert.AreEqual(expected, version.TotalCodewords);

            foreach (var level in new[]
                     {
                         BbQrErrorCorrectionLevel.L,
                         BbQrErrorCorrectionLevel.M,
                         BbQrErrorCorrectionLevel.Q,
                         BbQrErrorCorrectionLevel.H,
                     })
            {
                var blocks = version.GetEcBlocksForLevel(level);
                Assert.AreEqual(expected, blocks.TotalDataCodewords + blocks.TotalEcCodewords);
            }
        }
    }

    [TestMethod]
    public void ErrorCorrectionCapacityIncreasesWithLevel()
    {
        for (var number = 1; number <= 40; number++)
        {
            var version = BbQrVersion.GetVersionForNumber(number);
            var l = version.GetEcBlocksForLevel(BbQrErrorCorrectionLevel.L).TotalEcCodewords;
            var m = version.GetEcBlocksForLevel(BbQrErrorCorrectionLevel.M).TotalEcCodewords;
            var q = version.GetEcBlocksForLevel(BbQrErrorCorrectionLevel.Q).TotalEcCodewords;
            var h = version.GetEcBlocksForLevel(BbQrErrorCorrectionLevel.H).TotalEcCodewords;

            Assert.IsTrue(l < m, $"Version {number}: L should carry fewer EC codewords than M.");
            Assert.IsTrue(m < q, $"Version {number}: M should carry fewer EC codewords than Q.");
            Assert.IsTrue(q < h, $"Version {number}: Q should carry fewer EC codewords than H.");
        }
    }

    [TestMethod]
    public void AlignmentPatternCentersAreOrderedAndInsideTheSymbol()
    {
        for (var number = 1; number <= 40; number++)
        {
            var version = BbQrVersion.GetVersionForNumber(number);
            var centers = version.AlignmentPatternCenters;
            if (centers.Length == 0)
            {
                Assert.AreEqual(1, number);
                continue;
            }

            Assert.AreEqual(6, centers[0]);
            Assert.AreEqual(version.DimensionForVersion - 7, centers[^1]);

            for (var i = 1; i < centers.Length; i++)
            {
                Assert.IsTrue(centers[i] > centers[i - 1], $"Version {number}: centres must ascend.");
            }
        }
    }

    [TestMethod]
    [DataRow(0, 0x5412)]
    [DataRow(1, 0x5125)]
    [DataRow(8, 0x77C4)]
    [DataRow(16, 0x1689)]
    [DataRow(31, 0x2BED)]
    public void FormatInformationMatchesThePublishedCode(int data, int expected) =>
        Assert.AreEqual(expected, BbQrFormatInformation.EncodeFormatBits(data));

    [TestMethod]
    [DataRow(7, 0x07C94)]
    [DataRow(8, 0x085BC)]
    [DataRow(9, 0x09A99)]
    [DataRow(10, 0x0A4D3)]
    [DataRow(40, 0x28C69)]
    public void VersionInformationMatchesThePublishedCode(int version, int expected) =>
        Assert.AreEqual(expected, BbQrFormatInformation.EncodeVersionBits(version));

    [TestMethod]
    public void FormatInformationRoundTripsThroughDecoding()
    {
        for (var data = 0; data < 32; data++)
        {
            var encoded = BbQrFormatInformation.EncodeFormatBits(data);
            var decoded = BbQrFormatInformation.Decode(encoded, encoded);

            Assert.IsNotNull(decoded);
            Assert.AreEqual((BbQrErrorCorrectionLevel)((data >> 3) & 0x03), decoded.ErrorCorrectionLevel);
            Assert.AreEqual(data & 0x07, decoded.DataMask);
        }
    }

    [TestMethod]
    public void FormatInformationSurvivesThreeBitErrors()
    {
        var encoded = BbQrFormatInformation.EncodeFormatBits(0b10_101);
        var damaged = encoded ^ 0b000_0000_0000_0111;

        var decoded = BbQrFormatInformation.Decode(damaged, damaged);

        Assert.IsNotNull(decoded);
        Assert.AreEqual(BbQrErrorCorrectionLevel.H, decoded.ErrorCorrectionLevel);
        Assert.AreEqual(5, decoded.DataMask);
    }

    [TestMethod]
    public void VersionInformationRoundTripsThroughDecoding()
    {
        for (var version = 7; version <= 40; version++)
        {
            var encoded = BbQrFormatInformation.EncodeVersionBits(version);
            var decoded = BbQrFormatInformation.DecodeVersionInformation(encoded);

            Assert.IsNotNull(decoded);
            Assert.AreEqual(version, decoded.VersionNumber);
        }
    }
}
