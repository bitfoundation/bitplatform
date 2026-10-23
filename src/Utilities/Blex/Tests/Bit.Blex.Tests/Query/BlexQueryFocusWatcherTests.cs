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
