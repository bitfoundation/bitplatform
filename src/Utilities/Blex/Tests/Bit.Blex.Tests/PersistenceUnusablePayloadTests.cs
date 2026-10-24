namespace Bit.Blex.Tests;

/// <summary>
/// A stored payload the store cannot take -- valid JSON that is not an object, or a value of the wrong type --
/// is reported, discarded and removed like an unreadable one, instead of failing again on every load; and it
/// leaves the store as it was rather than half-restored. A debounced save still waiting when the page goes away
/// is written as it goes.
/// </summary>
[TestClass]
public class PersistenceUnusablePayloadTests
{
    private sealed class MemoryStorage : IBlexStorage, IBlexStorageSuspension
    {
        public Dictionary<string, string> Data { get; } = [];

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

        public event Action? Suspending;

        public int SuspensionWatches { get; private set; }

        public ValueTask StartWatchingSuspensionAsync(CancellationToken cancellationToken = default)
        {
            SuspensionWatches++;
            return ValueTask.CompletedTask;
        }

        public void Suspend() => Suspending?.Invoke();
    }

    private static (BlexManager Manager, SettingsStore Store, List<BlexError> Errors) Setup()
    {
        var errors = new List<BlexError>();
        var manager = new BlexManager { OnError = errors.Add };
        var store = new SettingsStore();
        manager.Register(store);
        return (manager, store, errors);
    }

    [TestMethod]
    public async Task A_value_the_store_cannot_take_is_reported_removed_and_applied_not_at_all()
    {
        var (manager, store, errors) = Setup();
        var storage = new MemoryStorage();
        storage.Data["blex:settings"] = """{"Theme":"dark","FontSize":"huge"}""";

        await using var persistor = new BlexStatePersistor(manager, storage);
        await persistor.StartAsync();

        Assert.Contains(e => e.Source == "persistence", errors);
        Assert.IsFalse(storage.Data.ContainsKey("blex:settings"), "it would fail the same way on every load");
        Assert.AreEqual("light", store.Theme, "the field read before the failing one must not be applied on its own");
        Assert.AreEqual(14, store.FontSize);
    }

    [TestMethod]
    [DataRow("[1,2,3]")]
    [DataRow("\"text\"")]
    [DataRow("null")]
    public async Task Json_that_is_not_an_object_is_reported_and_removed(string payload)
    {
        var (manager, store, errors) = Setup();
        var storage = new MemoryStorage();
        storage.Data["blex:settings"] = payload;

        await using var persistor = new BlexStatePersistor(manager, storage);
        await persistor.StartAsync();

        Assert.Contains(e => e.Source == "persistence", errors);
        Assert.IsFalse(storage.Data.ContainsKey("blex:settings"));
        Assert.AreEqual("light", store.Theme);
    }

    [TestMethod]
    public async Task A_debounced_save_is_written_when_the_page_is_hidden()
    {
        var (manager, store, errors) = Setup();
        var storage = new MemoryStorage();
        await using var persistor = new BlexStatePersistor(manager, storage, new BlexPersistenceOptions { DebounceInterval = TimeSpan.FromHours(1) });
        await persistor.StartAsync();
        Assert.AreEqual(1, storage.SuspensionWatches);

        store.SetTheme("dark");
        Assert.IsFalse(storage.Data.ContainsKey("blex:settings"), "still waiting out the debounce");

        storage.Suspend();

        Assert.Contains("dark", storage.Data["blex:settings"], "written synchronously, before the page is gone");
        Assert.IsEmpty(errors);
    }

    [TestMethod]
    public async Task A_disposed_persistor_stops_listening_for_the_page_going_away()
    {
        var (manager, store, _) = Setup();
        var storage = new MemoryStorage();
        var persistor = new BlexStatePersistor(manager, storage, new BlexPersistenceOptions { DebounceInterval = TimeSpan.FromHours(1) });
        await persistor.StartAsync();
        await persistor.DisposeAsync();
        storage.Data.Clear();

        store.SetTheme("dark");
        storage.Suspend();

        Assert.IsEmpty(storage.Data);
    }
}
