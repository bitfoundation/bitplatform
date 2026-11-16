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
        => UseGatedStorage(stored is null ? [] : new Dictionary<string, string> { ["settings"] = stored });

    private GatedStorage UseGatedStorage(Dictionary<string, string> stored)
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

    [TestMethod]
    public async Task A_change_to_a_field_kept_out_of_storage_stays_undoable()
    {
        Services.AddBitBlexStore<SearchHistoryStore>();
        var storage = UseGatedStorage(new Dictionary<string, string> { ["search"] = """{"Recent":["stored"]}""" });
        RenderProvider();
        var search = Services.GetRequiredService<SearchHistoryStore>();

        // Query is [BlexState(Persist = false)]: the stored state does not set it, so the edit survives the read.
        await Renderer.Dispatcher.InvokeAsync(() => search.Type("x"));
        await storage.OpenAsync(Renderer.Dispatcher);

        Assert.AreEqual("x", search.Query);
        Assert.AreEqual("search/Type", History.NextUndoLabel, "The edit the stored state left alone was baked into the baseline.");

        await Renderer.Dispatcher.InvokeAsync(History.Undo);

        Assert.AreEqual("", search.Query);
        CollectionAssert.AreEqual(new[] { "stored" }, search.Recent);
    }

    [TestMethod]
    public async Task A_change_to_a_field_the_merge_hook_keeps_live_stays_undoable()
    {
        Services.AddBitBlexPersistence(o => o.Merge = (_, persisted, current) =>
        {
            persisted["Theme"] = current["Theme"]?.DeepClone();
            return persisted;
        });
        var storage = UseGatedStorage("""{"Theme":"stored","FontSize":20}""");
        RenderProvider();

        await Renderer.Dispatcher.InvokeAsync(() => Settings.SetTheme("dark"));
        await storage.OpenAsync(Renderer.Dispatcher);

        Assert.AreEqual("dark", Settings.Theme);
        Assert.AreEqual(20, Settings.FontSize);
        Assert.IsTrue(History.CanUndo);

        await Renderer.Dispatcher.InvokeAsync(History.Undo);

        Assert.AreEqual("light", Settings.Theme);
        Assert.AreEqual(20, Settings.FontSize, "Undo put the declared default back over the stored field.");
    }

    [TestMethod]
    public async Task Stored_state_a_later_RehydrateAsync_reads_for_the_first_time_becomes_the_baseline()
    {
        var storage = UseGatedStorage("""{"Theme":"stored","FontSize":20}""");
        storage.FailReads = 1;
        RenderProvider();
        await storage.OpenAsync(Renderer.Dispatcher);
        Assert.AreEqual("persistence", Assert.ContainsSingle(_errors).Source);

        // Startup could not read storage: the app runs on the defaults, and an edit meanwhile is a step from them.
        await Renderer.Dispatcher.InvokeAsync(() => Settings.SetTheme("dark"));
        await Renderer.Dispatcher.InvokeAsync(() => Services.GetRequiredService<BlexStatePersistor>().RehydrateAsync());

        Assert.AreEqual("stored", Settings.Theme);
        Assert.IsFalse(History.CanUndo, "Undo would put the declared defaults back over the state storage holds.");
    }

    [TestMethod]
    public async Task Another_restore_while_storage_is_read_still_moves_away_from_the_steps()
    {
        var storage = UseGatedStorage();
        RenderProvider();

        // The counter is not persisted: a restore of it is time-travel, not its stored state arriving.
        await Renderer.Dispatcher.InvokeAsync(Counter.Increment);
        await Renderer.Dispatcher.InvokeAsync(() => Services.GetRequiredService<BlexManager>()
            .RestoreGlobalState(new System.Text.Json.Nodes.JsonObject { ["counter"] = new System.Text.Json.Nodes.JsonObject { ["Count"] = 5 } }));
        await storage.OpenAsync(Renderer.Dispatcher);

        Assert.AreEqual(5, Counter.Count);
        Assert.IsTrue(History.CanUndo, "The restore rewrote the steps as if it were the counter's stored state.");

        await Renderer.Dispatcher.InvokeAsync(History.Undo);
        Assert.AreEqual(0, Counter.Count);
    }

    [TestMethod]
    public async Task A_typed_history_resolved_while_storage_is_read_takes_the_stored_state_too()
    {
        Services.AddBitBlexHistory<SettingsStore>();
        var storage = UseGatedStorage("""{"Theme":"stored","FontSize":20}""");
        RenderProvider();
        var typed = Services.GetRequiredService<BlexHistory<SettingsStore>>();

        await Renderer.Dispatcher.InvokeAsync(() => Settings.SetTheme("dark"));
        Assert.IsTrue(typed.CanUndo);
        await storage.OpenAsync(Renderer.Dispatcher);

        Assert.AreEqual("stored", Settings.Theme);
        Assert.IsFalse(typed.CanUndo, "Undo would put the declared defaults back over the state storage holds.");
    }

    [TestMethod]
    public async Task A_step_the_stored_state_replaced_comes_off_even_with_a_filtered_action_after_it()
    {
        Services.AddBitBlexHistory(filter: ctx => ctx.Store is not CounterStore);
        var storage = UseGatedStorage("""{"Theme":"stored","FontSize":20}""");
        RenderProvider();

        await Renderer.Dispatcher.InvokeAsync(() => Settings.SetTheme("dark"));
        await Renderer.Dispatcher.InvokeAsync(Counter.Increment);   // filtered: moves the present, not the boundary
        await storage.OpenAsync(Renderer.Dispatcher);

        Assert.AreEqual("stored", Settings.Theme);
        Assert.IsNull(History.NextUndoLabel, "An undo labelled with the replaced action would only revert the filtered one.");
    }

    [TestMethod]
    public async Task A_redo_the_stored_state_replaced_comes_off()
    {
        var storage = UseGatedStorage("""{"Theme":"stored","FontSize":20}""");
        RenderProvider();

        await Renderer.Dispatcher.InvokeAsync(() => Settings.SetTheme("dark"));
        await Renderer.Dispatcher.InvokeAsync(History.Undo);
        Assert.IsTrue(History.CanRedo);
        await storage.OpenAsync(Renderer.Dispatcher);

        Assert.AreEqual("stored", Settings.Theme);
        Assert.IsFalse(History.CanRedo, "Redo would restore what is already showing.");
    }

    [TestMethod]
    public async Task Stored_state_arriving_with_nothing_dispatched_raises_no_Changed()
    {
        var storage = UseGatedStorage("""{"Theme":"stored","FontSize":20}""");
        RenderProvider();
        var raised = 0;
        History.Changed += () => raised++;

        await storage.OpenAsync(Renderer.Dispatcher);

        Assert.AreEqual("stored", Settings.Theme);
        Assert.AreEqual(0, raised);
    }

    /// <summary>
    /// Storage whose reads wait until the test lets them through, as a Blazor Server round trip does. Holds a payload
    /// per store name; <see cref="FailReads"/> makes that many reads throw, as storage the browser refuses does.
    /// </summary>
    private sealed class GatedStorage(Dictionary<string, string> stored) : IBlexStorage
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int FailReads { get; set; }

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
            if (FailReads > 0)
            {
                FailReads--;
                throw new JSException("SecurityError: The operation is insecure.");
            }

            foreach (var (name, payload) in stored)
            {
                if (key.EndsWith(name, StringComparison.Ordinal))
                    return payload;
            }

            return null;
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
