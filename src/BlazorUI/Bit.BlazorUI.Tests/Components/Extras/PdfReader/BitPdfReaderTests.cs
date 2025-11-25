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

    private void SetupJs(int pages = 3)
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.Extras.initScripts");
        Context.JSInterop.Setup<int>("BitBlazorUI.PdfReader.setup").SetResult(pages);
        Context.JSInterop.SetupVoid("BitBlazorUI.PdfReader.renderPage");
        Context.JSInterop.SetupVoid("BitBlazorUI.PdfReader.refreshPage");
        Context.JSInterop.SetupVoid("BitBlazorUI.PdfReader.dispose");
    }

    [TestMethod]
    public void BitPdfReaderShouldRenderSingleCanvasWhenRenderAllPagesIsFalse()
    {
        SetupJs(3);

        var component = RenderComponent<BitPdfReader>(parameters =>
        {
            parameters.Add(p => p.Config, new BitPdfReaderConfig { Id = PdfId });
            parameters.Add(p => p.RenderAllPages, false);
        });

        component.WaitForAssertion(() =>
        {
            var canvas = component.Find("canvas");
            Assert.AreEqual(PdfId, canvas.Id);
            var renders = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.PdfReader.renderPage").ToList();
            Assert.AreEqual(1, renders.Count);
            Assert.AreEqual(1, (int)renders[0].Arguments[1]!);
        });
    }

    [TestMethod]
    public void BitPdfReaderShouldRenderAllPagesWhenEnabled()
    {
        SetupJs(4);

        var component = RenderComponent<BitPdfReader>(parameters =>
        {
            parameters.Add(p => p.Config, new BitPdfReaderConfig { Id = PdfId });
            parameters.Add(p => p.RenderAllPages, true);
        });

        component.WaitForAssertion(() =>
        {
            var canvases = component.FindAll("canvas");
            Assert.AreEqual(4, canvases.Count);
            Assert.IsTrue(canvases.Any(c => c.Id == $"{PdfId}-1"));
            Assert.IsTrue(canvases.Any(c => c.Id == $"{PdfId}-4"));

            var renders = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.PdfReader.renderPage").ToList();
            Assert.AreEqual(4, renders.Count);
        });
    }

    [TestMethod]
    public void BitPdfReaderShouldInvokeLoadedAndPageRenderedCallbacks()
    {
        SetupJs(2);

        var loaded = false;
        var rendered = false;

        var component = RenderComponent<BitPdfReader>(parameters =>
        {
            parameters.Add(p => p.Config, new BitPdfReaderConfig { Id = PdfId });
            parameters.Add(p => p.OnPdfLoaded, EventCallback.Factory.Create(this, () => loaded = true));
            parameters.Add(p => p.OnPdfPageRendered, EventCallback.Factory.Create(this, () => rendered = true));
        });

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(loaded);
            Assert.IsTrue(rendered);
        });
    }

    [TestMethod]
    public async Task BitPdfReaderNavigationShouldRenderCorrectPages()
    {
        SetupJs(3);

        var component = RenderComponent<BitPdfReader>(parameters =>
        {
            parameters.Add(p => p.Config, new BitPdfReaderConfig { Id = PdfId });
        });

        await component.Instance.Next();  // page 2
        await component.Instance.Prev();  // page 1
        await component.Instance.Last();  // page 3
        await component.Instance.First(); // page 1 again

        var renders = Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.PdfReader.renderPage")
                                                   .Select(i => (int)i.Arguments[1]!)
                                                   .ToList();

        CollectionAssert.AreEqual(new List<int> { 1, 2, 1, 3, 1 }, renders);
    }

    [TestMethod]
    public async Task BitPdfReaderRefreshAllShouldCallJsForEachPage()
    {
        SetupJs(3);

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
        SetupJs();

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
        SetupJs();

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
