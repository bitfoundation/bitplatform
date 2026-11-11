using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

[TestClass]
public partial class BitPdfViewerStreamTests : BunitTestContext
{
    [TestMethod]
    public async Task StreamViewerShouldPreserveFeaturesAndReturnOnlyExplicitUnretainedCopies()
    {
        byte[] bytes = TestPdf.HelloWorld();
        var input = new PdfTrackingStream(new byte[23].Concat(bytes).ToArray(), shortRead: 7) { Position = 23 };
        int factories = 0;
        var source = BitPdfSource.FromStreamFactory(_ =>
        {
            factories++;
            return Task.FromResult<Stream>(input);
        }).WithFileName("hello.pdf").WithPassword(null).WithHeaders(new Dictionary<string, string>());
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, source));
        component.WaitForPages(1);

        Assert.IsNull(component.Instance.GetBytes());
        Assert.AreEqual(bytes.Length, component.Instance.FileSize);
        Assert.AreEqual(1, factories);
        Assert.IsFalse(component.Find("button[aria-label='Download document']").HasAttribute("disabled"));
        Assert.Contains("Hello", component.Markup);
        Assert.ThrowsExactly<InvalidOperationException>(() => component.Instance.RenderPageHtml(1));
        Assert.ThrowsExactly<InvalidOperationException>(() => component.Instance.ExtractPageText(1));
        Assert.ThrowsExactly<InvalidOperationException>(() => component.Instance.ExtractText());

        string? html = null, text = null;
        byte[]? copy = null, second = null;
        await component.InvokeAsync(async () =>
        {
            html = await component.Instance.RenderPageHtmlAsync(1, TestContext.CancellationToken);
            text = await component.Instance.ExtractTextAsync(cancellationToken: TestContext.CancellationToken);
            copy = await component.Instance.GetBytesAsync(TestContext.CancellationToken);
            second = await component.Instance.GetBytesAsync(TestContext.CancellationToken);
            await component.Instance.Search("Hello");
        });
        Assert.Contains("Hello", html!);
        Assert.Contains("Hello", text!);
        Assert.AreSequenceEqual(bytes, copy!);
        Assert.AreSequenceEqual(bytes, second!);
        Assert.AreNotSame(copy, second);
        Assert.IsNull(component.Instance.GetBytes());
        Assert.AreEqual(1, component.Instance.SearchMatchCount);
        Assert.IsEmpty(component.Instance.FailedPages);
        Assert.AreEqual(0, input.DisposeCount);
        await component.InvokeAsync(() => component.Instance.OpenAsync(null));
        Assert.AreEqual(1, input.DisposeCount);
        Assert.AreEqual(0L, component.Instance.FileSize);
    }

    [TestMethod]
    public async Task StreamDownloadShouldCopyFromOriginWithoutMaterializingAndRespectRenameAndCancel()
    {
        byte[] bytes = TestPdf.HelloWorld();
        var input = new PdfTrackingStream(new byte[11].Concat(bytes).ToArray(), shortRead: 5) { Position = 11 };
        bool cancel = false;
        var planned = Context.JSInterop.SetupVoid("BitBlazorUI.PdfViewer.download", _ => true);
        var component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.Source, BitPdfSource.FromStreamFactory(_ => Task.FromResult<Stream>(input), "hello.pdf"))
            .Add(v => v.OnDownloading, EventCallback.Factory.Create<BitPdfDownloadArgs>(this, args =>
            {
                args.FileName = "renamed.pdf";
                args.Cancel = cancel;
            })));
        component.WaitForPages(1);
        var download = component.InvokeAsync(() => component.Instance.Download());
        component.WaitForAssertion(() => Assert.AreEqual(1, planned.Invocations.Count));
        var invocation = planned.Invocations.Single();
        Assert.AreEqual("renamed.pdf", invocation.Arguments[0]);
        var reference = (DotNetStreamReference)invocation.Arguments[1]!;
        Assert.IsFalse(reference.Stream is MemoryStream);
        using var copied = new MemoryStream();
        await reference.Stream.CopyToAsync(copied, TestContext.CancellationToken);
        planned.SetVoidResult();
        await download;
        Assert.AreSequenceEqual(bytes, copied.ToArray());
        Assert.IsNull(component.Instance.GetBytes());
        Assert.AreEqual(0, input.DisposeCount);
        cancel = true;
        await component.InvokeAsync(() => component.Instance.Download());
        Assert.ContainsSingle(Context.InvocationsOf("BitBlazorUI.PdfViewer.download"));
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task StreamDownloadFailureShouldNotifyAfterCleanupAndAllowReentrantWork(bool replace, bool pendingRender)
    {
        var input = new PdfTrackingStream(TestPdf.HelloWorld());
        var errors = new List<string>();
        BitPdfViewer? viewer = null;
        int attempts = 0;
        bool cancel = false;
        string? text = null;
        // WaitForPages observes markup, not completion of OnAfterRenderAsync.
        // bUnit's handler list must be configured before rendering can read it.
        var planned = Context.JSInterop.SetupVoid("BitBlazorUI.PdfViewer.download", _ => true);
        var widths = Context.JSInterop.SetupVoid("BitBlazorUI.PdfViewer.correctTextWidths", _ => true);
        if (!pendingRender) widths.SetVoidResult();
        try
        {
            var component = RenderComponent<BitPdfViewer>(p => p
                .Add(v => v.Source, BitPdfSource.FromStreamFactory(_ => Task.FromResult<Stream>(input)))
                .Add(v => v.OnDownloading, EventCallback.Factory.Create<BitPdfDownloadArgs>(this, args =>
                {
                    attempts++;
                    args.Cancel = cancel;
                }))
                .Add(v => v.OnError, EventCallback.Factory.Create<string>(this, async message =>
                {
                    errors.Add(message);
                    input.FailRead = false;
                    if (replace)
                        await viewer!.OpenAsync(BitPdfSource.FromBytes(TestPdf.MultiPage(2))).WaitAsync(TimeSpan.FromSeconds(3), TestContext.CancellationToken);
                    else
                        text = await viewer!.ExtractPageTextAsync(1, TestContext.CancellationToken).WaitAsync(TimeSpan.FromSeconds(3), TestContext.CancellationToken);
                    // The busy flag must also be cleared before entering host code.
                    cancel = true;
                    await viewer.Download();
                })));
            component.WaitForPages(1);
            if (pendingRender)
            {
                // Keep post-render interop pending throughout the failed download and
                // host callback, so the overlap from CI is exercised deliberately.
                component.WaitForAssertion(() => Assert.AreEqual(1, widths.Invocations.Count));
            }
            viewer = component.Instance;
            input.FailRead = true;
            var download = component.InvokeAsync(() => viewer.Download());
            component.WaitForAssertion(() => Assert.AreEqual(1, planned.Invocations.Count));
            var reference = (DotNetStreamReference)planned.Invocations.Single().Arguments[1]!;
            using var copied = new MemoryStream();
            var failure = await Assert.ThrowsAsync<IOException>(() => reference.Stream.CopyToAsync(copied, TestContext.CancellationToken));
            // Browser arrayBuffer rejects when its .NET stream read fails.
            planned.SetException(new JSException(failure.Message));
            await download.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);

            component.WaitForPages(replace ? 2 : 1);
            Assert.AreSequenceEqual(new[] { failure.Message }, errors);
            Assert.AreEqual(2, attempts);
            Assert.AreEqual(1, planned.Invocations.Count);
            Assert.AreEqual(replace ? 1 : 0, input.DisposeCount);
            Assert.IsEmpty(viewer.FailedPages);
            if (!replace) Assert.Contains("Hello", text!);
        }
        finally
        {
            widths.SetVoidResult();
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task DownloadHostCallbackExceptionsShouldStillPropagate(bool streamed)
    {
        var failure = new InvalidOperationException("Host download callback failed.");
        int attempts = 0, errors = 0;
        var source = streamed
            ? BitPdfSource.FromStreamFactory(_ => Task.FromResult<Stream>(new PdfTrackingStream(TestPdf.HelloWorld())))
            : BitPdfSource.FromBytes(TestPdf.HelloWorld());
        var component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.Source, source)
            .Add(v => v.OnError, EventCallback.Factory.Create<string>(this, _ => errors++))
            .Add(v => v.OnDownloading, EventCallback.Factory.Create<BitPdfDownloadArgs>(this, args =>
            {
                if (++attempts == 1) throw failure;
                args.Cancel = true;
            })));
        component.WaitForPages(1);
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => component.InvokeAsync(() => component.Instance.Download()));
        Assert.AreSame(failure, thrown);
        await component.InvokeAsync(() => component.Instance.Download());
        Assert.AreEqual(2, attempts);
        Assert.AreEqual(0, errors);
        Assert.IsEmpty(Context.InvocationsOf("BitBlazorUI.PdfViewer.download"));
    }

    [TestMethod]
    public async Task BufferedDownloadInteropExceptionsShouldStillPropagate()
    {
        int errors = 0;
        var failure = new JSException("Browser download failed.");
        Context.JSInterop.SetupVoid("BitBlazorUI.PdfViewer.download", _ => true).SetException(failure);
        var component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()))
            .Add(v => v.OnError, EventCallback.Factory.Create<string>(this, _ => errors++)));
        component.WaitForPages(1);
        var thrown = await Assert.ThrowsAsync<JSException>(() => component.InvokeAsync(() => component.Instance.Download()));
        Assert.AreSame(failure, thrown);
        Assert.AreEqual(0, errors);
    }

    [TestMethod]
    public async Task StreamDownloadCancellationShouldNotBeReportedAsFailure()
    {
        int errors = 0;
        Context.JSInterop.SetupVoid("BitBlazorUI.PdfViewer.download", _ => true).SetException(new OperationCanceledException());
        var component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.Source, BitPdfSource.FromStreamFactory(_ => Task.FromResult<Stream>(new PdfTrackingStream(TestPdf.HelloWorld()))))
            .Add(v => v.OnError, EventCallback.Factory.Create<string>(this, _ => errors++)));
        component.WaitForPages(1);
        await Assert.ThrowsAsync<OperationCanceledException>(() => component.InvokeAsync(() => component.Instance.Download()));
        Assert.AreEqual(0, errors);
        Assert.IsEmpty(component.Instance.FailedPages);
    }

    [TestMethod]
    public async Task StreamFactoryShouldOpenOnceForPasswordRetriesAndFreshForEveryViewerLoad()
    {
        var opened = new List<PdfTrackingStream>();
        var source = BitPdfSource.FromStreamFactory(_ =>
        {
            var stream = new PdfTrackingStream(TestPdf.Encrypted("hunter2"));
            opened.Add(stream);
            return Task.FromResult<Stream>(stream);
        });
        int passwords = 0;
        var first = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.Source, source)
            .Add(v => v.OnPasswordRequested, () => Task.FromResult<string?>(++passwords == 1 ? "wrong" : "hunter2")));
        first.WaitForPages(1);
        Assert.AreEqual(2, passwords);
        Assert.ContainsSingle(opened);
        var second = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, source.WithPassword("hunter2")));
        second.WaitForPages(1);
        Assert.HasCount(2, opened);
        await first.InvokeAsync(() => first.Instance.OpenAsync(null));
        Assert.AreEqual(1, opened[0].DisposeCount);
        Assert.AreEqual(0, opened[1].DisposeCount);
        await first.InvokeAsync(() => first.Instance.OpenAsync(source.WithPassword("hunter2")));
        first.WaitForPages(1);
        Assert.HasCount(3, opened);
        Assert.AreEqual(0, opened[2].DisposeCount);
    }

    [TestMethod]
    [DataRow("null")]
    [DataRow("not-readable")]
    [DataRow("not-seekable")]
    [DataRow("too-large")]
    [DataRow("invalid-pdf")]
    public void StreamFactoryFailuresShouldSurfaceOnceAndDisposeReturnedHandles(string failure)
    {
        var input = new PdfTrackingStream(failure == "invalid-pdf" ? [] : TestPdf.HelloWorld());
        if (failure == "not-readable") input.Readable = false;
        if (failure == "not-seekable") input.Seekable = false;
        if (failure == "too-large") input.LengthOverride = (long)int.MaxValue + 1;
        int errors = 0;
        var component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.Source, BitPdfSource.FromStreamFactory(_ => Task.FromResult<Stream>(failure == "null" ? null! : input)))
            .Add(v => v.OnError, EventCallback.Factory.Create<string>(this, _ => errors++)));
        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, errors);
            Assert.IsNull(component.Instance.Document);
            Assert.AreEqual(0L, component.Instance.FileSize);
            Assert.AreEqual("false", component.Find(".bit-pdv-surface").GetAttribute("aria-busy"));
            if (failure != "null") Assert.AreEqual(1, input.DisposeCount);
        });
        if (failure == "null") input.Dispose();
    }

    [TestMethod]
    public async Task SupersededFactoryShouldReceiveCancellationAndDisposeItsLateResultWithoutErrors()
    {
        var result = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        int errors = 0;
        var component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.Source, BitPdfSource.FromStreamFactory(token => { entered.TrySetResult(token); return result.Task; }))
            .Add(v => v.OnError, EventCallback.Factory.Create<string>(this, _ => errors++)));
        var token = await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        await component.InvokeAsync(() => component.Instance.OpenAsync(BitPdfSource.FromBytes(TestPdf.MultiPage(2))));
        component.WaitForPages(2);
        Assert.IsTrue(token.IsCancellationRequested);
        var late = new PdfTrackingStream(TestPdf.HelloWorld());
        result.SetResult(late);
        component.WaitForAssertion(() => Assert.AreEqual(1, late.DisposeCount));
        Assert.AreEqual(0, errors);
        Assert.AreEqual(2, component.Instance.PageCount);
        Assert.IsEmpty(component.Instance.FailedPages);
    }

    [TestMethod]
    public async Task SupersedingAnActiveParseShouldCancelWithoutRecoveryOrPageFailures()
    {
        using var release = new ManualResetEventSlim();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var input = new PdfTrackingStream(TestPdf.HelloWorld());
        int errors = 0;
        input.OnRead = () =>
        {
            started.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(5), TestContext.CancellationToken)) throw new IOException("Test read timed out.");
        };
        var component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.Source, BitPdfSource.FromStreamFactory(token =>
            {
                token.Register(() => cancelled.TrySetResult());
                return Task.FromResult<Stream>(input);
            }))
            .Add(v => v.OnError, EventCallback.Factory.Create<string>(this, _ => errors++)));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        var replacement = component.InvokeAsync(() => component.Instance.OpenAsync(BitPdfSource.FromBytes(TestPdf.MultiPage(2))));
        try { await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken); }
        finally { release.Set(); }
        await replacement;
        component.WaitForPages(2);
        Assert.AreEqual(1, input.DisposeCount);
        Assert.IsEmpty(component.Instance.FailedPages);
        Assert.AreEqual(0, errors);
        Assert.AreEqual(1, input.ReadCount);
    }

    [TestMethod]
    public async Task DisposingDuringFactoryOpenShouldCancelAndDisposeTheLateStream()
    {
        var result = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        int loaded = 0, errors = 0;
        var component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.Source, BitPdfSource.FromStreamFactory(token => { entered.TrySetResult(token); return result.Task; }))
            .Add(v => v.OnDocumentLoaded, EventCallback.Factory.Create(this, () => loaded++))
            .Add(v => v.OnError, EventCallback.Factory.Create<string>(this, _ => errors++)));
        var token = await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        await component.InvokeAsync(async () => await component.Instance.DisposeAsync());
        Assert.IsTrue(token.IsCancellationRequested);
        var late = new PdfTrackingStream(TestPdf.HelloWorld());
        result.SetResult(late);
        component.WaitForAssertion(() => Assert.AreEqual(1, late.DisposeCount));
        Assert.AreEqual(0, loaded);
        Assert.AreEqual(0, errors);
    }

    [TestMethod]
    public async Task ReplacementShouldInvalidateRetainedDocumentAndRawStreamsAndPreserveBufferedHelpers()
    {
        var input = new PdfTrackingStream(TestPdf.HelloWorld());
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source,
            BitPdfSource.FromStreamFactory(_ => Task.FromResult<Stream>(input))));
        component.WaitForPages(1);
        var old = component.Instance.Document!;
        var page = old.Pages[0];
        var raw = (BitPdfBaseStream)page.Dict.Get("Contents")!;
        _ = old.Metadata;
        byte[] replacement = TestPdf.MultiPage(2);
        await component.InvokeAsync(() => component.Instance.OpenAsync(BitPdfSource.FromBytes(replacement)));
        component.WaitForPages(2);
        Assert.AreEqual(1, input.DisposeCount);
        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = old.Pages);
        Assert.ThrowsExactly<ObjectDisposedException>(() => raw.GetByte());
        Assert.AreSame(replacement, component.Instance.GetBytes());
        Assert.AreSame(replacement, await component.Instance.GetBytesAsync(TestContext.CancellationToken));
        Assert.Contains("Page", component.Instance.ExtractText());
        Assert.IsFalse(string.IsNullOrEmpty(component.Instance.RenderPageHtml(1)));
    }

    [TestMethod]
    [DataRow(BitPdfRenderMode.Html)]
    [DataRow(BitPdfRenderMode.Canvas)]
    public async Task ServerStreamReadsShouldRunOutsideDispatcherWithBackgroundRenderingDisabled(BitPdfRenderMode mode)
    {
        var input = new PdfTrackingStream(TestPdf.MultiPage(4));
        int readsOnDispatcher = 0;
        input.OnRead = () => { if (Renderer.Dispatcher.CheckAccess()) Interlocked.Increment(ref readsOnDispatcher); };
        var component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.BackgroundRendering, false)
            .Add(v => v.RenderMode, mode)
            .Add(v => v.Source, BitPdfSource.FromStreamFactory(_ => Task.FromResult<Stream>(input))));
        component.WaitForPages(4);
        await component.InvokeAsync(async () =>
        {
            await component.Instance.GoToPage(4);
            await component.Instance.ShowSidebar(BitPdfSidebar.Thumbnails);
            await component.Instance.ExtractTextAsync(cancellationToken: TestContext.CancellationToken);
            await component.Instance.RenderPageHtmlAsync(3, TestContext.CancellationToken);
            await component.Instance.Search("Page");
            await component.Instance.Print();
            await component.Instance.GetBytesAsync(TestContext.CancellationToken);
        });
        Assert.AreEqual(0, readsOnDispatcher);
        Assert.IsGreaterThan(0, input.ReadCount);
        Assert.IsEmpty(component.Instance.FailedPages);
        Assert.ContainsSingle(Context.InvocationsOf("BitBlazorUI.PdfViewer.print"));
    }

    [TestMethod]
    public async Task FromStreamAsyncShouldRemainAnIndependentSnapshotWithoutOwnershipTransfer()
    {
        byte[] bytes = TestPdf.HelloWorld();
        var input = new MemoryStream(new byte[9].Concat(bytes).ToArray(), writable: true) { Position = 9 };
        var source = await BitPdfSource.FromStreamAsync(input, "snapshot.pdf", TestContext.CancellationToken);
        Assert.IsTrue(input.CanRead);
        input.Position = 9;
        input.WriteByte(0);
        input.Dispose();
        Assert.AreSequenceEqual(bytes, source.Bytes!);
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, source));
        component.WaitForPages(1);
        Assert.Contains("Hello", component.Instance.ExtractText());
    }

    [TestMethod]
    public async Task AsyncHelpersShouldHonorCallerCancellationWithoutFailingPages()
    {
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source,
            BitPdfSource.FromStreamFactory(_ => Task.FromResult<Stream>(new PdfTrackingStream(TestPdf.HelloWorld())))));
        component.WaitForPages(1);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => component.Instance.GetBytesAsync(cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => component.Instance.RenderPageHtmlAsync(1, cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => component.Instance.ExtractPageTextAsync(1, cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => component.Instance.ExtractTextAsync(cancellationToken: cancellation.Token));
        Assert.IsEmpty(component.Instance.FailedPages);
        Assert.IsNull(component.Instance.GetBytes());
    }

    [TestMethod]
    public async Task SearchInputAndOptionsShouldReportLaterStreamFailuresAndAllowRetry()
    {
        var input = new PdfTrackingStream(PdfWithUncachedPageContent());
        var errors = new List<string>();
        var component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.Source, BitPdfSource.FromStreamFactory(_ => Task.FromResult<Stream>(input)))
            .Add(v => v.OnError, EventCallback.Factory.Create<string>(this, errors.Add)));
        component.WaitForPages(3);
        await component.InvokeAsync(() => component.Instance.OnShortcut("find"));
        input.FailRead = true;

        await component.Find(".bit-pdv-search-input").InputAsync(new ChangeEventArgs { Value = "Page" });
        AssertSearchFailed(1);
        await component.Find("button[aria-label='Match case']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        AssertSearchFailed(2);

        input.FailRead = false;
        await component.Find(".bit-pdv-search-input").InputAsync(new ChangeEventArgs { Value = "Page" });
        Assert.AreEqual(3, component.Instance.SearchMatchCount);
        Assert.HasCount(2, errors);
        Assert.IsEmpty(component.Instance.FailedPages);

        void AssertSearchFailed(int count)
        {
            Assert.HasCount(count, errors);
            Assert.Contains("Reading the PDF stream failed", errors[^1]);
            Assert.AreEqual("false", component.Find(".bit-pdv-surface").GetAttribute("aria-busy"));
            Assert.AreEqual(0, component.Instance.SearchMatchCount);
            Assert.AreEqual(0, component.Instance.SearchMatchIndex);
            Assert.IsEmpty(component.Instance.FailedPages);
            Assert.IsNotNull(component.Instance.Document);
        }
    }

    [TestMethod]
    public async Task SearchFailureHandlerShouldBeAbleToReplaceTheDocument()
    {
        var input = new PdfTrackingStream(PdfWithUncachedPageContent());
        BitPdfViewer? viewer = null;
        int errors = 0;
        var component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.Source, BitPdfSource.FromStreamFactory(_ => Task.FromResult<Stream>(input)))
            .Add(v => v.OnError, EventCallback.Factory.Create<string>(this, async _ =>
            {
                errors++;
                await viewer!.OpenAsync(BitPdfSource.FromBytes(TestPdf.HelloWorld()));
            })));
        component.WaitForPages(3);
        viewer = component.Instance;
        input.FailRead = true;
        await component.InvokeAsync(() => viewer.Search("Page")).WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        component.WaitForPages(1);
        Assert.AreEqual(1, errors);
        Assert.AreEqual(1, input.DisposeCount);
        Assert.IsEmpty(viewer.FailedPages);
    }

    [TestMethod]
    public async Task SupersededSearchReadFailureShouldNotReportAnErrorOrChangeTheReplacement()
    {
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var input = new PdfTrackingStream(PdfWithUncachedPageContent());
        int errors = 0;
        var component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.Source, BitPdfSource.FromStreamFactory(token =>
            {
                token.Register(() => cancelled.TrySetResult());
                return Task.FromResult<Stream>(input);
            }))
            .Add(v => v.OnError, EventCallback.Factory.Create<string>(this, _ => errors++)));
        component.WaitForPages(3);
        input.OnRead = () =>
        {
            entered.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(5), TestContext.CancellationToken)) throw new IOException("Test read timed out.");
        };
        input.FailRead = true;
        var search = component.InvokeAsync(() => component.Instance.Search("Page"));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        var replacement = component.InvokeAsync(() => component.Instance.OpenAsync(BitPdfSource.FromBytes(TestPdf.HelloWorld())));
        try { await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken); }
        finally { release.Set(); }
        await Task.WhenAll(search, replacement).WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        component.WaitForPages(1);
        Assert.AreEqual(0, errors);
        Assert.IsEmpty(component.Instance.FailedPages);
        Assert.AreEqual(0, component.Instance.SearchMatchCount);
        Assert.AreEqual(1, input.DisposeCount);
    }

    [TestMethod]
    [DataRow(false, BitPdfRenderMode.Html)]
    [DataRow(false, BitPdfRenderMode.Canvas)]
    [DataRow(true, BitPdfRenderMode.Html)]
    [DataRow(true, BitPdfRenderMode.Canvas)]
    public async Task StreamCopiesShouldPauseAutomaticPageAndThumbnailRendering(bool download, BitPdfRenderMode mode)
    {
        byte[] bytes = TestPdf.MultiPage(3); // All content is cached, so even cache hits must wait for the copy.
        var input = new PausedAsyncPdfStream(bytes);
        var errors = new List<string>();
        var rendered = new List<int>();
        var planned = Context.JSInterop.SetupVoid("BitBlazorUI.PdfViewer.download", _ => true);
        var component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.BackgroundRendering, false)
            .Add(v => v.RenderMode, mode)
            .Add(v => v.Source, BitPdfSource.FromStreamFactory(_ => Task.FromResult<Stream>(input)))
            .Add(v => v.OnError, EventCallback.Factory.Create<string>(this, errors.Add))
            .Add(v => v.OnPageRendered, EventCallback.Factory.Create<int>(this, rendered.Add)));
        component.WaitForPages(3);
        using var copied = new MemoryStream();
        byte[]? result = null;
        var operation = component.InvokeAsync(async () =>
        {
            if (download) await component.Instance.Download();
            else result = await component.Instance.GetBytesAsync(TestContext.CancellationToken);
        });
        DotNetStreamReference? reference = null;
        if (download)
        {
            component.WaitForAssertion(() => Assert.AreEqual(1, planned.Invocations.Count));
            reference = (DotNetStreamReference)planned.Invocations.Single().Arguments[1]!;
        }
        else await input.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        var pages = component.InvokeAsync(() => component.Instance.EnsurePagesRendered([2]));
        var thumbs = component.InvokeAsync(() => component.Instance.EnsureThumbsRendered([3]));
        try
        {
            // Downloads also wait while JS has not yet requested its first bytes:
            // serialization must come from the scheduler, not a blocking server I/O wait.
            await Task.Delay(100, TestContext.CancellationToken);
            Assert.IsFalse(pages.IsCompleted, "The page pump must wait asynchronously for the copy.");
            Assert.IsFalse(thumbs.IsCompleted, "The thumbnail pump must wait asynchronously for the copy.");
            Assert.AreSequenceEqual(new[] { 1 }, rendered);
        }
        finally
        {
            input.Resume.TrySetResult();
            if (reference is not null) await reference.Stream.CopyToAsync(copied, TestContext.CancellationToken);
            planned.SetVoidResult();
            await operation;
            await Task.WhenAll(pages, thumbs).WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        }
        Assert.AreSequenceEqual(bytes, download ? copied.ToArray() : result!);
        Assert.AreSequenceEqual(new[] { 1, 2 }, rendered);
        Assert.IsEmpty(errors);
        Assert.IsEmpty(component.Instance.FailedPages);
        Assert.IsNull(component.Instance.GetBytes());
    }

    [TestMethod]
    public async Task SupersedingADownloadShouldReleaseTheRenderSchedulerWhileInteropIsPending()
    {
        var input = new PausedAsyncPdfStream(TestPdf.MultiPage(3));
        int errors = 0;
        var planned = Context.JSInterop.SetupVoid("BitBlazorUI.PdfViewer.download", _ => true);
        var component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.Source, BitPdfSource.FromStreamFactory(_ => Task.FromResult<Stream>(input)))
            .Add(v => v.OnError, EventCallback.Factory.Create<string>(this, _ => errors++)));
        component.WaitForPages(3);
        var download = component.InvokeAsync(() => component.Instance.Download());
        component.WaitForAssertion(() => Assert.AreEqual(1, planned.Invocations.Count));
        var reference = (DotNetStreamReference)planned.Invocations.Single().Arguments[1]!;
        using var copied = new MemoryStream();
        var transfer = reference.Stream.CopyToAsync(copied, TestContext.CancellationToken);
        await input.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        try
        {
            await component.InvokeAsync(() => component.Instance.OpenAsync(BitPdfSource.FromBytes(TestPdf.HelloWorld())))
                .WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
            await download.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
            await Assert.ThrowsAsync<OperationCanceledException>(() => transfer);
            component.WaitForPages(1);
            Assert.AreEqual(1, input.DisposeCount);
            Assert.AreEqual(0, errors);
            Assert.IsEmpty(component.Instance.FailedPages);
        }
        finally
        {
            input.Resume.TrySetResult();
            planned.SetVoidResult();
        }
    }

    private static byte[] PdfWithUncachedPageContent()
    {
        // Keep page dictionaries at the head and content for later pages away from both
        // header and xref blocks. Loading and the eager first render must not read it.
        string padding = TestPdf.Stream(new string(' ', 128 * 1024));
        return TestPdf.Build([
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [4 0 R 5 0 R 6 0 R] /Count 3 >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            Page(7), Page(9), Page(9),
            TestPdf.Stream("BT /F1 24 Tf 50 100 Td (Page one) Tj ET"),
            padding,
            TestPdf.Stream("BT /F1 24 Tf 50 100 Td (Page later) Tj ET"),
            padding,
        ], rootObjNum: 1);

        static string Page(int content) => "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] "
            + $"/Resources << /Font << /F1 3 0 R >> >> /Contents {content} 0 R >>";
    }

    private sealed class PausedAsyncPdfStream(byte[] bytes) : PdfTrackingStream(bytes)
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Resume { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Entered.TrySetResult();
            await Resume.Task.WaitAsync(cancellationToken);
            return await base.ReadAsync(buffer, cancellationToken);
        }
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
