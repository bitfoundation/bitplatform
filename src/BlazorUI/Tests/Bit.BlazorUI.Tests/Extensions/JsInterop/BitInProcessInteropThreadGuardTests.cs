using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Extensions.JsInterop;

/// <summary>
/// The guard behind <c>FastInvoke</c>'s synchronous path only ever runs in the browser, which is the one
/// place these tests are not. Its decision is a pure function of the two facts it is given, so that is what
/// is tested, along with what it does once it has decided - which matters more than it looks: the guard is a
/// heuristic, and the single-threaded WebAssembly runtime nearly every Blazor WASM app is built with runs
/// queued work items on the main thread, where synchronous interop is perfectly legal. A guard that aborted
/// on that would take down a debug build over a non-problem.
/// </summary>
[TestClass]
public class BitInProcessInteropThreadGuardTests
{
    [TestInitialize]
    public void Init() => BitInProcessInteropThreadGuard.ResetReported();

    [TestCleanup]
    public void Cleanup() => BitInProcessInteropThreadGuard.ResetReported();


    [TestMethod]
    public void ItReportsOnlyAThreadPoolThreadInsideTheBrowser()
    {
        // The rule it stands for exists on WebAssembly alone: Server and Hybrid take the asynchronous path,
        // where no thread has any affinity to respect.
        Assert.IsTrue(BitInProcessInteropThreadGuard.ShouldReport(isBrowser: true, isThreadPoolThread: true));

        Assert.IsFalse(BitInProcessInteropThreadGuard.ShouldReport(isBrowser: true, isThreadPoolThread: false));
        Assert.IsFalse(BitInProcessInteropThreadGuard.ShouldReport(isBrowser: false, isThreadPoolThread: true));
        Assert.IsFalse(BitInProcessInteropThreadGuard.ShouldReport(isBrowser: false, isThreadPoolThread: false));
    }

    [TestMethod]
    public void ItsMessageNamesTheCallAndSaysWhatToDoInstead()
    {
        var message = BitInProcessInteropThreadGuard.BuildMessage("BitBlazorUI.Utils.setStyle");

        StringAssert.Contains(message, "BitBlazorUI.Utils.setStyle", "a report that does not name the call cannot be acted on");
        StringAssert.Contains(message, "ConfigureAwait(false)", "the usual cause belongs in the message");
        StringAssert.Contains(message, "WasmEnableThreads", "so does the configuration the rule comes from");
    }


    [TestMethod]
    public void ItNeverThrows()
    {
        // This is the whole point of it being a report rather than an assertion. Off-browser it decides
        // against reporting, and either way a diagnostic must not be able to take a render down.
        BitInProcessInteropThreadGuard.Report("BitBlazorUI.Test.doStuff");
        BitInProcessInteropThreadGuard.Report("BitBlazorUI.Test.doStuff");
    }

    [TestMethod]
    public async Task ItNeverThrowsFromAThreadPoolThread()
    {
        // The thread it is meant to catch. Off-browser it stays quiet; on WebAssembly it would write a line.
        // Neither may throw - a component that legitimately reaches interop from a queued continuation on
        // single-threaded WebAssembly goes through here on every call in a debug build.
        await Task.Run(() => BitInProcessInteropThreadGuard.Report("BitBlazorUI.Test.doStuff"));
    }

    [TestMethod]
    public void ItSaysNothingOutsideTheBrowser()
    {
        // Every one of these tests runs off-browser, which is also where the whole suite runs: if the guard
        // wrote anything here it would be writing it on every FastInvoke of every other test.
        var error = new StringWriter();
        var original = Console.Error;

        try
        {
            Console.SetError(error);

            BitInProcessInteropThreadGuard.Report("BitBlazorUI.Test.doStuff");
        }
        finally
        {
            Console.SetError(original);
        }

        Assert.AreEqual(string.Empty, error.ToString());
    }

    [TestMethod]
    public void TheFastPathRunsTheGuardWithoutDisturbingTheCall()
    {
        // End to end through the real extension method, in this project's DEBUG build - where the
        // [Conditional("DEBUG")] call is compiled in. The call must behave exactly as it does anywhere else.
        var jsRuntime = new FakeInProcessJsRuntime();

        var task = jsRuntime.FastInvokeVoid("BitBlazorUI.Test.doStuff", "arg1");

        Assert.IsTrue(task.IsCompletedSuccessfully);
        Assert.AreEqual(1, jsRuntime.Invocations);
    }


    private sealed class FakeInProcessJsRuntime : Microsoft.JSInterop.IJSInProcessRuntime
    {
        public int Invocations { get; private set; }

        public TResult Invoke<TResult>(string identifier, params object?[]? args)
        {
            Invocations++;
            return default!;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => new(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => new(default(TValue)!);
    }
}
