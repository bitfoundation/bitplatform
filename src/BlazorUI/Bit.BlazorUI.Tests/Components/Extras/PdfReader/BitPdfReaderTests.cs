using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfReader;

[TestClass]
public class BitPdfReaderTests : BunitTestContext
{
    private const string PdfId = "pdf-test";

    private void SetupJSInterop(int pagesCount = 3)
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Extras.initScripts");
        Context.JSInterop.SetupVoid("BitBlazorUI.PdfReader.renderPage");
        Context.JSInterop.SetupVoid("BitBlazorUI.PdfReader.refreshPage");
        Context.JSInterop.SetupVoid("BitBlazorUI.PdfReader.dispose");

        Context.JSInterop.Setup<int>("BitBlazorUI.PdfReader.setup", inv => inv.Identifier == "BitBlazorUI.PdfReader.setup").SetResult(pagesCount);
    }

    [TestMethod]
    public void BitPdfReaderShouldRenderSingleCanvasWhenRenderAllPagesIsFalse()
    {
        SetupJSInterop(3);

        var component = RenderComponent<BitPdfReader>(parameters =>
        {
            parameters.Add(p => p.Config, new BitPdfReaderConfig { Id = PdfId });
            parameters.Add(p => p.RenderAllPages, false);
        });

        var canvas = component.Find("canvas");

        var renders = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.PdfReader.renderPage").ToList();

        Assert.AreEqual(PdfId, canvas.Id);
        Assert.AreEqual(1, renders.Count);
        Assert.AreEqual(1, (int)renders[0].Arguments[1]!);
    }

    [TestMethod]
    public void BitPdfReaderShouldRenderAllPagesWhenEnabled()
    {
        SetupJSInterop(4);

        var component = RenderComponent<BitPdfReader>(parameters =>
        {
            parameters.Add(p => p.Config, new BitPdfReaderConfig { Id = PdfId });
            parameters.Add(p => p.RenderAllPages, true);
        });

        var canvases = component.FindAll("canvas");

        var renders = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.PdfReader.renderPage").ToList();

        Assert.AreEqual(4, renders.Count);
        Assert.AreEqual(4, canvases.Count);
        Assert.IsTrue(canvases.Any(c => c.Id == $"{PdfId}-1"));
        Assert.IsTrue(canvases.Any(c => c.Id == $"{PdfId}-4"));
    }

    [TestMethod]
    public void BitPdfReaderShouldInvokeLoadedAndPageRenderedCallbacks()
    {
        SetupJSInterop(2);

        var loaded = false;
        var rendered = false;

        var component = RenderComponent<BitPdfReader>(parameters =>
        {
            parameters.Add(p => p.Config, new BitPdfReaderConfig { Id = PdfId });
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

        var component = RenderComponent<BitPdfReader>(parameters =>
        {
            parameters.Add(p => p.Config, new BitPdfReaderConfig { Id = PdfId });
        });

        await component.Instance.Next();
        await component.Instance.Prev();
        await component.Instance.Last();
        await component.Instance.First();

        var renders = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.PdfReader.renderPage")
                                                   .Select(i => (int)i.Arguments[1]!)
                                                   .ToList();

        CollectionAssert.AreEqual(new List<int> { 1, 2, 1, 3, 1 }, renders);
    }

    [TestMethod]
    public async Task BitPdfReaderRefreshAllShouldCallJsForEachPage()
    {
        SetupJSInterop(3);

        var component = RenderComponent<BitPdfReader>(parameters =>
        {
            parameters.Add(p => p.Config, new BitPdfReaderConfig { Id = PdfId });
            parameters.Add(p => p.RenderAllPages, true);
        });

        await component.Instance.RefreshAll();

        var refreshes = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.PdfReader.refreshPage").ToList();

        Assert.AreEqual(3, refreshes.Count);
    }

    [TestMethod]
    public void BitPdfReaderShouldRespectOrientationAndEnabledState()
    {
        SetupJSInterop();

        var component = RenderComponent<BitPdfReader>(parameters =>
        {
            parameters.Add(p => p.Config, new BitPdfReaderConfig { Id = PdfId });
            parameters.Add(p => p.Horizontal, true);
            parameters.Add(p => p.IsEnabled, false);
        });

        var root = component.Find(".bit-pdr");

        Assert.IsTrue(root.ClassList.Contains("bit-pdr-hor"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitPdfReaderShouldApplyCanvasClassAndStyle()
    {
        SetupJSInterop();

        var component = RenderComponent<BitPdfReader>(parameters =>
        {
            parameters.Add(p => p.Config, new BitPdfReaderConfig { Id = PdfId });
            parameters.Add(p => p.CanvasClass, "canvas-class");
            parameters.Add(p => p.CanvasStyle, "height:100px;");
        });

        var canvas = component.Find("canvas");

        Assert.IsTrue(canvas.ClassList.Contains("canvas-class"));
        Assert.IsTrue((canvas.GetAttribute("style") ?? string.Empty).Contains("height:100px"));
    }
}
