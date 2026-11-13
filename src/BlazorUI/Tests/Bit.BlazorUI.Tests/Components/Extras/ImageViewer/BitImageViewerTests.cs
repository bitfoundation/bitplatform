using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.ImageViewer;

[TestClass]
public class BitImageViewerTests : BunitTestContext
{
    private const string Setup = "BitBlazorUI.BitImageViewer.setup";

    // The handle the script hands back, recording what the viewer tells it, in order.
    private sealed class Handle(BunitJSModuleInterop module)
    {
        public List<(string Identifier, object?[]? Args)> Calls => module.Invocations.Select(i => (i.Identifier, (object?[]?)i.Arguments.ToArray())).ToList();
    }

    private Handle SetupHandle()
    {
        var module = Context.JSInterop.SetupModule(invocation => invocation.Identifier == Setup);
        module.Mode = JSRuntimeMode.Loose;
        return new Handle(module);
    }

    private IRenderedComponent<BitImageViewer> Render(Action<ComponentParameterCollectionBuilder<BitImageViewer>>? more = null, List<double>? zoomChanges = null, string? src = "/photo.jpg")
    {
        return RenderComponent<BitImageViewer>(parameters =>
        {
            parameters.Add(p => p.Id, "imv");
            parameters.Add(p => p.Src, src);
            parameters.Add(p => p.Alt, "A lighthouse at dusk");
            if (zoomChanges is not null) parameters.Add(p => p.ZoomChanged, (double z) => zoomChanges.Add(z));
            more?.Invoke(parameters);
        });
    }

    private static T Option<T>(object options, string name) => (T)options.GetType().GetProperty(name)!.GetValue(options)!;

    [TestMethod]
    public void BitImageViewerShouldRenderAFocusableNamedViewportAroundTheImage()
    {
        var com = Render();

        Assert.IsTrue(com.Find("#imv").ClassList.Contains("bit-imv"));

        var viewport = com.Find(".bit-imv-vpt");

        Assert.AreEqual("region", viewport.GetAttribute("role"));
        Assert.AreEqual("0", viewport.GetAttribute("tabindex"));
        Assert.AreEqual("image viewer", viewport.GetAttribute("aria-roledescription"));
        Assert.AreEqual("A lighthouse at dusk", viewport.GetAttribute("aria-label"));
        Assert.AreEqual("+ - 0 ArrowUp ArrowDown ArrowLeft ArrowRight", viewport.GetAttribute("aria-keyshortcuts"));
        Assert.IsNull(viewport.GetAttribute("aria-disabled"));

        // Described by how it is operated, in a text only the screen readers get.
        var instructions = com.Find($"#{viewport.GetAttribute("aria-describedby")}");
        Assert.IsTrue(instructions.ClassList.Contains("bit-imv-ins"));
        StringAssert.Contains(instructions.TextContent, "Plus and minus zoom");

        var img = viewport.QuerySelector("img.bit-imv-img")!;
        Assert.AreEqual("/photo.jpg", img.GetAttribute("src"));
        Assert.AreEqual("A lighthouse at dusk", img.GetAttribute("alt"));
        Assert.AreEqual("false", img.GetAttribute("draggable"));
    }

