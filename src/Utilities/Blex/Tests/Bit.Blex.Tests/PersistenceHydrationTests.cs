using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Bit.Blex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// Hydration status (<c>hasHydrated</c>/<c>onFinishHydration</c> in zustand terms) and the
/// <c>merge</c> hook that decides how a stored payload meets the live store.
/// </summary>
[TestClass]
public class PersistenceHydrationTests
{
    private sealed class InMemoryStorage : IBlexStorage
    {
        public Dictionary<string, string> Data { get; } = new();

        /// <summary>When set, reads wait on it -- storage that is slow to answer.</summary>
        public TaskCompletionSource? ReadGate { get; set; }

        public async ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (ReadGate is { } gate)
                await gate.Task;

            return Data.TryGetValue(key, out var v) ? v : null;
        }

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

    private static (BlexManager Manager, SettingsStore Store) Setup()
    {
        var manager = new BlexManager();
        var store = new SettingsStore();
        manager.Register(store);
        return (manager, store);
    }

    [TestMethod]
    public async Task HasHydrated_IsFalseUntilRehydrationCompletes()
    {
        var (manager, _) = Setup();
        var persistor = new BlexStatePersistor(manager, new InMemoryStorage());

        Assert.IsFalse(persistor.HasHydrated);
        await persistor.StartAsync();
        Assert.IsTrue(persistor.HasHydrated);
    }

    [TestMethod]
    public async Task HasHydrated_StaysFalse_WhenHydrationIsSkipped()
    {
        var (manager, _) = Setup();
        var options = new BlexPersistenceOptions { SkipHydration = true };
        var persistor = new BlexStatePersistor(manager, new InMemoryStorage(), options);

        await persistor.StartAsync();
        Assert.IsFalse(persistor.HasHydrated);

        await persistor.RehydrateAsync();
        Assert.IsTrue(persistor.HasHydrated);
    }

    [TestMethod]
    public async Task HydratedEvent_FiresOnceHydrationLands()
    {
        var (manager, _) = Setup();
        var persistor = new BlexStatePersistor(manager, new InMemoryStorage());

        var fired = 0;
        persistor.Hydrated += () => fired++;

        await persistor.StartAsync();
        Assert.AreEqual(1, fired);
    }

    [TestMethod]
    public async Task WhenHydratedAsync_CompletesOnHydration()
    {
        var (manager, store) = Setup();
        var storage = new InMemoryStorage();
        storage.Data["blex:settings"] = """{"Theme":"dark","FontSize":18}""";

        var options = new BlexPersistenceOptions { SkipHydration = true };
        var persistor = new BlexStatePersistor(manager, storage, options);
        await persistor.StartAsync();

        var waiting = persistor.WhenHydratedAsync();
        Assert.IsFalse(waiting.IsCompleted);

        await persistor.RehydrateAsync();
        await waiting.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.AreEqual("dark", store.Theme);
    }

    [TestMethod]
    public async Task WhenHydratedAsync_IsAlreadyCompleteAfterHydration()
    {
        var (manager, _) = Setup();
        var persistor = new BlexStatePersistor(manager, new InMemoryStorage());
        await persistor.StartAsync();

        Assert.IsTrue(persistor.WhenHydratedAsync().IsCompleted);
    }

    [TestMethod]
    public async Task WhenHydratedAsync_IsReleasedOnDisposal()
    {
        var (manager, _) = Setup();
        var options = new BlexPersistenceOptions { SkipHydration = true };
        var persistor = new BlexStatePersistor(manager, new InMemoryStorage(), options);
        await persistor.StartAsync();

        var waiting = persistor.WhenHydratedAsync();
        await persistor.DisposeAsync();

        // A persistor torn down before it hydrated must not leave callers awaiting forever; the
        // flag still reports that hydration never happened.
        await waiting.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsFalse(persistor.HasHydrated);

        // Nor callers that only start waiting once it is gone.
        Assert.IsTrue(persistor.WhenHydratedAsync().IsCompleted);
    }

    [TestMethod]
    public async Task WhenHydratedAsync_AnActionDispatchedTheMomentItCompletes_IsSaved()
    {
        var (manager, store) = Setup();
        var storage = new InMemoryStorage();
        var persistor = new BlexStatePersistor(manager, storage);

        var dispatched = DispatchOnceHydrated();

        async Task DispatchOnceHydrated()
        {
            await persistor.WhenHydratedAsync();
            store.SetTheme("dark");
        }

        // The waiter resumes on the thread pool once released. Holding the announcement until it has dispatched
        // lets it get there before StartAsync takes another step, as a thread-pool continuation may.
        persistor.Hydrated += () => dispatched.Wait(TimeSpan.FromSeconds(5));

        await persistor.StartAsync();
        await dispatched.WaitAsync(TimeSpan.FromSeconds(5));
        await persistor.FlushAsync();

        Assert.IsTrue(storage.Data.TryGetValue("blex:settings", out var saved), "The action was never saved.");
        StringAssert.Contains(saved, "dark");
    }

