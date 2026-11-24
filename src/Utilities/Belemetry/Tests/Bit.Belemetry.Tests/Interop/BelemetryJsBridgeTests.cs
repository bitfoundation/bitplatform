using System.Text.Json;
using Microsoft.JSInterop;

namespace Bit.Belemetry.Tests.Interop;

[TestClass]
public class BelemetryJsBridgeTests
{
    [TestMethod]
    public async Task DisposedWhileImporting_TheStartEndsQuietly_AndReleasesWhatItMade()
    {
        await using var host = TestHost.Create();
        var registry = host.Services.GetRequiredService<BelemetryBrowserBridgeRegistry>();
        var js = new PendingImportJsRuntime();
        var bridge = new BelemetryJsBridge(host.Settings, host.Pipeline, host.Consent, registry, js);

        var first = bridge.StartAsync().AsTask();
        var second = bridge.StartAsync().AsTask(); // Waits for the lock the first holds.
        await bridge.DisposeAsync(); // A circuit closing while the page starts.
        js.Import.SetResult(js.Module);

        Assert.IsFalse(await first, "Disposing the semaphore under the first start made its Release throw ObjectDisposedException.");
        Assert.IsFalse(await second, "A start that waited for the lock finds the bridge disposed.");
        Assert.IsFalse(bridge.IsStarted);
        Assert.AreEqual(1, js.Module.Disposals, "DisposeAsync found no module to release; the start releases the one it imported.");
        Assert.IsNull(registry.SendRequests, "A disposed bridge never registers itself with the process's registry.");
        Assert.AreEqual(1, js.Imports, "The second start imports nothing.");
        CollectionAssert.AreEqual(Array.Empty<string>(), js.Module.Calls, "A disposed bridge starts nothing in the page.");
    }

    [TestMethod]
    [DataRow("isStarted")]
    [DataRow("start")]
    [DataRow("attachDotNet")]
    public async Task ACallFailingAsTheCircuitCloses_ReleasesTheModule(string failing)
    {
        await using var host = TestHost.Create();
        var js = new PendingImportJsRuntime();
        js.Import.SetResult(js.Module);
        var pending = js.Module.Pend(failing);
        var bridge = new BelemetryJsBridge(host.Settings, host.Pipeline, host.Consent, host.Services.GetRequiredService<BelemetryBrowserBridgeRegistry>(), js);

        var start = bridge.StartAsync().AsTask();
        await bridge.DisposeAsync();
        pending.SetException(new JSDisconnectedException("The circuit closed."));

        Assert.IsFalse(await start);
        Assert.IsFalse(bridge.IsStarted);
        Assert.AreEqual(1, js.Module.Disposals, "The module the start imported is released when one of its calls throws.");
    }

    [TestMethod]
    public async Task ACallFailingOnALivePage_ReleasesTheModule_AndTheNextStartImportsAgain()
    {
        // WebAssembly or Hybrid: a JSException from start leaves the page alive, and every later call starts again.
        await using var host = TestHost.Create();
        var js = new PendingImportJsRuntime();
        js.Import.SetResult(js.Module);
        js.Module.Pend("start").SetException(new JSException("start failed"));
        var bridge = new BelemetryJsBridge(host.Settings, host.Pipeline, host.Consent, host.Services.GetRequiredService<BelemetryBrowserBridgeRegistry>(), js);

        Assert.IsFalse(await bridge.StartAsync());
        Assert.AreEqual(1, js.Module.Disposals);

        js.Module.Pend("start").SetException(new JSException("start failed"));
        Assert.IsFalse(await bridge.StartAsync());
        Assert.AreEqual(2, js.Imports);
        Assert.AreEqual(2, js.Module.Disposals, "Each failed start releases its own import.");
    }

