using Microsoft.JSInterop;

namespace Bit.Belemetry.Tests.Interop;

[TestClass]
public class BelemetryJsBridgeTests
{
    [TestMethod]
    public async Task DisposedWhileStarting_TheStartEndsQuietly_AndReleasesWhatItMade()
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
        Assert.IsTrue(js.Module.Disposed, "DisposeAsync found no module to release; the start releases the one it imported.");
        Assert.IsNull(registry.SendRequests, "A disposed bridge never registers itself with the process's registry.");
        Assert.AreEqual(1, js.Imports, "The second start imports nothing.");
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
        public bool Disposed { get; private set; }

        // isStarted is false, the void calls need no result.
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => ValueTask.FromResult(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
