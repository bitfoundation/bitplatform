using System;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

/// <summary>
/// The stream filter pipeline (<see cref="BitPdfStreamDecoder"/>): every filter the engine
/// decodes, the PNG and TIFF predictors, filter chains, and what a damaged stream does to
/// the rest of its document. The fixtures are encoded by the test's own encoders, never by
/// the engine.
/// </summary>
[TestClass]
public class BitPdfStreamFilterTests
{
    private static readonly byte[] Sample = Encoding.ASCII.GetBytes(
        "BT /F1 12 Tf 10 100 Td (Hello, filters!) Tj ET\nq 1 0 0 rg 0 0 50 50 re f Q\n");

    // A deterministic, mostly incompressible buffer long enough to reach every LZW code width.
    private static byte[] Noise(int length, int seed = 7)
    {
        var bytes = new byte[length];
        new Random(seed).NextBytes(bytes);
        return bytes;
    }

    private static byte[] Decode(byte[] encoded, string dictEntries)
    {
        var parser = new BitPdfParser(new BitPdfLexer(new BitPdfStream(Encoding.Latin1.GetBytes($"<< {dictEntries} >>"))));
        var dict = (BitPdfDict)parser.GetObj();
        return BitPdfStreamDecoder.Decode(new BitPdfStream(encoded, dict: dict));
    }

    // ----- FlateDecode -----

    [TestMethod]
    public void FlateDecodeShouldInflateAZlibStream()
    {
        byte[] decoded = Decode(BitPdfEngineTestFile.Zlib(Sample), "/Filter /FlateDecode");

        CollectionAssert.AreEqual(Sample, decoded);
    }

    [TestMethod]
    public void FlateDecodeShouldAcceptARawDeflateStreamWithNoZlibHeader()
    {
        byte[] decoded = Decode(BitPdfEngineTestFile.RawDeflate(Sample), "/Filter /FlateDecode");

        CollectionAssert.AreEqual(Sample, decoded);
    }

    [TestMethod]
    public void FlateDecodeShouldAcceptTheAbbreviatedFilterNameInAnArray()
    {
        byte[] decoded = Decode(BitPdfEngineTestFile.Zlib(Sample), "/Filter [/Fl]");

        CollectionAssert.AreEqual(Sample, decoded);
    }

    [TestMethod]
    public void FlateDecodeShouldKeepWhatInflatedBeforeATruncation()
    {
        byte[] input = Encoding.ASCII.GetBytes(string.Concat(Enumerable.Range(0, 400).Select(i => $"line {i} ")));
        byte[] zlib = BitPdfEngineTestFile.Zlib(input);
        byte[] truncated = zlib[..(zlib.Length / 2)];

        byte[] decoded = Decode(truncated, "/Filter /FlateDecode");

        Assert.IsTrue(decoded.Length > 0, "The bytes inflated before the cut are kept.");
        CollectionAssert.AreEqual(input[..decoded.Length], decoded);
    }

    [TestMethod]
    public void FlateDecodeShouldKeepTheDataWhenTheAdlerChecksumIsWrong()
    {
        byte[] zlib = BitPdfEngineTestFile.Zlib(Sample);
        zlib[^1] ^= 0xFF; // corrupt the trailing Adler-32

        byte[] decoded = Decode(zlib, "/Filter /FlateDecode");

        CollectionAssert.AreEqual(Sample, decoded);
    }

    [TestMethod]
    public void FlateDecodeOfGarbageShouldReturnNothingRatherThanThrow()
    {
        byte[] decoded = Decode(Encoding.ASCII.GetBytes("this is not deflate data at all"), "/Filter /FlateDecode");

        Assert.AreEqual(0, decoded.Length);
    }

    [TestMethod]
    [DataRow(0, DisplayName = "None")]
    [DataRow(1, DisplayName = "Sub")]
    [DataRow(2, DisplayName = "Up")]
    [DataRow(3, DisplayName = "Average")]
    [DataRow(4, DisplayName = "Paeth")]
    [DataRow(-1, DisplayName = "A different filter on every row")]
    public void FlateDecodeShouldReversePngPredictors(int filterType)
    {
        // 6 rows of 4 one-byte pixels: rows that differ from the row above and within themselves.
        byte[] pixels = Enumerable.Range(0, 24).Select(i => (byte)((i * 37) ^ (i / 4 * 11))).ToArray();
        byte[] encoded = BitPdfEngineTestFile.Zlib(BitPdfEngineTestFile.PngPredict(pixels, rowBytes: 4, filterType));

        byte[] decoded = Decode(encoded, "/Filter /FlateDecode /DecodeParms << /Predictor 15 /Columns 4 >>");

        CollectionAssert.AreEqual(pixels, decoded);
    }

