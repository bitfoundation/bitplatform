using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

/// <summary>
/// PDF strings from the lexer to the text an app reads: literal strings (escapes, octal
/// codes, balanced parentheses, line continuations), hexadecimal strings, names, and the
/// three text-string encodings (PDFDocEncoding, UTF-16BE and UTF-8 with their BOMs) as they
/// reach the document's metadata and outline.
/// </summary>
[TestClass]
public class BitPdfStringEncodingTests
{
    private static object Parse(string pdf)
        => new BitPdfParser(new BitPdfLexer(new BitPdfStream(Encoding.Latin1.GetBytes(pdf)))).GetObj();

    private static byte[] StringBytes(string pdf) => ((BitPdfString)Parse(pdf)).Bytes;

    private static string Latin1(string pdf) => Encoding.Latin1.GetString(StringBytes(pdf));

    // ----- Literal strings -----

    [TestMethod]
    public void ALiteralStringShouldDecodeEveryNamedEscape()
    {
        Assert.AreEqual("a\nb\rc\td\be\ff(g)h\\i", Latin1(@"(a\nb\rc\td\be\ff\(g\)h\\i)"));
    }

    [TestMethod]
    public void ALiteralStringShouldDecodeOneToThreeDigitOctalEscapes()
    {
        // \101 = 'A', \60 = '0', \0053 = \005 then '3', \7 = 0x07, \777 overflows and keeps the low byte.
        CollectionAssert.AreEqual(new byte[] { 0x41, 0x30, 0x05, 0x33, 0x07, 0xFF }, StringBytes(@"(\101\60\0053\7\777)"));
    }

    [TestMethod]
    public void ALiteralStringShouldKeepBalancedParenthesesUnescaped()
    {
        Assert.AreEqual("outer (inner (deep)) end", Latin1("(outer (inner (deep)) end)"));
    }

    [TestMethod]
    public void AnEscapedParenthesisShouldNotChangeTheNestingDepth()
    {
        Assert.AreEqual("a)b(c", Latin1(@"(a\)b\(c)"));
        // The string ends at the first unescaped ')', and the next token follows it.
        var array = (List<object?>)Parse(@"[(x\)) (y)]");
        Assert.AreEqual(2, array.Count);
        Assert.AreEqual("x)", ((BitPdfString)array[0]!).AsLatin1());
    }

    [TestMethod]
    [DataRow("(abc\\\ndef)")]
    [DataRow("(abc\\\r\ndef)")]
    [DataRow("(abc\\\rdef)")]
    public void ABackslashBeforeAnEndOfLineShouldContinueTheString(string pdf)
    {
        Assert.AreEqual("abcdef", Latin1(pdf));
    }

    [TestMethod]
    public void AnUnknownEscapeShouldKeepTheCharacterAndDropTheBackslash()
    {
        // PDF 32000-1 §7.3.4.2: "If the character following the REVERSE SOLIDUS is not one of
        // those shown in Table 3, the REVERSE SOLIDUS shall be ignored."
        Assert.AreEqual("qx", Latin1(@"(\q\x)"));
    }

    [TestMethod]
    public void AnUnterminatedLiteralStringShouldReturnWhatItRead()
    {
        Assert.AreEqual("never closed (nested", Latin1("(never closed (nested"));
    }

    // PDF 32000-1 §7.3.4.2: an end-of-line marker inside a literal string with no backslash
    // before it is read as a single LF, whether it was CR, LF or CR LF. (The lexer used to
    // keep the CR, so a title written over two lines on Windows kept it.)
    [TestMethod]
    [DataRow("(a\r\nb)")]
    [DataRow("(a\rb)")]
    [DataRow("(a\nb)")]
    public void AnUnescapedEndOfLineInALiteralStringShouldBeReadAsALineFeed(string pdf)
    {
        Assert.AreEqual("a\nb", Latin1(pdf));
    }

    [TestMethod]
    public void AnEscapedCarriageReturnInALiteralStringShouldStayACarriageReturn()
    {
        // Only the bare end-of-line marker is normalized: the \r escape names the byte itself, and a
        // CR LF pair split by the escape is a CR followed by a bare LF.
        Assert.AreEqual("a\rb", Latin1(@"(a\rb)"));
        Assert.AreEqual("a\r\nb", Latin1("(a\\r\nb)"));
    }

    // ----- Hexadecimal strings and names -----

    [TestMethod]
    public void AHexStringShouldDecodeBothCasesAndSkipWhitespace()
    {
        Assert.AreEqual("Hello", Latin1("<48 65\n6c6C\t6F>"));
    }

    [TestMethod]
    public void AHexStringWithAnOddDigitCountShouldPadTheLastNibbleWithZero()
    {
        CollectionAssert.AreEqual(new byte[] { 0x90, 0x1F, 0xA0 }, StringBytes("<901FA>"));
    }

    [TestMethod]
    public void AnEmptyHexStringShouldBeEmpty()
    {
        Assert.AreEqual(0, StringBytes("<>").Length);
    }

    [TestMethod]
    public void ANameShouldDecodeItsHashEscapes()
    {
        Assert.AreEqual("Lime Green", ((BitPdfName)Parse("/Lime#20Green")).Value);
        Assert.AreEqual("A#B", ((BitPdfName)Parse("/A#23B")).Value);
        Assert.AreEqual("paired()parentheses", ((BitPdfName)Parse("/paired#28#29parentheses")).Value);
    }