    [TestMethod]
    public async Task DisposedWhileAttaching_DetachesTheCallbacksInThePage()
    {
        await using var host = TestHost.Create(o => o.Browser.Transport = BelemetryBrowserTransport.Relay);
        var js = new PendingImportJsRuntime();
        js.Import.SetResult(js.Module);
        var attach = js.Module.Pend("attachDotNet");
        var bridge = new BelemetryJsBridge(host.Settings, host.Pipeline, host.Consent, host.Services.GetRequiredService<BelemetryBrowserBridgeRegistry>(), js);

        var start = bridge.StartAsync().AsTask();
        await bridge.DisposeAsync();
        attach.SetResult(7);

        Assert.IsFalse(await start);
        Assert.IsFalse(bridge.IsStarted);
        Assert.AreEqual(1, js.Module.Disposals);
        var detach = js.Module.Invocations.Single(i => i.Identifier == "detachDotNet");
        Assert.AreEqual(7, detach.Args!.Single(), "The page stops calling the released callbacks (relay batches, the unload drain).");
        var callbacks = (DotNetObjectReference<BelemetryJsCallbacks>)js.Module.Invocations.Single(i => i.Identifier == "attachDotNet").Args![0]!;
        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = callbacks.Value);
    }

    [TestMethod]
    public async Task DisposedWhileReadingTheContext_TheStartReportsNoBridge_AndNothingIsApplied()
    {
        await using var host = TestHost.Create();
        var registry = host.Services.GetRequiredService<BelemetryBrowserBridgeRegistry>();
        var js = new PendingImportJsRuntime();
        js.Import.SetResult(js.Module);
        js.Module.Pend("attachDotNet").SetResult(3);
        var getContext = js.Module.Pend("getContext");
        var bridge = new BelemetryJsBridge(host.Settings, host.Pipeline, host.Consent, registry, js);
        var changes = 0;
        bridge.ContextChanged += _ => changes++;

        var start = bridge.StartAsync().AsTask();
        Assert.IsTrue(bridge.IsStarted, "The module is handed to the bridge before its context is read.");
        await bridge.DisposeAsync();
        using var context = JsonDocument.Parse("""{"consent":3,"decided":true,"sessionId":"s-1","pseudoId":null,"userId":null}""");
        getContext.SetResult(context.RootElement.Clone());

        Assert.IsFalse(await start, "A start that did not end with a live bridge reports none (OnReady).");
        Assert.AreEqual(0, changes, "No context is raised to subscribers of a disposed bridge.");
        Assert.IsNull(bridge.Context);
        Assert.IsFalse(bridge.IsStarted, "DisposeAsync takes the module off the bridge.");
        Assert.IsFalse(await bridge.StartAsync(), "A disposed bridge never reports a start, not even one it had.");
        Assert.AreEqual(1, js.Module.Disposals);
        Assert.AreEqual(3, js.Module.Invocations.Single(i => i.Identifier == "detachDotNet").Args!.Single());
        Assert.IsNull(registry.SendRequests);
    }

    [TestMethod]
    public async Task StartAfterDispose_ReturnsFalse_WithoutTouchingTheRuntime()
    {
        await using var host = TestHost.Create();
        var js = new PendingImportJsRuntime();
        var bridge = new BelemetryJsBridge(host.Settings, host.Pipeline, host.Consent, host.Services.GetRequiredService<BelemetryBrowserBridgeRegistry>(), js);

        await bridge.DisposeAsync();

        Assert.IsFalse(await bridge.StartAsync());
        Assert.AreEqual(0, js.Imports);
    }

    private sealed class PendingImportJsRuntime : IJSRuntime
    {
        public TaskCompletionSource<IJSObjectReference> Import { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public FakeModule Module { get; } = new();

        public int Imports { get; private set; }

        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Assert.AreEqual("import", identifier);
            Imports++;
            return (TValue)await Import.Task;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => InvokeAsync<TValue>(identifier, args);
    }

    private sealed class FakeModule : IJSObjectReference
    {
        private readonly Dictionary<string, TaskCompletionSource<object?>> _pending = [];

        public List<(string Identifier, object?[]? Args)> Invocations { get; } = [];

        public string[] Calls => Invocations.Select(i => i.Identifier).ToArray();

        public int Disposals { get; private set; }

        /// <summary>The next call of <paramref name="identifier"/> waits for (and then takes) the returned result.</summary>
        public TaskCompletionSource<object?> Pend(string identifier) => _pending[identifier] = new(TaskCreationOptions.RunContinuationsAsynchronously);

        // Without a pending result, isStarted is false, attachDotNet's id 0, and the void calls need none.
        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Invocations.Add((identifier, args));
            if (_pending.Remove(identifier, out var pending) is false) return default!;
            return await pending.Task is TValue value ? value : default!;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync()
        {
            Disposals++;
            return ValueTask.CompletedTask;
        }
    }
}
