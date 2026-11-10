using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RangeAccess = Bit.BlazorUI.Tests.Components.Extras.PdfViewer.BitPdfRangeSourceTests.RangeAccess;
using RangeHandler = Bit.BlazorUI.Tests.Components.Extras.PdfViewer.BitPdfRangeSourceTests.RangeHandler;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

public partial class BitPdfRangeEngineTests
{
    [TestMethod]
    public void BufferedObjectStreamContentFailureShouldDecodeOnlyOnceAcrossReferences()
    {
        var file = new BitPdfEngineTestFile();
        file.Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        file.Obj(2, "<< /Type /Pages /Count 0 /Kids [] >>");
        file.Stream(3, new byte[] { 255, 255, 255 }, "/Type /ObjStm /N 2 /First 8 /Filter /FlateDecode");
        file.XRefStream(4, "/Root 1 0 R", new[]
        {
            (0, 0, 0, 65535), (1, 1, file.Offsets[1], 0), (2, 1, file.Offsets[2], 0),
            (3, 1, file.Offsets[3], 0), (6, 2, 3, 0), (7, 2, 3, 1),
        });
        var document = BitPdfDocument.Load(file.ToArray());
        var raw = (BitPdfBaseStream)document.XRef.Fetch(new BitPdfRef(3, 0))!;
        var decoder = new FailingFilterXRef();
        raw.Dict = new BitPdfDict(decoder);
        raw.Dict.Set("First", 8d);
        raw.Dict.Set("N", 2d);
        raw.Dict.Set("Filter", new BitPdfRef(999, 0));

        Assert.Throws<BitPdfFormatException>(() => document.XRef.Fetch(new BitPdfRef(6, 0)));
        Assert.IsNull(document.XRef.Fetch(new BitPdfRef(7, 0)));
        Assert.IsNull(document.XRef.Fetch(new BitPdfRef(6, 0)));
        Assert.AreEqual(1, decoder.Attempts, "Content failures must not repeatedly inflate the same object stream.");
    }

    [TestMethod]
    public async Task DetailMissesShouldKeepCompleteXrefObjectsWithoutPublishingPartialResults()
    {
        var file = new BitPdfEngineTestFile();
        file.Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        file.Obj(2, "<< /Type /Pages /Count 0 /Kids [] >>");
        for (int i = 10; i < 90; i++)
        {
            file.Raw(new string(' ', 4096));
            file.Obj(i, i == 89 ? "<< /Value 1 >>" : $"<< /Next {i + 1} 0 R >>");
        }
        file.XRefTable("/Size 90 /Root 1 0 R", file.Offsets.Keys.Append(0));
        using var handler = new RangeHandler(file.ToArray());
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 16 * 1024 * 1024, 4096);
        var document = await source.Load();
        var firstObjects = new HashSet<object>(ReferenceEqualityComparer.Instance);
        int attempts = 0;
        Func<BitPdfDocument, int> build = document =>
        {
            attempts++;
            var node = (BitPdfDict)document.XRef.Fetch(new BitPdfRef(10, 0))!;
            firstObjects.Add(node);
            while (node.Get("Next") is BitPdfDict next) node = next;
            return (int)(double)node.Get("Value")!;
        };
        int result = await source.Run(document, build);

        Assert.AreEqual(1, result);
        Assert.IsTrue(attempts > 10);
        Assert.AreEqual(1, firstObjects.Count, "A complete dictionary must survive later missing-data restarts.");
    }

    private sealed class FailingFilterXRef : IBitPdfXRef
    {
        internal int Attempts;

        public object? Fetch(BitPdfRef reference, bool suppressEncryption = false)
        {
            Attempts++;
            // Fail at the decoder boundary without allocating a decompression bomb.
            throw new BitPdfFormatException("Decoded stream too large.");
        }

        public object? FetchIfRef(object? value, bool suppressEncryption = false)
            => value is BitPdfRef reference ? Fetch(reference, suppressEncryption) : value;
    }
}
