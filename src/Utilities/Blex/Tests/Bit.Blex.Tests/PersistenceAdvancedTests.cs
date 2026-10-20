using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

[TestClass]
public class PersistenceAdvancedTests
{
    private sealed class InMemoryStorage : IBlexStorage
    {
        public Dictionary<string, string> Data { get; } = new();

        public ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
            => new(Data.TryGetValue(key, out var v) ? v : null);

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

    private static (SettingsStore Store, BlexManager Manager, InMemoryStorage Storage) Setup()
    {
        var store = new SettingsStore();
        var manager = new BlexManager();
        manager.Register(store);
        return (store, manager, new InMemoryStorage());
    }

    [TestMethod]
    public async Task CorruptPayload_IsReportedDiscardedAndRemoved_StartupContinues()
    {
        var (store, manager, storage) = Setup();
        storage.Data["blex:settings"] = "{not valid json!!";
        var errors = new List<BlexError>();
        manager.OnError = errors.Add;

        await using var persistor = new BlexStatePersistor(manager, storage);
        await persistor.StartAsync(); // must not throw

        Assert.AreEqual("light", store.Theme); // defaults kept
        Assert.ContainsSingle(errors);
        Assert.AreEqual("persistence", errors[0].Source);
        Assert.IsFalse(storage.Data.ContainsKey("blex:settings")); // corrupt entry removed
    }

    [TestMethod]
    public async Task RehydrationSubscriberThrow_KeepsStoredData()
    {
        var (store, manager, storage) = Setup();
        storage.Data["blex:settings"] = """{"Theme":"dark","FontSize":16}""";
        var errors = new List<BlexError>();
        manager.OnError = errors.Add;

        // The payload is valid; only a UI subscriber misbehaves during the restore notification.
        store.StateChanged += () => throw new InvalidOperationException("render boom");

        await using var persistor = new BlexStatePersistor(manager, storage);
        await persistor.StartAsync(); // must not throw

        Assert.AreEqual("dark", store.Theme); // state applied
        Assert.IsTrue(storage.Data.ContainsKey("blex:settings")); // data NOT deleted

        // The store isolates the misbehaving notification subscriber itself, so the failure is
        // attributed to the subscriber (naming the store) rather than to persistence.
        Assert.Contains(e => e.Source == "subscriber" && e.Detail == "settings", errors);
    }

    [TestMethod]
    public async Task VersionedPayload_RoundTrips_InEnvelope()
    {
        var (store, manager, storage) = Setup();
        var options = new BlexPersistenceOptions { Version = 2 };

        await using (var persistor = new BlexStatePersistor(manager, storage, options))
        {
            await persistor.StartAsync();
            store.SetTheme("dark");
            await persistor.FlushAsync();
        }

        var stored = JsonNode.Parse(storage.Data["blex:settings"])!.AsObject();
        Assert.AreEqual(2, stored["__blexVersion"]!.GetValue<int>());
        Assert.AreEqual("dark", stored["state"]!["Theme"]!.GetValue<string>());

        // A fresh app instance rehydrates from the envelope.
        var (store2, manager2, _) = (new SettingsStore(), new BlexManager(), 0);
        manager2.Register(store2);
        await using var persistor2 = new BlexStatePersistor(manager2, storage, new BlexPersistenceOptions { Version = 2 });
        await persistor2.StartAsync();
        Assert.AreEqual("dark", store2.Theme);
    }

    [TestMethod]
    public async Task VersionMismatch_RunsMigration()
    {
        var (store, manager, storage) = Setup();
        // Legacy (v0, unversioned) payload with an obsolete theme name.
        storage.Data["blex:settings"] = """{"Theme":"classic","FontSize":11}""";

        var options = new BlexPersistenceOptions
        {
            Version = 1,
            Migrate = (storeName, fromVersion, state) =>
            {
                Assert.AreEqual("settings", storeName);
                Assert.AreEqual(0, fromVersion);
                state["Theme"] = "light"; // rename the obsolete value
                return state;
            },
        };

        await using var persistor = new BlexStatePersistor(manager, storage, options);
        await persistor.StartAsync();

        Assert.AreEqual("light", store.Theme);
        Assert.AreEqual(11, store.FontSize); // untouched fields survive migration
    }

    [TestMethod]
    public async Task VersionMismatch_WithoutMigration_DiscardsPayload()
    {
        var (store, manager, storage) = Setup();
        storage.Data["blex:settings"] = """{"Theme":"ancient"}""";

        await using var persistor = new BlexStatePersistor(manager, storage, new BlexPersistenceOptions { Version = 3 });
        await persistor.StartAsync();

        Assert.AreEqual("light", store.Theme); // discarded -> defaults
    }

    [TestMethod]
    public async Task Debounce_CoalescesBurstsIntoOneWrite()
    {
        var (store, manager, storage) = Setup();
        var writes = 0;
        var counting = new CountingStorage(storage, () => writes++);
        // Long enough that the timer cannot fire on its own between the burst and the assertion
        // below - FlushAsync is what drives the write here, so the interval's length is irrelevant
        // to what this test covers, and a short one would only make it flake on a loaded runner.
        var options = new BlexPersistenceOptions { DebounceInterval = TimeSpan.FromMinutes(5) };

        await using var persistor = new BlexStatePersistor(manager, counting, options);
        await persistor.StartAsync();

        store.SetTheme("a");
        store.SetTheme("b");
        store.SetTheme("c");
        Assert.AreEqual(0, writes); // nothing written yet

        await persistor.FlushAsync();
        Assert.AreEqual(1, writes); // one coalesced write
        Assert.Contains("\"c\"", storage.Data["blex:settings"]);
    }

    [TestMethod]
    public async Task DisposeAsync_FlushesPendingDebouncedWrites()
    {
        var (store, manager, storage) = Setup();
        var options = new BlexPersistenceOptions { DebounceInterval = TimeSpan.FromMinutes(5) };

        var persistor = new BlexStatePersistor(manager, storage, options);
        await persistor.StartAsync();
        store.SetTheme("dark");
        Assert.IsFalse(storage.Data.ContainsKey("blex:settings"));

        await persistor.DisposeAsync();
        Assert.Contains("dark", storage.Data["blex:settings"]);
    }

    [TestMethod]
    public async Task RestoredState_IsWrittenBackToStorage()
    {
        var (store, manager, storage) = Setup();
        var history = new BlexHistory(manager);
        history.Start();

        await using var persistor = new BlexStatePersistor(manager, storage);
        await persistor.StartAsync();

        store.SetTheme("dark");
        await persistor.FlushAsync();
        Assert.Contains("dark", storage.Data["blex:settings"]);

        history.Undo(); // restore does not record an action...
        await persistor.FlushAsync();

        // ...but storage must still reflect the restored (light) state, or a reload would
        // resurrect the undone value.
        Assert.Contains("light", storage.Data["blex:settings"]);
    }

    private sealed class CountingStorage(IBlexStorage inner, Action onWrite) : IBlexStorage
    {
        public ValueTask<string?> GetAsync(string key, CancellationToken ct = default) => inner.GetAsync(key, ct);

        public ValueTask SetAsync(string key, string value, CancellationToken ct = default)
        {
            onWrite();
            return inner.SetAsync(key, value, ct);
        }

        public ValueTask RemoveAsync(string key, CancellationToken ct = default) => inner.RemoveAsync(key, ct);
    }
}
