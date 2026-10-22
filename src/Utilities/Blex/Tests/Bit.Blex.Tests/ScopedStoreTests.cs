using System.Text.Json.Nodes;
using Bit.Blex.Testing;

namespace Bit.Blex.Tests;

/// <summary>A hand-written store whose name ignores the instance key, so it cannot be scoped.</summary>
public sealed class FixedNameStore : BlexStoreBase
{
    public override string Name => "fixed";

    public override JsonObject SerializeState() => [];

    public override void DeserializeState(JsonObject state)
    {
    }
}

/// <summary>
/// Several instances of one store, each under its own name: <see cref="BlexManager.CreateScope{TStore}"/>
/// and the persistence that follows an instance in and out of the manager.
/// </summary>
[TestClass]
public class ScopedStoreTests
{
    private sealed class GatedStorage : IBlexStorage
    {
        public Dictionary<string, string> Data { get; } = [];

        public List<string> Writes { get; } = [];

        /// <summary>When set, reads wait on it -- storage that is slow to answer.</summary>
        public TaskCompletionSource? ReadGate { get; set; }

        private int _readsStarted;

        public int ReadsStarted => Volatile.Read(ref _readsStarted);

        public async ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _readsStarted);
            if (ReadGate is { } gate)
                await gate.Task;

