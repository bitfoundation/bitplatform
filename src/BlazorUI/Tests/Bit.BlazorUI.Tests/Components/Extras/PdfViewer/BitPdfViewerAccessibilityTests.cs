using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
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

    [TestMethod]
    public void BitPdfViewerShouldHideThePaintedLayerAndKeepTheTextLayerReadable()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll("[data-page='1'] .bit-pdv-html-page").Count));

        // The painted glyphs are a picture of the words the selection layer holds, so only one of the two is read.
        var painted = component.Find(".bit-pdv-html-page > [aria-hidden='true']");
        Assert.IsTrue(painted.QuerySelectorAll("span").Length > 0);
        Assert.AreEqual(0, painted.QuerySelectorAll("[data-bit-pdv-sel]").Length);

        var text = component.Find(".bit-pdv-text-layer [data-bit-pdv-sel]");
        Assert.IsNull(text.Closest("[aria-hidden='true']"));
        StringAssert.Contains(text.TextContent, "Hello");
    }

    [TestMethod]
    public void BitPdfViewerPagesShouldBeNamedGroups()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.RichDocument()));
            parameters.Add(p => p.Texts, new BitPdfViewerTexts { PageAriaLabelFormat = "Seite {0}" });
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.PageCount));

        var pages = component.FindAll(".bit-pdv-page[data-page]");

        // A group, not a region: hundreds of landmarks would bury the viewer's own. Named by the document's labels.
        Assert.AreEqual("group", pages[0].GetAttribute("role"));
        Assert.AreEqual("Seite i", pages[0].GetAttribute("aria-label"));
        Assert.AreEqual("Seite ii", pages[1].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPdfViewerInternalLinksShouldBeNamedLinksTheKeyboardReaches()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithInternalLink()));
            parameters.Add(p => p.Texts, new BitPdfViewerTexts { LinkAriaLabelFormat = "Zu Seite {0}" });
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.PageCount));

        var link = component.Find("[data-bit-pdv-page]");

        Assert.AreEqual("A", link.TagName);
        Assert.AreEqual("#page=2", link.GetAttribute("href"));
        Assert.AreEqual("Zu Seite 2", link.GetAttribute("aria-label"));
        Assert.IsNull(link.GetAttribute("tabindex"));
        // The hotspot sits outside the hidden painted layer, or nothing could reach it.
        Assert.IsNull(link.Closest("[aria-hidden='true']"));
    }

    [TestMethod]
    public void BitPdfViewerExternalLinksShouldBeNamedByTheirAddressAndWrappedOnesReachedOnce()
    {
        var bodies = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents 4 0 R /Annots [5 0 R] >>",
            TestPdf.Stream("BT ET"),
            // A link wrapped across two lines: one link, two quads.
            "<< /Type /Annot /Subtype /Link /Rect [10 10 190 60] " +
                "/QuadPoints [10 60 190 60 10 40 190 40 10 30 100 30 10 10 100 10] " +
                "/A << /S /URI /URI (https://bitplatform.dev) >> >>",
        };

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.Build(bodies, rootObjNum: 1)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll("[data-page='1'] .bit-pdv-html-page").Count));

        var links = component.FindAll(".bit-pdv-html-page a[href='https://bitplatform.dev']");

        Assert.AreEqual(2, links.Count);
        Assert.AreEqual("https://bitplatform.dev", links[0].GetAttribute("title"));
        Assert.IsNull(links[0].GetAttribute("tabindex"));
        Assert.AreEqual("-1", links[1].GetAttribute("tabindex"));
        Assert.AreEqual("true", links[1].GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public async Task BitPdfViewerThumbnailsShouldHoldNoLinks()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.WithInternalLink()));
            parameters.Add(p => p.DefaultSidebar, BitPdfSidebar.Thumbnails);
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.FindAll("[data-thumb]").Count));

        // The sidebar's spy asks for the thumbnails it scrolls into view; there is no browser here to ask.
        await component.InvokeAsync(() => component.Instance.EnsureThumbsRendered([1]));
        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll("[data-thumb='1'] .bit-pdv-html-page").Count));

        // An option of a listbox with a tab stop inside it is a tab stop nobody can see: what the thumbnail shows is
        // inert, whether it was built for the sidebar or reused from the page.
        Assert.IsTrue(component.FindAll("[data-thumb] a").All(a => a.Closest("[inert]") is not null));
        Assert.IsNotNull(component.Find("[data-thumb='1'] .bit-pdv-thumb-svg").GetAttribute("inert"));
        Assert.AreEqual(1, component.FindAll(".bit-pdv-surface a[data-bit-pdv-page]").Count);
    }

    [TestMethod]
    public void BitPdfViewerShouldAnnounceAFailedLoadAsAnAlert()
    {
        var errors = new List<string>();

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes("%PDF-1.7 this is not a pdf"u8.ToArray(), "broken.pdf"));
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<string>(this, e => errors.Add(e)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, errors.Count));

        var alert = component.Find(".bit-pdv-error");
        Assert.AreEqual("alert", alert.GetAttribute("role"));
        StringAssert.Contains(alert.TextContent, errors[0]);
    }

    [TestMethod]
    public void BitPdfViewerErrorTemplateShouldReceiveTheMessageOnErrorReceives()
    {
        var errors = new List<string>();

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes("%PDF-1.7 this is not a pdf"u8.ToArray()));
            parameters.Add(p => p.OnError, EventCallback.Factory.Create<string>(this, e => errors.Add(e)));
            parameters.Add(p => p.ErrorTemplate, (RenderFragment<string>)(message => builder =>
                builder.AddMarkupContent(0, $"<span class=\"custom-error\">{message}</span>")));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, errors.Count));

        var alert = component.Find(".bit-pdv-error");
        Assert.AreEqual("alert", alert.GetAttribute("role"));
        Assert.AreEqual(errors[0], alert.QuerySelector(".custom-error")!.TextContent);

        // A document that does open takes the failure's place.
        component.Render(parameters => parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld())));

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));
        Assert.AreEqual(0, component.FindAll(".bit-pdv-error").Count);
    }

    [TestMethod]
    public void BitPdfViewerEmptyTemplateShouldReplaceTheNoDocumentText()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.EmptyTemplate, (RenderFragment)(builder =>
                builder.AddMarkupContent(0, "<span class=\"custom-empty\">Drop a pdf here</span>")));
        });

        var empty = component.Find(".bit-pdv-empty");

        Assert.IsNotNull(empty.QuerySelector(".custom-empty"));
        Assert.IsFalse(empty.TextContent.Contains("No document loaded."));
        Assert.AreEqual(0, component.FindAll(".bit-pdv-error").Count);
    }

    [TestMethod]
    public void BitPdfViewerLoadingTemplateShouldReplaceThePlaceholderWhileTheDocumentLoads()
    {
        // A response held open keeps the viewer loading for as long as the test needs.
        var response = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        Services.AddSingleton(new HttpClient(new HeldHandler(response.Task)) { BaseAddress = new Uri("https://localhost/") });

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromUrl("doc.pdf"));
            parameters.Add(p => p.LoadingTemplate, (RenderFragment)(builder =>
                builder.AddMarkupContent(0, "<span class=\"custom-loading\">Opening</span>")));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".custom-loading").Count));
        Assert.AreEqual(0, component.FindAll(".bit-pdv-page-loading").Count);
        // The template replaces the placeholder, not the loading bar or the busy state.
        Assert.AreEqual(1, component.FindAll(".bit-pdv-progress").Count);
        Assert.AreEqual("true", component.Find(".bit-pdv-surface").GetAttribute("aria-busy"));

        response.SetResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(TestPdf.HelloWorld()) });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));
        Assert.AreEqual(0, component.FindAll(".custom-loading").Count);
    }

    private sealed class HeldHandler(Task<HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => response;
    }

    [TestMethod]
    [DataRow(true, 1)]
    [DataRow(false, 0)]
    public async Task BitPdfViewerShouldMoveFocusOntoTheSurfaceWhenTheFocusedSidebarCloses(bool sidebarHasFocus, int focusCalls)
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.PdfViewer.sidebarHasFocus", _ => true).SetResult(sidebarHasFocus);

        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(2)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.PageCount));
        await component.InvokeAsync(() => component.Instance.OnShortcut("sidebar"));
        int before = Context.JSInterop.Invocations["BitBlazorUI.PdfViewer.focus"].Count;

        // F4 from a thumbnail takes the panel - and the focus inside it - out of the DOM.
        await component.InvokeAsync(() => component.Instance.OnShortcut("sidebar"));

        Assert.AreEqual(BitPdfSidebar.None, component.Instance.Sidebar);
        // Focus the reader had elsewhere (a host button, say) is not taken from them.
        component.WaitForAssertion(() =>
            Assert.AreEqual(before + focusCalls, Context.JSInterop.Invocations["BitBlazorUI.PdfViewer.focus"].Count));
    }

    [TestMethod]
    public void BitPdfViewerToolbarShouldExposeTheShortcutsItHandles()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(2)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.PageCount));

        Assert.AreEqual("Control+F", component.Find("button[aria-label='Find in document']").GetAttribute("aria-keyshortcuts"));
        Assert.AreEqual("Control+P", component.Find("button[aria-label='Print document']").GetAttribute("aria-keyshortcuts"));
        Assert.AreEqual("N J PageDown", component.Find("button[aria-label='Next page']").GetAttribute("aria-keyshortcuts"));
        Assert.AreEqual("F4", component.Find("button[aria-label='Page thumbnails']").GetAttribute("aria-keyshortcuts"));

        // A key the viewer no longer answers to is not announced.
        component.Render(parameters => parameters.Add(p => p.EnableKeyboardShortcuts, false));

        Assert.AreEqual(0, component.FindAll("[aria-keyshortcuts]").Count);
    }

    [TestMethod]
    public void BitPdfViewerPageBoxShouldBeDescribedByThePageCountInWords()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(3)));
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.Instance.PageCount));

        var box = component.Find(".bit-pdv-page-input");
        var description = component.Find($"#{box.GetAttribute("aria-describedby")}");

        Assert.AreEqual("3 page(s).", description.TextContent);
        // The visible "/ 3" would be read again, as "slash three".
        Assert.AreEqual("true", component.Find(".bit-pdv-page-total").GetAttribute("aria-hidden"));
    }
}
