using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Extensions.JsInterop;

[TestClass]
// These tests mutate the process-global IJSRuntimeFastExtensions.OnError hook. The assembly does not opt
// into MSTest parallelization today, so this is defensive: it keeps the tests serialized (and prevents
// cross-class bleed) should parallelization ever be enabled. Note that Interlocked/Volatile would NOT make
// this parallel-safe; reference writes are already atomic and the real issue is a single shared slot.
// If more global interop state like this accumulates, replace the global with a per-context test seam
// (e.g. an AsyncLocal overlay exposed via a scoped IDisposable override) and drop this attribute.
[DoNotParallelize]
public class IJSRuntimeFastExtensionsTests
{
    [TestCleanup]
    public void ResetErrorHandler()
    {
        // OnError is a process-global hook; reset it so tests don't leak into one another.
        IJSRuntimeFastExtensions.OnError = null;
    }

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
    public void FastInvokeVoid_WhenJsonExceptionAndOnErrorSet_ShouldRouteToHandler()
    {
        var reported = new List<(string Identifier, Exception Exception)>();
        IJSRuntimeFastExtensions.OnError = (identifier, exception) => reported.Add((identifier, exception));

        var jsRuntime = new FakeInProcessJsRuntime { ExceptionFactory = _ => new JsonException("bad json") };

        var task = jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff");

        Assert.IsTrue(task.IsCompleted);
        Assert.IsFalse(task.IsFaulted);
        Assert.AreEqual(1, reported.Count);
        Assert.AreEqual("BitBlazorUI.Test.doStuff", reported[0].Identifier);
        Assert.IsInstanceOfType(reported[0].Exception, typeof(JsonException));
    }

    [TestMethod]
    public async Task FastInvoke_WhenJsonExceptionAndOnErrorSet_ShouldRouteToHandlerAndReturnDefault()
    {
        var reported = new List<(string Identifier, Exception Exception)>();
        IJSRuntimeFastExtensions.OnError = (identifier, exception) => reported.Add((identifier, exception));

        var jsRuntime = new FakeInProcessJsRuntime { ExceptionFactory = _ => new JsonException("bad json") };

        var task = jsRuntime.FastInvoke<bool>("BitBlazorUI.Test.getStuff");

        Assert.IsTrue(task.IsCompleted);
        Assert.IsFalse(task.IsFaulted);
        Assert.AreEqual(default, await task);
        Assert.AreEqual(1, reported.Count);
        Assert.AreEqual("BitBlazorUI.Test.getStuff", reported[0].Identifier);
    }

    [TestMethod]
    public void ReportIfUnexpectedNull_WhenResultIsNullAndRuntimeIsValid_ShouldRouteToOnError()
    {
        var reported = new List<(string Identifier, Exception Exception)>();
        IJSRuntimeFastExtensions.OnError = (identifier, exception) => reported.Add((identifier, exception));

        var jsRuntime = new FakeInProcessJsRuntime();

        var result = jsRuntime.ReportIfUnexpectedNull("BitBlazorUI.Test.getStuff", (string?)null);

        Assert.IsNull(result);
        Assert.AreEqual(1, reported.Count);
        Assert.AreEqual("BitBlazorUI.Test.getStuff", reported[0].Identifier);
        Assert.IsInstanceOfType(reported[0].Exception, typeof(InvalidOperationException));
    }

    [TestMethod]
    public void ReportIfUnexpectedNull_WhenRuntimeIsInvalid_ShouldNotReport()
    {
        var reported = new List<(string Identifier, Exception Exception)>();
        IJSRuntimeFastExtensions.OnError = (identifier, exception) => reported.Add((identifier, exception));

        var jsRuntime = new UnsupportedJavaScriptRuntime();

        var result = jsRuntime.ReportIfUnexpectedNull("BitBlazorUI.Test.getStuff", (string?)null);

        Assert.IsNull(result);
        Assert.AreEqual(0, reported.Count);
    }

    [TestMethod]
    public void ReportIfUnexpectedNull_WhenResultIsPresent_ShouldNotReport()
    {
        var reported = new List<(string Identifier, Exception Exception)>();
        IJSRuntimeFastExtensions.OnError = (identifier, exception) => reported.Add((identifier, exception));

        var jsRuntime = new FakeInProcessJsRuntime();

        var result = jsRuntime.ReportIfUnexpectedNull("BitBlazorUI.Test.getStuff", "controller-id");

        Assert.AreEqual("controller-id", result);
        Assert.AreEqual(0, reported.Count);
    }

    [TestMethod]
    public void FastInvokeVoid_WhenOnErrorHandlerThrows_ShouldNotPropagate()
    {
        IJSRuntimeFastExtensions.OnError = (_, _) => throw new InvalidOperationException("faulty handler");

        var jsRuntime = new FakeInProcessJsRuntime { ExceptionFactory = _ => new JsonException("bad json") };

        // A throwing error handler must never escape the interop call.
        var task = jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff");

        Assert.IsTrue(task.IsCompleted);
        Assert.IsFalse(task.IsFaulted);
    }

    [TestMethod]
    public void FastInvokeVoid_WhenInProcessRuntimeThrowsJSException_ShouldSwallowAndComplete()
    {
        var jsRuntime = new FakeInProcessJsRuntime { ExceptionFactory = _ => new JSException("js error") };

        var task = jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff");

        Assert.IsTrue(task.IsCompleted);
        Assert.IsFalse(task.IsFaulted);
    }

    [TestMethod]
    public async Task FastInvoke_WhenInProcessRuntimeThrowsJSException_ShouldReturnDefault()
    {
        var jsRuntime = new FakeInProcessJsRuntime { ExceptionFactory = _ => new JSException("js error") };

        var task = jsRuntime.FastInvoke<bool>("BitBlazorUI.Test.getStuff");

        Assert.IsTrue(task.IsCompleted);
        Assert.IsFalse(task.IsFaulted);
        Assert.AreEqual(default, await task);
    }

    [TestMethod]
    public void FastInvokeVoid_WhenJSExceptionAndOnErrorSet_ShouldRouteToHandler()
    {
        var reported = new List<(string Identifier, Exception Exception)>();
        IJSRuntimeFastExtensions.OnError = (identifier, exception) => reported.Add((identifier, exception));

        var jsRuntime = new FakeInProcessJsRuntime { ExceptionFactory = _ => new JSException("js error") };

        var task = jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff");

        Assert.IsTrue(task.IsCompleted);
        Assert.IsFalse(task.IsFaulted);
        Assert.AreEqual(1, reported.Count);
        Assert.AreEqual("BitBlazorUI.Test.doStuff", reported[0].Identifier);
        Assert.IsInstanceOfType(reported[0].Exception, typeof(JSException));
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


    private record Invocation(string Identifier, object?[]? Args);

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
            SyncInvocations.Add(new Invocation(identifier, args));

            if (ExceptionFactory is not null) throw ExceptionFactory(identifier);

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
    /// A fake <see cref="IJSRuntime"/> that is NOT in-process, used to verify the async fallback path.
    /// </summary>
    private sealed class FakeJsRuntime : IJSRuntime
    {
        public List<Invocation> AsyncInvocations { get; } = [];

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
