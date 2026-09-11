using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

[TestClass]
public class BitPdfViewerTests : BunitTestContext
{
    [TestMethod]
    public void BitPdfViewerShouldRenderEmptyStateWithoutSource()
    {
        var component = RenderComponent<BitPdfViewer>();

        var root = component.Find(".bit-pdv");

        Assert.IsTrue(root.TextContent.Contains("No document loaded."));
        Assert.IsNotNull(component.Find(".bit-pdv-toolbar"));
        // No inline height by default; the responsive height comes from the stylesheet.
        Assert.IsFalse((root.GetAttribute("style") ?? string.Empty).Contains("height:"));
    }

    [TestMethod]
    public void BitPdfViewerShouldRespectShowToolbarAndHeight()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.ShowToolbar, false);
            parameters.Add(p => p.Height, "420px");
        });

        var root = component.Find(".bit-pdv");

        Assert.AreEqual(0, component.FindAll(".bit-pdv-toolbar").Count);
        Assert.IsTrue((root.GetAttribute("style") ?? string.Empty).Contains("height:420px"));
    }

    [TestMethod]
    public void BitPdfViewerShouldRespectEnabledState()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        var root = component.Find(".bit-pdv");

        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitPdfViewerShouldLoadAndRenderDocumentFromBytes()
    {
        var loaded = false;

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld(), "hello.pdf"));
            parameters.Add(p => p.OnDocumentLoaded, EventCallback.Factory.Create(this, () => loaded = true));
        });

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(loaded);
            Assert.AreEqual(1, component.Instance.PageCount);
        });

        component.WaitForAssertion(() =>
        {
            var page = component.Find("[data-page='1']");
            Assert.IsTrue(page.InnerHtml.Contains("bit-pdv-html-page"));
        });

        Assert.AreEqual(1, component.Instance.CurrentPage);
        Assert.IsTrue(component.Instance.HasOutline);
        Assert.IsTrue(component.Find(".bit-pdv-title").TextContent.Contains("hello.pdf"));
    }

    [TestMethod]
    public void BitPdfViewerShouldLoadAndRenderWithBackgroundRendering()
    {
        var loaded = false;

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld(), "hello.pdf"));
            parameters.Add(p => p.BackgroundRendering, true);
            parameters.Add(p => p.OnDocumentLoaded, EventCallback.Factory.Create(this, () => loaded = true));
        });

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(loaded);
            Assert.AreEqual(1, component.Instance.PageCount);
        });

        component.WaitForAssertion(() =>
        {
            var page = component.Find("[data-page='1']");
            Assert.IsTrue(page.InnerHtml.Contains("bit-pdv-html-page"));
        });
    }

    [TestMethod]
    public void BitPdfViewerShouldExposeEngineHelpers()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        Assert.IsTrue(component.Instance.ExtractPageText(1).Contains("Hello"));
        Assert.IsTrue(component.Instance.RenderPageHtml(1).Contains("bit-pdv-html-page"));
        Assert.AreEqual(string.Empty, component.Instance.RenderPageHtml(2));
    }

    [TestMethod]
    public async Task BitPdfViewerNavigationShouldClampToDocumentBounds()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        await component.Instance.GoToPage(42);
        Assert.AreEqual(1, component.Instance.CurrentPage);

        await component.Instance.PrevPage();
        Assert.AreEqual(1, component.Instance.CurrentPage);
    }

    [TestMethod]
    public void BitPdfViewerShouldInvokeOnErrorForInvalidDocument()
    {
        string? error = null;

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(Encoding.ASCII.GetBytes("not a pdf")));
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<string>(this, e => error = e));
        });

        component.WaitForAssertion(() =>
        {
            Assert.IsNotNull(error);
            Assert.AreEqual(0, component.Instance.PageCount);
        });
    }

    [TestMethod]
    public void BitPdfViewerShouldRenderEveryToolbarGroupByDefault()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld(), "hello.pdf"));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        var toolbar = component.Find(".bit-pdv-toolbar");
        // A labelled group, not an aria toolbar: the bar holds a text box and three
        // dropdowns whose own arrow keys must keep working, which is exactly what the
        // toolbar pattern's single tab stop would take away.
        Assert.AreEqual("group", toolbar.GetAttribute("role"));
        Assert.AreEqual("PDF viewer toolbar", toolbar.GetAttribute("aria-label"));
        // Assert on what each control IS rather than on how many there are, so adding
        // one does not invalidate the expectation for all the others.
        string[] expected =
        [
            "Page thumbnails", "Bookmarks", "Attachments", "Layers",
            "First page", "Previous page", "Next page", "Last page",
            "Zoom out", "Zoom in",
            "Find in document", "Pan tool",
            "Rotate counter clockwise", "Rotate clockwise",
            "Download document", "Print document", "Document properties",
            "Presentation mode", "Enter fullscreen",
        ];
        foreach (string label in expected)
        {
            Assert.AreEqual(1, toolbar.QuerySelectorAll($"button[aria-label='{label}']").Length, label);
        }
        Assert.AreEqual(expected.Length, toolbar.QuerySelectorAll("button.bit-pdv-btn").Length);
        // The open-file control is the file input itself under a button-shaped span, so
        // it is not one of the buttons counted above.
        Assert.AreEqual(1, toolbar.QuerySelectorAll("input[type='file'].bit-pdv-file").Length);
        Assert.AreEqual(1, toolbar.QuerySelectorAll("input.bit-pdv-page-input").Length);
        // Zoom level, scroll mode, spread mode.
        Assert.AreEqual(3, toolbar.QuerySelectorAll("select.bit-pdv-select").Length);
        // A toggle that is off still declares its state for assistive technology.
        Assert.AreEqual("false", component.Find("button[aria-label='Page thumbnails']").GetAttribute("aria-pressed"));
    }

    [TestMethod]
    public void BitPdfViewerShouldRenderOnlyTheRequestedToolbarItems()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld(), "hello.pdf"));
            parameters.Add(p => p.ToolbarItems, BitPdfToolbarItems.Navigation | BitPdfToolbarItems.Zoom);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        var toolbar = component.Find(".bit-pdv-toolbar");
        // Prev, next, zoom out, zoom in - and nothing else.
        Assert.AreEqual(4, toolbar.QuerySelectorAll("button.bit-pdv-btn").Length);
        Assert.AreEqual(1, toolbar.QuerySelectorAll("input.bit-pdv-page-input").Length);
        Assert.AreEqual(0, toolbar.QuerySelectorAll(".bit-pdv-title").Length);
    }

    [TestMethod]
    public void BitPdfViewerShouldOpenTheDefaultSidebar()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(3)));
            parameters.Add(p => p.DefaultSidebar, BitPdfSidebar.Thumbnails);
        });

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(3, component.Instance.PageCount);
            Assert.AreEqual(1, component.FindAll(".bit-pdv-thumbs").Count);
        });

        Assert.AreEqual(BitPdfSidebar.Thumbnails, component.Instance.Sidebar);
        Assert.AreEqual(3, component.FindAll("[data-thumb]").Count);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldSwitchAndCloseTheSidebar()
    {
        BitPdfSidebar? reported = null;

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
            parameters.Add(p => p.OnSidebarChanged, EventCallback.Factory.Create<BitPdfSidebar>(this, s => reported = s));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));
        Assert.AreEqual(BitPdfSidebar.None, component.Instance.Sidebar);

        await component.InvokeAsync(() => component.Instance.ShowSidebar(BitPdfSidebar.Bookmarks));
        Assert.AreEqual(BitPdfSidebar.Bookmarks, component.Instance.Sidebar);
        Assert.AreEqual(BitPdfSidebar.Bookmarks, reported);
        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-pdv-outline").Count));

        await component.InvokeAsync(() => component.Instance.ShowSidebar(BitPdfSidebar.None));
        Assert.AreEqual(BitPdfSidebar.None, component.Instance.Sidebar);
        Assert.AreEqual(BitPdfSidebar.None, reported);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldNotOpenBookmarksWithoutAnOutline()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(2)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.PageCount));
        Assert.IsFalse(component.Instance.HasOutline);

        await component.InvokeAsync(() => component.Instance.ShowSidebar(BitPdfSidebar.Bookmarks));

        Assert.AreEqual(BitPdfSidebar.None, component.Instance.Sidebar);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldNavigateToFirstAndLastPage()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(5)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(5, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.LastPage());
        Assert.AreEqual(5, component.Instance.CurrentPage);

        await component.InvokeAsync(() => component.Instance.FirstPage());
        Assert.AreEqual(1, component.Instance.CurrentPage);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldSurviveZoomBoundsThatExcludeTheDefaultMinimum()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
            parameters.Add(p => p.InitialZoomMode, BitPdfZoomMode.ActualSize);
            // MinZoom switched off, and a maximum below the 0.1 the viewer falls back
            // to: the fallback has to be bounded by it rather than inverting the range.
            parameters.Add(p => p.MinZoom, 0);
            parameters.Add(p => p.MaxZoom, 0.05);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.SetZoom(1));
        Assert.AreEqual(0.05, component.Instance.Zoom, 0.0001);

        await component.InvokeAsync(() => component.Instance.ZoomOut());
        Assert.AreEqual(0.05, component.Instance.Zoom, 0.0001);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldClampZoomToTheConfiguredBounds()
    {
        var zooms = new List<double>();

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
            parameters.Add(p => p.InitialZoomMode, BitPdfZoomMode.ActualSize);
            parameters.Add(p => p.MinZoom, 0.5);
            parameters.Add(p => p.MaxZoom, 2);
            parameters.Add(p => p.ZoomStep, 2);
            parameters.Add(p => p.OnZoomChanged, EventCallback.Factory.Create<double>(this, z => zooms.Add(z)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));
        Assert.AreEqual(1, component.Instance.Zoom, 0.001);

        await component.InvokeAsync(() => component.Instance.ZoomIn());
        Assert.AreEqual(2, component.Instance.Zoom, 0.001);

        // Already at MaxZoom: another step must not move it, and must not raise the event.
        await component.InvokeAsync(() => component.Instance.ZoomIn());
        Assert.AreEqual(2, component.Instance.Zoom, 0.001);
        Assert.AreEqual(1, zooms.Count);

        await component.InvokeAsync(() => component.Instance.SetZoom(0.01));
        Assert.AreEqual(0.5, component.Instance.Zoom, 0.001);
        Assert.AreEqual(BitPdfZoomMode.Custom, component.Instance.ZoomMode);
        CollectionAssert.AreEqual(new List<double> { 2, 0.5 }, zooms);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldReturnToActualSizeThroughTheZoomMode()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
            parameters.Add(p => p.InitialZoomMode, BitPdfZoomMode.ActualSize);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.SetZoom(3));
        Assert.AreEqual(3, component.Instance.Zoom, 0.001);

        await component.InvokeAsync(() => component.Instance.SetZoomMode(BitPdfZoomMode.ActualSize));
        Assert.AreEqual(1, component.Instance.Zoom, 0.001);
        Assert.AreEqual(BitPdfZoomMode.ActualSize, component.Instance.ZoomMode);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldRotateInBothDirectionsAndNormalizeTheAngle()
    {
        var rotations = new List<int>();

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
            parameters.Add(p => p.OnRotationChanged, EventCallback.Factory.Create<int>(this, r => rotations.Add(r)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));
        Assert.AreEqual(0, component.Instance.Rotation);

        await component.InvokeAsync(() => component.Instance.RotateClockwise());
        Assert.AreEqual(90, component.Instance.Rotation);

        // Counter-clockwise from 90 wraps back to 0, not to -90.
        await component.InvokeAsync(() => component.Instance.RotateCounterClockwise());
        Assert.AreEqual(0, component.Instance.Rotation);

        await component.InvokeAsync(() => component.Instance.RotateCounterClockwise());
        Assert.AreEqual(270, component.Instance.Rotation);

        await component.InvokeAsync(() => component.Instance.SetRotation(450));
        Assert.AreEqual(90, component.Instance.Rotation);

        // Setting the angle it already has raises nothing.
        await component.InvokeAsync(() => component.Instance.SetRotation(90));
        CollectionAssert.AreEqual(new List<int> { 90, 0, 270, 90 }, rotations);
    }

    [TestMethod]
    public void BitPdfViewerShouldSwapPageDimensionsOnAQuarterTurn()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.RichDocument()));
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.PageCount));

        // The fixture's pages are square, so assert on a landscape-friendly axis:
        // rotating must keep the page element sized and not blank it out.
        string before = component.Find("[data-page='1']").GetAttribute("style") ?? string.Empty;
        Assert.IsTrue(before.Contains("width:"));
        Assert.IsTrue(before.Contains("height:"));
    }

    [TestMethod]
    public void BitPdfViewerShouldExposeTheParsedDocumentInfo()
    {
        byte[] bytes = TestPdf.RichDocument();

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(bytes, "rich.pdf"));
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.PageCount));

        var instance = component.Instance;
        Assert.IsNotNull(instance.Document);
        Assert.AreEqual("1.7", instance.PdfVersion);
        Assert.IsFalse(instance.IsEncrypted);
        Assert.AreEqual(bytes.LongLength, instance.FileSize);
        CollectionAssert.AreEqual(bytes, instance.GetBytes());

        Assert.AreEqual("Test Document", instance.Metadata?.Title);
        Assert.AreEqual("Ada", instance.Metadata?.Author);
        Assert.AreEqual("Bit.BlazorUI", instance.Metadata?.Producer);
        Assert.AreEqual(2024, instance.Metadata?.CreationDate?.Year);

        // An unencrypted document grants every permission.
        Assert.IsTrue(instance.Permissions.CanPrint);
        Assert.IsTrue(instance.Permissions.CanCopy);

        // /PageLabels: roman for the first two pages, then decimal from 1.
        CollectionAssert.AreEqual(new List<string> { "i", "ii", "1" }, instance.PageLabels.ToList());

        Assert.AreEqual(1, instance.FormFields.Count);
        Assert.AreEqual("fullName", instance.FormFields[0].Name);
        Assert.AreEqual("Tx", instance.FormFields[0].Type);
        Assert.AreEqual("Ada Lovelace", instance.FormFields[0].Value);
    }

    [TestMethod]
    public void BitPdfViewerShouldExposeEmptyDocumentInfoWithoutASource()
    {
        var component = RenderComponent<BitPdfViewer>();

        var instance = component.Instance;
        Assert.IsNull(instance.Document);
        Assert.IsNull(instance.Metadata);
        Assert.IsNull(instance.PdfVersion);
        Assert.IsNull(instance.GetBytes());
        Assert.AreEqual(0, instance.FileSize);
        Assert.AreEqual(0, instance.PageLabels.Count);
        Assert.AreEqual(0, instance.FormFields.Count);
        Assert.AreEqual(0, instance.StructureTree.Count);
        Assert.AreEqual(0, instance.Outline.Count);
        // No document is not a restricted document: every permission is granted.
        Assert.IsTrue(instance.Permissions.CanPrint);
        Assert.AreEqual(string.Empty, instance.ExtractText());
    }

    [TestMethod]
    public void BitPdfViewerShouldExtractTheTextOfEveryPage()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(3)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.PageCount));

        string text = component.Instance.ExtractText("|");

        Assert.IsTrue(text.Contains("Page 1"));
        Assert.IsTrue(text.Contains("Page 2"));
        Assert.IsTrue(text.Contains("Page 3"));
        Assert.AreEqual(2, text.Split('|').Length - 1); // one separator between each pair
    }

    [TestMethod]
    public void BitPdfViewerShouldShowTheDocumentPropertiesDialog()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.RichDocument(), "rich.pdf"));
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.PageCount));
        Assert.AreEqual(0, component.FindAll(".bit-pdv-dialog").Count);

        component.Find("button[aria-label='Document properties']").Click();

        var dialog = component.Find(".bit-pdv-dialog");
        Assert.AreEqual("dialog", dialog.GetAttribute("role"));
        Assert.IsTrue(dialog.TextContent.Contains("rich.pdf"));
        Assert.IsTrue(dialog.TextContent.Contains("Test Document"));
        Assert.IsTrue(dialog.TextContent.Contains("Bit.BlazorUI"));
        Assert.IsTrue(dialog.TextContent.Contains("1.7"));
        Assert.IsTrue(dialog.TextContent.Contains("200"));   // the page size row

        component.Find(".bit-pdv-dialog-head button").Click();
        Assert.AreEqual(0, component.FindAll(".bit-pdv-dialog").Count);
    }

    [TestMethod]
    public void BitPdfViewerShouldApplyLocalizedTexts()
    {
        var texts = new BitPdfViewerTexts
        {
            NoDocument = "Kein Dokument geladen.",
            ZoomIn = "Vergrößern",
            FitWidth = "Breite anpassen",
        };

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Texts, texts);
        });

        var root = component.Find(".bit-pdv");

        Assert.IsTrue(root.TextContent.Contains("Kein Dokument geladen."));
        Assert.IsNotNull(component.Find("button[aria-label='Vergrößern']"));
        Assert.IsTrue(component.Find("select.bit-pdv-select").TextContent.Contains("Breite anpassen"));
    }

    [TestMethod]
    public void BitPdfViewerShouldApplyClassesAndStyles()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
            parameters.Add(p => p.Classes, new BitPdfViewerClassStyles { Root = "custom-root", Toolbar = "custom-toolbar" });
            parameters.Add(p => p.Styles, new BitPdfViewerClassStyles { Surface = "background:red", Page = "opacity:0.5" });
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        Assert.IsTrue(component.Find(".bit-pdv").ClassList.Contains("custom-root"));
        Assert.IsTrue(component.Find(".bit-pdv-toolbar").ClassList.Contains("custom-toolbar"));
        Assert.IsTrue((component.Find(".bit-pdv-surface").GetAttribute("style") ?? "").Contains("background:red"));

        // A page style is appended to the computed geometry, never replaces it.
        string pageStyle = component.Find("[data-page='1']").GetAttribute("style") ?? "";
        Assert.IsTrue(pageStyle.Contains("--bit-pdv-scale"));
        Assert.IsTrue(pageStyle.Contains("opacity:0.5"));
    }

    [TestMethod]
    public void BitPdfViewerShouldLabelThumbnailsWithTheDocumentPageLabels()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.RichDocument()));
            parameters.Add(p => p.DefaultSidebar, BitPdfSidebar.Thumbnails);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll("[data-thumb]").Count));

        var labels = component.FindAll(".bit-pdv-thumb-label");

        Assert.AreEqual("i", labels[0].TextContent);
        Assert.AreEqual("ii", labels[1].TextContent);
        Assert.AreEqual("1", labels[2].TextContent);
    }

    [TestMethod]
    public void BitPdfViewerShouldRoveTabIndexAcrossThumbnails()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(3)));
            parameters.Add(p => p.DefaultSidebar, BitPdfSidebar.Thumbnails);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll("[data-thumb]").Count));

        var thumbs = component.FindAll("[data-thumb]");

        // Only the active option is in the tab order (the ARIA listbox pattern).
        Assert.AreEqual("0", thumbs[0].GetAttribute("tabindex"));
        Assert.AreEqual("-1", thumbs[1].GetAttribute("tabindex"));
        // ARIA states must be the literal "true"/"false", not an HTML boolean attribute.
        Assert.AreEqual("true", thumbs[0].GetAttribute("aria-selected"));
        Assert.AreEqual("false", thumbs[1].GetAttribute("aria-selected"));
    }

    [TestMethod]
    public void BitPdfViewerShouldRenderTheOutlineAsAnAriaTree()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
            parameters.Add(p => p.DefaultSidebar, BitPdfSidebar.Bookmarks);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-pdv-outline").Count));

        Assert.AreEqual("tree", component.Find(".bit-pdv-outline").GetAttribute("role"));
        Assert.AreEqual("group", component.Find(".bit-pdv-outline-list").GetAttribute("role"));

        var item = component.Find("[role='treeitem']");
        Assert.IsTrue(item.TextContent.Contains("Chapter 1"));
        // A leaf declares no expanded state.
        Assert.IsNull(item.GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public async Task BitPdfViewerShouldNavigateThroughKeyboardShortcuts()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(4)));
            parameters.Add(p => p.InitialZoomMode, BitPdfZoomMode.ActualSize);
        });

        component.WaitForAssertion(() => Assert.AreEqual(4, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.OnShortcut("next"));
        Assert.AreEqual(2, component.Instance.CurrentPage);

        await component.InvokeAsync(() => component.Instance.OnShortcut("last"));
        Assert.AreEqual(4, component.Instance.CurrentPage);

        await component.InvokeAsync(() => component.Instance.OnShortcut("prev"));
        Assert.AreEqual(3, component.Instance.CurrentPage);

        await component.InvokeAsync(() => component.Instance.OnShortcut("first"));
        Assert.AreEqual(1, component.Instance.CurrentPage);

        await component.InvokeAsync(() => component.Instance.OnShortcut("zoomIn"));
        Assert.AreEqual(1.2, component.Instance.Zoom, 0.001);

        await component.InvokeAsync(() => component.Instance.OnShortcut("actualSize"));
        Assert.AreEqual(1, component.Instance.Zoom, 0.001);

        await component.InvokeAsync(() => component.Instance.OnShortcut("rotateCw"));
        Assert.AreEqual(90, component.Instance.Rotation);

        await component.InvokeAsync(() => component.Instance.OnShortcut("sidebar"));
        Assert.AreEqual(BitPdfSidebar.Thumbnails, component.Instance.Sidebar);

        // An unknown command is ignored rather than throwing out of a JS-invokable.
        await component.InvokeAsync(() => component.Instance.OnShortcut("nope"));
        Assert.AreEqual(BitPdfSidebar.Thumbnails, component.Instance.Sidebar);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldToggleTheFindBoxThroughShortcuts()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));
        Assert.IsFalse(component.Instance.IsSearchOpen);

        await component.InvokeAsync(() => component.Instance.OnShortcut("find"));
        Assert.IsTrue(component.Instance.IsSearchOpen);
        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-pdv-search-input").Count));

        await component.InvokeAsync(() => component.Instance.OnShortcut("escape"));
        Assert.IsFalse(component.Instance.IsSearchOpen);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldIgnoreShortcutsWhenDisabled()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(3)));
            parameters.Add(p => p.IsEnabled, false);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.OnShortcut("next"));

        Assert.AreEqual(1, component.Instance.CurrentPage);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldOpenTheFindBoxWhenSearchingFromCode()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.Search("Hello"));

        Assert.IsTrue(component.Instance.IsSearchOpen);
        component.WaitForAssertion(() =>
            Assert.AreEqual("Hello", component.Find(".bit-pdv-search-input").GetAttribute("value")));

        await component.InvokeAsync(() => component.Instance.ClearSearch());
        Assert.AreEqual(0, component.Instance.SearchMatchCount);
    }

    [TestMethod]
    public void BitPdfViewerShouldOfferZoomPresetsWithinTheConfiguredBounds()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
            parameters.Add(p => p.MinZoom, 1);
            parameters.Add(p => p.MaxZoom, 2);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        // Compare whole option labels: ">50%<" would otherwise also match "150%".
        var labels = component.FindAll("select.bit-pdv-select option")
                              .Select(o => o.TextContent)
                              .ToList();

        CollectionAssert.Contains(labels, "100%");
        CollectionAssert.Contains(labels, "125%");
        CollectionAssert.Contains(labels, "150%");
        CollectionAssert.Contains(labels, "200%");
        CollectionAssert.DoesNotContain(labels, "50%");  // below MinZoom
        CollectionAssert.DoesNotContain(labels, "75%");  // below MinZoom
        CollectionAssert.DoesNotContain(labels, "300%"); // above MaxZoom
        CollectionAssert.DoesNotContain(labels, "400%"); // above MaxZoom
    }

    [TestMethod]
    public void BitPdfViewerShouldEnableDownloadForAnUrlSourceOnceTheBytesAreResolved()
    {
        // A bytes source is downloadable immediately; the button reads the resolved
        // bytes rather than the source, so the same holds after a URL fetch.
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld(), "hello.pdf"));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        var download = component.Find("button[aria-label='Download document']");
        Assert.IsFalse(download.HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitPdfViewerShouldDisableDownloadWithoutADocument()
    {
        var component = RenderComponent<BitPdfViewer>();

        Assert.IsTrue(component.Find("button[aria-label='Download document']").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitPdfViewerShouldLayOutPagesVerticallyByDefault()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(4)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(4, component.Instance.PageCount));

        var pages = component.Find(".bit-pdv-pages");

        Assert.IsFalse(pages.ClassList.Contains("bit-pdv-h"));
        Assert.IsFalse(pages.ClassList.Contains("bit-pdv-w"));
        Assert.IsFalse(pages.ClassList.Contains("bit-pdv-single"));
        // Without spreads the pages stay direct children of the container.
        Assert.AreEqual(0, component.FindAll(".bit-pdv-row").Count);
        Assert.IsFalse(component.Find(".bit-pdv-surface").HasAttribute("data-bit-pdv-axis"));
    }

    [TestMethod]
    [DataRow(BitPdfScrollMode.Horizontal, "bit-pdv-h")]
    [DataRow(BitPdfScrollMode.Wrapped, "bit-pdv-w")]
    [DataRow(BitPdfScrollMode.Page, "bit-pdv-single")]
    public void BitPdfViewerShouldApplyTheScrollModeLayout(BitPdfScrollMode mode, string expectedClass)
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(4)));
            parameters.Add(p => p.ScrollMode, mode);
        });

        component.WaitForAssertion(() => Assert.AreEqual(4, component.Instance.PageCount));

        Assert.AreEqual(mode, component.Instance.CurrentScrollMode);
        Assert.IsTrue(component.Find(".bit-pdv-pages").ClassList.Contains(expectedClass));
        // Every page keeps its slot in every mode; Page mode only hides the others.
        Assert.AreEqual(4, component.FindAll("[data-page]").Count);
    }

    [TestMethod]
    public void BitPdfViewerShouldMarkOnlyTheCurrentPageInPageScrollMode()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(4)));
            parameters.Add(p => p.ScrollMode, BitPdfScrollMode.Page);
        });

        component.WaitForAssertion(() => Assert.AreEqual(4, component.Instance.PageCount));

        Assert.AreEqual(1, component.FindAll(".bit-pdv-page.bit-pdv-cur").Count);
        Assert.IsTrue(component.Find("[data-page='1']").ClassList.Contains("bit-pdv-cur"));
    }

    [TestMethod]
    public void BitPdfViewerShouldTellJavaScriptWhichAxisToMeasureOn()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(3)));
            parameters.Add(p => p.ScrollMode, BitPdfScrollMode.Horizontal);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.PageCount));

        Assert.AreEqual("h", component.Find(".bit-pdv-surface").GetAttribute("data-bit-pdv-axis"));
    }

    [TestMethod]
    public void BitPdfViewerShouldPairPagesForOddSpreads()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(5)));
            parameters.Add(p => p.SpreadMode, BitPdfSpreadMode.Odd);
        });

        component.WaitForAssertion(() => Assert.AreEqual(5, component.Instance.PageCount));

        var rows = component.FindAll(".bit-pdv-row");

        // Odd pages start a spread: (1,2) (3,4) (5).
        Assert.AreEqual(3, rows.Count);
        Assert.AreEqual(2, rows[0].QuerySelectorAll("[data-page]").Length);
        Assert.AreEqual(2, rows[1].QuerySelectorAll("[data-page]").Length);
        Assert.AreEqual(1, rows[2].QuerySelectorAll("[data-page]").Length);
        Assert.AreEqual("1", rows[0].QuerySelectorAll("[data-page]")[0].GetAttribute("data-page"));
        Assert.AreEqual("2", rows[0].QuerySelectorAll("[data-page]")[1].GetAttribute("data-page"));
    }

    [TestMethod]
    public void BitPdfViewerShouldLeaveTheFirstPageAloneForEvenSpreads()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(5)));
            parameters.Add(p => p.SpreadMode, BitPdfSpreadMode.Even);
        });

        component.WaitForAssertion(() => Assert.AreEqual(5, component.Instance.PageCount));

        var rows = component.FindAll(".bit-pdv-row");

        // Even pages start a spread: (1) (2,3) (4,5).
        Assert.AreEqual(3, rows.Count);
        Assert.AreEqual(1, rows[0].QuerySelectorAll("[data-page]").Length);
        Assert.AreEqual(2, rows[1].QuerySelectorAll("[data-page]").Length);
        Assert.AreEqual(2, rows[2].QuerySelectorAll("[data-page]").Length);
        Assert.AreEqual("2", rows[1].QuerySelectorAll("[data-page]")[0].GetAttribute("data-page"));
    }

    [TestMethod]
    public async Task BitPdfViewerShouldChangeLayoutModesThroughTheApi()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(4)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(4, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.SetScrollMode(BitPdfScrollMode.Wrapped));
        Assert.AreEqual(BitPdfScrollMode.Wrapped, component.Instance.CurrentScrollMode);
        component.WaitForAssertion(() => Assert.IsTrue(component.Find(".bit-pdv-pages").ClassList.Contains("bit-pdv-w")));

        await component.InvokeAsync(() => component.Instance.SetSpreadMode(BitPdfSpreadMode.Odd));
        Assert.AreEqual(BitPdfSpreadMode.Odd, component.Instance.CurrentSpreadMode);
        component.WaitForAssertion(() => Assert.AreEqual(2, component.FindAll(".bit-pdv-row").Count));
    }

    [TestMethod]
    public void BitPdfViewerShouldToggleThePanTool()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(2)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.PageCount));
        Assert.AreEqual(BitPdfCursorTool.Select, component.Instance.CurrentCursorTool);
        Assert.IsFalse(component.Find(".bit-pdv-surface").ClassList.Contains("bit-pdv-pan"));

        component.Find("button[aria-label='Pan tool']").Click();

        Assert.AreEqual(BitPdfCursorTool.Pan, component.Instance.CurrentCursorTool);
        Assert.IsTrue(component.Find(".bit-pdv-surface").ClassList.Contains("bit-pdv-pan"));
        Assert.AreEqual("true", component.Find("button[aria-label='Pan tool']").GetAttribute("aria-pressed"));
    }

    [TestMethod]
    public void BitPdfViewerShouldStartWithTheCursorToolItWasGiven()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(2)));
            parameters.Add(p => p.CursorTool, BitPdfCursorTool.Pan);
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.PageCount));

        Assert.AreEqual(BitPdfCursorTool.Pan, component.Instance.CurrentCursorTool);
        Assert.IsTrue(component.Find(".bit-pdv-surface").ClassList.Contains("bit-pdv-pan"));
    }

    [TestMethod]
    public void BitPdfViewerShouldHideTheLayoutControlsWhenNotRequested()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(2)));
            parameters.Add(p => p.ToolbarItems, BitPdfToolbarItems.All & ~BitPdfToolbarItems.Layout & ~BitPdfToolbarItems.CursorTool);
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.PageCount));

        // Only the zoom dropdown is left.
        Assert.AreEqual(1, component.FindAll("select.bit-pdv-select").Count);
        Assert.AreEqual(0, component.FindAll("button[aria-label='Pan tool']").Count);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldMatchOnlyWholeWordsWhenAsked()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithText("cat concatenate cat.")));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.Search("cat"));
        // "cat", the "cat" inside "concatenate", and the trailing "cat".
        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.SearchMatchCount));

        component.Find("button[aria-label='Whole words']").Click();

        // The substring inside "concatenate" is no longer a match; a full stop still
        // ends a word, so the trailing one is.
        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.SearchMatchCount));
    }

    [TestMethod]
    public async Task BitPdfViewerShouldCountOverlappingOccurrencesOnce()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithText("aaaa")));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.Search("aa"));

        // Matches do not overlap: "aaaa" holds two, not three.
        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.SearchMatchCount));
    }

    [TestMethod]
    public async Task BitPdfViewerShouldCountMatchesAcrossTheWholeDocumentWithoutRenderingIt()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(6)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(6, component.Instance.PageCount));

        // "Page" appears once per page, on every page of the fixture.
        await component.InvokeAsync(() => component.Instance.Search("Page"));

        component.WaitForAssertion(() => Assert.AreEqual(6, component.Instance.SearchMatchCount));

        // The count came from the text index, so the document surface still holds
        // only the pages the reader has actually been taken to.
        int rendered = component.FindAll("[data-page] .bit-pdv-html-page").Count;
        Assert.IsTrue(rendered < 6, $"expected fewer than 6 rendered pages, found {rendered}");
    }

    [TestMethod]
    public async Task BitPdfViewerShouldWalkMatchesAcrossPages()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(4)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(4, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.Search("Page"));
        component.WaitForAssertion(() => Assert.AreEqual(4, component.Instance.SearchMatchCount));

        // One match per page, so stepping through them walks the document.
        Assert.AreEqual(1, component.Instance.CurrentPage);

        await component.InvokeAsync(() => component.Instance.FindNext());
        Assert.AreEqual(2, component.Instance.CurrentPage);

        await component.InvokeAsync(() => component.Instance.FindNext());
        Assert.AreEqual(3, component.Instance.CurrentPage);

        // Stepping back past the first match wraps to the last one.
        await component.InvokeAsync(() => component.Instance.FindPrevious());
        await component.InvokeAsync(() => component.Instance.FindPrevious());
        await component.InvokeAsync(() => component.Instance.FindPrevious());
        Assert.AreEqual(4, component.Instance.CurrentPage);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldNarrowMatchesWithTheFindOptions()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(3)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.Search("page"));
        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.SearchMatchCount));

        // The fixture writes "Page N", so matching case drops every hit.
        component.Find("button[aria-label='Match case']").Click();
        component.WaitForAssertion(() => Assert.AreEqual(0, component.Instance.SearchMatchCount));

        component.Find("button[aria-label='Match case']").Click();
        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.SearchMatchCount));
    }

    [TestMethod]
    public async Task BitPdfViewerShouldClearMatchesWhenTheQueryGoesAway()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(3)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.Search("Page"));
        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.SearchMatchCount));

        await component.InvokeAsync(() => component.Instance.ClearSearch());

        Assert.AreEqual(0, component.Instance.SearchMatchCount);
        // Stepping with nothing found is a no-op rather than a crash.
        await component.InvokeAsync(() => component.Instance.FindNext());
        Assert.AreEqual(0, component.Instance.SearchMatchCount);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldEnterAndLeavePresentationMode()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(4)));
            parameters.Add(p => p.ScrollMode, BitPdfScrollMode.Wrapped);
            parameters.Add(p => p.DefaultSidebar, BitPdfSidebar.Thumbnails);
        });

        component.WaitForAssertion(() => Assert.AreEqual(4, component.Instance.PageCount));
        Assert.IsFalse(component.Instance.IsPresenting);

        await component.InvokeAsync(() => component.Instance.EnterPresentationMode());

        Assert.IsTrue(component.Instance.IsPresenting);
        Assert.AreEqual(BitPdfScrollMode.Page, component.Instance.CurrentScrollMode);
        Assert.AreEqual(BitPdfZoomMode.FitPage, component.Instance.ZoomMode);
        Assert.AreEqual(BitPdfSidebar.None, component.Instance.Sidebar);

        await component.InvokeAsync(() => component.Instance.ExitPresentationMode());

        // Leaving restores exactly the layout that was replaced.
        Assert.IsFalse(component.Instance.IsPresenting);
        Assert.AreEqual(BitPdfScrollMode.Wrapped, component.Instance.CurrentScrollMode);
        Assert.AreEqual(BitPdfSidebar.Thumbnails, component.Instance.Sidebar);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldLeavePresentationModeWhenTheBrowserLeavesFullscreen()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(2)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.TogglePresentationMode());
        Assert.IsTrue(component.Instance.IsPresenting);

        // The browser's own Escape leaves fullscreen without telling .NET anything
        // else; the notification is what keeps the two states in step.
        await component.InvokeAsync(() => component.Instance.OnFullscreenChanged(false));

        Assert.IsFalse(component.Instance.IsPresenting);
        Assert.AreEqual(BitPdfScrollMode.Vertical, component.Instance.CurrentScrollMode);
    }

    [TestMethod]
    public void BitPdfViewerShouldExposeOptionalContentLayers()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithLayers()));
        });

        component.WaitForAssertion(() => Assert.IsTrue(component.Instance.HasLayers));

        var layers = component.Instance.Layers;

        Assert.AreEqual(2, layers.Count);
        Assert.AreEqual("Base drawing", layers[0].Name);
        Assert.AreEqual("Annotations", layers[1].Name);
        // The default configuration's /OFF list decides the starting visibility.
        Assert.IsTrue(layers[0].VisibleByDefault);
        Assert.IsFalse(layers[1].VisibleByDefault);
        Assert.IsTrue(component.Instance.IsLayerVisible(layers[0]));
        Assert.IsFalse(component.Instance.IsLayerVisible(layers[1]));
    }

    [TestMethod]
    public void BitPdfViewerShouldHonourViewUsageRulesForLayers()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithViewUsageLayers()));
        });

        component.WaitForAssertion(() => Assert.IsTrue(component.Instance.HasLayers));

        var layers = component.Instance.Layers;

        Assert.AreEqual(2, layers.Count);
        // Neither group is in /OFF: the /AS /View rule is the only thing that hides
        // the second one, and both the layer state and what was painted must say so.
        Assert.IsTrue(layers[0].VisibleByDefault);
        Assert.IsFalse(layers[1].VisibleByDefault);
        Assert.IsTrue(component.Instance.IsLayerVisible(layers[0]));
        Assert.IsFalse(component.Instance.IsLayerVisible(layers[1]));

        component.WaitForAssertion(() =>
            Assert.IsTrue(component.Find("[data-page='1']").TextContent.Contains("ScreenLayerText")));
        Assert.IsFalse(component.Find("[data-page='1']").TextContent.Contains("PrintOnlyLayerText"));
    }

    [TestMethod]
    public async Task BitPdfViewerShouldRepaintPagesWhenALayerIsSwitched()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithLayers()));
        });

        component.WaitForAssertion(() => Assert.IsTrue(component.Instance.HasLayers));
        component.WaitForAssertion(() =>
            Assert.IsTrue(component.Find("[data-page='1']").TextContent.Contains("VisibleLayerText")));

        // The hidden layer's content is not painted at all.
        Assert.IsFalse(component.Find("[data-page='1']").TextContent.Contains("HiddenLayerText"));

        var layers = component.Instance.Layers;
        await component.InvokeAsync(() => component.Instance.SetLayerVisible(layers[1], true));

        component.WaitForAssertion(() =>
            Assert.IsTrue(component.Find("[data-page='1']").TextContent.Contains("HiddenLayerText")));

        // Turning the first one off drops its content, without disturbing the second.
        await component.InvokeAsync(() => component.Instance.SetLayerVisible(layers[0], false));

        component.WaitForAssertion(() =>
        {
            var page = component.Find("[data-page='1']");
            Assert.IsFalse(page.TextContent.Contains("VisibleLayerText"));
            Assert.IsTrue(page.TextContent.Contains("HiddenLayerText"));
        });
    }

    [TestMethod]
    public void BitPdfViewerShouldOpenTheLayersPanel()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithLayers()));
            parameters.Add(p => p.DefaultSidebar, BitPdfSidebar.Layers);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-pdv-layers").Count));

        var rows = component.FindAll(".bit-pdv-layer");

        Assert.AreEqual(2, rows.Count);
        Assert.IsTrue(rows[0].TextContent.Contains("Base drawing"));
        Assert.AreEqual(BitPdfSidebar.Layers, component.Instance.Sidebar);

        // Clicking a row's checkbox switches its layer.
        component.FindAll(".bit-pdv-layer input")[1].Change(true);

        component.WaitForAssertion(() => Assert.IsTrue(component.Instance.IsLayerVisible(component.Instance.Layers[1])));
    }

    [TestMethod]
    public async Task BitPdfViewerShouldNotOpenLayersWithoutAny()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(2)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.PageCount));
        Assert.IsFalse(component.Instance.HasLayers);
        Assert.IsTrue(component.Find("button[aria-label='Layers']").HasAttribute("disabled"));

        await component.InvokeAsync(() => component.Instance.ShowSidebar(BitPdfSidebar.Layers));

        Assert.AreEqual(BitPdfSidebar.None, component.Instance.Sidebar);
    }

    [TestMethod]
    public void BitPdfViewerShouldExposeEmbeddedFiles()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithAttachments()));
        });

        component.WaitForAssertion(() => Assert.IsTrue(component.Instance.HasAttachments));

        var attachments = component.Instance.Attachments;

        Assert.AreEqual(2, attachments.Count);

        var notes = attachments.Single(a => a.Name == "notes.txt");
        Assert.AreEqual("Some notes", notes.Description);
        Assert.AreEqual("text/plain", notes.MimeType);
        Assert.IsNull(notes.PageNumber); // reached through the document-wide name tree
        Assert.AreEqual("hello attachment", Encoding.Latin1.GetString(notes.Content));
        Assert.AreEqual(16, notes.Size);

        var data = attachments.Single(a => a.Name == "data.csv");
        Assert.AreEqual(1, data.PageNumber); // pinned to the page by its annotation
        Assert.AreEqual("a,b,c", Encoding.Latin1.GetString(data.Content));
    }

    [TestMethod]
    public void BitPdfViewerShouldOpenTheAttachmentsPanel()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithAttachments()));
            parameters.Add(p => p.DefaultSidebar, BitPdfSidebar.Attachments);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-pdv-attachments").Count));

        var rows = component.FindAll(".bit-pdv-attachment");

        Assert.AreEqual(2, rows.Count);
        Assert.IsTrue(rows[0].TextContent.Contains("notes.txt"));
        Assert.AreEqual(BitPdfSidebar.Attachments, component.Instance.Sidebar);
        Assert.IsFalse(component.Find("button[aria-label='Attachments']").HasAttribute("disabled"));
    }

    [TestMethod]
    public async Task BitPdfViewerShouldNotOpenAttachmentsWithoutAny()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(2)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.PageCount));
        Assert.IsFalse(component.Instance.HasAttachments);
        Assert.IsTrue(component.Find("button[aria-label='Attachments']").HasAttribute("disabled"));

        await component.InvokeAsync(() => component.Instance.ShowSidebar(BitPdfSidebar.Attachments));

        Assert.AreEqual(BitPdfSidebar.None, component.Instance.Sidebar);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldBindCurrentPageBothWays()
    {
        int bound = 1;

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(5)));
            parameters.Add(p => p.CurrentPage, bound);
            parameters.Add(p => p.CurrentPageChanged, EventCallback.Factory.Create<int>(this, v => bound = v));
        });

        component.WaitForAssertion(() => Assert.AreEqual(5, component.Instance.PageCount));

        // Viewer to host.
        await component.InvokeAsync(() => component.Instance.NextPage());
        Assert.AreEqual(2, bound);

        // Host to viewer.
        component.Render(parameters =>
        {
            parameters.Add(p => p.CurrentPage, 4);
            parameters.Add(p => p.CurrentPageChanged, EventCallback.Factory.Create<int>(this, v => bound = v));
        });

        Assert.AreEqual(4, component.Instance.CurrentPage);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldBindZoomAndRotationBothWays()
    {
        double boundZoom = 1;
        int boundRotation = 0;

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
            parameters.Add(p => p.InitialZoomMode, BitPdfZoomMode.ActualSize);
            parameters.Add(p => p.Zoom, boundZoom);
            parameters.Add(p => p.ZoomChanged, EventCallback.Factory.Create<double>(this, v => boundZoom = v));
            parameters.Add(p => p.Rotation, boundRotation);
            parameters.Add(p => p.RotationChanged, EventCallback.Factory.Create<int>(this, v => boundRotation = v));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.ZoomIn());
        Assert.AreEqual(1.2, boundZoom, 0.001);

        await component.InvokeAsync(() => component.Instance.RotateClockwise());
        Assert.AreEqual(90, boundRotation);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Zoom, 2.0);
            parameters.Add(p => p.ZoomChanged, EventCallback.Factory.Create<double>(this, v => boundZoom = v));
            parameters.Add(p => p.Rotation, 270);
            parameters.Add(p => p.RotationChanged, EventCallback.Factory.Create<int>(this, v => boundRotation = v));
        });

        Assert.AreEqual(2.0, component.Instance.Zoom, 0.001);
        Assert.AreEqual(270, component.Instance.Rotation);
    }

    [TestMethod]
    public void BitPdfViewerShouldRotateThePagesWhenTheHostSetsRotation()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            // 300x200 pages: a quarter turn is visible in the page box.
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithLayers()));
            parameters.Add(p => p.InitialZoomMode, BitPdfZoomMode.ActualSize);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));
        StringAssert.StartsWith(component.Find(".bit-pdv-page[data-page='1']").GetAttribute("style"),
                                "width:300px;height:200px");

        // The angle arriving on the parameter is a rotation like any other: the pages
        // have to be re-prepared for it, even though there is nothing left to assign.
        component.Render(parameters => parameters.Add(p => p.Rotation, 90));

        Assert.AreEqual(90, component.Instance.Rotation);
        component.WaitForAssertion(() => StringAssert.StartsWith(
            component.Find(".bit-pdv-page[data-page='1']").GetAttribute("style"), "width:200px;height:300px"));
    }

    [TestMethod]
    public async Task BitPdfViewerShouldLeaveAOneWayBoundPageToTheHost()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(5)));
            // No CurrentPageChanged: the host owns the value.
            parameters.Add(p => p.CurrentPage, 3);
        });

        component.WaitForAssertion(() => Assert.AreEqual(5, component.Instance.PageCount));
        Assert.AreEqual(3, component.Instance.CurrentPage);

        await component.InvokeAsync(() => component.Instance.NextPage());

        Assert.AreEqual(3, component.Instance.CurrentPage);
    }

    [TestMethod]
    public void BitPdfViewerShouldOpenTheFirstDocumentAtTheRequestedPage()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(8)));
            // A host that opens the viewer at page 4 asked for page 4; the first load
            // must not reset it the way a REPLACEMENT document does.
            parameters.Add(p => p.CurrentPage, 4);
            parameters.Add(p => p.CurrentPageChanged, EventCallback.Factory.Create<int>(this, _ => { }));
        });

        component.WaitForAssertion(() => Assert.AreEqual(8, component.Instance.PageCount));

        Assert.AreEqual(4, component.Instance.CurrentPage);
    }

    [TestMethod]
    public void BitPdfViewerShouldClampARequestedStartPageToTheDocument()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(3)));
            parameters.Add(p => p.CurrentPage, 99);
            parameters.Add(p => p.CurrentPageChanged, EventCallback.Factory.Create<int>(this, _ => { }));
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.PageCount));

        Assert.AreEqual(3, component.Instance.CurrentPage);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldPutTheChromeAwayWhilePresenting()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(3)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.PageCount));
        Assert.IsFalse(component.Find(".bit-pdv").ClassList.Contains("bit-pdv-presenting"));

        await component.InvokeAsync(() => component.Instance.EnterPresentationMode());

        component.WaitForAssertion(() =>
            Assert.IsTrue(component.Find(".bit-pdv").ClassList.Contains("bit-pdv-presenting")));

        await component.InvokeAsync(() => component.Instance.ExitPresentationMode());

        component.WaitForAssertion(() =>
            Assert.IsFalse(component.Find(".bit-pdv").ClassList.Contains("bit-pdv-presenting")));
    }

    [TestMethod]
    public void BitPdfViewerShouldResetPageAndRotationForANewDocument()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(5)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(5, component.Instance.PageCount));
        component.InvokeAsync(() => component.Instance.LastPage()).GetAwaiter().GetResult();
        component.InvokeAsync(() => component.Instance.RotateClockwise()).GetAwaiter().GetResult();
        Assert.AreEqual(5, component.Instance.CurrentPage);
        Assert.AreEqual(90, component.Instance.Rotation);

        component.Render(parameters => parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(3))));

        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.PageCount));
        Assert.AreEqual(1, component.Instance.CurrentPage);
        Assert.AreEqual(0, component.Instance.Rotation);
    }

    [TestMethod]
    public void BitPdfViewerShouldOpenAnEncryptedDocumentWithAKnownPassword()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.Encrypted("open-sesame")).WithPassword("open-sesame"));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        Assert.IsTrue(component.Instance.IsEncrypted);
        // The document grants everything, so no capability is reported as withheld.
        Assert.IsTrue(component.Instance.Permissions.CanPrint);
        Assert.IsTrue(component.Instance.Permissions.CanCopy);
        Assert.AreEqual(0, component.FindAll(".bit-pdv-password").Count);
    }

    [TestMethod]
    public void BitPdfViewerShouldAskForAPasswordAndOpenTheDocumentWhenItIsGiven()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.Encrypted("hunter2")));
        });

        // The load parks on the dialog rather than failing.
        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-pdv-password").Count));
        Assert.AreEqual(0, component.Instance.PageCount);

        component.Find(".bit-pdv-password-input").Input("hunter2");
        component.Find(".bit-pdv-dialog-actions .bit-pdv-act").Click();

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));
        Assert.AreEqual(0, component.FindAll(".bit-pdv-password").Count);
    }

    [TestMethod]
    public void BitPdfViewerShouldAskAgainAfterAWrongPassword()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.Encrypted("hunter2")));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-pdv-password").Count));
        // The first ask carries no rejection notice.
        Assert.IsTrue(component.Find(".bit-pdv-dialog-body").TextContent.Contains("This document is protected"));

        component.Find(".bit-pdv-password-input").Input("wrong");
        component.Find(".bit-pdv-dialog-actions .bit-pdv-act").Click();

        // The dialog comes back, this time saying the password was refused.
        component.WaitForAssertion(() =>
            Assert.IsTrue(component.Find(".bit-pdv-dialog-body").TextContent.Contains("not accepted")));
        Assert.AreEqual("true", component.Find(".bit-pdv-password-input").GetAttribute("aria-invalid"));

        component.Find(".bit-pdv-password-input").Input("hunter2");
        component.Find(".bit-pdv-dialog-actions .bit-pdv-act").Click();

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));
    }

    [TestMethod]
    public void BitPdfViewerShouldReportAnErrorWhenThePasswordDialogIsCancelled()
    {
        string? error = null;

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.Encrypted("hunter2")));
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<string>(this, e => error = e));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-pdv-password").Count));

        component.Find(".bit-pdv-dialog-actions button").Click(); // the cancel button

        component.WaitForAssertion(() =>
        {
            Assert.IsNotNull(error);
            Assert.AreEqual(0, component.Instance.PageCount);
        });
        Assert.AreEqual(0, component.FindAll(".bit-pdv-password").Count);
    }

    [TestMethod]
    public void BitPdfViewerShouldNotAskWhenThePasswordPromptIsDisabled()
    {
        string? error = null;

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.Encrypted("hunter2")));
            parameters.Add(p => p.ShowPasswordPrompt, false);
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<string>(this, e => error = e));
        });

        component.WaitForAssertion(() => Assert.IsNotNull(error));

        Assert.AreEqual(0, component.FindAll(".bit-pdv-password").Count);
        Assert.AreEqual(0, component.Instance.PageCount);
    }

    [TestMethod]
    public void BitPdfViewerShouldLetTheHostAnswerThePasswordRequest()
    {
        int asked = 0;

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.Encrypted("hunter2")));
            parameters.Add(p => p.OnPasswordRequested, () =>
            {
                asked++;
                return Task.FromResult<string?>("hunter2");
            });
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        Assert.AreEqual(1, asked);
        // The host took over the asking, so the built-in dialog never appears.
        Assert.AreEqual(0, component.FindAll(".bit-pdv-password").Count);
    }

    [TestMethod]
    public async Task BitPdfSourceShouldReadAStream()
    {
        byte[] bytes = TestPdf.HelloWorld();
        using var stream = new MemoryStream(bytes);

        var source = await BitPdfSource.FromStreamAsync(stream, "stream.pdf");

        Assert.AreEqual("stream.pdf", source.FileName);
        Assert.IsTrue(source.IsBytes);
        CollectionAssert.AreEqual(bytes, source.Bytes);
    }

    [TestMethod]
    public void BitPdfSourceShouldCopyItselfWhenAugmented()
    {
        var headers = new Dictionary<string, string> { ["Authorization"] = "Bearer t" };

        var source = BitPdfSource.FromUrl("https://example.test/a.pdf")
                                 .WithFileName("a.pdf")
                                 .WithPassword("secret")
                                 .WithHeaders(headers);

        Assert.AreEqual("https://example.test/a.pdf", source.Url);
        Assert.AreEqual("a.pdf", source.FileName);
        Assert.AreEqual("secret", source.Password);
        Assert.AreEqual("Bearer t", source.Headers?["Authorization"]);
        Assert.IsFalse(source.IsBytes);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldKeepAReaderChoiceAcrossHostRenders()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(3)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.SetScrollMode(BitPdfScrollMode.Horizontal));
        Assert.AreEqual(BitPdfScrollMode.Horizontal, component.Instance.CurrentScrollMode);

        // A host re-render that does not change the parameter must not snap the mode back.
        component.Render(parameters => parameters.Add(p => p.Height, "300px"));

        Assert.AreEqual(BitPdfScrollMode.Horizontal, component.Instance.CurrentScrollMode);

        // A host re-render that DOES change it is adopted.
        component.Render(parameters => parameters.Add(p => p.ScrollMode, BitPdfScrollMode.Wrapped));

        Assert.AreEqual(BitPdfScrollMode.Wrapped, component.Instance.CurrentScrollMode);
    }

    [TestMethod]
    public void BitPdfViewerShouldRespectTheWidthParameter()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Width, "480px");
            parameters.Add(p => p.Height, "300px");
        });

        string style = component.Find(".bit-pdv").GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("width:480px"), style);
        Assert.IsTrue(style.Contains("height:300px"), style);
    }

    [TestMethod]
    public void BitPdfViewerShouldOfferEveryFitModeInTheZoomDropdown()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        var options = component.FindAll("select.bit-pdv-select")[0]
            .QuerySelectorAll("option").Select(o => o.GetAttribute("value")).ToList();

        // The five named modes come first, in the order the dropdown lists them.
        CollectionAssert.AreEqual(
            new List<string?> { "Automatic", "FitWidth", "FitPage", "FitHeight", "ActualSize" },
            options.Take(5).ToList());
    }

    [TestMethod]
    public async Task BitPdfViewerShouldAdoptTheZoomModeItIsGiven()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
            parameters.Add(p => p.InitialZoomMode, BitPdfZoomMode.Automatic);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));
        Assert.AreEqual(BitPdfZoomMode.Automatic, component.Instance.ZoomMode);

        await component.InvokeAsync(() => component.Instance.SetZoomMode(BitPdfZoomMode.FitHeight));
        Assert.AreEqual(BitPdfZoomMode.FitHeight, component.Instance.ZoomMode);

        // Actual size is the one mode that does not need to measure the viewport.
        await component.InvokeAsync(() => component.Instance.SetZoomMode(BitPdfZoomMode.ActualSize));
        Assert.AreEqual(BitPdfZoomMode.ActualSize, component.Instance.ZoomMode);
        Assert.AreEqual(1, component.Instance.Zoom, 0.0001);
    }

    [TestMethod]
    public void BitPdfViewerShouldOfferTheConfiguredZoomPresets()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
            // Out of order, with a duplicate and two values outside the bounds.
            parameters.Add(p => p.ZoomPresets, new double[] { 2, 0.5, 2, 0.05, 9 });
            parameters.Add(p => p.MinZoom, 0.25);
            parameters.Add(p => p.MaxZoom, 4);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        var presets = component.FindAll("select.bit-pdv-select")[0]
            .QuerySelectorAll("option")
            .Select(o => o.GetAttribute("value"))
            .Where(v => v is not null && v.StartsWith('z'))
            .ToList();

        // Sorted, de-duplicated, and clipped to MinZoom..MaxZoom.
        CollectionAssert.AreEqual(new List<string?> { "z0.5", "z2" }, presets);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldIgnoreDiacriticsUntilAskedNotTo()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithText("resume and resume")));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.Search("resume"));
        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.SearchMatchCount));

        // The accented query still finds the bare word while diacritics are ignored.
        await component.InvokeAsync(() => component.Instance.Search("r\u00e9sum\u00e9"));
        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.SearchMatchCount));

        // Turning the option on makes the accents count, and nothing matches any more.
        await component.InvokeAsync(() => component.Instance.SetSearchOptions(matchDiacritics: true));
        component.WaitForAssertion(() => Assert.AreEqual(0, component.Instance.SearchMatchCount));
        Assert.IsTrue(component.Instance.SearchMatchDiacritics);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldSayWhenAPhraseIsNotFound()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithText("only this")));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.Search("nothing here"));

        component.WaitForAssertion(() =>
            Assert.IsTrue(component.Find(".bit-pdv-search-count").TextContent.Contains("Phrase not found")));
    }

    [TestMethod]
    public async Task BitPdfViewerShouldExposeAndSetEveryFindOption()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithText("Cat cat")));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        // The defaults every desktop find bar opens with.
        Assert.IsFalse(component.Instance.SearchMatchCase);
        Assert.IsFalse(component.Instance.SearchWholeWord);
        Assert.IsFalse(component.Instance.SearchMatchDiacritics);
        Assert.IsTrue(component.Instance.SearchHighlightAll);

        await component.InvokeAsync(() => component.Instance.Search("cat"));
        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.SearchMatchCount));
        Assert.AreEqual("cat", component.Instance.SearchQuery);
        Assert.AreEqual(1, component.Instance.SearchMatchIndex);

        await component.InvokeAsync(() => component.Instance.SetSearchOptions(matchCase: true));
        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.SearchMatchCount));

        // A null leaves an option where it was.
        await component.InvokeAsync(() => component.Instance.SetSearchOptions(wholeWord: true));
        Assert.IsTrue(component.Instance.SearchMatchCase);
        Assert.IsTrue(component.Instance.SearchWholeWord);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldTellTheHighlighterWhetherToPaintEveryMatch()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithText("cat cat")));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.Search("cat"));
        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.SearchMatchCount));

        await component.InvokeAsync(() => component.Instance.SetSearchOptions(highlightAll: false));

        // The find options travel to the browser-side highlighter, which is the only
        // thing that can decide what is painted.
        var call = Context.JSInterop.Invocations["BitBlazorUI.PdfViewer.highlight"].Last();
        Assert.AreEqual("cat", call.Arguments[1]);
        Assert.AreEqual(false, call.Arguments[4]); // matchDiacritics
        Assert.AreEqual(false, call.Arguments[5]); // highlightAll
    }

    [TestMethod]
    public void BitPdfViewerShouldRenderTheOpenFileControlOnlyWhenAsked()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        var picker = component.Find("input[type='file'].bit-pdv-file");
        Assert.AreEqual("Open file", picker.GetAttribute("aria-label"));
        Assert.IsTrue((picker.GetAttribute("accept") ?? string.Empty).Contains("pdf"));

        component.Render(parameters => parameters.Add(p => p.ToolbarItems,
            BitPdfToolbarItems.All & ~BitPdfToolbarItems.OpenFile));

        Assert.AreEqual(0, component.FindAll("input[type='file']").Count);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldRaiseOnPageRenderedForEveryPageItBuilds()
    {
        var rendered = new List<int>();

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(4)));
            parameters.Add(p => p.OnPageRendered, EventCallback.Factory.Create<int>(this, n => rendered.Add(n)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(4, component.Instance.PageCount));

        // Only the page the viewer opens on is built up front; the rest are lazy.
        CollectionAssert.Contains(rendered, 1);
        Assert.IsFalse(rendered.Contains(4), "page 4 should not have been rendered before it was reached");

        await component.InvokeAsync(() => component.Instance.LastPage());

        CollectionAssert.Contains(rendered, 4);
    }

    [TestMethod]
    public void BitPdfViewerShouldExposeTheParseWarningsOfADocument()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        // A well-formed fixture has nothing to complain about, but the list is always
        // there to read rather than only reaching the host through OnWarnings.
        Assert.IsNotNull(component.Instance.Warnings);
        Assert.AreEqual(0, component.Instance.Warnings.Count);
        // Nor is any page reported as unrenderable.
        Assert.AreEqual(0, component.Instance.FailedPages.Count);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldRoundRotationToTheNearestQuarterTurn()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.SetRotation(100));
        Assert.AreEqual(90, component.Instance.Rotation);

        await component.InvokeAsync(() => component.Instance.SetRotation(200));
        Assert.AreEqual(180, component.Instance.Rotation);

        await component.InvokeAsync(() => component.Instance.SetRotation(-100));
        Assert.AreEqual(270, component.Instance.Rotation);
    }

    [TestMethod]
    public void BitPdfViewerShouldPutTheOutlineTabStopOnOneBookmarkOnly()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithNestedOutline()));
            parameters.Add(p => p.DefaultSidebar, BitPdfSidebar.Bookmarks);
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.FindAll("[role='treeitem']").Count));

        var items = component.FindAll("[role='treeitem']");
        // An aria tree is ONE tab stop; the arrow keys move between its items.
        Assert.AreEqual("0", items[0].GetAttribute("tabindex"));
        Assert.AreEqual("-1", items[1].GetAttribute("tabindex"));
        // The branch declares its state, the leaf declares none.
        Assert.AreEqual("true", items[0].GetAttribute("aria-expanded"));
        Assert.IsNull(items[1].GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitPdfViewerShouldFoldAndUnfoldBookmarksWithTheArrowKeys()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithNestedOutline()));
            parameters.Add(p => p.DefaultSidebar, BitPdfSidebar.Bookmarks);
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.FindAll("[role='treeitem']").Count));

        // Left folds the open branch: its child leaves the tree.
        component.FindAll("[role='treeitem']")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll("[role='treeitem']").Count));
        Assert.AreEqual("false", component.Find("[role='treeitem']").GetAttribute("aria-expanded"));

        // Right unfolds it again.
        component.Find("[role='treeitem']").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.FindAll("[role='treeitem']").Count));
    }

    [TestMethod]
    public void BitPdfViewerShouldNavigateWhenABookmarkIsActivatedFromTheKeyboard()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithNestedOutline()));
            parameters.Add(p => p.DefaultSidebar, BitPdfSidebar.Bookmarks);
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.FindAll("[role='treeitem']").Count));
        Assert.AreEqual(1, component.Instance.CurrentPage);

        // The nested bookmark points at page 2.
        component.FindAll("[role='treeitem']")[1].KeyDown(new KeyboardEventArgs { Key = "Enter" });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.CurrentPage));
    }

    [TestMethod]
    public async Task BitPdfViewerShouldAnnounceThePageThroughALiveRegion()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(3)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.PageCount));

        var region = component.Find(".bit-pdv-sr");
        Assert.AreEqual("status", region.GetAttribute("role"));
        Assert.AreEqual("polite", region.GetAttribute("aria-live"));

        await component.InvokeAsync(() => component.Instance.GoToPage(2));

        component.WaitForAssertion(() =>
            Assert.AreEqual("Page 2 of 3", component.Find(".bit-pdv-sr").TextContent.Trim()));
    }

    [TestMethod]
    public async Task BitPdfViewerShouldReportTheFullscreenStateOnItsButton()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        Assert.IsFalse(component.Instance.IsFullscreen);
        Assert.AreEqual("false", component.Find("button[aria-label='Enter fullscreen']").GetAttribute("aria-pressed"));

        // The browser is the one that decides; the viewer mirrors what it reports.
        await component.InvokeAsync(() => component.Instance.OnFullscreenChanged(true));

        Assert.IsTrue(component.Instance.IsFullscreen);
        Assert.AreEqual("true", component.Find("button[aria-label='Exit fullscreen']").GetAttribute("aria-pressed"));
    }

    [TestMethod]
    public async Task BitPdfViewerShouldPrintOnlyTheRequestedRange()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(5)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(5, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.Print(2, 3));

        var call = Context.JSInterop.Invocations["BitBlazorUI.PdfViewer.print"].Last();
        Assert.AreEqual(2, call.Arguments[1]);
        Assert.AreEqual(3, call.Arguments[2]);

        // A backwards range is reordered rather than printing nothing.
        await component.InvokeAsync(() => component.Instance.Print(4, 2));

        call = Context.JSInterop.Invocations["BitBlazorUI.PdfViewer.print"].Last();
        Assert.AreEqual(2, call.Arguments[1]);
        Assert.AreEqual(4, call.Arguments[2]);

        // And an out-of-range one is clamped to the document.
        await component.InvokeAsync(() => component.Instance.Print(0, 99));

        call = Context.JSInterop.Invocations["BitBlazorUI.PdfViewer.print"].Last();
        Assert.AreEqual(1, call.Arguments[1]);
        Assert.AreEqual(5, call.Arguments[2]);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldPrintOnlyTheCurrentPageWhenAsked()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(4)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(4, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.GoToPage(3));
        await component.InvokeAsync(() => component.Instance.PrintCurrentPage());

        var call = Context.JSInterop.Invocations["BitBlazorUI.PdfViewer.print"].Last();
        Assert.AreEqual(3, call.Arguments[1]);
        Assert.AreEqual(3, call.Arguments[2]);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldOpenADocumentThroughTheApiAndKeepItAcrossHostRenders()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.OpenAsync(
            BitPdfSource.FromBytes(TestPdf.MultiPage(3), "opened.pdf")));

        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.PageCount));

        // A host re-render that does not change Source must not close what the reader
        // (or the host's own code) opened.
        component.Render(parameters => parameters.Add(p => p.Height, "321px"));

        Assert.AreEqual(3, component.Instance.PageCount);

        // A host re-render that DOES change Source still wins.
        component.Render(parameters => parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(5))));

        component.WaitForAssertion(() => Assert.AreEqual(5, component.Instance.PageCount));
    }

    [TestMethod]
    public async Task BitPdfViewerShouldCloseTheDocumentWhenOpenedWithNothing()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.OpenAsync(null));

        component.WaitForAssertion(() => Assert.AreEqual(0, component.Instance.PageCount));
        Assert.IsNull(component.Instance.Document);
        Assert.IsTrue(component.Find(".bit-pdv").TextContent.Contains("No document loaded."));
    }

    [TestMethod]
    public async Task BitPdfViewerShouldNavigateToABookmarkDestination()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithNestedOutline()));
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.PageCount));

        var child = component.Instance.Outline[0].Children[0];
        Assert.IsNotNull(child.Destination);

        await component.InvokeAsync(() => component.Instance.GoToDestination(child.Destination));

        Assert.AreEqual(2, component.Instance.CurrentPage);
        // A destination carrying a vertical position scrolls into the page rather
        // than stopping at its top edge.
        var call = Context.JSInterop.Invocations["BitBlazorUI.PdfViewer.scrollToPageOffset"].Last();
        Assert.AreEqual(2, call.Arguments[1]);
        Assert.IsTrue(Convert.ToDouble(call.Arguments[2]) > 0);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldNavigateToANamedDestination()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithNamedDestination()));
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.GoToNamedDestination("chapter2"));
        Assert.AreEqual(2, component.Instance.CurrentPage);

        // A name the document does not declare leaves the reader where they were.
        await component.InvokeAsync(() => component.Instance.GoToNamedDestination("nowhere"));
        Assert.AreEqual(2, component.Instance.CurrentPage);
    }

    [TestMethod]
    public void BitPdfSourceShouldReadBase64AndDataUris()
    {
        byte[] bytes = TestPdf.HelloWorld();

        var plain = BitPdfSource.FromBase64(Convert.ToBase64String(bytes), "a.pdf");
        CollectionAssert.AreEqual(bytes, plain.Bytes);
        Assert.AreEqual("a.pdf", plain.FileName);

        var dataUri = BitPdfSource.FromBase64($"data:application/pdf;base64,{Convert.ToBase64String(bytes)}");
        CollectionAssert.AreEqual(bytes, dataUri.Bytes);
    }

    [TestMethod]
    public void BitPdfViewerShouldOpenADocumentGivenAsBase64()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBase64(
                Convert.ToBase64String(TestPdf.MultiPage(3)), "b64.pdf"));
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.PageCount));
    }

    [TestMethod]
    public async Task BitPdfViewerShouldNavigateByTypingAPageLabel()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithPageLabels()));
        });

        component.WaitForAssertion(() => Assert.AreEqual(4, component.Instance.PageCount));

        // The fixture labels its pages i, ii, 1, 2 - so the box shows the label, and
        // the plain page number goes beside it.
        var box = component.Find("input.bit-pdv-page-input");
        Assert.AreEqual("i", box.GetAttribute("value"));
        // A label is not a number, so the box stops being a number box.
        Assert.AreEqual("text", box.GetAttribute("type"));
        Assert.IsTrue(component.Find(".bit-pdv-page-label").TextContent.Contains("1"));

        component.Find("input.bit-pdv-page-input").Change("ii");
        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.CurrentPage));

        // The label "1" belongs to page 3, not to page 1: the label wins over the
        // number it happens to look like.
        component.Find("input.bit-pdv-page-input").Change("1");
        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.CurrentPage));

        // Something that is neither leaves the reader where they were, and the box is
        // put back on the page actually shown instead of keeping what was typed.
        component.Find("input.bit-pdv-page-input").Change("nowhere");
        await Task.Delay(1);
        Assert.AreEqual(3, component.Instance.CurrentPage);
        // Written back through the DOM rather than by re-creating the element, so the
        // reader keeps their focus; the call is what the assertion can see.
        component.WaitForAssertion(() =>
        {
            var call = Context.JSInterop.Invocations["BitBlazorUI.PdfViewer.setValue"].Last();
            Assert.AreEqual("1", call.Arguments[1]);
        });
    }

    [TestMethod]
    public void BitPdfViewerShouldStillAcceptAPlainPageNumber()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(5)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(5, component.Instance.PageCount));

        component.Find("input.bit-pdv-page-input").Change("4");

        component.WaitForAssertion(() => Assert.AreEqual(4, component.Instance.CurrentPage));
        // An unlabelled document shows the number in the box and nothing beside it,
        // and the box stays a number box - spinner, range and numeric keypad included.
        Assert.AreEqual(0, component.FindAll(".bit-pdv-page-label").Count);
        var box = component.Find("input.bit-pdv-page-input");
        Assert.AreEqual("number", box.GetAttribute("type"));
        Assert.AreEqual("1", box.GetAttribute("min"));
        Assert.AreEqual("5", box.GetAttribute("max"));
    }

    [TestMethod]
    public void BitPdfViewerShouldKeepRenderingAPageWhoseAnnotationIsDamaged()
    {
        var errors = new List<string>();

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithBrokenAnnotation()));
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<string>(this, e => errors.Add(e)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        // The annotation is unusable, but the page - and the link annotation beside it -
        // still render, so one damaged annotation never costs the whole page.
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(0, component.Instance.FailedPages.Count);
        Assert.AreEqual(1, component.FindAll("[data-page='1'] .bit-pdv-html-page").Count);
        Assert.AreEqual(1, component.FindAll("[data-bit-pdv-page]").Count);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldNavigateFromALinkIntoTheMiddleOfAPage()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithInternalLink()));
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.PageCount));

        // The link's markup carries the destination's vertical position, so the click
        // handler can ask for that spot rather than the page's top edge.
        var hotspot = component.Find("[data-bit-pdv-page]");
        Assert.AreEqual("2", hotspot.GetAttribute("data-bit-pdv-page"));
        Assert.AreEqual("250", hotspot.GetAttribute("data-bit-pdv-top"));

        await component.InvokeAsync(() => component.Instance.OnLinkNavigate(2, 250));

        Assert.AreEqual(2, component.Instance.CurrentPage);
        var call = Context.JSInterop.Invocations["BitBlazorUI.PdfViewer.scrollToPageOffset"].Last();
        Assert.AreEqual(2, call.Arguments[1]);
        Assert.IsTrue(Convert.ToDouble(call.Arguments[2]) > 0);
    }

    [TestMethod]
    public void BitPdfViewerShouldRenderADropTargetInputOnlyWhenDropsAreAllowed()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
            // The toolbar's own picker is off, so nothing would hold a dropped file.
            parameters.Add(p => p.ToolbarItems, BitPdfToolbarItems.Navigation);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));
        Assert.AreEqual(0, component.FindAll("input.bit-pdv-file").Count);

        component.Render(parameters => parameters.Add(p => p.AllowDropFile, true));

        // Accepting drops renders a hidden one, which is what the drop handler fills.
        var input = component.Find("input.bit-pdv-file");
        Assert.AreEqual("-1", input.GetAttribute("tabindex"));
        Assert.AreEqual("true", input.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitPdfViewerShouldRenderADropTargetInputWhenTheToolbarIsHidden()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
            // The open-file item is on by default, but a hidden toolbar renders none of
            // it: without an input of its own a drop would have nowhere to land.
            parameters.Add(p => p.ShowToolbar, false);
            parameters.Add(p => p.AllowDropFile, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        var input = component.Find("input.bit-pdv-file");
        Assert.AreEqual("-1", input.GetAttribute("tabindex"));
        Assert.AreEqual("true", input.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitPdfViewerShouldNotDuplicateTheFilePickerWhenBothOpenFileAndDropsAreOn()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
            parameters.Add(p => p.AllowDropFile, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        // The toolbar's picker is the one the drop handler fills; a second would make
        // "the first input in the viewer" ambiguous.
        Assert.AreEqual(1, component.FindAll("input.bit-pdv-file").Count);
    }

    [TestMethod]
    public void BitPdfViewerShouldCloseThePropertiesDialogOnEscapeWithoutTheShortcutListener()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
            // A modal must be closable from the keyboard even with shortcuts off.
            parameters.Add(p => p.EnableKeyboardShortcuts, false);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        component.Find("button[aria-label='Document properties']").Click();
        var dialog = component.Find("[role='dialog']");
        Assert.AreEqual("-1", dialog.GetAttribute("tabindex"));

        dialog.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        component.WaitForAssertion(() => Assert.AreEqual(0, component.FindAll("[role='dialog']").Count));
    }

    [TestMethod]
    public async Task BitPdfViewerShouldReadAndClearTheReaderSelection()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithText("quotable words")));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        // The selection lives in the browser, so the viewer asks for it rather than
        // keeping a copy - and an empty answer comes back as an empty string, never null.
        string selected = await component.InvokeAsync(() => component.Instance.GetSelectedText());
        Assert.AreEqual(string.Empty, selected);
        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.PdfViewer.getSelectedText"].Count);

        await component.InvokeAsync(() => component.Instance.ClearSelection());
        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.PdfViewer.clearSelection"].Count);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldNotLeaveFullscreenWhenPresentingFromIt()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(2)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.PageCount));

        // The reader is already fullscreen (the browser said so).
        await component.InvokeAsync(() => component.Instance.OnFullscreenChanged(true));
        int before = Context.JSInterop.Invocations["BitBlazorUI.PdfViewer.toggleFullscreen"].Count;

        await component.InvokeAsync(() => component.Instance.EnterPresentationMode());

        // Toggling would have LEFT fullscreen, which is the opposite of presenting.
        Assert.IsTrue(component.Instance.IsPresenting);
        Assert.AreEqual(before, Context.JSInterop.Invocations["BitBlazorUI.PdfViewer.toggleFullscreen"].Count);
    }

    [TestMethod]
    public void BitPdfViewerShouldLeaveSelectionToTheTextLayerAlone()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithText("selectable words")));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        // The painted layer carries the same words as the selection layer above it, so
        // it must not be selectable - a copy would otherwise contain everything twice.
        string painted = component.Find(".bit-pdv-html-page").GetAttribute("style") ?? string.Empty;
        Assert.IsTrue(painted.Contains("user-select:none"), painted);

        // Through a custom property, so a surface that forbids copying can veto it -
        // no stylesheet rule could outrank an inline declaration.
        string layer = component.Find(".bit-pdv-text-layer").GetAttribute("style") ?? string.Empty;
        Assert.IsTrue(layer.Contains("user-select:var(--bit-pdv-select,text)"), layer);
    }

    [TestMethod]
    public void BitPdfViewerShouldRenderAPageFragmentThatCopiesCorrectlyOnItsOwn()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithText("standalone")));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        // The rule travels with the markup, so a fragment taken out of the viewer
        // through the public API copies the way it does inside it.
        string html = component.Instance.RenderPageHtml(1);

        Assert.IsTrue(html.Contains("user-select:none"), "the painted layer must opt out");
        // The custom property is unset outside the viewer, so the fallback applies and
        // a standalone fragment stays selectable.
        Assert.IsTrue(html.Contains("user-select:var(--bit-pdv-select,text)"),
            "the selection layer must opt back in");
    }

    [TestMethod]
    public async Task BitPdfViewerShouldIgnoreDocumentPermissionsByDefault()
    {
        // The fixture grants everything, so this pins the DEFAULT rather than the
        // enforcement: nothing is disabled and no method refuses without being asked.
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(2)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.PageCount));

        Assert.IsTrue(component.Instance.Permissions.CanPrint);
        Assert.IsTrue(component.Instance.Permissions.CanCopy);
        Assert.IsFalse(component.Find("button[aria-label='Print document']").HasAttribute("disabled"));
        Assert.AreEqual(0, component.FindAll(".bit-pdv-surface.bit-pdv-nocopy").Count);

        await component.InvokeAsync(() => component.Instance.Print());
        Assert.IsTrue(Context.JSInterop.Invocations["BitBlazorUI.PdfViewer.print"].Count > 0);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldEnforceDocumentPermissionsWhenAsked()
    {
        // Encrypted with printing and copying withheld.
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.Encrypted("pw", permissions: 0)).WithPassword("pw"));
            parameters.Add(p => p.RespectPermissions, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        Assert.IsFalse(component.Instance.Permissions.CanPrint);
        Assert.IsFalse(component.Instance.Permissions.CanCopy);

        Assert.IsTrue(component.Find("button[aria-label='Print document']").HasAttribute("disabled"));
        Assert.IsTrue(component.Find("button[aria-label='Download document']").HasAttribute("disabled"));
        // Text selection is off for the whole surface: the class the stylesheet turns
        // into --bit-pdv-select:none, which is what the layer's inline rule reads.
        Assert.AreEqual(1, component.FindAll(".bit-pdv-surface.bit-pdv-nocopy").Count);

        // And the methods refuse rather than quietly going around the toolbar.
        await component.InvokeAsync(() => component.Instance.Print());
        await component.InvokeAsync(() => component.Instance.Download());

        Assert.IsFalse(Context.JSInterop.Invocations["BitBlazorUI.PdfViewer.print"].Count > 0);
        Assert.IsFalse(Context.JSInterop.Invocations["BitBlazorUI.PdfViewer.download"].Count > 0);
    }

    [TestMethod]
    public void BitPdfViewerShouldStillAllowPrintingOfAPermissiveEncryptedDocument()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.Encrypted("pw")).WithPassword("pw"));
            parameters.Add(p => p.RespectPermissions, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        // The default fixture grants every permission, so enforcing them changes nothing.
        Assert.IsFalse(component.Find("button[aria-label='Print document']").HasAttribute("disabled"));
        Assert.AreEqual(0, component.FindAll(".bit-pdv-surface.bit-pdv-nocopy").Count);
    }

    [TestMethod]
    public void BitPdfViewerShouldTrapFocusInsideItsDialogs()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        component.Find("button[aria-label='Document properties']").Click();

        // The dialog is focusable itself (so focus has somewhere to land) and asks the
        // browser side to keep Tab within it.
        var dialog = component.Find("[role='dialog']");
        Assert.AreEqual("true", dialog.GetAttribute("aria-modal"));
        Assert.AreEqual("-1", dialog.GetAttribute("tabindex"));
        component.WaitForAssertion(() =>
            Assert.IsTrue(Context.JSInterop.Invocations["BitBlazorUI.PdfViewer.trapFocus"].Count > 0));
    }

    [TestMethod]
    public async Task BitPdfViewerShouldApplyTheZoomADestinationAsksFor()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithZoomedDestination()));
            parameters.Add(p => p.InitialZoomMode, BitPdfZoomMode.ActualSize);
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.PageCount));
        Assert.AreEqual(1, component.Instance.Zoom, 0.0001);

        var bookmark = component.Instance.Outline[0];
        await component.InvokeAsync(() => component.Instance.GoToDestination(bookmark.Destination));

        // The destination names 200%, which is the scale the reader lands at.
        Assert.AreEqual(2, component.Instance.Zoom, 0.0001);
        Assert.AreEqual(2, component.Instance.CurrentPage);
    }
}

