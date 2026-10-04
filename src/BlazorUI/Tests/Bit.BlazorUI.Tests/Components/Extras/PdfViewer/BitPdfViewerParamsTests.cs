using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

/// <summary>
/// Covers the BitParams cascade of the PdfViewer: what a BitPdfViewerParams fills in, what it leaves alone because
/// the viewer wrote it for itself, and that a cascade that goes away takes its values with it.
/// </summary>
[TestClass]
public class BitPdfViewerParamsTests : BunitTestContext
{
    // What belongs to a single viewer rather than to a group of them: its document, the page, zoom and rotation the
    // reader is on, and its events.
    private static readonly string[] _notCascaded =
    [
        nameof(BitPdfViewer.CascadingParameters),
        nameof(BitPdfViewer.Source),
        nameof(BitPdfViewer.CurrentPage),
        "CurrentPageChanged",
        nameof(BitPdfViewer.Zoom),
        "ZoomChanged",
        nameof(BitPdfViewer.Rotation),
        "RotationChanged",
        nameof(BitPdfViewer.OnDocumentLoaded),
        nameof(BitPdfViewer.OnDownloading),
        nameof(BitPdfViewer.OnError),
        nameof(BitPdfViewer.OnFileOpened),
        nameof(BitPdfViewer.OnPageChanged),
        nameof(BitPdfViewer.OnPageRendered),
        nameof(BitPdfViewer.OnPrinting),
        nameof(BitPdfViewer.OnProgress),
        nameof(BitPdfViewer.OnRotationChanged),
        nameof(BitPdfViewer.OnSidebarChanged),
        nameof(BitPdfViewer.OnWarnings),
        nameof(BitPdfViewer.OnZoomChanged),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitPdfViewerParams viewerParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { viewerParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitPdfViewer>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitPdfViewerParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitPdfViewer", BitPdfViewerParams.ParamName);
        Assert.AreEqual(BitPdfViewerParams.ParamName, new BitPdfViewerParams().Name);
    }

    [TestMethod]
    public void BitPdfViewerParamsShouldCarryEveryParameterThatBelongsToAGroupOfViewers()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitPdfViewer).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                             .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                             .Select(p => p.Name)
                                             .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                             .ToArray();

        Assert.IsTrue(parameters.Length > 20, "The reflection found too few parameters to be trusted.");

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitPdfViewerParams).GetProperty(name), $"BitPdfViewerParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitPdfViewerShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitPdfViewerParams
        {
            Class = "cascaded",
            Classes = new() { Root = "cascaded-root", Toolbar = "cascaded-toolbar" },
            Styles = new() { Toolbar = "color:red" },
            Height = "20rem",
            Width = "30rem",
            ToolbarItems = BitPdfToolbarItems.Navigation | BitPdfToolbarItems.Zoom,
            Texts = new() { ToolbarAriaLabel = "PDF-Werkzeugleiste", DocumentAriaLabel = "Dokument" },
            ToolbarEndTemplate = viewer => builder => builder.AddMarkupContent(0, "<button class=\"custom-end\">Share</button>"),
        });

        var root = component.Find(".bit-pdv");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        StringAssert.Contains(root.GetAttribute("style"), "--bit-PdfViewer-height:20rem");
        StringAssert.Contains(root.GetAttribute("style"), "--bit-PdfViewer-width:30rem");

        var toolbar = component.Find(".bit-pdv-toolbar");

        Assert.IsTrue(toolbar.ClassList.Contains("cascaded-toolbar"));
        StringAssert.Contains(toolbar.GetAttribute("style"), "color:red");
        Assert.AreEqual("PDF-Werkzeugleiste", toolbar.GetAttribute("aria-label"));

        // Only the two groups asked for: no find, no print, no download.
        Assert.AreEqual(0, component.FindAll("button[aria-label='Find in document']").Count);
        Assert.AreEqual(0, component.FindAll("button[aria-label='Print document']").Count);
        Assert.AreEqual(1, component.FindAll("button[aria-label='Zoom in']").Count);

        Assert.AreEqual(1, component.FindAll(".bit-pdv-toolbar .custom-end").Count);
        Assert.AreEqual("Dokument", component.Find(".bit-pdv-surface").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPdfViewerShouldTakeTheCascadedLayout()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>
            {
                new BitPdfViewerParams { ScrollMode = BitPdfScrollMode.Wrapped, CursorTool = BitPdfCursorTool.Pan },
            });
            parameters.AddChildContent<BitPdfViewer>(viewer => viewer.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(2))));
        });

        var viewer = component.FindComponent<BitPdfViewer>();

        viewer.WaitForAssertion(() => Assert.AreEqual(2, viewer.Instance.PageCount));

        Assert.AreEqual(BitPdfScrollMode.Wrapped, viewer.Instance.CurrentScrollMode);
        Assert.AreEqual(BitPdfCursorTool.Pan, viewer.Instance.CurrentCursorTool);
        Assert.AreEqual(1, component.FindAll(".bit-pdv-pages.bit-pdv-w").Count);
        Assert.AreEqual(1, component.FindAll(".bit-pdv-surface.bit-pdv-pan").Count);
    }

    [TestMethod]
    public void BitPdfViewerShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitPdfViewerParams
        {
            ShowToolbar = false,
            Height = "20rem",
            Texts = new() { DocumentAriaLabel = "Cascaded" },
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitPdfViewer.ShowToolbar), true);
            builder.AddAttribute(2, nameof(BitPdfViewer.Height), "5rem");
            builder.AddAttribute(3, nameof(BitPdfViewer.AriaLabel), "Own");
        });

        Assert.AreEqual(1, component.FindAll(".bit-pdv-toolbar").Count);
        StringAssert.Contains(component.Find(".bit-pdv").GetAttribute("style"), "--bit-PdfViewer-height:5rem");
        Assert.AreEqual("Own", component.Find(".bit-pdv-surface").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPdfViewerShouldDropTheCascadedValuesWhenTheParamsGoAway()
    {
        var viewerParams = new BitPdfViewerParams { ShowToolbar = false, Height = "20rem" };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { viewerParams });
            parameters.AddChildContent<BitPdfViewer>();
        });

        Assert.AreEqual(0, component.FindAll(".bit-pdv-toolbar").Count);
        StringAssert.Contains(component.Find(".bit-pdv").GetAttribute("style"), "--bit-PdfViewer-height:20rem");

        component.Render(parameters => parameters.Add(p => p.Parameters, new List<IBitComponentParams>()));

        Assert.AreEqual(1, component.FindAll(".bit-pdv-toolbar").Count);
        Assert.IsFalse((component.Find(".bit-pdv").GetAttribute("style") ?? string.Empty).Contains("--bit-PdfViewer-height"));
    }
}
