using System.Collections.Generic;
using Bit.BlazorUI.Tests;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Legacy.Tests.PdfReader;

[TestClass]
public class BitPdfReaderTests : BunitTestContext
{
    private const string PdfId = "pdf-test";

    private void SetupJSInterop(int pagesCount = 3)
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Legacy.Utils.initScripts");
        Context.JSInterop.SetupVoid("BitBlazorUI.Legacy.PdfReader.renderPage");
        Context.JSInterop.SetupVoid("BitBlazorUI.Legacy.PdfReader.refreshPage");
        Context.JSInterop.SetupVoid("BitBlazorUI.Legacy.PdfReader.dispose");

        Context.JSInterop.Setup<int>("BitBlazorUI.Legacy.PdfReader.setup", inv => inv.Identifier == "BitBlazorUI.Legacy.PdfReader.setup").SetResult(pagesCount);
    }

    [TestMethod]
    public void BitPdfReaderShouldRenderSingleCanvasWhenRenderAllPagesIsFalse()
    {
        SetupJSInterop(3);

        var component = RenderComponent<BitPdfReaderLegacy>(parameters =>
        {
            parameters.Add(p => p.Config, new BitPdfReaderLegacyConfig { Id = PdfId });
            parameters.Add(p => p.RenderAllPages, false);
        });

        var canvas = component.Find("canvas");

        var renders = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.Legacy.PdfReader.renderPage").ToList();

        Assert.AreEqual(PdfId, canvas.Id);
        Assert.ContainsSingle(renders);
        Assert.AreEqual(1, (int)renders[0].Arguments[1]!);
    }

    [TestMethod]
    public void BitPdfReaderShouldRenderAllPagesWhenEnabled()
    {
        SetupJSInterop(4);

        var component = RenderComponent<BitPdfReaderLegacy>(parameters =>
        {
            parameters.Add(p => p.Config, new BitPdfReaderLegacyConfig { Id = PdfId });
            parameters.Add(p => p.RenderAllPages, true);
        });

        var canvases = component.FindAll("canvas");

        var renders = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.Legacy.PdfReader.renderPage").ToList();

        Assert.HasCount(4, renders);
        Assert.HasCount(4, canvases);
        Assert.Contains(c => c.Id == $"{PdfId}-1", canvases);
        Assert.Contains(c => c.Id == $"{PdfId}-4", canvases);
    }

    [TestMethod]
    public void BitPdfReaderShouldInvokeLoadedAndPageRenderedCallbacks()
    {
        SetupJSInterop(2);

        var loaded = false;
        var rendered = false;

        var component = RenderComponent<BitPdfReaderLegacy>(parameters =>
        {
            parameters.Add(p => p.Config, new BitPdfReaderLegacyConfig { Id = PdfId });
            parameters.Add(p => p.OnPdfLoaded, EventCallback.Factory.Create(this, () => loaded = true));
            parameters.Add(p => p.OnPdfPageRendered, EventCallback.Factory.Create(this, () => rendered = true));
        });

        Assert.IsTrue(loaded);
        Assert.IsTrue(rendered);
    }

    [TestMethod]
    public async Task BitPdfReaderNavigationShouldRenderCorrectPages()
    {
        SetupJSInterop(3);

        var component = RenderComponent<BitPdfReaderLegacy>(parameters =>
        {
            parameters.Add(p => p.Config, new BitPdfReaderLegacyConfig { Id = PdfId });
        });

        await component.Instance.Next();
        await component.Instance.Prev();
        await component.Instance.Last();
        await component.Instance.First();

        var renders = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.Legacy.PdfReader.renderPage")
                                                   .Select(i => (int)i.Arguments[1]!)
                                                   .ToList();

        Assert.AreSequenceEqual(new List<int> { 1, 2, 1, 3, 1 }, renders);
    }

    [TestMethod]
    public async Task BitPdfReaderRefreshAllShouldCallJsForEachPage()
    {
        SetupJSInterop(3);

        var component = RenderComponent<BitPdfReaderLegacy>(parameters =>
        {
            parameters.Add(p => p.Config, new BitPdfReaderLegacyConfig { Id = PdfId });
            parameters.Add(p => p.RenderAllPages, true);
        });

        await component.Instance.RefreshAll();

        var refreshes = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.Legacy.PdfReader.refreshPage").ToList();

        Assert.HasCount(3, refreshes);
    }

    [TestMethod]
    public void BitPdfReaderShouldRespectOrientationAndEnabledState()
    {
        SetupJSInterop();

        var component = RenderComponent<BitPdfReaderLegacy>(parameters =>
        {
            parameters.Add(p => p.Config, new BitPdfReaderLegacyConfig { Id = PdfId });
            parameters.Add(p => p.Horizontal, true);
            parameters.Add(p => p.Disabled, true);
        });

        var root = component.Find(".bit-pdr");

        Assert.IsTrue(root.ClassList.Contains("bit-pdr-hor"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitPdfReaderShouldApplyCanvasClassAndStyle()
    {
        SetupJSInterop();

        var component = RenderComponent<BitPdfReaderLegacy>(parameters =>
        {
            parameters.Add(p => p.Config, new BitPdfReaderLegacyConfig { Id = PdfId });
            parameters.Add(p => p.CanvasClass, "canvas-class");
            parameters.Add(p => p.CanvasStyle, "height:100px;");
        });

        var canvas = component.Find("canvas");

        Assert.IsTrue(canvas.ClassList.Contains("canvas-class"));
        Assert.Contains("height:100px", (canvas.GetAttribute("style") ?? string.Empty));
    }

    [TestMethod]
    public async Task BitPdfReaderShouldStartOnTheInitialPageAndStayInsideTheDocument()
    {
        SetupJSInterop(3);

        var component = RenderComponent<BitPdfReaderLegacy>(parameters =>
        {
            parameters.Add(p => p.Config, new BitPdfReaderLegacyConfig { Id = PdfId });
            parameters.Add(p => p.InitialPageNumber, 2);
        });

        Assert.AreEqual(3, component.Instance.NumberOfPages);
        Assert.AreEqual(2, component.Instance.CurrentPageNumber);

        // A page before the first or after the last is ignored rather than asked of the script.
        await component.Instance.Go(0);
        await component.Instance.Go(4);

        Assert.AreEqual(2, component.Instance.CurrentPageNumber);

        var renders = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.Legacy.PdfReader.renderPage")
                                                   .Select(i => (int)i.Arguments[1]!)
                                                   .ToList();

        Assert.AreSequenceEqual(new List<int> { 2 }, renders);
    }

    [TestMethod]
    public async Task BitPdfReaderRefreshShouldRefreshTheCurrentPage()
    {
        SetupJSInterop(3);

        var rendered = 0;

        var component = RenderComponent<BitPdfReaderLegacy>(parameters =>
        {
            parameters.Add(p => p.Config, new BitPdfReaderLegacyConfig { Id = PdfId });
            parameters.Add(p => p.OnPdfPageRendered, () => rendered++);
        });

        await component.Instance.Next();
        await component.Instance.Refresh();

        var refresh = Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.PdfReader.refreshPage");
        Assert.AreEqual(2, refresh.Arguments[1]);
        // The first page rendered on setup, and the refreshed one.
        Assert.AreEqual(2, rendered);
    }

    [TestMethod]
    public async Task BitPdfReaderShouldPrerenderItsCanvasWithoutTheBrowser()
    {
        // pdf.js is only loaded once the page is interactive: the canvas it draws into is what a prerender sends.
        var html = await Prerenderer.RenderAsync<BitPdfReaderLegacy>(new Dictionary<string, object?>
        {
            [nameof(BitPdfReaderLegacy.Config)] = new BitPdfReaderLegacyConfig { Id = PdfId },
            [nameof(BitPdfReaderLegacy.CanvasClass)] = "canvas-class",
            [nameof(BitPdfReaderLegacy.Horizontal)] = true,
        });

        Assert.Contains("bit-pdr", html);
        Assert.Contains("bit-pdr-hor", html);
        Assert.Contains($"id=\"{PdfId}\"", html);
        Assert.Contains("canvas-class", html);
        Assert.ContainsSingle(System.Text.RegularExpressions.Regex.Matches(html, "<canvas"));
    }

    [TestMethod]
    public void BitPdfReaderShouldSplatTheHtmlAttributesOnTheRoot()
    {
        SetupJSInterop();

        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitPdfReaderLegacy>(0);
            builder.AddAttribute(1, nameof(BitPdfReaderLegacy.Config), new BitPdfReaderLegacyConfig { Id = PdfId });
            builder.AddAttribute(2, nameof(BitPdfReaderLegacy.Id), "reader");
            builder.AddAttribute(3, nameof(BitPdfReaderLegacy.Class), "custom-class");
            builder.AddAttribute(4, nameof(BitPdfReaderLegacy.Style), "padding:1px");
            builder.AddAttribute(5, nameof(BitPdfReaderLegacy.Dir), BitDir.Rtl);
            builder.AddAttribute(6, "data-test", "root");
            builder.CloseComponent();
        });

        var root = component.Find(".bit-pdr");

        Assert.AreEqual("reader", root.Id);
        Assert.AreEqual("root", root.GetAttribute("data-test"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.Contains("padding:1px", root.GetAttribute("style")!);
        // The canvas keeps its own id, the one pdf.js draws into.
        Assert.AreEqual(PdfId, component.Find("canvas").Id);
    }

    [TestMethod]
    public async Task BitPdfReaderShouldDisposeJsInterop()
    {
        SetupJSInterop();

        var component = RenderComponent<BitPdfReaderLegacy>(parameters =>
        {
            parameters.Add(p => p.Config, new BitPdfReaderLegacyConfig { Id = PdfId });
        });

        await component.Instance.DisposeAsync();

        Assert.AreEqual(PdfId, Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.PdfReader.dispose").Arguments[0]);

        // Disposing again is a no-op rather than a second teardown.
        await component.Instance.DisposeAsync();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Legacy.PdfReader.dispose", 1);
    }
}
