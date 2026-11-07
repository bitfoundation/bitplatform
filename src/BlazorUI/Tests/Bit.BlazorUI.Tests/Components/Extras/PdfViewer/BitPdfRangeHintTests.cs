using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using RangeAccess = Bit.BlazorUI.Tests.Components.Extras.PdfViewer.BitPdfRangeSourceTests.RangeAccess;
using RangeHandler = Bit.BlazorUI.Tests.Components.Extras.PdfViewer.BitPdfRangeSourceTests.RangeHandler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

[TestClass]
public class BitPdfRangeHintTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Type Hints = typeof(BitPdfDocument).Assembly.GetType("Bit.BlazorUI.BitPdfLinearizationHints", true)!;

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void PageAndSharedHintsShouldValidateBitWidthsAndFileOffsets(bool overflow)
    {
        byte[] data = Table();
        var streams = overflow ? new[] { (500, 100), (2050, 80) } : new[] { (500, 100) };
        var header = Activator.CreateInstance(Hints.GetNestedType("Header", Flags)!, [2, 0, 5, 2000, streams])!;
        var hints = Hints.GetMethod("Read", Flags)!.Invoke(null, [data, 38, header, 10000, (Func<int, int, bool>)((_, _) => true)])!;
        var ranges = (List<(int Offset, int Length)>)Hints.GetMethod("Ranges", Flags)!.Invoke(hints, [1, 1000])!;
        CollectionAssert.AreEqual(overflow ? new[] { (2000, 180), (3080, 50) } : new[] { (2000, 100), (3000, 50) }, ranges.ToArray());
        Assert.AreEqual(0, ((List<(int, int)>)Hints.GetMethod("Ranges", Flags)!.Invoke(hints, [1, 100])!).Count);
    }

    [TestMethod]
    [DataRow("width")]
    [DataRow("truncated")]
    [DataRow("offset")]
    [DataRow("xref")]
    [DataRow("count")]
    public void InvalidHintsMustNotProduceAcquisitionRanges(string problem)
    {
        byte[] data = Table();
        if (problem == "width") data[9] = 33;
        if (problem == "truncated") data = data[..^1];
        var header = Activator.CreateInstance(Hints.GetNestedType("Header", Flags)!,
            [problem == "count" ? 100001 : 2, 0, 5, 2000, new[] { (500, 100) }])!;
        var error = Assert.ThrowsExactly<TargetInvocationException>(() => Hints.GetMethod("Read", Flags)!.Invoke(null,
            [data, 38, header, problem == "offset" ? 2050 : 10000, (Func<int, int, bool>)((_, _) => problem != "xref")]));
        Assert.IsInstanceOfType<BitPdfFormatException>(error.InnerException);
    }

    [TestMethod]
    public void RetryBudgetsShouldSurviveAttemptsButRemainIndependentForEachIdentityAndOperation()
    {
        var type = typeof(BitPdfDocument).Assembly.GetType("Bit.BlazorUI.BitPdfBudget", true)!;
        object identity = new();
        object Budget(object owner) => type.GetMethod("ForRetry", Flags)!.Invoke(null, [10L, owner, "test"])!;
        bool Spend(object budget, long amount) => (bool)type.GetMethod("TrySpend", Flags)!.Invoke(budget, [amount])!;
        using ((IDisposable)type.GetMethod("BeginRetryScope", Flags)!.Invoke(null, null)!)
        {
            Assert.IsTrue(Spend(Budget(identity), 7));
            using ((IDisposable)type.GetMethod("BeginRetryScope", Flags)!.Invoke(null, null)!)
                Assert.IsFalse(Spend(Budget(identity), 4));
            Assert.IsTrue(Spend(Budget(new object()), 10));
            Assert.IsTrue(Spend(Budget(identity), 3));
        }
        Assert.IsTrue(Spend(Budget(identity), 10));
    }

    [TestMethod]
    public void DeferredAttachmentsShouldShareTheAggregateBudgetAndDeduplicateBeforeSpending()
    {
        var assembly = typeof(BitPdfDocument).Assembly;
        var budget = Activator.CreateInstance(assembly.GetType("Bit.BlazorUI.BitPdfBudget", true)!, [10L])!;
        var method = assembly.GetType("Bit.BlazorUI.BitPdfEmbeddedFiles", true)!.GetMethod("ReadFile", Flags)!;
        var document = BitPdfDocument.Load(BitPdfGenericSourceTests.Fixture("hello"));
        var completed = new HashSet<object>();
        BitPdfDict Spec()
        {
            var stream = new BitPdfStream(new byte[] { 1, 2, 3, 4 }) { Dict = new() };
            var ef = new BitPdfDict(); ef.Set("F", stream);
            var spec = new BitPdfDict(); spec.Set("EF", ef);
            return spec;
        }
        var first = Spec();
        var result = method.Invoke(null, [document.XRef, first, null, "first", budget, completed])!;
        var resultType = result.GetType();
        completed.Add(resultType.GetProperty("Identity", Flags)!.GetValue(result)!);
        Assert.IsNull(method.Invoke(null, [document.XRef, first, null, "duplicate", budget, completed]));
        var second = method.Invoke(null, [document.XRef, Spec(), null, "second", budget, completed])!;
        var attachment = (BitPdfAttachment)resultType.GetProperty("Attachment", Flags)!.GetValue(second)!;
        Assert.AreEqual(4, attachment.Content.Length);
        var third = method.Invoke(null, [document.XRef, Spec(), null, "third", budget, completed])!;
        Assert.AreEqual(0, ((BitPdfAttachment)resultType.GetProperty("Attachment", Flags)!.GetValue(third)!).Content.Length);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task PrimaryAndOverflowStreamsShouldBeReadAsOneHintTable(bool malformed)
    {
        using var handler = new RangeHandler(OverflowFixture(malformed));
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 1024 * 1024, 65536);
        var document = await source.Load();
        var ranges = await source.Run(document, d => (List<(int, int)>)typeof(BitPdfXRef).GetMethod("HintRanges", Flags)!
            .Invoke(d.XRef, [1, d.PageCount, 262144])!);
        Assert.AreEqual(!malformed, ranges.Count > 0);
        Assert.AreEqual(400d, await source.Run(document, d => d.Pages[1].Width));
        Assert.AreEqual(1, handler.Requests.Count);
    }

    private static byte[] OverflowFixture(bool malformed)
    {
        byte[] original = BitPdfRangeEngineTests.LinearizedTwoPages();
        var reference = BitPdfDocument.Load(original);
        byte[] hints = BitPdfStreamDecoder.Decode((BitPdfBaseStream)reference.XRef.Fetch(new(6, 0))!);
        if (malformed) hints[^1] = 255;
        string text = Encoding.Latin1.GetString(original);
        string Object(int number)
        {
            int start = text.IndexOf($"{number} 0 obj\n", StringComparison.Ordinal);
            int end = text.IndexOf("endobj", start, StringComparison.Ordinal) + 6;
            return text[start..end] + "\n";
        }
        var offsets = new Dictionary<int, int>();
        int firstHintLength = 0, overflowLength = 0, mainXref = 0, total = 0;
        byte[] Build()
        {
            using var output = new MemoryStream();
            void Text(string value) => output.Write(Encoding.Latin1.GetBytes(value));
            Text("%PDF-1.3\n% hints\n");
            offsets[4] = (int)output.Position;
            Text($"4 0 obj\n<< /Linearized 1 /L {total:D10} /H [{offsets.GetValueOrDefault(6):D10} {firstHintLength:D10} {offsets.GetValueOrDefault(9):D10} {overflowLength:D10}] /O 7 /E {offsets.GetValueOrDefault(1):D10} /N 2 /T {mainXref + 8:D10} >>\nendobj\n");
            int firstXref = (int)output.Position;
            Text("xref\n4 5\n");
            for (int i = 4; i <= 8; i++) Text($"{offsets.GetValueOrDefault(i):D10} 00000 n \n");
            Text($"trailer\n<< /Root 5 0 R /Size 10 /Prev {mainXref:D10} >>\nstartxref\n0\n%%EOF\n");
            void Obj(int number) { offsets[number] = (int)output.Position; Text(Object(number)); }
            void Stream(int number, int start, int length, string attributes)
            {
                offsets[number] = (int)output.Position;
                Text($"{number} 0 obj\n<< /Length {length} {attributes} >>\nstream\n");
                output.Write(hints.AsSpan(start, length));
                Text("\nendstream\nendobj\n");
            }
            Obj(5); Stream(6, 0, 30, "/S 36"); firstHintLength = (int)output.Position - offsets[6];
            Obj(7); Obj(8); Obj(1); Obj(2); Obj(3);
            Stream(9, 30, hints.Length - 30, ""); overflowLength = (int)output.Position - offsets[9];
            mainXref = (int)output.Position;
            Text("xref\n0 4\n0000000000 65535 f \n");
            for (int i = 1; i <= 3; i++) Text($"{offsets[i]:D10} 00000 n \n");
            Text($"9 1\n{offsets[9]:D10} 00000 n \ntrailer\n<< /Size 10 >>\nstartxref\n{firstXref:D10}\n%%EOF\n");
            total = (int)output.Position;
            return output.ToArray();
        }
        Build();
        uint firstOffset = (uint)(offsets[7] - firstHintLength);
        for (int i = 0; i < 4; i++) hints[4 + i] = (byte)(firstOffset >> (24 - i * 8));
        return Build();
    }

    private static byte[] Table()
    {
        using var output = new MemoryStream();
        int bit = 0;
        byte current = 0;
        void Write(uint number, int width)
        {
            for (int i = width - 1; i >= 0; i--)
            {
                current |= (byte)(((number >> i) & 1) << (7 - bit));
                if (++bit == 8) { output.WriteByte(current); current = 0; bit = 0; }
            }
        }
        void Align() { if (bit != 0) { output.WriteByte(current); current = 0; bit = 0; } }
        Write(1, 32); Write(1000, 32); Write(0, 16); Write(100, 32); Write(0, 16);
        Write(0, 32); Write(0, 16); Write(10, 32); Write(0, 16);
        Write(1, 16); Write(1, 16); Write(0, 16); Write(1, 16);
        Write(1, 1); Write(1, 1); Align(); // One shared reference per page.
        Write(0, 1); Write(1, 1); Align();
        Write(20, 32); Write(2900, 32); Write(1, 32); Write(2, 32);
        Write(0, 16); Write(50, 32); Write(0, 16);
        Write(0, 1); Write(0, 1); Align();
        return output.ToArray();
    }
}
