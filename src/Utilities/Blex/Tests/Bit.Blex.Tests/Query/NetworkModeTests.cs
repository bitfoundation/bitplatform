using Bit.Blex.Query;
using static Bit.Blex.Tests.Query.QueryTest;

namespace Bit.Blex.Tests.Query;

/// <summary>
/// <see cref="BlexNetworkMode"/>: offline, requests wait -- paused, not failed -- and go on once the network returns,
/// instead of spending their retries on requests that cannot succeed.
/// </summary>
[TestClass]
public class NetworkModeTests
{
    [TestMethod]
    public async Task A_fetch_started_offline_waits_paused_and_runs_once_back_online()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<string>();
        client.NotifyOffline();

        using var query = client.Query(["n"], server.Fetch());

        Assert.IsTrue(query.IsPaused);
        Assert.IsTrue(query.IsPending);
        Assert.IsFalse(query.IsLoading, "nothing is being fetched");
        Assert.AreEqual(0, server.Calls);

        client.NotifyReconnected();
        await Until(() => server.Calls == 1);
        Assert.IsTrue(query.IsFetching);

        server.Respond(0, "v");
        await query.WhenSettledAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual("v", query.Data);
    }

    [TestMethod]
    public async Task Going_offline_between_retries_pauses_them_instead_of_spending_them()
    {
        var (client, clock, _) = Client(o => { o.RetryCount = 1; o.RetryDelay = TimeSpan.FromSeconds(1); });
        var server = new FakeServer<string>();
        using var query = client.Query(["n"], server.Fetch());

        server.Fail(0, new HttpRequestException("network down"));
        await Until(() => query.FailureCount == 1);
        client.NotifyOffline();
        await Until(() => clock.Timers > 0);
        clock.Advance(TimeSpan.FromSeconds(1));

        await Until(() => query.IsPaused);
        Assert.AreEqual(1, server.Calls, "the retry waits for the network");

        client.NotifyReconnected();
        await Until(() => server.Calls == 2);
        server.Respond(1, "v");
        await query.WhenSettledAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(query.IsSuccess);
    }

    [TestMethod]
    public async Task Always_fetches_whatever_the_network()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<string>();
        client.NotifyOffline();

        using var query = client.Query(["local"], server.Fetch(), new BlexQueryOptions { NetworkMode = BlexNetworkMode.Always });

        Assert.AreEqual(1, server.Calls);
        Assert.IsTrue(query.IsFetching);
        server.Respond(0, "from IndexedDB");
        await query.WhenSettledAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual("from IndexedDB", query.Data);
    }

    [TestMethod]
    public async Task A_mutation_made_offline_shows_its_optimistic_update_and_is_sent_once_online()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<int>();
        client.SetQueryData<int>(["count"], 1);
        var increment = client.Mutation<int, int>(
            (by, ct) => server.Fetch()(ct),
            new BlexMutationOptions<int, int> { OnMutate = by => client.Optimistic<int>(["count"], n => n + by) });
        client.NotifyOffline();

        var sent = increment.MutateAsync(1);

        Assert.IsTrue(increment.IsPending);
        Assert.IsTrue(increment.IsPaused);
        Assert.AreEqual(2, client.GetQueryData<int>(["count"]), "the optimistic update shows at once");
        Assert.AreEqual(0, server.Calls);

        client.NotifyReconnected();
        await Until(() => server.Calls == 1);
        Assert.IsFalse(increment.IsPaused);
        server.Respond(0, 2);
        await sent.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(increment.IsSuccess);
    }

    [TestMethod]
    public async Task A_paused_fetch_that_is_cancelled_or_evicted_never_sends_a_request()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<string>();
        client.NotifyOffline();

        // Not refetched on reconnect, so what reaches the server is only what was paused.
        using var cancelled = client.Query(["a"], server.Fetch(), new BlexQueryOptions { RefetchOnFocus = false });
        using var removed = client.Query(["b"], server.Fetch(), new BlexQueryOptions { RefetchOnFocus = false });

        client.Cancel(["a"]);
        Assert.IsFalse(cancelled.IsPaused, "cancelled: idle again");
        Assert.IsFalse(cancelled.IsFetching);

        client.Remove(["b"]);   // its observer starts over on a fresh entry: a new fetch, paused too
        client.NotifyReconnected();
        await Until(() => server.Calls == 1);
        await Task.Delay(50);

        Assert.AreEqual(1, server.Calls, "the cancelled fetch and the evicted one never ran; only the fresh entry's did");
    }

    [TestMethod]
    public async Task A_read_through_waits_for_the_network_and_honours_its_token()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<string>();
        client.NotifyOffline();
        using var cts = new CancellationTokenSource();

        var read = client.FetchAsync(["n"], server.Fetch(), cancellationToken: cts.Token);
        await Task.Delay(20);
        Assert.IsFalse(read.IsCompleted);

        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => read);
        Assert.AreEqual(0, server.Calls);
    }

    [TestMethod]
    public async Task A_mutation_settles_offline_without_waiting_for_the_refetches_it_invalidated()
    {
        var (client, _, _) = Client();
        var lists = new FakeServer<string>();
        using var list = client.Query(["drafts"], lists.Fetch());
        lists.Respond(0, "before");
        await list.WhenSettledAsync();
        client.NotifyOffline();

        // A local write (IndexedDB) that runs offline, invalidating a list that waits for the network.
        var save = client.Mutation<string, string>(
            (draft, ct) => Task.FromResult(draft),
            new BlexMutationOptions<string, string> { NetworkMode = BlexNetworkMode.Always, Invalidates = [["drafts"]] });

        await save.MutateAsync("x").WaitAsync(TimeSpan.FromSeconds(5));

        Assert.IsTrue(save.IsSuccess);
        Assert.IsTrue(list.IsPaused, "the list refetches once the network is back");
    }

    [TestMethod]
    public async Task A_watcher_that_goes_away_does_not_leave_the_cache_offline()
    {
        var (client, _, _) = Client();
        var watcher = new Bit.Blex.Blazor.BlexQueryFocusWatcher(new FakeJsRuntime(), client, new BlexManager());
        await watcher.StartAsync(reportOffline: true);
        watcher.OnOffline();
        Assert.IsFalse(client.IsOnline);

        await watcher.DisposeAsync();   // the page went away; nobody is left to hear "online"

        Assert.IsTrue(client.IsOnline);
    }

    [TestMethod]
    public void Reconnecting_when_already_online_changes_nothing()
    {
        var (client, _, _) = Client();
        Assert.IsTrue(client.IsOnline);
        client.NotifyReconnected();
        Assert.IsTrue(client.IsOnline);

        client.NotifyOffline();
        client.NotifyOffline();
        Assert.IsFalse(client.IsOnline);
    }
}
