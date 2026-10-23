using Bit.Blex.Query;
using static Bit.Blex.Tests.Query.QueryTest;

namespace Bit.Blex.Tests.Query;

/// <summary>
/// <see cref="BlexQueryClient"/>: fetching, sharing, freshness, retries, invalidation, cancellation and
/// garbage collection of cached server state.
/// </summary>
[TestClass]
public class BlexQueryClientTests
{
    private static readonly BlexQueryOptions Fresh = new() { StaleTime = TimeSpan.FromMinutes(1) };

    [TestMethod]
    public async Task A_query_fetches_and_moves_from_loading_to_success()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<string>();

        using var query = client.Query(["greeting"], server.Fetch());

        Assert.AreEqual(1, server.Calls);
        Assert.IsTrue(query.IsLoading);
        Assert.IsTrue(query.IsPending);
        Assert.IsFalse(query.HasData);

        server.Respond(0, "hello");
        await query.WhenSettledAsync();

        Assert.IsTrue(query.IsSuccess);
        Assert.IsFalse(query.IsFetching);
        Assert.AreEqual("hello", query.Data);
        Assert.IsNotNull(query.DataUpdatedAt);
    }

    [TestMethod]
    public async Task Observers_mounting_together_share_one_request()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<int>();

        using var a = client.Query(["n"], server.Fetch());
        using var b = client.Query(["n"], server.Fetch());
        using var c = client.Query(["n"], server.Fetch());

        Assert.AreEqual(1, server.Calls, "the observers that mounted while a fetch ran must join it");

        server.Respond(0, 7);
        await a.WhenSettledAsync();

        Assert.AreEqual(7, b.Data);
        Assert.AreEqual(7, c.Data);
    }

    [TestMethod]
    public async Task Fresh_data_is_served_without_a_request()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<int>();

        using (var first = client.Query(["n"], server.Fetch(), Fresh))
        {
            server.Respond(0, 1);
            await first.WhenSettledAsync();
        }

        using var second = client.Query(["n"], server.Fetch(), Fresh);

        Assert.AreEqual(1, server.Calls);
        Assert.AreEqual(1, second.Data);
        Assert.IsFalse(second.IsFetching);
    }

    [TestMethod]
    public async Task Stale_data_is_shown_while_it_is_refetched()
    {
        var (client, clock, _) = Client();
        var server = new FakeServer<int>();

        using (var first = client.Query(["n"], server.Fetch(), Fresh))
        {
            server.Respond(0, 1);
            await first.WhenSettledAsync();
        }

        clock.Advance(TimeSpan.FromMinutes(1));   // now stale
        using var second = client.Query(["n"], server.Fetch(), Fresh);

        Assert.AreEqual(2, server.Calls);
        Assert.AreEqual(1, second.Data, "the cached copy is shown meanwhile");
        Assert.IsTrue(second.IsRefetching);
        Assert.IsFalse(second.IsLoading);

        server.Respond(1, 2);
        await second.WhenSettledAsync();
        Assert.AreEqual(2, second.Data);
    }

    [TestMethod]
    public async Task A_failed_fetch_keeps_earlier_data_alongside_its_error()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<int>();
        using var query = client.Query(["n"], server.Fetch());
        server.Respond(0, 1);
        await query.WhenSettledAsync();

        var refetch = query.RefetchAsync();
        server.Fail(1, new InvalidOperationException("down"));
        await refetch;

        Assert.IsTrue(query.IsError);
        Assert.AreEqual("down", query.Error?.Message);
        Assert.AreEqual(1, query.Data, "data from the earlier success is kept");
        Assert.IsTrue(query.IsStale, "a failed refetch leaves the data stale");
    }

    [TestMethod]
    public async Task A_failure_is_retried_with_backoff_before_it_becomes_the_error()
    {
        var (client, clock, _) = Client(o => o.RetryCount = 2);
        var server = new FakeServer<int>();

        using var query = client.Query(["n"], server.Fetch());
        server.Fail(0, new InvalidOperationException("1"));
        await Until(() => clock.Timers == 1, "the first backoff");

        Assert.AreEqual(1, query.FailureCount);
        Assert.AreEqual("1", query.FailureReason?.Message);
        Assert.IsNull(query.Error, "a failure that will be retried is not the query's error yet");
        Assert.IsTrue(query.IsLoading);

        clock.Advance(TimeSpan.FromMilliseconds(999));
        Assert.AreEqual(1, server.Calls);
        clock.Advance(TimeSpan.FromMilliseconds(1));
        await Until(() => server.Calls == 2);

        server.Fail(1, new InvalidOperationException("2"));
        await Until(() => clock.Timers == 2, "the second backoff");
        clock.Advance(TimeSpan.FromMilliseconds(1999));
        Assert.AreEqual(2, server.Calls, "the wait doubles");
        clock.Advance(TimeSpan.FromMilliseconds(1));
        await Until(() => server.Calls == 3);

        server.Fail(2, new InvalidOperationException("3"));
        await query.WhenSettledAsync();

        Assert.IsTrue(query.IsError);
        Assert.AreEqual("3", query.Error?.Message);
        Assert.AreEqual(3, query.FailureCount);
    }

    [TestMethod]
    public async Task Invalidation_refetches_observed_queries_and_marks_the_rest_stale()
    {
        var (client, _, _) = Client();
        var todos = new FakeServer<string>();
        var users = new FakeServer<string>();

        using var list = client.Query(["todos"], todos.Fetch(), Fresh);
        using var one = client.Query(["todos", 1], todos.Fetch(), Fresh);
        using var user = client.Query(["users", 1], users.Fetch(), Fresh);
        todos.Respond(0, "list v1");
        todos.Respond(1, "one v1");
        users.Respond(0, "ada");
        await Task.WhenAll(list.WhenSettledAsync(), one.WhenSettledAsync(), user.WhenSettledAsync());

        // An unobserved query under the prefix: marked, not fetched.
        client.SetQueryData<string>(["todos", 2], "two v1");

        var invalidation = client.InvalidateAsync(["todos"]);

        Assert.AreEqual(4, todos.Calls, "both observed todos queries refetch");
        Assert.AreEqual(1, users.Calls, "a key outside the prefix is untouched");

        todos.Respond(2, "list v2");
        todos.Respond(3, "one v2");
        await invalidation;

        Assert.AreEqual("list v2", list.Data);
        Assert.AreEqual("one v2", one.Data);
        Assert.IsFalse(user.IsStale);

        using var two = client.Query(["todos", 2], todos.Fetch(), Fresh);
        Assert.AreEqual(5, todos.Calls, "the invalidated, unobserved query refetches once observed");
        Assert.AreEqual("two v1", two.Data);
    }

    [TestMethod]
    public async Task An_exact_invalidation_covers_only_its_key()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<string>();
        using var list = client.Query(["todos"], server.Fetch(), Fresh);
        using var one = client.Query(["todos", 1], server.Fetch(), Fresh);
        server.Respond(0, "list");
        server.Respond(1, "one");
        await Task.WhenAll(list.WhenSettledAsync(), one.WhenSettledAsync());

        _ = client.InvalidateAsync(["todos"], exact: true);

        Assert.AreEqual(3, server.Calls);
        Assert.IsFalse(one.IsStale);
    }

    [TestMethod]
    public async Task A_refetch_supersedes_the_running_fetch_whose_answer_is_then_ignored()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<string>();
        using var query = client.Query(["n"], server.Fetch(ignoreToken: true));

        var refetch = query.RefetchAsync();
        Assert.IsTrue(server.WasCancelled(0), "the superseded request is told to stop");

        server.Respond(1, "new");
        await refetch;
        server.Respond(0, "old");   // the slow, superseded response lands last
        await Task.Delay(20);

        Assert.AreEqual("new", query.Data, "a stale response must never overwrite a newer one");
    }

    [TestMethod]
    public async Task Cancelling_returns_to_idle_with_what_was_there()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<string>();
        using var query = client.Query(["n"], server.Fetch());
        server.Respond(0, "kept");
        await query.WhenSettledAsync();

        var refetch = query.RefetchAsync();
        client.Cancel(["n"]);
        await refetch;

        Assert.IsFalse(query.IsFetching);
        Assert.AreEqual("kept", query.Data);
        Assert.IsNull(query.Error, "cancellation is not a failure");
    }

    [TestMethod]
    public async Task An_unobserved_query_is_collected_after_its_gc_time()
    {
        var (client, clock, _) = Client(o => o.GcTime = TimeSpan.FromSeconds(30));
        var server = new FakeServer<string>();

        var query = client.Query(["n"], server.Fetch());
        server.Respond(0, "cached");
        await query.WhenSettledAsync();
        query.Dispose();

        clock.Advance(TimeSpan.FromSeconds(29));
        Assert.AreEqual("cached", client.GetQueryData<string>(["n"]));

        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.IsFalse(client.TryGetQueryData<string>(["n"], out _));
        Assert.IsEmpty(client.GetQueries());
    }

    [TestMethod]
    public async Task Observing_again_before_collection_keeps_the_query()
    {
        var (client, clock, _) = Client(o => o.GcTime = TimeSpan.FromSeconds(30));
        var server = new FakeServer<string>();

        var first = client.Query(["n"], server.Fetch(), Fresh);
        server.Respond(0, "cached");
        await first.WhenSettledAsync();
        first.Dispose();

        clock.Advance(TimeSpan.FromSeconds(20));
        using var second = client.Query(["n"], server.Fetch(), Fresh);
        clock.Advance(TimeSpan.FromMinutes(5));

        Assert.AreEqual("cached", second.Data);
        Assert.AreEqual(1, server.Calls);
        Assert.ContainsSingle(client.GetQueries());
    }

    [TestMethod]
    public async Task A_disabled_query_waits_until_it_is_enabled()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<string>();

        using var query = client.Query(["user", 1], server.Fetch(), new BlexQueryOptions { Enabled = false });
        Assert.AreEqual(0, server.Calls);
        Assert.IsTrue(query.IsPending);
        Assert.IsFalse(query.IsLoading, "nothing is loading while the query is disabled");

        query.Update(server.Fetch(), new BlexQueryOptions { Enabled = true });
        Assert.AreEqual(1, server.Calls);

        server.Respond(0, "ada");
        await query.WhenSettledAsync();
        Assert.AreEqual("ada", query.Data);
    }

    [TestMethod]
    public async Task A_refetch_interval_polls_while_observed()
    {
        var (client, clock, _) = Client();
        var server = new FakeServer<int>();

        var query = client.Query(["n"], server.Fetch(), new BlexQueryOptions { RefetchInterval = TimeSpan.FromSeconds(10), StaleTime = TimeSpan.MaxValue });
        server.Respond(0, 1);
        await query.WhenSettledAsync();

        clock.Advance(TimeSpan.FromSeconds(10));
        await Until(() => server.Calls == 2);
        server.Respond(1, 2);
        await query.WhenSettledAsync();
        Assert.AreEqual(2, query.Data);

        query.Dispose();
        clock.Advance(TimeSpan.FromSeconds(30));
        await Task.Delay(20);
        Assert.AreEqual(2, server.Calls, "polling stops with the observer");
    }

    [TestMethod]
    public async Task Focus_refetches_stale_observed_queries_only()
    {
        var (client, clock, _) = Client();
        var server = new FakeServer<int>();
        using var stale = client.Query(["stale"], server.Fetch(), Fresh);
        using var noFocus = client.Query(["opt-out"], server.Fetch(), Fresh with { RefetchOnFocus = false });
        server.Respond(0, 1);
        server.Respond(1, 1);
        await Task.WhenAll(stale.WhenSettledAsync(), noFocus.WhenSettledAsync());

        client.NotifyFocused();
        Assert.AreEqual(2, server.Calls, "fresh data is not refetched on focus");

        clock.Advance(TimeSpan.FromMinutes(1));
        client.NotifyFocused();
        Assert.AreEqual(3, server.Calls, "only the stale query that wants it refetches");
    }

    [TestMethod]
    public async Task FetchAsync_reads_through_the_cache()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<int>();

        var first = client.FetchAsync(["n"], server.Fetch(), Fresh);
        server.Respond(0, 5);
        Assert.AreEqual(5, await first);

        Assert.AreEqual(5, await client.FetchAsync(["n"], server.Fetch(), Fresh));
        Assert.AreEqual(1, server.Calls, "fresh data is served from the cache");
    }

    [TestMethod]
    public async Task FetchAsync_throws_the_failure()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<int>();

        var fetch = client.FetchAsync(["n"], server.Fetch());
        server.Fail(0, new InvalidOperationException("nope"));

        var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => fetch);
        Assert.AreEqual("nope", error.Message);
    }

    [TestMethod]
    public async Task Prefetching_fills_the_cache_for_the_observer_that_comes_next()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<int>();

        var prefetch = client.PrefetchAsync(["n"], server.Fetch(), Fresh);
        server.Respond(0, 3);
        await prefetch;

        using var query = client.Query(["n"], server.Fetch(), Fresh);
        Assert.AreEqual(3, query.Data);
        Assert.AreEqual(1, server.Calls);
    }

    [TestMethod]
    public void One_key_cannot_hold_two_types()
    {
        var (client, _, _) = Client();
        client.SetQueryData<int>(["n"], 1);

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => client.SetQueryData<string>(["n"], "1"));
        Assert.Contains("different keys", error.Message);
    }

    [TestMethod]
    public void SetQueryData_notifies_observers_and_can_derive_from_the_cached_value()
    {
        var (client, _, _) = Client();
        client.SetQueryData<int>(["n"], 1);
        using var query = client.Query(["n"], _ => Task.FromResult(0), new BlexQueryOptions { StaleTime = TimeSpan.MaxValue });
        var changes = 0;
        query.Changed += () => changes++;

        client.SetQueryData<int>(["n"], n => n + 1);

        Assert.AreEqual(2, query.Data);
        Assert.AreEqual(1, changes);
    }

    [TestMethod]
    public async Task An_equal_result_keeps_the_cached_instance()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<Page>();
        using var query = client.Query(["page"], server.Fetch());
        server.Respond(0, new Page(1, 20));
        await query.WhenSettledAsync();
        var first = query.Data;

        var refetch = query.RefetchAsync();
        server.Respond(1, new Page(1, 20));
        await refetch;

        Assert.AreSame(first, query.Data, "an equal refetch must not hand components a new instance");
    }

    [TestMethod]
    public void A_throwing_observer_is_isolated_and_reported()
    {
        var (client, _, errors) = Client();
        client.SetQueryData<int>(["n"], 1);
        using var query = client.Query(["n"], _ => Task.FromResult(0), new BlexQueryOptions { StaleTime = TimeSpan.MaxValue });
        var reached = false;
        query.Changed += () => throw new InvalidOperationException("bad handler");
        query.Changed += () => reached = true;

        client.SetQueryData<int>(["n"], 2);

        Assert.IsTrue(reached, "a throwing handler must not starve the ones behind it");
        var error = Assert.ContainsSingle(errors);
        Assert.AreEqual("query", error.Source);
    }

    [TestMethod]
    public async Task Disposing_the_client_cancels_its_fetches()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<int>();
        var query = client.Query(["n"], server.Fetch());

        client.Dispose();
        await query.WhenSettledAsync();

        Assert.IsTrue(server.WasCancelled(0));
        Assert.IsEmpty(client.GetQueries());
        Assert.ThrowsExactly<ObjectDisposedException>(() => client.Query(["n"], server.Fetch()));
    }

    [TestMethod]
    public async Task Timers_run_on_the_managers_dispatcher()
    {
        var clock = new CountingClock();
        var dispatcher = new CountingDispatcher();
        var manager = new BlexManager { TimeProvider = clock, Dispatcher = dispatcher };
        var client = new BlexQueryClient(manager, new BlexQueryClientOptions { GcTime = TimeSpan.FromSeconds(1) });

        var query = client.Query<int>(["n"], _ => Task.FromResult(1));
        await query.WhenSettledAsync();
        query.Dispose();

        clock.Advance(TimeSpan.FromSeconds(1));
        await Until(() => client.GetQueries().Count == 0);

        Assert.AreEqual(1, dispatcher.Invocations, "garbage collection touches the cache from the dispatcher");
    }

    [TestMethod]
    public async Task The_cache_reports_what_it_holds()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<int>();
        using var query = client.Query(["n"], server.Fetch());

        var info = Assert.ContainsSingle(client.GetQueries());
        Assert.AreEqual(BlexFetchStatus.Fetching, info.FetchStatus);
        Assert.AreEqual(1, info.ObserverCount);
        Assert.AreEqual(typeof(int), info.DataType);
        Assert.AreEqual(1, client.FetchingCount);

        server.Respond(0, 1);
        await query.WhenSettledAsync();
        Assert.AreEqual(0, client.FetchingCount);
    }

    private sealed class CountingDispatcher : IBlexDispatcher
    {
        public int Invocations { get; private set; }

        public bool CheckAccess() => false;

        public Task InvokeAsync(Func<Task> workItem)
        {
            Invocations++;
            return workItem();
        }
    }
}