    [TestMethod]
    public void BitImageViewerShouldRenderANamedGroupOfZoomButtons()
    {
        var com = Render();

        var toolbar = com.Find(".bit-imv-tbr");
        Assert.AreEqual("group", toolbar.GetAttribute("role"));
        Assert.AreEqual("Zoom", toolbar.GetAttribute("aria-label"));

        var buttons = com.FindAll(".bit-imv-btn");
        CollectionAssert.AreEqual(new[] { "Zoom out", "Zoom in", "Fit to view" }, buttons.Select(b => b.GetAttribute("aria-label")).ToArray());
        Assert.IsTrue(buttons.All(b => b.GetAttribute("type") == "button"));

        Assert.AreEqual("100%", com.Find(".bit-imv-zmt").TextContent);
        Assert.AreEqual("polite", com.Find(".bit-imv-zmt").GetAttribute("aria-live"));

        // At the fitted zoom, the smallest one by default, there is no zooming out nor anything to fit.
        Assert.IsTrue(buttons[0].HasAttribute("disabled"));
        Assert.AreEqual("true", buttons[0].GetAttribute("aria-disabled"));
        Assert.IsFalse(buttons[1].HasAttribute("disabled"));
        Assert.IsTrue(buttons[2].HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitImageViewerShouldBeNamedByItsAriaLabelOverItsAlt()
    {
        var com = Render(p => p.Add(x => x.AriaLabel, "Floor plan"));

        Assert.AreEqual("Floor plan", com.Find(".bit-imv-vpt").GetAttribute("aria-label"));
        Assert.AreEqual("A lighthouse at dusk", com.Find("img").GetAttribute("alt"));
    }

    [TestMethod]
    public void BitImageViewerWithoutASrcShouldRenderNoImage()
    {
        var com = Render(src: null);

        Assert.AreEqual(0, com.FindAll("img").Count);
        Assert.IsNotNull(com.Find(".bit-imv-vpt"));
    }

    [TestMethod]
    public void BitImageViewerShouldSetTheHeightOfItsViewport()
    {
        var com = Render(p =>
        {
            p.Add(x => x.Height, "30rem");
            p.Add(x => x.Styles, new BitImageViewerClassStyles { Viewport = "margin:1px" });
        });

        Assert.AreEqual("height:30rem;margin:1px", com.Find(".bit-imv-vpt").GetAttribute("style"));
    }

    [TestMethod]
    public void BitImageViewerButtonsShouldStepTheZoomAndFitItAgain()
    {
        var changes = new List<double>();
        var com = Render(zoomChanges: changes);

        var zoomIn = com.Find("[aria-label='Zoom in']");
        zoomIn.Click();
        com.Find("[aria-label='Zoom in']").Click();

        Assert.AreEqual(1.5625, com.Instance.Zoom);
        Assert.AreEqual("156%", com.Find(".bit-imv-zmt").TextContent);
        Assert.IsFalse(com.Find("[aria-label='Zoom out']").HasAttribute("disabled"));
        Assert.IsFalse(com.Find("[aria-label='Fit to view']").HasAttribute("disabled"));

        com.Find("[aria-label='Zoom out']").Click();
        Assert.AreEqual(1.25, com.Instance.Zoom);

        com.Find("[aria-label='Fit to view']").Click();
        Assert.AreEqual(1, com.Instance.Zoom);

        CollectionAssert.AreEqual(new[] { 1.25, 1.5625, 1.25, 1 }, changes);
    }

    [TestMethod]
    public void BitImageViewerShouldStepByItsZoomFactorAndStopAtMaxZoom()
    {
        var com = Render(p =>
        {
            p.Add(x => x.ZoomFactor, 2);
            p.Add(x => x.MaxZoom, 3);
        });

        com.Find("[aria-label='Zoom in']").Click();
        Assert.AreEqual(2, com.Instance.Zoom);

        com.Find("[aria-label='Zoom in']").Click();
        Assert.AreEqual(3, com.Instance.Zoom, "a step past the largest zoom stops at it");
        Assert.IsTrue(com.Find("[aria-label='Zoom in']").HasAttribute("disabled"));
    }

    [TestMethod]
    [DataRow(20, "800%")]
    [DataRow(0.1, "100%")]
    public void BitImageViewerShouldShowAZoomOutOfRangeAsTheZoomItIsHeldAt(double zoom, string shown)
    {
        var com = Render(p => p.Add(x => x.Zoom, zoom));

        Assert.AreEqual(shown, com.Find(".bit-imv-zmt").TextContent);
    }

    [TestMethod]
    public void BitImageViewerShouldAllowAZoomBelowTheFittedOneWithALowerMinZoom()
    {
        var com = Render(p => p.Add(x => x.MinZoom, 0.5));

        Assert.IsFalse(com.Find("[aria-label='Zoom out']").HasAttribute("disabled"));

        com.Find("[aria-label='Zoom out']").Click();
        Assert.AreEqual(0.8, com.Instance.Zoom);

        // Fitting it again is still a step to take.
        Assert.IsFalse(com.Find("[aria-label='Fit to view']").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitImageViewerShouldSetTheScriptUpWithItsOptionsAndZoom()
    {
        SetupHandle();

        Render(p =>
        {
            p.Add(x => x.Zoom, 2);
            p.Add(x => x.MinZoom, 0.5);
            p.Add(x => x.MaxZoom, 4);
            p.Add(x => x.ZoomFactor, 1.5);
            p.Add(x => x.DoubleClickZoom, 6);
            p.Add(x => x.WheelZoom, true);
        });

        var invocation = Context.JSInterop.Invocations[Setup].Single();
        var options = invocation.Arguments[2]!;

        Assert.AreEqual(0.5, Option<double>(options, "Min"));
        Assert.AreEqual(4, Option<double>(options, "Max"));
        Assert.AreEqual(1.5, Option<double>(options, "Factor"));
        Assert.AreEqual(4, Option<double>(options, "DoubleClick"), "a double-click zoom past the largest one is held at it");
        Assert.IsTrue(Option<bool>(options, "Wheel"));
        Assert.IsFalse(Option<bool>(options, "Disabled"));
        Assert.AreEqual(2.0, invocation.Arguments[3]);
    }

    [TestMethod]
    public void BitImageViewerShouldTellTheScriptTheZoomItSets_ButNotTheOneTheScriptReported()
    {
        var handle = SetupHandle();
        var changes = new List<double>();
        var com = Render(zoomChanges: changes);

        com.Find("[aria-label='Zoom in']").Click();

        var setZoom = handle.Calls.Where(c => c.Identifier == "setZoom").ToList();
        Assert.AreEqual(1, setZoom.Count);
        Assert.AreEqual(1.25, setZoom[0].Args![0]);

        // A zoom the wheel, a pinch or the keyboard arrived at is the script's own: the viewer takes it, and sends
        // nothing back.
        com.InvokeAsync(() => com.Instance._OnZoom(3.2)).Wait();

        Assert.AreEqual(3.2, com.Instance.Zoom);
        Assert.AreEqual("320%", com.Find(".bit-imv-zmt").TextContent);
        CollectionAssert.AreEqual(new[] { 1.25, 3.2 }, changes);
        Assert.AreEqual(1, handle.Calls.Count(c => c.Identifier == "setZoom"));
    }

    [TestMethod]
    public void BitImageViewerShouldPutTheScriptBackWhereAOneWayZoomHoldsIt()
    {
        var handle = SetupHandle();
        var com = Render(p => p.Add(x => x.Zoom, 2));

        com.InvokeAsync(() => com.Instance._OnZoom(3)).Wait();

        Assert.AreEqual(2, com.Instance.Zoom);
        Assert.AreEqual(2.0, handle.Calls.Last(c => c.Identifier == "setZoom").Args![0]);
    }

    [TestMethod]
    public void BitImageViewerShouldUpdateTheScriptWhenItsOptionsChange()
    {
        var handle = SetupHandle();
        var com = Render();

        com.Render(p => p.Add(x => x.MaxZoom, 3));

        var update = handle.Calls.Single(c => c.Identifier == "update");
        Assert.AreEqual(3, Option<double>(update.Args![0]!, "Max"));

        // A render that changes nothing tells it nothing.
        com.Render(p => p.Add(x => x.MaxZoom, 3));
        Assert.AreEqual(1, handle.Calls.Count(c => c.Identifier == "update"));
    }

    [TestMethod]
    public async Task BitImageViewerShouldDisposeTheScriptWithItself()
    {
        var handle = SetupHandle();
        Render();

        await Context.DisposeComponentsAsync();

        Assert.AreEqual("dispose", handle.Calls.Last().Identifier);
    }

    [TestMethod]
    public void DisabledBitImageViewerShouldNeitherZoomNorTakeTheFocus()
    {
        SetupHandle();

        // Bound both ways, so a zoom refused here is refused by the viewer and not by a one-way binding.
        var changes = new List<double>();
        var com = Render(p =>
        {
            p.Add(x => x.Disabled, true);
            p.Add(x => x.Zoom, 2);
        }, changes);

        var viewport = com.Find(".bit-imv-vpt");
        Assert.AreEqual("-1", viewport.GetAttribute("tabindex"));
        Assert.AreEqual("true", viewport.GetAttribute("aria-disabled"));

        Assert.IsTrue(com.FindAll(".bit-imv-btn").All(b => b.HasAttribute("disabled") && b.GetAttribute("aria-disabled") == "true"));
        Assert.IsTrue(Option<bool>(Context.JSInterop.Invocations[Setup].Single().Arguments[2]!, "Disabled"));

        com.InvokeAsync(() => com.Instance.ZoomIn()).Wait();
        com.InvokeAsync(() => com.Instance.Reset()).Wait();
        Assert.AreEqual(2, com.Instance.Zoom);
        Assert.AreEqual(0, changes.Count);
    }

    [TestMethod]
    public void BitImageViewerWithNoToolbarShouldRenderNoButtons()
    {
        var com = Render(p => p.Add(x => x.NoToolbar, true));

        Assert.AreEqual(0, com.FindAll(".bit-imv-tbr").Count);
        Assert.AreEqual(0, com.FindAll("button").Count);
    }

    [TestMethod]
    public void BitImageViewerShouldSayItsTexts()
    {
        var com = Render(p => p.Add(x => x.Texts, new BitImageViewerTexts
        {
            RoleDescription = "visor",
            Instructions = "Usa más y menos.",
            ToolbarAriaLabel = "Ampliación",
            ZoomIn = "Acercar",
            ZoomOut = "Alejar",
            Reset = "Ajustar",
        }));

        Assert.AreEqual("visor", com.Find(".bit-imv-vpt").GetAttribute("aria-roledescription"));
        Assert.AreEqual("Usa más y menos.", com.Find(".bit-imv-ins").TextContent);
        Assert.AreEqual("Ampliación", com.Find(".bit-imv-tbr").GetAttribute("aria-label"));
        CollectionAssert.AreEqual(new[] { "Alejar", "Acercar", "Ajustar" }, com.FindAll(".bit-imv-btn").Select(b => b.GetAttribute("title")).ToArray());
    }

    [TestMethod]
    public void BitImageViewerShouldApplyTheClassesAndStylesOfItsParts()
    {
        var com = Render(p =>
        {
            p.Add(x => x.Classes, new BitImageViewerClassStyles { Root = "c-root", Viewport = "c-vpt", Image = "c-img", Toolbar = "c-tbr", Button = "c-btn", ZoomText = "c-zmt" });
            p.Add(x => x.Styles, new BitImageViewerClassStyles { Root = "margin:1px", Viewport = "margin:2px", Image = "margin:3px", Toolbar = "margin:4px", Button = "margin:5px", ZoomText = "margin:6px" });
        });

        void AssertPart(string selector, string cssClass, string style)
        {
            foreach (var element in com.FindAll(selector))
            {
                Assert.IsTrue(element.ClassList.Contains(cssClass), $"{selector} has no {cssClass}.");
                StringAssert.Contains(element.GetAttribute("style"), style, $"{selector} has no {style}.");
            }
        }

        AssertPart(".bit-imv", "c-root", "margin:1px");
        AssertPart(".bit-imv-vpt", "c-vpt", "margin:2px");
        AssertPart(".bit-imv-img", "c-img", "margin:3px");
        AssertPart(".bit-imv-tbr", "c-tbr", "margin:4px");
        AssertPart(".bit-imv-btn", "c-btn", "margin:5px");
        AssertPart(".bit-imv-zmt", "c-zmt", "margin:6px");
        Assert.AreEqual(3, com.FindAll(".bit-imv-btn.c-btn").Count);
    }
}
