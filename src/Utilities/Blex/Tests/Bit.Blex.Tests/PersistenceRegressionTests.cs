using System.Text.Json.Nodes;
using Microsoft.Extensions.Time.Testing;

namespace Bit.Blex.Tests;

/// <summary>
/// Regressions found auditing persistence: saves made while a hydration reads (or applies) other stores, edits made
/// while startup hydration is still reading, a change from another tab arriving while this tab's own write is in
/// flight, and a held save released after disposal.
/// </summary>
[TestClass]
public class PersistenceRegressionTests
{
    /// <summary>Storage whose reads of a key, and whose writes, can be held until the test lets them through.</summary>
    private sealed class SlowStorage : IBlexStorage, IBlexExternalStorageChange
    {
        public Dictionary<string, string> Data { get; } = [];

        public Dictionary<string, TaskCompletionSource> ReadGates { get; } = [];

        /// <summary>Keys whose next read fails, as storage that is not ready yet does (prerendering).</summary>
        public HashSet<string> FailNextRead { get; } = [];

        public TaskCompletionSource? WriteGate { get; set; }

        public int Writes { get; private set; }

        public event Action<string?>? ExternalChange;

        public async ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (ReadGates.TryGetValue(key, out var gate))
                await gate.Task;

            if (FailNextRead.Remove(key))
                throw new InvalidOperationException("storage is not ready");

            return Data.TryGetValue(key, out var value) ? value : null;
        }

        public async ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            if (WriteGate is { } gate)
                await gate.Task;

