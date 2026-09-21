using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Extensions.JsInterop;

[TestClass]
public class IJSRuntimeFastExtensionsTests
{
    [TestMethod]
    public void FastInvokeVoid_WhenInProcessRuntime_ShouldInvokeSynchronously()
    {
        var jsRuntime = new FakeInProcessJsRuntime();

        var task = jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff", "arg1", 2);

        // The in-process path runs synchronously, so the returned task must already be completed.
        Assert.IsTrue(task.IsCompleted);
        Assert.AreEqual(1, jsRuntime.SyncInvocations.Count);
        Assert.AreEqual("BitBlazorUI.Test.doStuff", jsRuntime.SyncInvocations[0].Identifier);
        CollectionAssert.AreEqual(new object?[] { "arg1", 2 }, jsRuntime.SyncInvocations[0].Args);
        Assert.AreEqual(0, jsRuntime.AsyncInvocations.Count);
    }

    [TestMethod]
    public async Task FastInvoke_WhenInProcessRuntime_ShouldReturnSynchronousResult()
    {
        var jsRuntime = new FakeInProcessJsRuntime { ResultFactory = _ => true };

        var task = jsRuntime.FastInvoke<bool>("BitBlazorUI.Test.getStuff", "x");

        Assert.IsTrue(task.IsCompleted);
        Assert.IsTrue(await task);
        Assert.AreEqual(1, jsRuntime.SyncInvocations.Count);
        Assert.AreEqual("BitBlazorUI.Test.getStuff", jsRuntime.SyncInvocations[0].Identifier);
        Assert.AreEqual(0, jsRuntime.AsyncInvocations.Count);
    }

    [TestMethod]
    public void FastInvokeVoid_WhenInProcessRuntimeThrowsJsonException_ShouldSwallowAndComplete()
    {
        var jsRuntime = new FakeInProcessJsRuntime { ExceptionFactory = _ => new JsonException("bad json") };

        var task = jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff");

        // A JsonException from the in-process runtime is swallowed; the call still completes successfully.
        Assert.IsTrue(task.IsCompleted);
        Assert.IsFalse(task.IsFaulted);
    }

    [TestMethod]
    public async Task FastInvoke_WhenInProcessRuntimeThrowsJsonException_ShouldReturnDefault()
    {
        var jsRuntime = new FakeInProcessJsRuntime { ExceptionFactory = _ => new JsonException("bad json") };

        var task = jsRuntime.FastInvoke<bool>("BitBlazorUI.Test.getStuff");

        Assert.IsTrue(task.IsCompleted);
        Assert.IsFalse(task.IsFaulted);
        Assert.AreEqual(default, await task);
    }

