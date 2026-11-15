using Bit.Blex.Blazor;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.Blex.Tests.Components;

/// <summary>
/// <see cref="BlexProvider"/> renders its content - and the page takes clicks - while storage is still being read:
/// on Blazor Server that read is a round trip to the browser. What the user does meanwhile has to be undoable, the
/// stored state has to become undo's baseline, and storage that cannot be read must not take the app down.
/// </summary>
[TestClass]
public class BlexProviderHydrationTests : BunitContext
{
    private readonly List<BlexError> _errors = [];

    public BlexProviderHydrationTests()
    {
        Services.AddBitBlexServices(o => o.OnError = _errors.Add);
        Services.AddBitBlexStore<SettingsStore>();
        Services.AddBitBlexStore<CounterStore>();
        Services.AddBitBlexPersistence();
        Services.AddBitBlexHistory();
    }

    private SettingsStore Settings => Services.GetRequiredService<SettingsStore>();

    private CounterStore Counter => Services.GetRequiredService<CounterStore>();

    private BlexHistory History => Services.GetRequiredService<BlexHistory>();

    // The storage is added first: the renderer info resolves the container, which takes no registrations after that.
    private void RenderProvider()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        Render<BlexProvider>(p => p.Add(x => x.ChildContent, (RenderFragment)(b => b.AddContent(0, "app"))));
    }

    private GatedStorage UseGatedStorage(string? stored = null)
    {
        var storage = new GatedStorage(stored);
        Services.AddSingleton<IBlexStorage>(storage);
        return storage;
    }

    [TestMethod]
    public async Task An_action_dispatched_while_storage_is_read_can_be_undone()
    {
        var storage = UseGatedStorage();
        RenderProvider();

        await Renderer.Dispatcher.InvokeAsync(() => Settings.SetTheme("dark"));
        await storage.OpenAsync(Renderer.Dispatcher);

        Assert.IsTrue(History.CanUndo, "The action the user dispatched before storage answered was not recorded.");

        await Renderer.Dispatcher.InvokeAsync(History.Undo);

        Assert.AreEqual("light", Settings.Theme);
        Assert.IsEmpty(_errors);
    }

    [TestMethod]
    public async Task The_stored_state_becomes_the_baseline_and_keeps_the_other_stores_steps()
    {
        var storage = UseGatedStorage("""{"Theme":"stored","FontSize":20}""");
        RenderProvider();

        // One action on a store whose stored state then replaces it, one on a store nothing is stored for.
        await Renderer.Dispatcher.InvokeAsync(() => Settings.SetTheme("dark"));
        await Renderer.Dispatcher.InvokeAsync(Counter.Increment);
        await storage.OpenAsync(Renderer.Dispatcher);

        Assert.AreEqual("stored", Settings.Theme);
        Assert.AreEqual(1, History.UndoCount, "The step the stored state replaced should have come off the stack.");
        Assert.AreEqual("counter/Increment", History.NextUndoLabel);

        await Renderer.Dispatcher.InvokeAsync(History.Undo);

        Assert.AreEqual(0, Counter.Count);
        Assert.AreEqual("stored", Settings.Theme, "Undo put the pre-hydration defaults back over the stored state.");
        Assert.IsFalse(History.CanUndo);
    }

    [TestMethod]
    public async Task A_restore_after_hydration_still_moves_the_baseline()
    {
        var storage = UseGatedStorage();
        RenderProvider();
        await storage.OpenAsync(Renderer.Dispatcher);

        await Renderer.Dispatcher.InvokeAsync(() => Settings.SetTheme("dark"));
        await Renderer.Dispatcher.InvokeAsync(() => Services.GetRequiredService<BlexManager>()
            .RestoreGlobalState(new System.Text.Json.Nodes.JsonObject { ["settings"] = new System.Text.Json.Nodes.JsonObject { ["Theme"] = "jumped", ["FontSize"] = 14 } }));

        // A time-travel jump, not a store's stored state arriving: the step before it is kept.
        Assert.IsTrue(History.CanUndo);
        await Renderer.Dispatcher.InvokeAsync(History.Undo);
        Assert.AreEqual("light", Settings.Theme);
    }

    [TestMethod]
    public async Task Storage_that_cannot_be_read_is_reported_and_the_app_still_runs()
    {
        Services.AddSingleton<IBlexStorage>(new ThrowingStorage());

        RenderProvider();
        await Renderer.Dispatcher.InvokeAsync(() => Settings.SetTheme("dark"));

        var error = Assert.ContainsSingle(_errors, "the read failure is reported once");
        Assert.AreEqual("persistence", error.Source);
        Assert.IsInstanceOfType<JSException>(error.Exception);
        Assert.IsTrue(History.CanUndo, "Undo/redo still starts when storage could not be read.");
    }

    /// <summary>Storage whose reads wait until the test lets them through, as a Blazor Server round trip does.</summary>
    private sealed class GatedStorage(string? stored) : IBlexStorage
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task OpenAsync(Dispatcher dispatcher)
        {
            await dispatcher.InvokeAsync(_gate.SetResult);
            // Let the hydration that was waiting run to its end on the renderer's dispatcher.
            for (var i = 0; i < 10; i++)
                await dispatcher.InvokeAsync(() => Task.Delay(10));
        }

        public async ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            await _gate.Task;
            return key.EndsWith("settings", StringComparison.Ordinal) ? stored : null;
        }

        public ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    /// <summary>Storage the browser refuses, as localStorage is with site data blocked.</summary>
    private sealed class ThrowingStorage : IBlexStorage
    {
        public ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
            => throw new JSException("SecurityError: The operation is insecure.");

        public ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