            Data[key] = value;
            Writes++;
        }

        public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            Data.Remove(key);
            return ValueTask.CompletedTask;
        }

        public ValueTask StartWatchingAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public void WriteFromAnotherTab(string key, string value)
        {
            Data[key] = value;
            ExternalChange?.Invoke(key);
        }
    }

    private static TaskCompletionSource Gate() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static string ThemeIn(SlowStorage storage, string key) => JsonNode.Parse(storage.Data[key])!["Theme"]!.GetValue<string>();

    [TestMethod]
    public async Task An_edit_made_while_a_rehydration_reads_another_store_is_saved()
    {
        var manager = new BlexManager();
        var settings = new SettingsStore();
        manager.Register(settings);
        manager.Register(new DraftsStore());
        var storage = new SlowStorage();
        var persistor = new BlexStatePersistor(manager, storage);
        await persistor.StartAsync();

        var gate = Gate();
        storage.ReadGates["blex:drafts"] = gate;
        var rehydration = persistor.RehydrateAsync();
        settings.SetTheme("dark");   // settings was read already; drafts is still being read
        gate.SetResult();
        await rehydration;
        await persistor.FlushAsync();

        Assert.AreEqual("dark", ThemeIn(storage, "blex:settings"));
    }

    [TestMethod]
    public async Task An_action_a_restore_subscriber_dispatches_to_another_store_is_saved()
    {
        var manager = new BlexManager();
        var settings = new SettingsStore();
        var drafts = new DraftsStore();
        manager.Register(settings);
        manager.Register(drafts);
        var storage = new SlowStorage();
        var persistor = new BlexStatePersistor(manager, storage, new BlexPersistenceOptions { SyncAcrossTabs = true });
        await persistor.StartAsync();

        settings.StateChanged += () =>
        {
            if (settings.Theme == "theirs" && drafts.Text != "follow")
                drafts.Write("follow");
        };
        storage.WriteFromAnotherTab("blex:settings", """{"Theme":"theirs","FontSize":14}""");
        await persistor.FlushAsync();

        Assert.AreEqual("follow", JsonNode.Parse(storage.Data["blex:drafts"])!["Text"]!.GetValue<string>());
    }

    [TestMethod]
    public async Task An_edit_to_a_late_store_made_while_startup_hydration_still_reads_is_saved()
    {
        var manager = new BlexManager();
        manager.Register(new SettingsStore());
        var storage = new SlowStorage();
        var persistor = new BlexStatePersistor(manager, storage);

        var gate = Gate();
        storage.ReadGates["blex:settings"] = gate;
        var start = persistor.StartAsync();

        // The page is interactive while the startup read runs: a scope opens, finds nothing stored, and is edited.
        using var scope = manager.CreateScope(new SettingsStore(), "doc");
        scope.Store.SetTheme("typed");

        gate.SetResult();
        await start;
        await persistor.FlushAsync();

        Assert.AreEqual("typed", ThemeIn(storage, "blex:settings:doc"));
    }

    [TestMethod]
    public async Task An_edit_to_a_store_not_read_yet_does_not_overwrite_its_stored_state()
    {
        var manager = new BlexManager();
        var settings = new SettingsStore();
        manager.Register(settings);
        var storage = new SlowStorage();
        storage.Data["blex:settings"] = """{"Theme":"stored","FontSize":14}""";
        var persistor = new BlexStatePersistor(manager, storage);

        var gate = Gate();
        storage.ReadGates["blex:settings"] = gate;
        var start = persistor.StartAsync();
        settings.SetTheme("early");   // before its state was read: held, not written over what storage has
        Assert.AreEqual("stored", ThemeIn(storage, "blex:settings"));

        gate.SetResult();
        await start;
        await persistor.FlushAsync();

        Assert.AreEqual("stored", settings.Theme);
        Assert.AreEqual("stored", ThemeIn(storage, "blex:settings"));
    }

    [TestMethod]
    public async Task A_store_the_startup_read_could_not_get_to_keeps_holding_its_saves()
    {
        var manager = new BlexManager();
        var settings = new SettingsStore();
        manager.Register(settings);
        var storage = new SlowStorage();
        storage.Data["blex:settings"] = """{"Theme":"stored","FontSize":14}""";
        var persistor = new BlexStatePersistor(manager, storage);

        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        storage.ReadGates["blex:settings"] = gate;
        var start = persistor.StartAsync();
        gate.SetException(new InvalidOperationException("JS interop is not available while prerendering."));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => start);

        settings.SetTheme("defaults");   // must not land over the state that could not be read
        await persistor.FlushAsync();
        Assert.AreEqual("stored", ThemeIn(storage, "blex:settings"));

        storage.ReadGates.Clear();
        await persistor.StartAsync();   // the retry on first render
        Assert.AreEqual("stored", settings.Theme);

        settings.SetTheme("after");
        await persistor.FlushAsync();
        Assert.AreEqual("after", ThemeIn(storage, "blex:settings"));
    }

    [TestMethod]
    public async Task A_failed_rehydration_does_not_hold_the_saves_of_stores_read_before_it()
    {
        var manager = new BlexManager();
        var settings = new SettingsStore();
        var drafts = new DraftsStore();
        manager.Register(settings);
        manager.Register(drafts);
        var storage = new SlowStorage();
        var persistor = new BlexStatePersistor(manager, storage);
        await persistor.StartAsync();

        var gate = Gate();
        storage.ReadGates["blex:settings"] = gate;
        var rehydration = persistor.RehydrateAsync();
        settings.SetTheme("during");   // held while the read is under way
        gate.SetException(new InvalidOperationException("storage hiccup"));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => rehydration);
        storage.ReadGates.Clear();

        // Both were read at startup: what they hold is their stored state and the user's edits, and saves go on.
        drafts.Write("after");
        await persistor.FlushAsync();

        Assert.AreEqual("during", ThemeIn(storage, "blex:settings"), "the save the hold collected is written");
        Assert.AreEqual("after", JsonNode.Parse(storage.Data["blex:drafts"])!["Text"]!.GetValue<string>());
    }

    [TestMethod]
    public async Task A_store_registered_after_a_failed_start_does_not_save_before_it_is_read()
    {
        var manager = new BlexManager();
        var storage = new SlowStorage();
        storage.Data["blex:drafts"] = """{"Text":"stored"}""";
        var persistor = new BlexStatePersistor(manager, storage);

        var gate = Gate();
        storage.ReadGates["blex:drafts"] = gate;
        manager.Register(new SettingsStore());
        storage.ReadGates["blex:settings"] = gate;
        var start = persistor.StartAsync();
        gate.SetException(new InvalidOperationException("JS interop is not available while prerendering."));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => start);

        // Registered between the failed start and its retry: no hydration has it yet.
        var drafts = new DraftsStore();
        manager.Register(drafts);
        drafts.Write("defaults and a keystroke");
        await persistor.FlushAsync();
        Assert.AreEqual("stored", JsonNode.Parse(storage.Data["blex:drafts"])!["Text"]!.GetValue<string>());

        storage.ReadGates.Clear();
        await persistor.StartAsync();
        Assert.AreEqual("stored", drafts.Text);
    }

    [TestMethod]
    public async Task A_rehydration_overlapping_the_startup_read_is_announced_after_it()
    {
        var manager = new BlexManager();
        var settings = new SettingsStore();
        manager.Register(settings);
        var storage = new SlowStorage();
        storage.Data["blex:settings"] = """{"Theme":"stored","FontSize":14}""";
        var persistor = new BlexStatePersistor(manager, storage);

        var gate = Gate();
        storage.ReadGates["blex:settings"] = gate;
        var start = persistor.StartAsync();
        var rehydration = persistor.RehydrateAsync();
        string? themeWhenAnnounced = null;
        persistor.Hydrated += () => themeWhenAnnounced ??= settings.Theme;

        Assert.IsFalse(rehydration.IsCompleted, "the store the startup read is reading is not read yet");
        gate.SetResult();
        await Task.WhenAll(start, rehydration);

        Assert.AreEqual("stored", themeWhenAnnounced);
    }

    [TestMethod]
    public async Task A_change_from_another_tab_waits_for_this_tabs_write_in_flight_so_both_converge()
    {
        var manager = new BlexManager();
        var settings = new SettingsStore();
        manager.Register(settings);
        var storage = new SlowStorage();
        var persistor = new BlexStatePersistor(manager, storage, new BlexPersistenceOptions { SyncAcrossTabs = true });
        await persistor.StartAsync();

        var write = Gate();
        storage.WriteGate = write;
        settings.SetTheme("mine");   // its write is on its way (an async protector, a queued interop call)
        storage.WriteFromAnotherTab("blex:settings", """{"Theme":"theirs","FontSize":14}""");

        write.SetResult();
        await persistor.FlushAsync();

        Assert.AreEqual(ThemeIn(storage, "blex:settings"), settings.Theme, "this tab shows what storage holds");
    }

    [TestMethod]
    public async Task A_save_released_after_disposal_is_written_at_once_rather_than_by_a_timer()
    {
        var time = new FakeTimeProvider();
        var manager = new BlexManager { TimeProvider = time };
        var storage = new SlowStorage();
        var persistor = new BlexStatePersistor(manager, storage, new BlexPersistenceOptions { DebounceInterval = TimeSpan.FromHours(1) });
        await persistor.StartAsync();

        var gate = Gate();
        storage.ReadGates["blex:settings:doc"] = gate;
        var scope = manager.CreateScope(new SettingsStore(), "doc");
        scope.Store.SetTheme("typed");

        persistor.Dispose();   // synchronous, as a MAUI singleton container is torn down: nothing flushes after it
        gate.SetResult();

        // The read finds nothing stored and releases the held save: written then, not an hour later.
        await Query.QueryTest.Until(() => storage.Data.ContainsKey("blex:settings:doc"), "the held save is written as the read ends");
        Assert.AreEqual("typed", ThemeIn(storage, "blex:settings:doc"));

        var writes = storage.Writes;
        time.Advance(TimeSpan.FromHours(1));
        Assert.AreEqual(writes, storage.Writes, "no timer was left to write later");
    }

    [TestMethod]
    public async Task A_startup_read_failing_partway_keeps_saving_the_stores_it_read_and_holds_the_rest()
    {
        var manager = new BlexManager { OnError = _ => { } };
        var settings = new SettingsStore();
        var drafts = new DraftsStore();
        manager.Register(settings);
        manager.Register(drafts);
        var storage = new SlowStorage();
        storage.Data["blex:drafts"] = """{"Text":"stored"}""";
        storage.FailNextRead.Add("blex:drafts");
        var persistor = new BlexStatePersistor(manager, storage);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => persistor.StartAsync());

        // Settings was read: its edit is saved. Drafts was not: its defaults must not overwrite what storage holds.
        settings.SetTheme("dark");
        drafts.Write("typed before the retry");
        var layout = new LayoutStore();
        manager.Register(layout);   // after the failure, before the retry
        layout.SetView("list");
        await persistor.FlushAsync();

        Assert.AreEqual("dark", ThemeIn(storage, "blex:settings"));
        StringAssert.Contains(storage.Data["blex:drafts"], "stored");
        Assert.IsFalse(storage.Data.ContainsKey("blex:layout"), "a store that registered before the retry was saved unread");

        await persistor.StartAsync();
        await persistor.FlushAsync();

        Assert.IsTrue(persistor.HasHydrated);
        Assert.AreEqual("stored", drafts.Text, "the retry restores what the failed read could not");
        Assert.AreEqual("dark", settings.Theme);
        StringAssert.Contains(storage.Data["blex:layout"], "list", "nothing was stored for it: its held change is written");
    }

    [TestMethod]
    public async Task A_rehydration_overlapping_a_failing_startup_read_reads_the_store_that_read_could_not()
    {
        var manager = new BlexManager { OnError = _ => { } };
        var settings = new SettingsStore();
        manager.Register(settings);
        var storage = new SlowStorage();
        storage.Data["blex:settings"] = """{"Theme":"dark","FontSize":14}""";
        var gate = Gate();
        storage.ReadGates["blex:settings"] = gate;
        storage.FailNextRead.Add("blex:settings");
        var persistor = new BlexStatePersistor(manager, storage);
        var announcedOver = new List<string>();
        persistor.Hydrated += () => announcedOver.Add(settings.Theme);

        var start = persistor.StartAsync();
        var rehydration = persistor.RehydrateAsync();   // leaves settings to the startup read under way
        storage.ReadGates.Remove("blex:settings");
        gate.SetResult();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => start);
        await rehydration;

        Assert.AreEqual("dark", settings.Theme);
        Assert.IsTrue(persistor.HasHydrated);
        Assert.AreSequenceEqual(["dark"], announcedOver, "hydration was announced over the store's defaults");
    }

    [TestMethod]
    public async Task A_flush_waits_for_a_store_read_under_way_and_the_save_it_releases()
    {
        var manager = new BlexManager();
        var settings = new SettingsStore();
        manager.Register(settings);
        var storage = new SlowStorage();
        var gate = Gate();
        storage.ReadGates["blex:settings"] = gate;
        var persistor = new BlexStatePersistor(manager, storage);

        var start = persistor.StartAsync();
        settings.SetTheme("dark");   // held: settings has not been read yet
        var flush = persistor.FlushAsync();
        Assert.IsFalse(flush.IsCompleted, "the flush returned while storage was still being read");

        gate.SetResult();   // nothing stored: the held change is the store's state
        await flush;

        Assert.AreEqual("dark", ThemeIn(storage, "blex:settings"));
        await start;
    }
}
