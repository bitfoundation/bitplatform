using Bit.Blex.Blazor;
using Bit.Blex.Query;
using Microsoft.JSInterop;
using static Bit.Blex.Tests.Query.QueryTest;

namespace Bit.Blex.Tests.Query;

/// <summary>
/// <see cref="BlexQueryFocusWatcher"/>: the browser telling the query cache that the tab is visible again or
/// the network is back, so stale observed queries refetch.
/// </summary>
[TestClass]
public class BlexQueryFocusWatcherTests
{
    [TestMethod]
    public async Task It_watches_through_the_query_module_and_lets_go_on_dispose()
    {
        var js = new FakeJsRuntime();
        js.Module.Returns["watch"] = 7;
        var (client, _, _) = Client();
        var watcher = new BlexQueryFocusWatcher(js, client, new BlexManager());

        await watcher.StartAsync();

        Assert.AreEqual("./_content/Bit.Blex/bit-blex-query.js", Assert.ContainsSingle(js.Imports));
        Assert.IsInstanceOfType<DotNetObjectReference<BlexQueryFocusWatcher>>(js.Module.ArgsOf("watch")![0]);

        await watcher.DisposeAsync();

        Assert.AreEqual(7, js.Module.ArgsOf("unwatch")![0]);
        Assert.IsTrue(js.Module.Disposed);
    }

    [TestMethod]
    public async Task It_reports_the_network_going_away_only_where_asked_to()
    {
        var js = new FakeJsRuntime();
        var (client, _, _) = Client();
        var watcher = new BlexQueryFocusWatcher(js, client, new BlexManager());

        await watcher.StartAsync(reportOffline: true);
        Assert.AreEqual(true, js.Module.ArgsOf("watch")![1]);

        watcher.OnOffline();
        Assert.IsFalse(client.IsOnline);
        watcher.OnReconnected();
        Assert.IsTrue(client.IsOnline);

        var serverSide = new FakeJsRuntime();
        await new BlexQueryFocusWatcher(serverSide, client, new BlexManager()).StartAsync();
        Assert.AreEqual(false, serverSide.Module.ArgsOf("watch")![1], "Blazor Server: the browser's network is not the requests'");
    }

    [TestMethod]
    public async Task With_SyncAcrossTabs_invalidations_travel_both_ways_and_stop_with_the_watcher()
    {
        var js = new FakeJsRuntime();
        js.Module.Returns["watch"] = 3;
        var (client, _, _) = Client(o => o.SyncAcrossTabs = true);
        var server = new FakeServer<string>();
        using var todo = client.Query(["todos", 42], server.Fetch());
        server.Respond(0, "milk");
        await todo.WhenSettledAsync();
        var watcher = new BlexQueryFocusWatcher(js, client, new BlexManager());

        await watcher.StartAsync();
        Assert.AreEqual("bit-blex-query", js.Module.ArgsOf("watch")![2]);

        // Each invalidation refetches the observed todo, and completes once that refetch has been answered.
        var here = client.InvalidateAsync(["todos"]);   // here: passed on
        await Until(() => js.Module.ArgsOf("broadcastInvalidation") is not null);
        Assert.AreSequenceEqual(new object?[] { 3, """["todos"]""", false }, js.Module.ArgsOf("broadcastInvalidation")!);
        server.Respond(1, "milk");
        await here.WaitAsync(TimeSpan.FromSeconds(5));

        var there = watcher.OnInvalidatedElsewhere("""["todos"]""", exact: false);   // there: applied, not passed on again
        Assert.AreEqual(3, server.Calls, "the other tab's invalidation refetched this tab's todo");
        server.Respond(2, "milk");
        await there.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual(1, js.Module.CountOf("broadcastInvalidation"));

        await watcher.DisposeAsync();
        var afterwards = client.InvalidateAsync(["todos"]);
        server.Respond(3, "milk");
        await afterwards.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual(1, js.Module.CountOf("broadcastInvalidation"), "a disposed watcher no longer passes anything on");
    }

    [TestMethod]
    public async Task An_invalidation_from_elsewhere_matches_keys_part_by_part()
    {
        var (client, _, _) = Client();
        client.SetQueryData<string>(["todos", 42], "a");
        client.SetQueryData<string>(["todos,42"], "b");      // one part with a comma in it
        client.SetQueryData<string>(["todosX"], "c");
        client.SetQueryData<string>(["todos"], "d");

        await client.InvalidateFromElsewhereAsync("""["todos"]""", exact: true);
        Assert.AreSequenceEqual(["""["todos"]"""], Invalidated(client));

        await client.InvalidateFromElsewhereAsync("""["todos"]""", exact: false);
        Assert.AreSequenceEqual(["""["todos",42]""", """["todos"]"""], Invalidated(client));

        static string[] Invalidated(BlexQueryClient client)
            => [.. client.GetQueries().Where(q => q.IsInvalidated).Select(q => q.Key.ToString()).Order(StringComparer.Ordinal)];
    }

    [TestMethod]
    public async Task Focus_and_reconnect_refetch_stale_observed_queries()
    {
        var (client, clock, _) = Client();
        var server = new FakeServer<int>();
        using var query = client.Query(["n"], server.Fetch(), new BlexQueryOptions { StaleTime = TimeSpan.FromMinutes(1) });
        server.Respond(0, 1);
        await query.WhenSettledAsync();
        var watcher = new BlexQueryFocusWatcher(new FakeJsRuntime(), client, new BlexManager());

        watcher.OnFocused();
        Assert.AreEqual(1, server.Calls, "fresh data is not refetched");

        clock.Advance(TimeSpan.FromMinutes(1));
        watcher.OnFocused();
        Assert.AreEqual(2, server.Calls);

        server.Respond(1, 2);
        await query.WhenSettledAsync();
        clock.Advance(TimeSpan.FromMinutes(1));
        watcher.OnReconnected();
        Assert.AreEqual(3, server.Calls);
    }

    [TestMethod]
    public async Task A_page_that_went_away_while_starting_is_not_an_error()
    {
        var js = new FakeJsRuntime { ImportFailure = () => new JSDisconnectedException("gone") };
        var errors = new List<BlexError>();
        var (client, _, _) = Client();
        var watcher = new BlexQueryFocusWatcher(js, client, new BlexManager { OnError = errors.Add });

        await watcher.StartAsync();
        await watcher.DisposeAsync();

        Assert.IsEmpty(errors);
    }
}