            return Data.TryGetValue(key, out var value) ? value : null;
        }

        public ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            Data[key] = value;
            Writes.Add(key);
            return ValueTask.CompletedTask;
        }

        public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            Data.Remove(key);
            return ValueTask.CompletedTask;
        }
    }

    [TestMethod]
    public void A_scoped_store_is_named_after_its_key()
    {
        var manager = new BlexManager();
        using var scope = manager.CreateScope(new CounterStore(), key: "left");

        Assert.AreEqual("left", scope.Key);
        Assert.AreEqual("left", scope.Store.InstanceKey);
        Assert.AreEqual("counter:left", scope.Store.Name);
        Assert.AreSame(scope.Store, manager.GetStore("counter:left"));
    }

    [TestMethod]
    public void Instances_keep_separate_state_and_separate_action_names()
    {
        var manager = new BlexManager();
        using var log = manager.RecordActions();
        using var left = manager.CreateScope(new CounterStore(), "left");
        using var right = manager.CreateScope(new CounterStore(), "right");

        left.Store.Add(1);
        right.Store.Add(5);

        Assert.AreEqual(1, left.Store.Count);
        Assert.AreEqual(5, right.Store.Count);
        CollectionAssert.AreEqual(new[] { "counter:left/Add", "counter:right/Add" }, log.QualifiedNames.ToArray());

        var tree = manager.CaptureGlobalState();
        Assert.AreEqual(1, tree["counter:left"]!["Count"]!.GetValue<int>());
        Assert.AreEqual(5, tree["counter:right"]!["Count"]!.GetValue<int>());
    }

    [TestMethod]
    public void Keys_are_assigned_when_none_is_given()
    {
        var manager = new BlexManager();
        using var a = manager.CreateScope(new CounterStore());
        using var b = manager.CreateScope(new CounterStore());

        Assert.AreNotEqual(a.Store.Name, b.Store.Name);
        StringAssert.StartsWith(a.Store.Name, "counter:");
    }

    [TestMethod]
    public async Task Disposing_the_scope_unregisters_and_disposes_the_store()
    {
        var manager = new BlexManager();
        var registered = new List<string>();
        var unregistered = new List<string>();
        manager.StoreRegistered += s => registered.Add(s.Name);
        manager.StoreUnregistered += s => unregistered.Add(s.Name);

        var scope = manager.CreateScope(new FailingStore(), "x");
        var watch = scope.Store.Watch();   // parks until its token is cancelled

        scope.Dispose();
        scope.Dispose();

        Assert.IsTrue(scope.IsDisposed);
        Assert.IsNull(manager.GetStore("failing:x"));

        // Disposing the store cancels its in-flight effects; without that this would never complete.
        await watch.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsNull(scope.Store.WatchError);
        CollectionAssert.AreEqual(new[] { "failing:x" }, registered);
        CollectionAssert.AreEqual(new[] { "failing:x" }, unregistered);
    }

    [TestMethod]
    public void Two_instances_under_one_key_are_reported()
    {
        var manager = new BlexManager();
        var errors = new List<BlexError>();
        manager.OnError = errors.Add;

        using var first = manager.CreateScope(new CounterStore(), "same");
        using var second = manager.CreateScope(new CounterStore(), "same");

        Assert.AreEqual("register", Assert.ContainsSingle(errors).Source);
    }

    [TestMethod]
    public void Only_an_unregistered_store_can_be_scoped()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);

        Assert.ThrowsExactly<InvalidOperationException>(() => manager.CreateScope(counter, "late"));
        Assert.ThrowsExactly<ArgumentException>(() => manager.CreateScope(new CounterStore(), " "));
    }

    [TestMethod]
    public void A_store_whose_name_ignores_the_key_cannot_be_scoped()
    {
        var manager = new BlexManager();
        var store = new FixedNameStore();

        Assert.ThrowsExactly<NotSupportedException>(() => manager.CreateScope(store, "a"));
        Assert.IsNull(store.InstanceKey);
        Assert.IsEmpty(manager.Stores);
    }

    [TestMethod]
    public async Task A_scoped_store_is_dispatchable_by_its_keyed_name()
    {
        var manager = new BlexManager();
        using var scope = manager.CreateScope(new CounterStore(), "a");

        await manager.DispatchAsync("counter:a/Add", new JsonObject { ["amount"] = 4 });

        Assert.AreEqual(4, scope.Store.Count);
    }

    [TestMethod]
    public void ResetState_returns_an_instance_to_the_state_it_was_scoped_with()
    {
        var manager = new BlexManager();
        var store = new CounterStore();
        store.Add(2);
        using var scope = manager.CreateScope(store, "a");

        store.Add(40);
        store.ResetState();

        Assert.AreEqual(2, store.Count);
    }

    [TestMethod]
    public async Task A_persistent_store_registered_after_startup_is_hydrated()
    {
        var manager = new BlexManager();
        var storage = new GatedStorage();
        storage.Data["blex:settings:dialog"] = """{"Theme":"dark","FontSize":20}""";
        var persistor = new BlexStatePersistor(manager, storage);
        await persistor.StartAsync();

        using var scope = manager.CreateScope(new SettingsStore(), "dialog");
        await persistor.FlushAsync();

        Assert.AreEqual("dark", scope.Store.Theme);
        Assert.AreEqual(20, scope.Store.FontSize);

        scope.Store.SetTheme("blue");
        await persistor.FlushAsync();
        StringAssert.Contains(storage.Data["blex:settings:dialog"], "blue");
    }

    [TestMethod]
    public async Task Its_saves_wait_until_its_stored_state_has_been_read()
    {
        var manager = new BlexManager();
        var storage = new GatedStorage();
        storage.Data["blex:settings:dialog"] = """{"Theme":"dark","FontSize":20}""";
        var persistor = new BlexStatePersistor(manager, storage);
        await persistor.StartAsync();

        storage.ReadGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var scope = manager.CreateScope(new SettingsStore(), "dialog");
        scope.Store.SetTheme("early");

        // Writing "early" now would overwrite the stored "dark" before anyone read it.
        Assert.IsEmpty(storage.Writes);

        storage.ReadGate.SetResult();
        await persistor.FlushAsync();

        Assert.AreEqual("dark", scope.Store.Theme);
        Assert.IsEmpty(storage.Writes);

        scope.Store.SetTheme("after");
        await persistor.FlushAsync();
        CollectionAssert.AreEqual(new[] { "blex:settings:dialog" }, storage.Writes);
    }

    [TestMethod]
    public async Task An_instance_leaving_mid_hydration_does_not_release_the_hold_of_the_next()
    {
        var manager = new BlexManager();
        var storage = new GatedStorage();
        storage.Data["blex:settings:doc"] = """{"Theme":"dark"}""";
        var persistor = new BlexStatePersistor(manager, storage);
        await persistor.StartAsync();

        var firstRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        storage.ReadGate = firstRead;
        var first = manager.CreateScope(new SettingsStore(), "doc");   // its read is now in flight
        first.Dispose();
        using var second = manager.CreateScope(new SettingsStore(), "doc");

        // Let the first read finish while the second one waits on storage of its own.
        var secondRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        storage.ReadGate = secondRead;
        firstRead.SetResult();
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (storage.ReadsStarted < 2 && DateTime.UtcNow < deadline)
            await Task.Delay(5);
        Assert.AreEqual(2, storage.ReadsStarted);

        second.Store.SetTheme("early");
        Assert.IsEmpty(storage.Writes, "The first instance finishing released the second one's hold.");

        secondRead.SetResult();
        await persistor.FlushAsync();
        Assert.AreEqual("dark", second.Store.Theme);
    }

    [TestMethod]
    public async Task A_store_leaving_writes_the_save_it_still_owes()
    {
        var manager = new BlexManager();
        var storage = new GatedStorage();
        var persistor = new BlexStatePersistor(manager, storage, new BlexPersistenceOptions { DebounceInterval = TimeSpan.FromHours(1) });
        await persistor.StartAsync();

        var scope = manager.CreateScope(new SettingsStore(), "dialog");
        await persistor.FlushAsync();
        scope.Store.SetTheme("last words");
        Assert.IsEmpty(storage.Writes, "Debounced: nothing is written yet.");

        scope.Dispose();
        await persistor.FlushAsync();

        StringAssert.Contains(storage.Data["blex:settings:dialog"], "last words");
    }

    [TestMethod]
    public async Task A_later_instance_for_the_same_key_finds_the_state_of_the_last()
    {
        var manager = new BlexManager();
        var storage = new GatedStorage();
        var persistor = new BlexStatePersistor(manager, storage);
        await persistor.StartAsync();

        using (var first = manager.CreateScope(new SettingsStore(), "doc-7"))
        {
            await persistor.FlushAsync();
            first.Store.SetTheme("sepia");
            await persistor.FlushAsync();
        }

        using var second = manager.CreateScope(new SettingsStore(), "doc-7");
        await persistor.FlushAsync();

        Assert.AreEqual("sepia", second.Store.Theme);
    }

    [TestMethod]
    public async Task With_deferred_hydration_a_late_store_waits_for_RehydrateAsync()
    {
        var manager = new BlexManager();
        var storage = new GatedStorage();
        storage.Data["blex:settings:dialog"] = """{"Theme":"dark"}""";
        var persistor = new BlexStatePersistor(manager, storage, new BlexPersistenceOptions { SkipHydration = true });
        await persistor.StartAsync();

        using var scope = manager.CreateScope(new SettingsStore(), "dialog");
        await persistor.FlushAsync();
        Assert.AreEqual("light", scope.Store.Theme);

        await persistor.RehydrateAsync();
        Assert.AreEqual("dark", scope.Store.Theme);
    }

    [TestMethod]
    public async Task Late_hydration_reports_its_store_to_OnRehydrated()
    {
        var manager = new BlexManager();
        var storage = new GatedStorage();
        storage.Data["blex:settings:dialog"] = """{"Theme":"dark"}""";
        var rehydrated = new List<string>();
        var persistor = new BlexStatePersistor(manager, storage, new BlexPersistenceOptions { OnRehydrated = (name, _) => rehydrated.Add(name) });
        await persistor.StartAsync();

        using var scope = manager.CreateScope(new SettingsStore(), "dialog");
        await persistor.FlushAsync();

        CollectionAssert.AreEqual(new[] { "settings:dialog" }, rehydrated);
    }
}