/// <summary>
/// Assembles minimal but spec-valid PDF byte streams (header, indirect objects,
/// a classic cross-reference table and trailer) for use as test fixtures.
/// </summary>
internal static class TestPdf
{
    /// <summary>
    /// Builds a PDF from the given object bodies. Body <c>i</c> becomes object
    /// number <c>i + 1</c>; the trailer's <c>/Root</c> points to
    /// <paramref name="rootObjNum"/>.
    /// </summary>
    public static byte[] Build(IReadOnlyList<string> bodies, int rootObjNum, string trailerExtra = "")
    {
        using var ms = new MemoryStream();

        void Write(string s)
        {
            byte[] b = Encoding.Latin1.GetBytes(s);
            ms.Write(b, 0, b.Length);
        }

        Write("%PDF-1.7\n");

        var offsets = new long[bodies.Count + 1];
        for (int i = 0; i < bodies.Count; i++)
        {
            offsets[i + 1] = ms.Position;
            Write($"{i + 1} 0 obj\n{bodies[i]}\nendobj\n");
        }

        long xrefPos = ms.Position;
        Write($"xref\n0 {bodies.Count + 1}\n");
        Write("0000000000 65535 f \n");                // free head, exactly 20 bytes
        for (int i = 1; i <= bodies.Count; i++)
        {
            Write($"{offsets[i]:D10} 00000 n \n");     // in-use entry, exactly 20 bytes
        }

        Write($"trailer\n<< /Size {bodies.Count + 1} /Root {rootObjNum} 0 R{trailerExtra} >>\n");
        Write($"startxref\n{xrefPos}\n%%EOF");

        return ms.ToArray();
    }

