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
}
