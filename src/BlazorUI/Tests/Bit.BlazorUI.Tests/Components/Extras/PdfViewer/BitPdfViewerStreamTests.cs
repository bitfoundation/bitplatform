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
public class BitPdfViewerStreamTests : BunitTestContext
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
        StringAssert.Contains(component.Markup, "Hello");
        Assert.ThrowsExactly<InvalidOperationException>(() => component.Instance.RenderPageHtml(1));
        Assert.ThrowsExactly<InvalidOperationException>(() => component.Instance.ExtractPageText(1));
        Assert.ThrowsExactly<InvalidOperationException>(() => component.Instance.ExtractText());

        string? html = null, text = null;
        byte[]? copy = null, second = null;
        await component.InvokeAsync(async () =>
        {
            html = await component.Instance.RenderPageHtmlAsync(1);
            text = await component.Instance.ExtractTextAsync();
            copy = await component.Instance.GetBytesAsync();
            second = await component.Instance.GetBytesAsync();
            await component.Instance.Search("Hello");
        });
        StringAssert.Contains(html!, "Hello");
        StringAssert.Contains(text!, "Hello");
        CollectionAssert.AreEqual(bytes, copy!);
        CollectionAssert.AreEqual(bytes, second!);
        Assert.AreNotSame(copy, second);
        Assert.IsNull(component.Instance.GetBytes());
        Assert.AreEqual(1, component.Instance.SearchMatchCount);
        Assert.AreEqual(0, component.Instance.FailedPages.Count);
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
        var component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.Source, BitPdfSource.FromStreamFactory(_ => Task.FromResult<Stream>(input), "hello.pdf"))
            .Add(v => v.OnDownloading, EventCallback.Factory.Create<BitPdfDownloadArgs>(this, args =>
            {
                args.FileName = "renamed.pdf";
                args.Cancel = cancel;
            })));
        component.WaitForPages(1);
        var planned = Context.JSInterop.SetupVoid("BitBlazorUI.PdfViewer.download", _ => true);
        var download = component.InvokeAsync(() => component.Instance.Download());
        component.WaitForAssertion(() => Assert.AreEqual(1, planned.Invocations.Count));
        var invocation = planned.Invocations.Single();
        Assert.AreEqual("renamed.pdf", invocation.Arguments[0]);
        var reference = (DotNetStreamReference)invocation.Arguments[1]!;
        Assert.IsFalse(reference.Stream is MemoryStream);
        using var copied = new MemoryStream();
        await reference.Stream.CopyToAsync(copied);
        planned.SetVoidResult();
        await download;
        CollectionAssert.AreEqual(bytes, copied.ToArray());
        Assert.IsNull(component.Instance.GetBytes());
        Assert.AreEqual(0, input.DisposeCount);
        cancel = true;
        await component.InvokeAsync(() => component.Instance.Download());
        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.PdfViewer.download"].Count);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task StreamDownloadFailureShouldNotifyAfterCleanupAndAllowReentrantWork(bool replace)
    {
        var input = new PdfTrackingStream(TestPdf.HelloWorld());
        var errors = new List<string>();
        BitPdfViewer? viewer = null;
        int attempts = 0;
        bool cancel = false;
        string? text = null;
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
                    await viewer!.OpenAsync(BitPdfSource.FromBytes(TestPdf.MultiPage(2))).WaitAsync(TimeSpan.FromSeconds(3));
                else
                    text = await viewer!.ExtractPageTextAsync(1).WaitAsync(TimeSpan.FromSeconds(3));
                // The busy flag must also be cleared before entering host code.
                cancel = true;
                await viewer.Download();
            })));
        component.WaitForPages(1);
        viewer = component.Instance;
        input.FailRead = true;
        var planned = Context.JSInterop.SetupVoid("BitBlazorUI.PdfViewer.download", _ => true);
        var download = component.InvokeAsync(() => viewer.Download());
        component.WaitForAssertion(() => Assert.AreEqual(1, planned.Invocations.Count));
        var reference = (DotNetStreamReference)planned.Invocations.Single().Arguments[1]!;
        using var copied = new MemoryStream();
        var failure = await Assert.ThrowsAsync<IOException>(() => reference.Stream.CopyToAsync(copied));
        // Browser arrayBuffer rejects when its .NET stream read fails.
        planned.SetException(new JSException(failure.Message));
        await download.WaitAsync(TimeSpan.FromSeconds(5));

        component.WaitForPages(replace ? 2 : 1);
        CollectionAssert.AreEqual(new[] { failure.Message }, errors);
        Assert.AreEqual(2, attempts);
        Assert.AreEqual(1, planned.Invocations.Count);
        Assert.AreEqual(replace ? 1 : 0, input.DisposeCount);
        Assert.AreEqual(0, viewer.FailedPages.Count);
        if (!replace) StringAssert.Contains(text!, "Hello");
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
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.PdfViewer.download"].Count);
    }

    [TestMethod]
    public async Task BufferedDownloadInteropExceptionsShouldStillPropagate()
    {
        int errors = 0;
        var component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()))
            .Add(v => v.OnError, EventCallback.Factory.Create<string>(this, _ => errors++)));
        component.WaitForPages(1);
        var failure = new JSException("Browser download failed.");
        Context.JSInterop.SetupVoid("BitBlazorUI.PdfViewer.download", _ => true).SetException(failure);
        var thrown = await Assert.ThrowsAsync<JSException>(() => component.InvokeAsync(() => component.Instance.Download()));
        Assert.AreSame(failure, thrown);
        Assert.AreEqual(0, errors);
    }

    [TestMethod]
    public async Task StreamDownloadCancellationShouldNotBeReportedAsFailure()
    {
        int errors = 0;
        var component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.Source, BitPdfSource.FromStreamFactory(_ => Task.FromResult<Stream>(new PdfTrackingStream(TestPdf.HelloWorld()))))
            .Add(v => v.OnError, EventCallback.Factory.Create<string>(this, _ => errors++)));
        component.WaitForPages(1);
        Context.JSInterop.SetupVoid("BitBlazorUI.PdfViewer.download", _ => true).SetException(new OperationCanceledException());
        await Assert.ThrowsAsync<OperationCanceledException>(() => component.InvokeAsync(() => component.Instance.Download()));
        Assert.AreEqual(0, errors);
        Assert.AreEqual(0, component.Instance.FailedPages.Count);
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
        Assert.AreEqual(1, opened.Count);
        var second = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, source.WithPassword("hunter2")));
        second.WaitForPages(1);
        Assert.AreEqual(2, opened.Count);
        await first.InvokeAsync(() => first.Instance.OpenAsync(null));
        Assert.AreEqual(1, opened[0].DisposeCount);
        Assert.AreEqual(0, opened[1].DisposeCount);
        await first.InvokeAsync(() => first.Instance.OpenAsync(source.WithPassword("hunter2")));
        first.WaitForPages(1);
        Assert.AreEqual(3, opened.Count);
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
        var token = await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await component.InvokeAsync(() => component.Instance.OpenAsync(BitPdfSource.FromBytes(TestPdf.MultiPage(2))));
        component.WaitForPages(2);
        Assert.IsTrue(token.IsCancellationRequested);
        var late = new PdfTrackingStream(TestPdf.HelloWorld());
        result.SetResult(late);
        component.WaitForAssertion(() => Assert.AreEqual(1, late.DisposeCount));
        Assert.AreEqual(0, errors);
        Assert.AreEqual(2, component.Instance.PageCount);
        Assert.AreEqual(0, component.Instance.FailedPages.Count);
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
            if (!release.Wait(TimeSpan.FromSeconds(5))) throw new IOException("Test read timed out.");
        };
        var component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.Source, BitPdfSource.FromStreamFactory(token =>
            {
                token.Register(() => cancelled.TrySetResult());
                return Task.FromResult<Stream>(input);
            }))
            .Add(v => v.OnError, EventCallback.Factory.Create<string>(this, _ => errors++)));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var replacement = component.InvokeAsync(() => component.Instance.OpenAsync(BitPdfSource.FromBytes(TestPdf.MultiPage(2))));
        try { await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5)); }
        finally { release.Set(); }
        await replacement;
        component.WaitForPages(2);
        Assert.AreEqual(1, input.DisposeCount);
        Assert.AreEqual(0, component.Instance.FailedPages.Count);
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
        var token = await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
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
        Assert.AreSame(replacement, await component.Instance.GetBytesAsync());
        StringAssert.Contains(component.Instance.ExtractText(), "Page");
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
            await component.Instance.ExtractTextAsync();
            await component.Instance.RenderPageHtmlAsync(3);
            await component.Instance.Search("Page");
            await component.Instance.Print();
            await component.Instance.GetBytesAsync();
        });
        Assert.AreEqual(0, readsOnDispatcher);
        Assert.IsTrue(input.ReadCount > 0);
        Assert.AreEqual(0, component.Instance.FailedPages.Count);
        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.PdfViewer.print"].Count);
    }

    [TestMethod]
    public async Task FromStreamAsyncShouldRemainAnIndependentSnapshotWithoutOwnershipTransfer()
    {
        byte[] bytes = TestPdf.HelloWorld();
        var input = new MemoryStream(new byte[9].Concat(bytes).ToArray(), writable: true) { Position = 9 };
        var source = await BitPdfSource.FromStreamAsync(input, "snapshot.pdf");
        Assert.IsTrue(input.CanRead);
        input.Position = 9;
        input.WriteByte(0);
        input.Dispose();
        CollectionAssert.AreEqual(bytes, source.Bytes!);
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, source));
        component.WaitForPages(1);
        StringAssert.Contains(component.Instance.ExtractText(), "Hello");
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
        Assert.AreEqual(0, component.Instance.FailedPages.Count);
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
        Assert.AreEqual(2, errors.Count);
        Assert.AreEqual(0, component.Instance.FailedPages.Count);

        void AssertSearchFailed(int count)
        {
            Assert.AreEqual(count, errors.Count);
            StringAssert.Contains(errors[^1], "Reading the PDF stream failed");
            Assert.AreEqual("false", component.Find(".bit-pdv-surface").GetAttribute("aria-busy"));
            Assert.AreEqual(0, component.Instance.SearchMatchCount);
            Assert.AreEqual(0, component.Instance.SearchMatchIndex);
            Assert.AreEqual(0, component.Instance.FailedPages.Count);
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
        await component.InvokeAsync(() => viewer.Search("Page")).WaitAsync(TimeSpan.FromSeconds(5));
        component.WaitForPages(1);
        Assert.AreEqual(1, errors);
        Assert.AreEqual(1, input.DisposeCount);
        Assert.AreEqual(0, viewer.FailedPages.Count);
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
            if (!release.Wait(TimeSpan.FromSeconds(5))) throw new IOException("Test read timed out.");
        };
        input.FailRead = true;
        var search = component.InvokeAsync(() => component.Instance.Search("Page"));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var replacement = component.InvokeAsync(() => component.Instance.OpenAsync(BitPdfSource.FromBytes(TestPdf.HelloWorld())));
        try { await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5)); }
        finally { release.Set(); }
        await Task.WhenAll(search, replacement).WaitAsync(TimeSpan.FromSeconds(5));
        component.WaitForPages(1);
        Assert.AreEqual(0, errors);
        Assert.AreEqual(0, component.Instance.FailedPages.Count);
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
        var component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.BackgroundRendering, false)
            .Add(v => v.RenderMode, mode)
            .Add(v => v.Source, BitPdfSource.FromStreamFactory(_ => Task.FromResult<Stream>(input)))
            .Add(v => v.OnError, EventCallback.Factory.Create<string>(this, errors.Add))
            .Add(v => v.OnPageRendered, EventCallback.Factory.Create<int>(this, rendered.Add)));
        component.WaitForPages(3);
        using var copied = new MemoryStream();
        var planned = Context.JSInterop.SetupVoid("BitBlazorUI.PdfViewer.download", _ => true);
        byte[]? result = null;
        var operation = component.InvokeAsync(async () =>
        {
            if (download) await component.Instance.Download();
            else result = await component.Instance.GetBytesAsync();
        });
        DotNetStreamReference? reference = null;
        if (download)
        {
            component.WaitForAssertion(() => Assert.AreEqual(1, planned.Invocations.Count));
            reference = (DotNetStreamReference)planned.Invocations.Single().Arguments[1]!;
        }
        else await input.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var pages = component.InvokeAsync(() => component.Instance.EnsurePagesRendered([2]));
        var thumbs = component.InvokeAsync(() => component.Instance.EnsureThumbsRendered([3]));
        try
        {
            // Downloads also wait while JS has not yet requested its first bytes:
            // serialization must come from the scheduler, not a blocking server I/O wait.
            await Task.Delay(100);
            Assert.IsFalse(pages.IsCompleted, "The page pump must wait asynchronously for the copy.");
            Assert.IsFalse(thumbs.IsCompleted, "The thumbnail pump must wait asynchronously for the copy.");
            CollectionAssert.AreEqual(new[] { 1 }, rendered);
        }
        finally
        {
            input.Resume.TrySetResult();
            if (reference is not null) await reference.Stream.CopyToAsync(copied);
            planned.SetVoidResult();
            await operation;
            await Task.WhenAll(pages, thumbs).WaitAsync(TimeSpan.FromSeconds(5));
        }
        CollectionAssert.AreEqual(bytes, download ? copied.ToArray() : result!);
        CollectionAssert.AreEqual(new[] { 1, 2 }, rendered);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(0, component.Instance.FailedPages.Count);
        Assert.IsNull(component.Instance.GetBytes());
    }

    [TestMethod]
    public async Task SupersedingADownloadShouldReleaseTheRenderSchedulerWhileInteropIsPending()
    {
        var input = new PausedAsyncPdfStream(TestPdf.MultiPage(3));
        int errors = 0;
        var component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.Source, BitPdfSource.FromStreamFactory(_ => Task.FromResult<Stream>(input)))
            .Add(v => v.OnError, EventCallback.Factory.Create<string>(this, _ => errors++)));
        component.WaitForPages(3);
        var planned = Context.JSInterop.SetupVoid("BitBlazorUI.PdfViewer.download", _ => true);
        var download = component.InvokeAsync(() => component.Instance.Download());
        component.WaitForAssertion(() => Assert.AreEqual(1, planned.Invocations.Count));
        var reference = (DotNetStreamReference)planned.Invocations.Single().Arguments[1]!;
        using var copied = new MemoryStream();
        var transfer = reference.Stream.CopyToAsync(copied);
        await input.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            await component.InvokeAsync(() => component.Instance.OpenAsync(BitPdfSource.FromBytes(TestPdf.HelloWorld())))
                .WaitAsync(TimeSpan.FromSeconds(5));
            await download.WaitAsync(TimeSpan.FromSeconds(5));
            await Assert.ThrowsAsync<OperationCanceledException>(() => transfer);
            component.WaitForPages(1);
            Assert.AreEqual(1, input.DisposeCount);
            Assert.AreEqual(0, errors);
            Assert.AreEqual(0, component.Instance.FailedPages.Count);
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
}