    [TestMethod]
    public void FastInvokeVoid_WhenInProcessRuntimeThrowsJSException_ShouldPropagate()
    {
        // A missing function, or an error thrown inside the JavaScript function, surfaces on the in-process
        // path exactly as it does on the asynchronous one, so a component behaves the same on every host.
        var jsRuntime = new FakeInProcessJsRuntime { ExceptionFactory = _ => new JSException("js error") };

        Assert.ThrowsExactly<JSException>(() => jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff"));
    }

    [TestMethod]
    public void FastInvoke_WhenInProcessRuntimeThrowsJSException_ShouldPropagate()
    {
        var jsRuntime = new FakeInProcessJsRuntime { ExceptionFactory = _ => new JSException("js error") };

        Assert.ThrowsExactly<JSException>(() => jsRuntime.FastInvoke<bool>("BitBlazorUI.Test.getStuff"));
    }

    [TestMethod]
    public void FastInvokeVoid_WhenNotInProcessRuntime_ShouldFallBackToAsync()
    {
        var jsRuntime = new FakeJsRuntime();

        _ = jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff", "arg1");

        Assert.AreEqual(1, jsRuntime.AsyncInvocations.Count);
        Assert.AreEqual("BitBlazorUI.Test.doStuff", jsRuntime.AsyncInvocations[0].Identifier);
    }

    [TestMethod]
    public void FastInvoke_WhenNotInProcessRuntime_ShouldFallBackToAsync()
    {
        var jsRuntime = new FakeJsRuntime();

        _ = jsRuntime.FastInvoke<bool>("BitBlazorUI.Test.getStuff", "arg1");

        Assert.AreEqual(1, jsRuntime.AsyncInvocations.Count);
        Assert.AreEqual("BitBlazorUI.Test.getStuff", jsRuntime.AsyncInvocations[0].Identifier);
    }


    [TestMethod]
    public void FastInvokeVoid_WhenInProcessRuntimeIsInvalid_ShouldNotInvoke()
    {
        // The fake is in-process but its type name matches the prerendering runtime, so IsRuntimeInvalid
        // is true. The synchronous path must now honor that guard and skip the call instead of invoking.
        var jsRuntime = new UnsupportedJavaScriptRuntime();

        var task = jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff", "arg1");

        Assert.IsTrue(task.IsCompleted);
        Assert.IsFalse(task.IsFaulted);
        Assert.AreEqual(0, jsRuntime.SyncInvocations.Count);
        Assert.AreEqual(0, jsRuntime.AsyncInvocations.Count);
    }

    [TestMethod]
    public async Task FastInvoke_WhenInProcessRuntimeIsInvalid_ShouldReturnDefaultWithoutInvoking()
    {
        var jsRuntime = new UnsupportedJavaScriptRuntime { ResultFactory = _ => true };

        var task = jsRuntime.FastInvoke<bool>("BitBlazorUI.Test.getStuff");

        Assert.IsTrue(task.IsCompleted);
        Assert.AreEqual(default, await task);
        Assert.AreEqual(0, jsRuntime.SyncInvocations.Count);
        Assert.AreEqual(0, jsRuntime.AsyncInvocations.Count);
    }


    [TestMethod]
    public void FastInvokeVoid_WhenServerRuntimeNotInitialized_ShouldNotInvoke()
    {
        // Blazor Server: an uninitialized circuit (RemoteJSRuntime.IsInitialized == false) is invalid,
        // so the call must be skipped rather than attempted against a disconnected circuit.
        var jsRuntime = new RemoteJSRuntime { IsInitialized = false };

        var task = jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff", "arg1");

        Assert.IsTrue(task.IsCompleted);
        Assert.IsFalse(task.IsFaulted);
        Assert.AreEqual(0, jsRuntime.AsyncInvocations.Count);
    }

    [TestMethod]
    public void FastInvokeVoid_WhenServerRuntimeInitialized_ShouldFallBackToAsync()
    {
        // An initialized Blazor Server circuit is a valid, non-in-process runtime, so the call must take
        // the regular asynchronous path.
        var jsRuntime = new RemoteJSRuntime { IsInitialized = true };

        _ = jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff", "arg1");

        Assert.AreEqual(1, jsRuntime.AsyncInvocations.Count);
        Assert.AreEqual("BitBlazorUI.Test.doStuff", jsRuntime.AsyncInvocations[0].Identifier);
    }

    [TestMethod]
    public void FastInvoke_WhenServerRuntimeInitialized_ShouldFallBackToAsync()
    {
        var jsRuntime = new RemoteJSRuntime { IsInitialized = true };

        _ = jsRuntime.FastInvoke<bool>("BitBlazorUI.Test.getStuff", "arg1");

        Assert.AreEqual(1, jsRuntime.AsyncInvocations.Count);
        Assert.AreEqual("BitBlazorUI.Test.getStuff", jsRuntime.AsyncInvocations[0].Identifier);
    }

    [TestMethod]
    public void FastInvokeVoid_WhenHybridRuntimeDisconnected_ShouldNotInvoke()
    {
        // Blazor Hybrid: a disposed/disconnected WebView has a null _ipcSender, which marks the runtime
        // invalid, so the call must be skipped.
        var jsRuntime = new WebViewJSRuntime();
        jsRuntime.SetConnected(false);

        var task = jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff", "arg1");

        Assert.IsTrue(task.IsCompleted);
        Assert.IsFalse(task.IsFaulted);
        Assert.AreEqual(0, jsRuntime.AsyncInvocations.Count);
    }

    [TestMethod]
    public void FastInvokeVoid_WhenHybridRuntimeConnected_ShouldFallBackToAsync()
    {
        var jsRuntime = new WebViewJSRuntime();
        jsRuntime.SetConnected(true);

        _ = jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff", "arg1");

        Assert.AreEqual(1, jsRuntime.AsyncInvocations.Count);
        Assert.AreEqual("BitBlazorUI.Test.doStuff", jsRuntime.AsyncInvocations[0].Identifier);
    }

    [TestMethod]
    public async Task FastInvokeVoid_WithTimeout_WhenInProcessRuntime_ShouldInvokeSynchronously()
    {
        var jsRuntime = new FakeInProcessJsRuntime();

        await jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff", TimeSpan.FromSeconds(30), "arg1");

        // The timeout overload still routes to the synchronous in-process path for a valid WASM runtime.
        Assert.AreEqual(1, jsRuntime.SyncInvocations.Count);
        Assert.AreEqual("BitBlazorUI.Test.doStuff", jsRuntime.SyncInvocations[0].Identifier);
        Assert.AreEqual(0, jsRuntime.AsyncInvocations.Count);
    }

    [TestMethod]
    public async Task FastInvoke_WithTimeout_WhenInProcessRuntime_ShouldReturnSynchronousResult()
    {
        var jsRuntime = new FakeInProcessJsRuntime { ResultFactory = _ => true };

        var result = await jsRuntime.FastInvoke<bool>("BitBlazorUI.Test.getStuff", TimeSpan.FromSeconds(30), "x");

        Assert.IsTrue(result);
        Assert.AreEqual(1, jsRuntime.SyncInvocations.Count);
        Assert.AreEqual(0, jsRuntime.AsyncInvocations.Count);
    }

    [TestMethod]
    public async Task FastInvokeVoid_WithInfiniteTimeout_WhenInProcessRuntime_ShouldInvokeSynchronously()
    {
        var jsRuntime = new FakeInProcessJsRuntime();

        // Timeout.InfiniteTimeSpan means "no CancellationTokenSource"; the call must still run synchronously.
        await jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff", Timeout.InfiniteTimeSpan, "arg1");

        Assert.AreEqual(1, jsRuntime.SyncInvocations.Count);
        Assert.AreEqual(0, jsRuntime.AsyncInvocations.Count);
    }

    [TestMethod]
    public void FastInvokeVoid_WithTimeout_WhenServerRuntimeNotInitialized_ShouldNotInvoke()
    {
        var jsRuntime = new RemoteJSRuntime { IsInitialized = false };

        var task = jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff", TimeSpan.FromSeconds(30), "arg1");

        Assert.IsTrue(task.IsCompleted);
        Assert.IsFalse(task.IsFaulted);
        Assert.AreEqual(0, jsRuntime.AsyncInvocations.Count);
    }


    [TestMethod]
    public void FastInvokeVoid_WhenInProcessRuntime_ShouldAskForTheVoidResultMarker()
    {
        // The in-process runtime deserializes whatever the JS function returned into TResult. IJSVoidResult
        // is the framework's marker for "there is nothing to read", so a JS function that happens to return a
        // value does not make the void path fail. Asking for anything else here (object, for instance) would
        // change that.
        var jsRuntime = new FakeInProcessJsRuntime();

        jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff");

        Assert.AreEqual(typeof(IJSVoidResult), jsRuntime.SyncInvocations[0].ResultType);
    }

    [TestMethod]
    public void FastInvoke_WhenInProcessRuntime_ShouldAskForTheRequestedResultType()
    {
        var jsRuntime = new FakeInProcessJsRuntime { ResultFactory = _ => 42 };

        jsRuntime.FastInvoke<int>("BitBlazorUI.Test.getStuff");

        Assert.AreEqual(typeof(int), jsRuntime.SyncInvocations[0].ResultType);
    }

    [TestMethod]
    public void FastInvoke_WhenInProcessRuntime_ShouldCompleteSynchronouslyWithoutYielding()
    {
        // The whole point of the fast path: an awaiting caller must not yield, so no render or event handler
        // is split across a continuation. IsCompletedSuccessfully is what the await fast-path checks.
        Assert.IsTrue(new FakeInProcessJsRuntime().FastInvokeVoid("BitBlazorUI.Test.doStuff").IsCompletedSuccessfully);
        Assert.IsTrue(new FakeInProcessJsRuntime { ResultFactory = _ => "x" }.FastInvoke<string>("BitBlazorUI.Test.getStuff").IsCompletedSuccessfully);
    }

    [TestMethod]
    public void FastInvoke_WhenRuntimeIsInvalid_ShouldCompleteSynchronouslyWithoutYielding()
    {
        var jsRuntime = new RemoteJSRuntime { IsInitialized = false };

        Assert.IsTrue(jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff").IsCompletedSuccessfully);
        Assert.IsTrue(jsRuntime.FastInvoke<string>("BitBlazorUI.Test.getStuff").IsCompletedSuccessfully);
    }


    [TestMethod]
    public void FastInvokeVoid_WithNoArguments_ShouldForwardAnEmptyArgumentArray()
    {
        var jsRuntime = new FakeInProcessJsRuntime();

        jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff");

        Assert.IsNotNull(jsRuntime.SyncInvocations[0].Args);
        Assert.AreEqual(0, jsRuntime.SyncInvocations[0].Args!.Length);
    }

    [TestMethod]
    public void FastInvokeVoid_WithNullArgumentArray_ShouldForwardItUnchanged()
    {
        // `params object?[]? args` binds a bare null to the array itself, and the framework accepts a null
        // args array. Nothing in between may turn it into an array holding one null.
        var jsRuntime = new FakeInProcessJsRuntime();

        jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff", null);

        Assert.IsNull(jsRuntime.SyncInvocations[0].Args);
    }

    [TestMethod]
    public void FastInvokeVoid_WithNullValuedArgument_ShouldForwardItAsAnArgument()
    {
        var jsRuntime = new FakeInProcessJsRuntime();

        jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff", "a", null, 3);

        CollectionAssert.AreEqual(new object?[] { "a", null, 3 }, jsRuntime.SyncInvocations[0].Args);
    }

    [TestMethod]
    public void FastInvokeVoid_WithOnlyATimeSpan_ShouldBindToTheTimeoutOverloadAndNotPassItToJavaScript()
    {
        // Overload resolution footgun: a lone TimeSpan/CancellationToken argument is the timeout/token, never
        // a JS argument. Pinned so a component can never accidentally ship one as interop payload.
        var jsRuntime = new FakeInProcessJsRuntime();

        _ = jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff", TimeSpan.FromSeconds(30));

        Assert.AreEqual(1, jsRuntime.SyncInvocations.Count);
        Assert.AreEqual(0, jsRuntime.SyncInvocations[0].Args!.Length);
    }

    [TestMethod]
    public void FastInvokeVoid_WithOnlyACancellationToken_ShouldBindToTheTokenOverloadAndNotPassItToJavaScript()
    {
        var jsRuntime = new FakeInProcessJsRuntime();

        _ = jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff", CancellationToken.None);

        Assert.AreEqual(1, jsRuntime.SyncInvocations.Count);
        Assert.AreEqual(0, jsRuntime.SyncInvocations[0].Args!.Length);
    }


    [TestMethod]
    public void FastInvoke_WithCancellationToken_WhenNotInProcessRuntime_ShouldForwardTheToken()
    {
        var jsRuntime = new FakeJsRuntime();
        using var cancellationTokenSource = new CancellationTokenSource();

        _ = jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff", cancellationTokenSource.Token, "arg1");
        _ = jsRuntime.FastInvoke<bool>("BitBlazorUI.Test.getStuff", cancellationTokenSource.Token, "arg1");

        Assert.AreEqual(cancellationTokenSource.Token, jsRuntime.AsyncInvocations[0].CancellationToken);
        Assert.AreEqual(cancellationTokenSource.Token, jsRuntime.AsyncInvocations[1].CancellationToken);
    }

    [TestMethod]
    public async Task FastInvoke_WithTimeout_WhenNotInProcessRuntime_ShouldForwardACancellableToken()
    {
        var jsRuntime = new FakeJsRuntime();

        await jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff", TimeSpan.FromSeconds(30), "arg1");

        // A finite timeout has to arrive as a token that can actually fire; the framework's own default
        // timeout is what is being overridden here.
        Assert.IsTrue(jsRuntime.AsyncInvocations[0].CancellationToken.CanBeCanceled);
    }

    [TestMethod]
    public async Task FastInvoke_WithInfiniteTimeout_WhenNotInProcessRuntime_ShouldForwardANonCancellableToken()
    {
        var jsRuntime = new FakeJsRuntime();

        await jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff", Timeout.InfiniteTimeSpan, "arg1");

        // Timeout.InfiniteTimeSpan means "no CancellationTokenSource", i.e. CancellationToken.None - which is
        // also what opts a Blazor Server call out of the framework's one-minute default timeout.
        Assert.IsFalse(jsRuntime.AsyncInvocations[0].CancellationToken.CanBeCanceled);
        Assert.AreEqual(CancellationToken.None, jsRuntime.AsyncInvocations[0].CancellationToken);
    }

    [TestMethod]
    public async Task FastInvokeVoid_WithTimeout_WhenTheAsyncCallOutlivesIt_ShouldCancel()
    {
        // The timeout overloads await instead of returning the ValueTask so their CancellationTokenSource
        // stays alive for the whole call. If it were disposed early the timeout could never fire and the call
        // would hang for as long as the underlying interop does.
        var jsRuntime = new FakeJsRuntime { HangUntilCancelled = true };

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(
            async () => await jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff", TimeSpan.FromMilliseconds(50)));
    }

    [TestMethod]
    public async Task FastInvoke_WithTimeout_WhenTheAsyncCallOutlivesIt_ShouldCancel()
    {
        var jsRuntime = new FakeJsRuntime { HangUntilCancelled = true };

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(
            async () => await jsRuntime.FastInvoke<bool>("BitBlazorUI.Test.getStuff", TimeSpan.FromMilliseconds(50)));
    }


    [TestMethod]
    public void FastInvoke_WhenInProcessRuntimeThrowsSomethingElse_ShouldPropagate()
    {
        // Only a JSON problem is swallowed. Anything else - a bug in the library, a disposed runtime - has to
        // surface, exactly as it would on the asynchronous path.
        var jsRuntime = new FakeInProcessJsRuntime { ExceptionFactory = _ => new InvalidOperationException("boom") };

        Assert.ThrowsExactly<InvalidOperationException>(() => jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff"));
        Assert.ThrowsExactly<InvalidOperationException>(() => jsRuntime.FastInvoke<bool>("BitBlazorUI.Test.getStuff"));
    }

    [TestMethod]
    public async Task FastInvoke_WhenRuntimeIsInvalid_ShouldReturnTheDefaultOfTheRequestedType()
    {
        // `default(TValue)!` must not become a null reference for a value type, nor a fabricated instance for
        // a reference type: a caller reads the same "nothing happened" answer the asynchronous path gives.
        var jsRuntime = new RemoteJSRuntime { IsInitialized = false };

        Assert.IsNull(await jsRuntime.FastInvoke<string?>("BitBlazorUI.Test.getStuff"));
        Assert.AreEqual(0, await jsRuntime.FastInvoke<int>("BitBlazorUI.Test.getStuff"));
        Assert.AreEqual(0m, await jsRuntime.FastInvoke<decimal>("BitBlazorUI.Test.getStuff"));
        Assert.IsFalse(await jsRuntime.FastInvoke<bool>("BitBlazorUI.Test.getStuff"));
        Assert.AreEqual(0, jsRuntime.AsyncInvocations.Count);
    }

    [TestMethod]
    public async Task FastInvoke_WithTimeoutOrToken_WhenRuntimeIsInvalid_ShouldReturnDefaultWithoutInvoking()
    {
        var jsRuntime = new RemoteJSRuntime { IsInitialized = false };

        Assert.IsFalse(await jsRuntime.FastInvoke<bool>("BitBlazorUI.Test.getStuff", TimeSpan.FromSeconds(30)));
        Assert.IsFalse(await jsRuntime.FastInvoke<bool>("BitBlazorUI.Test.getStuff", CancellationToken.None));
        await jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff", CancellationToken.None);

        Assert.AreEqual(0, jsRuntime.AsyncInvocations.Count);
    }

    [TestMethod]
    public async Task FastInvoke_WhenHybridRuntimeDisconnected_ShouldReturnDefaultWithoutInvoking()
    {
        var jsRuntime = new WebViewJSRuntime();
        jsRuntime.SetConnected(false);

        Assert.IsFalse(await jsRuntime.FastInvoke<bool>("BitBlazorUI.Test.getStuff"));
        Assert.AreEqual(0, jsRuntime.AsyncInvocations.Count);
    }

    [TestMethod]
    public async Task FastInvoke_WhenInProcessRuntimeReturnsNull_ShouldPassItThrough()
    {
        // A JS function that answers with null/undefined is not an error: the caller gets null and decides.
        var jsRuntime = new FakeInProcessJsRuntime();

        Assert.IsNull(await jsRuntime.FastInvoke<string?>("BitBlazorUI.Test.getStuff"));
        Assert.AreEqual(1, jsRuntime.SyncInvocations.Count);
    }


    [TestMethod]
    public void FastInvoke_WhenCalledWithANullRuntime_ShouldThrow()
    {
        // A missing runtime is a wiring bug to surface, not a state to service - the same contract
        // IsRuntimeInvalid holds, reached through every public overload.
        IJSRuntime jsRuntime = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() => jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff"));
        Assert.ThrowsExactly<ArgumentNullException>(() => jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff", CancellationToken.None));
        Assert.ThrowsExactly<ArgumentNullException>(() => jsRuntime.FastInvoke<bool>("BitBlazorUI.Test.getStuff"));
        Assert.ThrowsExactly<ArgumentNullException>(() => jsRuntime.FastInvoke<bool>("BitBlazorUI.Test.getStuff", CancellationToken.None));
    }

    [TestMethod]
    public async Task FastInvoke_WithTimeout_WhenCalledWithANullRuntime_ShouldThrow()
    {
        // These two overloads are `async`, so their exception arrives on the returned task rather than at the
        // call - awaiting is what surfaces it.
        IJSRuntime jsRuntime = null!;

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            async () => await jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff", TimeSpan.FromSeconds(30)));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            async () => await jsRuntime.FastInvoke<bool>("BitBlazorUI.Test.getStuff", TimeSpan.FromSeconds(30)));
    }


    [TestMethod]
    public void FastInvoke_WhenCalledConcurrentlyAcrossRuntimeTypes_ShouldRouteEachOneCorrectly()
    {
        // IsRuntimeInvalid caches one probe per concrete runtime type in a shared static dictionary, and the
        // fast path is called from every component on every render. Racing the cache must not mix the probes
        // up or hand a runtime another one's answer.
        // Each iteration gets its own runtimes, so the recording lists stay single-threaded while the probe
        // cache - the one piece of shared state - is hammered from every thread at once.
        var failures = new ConcurrentQueue<string>();

        Parallel.For(0, 200, _ =>
        {
            var inProcess = new FakeInProcessJsRuntime();
            var disconnectedServer = new RemoteJSRuntime { IsInitialized = false };
            var liveServer = new RemoteJSRuntime { IsInitialized = true };
            var prerender = new UnsupportedJavaScriptRuntime();

            inProcess.FastInvokeVoid("BitBlazorUI.Test.doStuff");
            disconnectedServer.FastInvokeVoid("BitBlazorUI.Test.doStuff");
            liveServer.FastInvokeVoid("BitBlazorUI.Test.doStuff");
            prerender.FastInvokeVoid("BitBlazorUI.Test.doStuff");

            if (inProcess.SyncInvocations.Count != 1) failures.Enqueue("a valid in-process runtime did not reach JavaScript");
            if (disconnectedServer.AsyncInvocations.Count != 0) failures.Enqueue("a disconnected circuit was invoked");
            if (liveServer.AsyncInvocations.Count != 1) failures.Enqueue("a live circuit did not take the asynchronous path");
            if (prerender.SyncInvocations.Count != 0) failures.Enqueue("prerendering reached JavaScript");
        });

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }


    private record Invocation(string Identifier, object?[]? Args)
    {
        /// <summary>The token the asynchronous fallback was handed, so the timeout/token overloads can be pinned.</summary>
        public CancellationToken CancellationToken { get; init; }

        /// <summary>
        /// The TResult the invocation asked for. The void path must ask for <see cref="IJSVoidResult"/>: the
        /// in-process runtime deserializes the JS return value into TResult, and that marker is what tells it
        /// there is nothing to read.
        /// </summary>
        public Type? ResultType { get; init; }
    }

    /// <summary>
    /// A fake <see cref="IJSInProcessRuntime"/> that records synchronous invocations and can be
    /// configured to return a value or throw, mirroring how the real WebAssembly runtime behaves.
    /// </summary>
    private sealed class FakeInProcessJsRuntime : IJSInProcessRuntime
    {
        public List<Invocation> SyncInvocations { get; } = [];
        public List<Invocation> AsyncInvocations { get; } = [];
        public Func<string, object?>? ResultFactory { get; set; }
        public Func<string, Exception>? ExceptionFactory { get; set; }

        public TResult Invoke<TResult>(string identifier, params object?[]? args)
        {
            SyncInvocations.Add(new Invocation(identifier, args) { ResultType = typeof(TResult) });

            if (ExceptionFactory is not null) throw ExceptionFactory(identifier);

            return ResultFactory is null ? default! : (TResult)ResultFactory(identifier)!;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            AsyncInvocations.Add(new Invocation(identifier, args) { ResultType = typeof(TValue) });
            return new ValueTask<TValue>(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            AsyncInvocations.Add(new Invocation(identifier, args) { ResultType = typeof(TValue), CancellationToken = cancellationToken });
            return new ValueTask<TValue>(default(TValue)!);
        }
    }

    /// <summary>
    /// A fake <see cref="IJSRuntime"/> that is NOT in-process, used to verify the async fallback path.
    /// </summary>
    private sealed class FakeJsRuntime : IJSRuntime
    {
        public List<Invocation> AsyncInvocations { get; } = [];

        /// <summary>
        /// When set, the invocation never completes on its own and only ends when the token it was handed is
        /// cancelled - the way a real interop call behaves when its timeout fires.
        /// </summary>
        public bool HangUntilCancelled { get; set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            AsyncInvocations.Add(new Invocation(identifier, args) { ResultType = typeof(TValue) });
            return new ValueTask<TValue>(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            AsyncInvocations.Add(new Invocation(identifier, args) { ResultType = typeof(TValue), CancellationToken = cancellationToken });

            if (HangUntilCancelled is false) return new ValueTask<TValue>(default(TValue)!);

            var tcs = new TaskCompletionSource<TValue>(TaskCreationOptions.RunContinuationsAsynchronously);
            // Mirrors the framework: a cancelled interop call faults with a TaskCanceledException.
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            return new ValueTask<TValue>(tcs.Task);
        }
    }

    /// <summary>
    /// A fake <see cref="IJSInProcessRuntime"/> whose type name matches the framework's prerendering
    /// runtime, so <c>IsRuntimeInvalid</c> reports it as invalid. Used to verify the synchronous
    /// in-process path now honors the runtime-validity guard and skips the call.
    /// </summary>
    private sealed class UnsupportedJavaScriptRuntime : IJSInProcessRuntime
    {
        public List<Invocation> SyncInvocations { get; } = [];
        public List<Invocation> AsyncInvocations { get; } = [];
        public Func<string, object?>? ResultFactory { get; set; }

        public TResult Invoke<TResult>(string identifier, params object?[]? args)
        {
            SyncInvocations.Add(new Invocation(identifier, args));
            return ResultFactory is null ? default! : (TResult)ResultFactory(identifier)!;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            AsyncInvocations.Add(new Invocation(identifier, args));
            return new ValueTask<TValue>(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            AsyncInvocations.Add(new Invocation(identifier, args));
            return new ValueTask<TValue>(default(TValue)!);
        }
    }

    /// <summary>
    /// A fake whose type name matches Blazor Server's <c>RemoteJSRuntime</c>. <c>IsRuntimeInvalid</c>
    /// reflects its public <c>IsInitialized</c> property: <see langword="false"/> models a circuit that
    /// is not yet connected (invalid), <see langword="true"/> a live circuit (valid, async path).
    /// </summary>
    private sealed class RemoteJSRuntime : IJSRuntime
    {
        public List<Invocation> AsyncInvocations { get; } = [];
        public bool IsInitialized { get; set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            AsyncInvocations.Add(new Invocation(identifier, args) { ResultType = typeof(TValue) });
            return new ValueTask<TValue>(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            AsyncInvocations.Add(new Invocation(identifier, args) { ResultType = typeof(TValue), CancellationToken = cancellationToken });
            return new ValueTask<TValue>(default(TValue)!);
        }
    }

    /// <summary>
    /// A fake whose type name matches Blazor Hybrid's <c>WebViewJSRuntime</c>. <c>IsRuntimeInvalid</c>
    /// reflects its private <c>_ipcSender</c> field: a null sender models a disposed/disconnected WebView
    /// (invalid), a non-null sender a live one (valid, async path).
    /// </summary>
    private sealed class WebViewJSRuntime : IJSRuntime
    {
        public List<Invocation> AsyncInvocations { get; } = [];

        // Name must match the framework field that IsRuntimeInvalid reflects.
        private object? _ipcSender;

        public void SetConnected(bool connected) => _ipcSender = connected ? new object() : null;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            AsyncInvocations.Add(new Invocation(identifier, args));
            return new ValueTask<TValue>(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            AsyncInvocations.Add(new Invocation(identifier, args));
            return new ValueTask<TValue>(default(TValue)!);
        }
    }
}
