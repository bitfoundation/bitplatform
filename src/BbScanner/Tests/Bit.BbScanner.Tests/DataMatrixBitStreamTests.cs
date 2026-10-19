using System.Text;

namespace Bit.BbScanner.Tests;

/// <summary>
/// Drives the Data Matrix codeword parser directly, because the encoder in the test kit only
/// emits ASCII: every other encodation mode, and every malformed stream, is only reachable this
/// way.
/// </summary>
[TestClass]
public class DataMatrixBitStreamTests
{
    private static readonly BbDataMatrixVersion Version = BbDataMatrixVersion.All.First(v => v.VersionNumber == 5);

    private static byte[] Pad(params int[] codewords)
    {
        var data = new byte[Version.TotalDataCodewords];
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = i < codewords.Length ? (byte)codewords[i] : (byte)129;
        }

        return data;
    }

    private static string? DecodeText(params int[] codewords) =>
        BbDataMatrixBitStreamParser.Decode(Pad(codewords), Version, 0)?.Text;

    [TestMethod]
    public void DecodesAsciiAndDigitPairs()
    {
        // Values 1 to 128 are the character plus one; 130 to 229 are a pair of digits.
        Assert.AreEqual("AB", DecodeText('A' + 1, 'B' + 1));
        Assert.AreEqual("42", DecodeText(130 + 42));
        Assert.AreEqual("A077", DecodeText('A' + 1, 130 + 7, '7' + 1));
    }

    [TestMethod]
    public void DecodesTheUpperShift()
    {
        // 235 shifts the next value into the upper half of Latin-1.
        var result = BbDataMatrixBitStreamParser.Decode(Pad(235, 0xE9 - 128 + 1), Version, 0);

        Assert.IsNotNull(result);
        Assert.AreEqual("é", result.Text);
    }

    [TestMethod]
    public void RejectsReservedCodewords()
    {
        // 242 to 253 and 255 are not assigned. A stream containing one is corrupt, and accepting
        // it would turn a Reed-Solomon miscorrection into a plausible looking value.
        foreach (var reserved in new[] { 0, 242, 247, 250, 253, 255 })
        {
            Assert.IsNull(DecodeText('A' + 1, 'B' + 1, reserved, 'C' + 1));
        }
    }

    [TestMethod]
    public void RejectsAnUnlatchThatIsNotLast()
    {
        Assert.IsNull(DecodeText('A' + 1, 254, 'B' + 1));
    }

    [TestMethod]
    public void DecodesTheStructuredAppendCountAsTheSpecificationDefinesIt()
    {
        // ISO/IEC 16022 stores the total as 17 minus the low nibble, not the low nibble plus two.
        // Symbol 2 of 3: index nibble 1, count nibble 17 - 3 = 14.
        var result = BbDataMatrixBitStreamParser.Decode(Pad(233, (1 << 4) | 14, 0x12, 0x34, 'A' + 1), Version, 0);

        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.StructuredAppendIndex);
        Assert.AreEqual(3, result.StructuredAppendCount);
        Assert.AreEqual(0x1234, result.StructuredAppendParity);
        Assert.AreEqual("A", result.Text);
    }

    [TestMethod]
    public void DecodesTheOneCodewordEciForm()
    {
        var result = BbDataMatrixBitStreamParser.Decode(Pad(241, 27, 'A' + 1), Version, 0);

        Assert.IsNotNull(result);
        Assert.AreEqual(26, result.Eci);
    }

    [TestMethod]
    [DataRow(241, 0, 'A' + 1)]
    [DataRow(241, 131, 0, 'A' + 1)]
    [DataRow(241, 131, 255, 'A' + 1)]
    [DataRow(241, 200, 0, 9, 'A' + 1)]
    [DataRow(241, 200, 5, 255, 'A' + 1)]
    public void AnEciWithAnUndefinedCodewordIsRejected(params int[] codewords)
    {
        // Codeword 0 is undefined in every position of an ECI and the continuation codewords
        // run from 1 to 254; accepting them produced assignment numbers such as -1.
        Assert.IsNull(BbDataMatrixBitStreamParser.Decode(Pad(codewords), Version, 0));
    }

    [TestMethod]
    public void C40Shift3RejectsValuesOutsideItsSet()
    {
        // 230 latches C40; shift 3 (value 2) followed by 32 is outside the thirty two character
        // shift 3 set. Encoded as the triple (2, 32, 0): value = 1600 * 2 + 40 * 32 + 0 + 1.
        const int Value = (1600 * 2) + (40 * 32) + 0 + 1;
        Assert.IsNull(BbDataMatrixBitStreamParser.Decode(Pad(230, Value >> 8, Value & 0xFF, 254), Version, 0));
    }

    [TestMethod]
    public void DecodesTheTwoCodewordEciForm()
    {
        // c1 between 128 and 191 means two codewords: (c1 - 128) * 254 + c2 - 1 + 127.
        var result = BbDataMatrixBitStreamParser.Decode(Pad(241, 131, 2, 'A' + 1), Version, 0);

        Assert.IsNotNull(result);
        Assert.AreEqual(((131 - 128) * 254) + 2 - 1 + 127, result.Eci);
        Assert.AreEqual("A", result.Text);
    }

    [TestMethod]
    public void DecodesTheThreeCodewordEciForm()
    {
        var result = BbDataMatrixBitStreamParser.Decode(Pad(241, 200, 5, 9, 'A' + 1), Version, 0);

        Assert.IsNotNull(result);
        Assert.AreEqual(((200 - 192) * 64516) + ((5 - 1) * 254) + 9 - 1 + 16383, result.Eci);
        Assert.AreEqual("A", result.Text);
    }

    [TestMethod]
    public void AppliesTheDeclaredCharacterSetToTheText()
    {
        // ECI 26 is UTF-8. The bytes are two code units of one character; without applying the
        // ECI they would come out as two Latin-1 characters.
        var utf8 = Encoding.UTF8.GetBytes("é");
        Assert.AreEqual(2, utf8.Length);

        // ECI 26, then a Base 256 run of two bytes. Base 256 values are randomised by their
        // one based position in the data stream, which the ECI codewords occupy first.
        var result = BbDataMatrixBitStreamParser.Decode(
            Pad(241, 27, 231, Randomise(2, 4), Randomise(utf8[0], 5), Randomise(utf8[1], 6)),
            Version,
            0);

        Assert.IsNotNull(result);
        Assert.AreEqual("é", result.Text);
        Assert.AreSequenceEqual(utf8, result.RawBytes);
    }

    [TestMethod]
    public void FlagsGs1OnlyWhenTheStreamOpensWithFnc1()
    {
        var leading = BbDataMatrixBitStreamParser.Decode(Pad(232, '0' + 1, '1' + 1), Version, 0);
        Assert.IsNotNull(leading);
        Assert.IsTrue(leading.IsGs1);

        // A separator in the middle of the payload is not a GS1 declaration.
        var middle = BbDataMatrixBitStreamParser.Decode(Pad('A' + 1, 232, 'B' + 1), Version, 0);
        Assert.IsNotNull(middle);
        Assert.IsFalse(middle.IsGs1);
        Assert.AreEqual("AB", middle.Text);
    }

    [TestMethod]
    public void DecodesBase256WithItsRandomisation()
    {
        // Base 256 bytes are randomised by position, and a one byte length prefix covers up to
        // 249 bytes.
        var payload = new byte[] { 0x00, 0x7F, 0xFF, 0x41 };
        var codewords = new List<int> { 231, Randomise(payload.Length, 2) };
        for (var i = 0; i < payload.Length; i++)
        {
            codewords.Add(Randomise(payload[i], 3 + i));
        }

        var result = BbDataMatrixBitStreamParser.Decode(Pad([.. codewords]), Version, 0);

        Assert.IsNotNull(result);
        Assert.AreSequenceEqual(payload, result.RawBytes);
    }

    private static int Randomise(int value, int position)
    {
        var pseudoRandom = ((149 * position) % 255) + 1;
        var temp = value + pseudoRandom;
        return temp <= 255 ? temp : temp - 256;
    }

    [TestMethod]
    public void DecodesC40Text()
    {
        // C40 packs three values into two codewords: 1600 * c1 + 40 * c2 + c3 + 1.
        // Basic set values 14 to 39 are 'A' to 'Z'.
        var value = (1600 * ('A' - 'A' + 14)) + (40 * ('B' - 'A' + 14)) + ('C' - 'A' + 14) + 1;
        var result = BbDataMatrixBitStreamParser.Decode(
            Pad(230, value >> 8, value & 0xFF, 254, 'D' + 1), Version, 0);

        Assert.IsNotNull(result);
        Assert.AreEqual("ABCD", result.Text);
    }

    [TestMethod]
    public void NeverThrowsOnArbitraryCodewordStreams()
    {
        // Reed-Solomon accepts about one random block in a million, so the parser is reached
        // with garbage in normal operation and has to fail by returning null.
        var random = new Random(31337);
        var accepted = 0;

        foreach (var version in BbDataMatrixVersion.All)
        {
            for (var attempt = 0; attempt < 200; attempt++)
            {
                var data = new byte[version.TotalDataCodewords];
                random.NextBytes(data);
                if (BbDataMatrixBitStreamParser.Decode(data, version, 0) is not null)
                {
                    accepted++;
                }
            }
        }

        // Some random streams are legal ASCII, which is fine; the contract is only that nothing
        // throws and that the parser is willing to say no.
        Assert.IsTrue(accepted < 200 * BbDataMatrixVersion.All.Count, "Every random stream was accepted.");
    }

    [TestMethod]
    public void RejectsAGridWhoseFinderPatternIsMissing()
    {
        // The solid L and the dashed timing pattern are the cheapest possible check that a
        // sampled grid is really a symbol, and they cost two lines of modules to verify.
        using var real = DataMatrixEncoder.Encode("BORDERS");
        Assert.IsTrue(BbDataMatrixReader.HasPlausibleBorders(real));

        using var damaged = DataMatrixEncoder.Encode("BORDERS");
        for (var y = 0; y < damaged.Height; y++)
        {
            damaged[0, y] = (y & 1) == 0;
        }

        Assert.IsFalse(BbDataMatrixReader.HasPlausibleBorders(damaged));

        var random = new Random(99);
        using var noise = new BbBitMatrix(real.Width, real.Height);
        for (var y = 0; y < noise.Height; y++)
        {
            for (var x = 0; x < noise.Width; x++)
            {
                noise[x, y] = random.Next(2) == 0;
            }
        }

        Assert.IsFalse(BbDataMatrixReader.HasPlausibleBorders(noise));
    }
}
