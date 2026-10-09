using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

[TestClass]
public class BitPdfByteSourceParsingTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ParserShouldReadStreamBoundariesAcrossBlocksAndResumeTokenizing(bool damagedLength)
    {
        // Put the endstream keyword across a 64 KiB boundary. The large declared
        // length also exercises bounds checking before offset + length arithmetic.
        const int keywordOffset = 65533;
        string prefix = "<< /Length 0000000000 >>\nstream\r\n";
        string content = new string(' ', keywordOffset - prefix.Length - 3) + "Q";
        int declaredLength = damagedLength ? int.MaxValue : content.Length;
        prefix = prefix.Replace("0000000000", declaredLength.ToString("D10"));
        byte[] bytes = Encoding.Latin1.GetBytes(prefix + content + "\r\nendstream\r\nendobj\n42");
        var parser = new BitPdfParser(new BitPdfLexer(new BitPdfStream(bytes)));

        var stream = parser.GetObj() as BitPdfStream;
        Assert.IsNotNull(stream);
        Assert.AreSame(bytes, stream.Buffer);
        Assert.AreEqual(prefix.Length, stream.Start);
        Assert.AreEqual(prefix.Length + content.Length, stream.End);
        Assert.AreEqual((double)declaredLength, stream.Dict!.Get("Length"));
        CollectionAssert.AreEqual(Encoding.Latin1.GetBytes(content), stream.GetBytes());
        Assert.AreEqual(42d, parser.GetObj());
    }

    [TestMethod]
    public void ParserShouldKeepRawBinaryStreamBytesAndIndependentCursorViews()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("<< /Length 4 >>\nstream\n\0\u00ffAQ\nendstream\n17");
        var parser = new BitPdfParser(new BitPdfLexer(new BitPdfStream(bytes)));
        var stream = parser.GetObj() as BitPdfStream;
        Assert.IsNotNull(stream);

        var sub = stream.MakeSubStream(stream.Start + 1, 2);
        Assert.AreEqual(255, sub.GetByte());
        Assert.AreEqual(stream.Start, stream.Pos);
        byte[] copy = stream.GetBytes();
        CollectionAssert.AreEqual(new byte[] { 0, 255, 65, 81 }, copy);
        copy[0] = 99;
        stream.Reset();
        Assert.AreEqual(0, stream.GetByte());
        Assert.AreEqual(17d, parser.GetObj());
    }

    [TestMethod]
    public void DocumentShouldRecoverDamagedXrefWithTheSamePageOutput()
    {
        byte[] original = TestPdf.HelloWorld();
        string text = Encoding.Latin1.GetString(original);
        int startxref = text.LastIndexOf("startxref\n", StringComparison.Ordinal);
        Assert.IsTrue(startxref >= 0);
        byte[] damaged = Encoding.Latin1.GetBytes(text[..startxref] + "startxref\n0\n%%EOF");

        var reference = BitPdfDocument.Load(original);
        var recovered = BitPdfDocument.Load(damaged);
        Assert.AreEqual(reference.PageCount, recovered.PageCount);
        Assert.IsTrue(recovered.Warnings.Count > 0);
        Assert.AreEqual(
            new BitPdfHtmlRenderer(reference.Pages[0], reference.XRef).Render(),
            new BitPdfHtmlRenderer(recovered.Pages[0], recovered.XRef).Render());
        Assert.AreEqual(
            new BitPdfCanvasRenderer(reference.Pages[0], reference.XRef).Render(),
            new BitPdfCanvasRenderer(recovered.Pages[0], recovered.XRef).Render());
    }

    [TestMethod]
    public void DocumentShouldPreserveHeaderRelativeXrefAndObjectOffsets()
    {
        byte[] original = TestPdf.HelloWorld();
        byte[] prefix = Encoding.ASCII.GetBytes("Junk preceding the PDF header\r\n");
        var prefixed = new byte[prefix.Length + original.Length];
        prefix.CopyTo(prefixed, 0);
        original.CopyTo(prefixed, prefix.Length);

        var document = BitPdfDocument.Load(prefixed);
        Assert.AreEqual("1.7", document.Version);
        Assert.AreEqual(1, document.PageCount);
        Assert.AreEqual(0, document.Warnings.Count);
        StringAssert.Contains(new BitPdfHtmlRenderer(document.Pages[0], document.XRef).Render(), "Hello");
    }
}
