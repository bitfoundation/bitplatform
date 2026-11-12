using Bit.Blex.Query;
using static Bit.Blex.Tests.Query.QueryTest;

namespace Bit.Blex.Tests.Query;

/// <summary>
/// Regressions found auditing the query cache: what a rollback puts back, per-query GC times, cancellations reported in
/// a fetch's own words, invalidations of handed-over queries nobody claimed yet, and a server clock running ahead.
/// </summary>
[TestClass]
public class QueryCacheRegressionTests
{
    private static readonly BlexQueryOptions Forever = new() { StaleTime = TimeSpan.MaxValue };

    private static BlexMutation<string, string> FailingAdd(BlexQueryClient client, FakeServer<string> server)
        => client.Mutation<string, string>(
            (_, ct) => server.Fetch()(ct),
            new() { OnMutate = text => client.Optimistic<IReadOnlyList<string>>(["todos"], items => [.. items ?? [], text]) });

    [TestMethod]
    public async Task A_rollback_keeps_the_error_and_the_staleness_of_a_failed_refetch()
    {
        var (client, _, _) = Client();
        var lists = new FakeServer<IReadOnlyList<string>>();
        using var list = client.Query(["todos"], lists.Fetch(), Forever);
        lists.Respond(0, ["a"]);
        await list.WhenSettledAsync();

        var invalidation = client.InvalidateAsync(["todos"]);
        lists.Fail(1, new InvalidOperationException("down"));
        await invalidation;
        Assert.IsTrue(list.IsError);
        Assert.IsTrue(list.IsStale);

        var server = new FakeServer<string>();
        var call = FailingAdd(client, server).MutateAsync("b");
        server.Fail(0, new InvalidOperationException("rejected"));
        await call;

        Assert.AreSequenceEqual(["a"], list.Data!.ToList());
        Assert.IsTrue(list.IsError, "the refetch that failed before the optimistic update still failed");
        Assert.AreEqual("down", list.Error?.Message);
        Assert.IsTrue(list.IsStale, "the invalidated query is still owed its refetch");
    }

    [TestMethod]
    public async Task A_rollback_to_a_failed_first_fetch_keeps_its_error()
    {
        var (client, _, _) = Client();
        var lists = new FakeServer<IReadOnlyList<string>>();
        using var list = client.Query(["todos"], lists.Fetch(), Forever);
        lists.Fail(0, new InvalidOperationException("down"));
        await list.WhenSettledAsync();
        Assert.IsTrue(list.IsError);

        var server = new FakeServer<string>();
        var call = FailingAdd(client, server).MutateAsync("b");
        server.Fail(0, new InvalidOperationException("rejected"));
        await call;

        Assert.IsFalse(list.HasData);
        Assert.IsTrue(list.IsError);
        Assert.IsNotNull(list.Error, "an Error status always comes with its error");
    }

    [TestMethod]
    public async Task A_query_may_ask_for_a_shorter_gc_time_than_the_clients_default()
    {
        var (client, clock, _) = Client();   // the default is 5 minutes
        var query = client.Query<int>(["n"], _ => Task.FromResult(1), new() { GcTime = TimeSpan.FromSeconds(1) });
        await query.WhenSettledAsync();
        query.Dispose();

        clock.Advance(TimeSpan.FromSeconds(1));

        Assert.IsFalse(client.TryGetQueryData<int>(["n"], out _));
    }

    [TestMethod]
    public async Task An_infinite_gc_time_keeps_the_query_for_good()
    {
        var (client, clock, _) = Client();
        var query = client.Query<int>(["n"], _ => Task.FromResult(1), new() { GcTime = Timeout.InfiniteTimeSpan });
        await query.WhenSettledAsync();
        query.Dispose();

        clock.Advance(TimeSpan.FromHours(1));

        Assert.IsTrue(client.TryGetQueryData<int>(["n"], out var data));
        Assert.AreEqual(1, data);
    }

