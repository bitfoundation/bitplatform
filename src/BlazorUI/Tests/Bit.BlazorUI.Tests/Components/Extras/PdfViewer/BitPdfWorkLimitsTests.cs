using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

[TestClass]
public class BitPdfWorkLimitsTests
{
    private sealed class NoRefs : IBitPdfXRef
    {
        public static readonly NoRefs Instance = new();
        public object? Fetch(BitPdfRef reference, bool suppressEncryption = false) => null;
        public object? FetchIfRef(object? value, bool suppressEncryption = false) => value;
    }

    private static object Parse(string pdf)
        => new BitPdfParser(new BitPdfLexer(new BitPdfStream(Encoding.Latin1.GetBytes(pdf)))).GetObj();

    private static BitPdfDict Dict(string entries) => (BitPdfDict)Parse($"<< {entries} >>");

    private static BitPdfDocument Document(string catalogEntries, string pageEntries, Action<BitPdfEngineTestFile> objects, int[] numbers)
    {
        var file = new BitPdfEngineTestFile();
        file.Obj(1, $"<< /Type /Catalog /Pages 2 0 R {catalogEntries} >>");
        file.Obj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        file.Obj(3, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 100 100] {pageEntries} >>");
        objects(file);
        int[] all = [0, 1, 2, 3, .. numbers];
        file.XRefTable($"/Size {all.Max() + 1} /Root 1 0 R", all);
        return BitPdfDocument.Load(file.ToArray());
    }

    private static byte[] Encrypt(byte[] plain, ushort r)
    {
        var cipher = new byte[plain.Length];
        for (int i = 0; i < plain.Length; i++)
        {
            byte c = (byte)(plain[i] ^ (r >> 8));
            cipher[i] = c;
            r = (ushort)((c + r) * 52845 + 22719);
        }
        return cipher;
    }

    private static byte[] Type1Program(byte[] subr, byte[] glyph)
    {
        byte[] s = Encrypt([0, 0, 0, 0, .. subr], 4330);
        byte[] g = Encrypt([0, 0, 0, 0, .. glyph], 4330);
        var priv = new MemoryStream();
        void Write(string text) => priv.Write(Encoding.Latin1.GetBytes(text));
        Write("dup /Private 8 dict dup begin /lenIV 4 def /Subrs 1 array\n");
        Write($"dup 0 {s.Length} RD ");
        priv.Write(s);
        Write(" NP\n2 index /CharStrings 1 dict dup begin\n");
        Write($"/A {g.Length} RD ");
        priv.Write(g);
        Write(" ND\nend\nend\n");
        byte[] eexec = Encrypt([0, 0, 0, 0, .. priv.ToArray()], 55665);
        return Encoding.Latin1.GetBytes("%!PS-AdobeFont-1.0: X\n/FontName /X def\n/Encoding StandardEncoding def\ncurrentfile eexec\n"
                                        + Convert.ToHexString(eexec) + "\n");
    }

    private static BitPdfFont FontWithProgram(byte[] program)
    {
        var descriptor = Dict("/Type /FontDescriptor /FontName /X /Flags 32");
        descriptor.Set("FontFile", new BitPdfStream(program, dict: Dict($"/Length {program.Length}")));
        var font = Dict("/Type /Font /Subtype /Type1 /BaseFont /X");
        font.Set("FontDescriptor", descriptor);
        return BitPdfFont.Create(font, NoRefs.Instance);
    }

    [TestMethod]
    public void AChainOfStreamsWhoseLengthsReferToTheNextShouldNotOverflowTheStack()
    {
        const int chain = 5_000;
        var document = Document("", "", file =>
        {
            for (int i = 0; i < chain; i++)
            {
                string length = i + 1 < chain ? $"{11 + i} 0 R" : "2";
                file.Obj(10 + i, $"<< /Length {length} >>\nstream\nab\nendstream");
            }
        }, [.. Enumerable.Range(10, chain)]);

        var first = document.XRef.Fetch(new BitPdfRef(10, 0));

        Assert.IsInstanceOfType<BitPdfBaseStream>(first);
    }