    // ----- Text-string encodings -----

    [TestMethod]
    public void AsTextShouldDecodeUtf16BigEndianAfterItsBom()
    {
        // "Grüße" plus a supplementary-plane character (U+1F600) as a surrogate pair.
        byte[] bytes = [0xFE, 0xFF, .. Encoding.BigEndianUnicode.GetBytes("Grüße 😀")];

        Assert.AreEqual("Grüße 😀", new BitPdfString(bytes).AsText());
    }

    [TestMethod]
    public void AsTextShouldDecodeUtf8AfterItsBom()
    {
        byte[] bytes = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("Ελληνικά")];

        Assert.AreEqual("Ελληνικά", new BitPdfString(bytes).AsText());
    }

    [TestMethod]
    [DataRow(0x18, "˘")]
    [DataRow(0x1F, "˜")]
    [DataRow(0x80, "•")]
    [DataRow(0x8D, "“")]
    [DataRow(0x8E, "”")]
    [DataRow(0x92, "™")]
    [DataRow(0x93, "ﬁ")]
    [DataRow(0x96, "Œ")]
    [DataRow(0x9E, "ž")]
    [DataRow(0xA0, "€")]
    [DataRow(0xA9, "©")]
    [DataRow(0xE9, "é")]
    [DataRow(0x41, "A")]
    public void AsTextShouldDecodePdfDocEncodingWithoutABom(int code, string expected)
    {
        Assert.AreEqual(expected, new BitPdfString([(byte)code]).AsText());
    }

    [TestMethod]
    public void AsTextShouldNotTakeALoneFeByteForABom()
    {
        // 0xFE alone (no 0xFF after it) is PDFDocEncoding's thorn, not half a BOM.
        Assert.AreEqual("þa", new BitPdfString([0xFE, 0x61]).AsText());
    }

    // ----- Through a document -----

    private static byte[] WithInfoAndOutline(string titleToken, string authorToken, string outlineTitleToken)
    {
        var file = new BitPdfEngineTestFile();
        file.Obj(1, "<< /Type /Catalog /Pages 2 0 R /Outlines 7 0 R >>");
        file.Obj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        file.Obj(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>");
        file.Stream(4, @"BT /F1 12 Tf 10 100 Td (a\(b\)c \101) Tj ET");
        file.Obj(5, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        file.Obj(6, $"<< /Title {titleToken} /Author {authorToken} /Producer (Caf\\351 \\200 \\222) >>");
        file.Obj(7, "<< /Type /Outlines /First 8 0 R /Last 9 0 R /Count 2 >>");
        file.Obj(8, $"<< /Title {outlineTitleToken} /Parent 7 0 R /Next 9 0 R /Dest [3 0 R /Fit] >>");
        file.Obj(9, @"<< /Title (Chapter \(two\)) /Parent 7 0 R /Prev 8 0 R /Dest [3 0 R /Fit] >>");
        file.XRefTable("/Size 10 /Root 1 0 R /Info 6 0 R", Enumerable.Range(0, 10));
        return file.ToArray();
    }

    [TestMethod]
    public void AUtf16TitleWrittenAsAHexStringShouldReachTheMetadata()
    {
        string hex = "<FEFF" + Hex(Encoding.BigEndianUnicode.GetBytes("Résumé – 日本語")) + ">";

        var document = BitPdfDocument.Load(WithInfoAndOutline(hex, "(plain)", "(One)"));

        Assert.AreEqual("Résumé – 日本語", document.Metadata.Title);
        Assert.AreEqual("plain", document.Metadata.Author);
    }

    [TestMethod]
    public void AUtf16TitleWrittenWithOctalEscapesShouldReachTheMetadata()
    {
        // The same BOM-prefixed UTF-16BE bytes, spelled as a literal string: "\376\377\000H\000i".
        var document = BitPdfDocument.Load(WithInfoAndOutline(@"(\376\377\000H\000i)", @"(\376\377\004\037)", "(One)"));

        Assert.AreEqual("Hi", document.Metadata.Title);
        Assert.AreEqual("П", document.Metadata.Author);
    }

    [TestMethod]
    public void APdfDocEncodedInfoStringShouldMapItsSpecialBytes()
    {
        var document = BitPdfDocument.Load(WithInfoAndOutline("(t)", "(a)", "(One)"));

        Assert.AreEqual("Café • ™", document.Metadata.Producer);
    }

    [TestMethod]
    public void AUtf16OutlineTitleShouldDecode()
    {
        string hex = "<FEFF" + Hex(Encoding.BigEndianUnicode.GetBytes("Глава 1")) + ">";

        var document = BitPdfDocument.Load(WithInfoAndOutline("(t)", "(a)", hex));

        Assert.AreEqual(2, document.Outline.Count);
        Assert.AreEqual("Глава 1", document.Outline[0].Title);
        Assert.AreEqual(1, document.Outline[0].PageNumber);
        Assert.AreEqual("Chapter (two)", document.Outline[1].Title);
    }

    [TestMethod]
    public void EscapesInAContentStreamStringShouldReachTheExtractedText()
    {
        var document = BitPdfDocument.Load(WithInfoAndOutline("(t)", "(a)", "(One)"));

        Assert.AreEqual("a(b)c A", document.Pages[0].ExtractText().Trim());
    }

    private static string Hex(byte[] bytes) => Convert.ToHexString(bytes);
}
