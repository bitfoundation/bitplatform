using System.Text.Json.Nodes;

namespace Bit.Blex.Tests;

[BlexStoreAttribute(Name = "search", Persist = true)]
public partial class SearchHistoryStore
{
    [BlexStateAttribute] private string[] _recent = [];
    [BlexStateAttribute(Persist = false)] private string _query = "";
    [BlexStateAttribute(Persist = false)] private int _page = 1;

    [BlexActionAttribute]
    private void OnType(string query) => Query = query;

    [BlexActionAttribute]
    private void OnCommit() => Recent = [Query, .. Recent];
}

/// <summary>
/// <c>[BlexState(Persist = false)]</c>: a field of a persistent store that never reaches storage, and
/// is ignored when storage still holds a value for it -- while staying part of every snapshot.
/// </summary>
[TestClass]
public class TransientStateTests
{
    private sealed class WatchableStorage : IBlexStorage, IBlexExternalStorageChange
    {
        public Dictionary<string, string> Data { get; } = [];

        public event Action<string?>? ExternalChange;

        public void RaiseExternal(string key, string value)
        {
            Data[key] = value;
            ExternalChange?.Invoke(key);
        }

        public ValueTask StartWatchingAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

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

    private static async Task<(BlexManager, SearchHistoryStore, WatchableStorage, BlexStatePersistor)> StartAsync(
        BlexPersistenceOptions? options = null,
        string? stored = null)
    {
        var manager = new BlexManager();
        var store = new SearchHistoryStore();
        manager.Register(store);

        var storage = new WatchableStorage();
        if (stored is not null)
            storage.Data["blex:search"] = stored;

        var persistor = new BlexStatePersistor(manager, storage, options);
        await persistor.StartAsync();
        return (manager, store, storage, persistor);
    }

    [TestMethod]
    public async Task A_transient_field_is_never_written()
    {
        var (_, store, storage, persistor) = await StartAsync();

        store.Type("blazor");
        store.Commit();
        await persistor.FlushAsync();

        var written = JsonNode.Parse(storage.Data["blex:search"])!.AsObject();
        Assert.IsTrue(written.ContainsKey("Recent"));
        Assert.IsFalse(written.ContainsKey("Query"));
        Assert.IsFalse(written.ContainsKey("Page"));
    }

    [TestMethod]
    public async Task A_stored_value_for_a_transient_field_is_ignored()
    {
        // Written before the field was excluded.
        var (_, store, _, _) = await StartAsync(stored: """{"Recent":["one"],"Query":"stale","Page":7}""");

        CollectionAssert.AreEqual(new[] { "one" }, store.Recent);
        Assert.AreEqual("", store.Query);
        Assert.AreEqual(1, store.Page);
    }

    [TestMethod]
    public async Task A_change_from_another_tab_does_not_carry_transient_fields_either()
    {
        var (_, store, storage, persistor) = await StartAsync(new BlexPersistenceOptions { SyncAcrossTabs = true });
        store.Type("mine");

        storage.RaiseExternal("blex:search", """{"Recent":["theirs"],"Query":"theirs"}""");
        await persistor.FlushAsync();

        CollectionAssert.AreEqual(new[] { "theirs" }, store.Recent);
        Assert.AreEqual("mine", store.Query);
    }

    [TestMethod]
    public async Task Partialize_runs_on_what_is_left()
    {
        JsonObject? seen = null;
        var (_, store, _, persistor) = await StartAsync(new BlexPersistenceOptions
        {
            Partialize = (_, state) => seen = (JsonObject)state.DeepClone(),
        });

        store.Type("x");
        await persistor.FlushAsync();

        Assert.IsNotNull(seen);
        Assert.IsFalse(seen.ContainsKey("Query"));
    }

    [TestMethod]
    public void A_transient_field_is_still_part_of_the_snapshot()
    {
        var store = new SearchHistoryStore();
        store.Type("still here");

        Assert.AreEqual("still here", store.SerializeState()["Query"]!.GetValue<string>());
    }
}
