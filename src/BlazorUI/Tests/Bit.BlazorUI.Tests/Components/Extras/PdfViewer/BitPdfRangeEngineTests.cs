using System;
using System.Linq;
using System.Net.Http;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RangeAccess = Bit.BlazorUI.Tests.Components.Extras.PdfViewer.BitPdfRangeSourceTests.RangeAccess;
using RangeHandler = Bit.BlazorUI.Tests.Components.Extras.PdfViewer.BitPdfRangeSourceTests.RangeHandler;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

[TestClass]
public class BitPdfRangeEngineTests
{
    [TestMethod]
    [DataRow("hello")]
    [DataRow("encrypted")]
    [DataRow("attachments")]
    [DataRow("layers")]
    [DataRow("rich")]
    [DataRow("resources")]
    [DataRow("prefixed")]
    [DataRow("xref-stream")]
    [DataRow("incremental")]
    [DataRow("article.pdf")]
    public async Task PartialDocumentShouldMatchBufferedPagesDestinationsAndBothRenderers(string fixture)
    {
        byte[] bytes = BitPdfGenericSourceTests.Fixture(fixture);
        string? password = fixture == "encrypted" ? "hunter2" : null;
        var expected = BitPdfDocument.Load(bytes, password);
        using var handler = new RangeHandler(bytes);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 16 * 1024 * 1024, 65536);
        var actual = await source.Load(password);
        Assert.AreEqual(expected.PageCount, actual.PageCount);
        Assert.AreEqual(expected.PageCount, actual.Pages.Count);
        for (int i = 0; i < actual.PageCount; i++)
        {
            int page = i;
            Assert.AreEqual(expected.Pages[i].ExtractText(), await source.Run(actual, d => d.Pages[page].ExtractText()));
            Assert.AreEqual(new BitPdfHtmlRenderer(expected.Pages[i], expected.XRef).Render(), await source.Run(actual, d => new BitPdfHtmlRenderer(d.Pages[page], d.XRef).Render()));
            Assert.AreEqual(JsonSerializer.Serialize(new BitPdfCanvasRenderer(expected.Pages[i], expected.XRef).Render()),
                await source.Run(actual, d => JsonSerializer.Serialize(new BitPdfCanvasRenderer(d.Pages[page], d.XRef).Render())));
        }
        Assert.AreEqual(expected.Metadata.Title, await source.Run(actual, d => d.Metadata.Title));
        Assert.AreEqual(expected.Outline.Count, await source.Run(actual, d => d.Outline.Count));
        CollectionAssert.AreEqual(expected.PageLabels.ToArray(), await source.Run(actual, d => d.PageLabels.ToArray()));
        Assert.AreEqual(expected.Attachments.Count, await source.Run(actual, d => d.Attachments.Count));
        Assert.AreEqual(expected.FormFields.Count, await source.Run(actual, d => d.FormFields.Count));
        Assert.IsTrue(source.Int("PeakBytes") <= 16 * 1024 * 1024);
    }

    [TestMethod]
    public async Task MissingDeclaredStreamShouldAcquireItsWholeBodyBeforeRestarting()
    {
        using var handler = new RangeHandler(MiddleStream(false));
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 1024 * 1024, 65536);
        var document = await source.Load();
        StringAssert.Contains(await source.Run(document, d => d.Pages[0].ExtractText()), "Middle stream");
        Assert.IsTrue(handler.Requests.Contains("bytes=65536-196607"), string.Join(",", handler.Requests));
    }

    [TestMethod]
    public async Task MissingObjectStreamShouldRollBackAndPublishOnlyCompleteObjects()
    {
        using var handler = new RangeHandler(MiddleStream(true));
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 1024 * 1024, 65536);
        var document = await source.Load();
        Assert.AreEqual("Middle title", await source.Run(document, d => d.Metadata.Title));
        int requests = handler.Requests.Count;
        Assert.AreEqual("Middle title", await source.Run(document, d => d.Metadata.Title));
        Assert.AreEqual(requests, handler.Requests.Count);
    }

    [TestMethod]
    public async Task EqualChildAndPageCountsMustStillRespectMixedSubtrees()
    {
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R /Outlines 8 0 R >>",
            "<< /Type /Pages /Count 3 /Kids [3 0 R 6 0 R 7 0 R] >>",
            "<< /Type /Pages /Parent 2 0 R /Count 2 /Kids [4 0 R 5 0 R] >>",
            "<< /Type /Page /Parent 3 0 R /MediaBox [0 0 100 792] >>",
            "<< /Type /Page /Parent 3 0 R /MediaBox [0 0 200 792] >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 792] >>",
            "<< /Type /Pages /Parent 2 0 R /Count 0 /Kids [] >>",
            "<< /Type /Outlines /First 9 0 R >>",
            "<< /Title (Third) /Dest [6 0 R /Fit] >>",
        };
        var pdf = new StringBuilder("%PDF-1.7\n");
        var offsets = new List<int>();
        for (int i = 0; i < objects.Length; i++)
        { offsets.Add(pdf.Length); pdf.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n"); }
        int xref = pdf.Length;
        pdf.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (int offset in offsets) pdf.Append($"{offset:D10} 00000 n \n");
        pdf.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        using var handler = new RangeHandler(Encoding.ASCII.GetBytes(pdf.ToString()));
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 1024 * 1024, 65536);
        var document = await source.Load();
        Assert.AreEqual(3, document.PageCount);
        Assert.AreEqual(200d, await source.Run(document, d => d.Pages[1].Width));
        Assert.AreEqual(300d, await source.Run(document, d => d.Pages[2].Width));
        Assert.AreEqual(3, await source.Run(document, d => d.Outline[0].PageNumber));
    }

    [TestMethod]
    public async Task NamedInfoTitleShouldMatchBufferedMetadataWithoutBuildingXmp()
    {
        byte[] bytes = BitPdfRangeViewerTests.SpacedPages(namedTitle: true);
        using var handler = new RangeHandler(bytes);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 1024 * 1024, 65536);
        var document = await source.Load();
        var title = await source.Run(document, d => (string?)typeof(BitPdfDocument)
            .GetMethod("ReadPartialTitle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(d, null));
        Assert.AreEqual(BitPdfDocument.Load(bytes).Metadata.Title, title);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ValidLinearizationShouldSelectTheFirstXrefButStaleLengthMustUseTheTail(bool staleLength)
    {
        // Two blank pages, generated and checked with qpdf through pikepdf.
        byte[] bytes = Convert.FromBase64String(
            "JVBERi0xLjMKJb/3ov4KNCAwIG9iago8PCAvTGluZWFyaXplZCAxIC9MIDEyNjggL0ggWyA1MjkgMTE2IF0gL08gNyAvRSA4MjEgL04gMiAvVCAxMDcwID4+CmVuZG9iagogICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAKeHJlZgo0IDUKMDAwMDAwMDAxNSAwMDAwMCBuIAowMDAwMDAwNDgwIDAwMDAwIG4gCjAwMDAwMDA1MjkgMDAwMDAgbiAKMDAwMDAwMDY0NSAwMDAwMCBuIAowMDAwMDAwNzUxIDAwMDAwIG4gCnRyYWlsZXIgPDwgL1Jvb3QgNSAwIFIgL1NpemUgOSAvUHJldiAxMDYyICAgICAgICAgICAgICAgICAgL0lEIFs8MGM3ZTc1OGRjZTljYTE2ZGJhMWY5ZTZjZTFhNDkxOGY+PDBjN2U3NThkY2U5Y2ExNmRiYTFmOWU2Y2UxYTQ5MThmPl0gPj4Kc3RhcnR4cmVmCjAKJSVFT0YKNSAwIG9iago8PCAvUGFnZXMgMyAwIFIgL1R5cGUgL0NhdGFsb2cgPj4KZW5kb2JqCjYgMCBvYmoKPDwgL0ZpbHRlciAvRmxhdGVEZWNvZGUgL1MgMzYgL0xlbmd0aCAzOSA+PgpzdHJlYW0KeJxjYGBgAiJBBhDYwIAAEDZQjoEFSZQJihkY3BjYJjAwAABFtQJcCmVuZHN0cmVhbQplbmRvYmoKNyAwIG9iago8PCAvQ29udGVudHMgOCAwIFIgL01lZGlhQm94IFsgMCAwIDIwMCAzMDAgXSAvUGFyZW50IDMgMCBSIC9SZXNvdXJjZXMgPDwgPj4gL1R5cGUgL1BhZ2UgPj4KZW5kb2JqCjggMCBvYmoKPDwgL0xlbmd0aCAwIC9GaWx0ZXIgL0ZsYXRlRGVjb2RlID4+CnN0cmVhbQoKZW5kc3RyZWFtCmVuZG9iagoxIDAgb2JqCjw8IC9Db250ZW50cyAyIDAgUiAvTWVkaWFCb3ggWyAwIDAgNDAwIDUwMCBdIC9QYXJlbnQgMyAwIFIgL1Jlc291cmNlcyA8PCA+PiAvVHlwZSAvUGFnZSA+PgplbmRvYmoKMiAwIG9iago8PCAvTGVuZ3RoIDAgL0ZpbHRlciAvRmxhdGVEZWNvZGUgPj4Kc3RyZWFtCgplbmRzdHJlYW0KZW5kb2JqCjMgMCBvYmoKPDwgL0NvdW50IDIgL0tpZHMgWyA3IDAgUiAxIDAgUiBdIC9UeXBlIC9QYWdlcyA+PgplbmRvYmoKeHJlZgowIDQKMDAwMDAwMDAwMCA2NTUzNSBmIAowMDAwMDAwODIxIDAwMDAwIG4gCjAwMDAwMDA5MjcgMDAwMDAgbiAKMDAwMDAwMDk5NyAwMDAwMCBuIAp0cmFpbGVyIDw8IC9TaXplIDQgL0lEIFs8MGM3ZTc1OGRjZTljYTE2ZGJhMWY5ZTZjZTFhNDkxOGY+PDBjN2U3NThkY2U5Y2ExNmRiYTFmOWU2Y2UxYTQ5MThmPl0gPj4Kc3RhcnR4cmVmCjIxNgolJUVPRgo=");
        string text = Encoding.ASCII.GetString(bytes);
        int lengthAt = text.IndexOf("/L ", StringComparison.Ordinal) + 3;
        if (staleLength)
            Encoding.ASCII.GetBytes((bytes.Length + 1).ToString()).CopyTo(bytes, lengthAt);
        using var handler = new RangeHandler(bytes);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 1024 * 1024, 65536);
        var document = await source.Load();
        var offset = (int?)typeof(BitPdfXRef).GetMethod("ReadLinearizedStart",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(document.XRef, null);
        if (staleLength) Assert.IsNull(offset);
        else Assert.AreEqual(text.IndexOf("xref\n", StringComparison.Ordinal), offset);
        Assert.AreEqual(2, document.PageCount);
        Assert.AreEqual(400d, document.Pages[1].Width);
    }

    private static byte[] MiddleStream(bool compressedObject)
    {
        using var output = new MemoryStream();
        var offsets = new Dictionary<int, int>();
        void Text(string value) => output.Write(Encoding.ASCII.GetBytes(value));
        void Obj(int number, string value) { offsets[number] = (int)output.Position; Text($"{number} 0 obj\n{value}\nendobj\n"); }
        void Stream(int number, string value, string attributes = "") => Obj(number, $"<< /Length {value.Length} {attributes} >>\nstream\n{value}\nendstream");
        Text("%PDF-1.7\n");
        Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        Obj(2, "<< /Type /Pages /Count 1 /Kids [3 0 R] >>");
        Obj(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 7 0 R >> >> /Contents 4 0 R >>");
        Obj(7, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        string content = "BT /F1 18 Tf 50 700 Td (Middle stream) Tj ET\n";
        Stream(4, compressedObject ? content : content + "%" + new string('a', 140000) + "\n");
        if (compressedObject)
        {
            Stream(5, new string('a', 80000));
            Stream(10, "11 0 << /Title (Middle title) /Padding (" + new string('b', 80000) + ") >>", "/Type /ObjStm /N 1 /First 5");
        }
        Stream(6, new string('c', 300000));
        int xref = (int)output.Position;
        if (compressedObject)
        {
            offsets[20] = xref;
            using var data = new MemoryStream();
            for (int i = 0; i <= 20; i++)
            {
                int type = i == 11 ? 2 : offsets.ContainsKey(i) ? 1 : 0;
                int field = i == 11 ? 10 : offsets.GetValueOrDefault(i);
                data.WriteByte((byte)type);
                data.WriteByte((byte)(field >> 24)); data.WriteByte((byte)(field >> 16));
                data.WriteByte((byte)(field >> 8)); data.WriteByte((byte)field);
                data.WriteByte(0); data.WriteByte(i == 0 ? (byte)255 : (byte)0);
            }
            Text($"20 0 obj\n<< /Type /XRef /Size 21 /Root 1 0 R /Info 11 0 R /W [1 4 2] /Length {data.Length} >>\nstream\n");
            output.Write(data.ToArray()); Text("\nendstream\nendobj\n");
        }
        else
        {
            Text("xref\n0 8\n0000000000 65535 f \n");
            for (int i = 1; i <= 7; i++) Text(offsets.TryGetValue(i, out int at) ? $"{at:D10} 00000 n \n" : "0000000000 00000 f \n");
            Text("trailer\n<< /Size 8 /Root 1 0 R >>\n");
        }
        Text($"startxref\n{xref}\n%%EOF\n");
        return output.ToArray();
    }

    [TestMethod]
    public async Task DamagedXrefMustRequestBufferedRecoveryWithoutScanningThroughRanges()
    {
        byte[] bytes = BitPdfGenericSourceTests.Fixture("damaged");
        using var handler = new RangeHandler(bytes);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 16777216, 65536);
        var error = await Assert.ThrowsAsync<System.Reflection.TargetInvocationException>(() => source.Load());
        Assert.IsInstanceOfType<System.IO.IOException>(error.InnerException);
        StringAssert.Contains(error.InnerException!.Message, "complete-file recovery");
        Assert.AreEqual(1, handler.Requests.Count);
    }
}
