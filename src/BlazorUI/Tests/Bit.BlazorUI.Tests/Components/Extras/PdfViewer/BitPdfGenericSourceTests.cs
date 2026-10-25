using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

[TestClass]
public class BitPdfGenericSourceTests
{
    [TestMethod]
    [DataRow("hello")]
    [DataRow("encrypted")]
    [DataRow("attachments")]
    [DataRow("layers")]
    [DataRow("rich")]
    [DataRow("resources")]
    [DataRow("damaged")]
    [DataRow("prefixed")]
    [DataRow("xref-stream")]
    [DataRow("incremental")]
    [DataRow("article.pdf")]
    [DataRow("sample-pdf.pdf")]
    [DataRow("hello-world.pdf")]
    public void GenericSourceShouldMatchBufferedDocumentAndBothRenderers(string fixture)
    {
        byte[] bytes = Fixture(fixture);
        string? password = fixture == "encrypted" ? "hunter2" : null;
        var expected = BitPdfDocument.Load(bytes, password);
        var source = SourceProxy.Create(bytes);
        var actual = source.Load(password);

        Assert.AreEqual(expected.Version, actual.Version);
        Assert.AreEqual(expected.PageCount, actual.PageCount);
        Assert.AreEqual(expected.Metadata?.Title, actual.Metadata?.Title);
        CollectionAssert.AreEqual(expected.Warnings.ToArray(), actual.Warnings.ToArray());
        Assert.AreEqual(expected.Attachments.Count, actual.Attachments.Count);
        for (int i = 0; i < expected.Attachments.Count; i++)
        {
            CollectionAssert.AreEqual(expected.Attachments[i].Content, actual.Attachments[i].Content);
        }
        for (int i = 0; i < actual.PageCount; i++)
        {
            var page = actual.Pages[i];
            CollectionAssert.AreEqual(expected.Pages[i].GetContentBytes(), page.GetContentBytes());
            Assert.AreEqual(expected.Pages[i].ExtractText(), page.ExtractText());
            Assert.AreEqual(new BitPdfHtmlRenderer(expected.Pages[i], expected.XRef).Render(),
                new BitPdfHtmlRenderer(page, actual.XRef).Render());
            Assert.AreEqual(JsonSerializer.Serialize(new BitPdfCanvasRenderer(expected.Pages[i], expected.XRef).Render()),
                JsonSerializer.Serialize(new BitPdfCanvasRenderer(page, actual.XRef).Render()));
        }

        Assert.IsTrue(source.Views.Count > 0);
        Assert.IsTrue(source.Windows.Count > 0);
        Assert.IsTrue(source.Windows.All(window => window is not BitPdfStream));
        // One document reader borrows the first block once. Per-object readers
        // would repeatedly ask for it during stream-boundary parsing.
        if (bytes.Length < 64 * 1024 && fixture != "damaged")
        {
            int expectedFullViews = 1 + (bytes.Length <= 1029 ? 1 : 0)
                + (bytes.Length <= 2048 ? 1 : 0) + (bytes.Length <= 1024 ? 1 : 0);
            Assert.AreEqual(expectedFullViews, source.Views.Count(view => view.Offset == 0 && view.Length == bytes.Length));
        }
    }

    [TestMethod]
    public void GenericSourceShouldPreservePasswordErrorsAndSupportAFreshRetry()
    {
        var source = SourceProxy.Create(TestPdf.Encrypted("hunter2"));
        Assert.ThrowsExactly<BitPdfPasswordException>(() => source.Load("wrong"));
        Assert.AreEqual(1, source.Load("hunter2").PageCount);
    }

    [TestMethod]
    public void StandaloneParserShouldUseItsDecodedBytesEvenWhenItHasADocumentXRef()
    {
        var source = SourceProxy.Create(TestPdf.HelloWorld());
        var document = source.Load(null);
        int originalReads = source.Views.Count;
        byte[] decoded = Encoding.ASCII.GetBytes("<< /Length 3 >>\nstream\nabc\nendstream\nendobj\n42");
        var parser = new BitPdfParser(new BitPdfLexer(new BitPdfStream(decoded)), document.XRef);

        var stream = parser.GetObj() as BitPdfStream;
        Assert.IsNotNull(stream);
        Assert.AreSame(decoded, stream.Buffer);
        CollectionAssert.AreEqual("abc"u8.ToArray(), stream.GetBytes());
        Assert.AreEqual(42d, parser.GetObj());
        Assert.AreEqual(originalReads, source.Views.Count);
    }