    [TestMethod]
    public void AType1SubroutineCallingItselfShouldNotOverflowTheStack()
    {
        var font = FontWithProgram(Type1Program(subr: [139, 10], glyph: [139, 10, 14]));

        Assert.IsNotNull(font);
    }

    [TestMethod]
    public void APfbSegmentWithANegativeLengthShouldNotLoopForever()
    {
        var font = FontWithProgram([0x80, 0x01, 0xFA, 0xFF, 0xFF, 0xFF, 0x00, 0x00]);

        Assert.IsFalse(font.HasEmbedded);
    }

    [TestMethod]
    public void AFormGraphThatMultipliesAtEveryLevelShouldStopAtTheRenderBudget()
    {
        const int levels = 12;
        string calls = string.Concat(Enumerable.Repeat("/F Do ", 10));
        var document = Document("", "/Resources << /XObject << /F 10 0 R >> >> /Contents 4 0 R", file =>
        {
            file.Stream(4, "/F Do");
            for (int i = 0; i < levels; i++)
            {
                string resources = i + 1 < levels ? $"/Resources << /XObject << /F {11 + i} 0 R >> >>" : "";
                file.Stream(10 + i, i + 1 < levels ? calls : "0 0 1 1 re f", $"/Type /XObject /Subtype /Form /BBox [0 0 1 1] {resources}");
            }
        }, [4, .. Enumerable.Range(10, levels)]);

        var watch = Stopwatch.StartNew();
        string html = new BitPdfHtmlRenderer(document.Pages[0], document.XRef).Render();

        Assert.IsTrue(watch.Elapsed < TimeSpan.FromSeconds(15), $"rendering took {watch.Elapsed}");
        StringAssert.Contains(html, "inset:0;background:");
    }

    [TestMethod]
    public void APageLabelTreeThatPointsBackAtItselfShouldBeWalkedOnce()
    {
        var document = Document("/PageLabels 10 0 R", "", file =>
        {
            file.Obj(10, "<< /Kids [10 0 R 10 0 R 11 0 R] >>");
            file.Obj(11, "<< /Nums [0 << /S /D /St 5 >>] >>");
        }, [10, 11]);

        Assert.AreEqual("5", document.PageLabels[0]);
    }

    [TestMethod]
    public void AnAlphabeticPageLabelOfAHugeNumberShouldStayShort()
    {
        var document = Document("/PageLabels << /Nums [0 << /S /A /St 2000000000 >>] >>", "", _ => { }, []);

        Assert.AreEqual("2000000000", document.PageLabels[0]);
    }

    [TestMethod]
    public void ANamedDestinationTreeThatPointsBackAtItselfShouldBeSearchedOnce()
    {
        var document = Document("/Names << /Dests 10 0 R >> /Outlines 20 0 R", "", file =>
        {
            file.Obj(10, "<< /Kids [10 0 R 10 0 R] >>");
            file.Obj(20, "<< /Type /Outlines /First 21 0 R /Last 21 0 R /Count 1 >>");
            file.Obj(21, "<< /Title (Item) /Parent 20 0 R /Dest (missing) >>");
        }, [10, 20, 21]);

        Assert.AreEqual(1, document.Outline.Count);
    }

    [TestMethod]
    public void APostScriptFunctionPushingPastTheStackLimitShouldStayLinear()
    {
        string program = "{ " + string.Concat(Enumerable.Repeat("1 0 index ", 40_000)) + "}";
        var stream = new BitPdfStream(Encoding.Latin1.GetBytes(program),
            dict: Dict($"/FunctionType 4 /Domain [0 1] /Range [0 1] /Length {program.Length}"));
        var function = BitPdfFunction.Create(stream, NoRefs.Instance);
        Assert.IsNotNull(function);

        var watch = Stopwatch.StartNew();
        double[] output = function.Eval([0]);

        Assert.IsTrue(watch.ElapsedMilliseconds < 500, $"evaluating took {watch.ElapsedMilliseconds}ms");
        Assert.AreEqual(1, output.Length);
    }

