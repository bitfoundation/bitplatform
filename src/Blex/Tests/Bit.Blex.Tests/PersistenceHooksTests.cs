using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// The hooks that let an app decide <em>what</em> is persisted and <em>when</em> it is applied:
/// <c>Partialize</c> (zustand's projection), <c>OnRehydrated</c> and <c>SkipHydration</c> plus the
/// explicit <see cref="BlexStatePersistor.RehydrateAsync"/> that pairs with it.
/// </summary>
[TestClass]
public class PersistenceHooksTests
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
    public async Task Partialize_WritesOnlyTheProjectedSubset()
    {
        var (store, manager, storage) = Setup();
        var options = new BlexPersistenceOptions
        {
            Partialize = (name, state) =>
            {
                Assert.AreEqual("settings", name);
                state.Remove("FontSize"); // a transient field we never want on disk
                return state;
            },
        };

        await using var persistor = new BlexStatePersistor(manager, storage, options);
        await persistor.StartAsync();

        store.SetTheme("dark");
        await persistor.FlushAsync();

        var saved = (JsonObject)JsonNode.Parse(storage.Data["blex:settings"])!;
        Assert.AreEqual("dark", saved["Theme"]!.GetValue<string>());
        Assert.IsFalse(saved.ContainsKey("FontSize"));
    }

    [TestMethod]
    public async Task Partialize_OmittedKeys_RehydrateToTheirDeclaredDefaults()
    {
        var (store, manager, storage) = Setup();
        var options = new BlexPersistenceOptions
        {
            Partialize = (_, state) => { state.Remove("FontSize"); return state; },
        };

        await using (var persistor = new BlexStatePersistor(manager, storage, options))
        {
            await persistor.StartAsync();
            store.FontSize = 99;
            store.SetTheme("dark");
            await persistor.FlushAsync();
        }

        var fresh = new SettingsStore();
        var freshManager = new BlexManager();
        freshManager.Register(fresh);
        await using var restore = new BlexStatePersistor(freshManager, storage, options);
        await restore.StartAsync();

        Assert.AreEqual("dark", fresh.Theme);
        Assert.AreEqual(14, fresh.FontSize); // the declared default, not the 99 we never saved
    }

    [TestMethod]
    public async Task Partialize_ReturningNull_SkipsTheWriteWithoutClobberingStorage()
    {
        var (store, manager, storage) = Setup();
        storage.Data["blex:settings"] = """{"Theme":"sepia","FontSize":20}""";
        var allow = false;
        var options = new BlexPersistenceOptions { Partialize = (_, state) => allow ? state : null };

        await using var persistor = new BlexStatePersistor(manager, storage, options);
        await persistor.StartAsync();
        Assert.AreEqual("sepia", store.Theme);

        store.SetTheme("dark");
        await persistor.FlushAsync();
        Assert.AreEqual("""{"Theme":"sepia","FontSize":20}""", storage.Data["blex:settings"]);

        allow = true;
        store.SetTheme("light");
        await persistor.FlushAsync();
        Assert.Contains("light", storage.Data["blex:settings"]);
    }

    [TestMethod]
    public async Task Partialize_MayReturnAParentedNode()
    {
        // Picking a sub-object out of a projection (state["inner"]) is a natural thing to write,
        // but a JsonNode cannot be parented twice, so the version envelope would reject it. The
        // persistor detaches it instead of making the caller remember.
        var (store, manager, storage) = Setup();
        var options = new BlexPersistenceOptions
        {
            Version = 3,
            Partialize = (_, state) =>
            {
                var envelope = new JsonObject { ["inner"] = state.DeepClone() };
                return (JsonObject)envelope["inner"]!;
            },
        };

        var errors = new List<BlexError>();
        manager.OnError = errors.Add;

        await using var persistor = new BlexStatePersistor(manager, storage, options);
        await persistor.StartAsync();

        store.SetTheme("dark");
        await persistor.FlushAsync();

        Assert.IsEmpty(errors);
        var saved = (JsonObject)JsonNode.Parse(storage.Data["blex:settings"])!;
        Assert.AreEqual(3, saved["__blexVersion"]!.GetValue<int>());
        Assert.AreEqual("dark", saved["state"]!["Theme"]!.GetValue<string>());
    }

    [TestMethod]
    public async Task OnRehydrated_ReportsTheAppliedSliceAndNullWhenNothingWasStored()
    {
        var (store, manager, storage) = Setup();
        var seen = new List<(string Store, JsonObject? State)>();
        var options = new BlexPersistenceOptions { OnRehydrated = (name, state) => seen.Add((name, state)) };

        await using (var first = new BlexStatePersistor(manager, storage, options))
        {
            await first.StartAsync();
            Assert.AreSequenceEqual([("settings", (JsonObject?)null)], seen); // nothing stored yet

            store.SetTheme("dark");
            await first.FlushAsync();
        }

        seen.Clear();
        var fresh = new SettingsStore();
        var freshManager = new BlexManager();
        freshManager.Register(fresh);
        await using var second = new BlexStatePersistor(freshManager, storage, options);
        await second.StartAsync();

        var (name, applied) = Assert.ContainsSingle(seen);
        Assert.AreEqual("settings", name);
        Assert.AreEqual("dark", applied!["Theme"]!.GetValue<string>());
    }

    [TestMethod]
    public async Task OnRehydrated_ThatThrows_IsContainedAndReported()
    {
        var (_, manager, storage) = Setup();
        storage.Data["blex:settings"] = """{"Theme":"dark","FontSize":16}""";
        var errors = new List<BlexError>();
        manager.OnError = errors.Add;
        var options = new BlexPersistenceOptions
        {
            OnRehydrated = (_, _) => throw new InvalidOperationException("callback boom"),
        };

        await using var persistor = new BlexStatePersistor(manager, storage, options);
        await persistor.StartAsync(); // must not throw

        Assert.Contains(e => e.Source == "persistence" && e.Exception is InvalidOperationException, errors);
    }

    [TestMethod]
    public async Task SkipHydration_StartsSavingWithoutRestoring_UntilRehydrateAsyncIsCalled()
    {
        var (store, manager, storage) = Setup();
        storage.Data["blex:settings"] = """{"Theme":"dark","FontSize":16}""";
        var options = new BlexPersistenceOptions { SkipHydration = true };

        await using var persistor = new BlexStatePersistor(manager, storage, options);
        await persistor.StartAsync();

        Assert.AreEqual("light", store.Theme); // stored state deliberately not applied yet

        // Saving is live from the start, so a change before hydration is still persisted.
        store.FontSize = 21;
        await persistor.FlushAsync();
        Assert.Contains("21", storage.Data["blex:settings"]);

        storage.Data["blex:settings"] = """{"Theme":"dark","FontSize":16}""";
        await persistor.RehydrateAsync();
        Assert.AreEqual("dark", store.Theme);
        Assert.AreEqual(16, store.FontSize);
    }

    [TestMethod]
    public async Task RehydrateAsync_DoesNotImmediatelyRewriteStorage()
    {
        var (store, manager, storage) = Setup();
        storage.Data["blex:settings"] = """{"Theme":"dark"}""";

        await using var persistor = new BlexStatePersistor(manager, storage);
        await persistor.StartAsync();
        await persistor.RehydrateAsync();
        await persistor.FlushAsync();

        // The restore itself is not an action, so nothing was scheduled and the (partial) stored
        // payload is left exactly as it was.
        Assert.AreEqual("""{"Theme":"dark"}""", storage.Data["blex:settings"]);
        Assert.AreEqual("dark", store.Theme);
    }
}