    [TestMethod]
    public void PublicType3GlyphAccessorShouldPreserveItsReturnTypeForGenericStreams()
    {
        var document = SourceProxy.Create(Resources()).Load(null);
        var fontDict = ((BitPdfDict)document.Pages[0].Resources!.Get("Font")!).Get("F1") as BitPdfDict;
        Assert.IsNotNull(fontDict);
        var font = BitPdfFont.Create(fontDict, document.XRef);
        Assert.IsNotNull(font.Type3);
        var glyph = font.Type3.GetGlyphProcedure(65);
        Assert.IsNotNull(glyph);
        StringAssert.Contains(Encoding.ASCII.GetString(glyph.GetBytes()), "600 700 re f");
        StringAssert.Contains(document.Pages[0].ExtractText(), "A");
        StringAssert.Contains(new BitPdfHtmlRenderer(document.Pages[0], document.XRef).Render(), "data:image/png;base64,");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void InlineImagesShouldWorkWithoutAConcreteStreamAndResumeAtTheNextOperator(bool filtered)
    {
        string filter = filtered ? " /F /ASCIIHexDecode" : "";
        string data = filtered ? "414243>" : "ABC";
        byte[] content = Encoding.ASCII.GetBytes($"BI /W 1 /H 1 /CS /RGB /BPC 8{filter} ID\r\n{data} EI Q");
        var opaque = new OpaqueStream(content, 0, content.Length);
        var ctor = typeof(BitPdfContentParser).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
            null, [typeof(BitPdfBaseStream)], null)!;
        var generic = (BitPdfContentParser)ctor.Invoke([opaque]);
        var expected = new BitPdfContentParser(content).Parse();
        var actual = generic.Parse();

        Assert.AreEqual(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual));
        Assert.AreEqual("Q", actual[^1].Operator);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void InlineImagesShouldRespectSubwindowsAndIgnoreMarkersInsideKnownBinarySamples(bool opaque)
    {
        string content = "BI /W 3 /H 1 /CS /RGB /BPC 8 ID x EI yz?? EI";
        byte[] bytes = Encoding.ASCII.GetBytes("prefix " + content + " outside");
        BitPdfBaseStream stream = opaque
            ? new OpaqueStream(bytes, 7, content.Length)
            : new BitPdfStream(bytes, 7, content.Length);
        var ctor = typeof(BitPdfContentParser).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
            null, [typeof(BitPdfBaseStream)], null)!;
        var parser = (BitPdfContentParser)ctor.Invoke([stream]);
        var operations = parser.Parse();
        Assert.AreEqual(1, operations.Count);
        CollectionAssert.AreEqual("x EI yz??"u8.ToArray(), (byte[])operations[0].Operands[1]!);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void InlineImagesShouldKeepTheExistingErrorForAnImpossibleDeclaredLength(bool opaque)
    {
        byte[] bytes = "BI /W 100 /H 1 /CS /RGB /BPC 8 ID ABC EI Q"u8.ToArray();
        BitPdfBaseStream stream = opaque ? new OpaqueStream(bytes, 0, bytes.Length) : new BitPdfStream(bytes);
        var ctor = typeof(BitPdfContentParser).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
            null, [typeof(BitPdfBaseStream)], null)!;
        var parser = (BitPdfContentParser)ctor.Invoke([stream]);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => parser.Parse());
    }

    internal static byte[] Fixture(string name) => name switch
    {
        "hello" => TestPdf.HelloWorld(),
        "encrypted" => TestPdf.Encrypted("hunter2"),
        "attachments" => TestPdf.WithAttachments(),
        "layers" => TestPdf.WithLayers(),
        "rich" => TestPdf.RichDocument(),
        "resources" => Resources(),
        "damaged" => Damage(TestPdf.HelloWorld()),
        "prefixed" => Encoding.ASCII.GetBytes("junk\n").Concat(TestPdf.HelloWorld()).ToArray(),
        "xref-stream" => XRefStream(),
        "incremental" => Incremental(),
        _ => File.ReadAllBytes(SourceFiles.GetPath("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "wwwroot", "samples", name)),
    };

    private static byte[] Damage(byte[] bytes)
    {
        string text = Encoding.Latin1.GetString(bytes);
        int start = text.LastIndexOf("startxref\n", StringComparison.Ordinal) + "startxref\n".Length;
        int end = text.IndexOf('\n', start);
        return Encoding.Latin1.GetBytes(text[..start] + "0" + text[end..]);
    }

    private static byte[] Resources() => TestPdf.Build([
        "<< /Type /Catalog /Pages 2 0 R /Metadata 9 0 R >>",
        "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
        "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 100 100] /Contents 4 0 R /Annots [17 0 R] " +
            "/Resources << /Font << /F1 5 0 R >> /XObject << /Im1 7 0 R /Im2 12 0 R /Fm1 18 0 R >> " +
            "/ColorSpace << /CS1 [/ICCBased 13 0 R] >> /Shading << /Sh1 14 0 R /Sh2 21 0 R >> /Pattern << /P1 19 0 R >> >> >>",
        TestPdf.Stream("BT /F1 12 Tf 10 20 Td (A) Tj ET q 20 0 0 20 40 40 cm /Im1 Do Q " +
            "q 10 0 0 10 60 40 cm /Im2 Do Q /Fm1 Do q /CS1 cs 1 0 0 scn 5 5 10 10 re f Q " +
            "q /Sh1 sh Q q /Sh2 sh Q q /Pattern cs /P1 scn 5 50 10 10 re f Q"),
        "<< /Type /Font /Subtype /Type3 /FontBBox [0 0 600 700] /FontMatrix [0.001 0 0 0.001 0 0] " +
            "/CharProcs << /A 6 0 R >> /Encoding << /Type /Encoding /Differences [65 /A] >> " +
            "/FirstChar 65 /LastChar 65 /Widths [600] /Resources << >> /ToUnicode 8 0 R >>",
        TestPdf.Stream("600 0 0 0 600 700 d1 0 0 600 700 re f"),
        TestPdf.Stream("ABC", " /Type /XObject /Subtype /Image /Width 1 /Height 1 /ColorSpace /DeviceRGB /BitsPerComponent 8 /SMask 10 0 R"),
        TestPdf.Stream("1 beginbfchar <41> <0041> endbfchar"),
        TestPdf.Stream("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\" />", "/Type /Metadata /Subtype /XML"),
        TestPdf.Stream("\u00ff", " /Type /XObject /Subtype /Image /Width 1 /Height 1 /ColorSpace /DeviceGray /BitsPerComponent 8"),
        TestPdf.Stream("ABCDEF"),
        TestPdf.Stream("\0", " /Type /XObject /Subtype /Image /Width 1 /Height 1 /ColorSpace [/Indexed /DeviceRGB 1 11 0 R] /BitsPerComponent 8"),
        TestPdf.Stream("profile", " /N 3 /Alternate /DeviceRGB"),
        "<< /ShadingType 2 /ColorSpace /DeviceRGB /Coords [0 0 100 0] /Function 15 0 R /Extend [true true] >>",
        TestPdf.Stream("\0\0\0\u00ff\0\0", " /FunctionType 0 /Domain [0 1] /Range [0 1 0 1 0 1] /Size [2] /BitsPerSample 8"),
        TestPdf.Stream("0 0 1 rg 0 0 10 10 re f", " /Type /XObject /Subtype /Form /BBox [0 0 10 10] /Resources << >>"),
        "<< /Type /Annot /Subtype /Stamp /Rect [70 70 80 80] /AP << /N 16 0 R >> >>",
        TestPdf.Stream("BT /F1 6 Tf 10 70 Td (A) Tj ET", " /Type /XObject /Subtype /Form /BBox [0 0 100 100] /Resources << /Font << /F1 5 0 R >> >>"),
        TestPdf.Stream("1 0 0 rg 0 0 2 2 re f", " /Type /Pattern /PatternType 1 /PaintType 1 /TilingType 1 /BBox [0 0 2 2] /XStep 2 /YStep 2 /Resources << >>"),
        TestPdf.Stream("{ dup dup }", " /FunctionType 4 /Domain [0 1] /Range [0 1 0 1 0 1]"),
        "<< /ShadingType 2 /ColorSpace /DeviceRGB /Coords [0 0 0 100] /Function 20 0 R /Extend [true true] >>",
    ], rootObjNum: 1);

    private static byte[] XRefStream()
    {
        using var output = new MemoryStream();
        void Write(string text) => output.Write(Encoding.Latin1.GetBytes(text));
        Write("%PDF-1.7\n");
        string[] bodies = [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 100 100] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            TestPdf.Stream("BT /F1 12 Tf 10 20 Td (Hello) Tj ET"),
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            TestPdf.Stream("7 0 << /Title (Compressed metadata) >>", " /Type /ObjStm /N 1 /First 4"),
        ];
        var offsets = new int[9];
        for (int i = 0; i < bodies.Length; i++)
        {
            offsets[i + 1] = (int)output.Position;
            Write($"{i + 1} 0 obj\n{bodies[i]}\nendobj\n");
        }
        offsets[8] = (int)output.Position;
        var entries = new byte[9 * 7];
        for (int i = 0; i < 9; i++)
        {
            int at = i * 7;
            entries[at] = (byte)(i == 0 ? 0 : i == 7 ? 2 : 1);
            int field = i == 7 ? 6 : offsets[i];
            for (int n = 0; n < 4; n++) entries[at + 1 + n] = (byte)(field >> (24 - n * 8));
            if (i == 0) entries[at + 5] = entries[at + 6] = 255;
        }
        Write($"8 0 obj\n<< /Type /XRef /Size 9 /Root 1 0 R /Info 7 0 R /W [1 4 2] /Length {entries.Length} >>\nstream\n");
        output.Write(entries);
        Write($"\nendstream\nendobj\nstartxref\n{offsets[8]}\n%%EOF");
        return output.ToArray();
    }

    private static byte[] Incremental()
    {
        byte[] bytes = TestPdf.HelloWorld();
        string text = Encoding.Latin1.GetString(bytes);
        int at = text.LastIndexOf("startxref\n", StringComparison.Ordinal) + "startxref\n".Length;
        int previous = int.Parse(text[at..text.IndexOf('\n', at)]);
        string info = "\n10 0 obj\n<< /Title (Updated metadata) >>\nendobj\n";
        int offset = bytes.Length + 1;
        int xref = bytes.Length + Encoding.Latin1.GetByteCount(info);
        string tail = $"xref\n10 1\n{offset:D10} 00000 n \ntrailer\n<< /Size 11 /Root 1 0 R /Info 10 0 R /Prev {previous} >>\nstartxref\n{xref}\n%%EOF";
        return bytes.Concat(Encoding.Latin1.GetBytes(info + tail)).ToArray();
    }

    // Bridge to the actual internal contract of the signed Extras assembly.
    // The compiled-link reader tests remain independent; no production API or
    // friend assembly is added solely for the integration fixture.
    private class SourceProxy : DispatchProxy
    {
        private byte[] _bytes = [];
        public List<(int Offset, int Length)> Views { get; } = [];
        public List<BitPdfBaseStream> Windows { get; } = [];
        public SourceProxy() { }

        public static SourceProxy Create(byte[] bytes)
        {
            var contract = typeof(BitPdfDocument).Assembly.GetType("Bit.BlazorUI.IBitPdfByteSource", throwOnError: true)!;
            var source = (SourceProxy)DispatchProxy.Create(contract, typeof(SourceProxy));
            source._bytes = bytes;
            return source;
        }

        public BitPdfDocument Load(string? password)
        {
            var method = typeof(BitPdfDocument).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .Single(method => method.Name == "Load" && method.GetParameters().Length == 2);
            try
            {
                return (BitPdfDocument)method.Invoke(null, [this, password])!;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            switch (method!.Name)
            {
                case "get_Length": return _bytes.Length;
                case "TryGetContiguous":
                    args![0] = default(ReadOnlyMemory<byte>);
                    return false;
                case "GetMemory":
                    int offset = (int)args![0]!;
                    int length = (int)args[1]!;
                    Assert.IsTrue(offset >= 0 && offset <= _bytes.Length && length >= 0 && length <= _bytes.Length - offset);
                    Views.Add((offset, length));
                    return Activator.CreateInstance(method.ReturnType, [(ReadOnlyMemory<byte>)_bytes.AsMemory(offset, length), null]);
                case "CreateStream":
                    int start = (int)args![0]!;
                    int count = (int?)args[1] ?? _bytes.Length - start;
                    Assert.IsTrue(start >= 0 && start <= _bytes.Length && count >= 0 && count <= _bytes.Length - start);
                    var stream = new OpaqueStream(_bytes, start, count);
                    Windows.Add(stream);
                    return stream;
                default: throw new AssertFailedException($"Unexpected source member: {method.Name}");
            }
        }
    }

    private sealed class OpaqueStream : BitPdfBaseStream
    {
        private readonly byte[] _bytes;
        public OpaqueStream(byte[] bytes, int start, int length)
        {
            _bytes = bytes;
            Start = Pos = start;
            End = start + length;
        }
        public override int GetByte() => Pos >= End ? -1 : _bytes[Pos++];
        public override byte[] GetBytes(int length = 0)
        {
            int count = length <= 0 ? End - Pos : Math.Min(length, End - Pos);
            byte[] result = _bytes.AsSpan(Pos, count).ToArray();
            Pos += count;
            return result;
        }
        public override byte[] GetByteRange(int begin, int end)
            => _bytes.AsSpan(Math.Max(0, begin), Math.Max(0, Math.Min(end, _bytes.Length) - Math.Max(0, begin))).ToArray();
        public override void Reset() => Pos = Start;
        public override void MoveStart() => Start = Pos;
        public override BitPdfBaseStream MakeSubStream(int start, int length, BitPdfDict? dict = null)
            => new OpaqueStream(_bytes, start, length) { Dict = dict };
    }
}