    [TestMethod]
    public void FlateDecodeShouldReverseAPngPredictorOverMultiByteRgbPixels()
    {
        // 3 rows of 2 RGB pixels: the Sub/Average/Paeth "left" neighbour is 3 bytes back.
        byte[] pixels = Enumerable.Range(0, 18).Select(i => (byte)(i * 13 + 5)).ToArray();
        byte[] encoded = BitPdfEngineTestFile.Zlib(BitPdfEngineTestFile.PngPredict(pixels, rowBytes: 6, filterType: -1, bytesPerPixel: 3));

        byte[] decoded = Decode(encoded, "/Filter /FlateDecode /DecodeParms << /Predictor 12 /Colors 3 /Columns 2 /BitsPerComponent 8 >>");

        CollectionAssert.AreEqual(pixels, decoded);
    }

    [TestMethod]
    public void FlateDecodeShouldReverseTheTiffPredictor()
    {
        byte[] pixels = Enumerable.Range(0, 24).Select(i => (byte)(i * 29 + 3)).ToArray();
        byte[] encoded = BitPdfEngineTestFile.Zlib(BitPdfEngineTestFile.TiffPredict(pixels, columns: 4, colors: 3));

        byte[] decoded = Decode(encoded, "/Filter /FlateDecode /DecodeParms << /Predictor 2 /Colors 3 /Columns 4 >>");

        CollectionAssert.AreEqual(pixels, decoded);
    }

    [TestMethod]
    public void FlateDecodeShouldIgnorePredictorOneAndAMissingDecodeParms()
    {
        byte[] encoded = BitPdfEngineTestFile.Zlib(Sample);

        CollectionAssert.AreEqual(Sample, Decode(encoded, "/Filter /FlateDecode /DecodeParms << /Predictor 1 >>"));
        CollectionAssert.AreEqual(Sample, Decode(encoded, "/Filter /FlateDecode /DecodeParms null"));
    }

    // ----- ASCIIHexDecode -----

    [TestMethod]
    public void AsciiHexDecodeShouldDecodeBothCasesAndSkipWhitespace()
    {
        byte[] decoded = Decode(Encoding.ASCII.GetBytes("48 65\n6c6C\t6F>"), "/Filter /ASCIIHexDecode");

        Assert.AreEqual("Hello", Encoding.ASCII.GetString(decoded));
    }

    [TestMethod]
    public void AsciiHexDecodeShouldStopAtTheEndOfDataMarker()
    {
        byte[] decoded = Decode(Encoding.ASCII.GetBytes("4142>4344"), "/Filter /AHx");

        Assert.AreEqual("AB", Encoding.ASCII.GetString(decoded));
    }

    [TestMethod]
    public void AsciiHexDecodeShouldTreatAnOddFinalDigitAsTheHighNibble()
    {
        // PDF 32000-1 §7.4.2: "the final digit shall be assumed to be 0".
        byte[] decoded = Decode(Encoding.ASCII.GetBytes("414>"), "/Filter /ASCIIHexDecode");

        CollectionAssert.AreEqual(new byte[] { 0x41, 0x40 }, decoded);
    }

    // ----- ASCII85Decode -----

    [TestMethod]
    public void Ascii85DecodeShouldDecodeAKnownVector()
    {
        // The classic Wikipedia/Adobe vector: "Man " -> "9jqo^", "sure." -> "F*2M7/c".
        CollectionAssert.AreEqual(Encoding.ASCII.GetBytes("Man "), Decode(Encoding.ASCII.GetBytes("9jqo^~>"), "/Filter /ASCII85Decode"));
        CollectionAssert.AreEqual(Encoding.ASCII.GetBytes("sure."), Decode(Encoding.ASCII.GetBytes("F*2M7/c~>"), "/Filter /A85"));
    }

