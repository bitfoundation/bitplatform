using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

/// <summary>
/// Covers the two ways a document leaves the viewer - a download and a print - and what a host can say about them:
/// OnDownloading / OnPrinting run first whatever started them, and the shortcuts follow the toolbar's items.
/// </summary>
[TestClass]
public class BitPdfViewerDownloadPrintTests : BunitTestContext
{
    private const string DownloadCall = "BitBlazorUI.PdfViewer.download";
    private const string PrintCall = "BitBlazorUI.PdfViewer.print";

    [TestMethod]
    public async Task BitPdfViewerOnDownloadingShouldRenameTheDownload()
    {
        string? offered = null;
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld(), "hello.pdf"));
            parameters.Add(p => p.OnDownloading, EventCallback.Factory.Create<BitPdfDownloadArgs>(this, args =>
            {
                offered = args.FileName;
                args.FileName = "renamed.pdf";
            }));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.Download());

        Assert.AreEqual("hello.pdf", offered);
        Assert.AreEqual("renamed.pdf", Context.JSInterop.Invocations[DownloadCall].Single().Arguments[0]);
    }

    [TestMethod]
    public async Task BitPdfViewerOnDownloadingShouldKeepTheDefaultNameWhenBlanked()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld(), "hello.pdf"));
            parameters.Add(p => p.OnDownloading, EventCallback.Factory.Create<BitPdfDownloadArgs>(this, args => args.FileName = " "));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.Download());

        Assert.AreEqual("hello.pdf", Context.JSInterop.Invocations[DownloadCall].Single().Arguments[0]);
    }

    [TestMethod]
    public async Task BitPdfViewerOnDownloadingShouldCancelTheDownload()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
            parameters.Add(p => p.OnDownloading, EventCallback.Factory.Create<BitPdfDownloadArgs>(this, args => args.Cancel = true));
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount));

        // The toolbar button and the shortcut go through the same gate as the method.
        component.Find("button[aria-label='Download document']").Click();
        await component.InvokeAsync(() => component.Instance.OnShortcut("download"));
        await component.InvokeAsync(() => component.Instance.Download());

        Assert.AreEqual(0, Context.JSInterop.Invocations[DownloadCall].Count);
    }

    [TestMethod]
    public async Task BitPdfViewerOnPrintingShouldReportTheRangeAndCancelThePrint()
    {
        BitPdfPrintArgs? received = null;
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(5)));
            parameters.Add(p => p.OnPrinting, EventCallback.Factory.Create<BitPdfPrintArgs>(this, args =>
            {
                received = args;
                args.Cancel = true;
            }));
        });

        component.WaitForAssertion(() => Assert.AreEqual(5, component.Instance.PageCount));

        // The range arrives as it will be printed: reordered and clamped.
        await component.InvokeAsync(() => component.Instance.Print(4, 2));

        Assert.IsNotNull(received);
        Assert.AreEqual(2, received.FromPage);
        Assert.AreEqual(4, received.ToPage);
        Assert.AreEqual(0, Context.JSInterop.Invocations[PrintCall].Count);
        // A cancelled print renders nothing for it and leaves no loading bar behind.
        Assert.AreEqual(0, component.FindAll(".bit-pdv-progress").Count);
    }

    [TestMethod]
    public async Task BitPdfViewerOnPrintingShouldLetThePrintThroughWhenNotCancelled()
    {
        int raised = 0;
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(2)));
            parameters.Add(p => p.OnPrinting, EventCallback.Factory.Create<BitPdfPrintArgs>(this, _ => raised++));
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.OnShortcut("print"));

        Assert.AreEqual(1, raised);
        var call = Context.JSInterop.Invocations[PrintCall].Single();
        Assert.AreEqual(1, call.Arguments[1]);
        Assert.AreEqual(2, call.Arguments[2]);
    }

    [TestMethod]
    public async Task BitPdfViewerShortcutsShouldNotPrintOrDownloadWhatTheToolbarLeftOut()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(2)));
            parameters.Add(p => p.ToolbarItems, BitPdfToolbarItems.Navigation | BitPdfToolbarItems.Zoom);
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.OnShortcut("print"));
        await component.InvokeAsync(() => component.Instance.OnShortcut("download"));

        Assert.AreEqual(0, Context.JSInterop.Invocations[PrintCall].Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations[DownloadCall].Count);

        // The public API is the host's own call, and stays available.
        await component.InvokeAsync(() => component.Instance.Print());
        await component.InvokeAsync(() => component.Instance.Download());

        Assert.AreEqual(1, Context.JSInterop.Invocations[PrintCall].Count);
        Assert.AreEqual(1, Context.JSInterop.Invocations[DownloadCall].Count);
    }

    [TestMethod]
    public async Task BitPdfViewerShortcutsShouldPrintAndDownloadWithAHiddenToolbar()
    {
        // A hidden toolbar keeps its items: a host drawing its own chrome keeps the shortcuts.
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(2)));
            parameters.Add(p => p.ShowToolbar, false);
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Instance.PageCount));

        await component.InvokeAsync(() => component.Instance.OnShortcut("print"));
        await component.InvokeAsync(() => component.Instance.OnShortcut("download"));

        Assert.AreEqual(1, Context.JSInterop.Invocations[PrintCall].Count);
        Assert.AreEqual(1, Context.JSInterop.Invocations[DownloadCall].Count);
    }
}