    [TestMethod]
    public async Task The_longest_gc_time_any_query_asked_for_wins()
    {
        var (client, clock, _) = Client();
        var shortLived = client.Query<int>(["n"], _ => Task.FromResult(1), new() { GcTime = TimeSpan.FromSeconds(1) });
        var longLived = client.Query<int>(["n"], _ => Task.FromResult(1), new() { GcTime = TimeSpan.FromMinutes(1) });
        await shortLived.WhenSettledAsync();
        shortLived.Dispose();
        longLived.Dispose();

        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.IsTrue(client.TryGetQueryData<int>(["n"], out _));

        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.IsFalse(client.TryGetQueryData<int>(["n"], out _));
    }

    [TestMethod]
    public async Task Cancelling_a_fetch_that_reports_it_in_its_own_words_is_no_error()
    {
        var (client, _, errors) = Client();
        var calls = 0;
        using var query = client.Query<string>(["n"], ct =>
        {
            if (calls++ == 0)
                return Task.FromResult("kept");

            // gRPC throws RpcException(Cancelled), an aborted stream an IOException: not an OperationCanceledException.
            var response = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            ct.Register(() => response.TrySetException(new IOException("aborted")));
            return response.Task;
        });
        await query.WhenSettledAsync();

        var refetch = query.RefetchAsync();
        client.Cancel(["n"]);
        await refetch;

        Assert.AreEqual("kept", query.Data);
        Assert.IsFalse(query.IsError);
        Assert.IsNull(query.Error);
        Assert.IsFalse(query.IsFetching);
        Assert.IsEmpty(errors);
    }

    [TestMethod]
    public async Task An_invalidation_reaches_a_handed_over_query_nobody_claimed_yet()
    {
        var (prerender, _, _) = Client();
        prerender.SetQueryData<string>(["todos", 1], "old");
        var (interactive, _, _) = Client();
        interactive.Hydrate(prerender.Dehydrate());

        await interactive.InvalidateAsync(["todos"]);

        var server = new FakeServer<string>();
        using var query = interactive.Query(["todos", 1], server.Fetch(), new() { StaleTime = TimeSpan.FromMinutes(5) });
        Assert.AreEqual("old", query.Data, "the handed-over data still shows while it is refetched");
        Assert.AreEqual(1, server.Calls, "the invalidated data is not taken for fresh");

        server.Respond(0, "new");
        await query.WhenSettledAsync();
        Assert.AreEqual("new", query.Data);
    }

    [TestMethod]
    public async Task An_invalidation_of_another_key_leaves_a_handed_over_query_fresh()
    {
        var (prerender, _, _) = Client();
        prerender.SetQueryData<string>(["todos"], "kept");
        var (interactive, _, _) = Client();
        interactive.Hydrate(prerender.Dehydrate());

        await interactive.InvalidateAsync(["todo"]);   // a prefix of the text, not of the key's parts

        var server = new FakeServer<string>();
        using var query = interactive.Query(["todos"], server.Fetch(), new() { StaleTime = TimeSpan.FromMinutes(5) });
        Assert.AreEqual(0, server.Calls);
    }

    [TestMethod]
    public void A_handoff_stamped_by_a_clock_running_ahead_is_not_younger_than_now()
    {
        var (prerender, prerenderClock, _) = Client();
        prerenderClock.Advance(TimeSpan.FromMinutes(1));   // the server's clock is a minute ahead of the browser's
        prerender.SetQueryData<string>(["q"], "v");
        var (interactive, _, _) = Client();
        interactive.Hydrate(prerender.Dehydrate());

        var server = new FakeServer<string>();
        using var query = interactive.Query(["q"], server.Fetch());   // no stale time: stale at once

        Assert.AreEqual("v", query.Data);
        Assert.IsTrue(query.IsStale);
        Assert.AreEqual(1, server.Calls);
    }
}