    [TestMethod]
    public void ASampledFunctionWithTooManyInputsShouldBeRefused()
    {
        string domain = string.Join(' ', Enumerable.Repeat("0 1", 20));
        string size = string.Join(' ', Enumerable.Repeat("1", 20));
        var stream = new BitPdfStream([0],
            dict: Dict($"/FunctionType 0 /Domain [{domain}] /Range [0 1] /Size [{size}] /BitsPerSample 8 /Length 1"));

        Assert.IsNull(BitPdfFunction.Create(stream, NoRefs.Instance));
    }

    [TestMethod]
    public void ACMapShouldResolveCodesThroughDisjointAndOverlappingRanges()
    {
        var disjoint = BitPdfCMap.Parse(Encoding.Latin1.GetBytes(
            "1 begincodespacerange <0000> <FFFF> endcodespacerange\n" +
            "3 begincidrange <0200> <02FF> 300 <0000> <00FF> 100 <0100> <01FF> 200 endcidrange"));

        Assert.AreEqual(105, disjoint.Lookup(0x0005));
        Assert.AreEqual(210, disjoint.Lookup(0x010A));
        Assert.AreEqual(300, disjoint.Lookup(0x0200));
        Assert.AreEqual(0x0500, disjoint.Lookup(0x0500));

        var overlapping = BitPdfCMap.Parse(Encoding.Latin1.GetBytes(
            "1 begincodespacerange <0000> <FFFF> endcodespacerange\n" +
            "3 begincidrange <0000> <00FF> 100 <0010> <0020> 900 <0080> <01FF> 2000 endcidrange"));

        Assert.AreEqual(116, overlapping.Lookup(0x0010));
        Assert.AreEqual(228, overlapping.Lookup(0x0080));
        Assert.AreEqual(2128, overlapping.Lookup(0x0100));
        Assert.AreEqual(0x0300, overlapping.Lookup(0x0300));
        for (int code = 0; code < 0x0200; code++)
        {
            Assert.AreEqual(code < 0x0100 ? 100 + code : 2000 + code - 0x0080, overlapping.Lookup(code));
        }

        var topmost = BitPdfCMap.Parse(Encoding.Latin1.GetBytes(
            "1 begincidrange <7FFFFFFFFFFFFFF0> <7FFFFFFFFFFFFFFF> 5 endcidrange"));

        Assert.AreEqual(6, topmost.Lookup(0x7FFFFFFFFFFFFFF1));
        Assert.AreEqual(19, topmost.Lookup(0x7FFFFFFFFFFFFFFE));
    }

    [TestMethod]
    public void AToUnicodeRangeShouldReuseItsDestinationForTheFirstCode()
    {
        var map = BitPdfToUnicodeCMap.Parse(Encoding.Latin1.GetBytes(
            "1 beginbfrange <0001> <0003> <0041> endbfrange"));

        Assert.AreEqual("A", map.Lookup(1));
        Assert.AreEqual("C", map.Lookup(3));
        Assert.AreSame(map.Lookup(1), map.Lookup(1));
    }

    [TestMethod]
    public void AToUnicodeMapShouldResolveCodesThroughOverlappingRanges()
    {
        var map = BitPdfToUnicodeCMap.Parse(Encoding.Latin1.GetBytes(
            "2 beginbfchar <0011> <0058> <0300> <0059> endbfchar\n" +
            "3 beginbfrange <0010> <0020> <0041> <0018> <0030> <0061> <0200> <0100> <0030> endbfrange"));

        Assert.AreEqual("A", map.Lookup(0x10));
        Assert.AreEqual("X", map.Lookup(0x11)); // a bfchar wins over a range
        Assert.AreEqual("Q", map.Lookup(0x20)); // the earlier range wins where two overlap
        Assert.AreEqual("j", map.Lookup(0x21));
        Assert.AreEqual("y", map.Lookup(0x30));
        Assert.IsNull(map.Lookup(0x31));
        Assert.IsNull(map.Lookup(0x200)); // a range whose low end is past its high end maps nothing
        Assert.AreEqual("Y", map.Lookup(0x300));
    }
}
