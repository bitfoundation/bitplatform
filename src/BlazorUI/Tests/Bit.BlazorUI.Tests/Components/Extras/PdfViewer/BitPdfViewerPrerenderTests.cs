using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

/// <summary>
/// What a prerender (or a static SSR page) of the viewer shows: the document parsed and its current page drawn by the
/// C# renderer, with every other page a slot of its final size, and - for a source the server cannot reach - the
/// loading state the interactive render then picks up from, rather than an error.
/// </summary>
[TestClass]
public class BitPdfViewerPrerenderTests : BunitTestContext
{
    private static IDocument Parse(string html) => new HtmlParser().ParseDocument(html);

    [TestMethod]
    public async Task BitPdfViewerShouldPrerenderTheCurrentPage()
    {
        var html = await Prerenderer.RenderAsync<BitPdfViewer>(new Dictionary<string, object?>
        {
            [nameof(BitPdfViewer.Source)] = BitPdfSource.FromBytes(TestPdf.MultiPage(3)),
        });

        var pages = Parse(html).QuerySelectorAll(".bit-pdv-page").ToArray();

        Assert.AreEqual(3, pages.Length);
        Assert.IsNull(pages[0].QuerySelector(".bit-pdv-page-placeholder"));
        StringAssert.Contains(pages[0].TextContent.Replace(" ", ""), "Page1");

        // The pages not drawn yet are placeholders already of their page's size, so the ones drawn later as they
        // scroll into view take the place they had and nothing below them moves.
        foreach (var page in pages.Skip(1))
        {
            Assert.IsNotNull(page.QuerySelector(".bit-pdv-page-placeholder"));
            StringAssert.Contains(page.GetAttribute("style"), "width:");
            StringAssert.Contains(page.GetAttribute("style"), "height:");
            Assert.AreEqual(pages[0].GetAttribute("style"), page.GetAttribute("style"));
        }
    }

    [TestMethod]
    public async Task BitPdfViewerShouldPrerenderTheLoadingStateWithoutAnHttpClient()
    {
        var errors = new List<string?>();

        var html = await Prerenderer.RenderAsync<BitPdfViewer>(new Dictionary<string, object?>
        {
            [nameof(BitPdfViewer.Source)] = BitPdfSource.FromUrl("/docs/sample.pdf"),
            [nameof(BitPdfViewer.OnError)] = EventCallback.Factory.Create<string?>(this, errors.Add),
        });

        var document = Parse(html);

        Assert.IsNotNull(document.QuerySelector(".bit-pdv-page-loading"));
        Assert.IsNull(document.QuerySelector(".bit-pdv-error"));
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldPrerenderTheLoadingStateForAUrlTheServerCannotReach()
    {
        var errors = new List<string?>();

        // The server's HttpClient has no BaseAddress, so a relative URL - the browser's to resolve - is not one it
        // can fetch at all.
        var html = await Prerenderer.RenderAsync<BitPdfViewer>(new Dictionary<string, object?>
        {
            [nameof(BitPdfViewer.Source)] = BitPdfSource.FromUrl("/docs/sample.pdf"),
            [nameof(BitPdfViewer.OnError)] = EventCallback.Factory.Create<string?>(this, errors.Add),
        }, services => services.AddSingleton(new HttpClient()));

        var document = Parse(html);

        Assert.IsNotNull(document.QuerySelector(".bit-pdv-page-loading"));
        Assert.IsNull(document.QuerySelector(".bit-pdv-error"));
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void BitPdfViewerShouldStillReportALoadThatFailsOnceRendered()
    {
        // Interactive, the first render comes before the fetch does, so its failure is the document's own and is
        // reported as it always was - only one that comes before the page has ever rendered is held back.
        var handler = new FailingHandler();
        Context.Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") });
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var errors = new List<string?>();
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromUrl("docs/missing.pdf"));
            parameters.Add(p => p.OnError, errors.Add);
        });

        component.WaitForAssertion(() => Assert.IsNotNull(component.Find(".bit-pdv-error")));
        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual(1, handler.Requests);
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            throw new HttpRequestException("The server cannot reach this URL.");
        }
    }
}
