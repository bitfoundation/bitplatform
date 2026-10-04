using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

/// <summary>
/// Covers what the viewer tells assistive technology beyond the patterns BitPdfViewerTests already pins: the names of
/// the surface and the thumbnails, the busy state, how the dialogs are labelled and described, the inert chrome of a
/// disabled viewer, and the custom toolbar content.
/// </summary>
[TestClass]
public class BitPdfViewerAccessibilityTests : BunitTestContext
{
    [TestMethod]
    public void BitPdfViewerSurfaceShouldNotBorrowTheToolbarsName()
    {
        var component = RenderComponent<BitPdfViewer>();

        var surface = component.Find(".bit-pdv-surface");

        Assert.AreEqual("region", surface.GetAttribute("role"));
        Assert.AreEqual("Document", surface.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPdfViewerSurfaceShouldBeNamedAfterTheFileThenTheHostsLabel()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld(), "report.pdf"));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));
        Assert.AreEqual("report.pdf", component.Find(".bit-pdv-surface").GetAttribute("aria-label"));

        component.Render(parameters => parameters.Add(p => p.AriaLabel, "Quarterly report"));

        Assert.AreEqual("Quarterly report", component.Find(".bit-pdv-surface").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPdfViewerShouldFallBackToTheDocumentsOwnTitleWithoutAFileName()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.RichDocument()));
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.PageCount));

        Assert.AreEqual("Test Document", component.Find(".bit-pdv-surface").GetAttribute("aria-label"));
        Assert.AreEqual("Test Document", component.Find(".bit-pdv-title").TextContent);
    }

    [TestMethod]
    public void BitPdfViewerSurfaceShouldNotBeBusyOnceTheDocumentIsIn()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));
        component.WaitForAssertion(() => Assert.AreEqual("false", component.Find(".bit-pdv-surface").GetAttribute("aria-busy")));
    }

    [TestMethod]
    public void BitPdfViewerThumbnailsShouldBeNamedAsPages()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.RichDocument()));
            parameters.Add(p => p.DefaultSidebar, BitPdfSidebar.Thumbnails);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll("[data-thumb]").Count));

        var thumbs = component.FindAll("[data-thumb]");

        // "Page ii", not a bare "ii" - and the visible caption is not read a second time.
        Assert.AreEqual("Page i", thumbs[0].GetAttribute("aria-label"));
        Assert.AreEqual("Page ii", thumbs[1].GetAttribute("aria-label"));
        Assert.AreEqual("true", component.FindAll(".bit-pdv-thumb-label")[0].GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitPdfViewerThumbnailNamesShouldBeLocalizable()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(2)));
            parameters.Add(p => p.DefaultSidebar, BitPdfSidebar.Thumbnails);
            parameters.Add(p => p.Texts, new BitPdfViewerTexts { ThumbnailAriaLabelFormat = "Seite {0}" });
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.FindAll("[data-thumb]").Count));

        Assert.AreEqual("Seite 2", component.FindAll("[data-thumb]")[1].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPdfViewerPasswordDialogShouldBeLabelledAndDescribedByItsOwnText()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.Encrypted("hunter2")));
            parameters.Add(p => p.Classes, new BitPdfViewerClassStyles { PasswordDialog = "custom-password" });
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-pdv-password").Count));

        var dialog = component.Find(".bit-pdv-password");
        Assert.IsTrue(dialog.ClassList.Contains("custom-password"));

        var title = component.Find($"#{dialog.GetAttribute("aria-labelledby")}");
        Assert.AreEqual("Password required", title.TextContent.Trim());

        // The prompt describes the box too, so a refusal is read out with the box it is retyped into.
        var input = component.Find(".bit-pdv-password-input");
        Assert.AreEqual(dialog.GetAttribute("aria-describedby"), input.GetAttribute("aria-describedby"));
        Assert.AreEqual("current-password", input.GetAttribute("autocomplete"));

        input.Input("wrong");
        component.Find(".bit-pdv-dialog-actions .bit-pdv-act").Click();

        component.WaitForAssertion(() =>
        {
            var description = component.Find($"#{component.Find(".bit-pdv-password-input").GetAttribute("aria-describedby")}");
            StringAssert.Contains(description.TextContent, "not accepted");
            Assert.IsTrue(description.ClassList.Contains("bit-pdv-password-error"));
        });
    }

    [TestMethod]
    public void BitPdfViewerPropertiesDialogShouldBeLabelledByItsHeading()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        component.InvokeAsync(component.Instance.ToggleProperties);

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll("[role='dialog']").Count));

        var dialog = component.Find("[role='dialog']");
        Assert.AreEqual("Document properties", component.Find($"#{dialog.GetAttribute("aria-labelledby")}").TextContent.Trim());
    }

    [TestMethod]
    public void BitPdfViewerShouldMakeItsChromeInertWhileDisabled()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(2)));
            parameters.Add(p => p.DefaultSidebar, BitPdfSidebar.Thumbnails);
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.FindAll("[data-thumb]").Count));

        Assert.IsTrue(component.Find(".bit-pdv-toolbar").HasAttribute("inert"));
        Assert.IsTrue(component.Find(".bit-pdv-thumbs").HasAttribute("inert"));

        component.Render(parameters => parameters.Add(p => p.IsEnabled, true));

        Assert.IsFalse(component.Find(".bit-pdv-toolbar").HasAttribute("inert"));
        Assert.IsFalse(component.Find(".bit-pdv-thumbs").HasAttribute("inert"));
    }

    [TestMethod]
    public void BitPdfViewerShouldMarkABookmarkWithoutATargetAsDisabled()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithNestedOutline()));
            parameters.Add(p => p.DefaultSidebar, BitPdfSidebar.Bookmarks);
        });

        component.WaitForAssertion(() => Assert.IsTrue(component.FindAll("[role='treeitem']").Count > 0));

        foreach (var item in component.FindAll("[role='treeitem']"))
        {
            var disabled = item.ClassList.Contains("bit-pdv-dis");
            Assert.AreEqual(disabled ? "true" : null, item.GetAttribute("aria-disabled"));
        }
    }

    [TestMethod]
    public void BitPdfViewerShouldRenderCustomToolbarContentWithTheViewerAsItsContext()
    {
        BitPdfViewer? context = null;

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.ToolbarItems, BitPdfToolbarItems.Navigation);
            parameters.Add(p => p.ToolbarStartTemplate, (RenderFragment<BitPdfViewer>)(viewer => builder =>
            {
                context = viewer;
                builder.AddMarkupContent(0, "<span class=\"custom-start\">Start</span>");
            }));
            parameters.Add(p => p.ToolbarEndTemplate, (RenderFragment<BitPdfViewer>)(viewer => builder =>
                builder.AddMarkupContent(0, "<button type=\"button\" class=\"custom-end\">End</button>")));
        });

        var toolbar = component.Find(".bit-pdv-toolbar");

        Assert.AreSame(component.Instance, context);

        // The start content opens the toolbar and the end content closes it, around the built-in controls.
        var groups = toolbar.Children;
        Assert.IsTrue(groups[0].QuerySelector(".custom-start") is not null);
        Assert.IsTrue(groups[^1].QuerySelector(".custom-end") is not null);
    }

    [TestMethod]
    public void BitPdfViewerShouldSetItsSizeThroughThePublicVariables()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Height, "300px");
            parameters.Add(p => p.Width, "50%");
        });

        var style = component.Find(".bit-pdv").GetAttribute("style") ?? string.Empty;

        StringAssert.Contains(style, "--bit-PdfViewer-height:300px");
        StringAssert.Contains(style, "--bit-PdfViewer-width:50%");
    }
}
