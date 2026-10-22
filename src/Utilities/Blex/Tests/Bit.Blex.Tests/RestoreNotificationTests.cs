using System.Text.Json;
using System.Text.Json.Nodes;
using Bit.Blex.Blazor;
using Bit.Blex.Testing;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bit.Blex.Tests;

/// <summary>
/// State that moves without an action -- hydration, a change from another tab, the prerender handoff,
/// <see cref="BlexStoreBase.RestoreState"/> -- must still reach every observer that keeps derived
/// state. Those paths used to restore the store silently, so a cross-store selector, a manager
/// subscription or the undo baseline went on reporting the value from before.
/// </summary>
[TestClass]
public class RestoreNotificationTests
{
    private sealed class MemoryStorage : IBlexStorage
    {
        public Dictionary<string, string> Data { get; } = [];

        public ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Data.TryGetValue(key, out var value) ? value : null);

        public ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            Data[key] = value;
            return ValueTask.CompletedTask;
        }

        public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            Data.Remove(key);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class WatchableStorage : IBlexStorage, IBlexExternalStorageChange
    {
        public MemoryStorage Inner { get; } = new();

        public event Action<string?>? ExternalChange;

        public void RaiseExternal(string key, string value)
        {
            Inner.Data[key] = value;
            ExternalChange?.Invoke(key);
        }

        public ValueTask StartWatchingAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default) => Inner.GetAsync(key, cancellationToken);

        public ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default) => Inner.SetAsync(key, value, cancellationToken);

        public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) => Inner.RemoveAsync(key, cancellationToken);
    }

    [TestMethod]
    public async Task A_cross_store_selector_is_recomputed_after_hydration()
    {
        var manager = new BlexManager();
        var settings = new SettingsStore();
        manager.Register(settings);

        using var theme = manager.CreateSelector(() => settings.Theme);
        Assert.AreEqual("light", theme.Value);

        var storage = new MemoryStorage();
        storage.Data["blex:settings"] = """{"Theme":"dark","FontSize":14}""";
        await new BlexStatePersistor(manager, storage).StartAsync();

        Assert.AreEqual("dark", theme.Value, "The memo still held the value from before hydration.");
    }

    [TestMethod]
    public async Task A_manager_subscription_hears_a_change_from_another_tab()
    {
        var manager = new BlexManager();
        var settings = new SettingsStore();
        manager.Register(settings);

        var storage = new WatchableStorage();
        var persistor = new BlexStatePersistor(manager, storage, new BlexPersistenceOptions { SyncAcrossTabs = true });
        await persistor.StartAsync();

        var seen = new List<string>();
        using var sub = manager.Subscribe(() => settings.Theme, seen.Add);

        storage.RaiseExternal("blex:settings", """{"Theme":"dark","FontSize":14}""");
        await persistor.FlushAsync();

        Assert.AreEqual("dark", settings.Theme);
        CollectionAssert.AreEqual(new[] { "dark" }, seen);
    }

    [TestMethod]
    public void RestoreState_raises_StateRestored_once()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);

        var raised = 0;
        manager.StateRestored += () => raised++;

        counter.RestoreState(new JsonObject { ["Count"] = 5 });

        Assert.AreEqual(5, counter.Count);
        Assert.AreEqual(1, raised);
    }

    [TestMethod]
    public void A_whole_tree_restore_still_raises_StateRestored_once_for_all_its_stores()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        var profile = new ProfileStore();
        manager.Register(counter);
        manager.Register(profile);

        var raised = 0;
        manager.StateRestored += () => raised++;

        manager.RestoreGlobalState(new JsonObject
        {
            ["counter"] = new JsonObject { ["Count"] = 3 },
            ["profile"] = new JsonObject { ["Age"] = 40 },
        });

        Assert.AreEqual(3, counter.Count);
        Assert.AreEqual(40, profile.Age);
        Assert.AreEqual(1, raised);
    }

    [TestMethod]
    public void A_store_restored_while_unregistered_notifies_nobody_but_itself()
    {
        var counter = new CounterStore();
        var changes = counter.CountNotifications(() => counter.RestoreState(new JsonObject { ["Count"] = 2 }));

        Assert.AreEqual(2, counter.Count);
        Assert.AreEqual(1, changes);
    }

    [TestMethod]
    public async Task History_moves_its_baseline_to_the_hydrated_state()
    {
        var manager = new BlexManager();
        var settings = new SettingsStore();
        manager.Register(settings);

        // Started before hydration, as a component resolving it early would.
        using var history = new BlexHistory(manager);
        history.Start();

        var storage = new MemoryStorage();
        storage.Data["blex:settings"] = """{"Theme":"dark","FontSize":14}""";
        await new BlexStatePersistor(manager, storage).StartAsync();

        settings.SetTheme("blue");
        history.Undo();

        Assert.AreEqual("dark", settings.Theme, "Undo went back past hydration to the declared defaults.");
    }

    [TestMethod]
    public async Task The_prerender_handoff_is_a_restore_too()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        using var doubled = manager.CreateSelector(() => counter.Count * 2);
        Assert.AreEqual(0, doubled.Value);

        // What the prerender left behind, exactly as the framework hands it to the interactive render.
        var persistence = new ComponentStatePersistenceManager(NullLogger<ComponentStatePersistenceManager>.Instance);
        await persistence.RestoreStateAsync(new SeededStateStore(new Dictionary<string, byte[]>
        {
            ["blex.cs:counter"] = JsonSerializer.SerializeToUtf8Bytes(new JsonObject { ["Count"] = 21 }),
        }));

        using var handoff = new BlexComponentStatePersistence(persistence.State, [counter], manager);
        handoff.TryRestore();

        Assert.AreEqual(21, counter.Count);
        Assert.AreEqual(42, doubled.Value);
    }

    private sealed class SeededStateStore(IDictionary<string, byte[]> state) : IPersistentComponentStateStore
    {
        public Task<IDictionary<string, byte[]>> GetPersistedStateAsync() => Task.FromResult(state);

        public Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> state) => Task.CompletedTask;
    }
}