    [TestMethod]
    public async Task OverlappingHydrations_LeaveSavingOn()
    {
        var (manager, store) = Setup();
        var storage = new InMemoryStorage();
        storage.Data["blex:settings"] = """{"Theme":"dark"}""";
        var persistor = new BlexStatePersistor(manager, storage);

        var startupRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        storage.ReadGate = startupRead;
        var start = persistor.StartAsync();

        // A second hydration starts while the first is reading storage, and finishes after it.
        var laterRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        storage.ReadGate = laterRead;
        var rehydrate = persistor.RehydrateAsync();

        startupRead.SetResult();
        await start;
        laterRead.SetResult();
        await rehydrate;

        store.SetTheme("after");
        await persistor.FlushAsync();

        StringAssert.Contains(storage.Data["blex:settings"], "after", "Saving stayed suspended after both hydrations finished.");
    }

    [TestMethod]
    public async Task RehydrateAsync_KeepsAChangeTheDebounceStillHolds()
    {
        var (manager, store) = Setup();
        var storage = new InMemoryStorage();
        storage.Data["blex:settings"] = """{"Theme":"dark"}""";
        var persistor = new BlexStatePersistor(manager, storage, new BlexPersistenceOptions { DebounceInterval = TimeSpan.FromHours(1) });
        await persistor.StartAsync();

        store.SetTheme("edited");   // not written yet
        await persistor.RehydrateAsync();

        Assert.AreEqual("edited", store.Theme, "The older stored state was read back over the change.");
        StringAssert.Contains(storage.Data["blex:settings"], "edited");
    }

    [TestMethod]
    public async Task APersistorDisposedWhileItHydrates_NeitherAnnouncesNorSaves()
    {
        var (manager, store) = Setup();
        var storage = new InMemoryStorage();
        var persistor = new BlexStatePersistor(manager, storage);
        var announced = false;
        persistor.Hydrated += () => announced = true;

        var read = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        storage.ReadGate = read;
        var start = persistor.StartAsync();
        await persistor.DisposeAsync();

        read.SetResult();
        await start;

        store.SetTheme("after");
        await persistor.FlushAsync();

        Assert.IsFalse(announced);
        Assert.IsFalse(persistor.HasHydrated);
        Assert.IsEmpty(storage.Data, "A disposed persistor saved a change.");
    }

    [TestMethod]
    public async Task HydratedEvent_AnActionItsHandlerDispatches_IsSaved()
    {
        var (manager, store) = Setup();
        var storage = new InMemoryStorage();
        var persistor = new BlexStatePersistor(manager, storage);
        persistor.Hydrated += () => store.SetTheme("dark");

        await persistor.StartAsync();
        await persistor.FlushAsync();

        Assert.IsTrue(storage.Data.TryGetValue("blex:settings", out var saved), "The action was never saved.");
        StringAssert.Contains(saved, "dark");
    }

    [TestMethod]
    public async Task Merge_CanKeepALiveValueThePayloadWouldOverwrite()
    {
        var (manager, store) = Setup();
        store.SetTheme("live");

        var storage = new InMemoryStorage();
        storage.Data["blex:settings"] = """{"Theme":"stored","FontSize":22}""";

        var options = new BlexPersistenceOptions
        {
            Merge = (name, persisted, current) =>
            {
                Assert.AreEqual("settings", name);
                persisted["Theme"] = current["Theme"]?.DeepClone();
                return persisted;
            },
        };

        var persistor = new BlexStatePersistor(manager, storage, options);
        await persistor.StartAsync();

        Assert.AreEqual("live", store.Theme);   // the live value won
        Assert.AreEqual(22, store.FontSize);    // the stored value applied
    }

    [TestMethod]
    public async Task Merge_ReturningNull_DiscardsThePayload()
    {
        var (manager, store) = Setup();
        var storage = new InMemoryStorage();
        storage.Data["blex:settings"] = """{"Theme":"stored"}""";

        var reported = new List<string?>();
        var options = new BlexPersistenceOptions
        {
            Merge = (_, _, _) => null,
            OnRehydrated = (name, state) => reported.Add(state is null ? null : name),
        };

        var persistor = new BlexStatePersistor(manager, storage, options);
        await persistor.StartAsync();

        Assert.AreEqual("light", store.Theme);
        Assert.AreSequenceEqual(new string?[] { null }, reported);
    }

    [TestMethod]
    public async Task Merge_MayReturnANestedNodeOfItsArguments()
    {
        var (manager, store) = Setup();
        var storage = new InMemoryStorage();
        storage.Data["blex:settings"] = """{"Nested":{"Theme":"dark","FontSize":30}}""";

        var options = new BlexPersistenceOptions
        {
            // Handing back a still-parented node must not throw: a JsonNode cannot be parented twice.
            Merge = (_, persisted, _) => persisted["Nested"] as JsonObject,
        };

        var persistor = new BlexStatePersistor(manager, storage, options);
        await persistor.StartAsync();

        Assert.AreEqual("dark", store.Theme);
        Assert.AreEqual(30, store.FontSize);
    }

    [TestMethod]
    public async Task AThrowingMerge_DiscardsThePayloadAndReports()
    {
        var (manager, store) = Setup();
        var errors = new List<BlexError>();
        manager.OnError = errors.Add;

        var storage = new InMemoryStorage();
        storage.Data["blex:settings"] = """{"Theme":"stored"}""";

        var options = new BlexPersistenceOptions { Merge = (_, _, _) => throw new InvalidOperationException("merge-boom") };
        var persistor = new BlexStatePersistor(manager, storage, options);
        await persistor.StartAsync();

        Assert.AreEqual("light", store.Theme);
        Assert.ContainsSingle(errors);
        Assert.AreEqual("persistence", errors[0].Source);
    }
}