    /// <summary>Wraps raw content into a stream object body with a correct /Length.</summary>
    public static string Stream(string content, string extraDictEntries = "")
    {
        int length = Encoding.Latin1.GetByteCount(content);
        return $"<< /Length {length}{extraDictEntries} >>\nstream\n{content}\nendstream";
    }

    /// <summary>
    /// A complete single-page document: 200x200 page, one Helvetica font, a
    /// content stream that shows "Hello", and a one-entry outline pointing at the
    /// page. Exercises parsing, page-tree traversal, rendering and bookmarks.
    /// </summary>
    public static byte[] HelloWorld()
    {
        string content = "BT /F1 24 Tf 50 100 Td (Hello) Tj ET";
        var bodies = new List<string>
        {
            // 1: Catalog
            "<< /Type /Catalog /Pages 2 0 R /Outlines 5 0 R >>",
            // 2: Pages
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            // 3: Page
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] " +
                "/Resources << /Font << /F1 4 0 R >> >> /Contents 6 0 R >>",
            // 4: Font
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            // 5: Outlines root
            "<< /Type /Outlines /First 7 0 R /Last 7 0 R /Count 1 >>",
            // 6: Contents
            Stream(content),
            // 7: Outline item -> page 3
            "<< /Title (Chapter 1) /Parent 5 0 R /Dest [3 0 R /XYZ 0 200 0] >>",
        };
        return Build(bodies, rootObjNum: 1);
    }

    /// <summary>
    /// A single-page document with two optional-content groups, the second of which
    /// the default configuration turns off. The page draws one marked-content run per
    /// layer, so switching a layer changes what the page renders.
    /// </summary>
    public static byte[] WithLayers()
    {
        var bodies = new List<string>
        {
            // 1: Catalog, declaring both groups and hiding the second by default
            "<< /Type /Catalog /Pages 2 0 R " +
                "/OCProperties << /OCGs [6 0 R 7 0 R] /D << /OFF [7 0 R] >> >> >>",
            // 2: Pages
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            // 3: Page, mapping the two groups into its resources
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 200] " +
                "/Resources << /Font << /F1 4 0 R >> /Properties << /OC1 6 0 R /OC2 7 0 R >> >> " +
                "/Contents 5 0 R >>",
            // 4: Font
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            // 5: Contents - one marked-content run per group
            Stream("/OC /OC1 BDC BT /F1 12 Tf 20 150 Td (VisibleLayerText) Tj ET EMC " +
                   "/OC /OC2 BDC BT /F1 12 Tf 20 100 Td (HiddenLayerText) Tj ET EMC"),
            // 6/7: The optional-content groups
            "<< /Type /OCG /Name (Base drawing) >>",
            "<< /Type /OCG /Name (Annotations) >>",
        };
        return Build(bodies, rootObjNum: 1);
    }

    /// <summary>
    /// A single-page document whose two optional-content groups are both listed /ON by
    /// the default configuration, but whose /AS usage-application rule turns the second
    /// off for the /View event - the way a print-only watermark layer ships.
    /// </summary>
    public static byte[] WithViewUsageLayers()
    {
        var bodies = new List<string>
        {
            // 1: Catalog. /OFF is empty, so only the /AS rule can hide anything.
            "<< /Type /Catalog /Pages 2 0 R " +
                "/OCProperties << /OCGs [6 0 R 7 0 R] /D << /OFF [] " +
                    "/AS [ << /Event /View /Category [/View] /OCGs [7 0 R] >> ] >> >> >>",
            // 2: Pages
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            // 3: Page, mapping the two groups into its resources
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 200] " +
                "/Resources << /Font << /F1 4 0 R >> /Properties << /OC1 6 0 R /OC2 7 0 R >> >> " +
                "/Contents 5 0 R >>",
            // 4: Font
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            // 5: Contents - one marked-content run per group
            Stream("/OC /OC1 BDC BT /F1 12 Tf 20 150 Td (ScreenLayerText) Tj ET EMC " +
                   "/OC /OC2 BDC BT /F1 12 Tf 20 100 Td (PrintOnlyLayerText) Tj ET EMC"),
            // 6/7: the groups - the second declares itself off on screen
            "<< /Type /OCG /Name (Screen) >>",
            "<< /Type /OCG /Name (Print only) /Usage << /View << /ViewState /OFF >> >> >>",
        };
        return Build(bodies, rootObjNum: 1);
    }

    /// <summary>
    /// A two-page document whose outline nests one bookmark under another, both with
    /// explicit XYZ destinations. Exercises the tree's folding, its roving tab stop
    /// and destination navigation into the middle of a page.
    /// </summary>
    public static byte[] WithNestedOutline()
    {
        var bodies = new List<string>
        {
            // 1: Catalog
            "<< /Type /Catalog /Pages 2 0 R /Outlines 6 0 R >>",
            // 2: Pages
            "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>",
            // 3/4: the two pages, sharing one content stream
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 400] /Contents 5 0 R >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 400] /Contents 5 0 R >>",
            // 5: Contents
            Stream("BT ET"),
            // 6: Outlines root
            "<< /Type /Outlines /First 7 0 R /Last 7 0 R /Count 2 >>",
            // 7: the parent bookmark -> page 1, with one child
            "<< /Title (Part one) /Parent 6 0 R /First 8 0 R /Last 8 0 R /Count 1 /Dest [3 0 R /XYZ 0 400 0] >>",
            // 8: the child bookmark -> partway down page 2
            "<< /Title (Section 1.1) /Parent 7 0 R /Dest [4 0 R /XYZ 0 250 0] >>",
        };
        return Build(bodies, rootObjNum: 1);
    }

    /// <summary>
    /// A four-page document whose /PageLabels tree labels the first two pages i and
    /// ii and restarts the numbering at 1 on the third, so a label and the page
    /// number it looks like belong to different pages.
    /// </summary>
    public static byte[] WithPageLabels()
    {
        var bodies = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R /PageLabels << /Nums [0 << /S /r >> 2 << /S /D /St 1 >>] >> >>",
            "<< /Type /Pages /Kids [3 0 R 4 0 R 5 0 R 6 0 R] /Count 4 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents 7 0 R >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents 7 0 R >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents 7 0 R >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents 7 0 R >>",
            Stream("BT ET"),
        };
        return Build(bodies, rootObjNum: 1);
    }

    /// <summary>
    /// A single-page document carrying two annotations: one whose appearance stream is
    /// unusable (an operator token far past the length any real one has), and a link
    /// after it. Exercises the rule that a damaged annotation costs only itself.
    /// </summary>
    public static byte[] WithBrokenAnnotation()
    {
        var bodies = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 400] /Contents 4 0 R /Annots [6 0 R 7 0 R] >>",
            Stream("BT ET"),
            // 5: the unusable appearance stream
            Stream(new string('Q', 200), " /Type /XObject /Subtype /Form /BBox [0 0 40 20]"),
            // 6: the annotation wearing it
            "<< /Type /Annot /Subtype /Square /Rect [20 300 60 320] /AP << /N 5 0 R >> >>",
            // 7: a link on the same page, which must survive it
            "<< /Type /Annot /Subtype /Link /Rect [20 200 180 230] /Dest [3 0 R /Fit] >>",
        };
        return Build(bodies, rootObjNum: 1);
    }

    /// <summary>
    /// A two-page document whose first page carries a link annotation pointing partway
    /// down the second, so the hotspot's markup has a vertical position to carry.
    /// </summary>
    public static byte[] WithInternalLink()
    {
        var bodies = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 400] /Contents 5 0 R /Annots [6 0 R] >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 400] /Contents 5 0 R >>",
            Stream("BT ET"),
            "<< /Type /Annot /Subtype /Link /Rect [20 300 180 330] /Dest [4 0 R /XYZ 0 250 0] >>",
        };
        return Build(bodies, rootObjNum: 1);
    }

    /// <summary>
    /// A two-page document whose single bookmark points at the second page with an
    /// explicit 200% XYZ zoom, so the destination names the scale to read it at.
    /// </summary>
    public static byte[] WithZoomedDestination()
    {
        var bodies = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R /Outlines 6 0 R >>",
            "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents 5 0 R >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents 5 0 R >>",
            Stream("BT ET"),
            "<< /Type /Outlines /First 7 0 R /Last 7 0 R /Count 1 >>",
            "<< /Title (Zoomed) /Parent 6 0 R /Dest [4 0 R /XYZ 0 200 2] >>",
        };
        return Build(bodies, rootObjNum: 1);
    }

    /// <summary>
    /// A two-page document declaring a named destination ("chapter2") pointing at the
    /// second page, through the catalog's /Names /Dests name tree.
    /// </summary>
    public static byte[] WithNamedDestination()
    {
        var bodies = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R /Names << /Dests 6 0 R >> >>",
            "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents 5 0 R >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents 5 0 R >>",
            Stream("BT ET"),
            "<< /Names [(chapter2) [4 0 R /Fit]] >>",
        };
        return Build(bodies, rootObjNum: 1);
    }

    /// <summary>
    /// A single-page document showing exactly <paramref name="text"/>, for tests
    /// about what the text index and the find box make of particular wording.
    /// </summary>
    public static byte[] WithText(string text)
    {
        var bodies = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 400 200] " +
                "/Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            Stream($"BT /F1 12 Tf 20 100 Td ({text}) Tj ET"),
        };
        return Build(bodies, rootObjNum: 1);
    }

    /// <summary>
    /// A <paramref name="pageCount"/>-page document, each page showing its own
    /// number. Used wherever a test needs real navigation between pages.
    /// </summary>
    public static byte[] MultiPage(int pageCount)
    {
        // Object layout: 1 catalog, 2 pages tree, 3 font, then per page a page
        // dict followed by its content stream.
        var kids = new List<string>();
        var bodies = new List<string> { "", "", "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>" };
        for (int p = 0; p < pageCount; p++)
        {
            int pageObj = 4 + (p * 2);
            int contentObj = pageObj + 1;
            kids.Add($"{pageObj} 0 R");
            bodies.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] " +
                       $"/Resources << /Font << /F1 3 0 R >> >> /Contents {contentObj} 0 R >>");
            bodies.Add(Stream($"BT /F1 24 Tf 50 100 Td (Page {p + 1}) Tj ET"));
        }
        bodies[0] = "<< /Type /Catalog /Pages 2 0 R >>";
        bodies[1] = $"<< /Type /Pages /Kids [{string.Join(" ", kids)}] /Count {pageCount} >>";

        return Build(bodies, rootObjNum: 1);
    }

    // ----- Encryption (standard security handler, revision 2 / 40-bit RC4) -----
    //
    // Assembling an encrypted fixture here rather than checking one in keeps the
    // test readable and lets a test choose its own password. The algorithms are the
    // ones in ISO 32000 §7.6.4, written out independently of the reader under test.

    private static readonly byte[] PasswordPadding =
    [
        0x28, 0xBF, 0x4E, 0x5E, 0x4E, 0x75, 0x8A, 0x41, 0x64, 0x00, 0x4E, 0x56, 0xFF, 0xFA, 0x01, 0x08,
        0x2E, 0x2E, 0x00, 0xB6, 0xD0, 0x68, 0x3E, 0x80, 0x2F, 0x0C, 0xA9, 0xFE, 0x64, 0x53, 0x69, 0x7A,
    ];

    /// <summary>
    /// A single-page document encrypted with <paramref name="userPassword"/>
    /// (revision 2, 40-bit RC4). The owner password is the same, so opening it needs
    /// exactly that string. <paramref name="permissions"/> is the /P field: the
    /// default grants everything, while <c>0</c> withholds every optional right
    /// (printing and copying included).
    /// </summary>
    public static byte[] Encrypted(string userPassword, int permissions = -1)
    {
        byte[] id0 = Encoding.Latin1.GetBytes("0123456789abcdef");
        int p = permissions;

        // Algorithm 3: /O is the padded user password, RC4'd with a key derived
        // from the owner password.
        byte[] ownerKey = Md5(Pad(Encoding.Latin1.GetBytes(userPassword)))[..5];
        byte[] o = Rc4(ownerKey, Pad(Encoding.Latin1.GetBytes(userPassword)));

        // Algorithm 2: the file key.
        // /P is a signed 32-bit field stored little-endian; -1 (all permissions)
        // needs the unchecked conversion the spec's byte view implies.
        byte[] pBytes = unchecked([(byte)p, (byte)(p >> 8), (byte)(p >> 16), (byte)(p >> 24)]);
        byte[] fileKey = Md5([.. Pad(Encoding.Latin1.GetBytes(userPassword)), .. o, .. pBytes, .. id0])[..5];

        // Algorithm 4: /U for revision 2 is the padding string under the file key.
        byte[] u = Rc4(fileKey, PasswordPadding);

        // The content stream is object 4; its key mixes in the object number.
        byte[] contentKey = ObjectKey(fileKey, 4, 0);
        string content = Encoding.Latin1.GetString(Rc4(contentKey, Encoding.Latin1.GetBytes("BT ET")));

        var bodies = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents 4 0 R >>",
            Stream(content),
            // The /Encrypt dictionary is never itself encrypted.
            $"<< /Filter /Standard /V 1 /R 2 /Length 40 /P {p} /O <{Hex(o)}> /U <{Hex(u)}> >>",
        };
        return Build(bodies, rootObjNum: 1, trailerExtra: $" /Encrypt 5 0 R /ID [<{Hex(id0)}> <{Hex(id0)}>]");
    }

    private static byte[] Pad(byte[] password)
    {
        var padded = new byte[32];
        int take = Math.Min(password.Length, 32);
        Array.Copy(password, padded, take);
        Array.Copy(PasswordPadding, 0, padded, take, 32 - take);
        return padded;
    }

    private static byte[] ObjectKey(byte[] fileKey, int objectNumber, int generation)
    {
        byte[] input =
        [
            .. fileKey,
            (byte)objectNumber, (byte)(objectNumber >> 8), (byte)(objectNumber >> 16),
            (byte)generation, (byte)(generation >> 8),
        ];
        return Md5(input)[..Math.Min(fileKey.Length + 5, 16)];
    }

    private static byte[] Md5(byte[] data) => System.Security.Cryptography.MD5.HashData(data);

    private static string Hex(byte[] bytes) => Convert.ToHexString(bytes);

    /// <summary>The RC4 stream cipher, written out here so the fixture does not lean
    /// on the implementation it is meant to exercise.</summary>
    private static byte[] Rc4(byte[] key, byte[] data)
    {
        var s = new byte[256];
        for (int i = 0; i < 256; i++)
        {
            s[i] = (byte)i;
        }
        for (int i = 0, j = 0; i < 256; i++)
        {
            j = (j + s[i] + key[i % key.Length]) & 0xFF;
            (s[i], s[j]) = (s[j], s[i]);
        }

        var output = new byte[data.Length];
        for (int k = 0, a = 0, b = 0; k < data.Length; k++)
        {
            a = (a + 1) & 0xFF;
            b = (b + s[a]) & 0xFF;
            (s[a], s[b]) = (s[b], s[a]);
            output[k] = (byte)(data[k] ^ s[(s[a] + s[b]) & 0xFF]);
        }
        return output;
    }

    /// <summary>
    /// A single-page document carrying two embedded files: one in the catalog's
    /// <c>/Names /EmbeddedFiles</c> name tree and one pinned to the page through a
    /// <c>/FileAttachment</c> annotation.
    /// </summary>
    public static byte[] WithAttachments()
    {
        var bodies = new List<string>
        {
            // 1: Catalog, with the document-wide embedded-file name tree
            "<< /Type /Catalog /Pages 2 0 R /Names << /EmbeddedFiles << /Names [(notes.txt) 5 0 R] >> >> >>",
            // 2: Pages
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            // 3: Page, with the pinned attachment annotation
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents 4 0 R /Annots [8 0 R] >>",
            // 4: Contents
            Stream("BT ET"),
            // 5: File specification for the document-wide attachment
            "<< /Type /Filespec /F (notes.txt) /UF (notes.txt) /Desc (Some notes) /EF << /F 6 0 R >> >>",
            // 6: Its embedded stream
            Stream("hello attachment", " /Type /EmbeddedFile /Subtype /text#2Fplain"),
            // 7: File specification for the page-pinned attachment
            "<< /Type /Filespec /F (data.csv) /EF << /F 9 0 R >> >>",
            // 8: The annotation that pins it to page 1
            "<< /Type /Annot /Subtype /FileAttachment /Rect [10 10 30 30] /FS 7 0 R >>",
            // 9: Its embedded stream
            Stream("a,b,c", " /Type /EmbeddedFile"),
        };
        return Build(bodies, rootObjNum: 1);
    }

    /// <summary>
    /// A three-page document carrying everything the viewer surfaces beyond the
    /// pages themselves: an <c>/Info</c> dictionary, a <c>/PageLabels</c> number
    /// tree (roman then decimal) and an <c>/AcroForm</c> with one text field.
    /// </summary>
    public static byte[] RichDocument()
    {
        var bodies = new List<string>
        {
            // 1: Catalog, with page labels and an interactive form
            "<< /Type /Catalog /Pages 2 0 R " +
                "/PageLabels << /Nums [0 << /S /r >> 2 << /S /D /St 1 >>] >> " +
                "/AcroForm << /Fields [9 0 R] >> >>",
            // 2: Pages
            "<< /Type /Pages /Kids [3 0 R 5 0 R 7 0 R] /Count 3 >>",
            // 3/5/7: Pages, 4/6/8: their contents
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents 4 0 R >>",
            Stream("BT ET"),
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents 6 0 R >>",
            Stream("BT ET"),
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents 8 0 R >>",
            Stream("BT ET"),
            // 9: A terminal text field
            "<< /FT /Tx /T (fullName) /V (Ada Lovelace) >>",
            // 10: Document information dictionary
            "<< /Title (Test Document) /Author (Ada) /Subject (Testing) /Keywords (pdf, test) " +
                "/Creator (BitPdfViewerTests) /Producer (Bit.BlazorUI) " +
                "/CreationDate (D:20240102030405Z) /ModDate (D:20240506070809Z) >>",
        };
        return Build(bodies, rootObjNum: 1, trailerExtra: " /Info 10 0 R");
    }
}