    [TestMethod]
    public void Ascii85DecodeShouldExpandTheZShorthandAndSkipWhitespace()
    {
        byte[] decoded = Decode(Encoding.ASCII.GetBytes("z 9jq\no^\r\nz~>"), "/Filter /ASCII85Decode");

        CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 0, (byte)'M', (byte)'a', (byte)'n', (byte)' ', 0, 0, 0, 0 }, decoded);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(257)]
    public void Ascii85DecodeShouldRoundTripEveryFinalGroupLength(int length)
    {
        byte[] data = Noise(length, seed: length);
        byte[] encoded = Encoding.ASCII.GetBytes(BitPdfEngineTestFile.Ascii85(data));

        CollectionAssert.AreEqual(data, Decode(encoded, "/Filter /ASCII85Decode"));
    }

    // ----- LZWDecode -----

    [TestMethod]
    public void LzwDecodeShouldDecodeTheSpecificationExample()
    {
        // PDF 32000-1 §7.4.4.2, Example: "-----A---B" encodes to 80 0B 60 50 22 0C 0C 85 01.
        byte[] encoded = [0x80, 0x0B, 0x60, 0x50, 0x22, 0x0C, 0x0C, 0x85, 0x01];

        byte[] decoded = Decode(encoded, "/Filter /LZWDecode");

        Assert.AreEqual("-----A---B", Encoding.ASCII.GetString(decoded));
    }

    [TestMethod]
    public void LzwDecodeShouldRoundTripThroughEveryCodeWidthAndATableReset()
    {
        // Incompressible input adds one table entry per byte or two, so 9000 bytes walk the
        // codes through 9, 10, 11 and 12 bits and past at least one Clear code.
        byte[] data = Noise(9000);

        byte[] decoded = Decode(BitPdfEngineTestFile.Lzw(data), "/Filter /LZWDecode");

        CollectionAssert.AreEqual(data, decoded);
    }

    [TestMethod]
    public void LzwDecodeShouldHandleTheKwKwKCase()
    {
        // A run of one byte makes the encoder emit a code before the decoder has defined it.
        byte[] data = Enumerable.Repeat((byte)'a', 2000).Concat(Encoding.ASCII.GetBytes("abababababab")).ToArray();

        byte[] decoded = Decode(BitPdfEngineTestFile.Lzw(data), "/Filter /LZWDecode");

        CollectionAssert.AreEqual(data, decoded);
    }

    [TestMethod]
    public void LzwDecodeShouldHonourEarlyChangeZero()
    {
        byte[] data = Noise(3000, seed: 3);

        byte[] decoded = Decode(BitPdfEngineTestFile.Lzw(data, earlyChange: 0), "/Filter /LZWDecode /DecodeParms << /EarlyChange 0 >>");

        CollectionAssert.AreEqual(data, decoded);
    }

    [TestMethod]
    public void LzwDecodeShouldApplyAPngPredictor()
    {
        byte[] pixels = Enumerable.Range(0, 40).Select(i => (byte)(i * 7)).ToArray();
        byte[] encoded = BitPdfEngineTestFile.Lzw(BitPdfEngineTestFile.PngPredict(pixels, rowBytes: 8, filterType: 2));

        byte[] decoded = Decode(encoded, "/Filter /LZWDecode /DecodeParms << /Predictor 12 /Columns 8 >>");

        CollectionAssert.AreEqual(pixels, decoded);
    }

    [TestMethod]
    public void LzwDecodeOfACorruptCodeShouldKeepTheBytesDecodedBeforeIt()
    {
        // Clear, 'A', then code 511 - far past the next free code (259).
        // Bits: 100000000 001000001 111111111 -> padded to bytes.
        byte[] encoded = [0x80, 0x10, 0x7F, 0xE0];

        byte[] decoded = Decode(encoded, "/Filter /LZWDecode");

        Assert.AreEqual("A", Encoding.ASCII.GetString(decoded));
    }

    // ----- RunLengthDecode -----

    [TestMethod]
    public void RunLengthDecodeShouldExpandLiteralAndRepeatRunsAndStopAtEod()
    {
        // 2 -> copy 3 literal bytes; 254 -> repeat the next byte 3 times; 128 -> EOD.
        byte[] encoded = [2, (byte)'a', (byte)'b', (byte)'c', 254, (byte)'z', 128, (byte)'X', (byte)'Y'];

        byte[] decoded = Decode(encoded, "/Filter /RunLengthDecode");

        Assert.AreEqual("abczzz", Encoding.ASCII.GetString(decoded));
    }

    [TestMethod]
    public void RunLengthDecodeShouldStopQuietlyOnATruncatedRun()
    {
        byte[] encoded = [5, (byte)'a', (byte)'b']; // promises 6 literal bytes, holds 2

        byte[] decoded = Decode(encoded, "/Filter /RL");

        Assert.AreEqual("ab", Encoding.ASCII.GetString(decoded));
    }

    // ----- Chains -----

    [TestMethod]
    public void AChainShouldApplyItsFiltersInOrder()
    {
        string encoded = BitPdfEngineTestFile.AsciiHex(BitPdfEngineTestFile.Zlib(Sample));

        byte[] decoded = Decode(Encoding.ASCII.GetBytes(encoded), "/Filter [/ASCIIHexDecode /FlateDecode]");

        CollectionAssert.AreEqual(Sample, decoded);
    }

    [TestMethod]
    public void AChainShouldPairEachFilterWithItsOwnDecodeParms()
    {
        byte[] pixels = Enumerable.Range(0, 30).Select(i => (byte)(255 - (i * 9))).ToArray();
        byte[] flate = BitPdfEngineTestFile.Zlib(BitPdfEngineTestFile.PngPredict(pixels, rowBytes: 5, filterType: 4));
        string encoded = BitPdfEngineTestFile.Ascii85(flate);

        byte[] decoded = Decode(Encoding.ASCII.GetBytes(encoded),
            "/Filter [/ASCII85Decode /FlateDecode] /DecodeParms [null << /Predictor 12 /Columns 5 >>]");

        CollectionAssert.AreEqual(pixels, decoded);
    }

    [TestMethod]
    public void AThreeFilterChainShouldDecode()
    {
        byte[] lzw = BitPdfEngineTestFile.Lzw(Sample);
        string encoded = BitPdfEngineTestFile.AsciiHex(Encoding.ASCII.GetBytes(BitPdfEngineTestFile.Ascii85(lzw)));

        byte[] decoded = Decode(Encoding.ASCII.GetBytes(encoded), "/Filter [/AHx /A85 /LZW]");

        CollectionAssert.AreEqual(Sample, decoded);
    }

    [TestMethod]
    public void AnUnknownFilterShouldPassTheBytesThrough()
    {
        byte[] decoded = Decode(Sample, "/Filter /NoSuchDecode");

        CollectionAssert.AreEqual(Sample, decoded);
    }

    [TestMethod]
    public void TheInlineOnlyAbbreviationFShouldNotBeReadAsAFilterOnARegularStream()
    {
        // On a stream object /F is a file specification; only inline images abbreviate /Filter.
        byte[] decoded = Decode(Encoding.ASCII.GetBytes("4142>"), "/F /AHx");

        Assert.AreEqual("4142>", Encoding.ASCII.GetString(decoded));
    }

    // ----- Through a document -----

    [TestMethod]
    [DataRow("flate")]
    [DataRow("hex")]
    [DataRow("a85")]
    [DataRow("lzw")]
    [DataRow("rle")]
    [DataRow("chain")]
    public void APageContentStreamShouldRenderThroughEachFilter(string kind)
    {
        byte[] content = BitPdfEngineTestFile.ShowText("Filtered text");
        (byte[] data, string entries) = kind switch
        {
            "flate" => (BitPdfEngineTestFile.Zlib(content), "/Filter /FlateDecode"),
            "hex" => (Encoding.ASCII.GetBytes(BitPdfEngineTestFile.AsciiHex(content)), "/Filter /ASCIIHexDecode"),
            "a85" => (Encoding.ASCII.GetBytes(BitPdfEngineTestFile.Ascii85(content)), "/Filter /ASCII85Decode"),
            "lzw" => (BitPdfEngineTestFile.Lzw(content), "/Filter /LZWDecode"),
            "rle" => ([(byte)(content.Length - 1), .. content, 128], "/Filter /RunLengthDecode"),
            _ => (Encoding.ASCII.GetBytes(BitPdfEngineTestFile.Ascii85(BitPdfEngineTestFile.Zlib(content))), "/Filter [/ASCII85Decode /FlateDecode]"),
        };

        var document = BitPdfDocument.Load(BitPdfEngineTestFile.SinglePage(data, entries));

        CollectionAssert.AreEqual(content, document.Pages[0].GetContentBytes());
        Assert.AreEqual("Filtered text", document.Pages[0].ExtractText().Trim());
        StringAssert.Contains(new BitPdfHtmlRenderer(document.Pages[0], document.XRef).Render(), "Filtered text");
    }

    [TestMethod]
    public void AFilterChainGivenThroughIndirectObjectsShouldDecode()
    {
        byte[] content = BitPdfEngineTestFile.ShowText("Indirect chain");
        var file = new BitPdfEngineTestFile();
        file.Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        file.Obj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        file.Obj(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>");
        file.Stream(4, Encoding.ASCII.GetBytes(BitPdfEngineTestFile.AsciiHex(BitPdfEngineTestFile.Zlib(content))), "/Filter 6 0 R /DecodeParms [null 7 0 R]");
        file.Obj(5, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        file.Obj(6, "[/ASCIIHexDecode 8 0 R]");
        file.Obj(7, "<< /Predictor 1 >>");
        file.Obj(8, "/FlateDecode");
        file.XRefTable("/Size 9 /Root 1 0 R", [0, 1, 2, 3, 4, 5, 6, 7, 8]);

        var document = BitPdfDocument.Load(file.ToArray());

        Assert.AreEqual("Indirect chain", document.Pages[0].ExtractText().Trim());
    }

    [TestMethod]
    public void ACorruptStreamShouldNotStopTheOtherPagesFromRendering()
    {
        var file = new BitPdfEngineTestFile();
        file.Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        file.Obj(2, "<< /Type /Pages /Kids [3 0 R 4 0 R 5 0 R 6 0 R] /Count 4 >>");
        const string resources = "/Resources << /Font << /F1 7 0 R >> >>";
        file.Obj(3, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] {resources} /Contents 8 0 R >>");
        file.Obj(4, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] {resources} /Contents 9 0 R >>");
        file.Obj(5, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] {resources} /Contents 10 0 R >>");
        file.Obj(6, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] {resources} /Contents [11 0 R 12 0 R] >>");
        file.Obj(7, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        file.Stream(8, Noise(64, seed: 1), "/Filter /FlateDecode");                       // not deflate at all
        file.Stream(9, BitPdfEngineTestFile.Zlib(BitPdfEngineTestFile.ShowText("Page two")), "/Filter /FlateDecode");
        file.Stream(10, Noise(64, seed: 2), "/Filter /LZWDecode");                        // garbage codes
        file.Stream(11, "(unterminated string Tj BT /F1 12 Tf", "");                       // unparseable content
        file.Stream(12, BitPdfEngineTestFile.Zlib(BitPdfEngineTestFile.ShowText("Page four")), "/Filter /FlateDecode");
        file.XRefTable("/Size 13 /Root 1 0 R", Enumerable.Range(0, 13));

        var document = BitPdfDocument.Load(file.ToArray());

        Assert.AreEqual(4, document.PageCount);
        var html = document.Pages.Select(p => new BitPdfHtmlRenderer(p, document.XRef).Render()).ToList();
        StringAssert.Contains(html[1], "Page two");
        Assert.AreEqual("Page two", document.Pages[1].ExtractText().Trim());
        _ = document.Pages[0].ExtractText();
        _ = document.Pages[2].ExtractText();
        _ = document.Pages[3].ExtractText();
    }

    // BitPdfPredictor.Apply computed the row length in int, which this /DecodeParms wraps to
    // -1: a zero PNG stride, and a DivideByZeroException out of Decode that ExtractText (and
    // the viewer's search built on it) did not catch. An impossible geometry now decodes to
    // nothing, as undecodable Flate data does.
    [TestMethod]
    public void AnOverflowingPredictorShouldNotThrowOutOfTheDecoder()
    {
        // 16383 * 16385 * 16 = 2^32 - 16, which wraps to -16 in int: rowBytes = (-16 + 7) / 8 = -1.
        const string parms = "/DecodeParms << /Predictor 12 /Colors 16383 /Columns 16385 /BitsPerComponent 16 >>";
        byte[] data = BitPdfEngineTestFile.Zlib(BitPdfEngineTestFile.ShowText("x"));

        _ = Decode(data, "/Filter /FlateDecode " + parms);

        var document = BitPdfDocument.Load(BitPdfEngineTestFile.SinglePage(data, "/Filter /FlateDecode " + parms));
        _ = document.Pages[0].ExtractText();
    }
}
