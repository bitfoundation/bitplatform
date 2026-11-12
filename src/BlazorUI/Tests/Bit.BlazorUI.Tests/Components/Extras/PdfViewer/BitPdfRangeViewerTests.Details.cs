using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RangeHandler = Bit.BlazorUI.Tests.Components.Extras.PdfViewer.BitPdfRangeSourceTests.RangeHandler;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

public partial class BitPdfRangeViewerTests
{
    private static BitPdfSource CreateRangeDefinition(int cacheSize = 16 * 1024 * 1024, int chunkSize = 4096)
        => BitPdfSource.FromUrl("https://pdf.test/document").WithRangeLoading(new()
        {
            CacheSize = cacheSize,
            ChunkSize = chunkSize,
            MaxFallbackSize = 0,
        });

    [TestMethod]
    public async Task OversizedAttachmentShouldNotHideTheFollowingFileOrRepeatTheScan()
    {
        byte[] bytes = Pdf("/Names << /EmbeddedFiles 10 0 R >>", "", file =>
        {
            file.Obj(10, "<< /Names [(large.bin) 11 0 R (small.txt) 12 0 R] >>");
            file.Obj(11, "<< /Type /Filespec /F (large.bin) /EF << /F 13 0 R >> >>");
            file.Obj(12, "<< /Type /Filespec /F (small.txt) /EF << /F 14 0 R >> >>");
            file.Raw(new string(' ', 8192));
            file.Stream(13, new byte[200000], "/Type /EmbeddedFile");
            file.Stream(14, "small attachment", "/Type /EmbeddedFile");
        });
        Assert.HasCount(2, BitPdfDocument.Load(bytes).Attachments);
        using var handler = new RangeHandler(bytes);
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, CreateRangeDefinition(128 * 1024)).Add(v => v.OnError, errors.Add));
        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount, string.Join("; ", errors)));
        component.WaitForPages(1);

        await component.InvokeAsync(() => component.Instance.ShowSidebar(BitPdfSidebar.Attachments));
        component.WaitForAssertion(() => Assert.ContainsSingle(component.Instance.Attachments));
        Assert.ContainsSingle(component.Instance.Attachments);
        Assert.AreEqual("small.txt", component.Instance.Attachments[0].Name);
        Assert.IsEmpty(errors, string.Join("; ", errors));
        int requests = handler.Requests.Count;
        await component.InvokeAsync(() => component.Instance.ShowSidebar(BitPdfSidebar.None));
        await component.InvokeAsync(() => component.Instance.ShowSidebar(BitPdfSidebar.Attachments));
        Assert.HasCount(requests, handler.Requests);
    }

    [TestMethod]
    public async Task PropertiesShouldLoadMetadataWithoutFormsStructureOrAttachments()
    {
        byte[] bytes = Pdf("/Metadata 10 0 R /AcroForm 11 0 R /StructTreeRoot 12 0 R /Names << /EmbeddedFiles 13 0 R >>", "", file =>
        {
            file.Raw(new string(' ', 8192));
            file.Stream(10, "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"><rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"/></x:xmpmeta>", "/Type /Metadata /Subtype /XML");
            file.Raw(new string(' ', 8192));
            file.Obj(11, "<< /Fields [14 0 R] >>");
            file.Obj(14, "<< /FT /Tx /T (Deferred field) /V (value) >>");
            file.Obj(12, "<< /Type /StructTreeRoot /K [15 0 R] >>");
            file.Obj(15, "<< /Type /StructElem /S /P /P 12 0 R /K 0 >>");
            file.Obj(13, "<< /Names [(deferred.txt) 16 0 R] >>");
            file.Obj(16, "<< /F (deferred.txt) /EF << /F 17 0 R >> >>");
            file.Stream(17, "deferred attachment");
        });
        using var handler = new RangeHandler(bytes);
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, CreateRangeDefinition()).Add(v => v.OnError, errors.Add));
        component.WaitForPages(1);

        await component.InvokeAsync(component.Instance.ToggleProperties);
        component.WaitForAssertion(() => Assert.IsNotNull(component.Instance.Metadata));

        Assert.IsNotNull(component.Instance.Metadata);
        Assert.IsEmpty(component.Instance.FormFields);
        Assert.IsEmpty(component.Instance.StructureTree);
        Assert.IsEmpty(component.Instance.Attachments);
        Assert.IsEmpty(errors, string.Join("; ", errors));

        await component.InvokeAsync(() => component.Instance.LoadDocumentDetailsAsync(TestContext.CancellationToken));
        Assert.ContainsSingle(component.Instance.FormFields);
        Assert.ContainsSingle(component.Instance.StructureTree);
        Assert.ContainsSingle(component.Instance.Attachments);
    }

    [TestMethod]
    public async Task StructuralPageFailureAfterPublicationShouldBePermanentAndReportedOnce()
    {
        var file = new BitPdfEngineTestFile();
        file.Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        file.Obj(2, "<< /Type /Pages /Count 2 /Kids [3 0 R 8 0 R] >>");
        file.Obj(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents 4 0 R >>");
        file.Stream(4, "");
        file.Obj(8, "<< /Type /Pages /Parent 2 0 R /Count 1 /Kids [] >>");
        file.XRefTable("/Size 9 /Root 1 0 R", file.Offsets.Keys.Append(0));
        using var handler = new RangeHandler(file.ToArray());
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, CreateRangeDefinition()).Add(v => v.OnError, errors.Add));
        component.WaitForPages(2);

        await component.InvokeAsync(() => component.Instance.GoToPage(2));
        Assert.Contains(2, component.Instance.FailedPages);
        Assert.ContainsSingle(errors);
        await component.InvokeAsync(() => component.Instance.GoToPage(2));
        Assert.ContainsSingle(errors);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task DependencyDiscoveryShouldIgnoreScalarsAndStopAtItsOptimizationBound(bool manyObjects)
    {
        string values = string.Concat(Enumerable.Repeat(manyObjects ? "<<>> " : "1 ", 66000));
        // Resource dictionaries may carry extension entries. These are valid
        // arrays the renderer does not need, while discovery still visits them.
        byte[] bytes = Pdf("", "", resources: "<< /Font << /F1 5 0 R >> /PrefetchStress [" + values + "] >>");
        using var handler = new RangeHandler(bytes);
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, CreateRangeDefinition(chunkSize: 65536)).Add(v => v.OnError, errors.Add));

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, component.Instance.PageCount, string.Join("; ", errors));
            Assert.AreEqual("false", component.Find(".bit-pdv-surface").GetAttribute("aria-busy"));
        }, TimeSpan.FromSeconds(20));
        Assert.IsNull(component.Instance.GetBytes(), "A valid large resource array must remain in Range mode.");
        Assert.IsEmpty(errors, string.Join("; ", errors));
        Assert.IsEmpty(component.Instance.FailedPages);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ConcurrentPageBuildsShouldKeepIndependentAttempts(bool runInline)
    {
        using var handler = new RangeHandler(BitPdfRangeViewerTests.SpacedPages());
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        var component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.BackgroundRendering, true).Add(v => v.OnError, errors.Add));
        // The seam is internal and instance-scoped; all document operations use
        // the public API. Both rows now execute distinct scheduler branches.
        typeof(BitPdfViewer).GetProperty("ForceInlineRangeWork",
            BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(component.Instance, runInline);
        component.Render(p => p.Add(v => v.Source, CreateRangeDefinition()));
        component.WaitForPages(3);

        var results = await component.InvokeAsync(() => Task.WhenAll(
            component.Instance.RenderPageHtmlAsync(2, TestContext.CancellationToken), component.Instance.RenderPageHtmlAsync(3, TestContext.CancellationToken)));

        Assert.Contains("Range page 2", results[0]);
        Assert.Contains("Range page 3", results[1]);
        Assert.IsEmpty(errors, string.Join("; ", errors));
    }

    [TestMethod]
    public async Task EncryptedRangeDocumentShouldRenderAfterWrongThenCorrectPasswordWithoutFallback()
    {
        byte[] bytes = TestPdf.Encrypted("pw", contentPadding: 100000);
        string expected = BitPdfDocument.Load(bytes, "pw").Pages[0].ExtractText();
        using var handler = new RangeHandler(bytes);
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        int prompts = 0, progress = 0;
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, CreateRangeDefinition())
            .Add(v => v.OnPasswordRequested, () => Task.FromResult<string?>(++prompts == 1 ? "wrong" : "pw"))
            .Add(v => v.OnProgress, (double _) => progress++)
            .Add(v => v.OnError, errors.Add));
        component.WaitForPages(1);

        string actual = await component.InvokeAsync(() => component.Instance.ExtractPageTextAsync(1, TestContext.CancellationToken));
        string html = await component.InvokeAsync(() => component.Instance.RenderPageHtmlAsync(1, TestContext.CancellationToken));

        Assert.AreEqual(expected, actual);
        Assert.IsFalse(string.IsNullOrEmpty(html));
        Assert.AreEqual(2, prompts);
        Assert.AreEqual(0, progress);
        Assert.IsNull(component.Instance.GetBytes());
        Assert.IsEmpty(errors, string.Join("; ", errors));
        var actualValue = handler.Requests.All(r => r?.StartsWith("bytes=", StringComparison.Ordinal) is true);
        Assert.IsTrue(actualValue);
    }

    private static byte[] Pdf(string catalogExtra, string pageExtra, Action<BitPdfEngineTestFile>? write = null, string? resources = null)
    {
        var file = new BitPdfEngineTestFile();
        file.Obj(1, $"<< /Type /Catalog /Pages 2 0 R {catalogExtra} >>");
        file.Obj(2, "<< /Type /Pages /Count 1 /Kids [3 0 R] >>");
        resources ??= "<< /Font << /F1 5 0 R >> >>";
        file.Obj(3, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources {resources} /Contents 4 0 R {pageExtra} >>");
        file.Stream(4, BitPdfEngineTestFile.ShowText("Deferred detail regression"));
        file.Obj(5, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        file.Obj(6, "<< /Title (Deferred details) >>");
        write?.Invoke(file);
        file.XRefTable($"/Size {file.Offsets.Keys.Max() + 1} /Root 1 0 R /Info 6 0 R", file.Offsets.Keys.Append(0));
        return file.ToArray();
    }
}
