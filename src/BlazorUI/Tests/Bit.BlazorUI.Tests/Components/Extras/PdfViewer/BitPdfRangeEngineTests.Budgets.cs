using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

public partial class BitPdfRangeEngineTests
{
    [TestMethod]
    [DataRow("FlateDecode")]
    [DataRow("RunLengthDecode")]
    [DataRow("ASCII85Decode")]
    public void HintDecoderMustEnforceItsLimitDuringExpansion(string filter)
    {
        byte[] encoded;
        int expected;
        if (filter == "FlateDecode")
        {
            using var output = new MemoryStream();
            using (var compressor = new ZLibStream(output, CompressionLevel.Fastest, leaveOpen: true))
                compressor.Write(new byte[4 * 1024 * 1024]);
            encoded = output.ToArray(); expected = 4 * 1024 * 1024;
        }
        else if (filter == "RunLengthDecode")
        { encoded = Enumerable.Range(0, 256).SelectMany(_ => new byte[] { 129, 42 }).Append((byte)128).ToArray(); expected = 32768; }
        else { encoded = Enumerable.Repeat((byte)'z', 1024).ToArray(); expected = 4096; }
        var dictionary = new BitPdfDict(); dictionary.Set("Filter", BitPdfName.Get(filter));
        var stream = new BitPdfStream(encoded) { Dict = dictionary };
        var decode = typeof(BitPdfStreamDecoder).GetMethods(BudgetFlags).Single(m => m.Name == "Decode" && m.GetParameters()[1].ParameterType == typeof(int));
        long before = GC.GetAllocatedBytesForCurrentThread();
        var error = Assert.ThrowsExactly<TargetInvocationException>(() => decode.Invoke(null, [stream, 1024, false]));
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.IsInstanceOfType<BitPdfFormatException>(error.InnerException);
        Assert.IsTrue(allocated < 1024 * 1024, $"Small hint budget allocated {allocated} bytes.");
        Assert.AreEqual(expected, BitPdfStreamDecoder.Decode(stream).Length, "The public decoder keeps its existing limit.");
    }

    [TestMethod]
    public void ReparsedCidWidthArraysMustShareAStableFontBudgetAcrossAttempts()
    {
        var budget = typeof(BitPdfDocument).Assembly.GetType("Bit.BlazorUI.BitPdfBudget", true)!;
        using var scope = (IDisposable)budget.GetMethod("BeginRetryScope", BudgetFlags)!.Invoke(null, null)!;
        var xref = new ReparsedFontXRef();
        BitPdfDict Font()
        {
            var font = new BitPdfDict(xref);
            font.Set("Subtype", BitPdfName.Get("Type0"));
            font.Set("DescendantFonts", new List<object?> { new BitPdfRef(1, 0) });
            return font;
        }
        Assert.ThrowsExactly<InvalidOperationException>(() => BitPdfFont.Create(Font(), xref));
        var parsed = BitPdfFont.Create(Font(), xref);
        Assert.AreEqual(1000d, parsed.Decode([0, 0])[0].Width1000);
        Assert.AreEqual(1, xref.Misses, "A new /W list must not reset the million-entry expansion budget.");
    }

    private sealed class ReparsedFontXRef : IBitPdfXRef
    {
        internal int Misses;
        public object? Fetch(BitPdfRef reference, bool suppressEncryption = false)
        {
            if (reference.Num == 2) { Misses++; throw new InvalidOperationException("Missing width operand."); }
            var widths = new List<object?>();
            for (int i = 0; i < 16; i++) widths.AddRange(new object?[] { 0d, 65535d, 100d });
            widths.Add(new BitPdfRef(2, 0));
            var dictionary = new BitPdfDict(this); dictionary.Set("W", widths);
            return dictionary;
        }
        public object? FetchIfRef(object? value, bool suppressEncryption = false) => value is BitPdfRef reference ? Fetch(reference) : value;
    }

    private const BindingFlags BudgetFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
}
