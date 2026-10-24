using Bit.Blex.Query;
using static Bit.Blex.Tests.Query.QueryTest;

namespace Bit.Blex.Tests.Query;

/// <summary>
/// <see cref="BlexMutation{TArg, TResult}"/>, optimistic updates and their rollback, and the prerender
/// handoff of the cache (<see cref="BlexQueryClient.Dehydrate"/> / <see cref="BlexQueryClient.Hydrate"/>).
/// </summary>
[TestClass]
public class BlexMutationTests
{
    private static readonly BlexQueryOptions Forever = new() { StaleTime = TimeSpan.MaxValue };

    private static async Task<(BlexQueryClient Client, BlexQuery<IReadOnlyList<string>> List, FakeServer<IReadOnlyList<string>> Lists, List<BlexError> Errors)> TodoList()
    {
        var (client, _, errors) = Client();
        var lists = new FakeServer<IReadOnlyList<string>>();
        var list = client.Query(["todos"], lists.Fetch(), Forever);
        lists.Respond(0, ["a"]);
        await list.WhenSettledAsync();
        return (client, list, lists, errors);
    }

    [TestMethod]
    public async Task A_mutation_moves_from_idle_to_pending_to_success()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<int>();
        var save = client.Mutation<string, int>((text, ct) => server.Fetch()(ct));
        var statuses = new List<BlexMutationStatus>();
        save.Changed += () => statuses.Add(save.Status);

        Assert.IsTrue(save.IsIdle);
        var call = save.MutateAsync("x");
        Assert.IsTrue(save.IsPending);
        Assert.AreEqual("x", save.Arg);

        server.Respond(0, 42);
        await call;

        Assert.IsTrue(save.IsSuccess);
        Assert.AreEqual(42, save.Data);
        Assert.AreSequenceEqual([BlexMutationStatus.Pending, BlexMutationStatus.Success], statuses);
    }

    [TestMethod]
    public async Task A_failed_mutation_does_not_throw_but_keeps_the_error()
    {
        var (client, _, _) = Client();
        Exception? reported = null;
        var save = client.Mutation<string, int>(
            (_, _) => Task.FromException<int>(new InvalidOperationException("rejected")),
            new() { OnError = (ex, _) => reported = ex });

        await save.MutateAsync("x");

        Assert.IsTrue(save.IsError);
        Assert.AreEqual("rejected", save.Error?.Message);
        Assert.AreSame(save.Error, reported);
    }

    [TestMethod]
    public async Task An_optimistic_update_shows_at_once_and_rolls_back_when_the_change_fails()
    {
        var (client, list, _, _) = await TodoList();
        var server = new FakeServer<string>();
        var add = client.Mutation<string, string>(
            (text, ct) => server.Fetch()(ct),
            new() { OnMutate = text => client.Optimistic<IReadOnlyList<string>>(["todos"], items => [.. items ?? [], text]) });

        var call = add.MutateAsync("b");
        Assert.AreSequenceEqual(["a", "b"], list.Data!.ToList(), "the optimistic value is shown before the server answers");

        server.Fail(0, new InvalidOperationException("rejected"));
        await call;

        Assert.AreSequenceEqual(["a"], list.Data!.ToList(), "the failure rolls it back");
        Assert.IsTrue(add.IsError);
    }

    [TestMethod]
    public async Task A_rollback_does_not_overwrite_newer_data_but_refetches()
    {
        var (client, list, lists, _) = await TodoList();
        var server = new FakeServer<string>();
        var add = client.Mutation<string, string>(
            (text, ct) => server.Fetch()(ct),
            new() { OnMutate = text => client.Optimistic<IReadOnlyList<string>>(["todos"], items => [.. items ?? [], text]) });

        var first = add.MutateAsync("b");
        var second = add.MutateAsync("c");          // its optimistic value lands on top of the first's
        Assert.AreSequenceEqual(["a", "b", "c"], list.Data!.ToList());

        server.Fail(0, new InvalidOperationException("first rejected"));
        await first;

        Assert.AreSequenceEqual(["a", "b", "c"], list.Data!.ToList(), "restoring ['a'] would erase the second change");
        Assert.AreEqual(2, lists.Calls, "the key is refetched instead, so the server settles it");
        lists.Respond(1, ["a", "c"]);
        await list.WhenSettledAsync();

        server.Respond(1, "c");
        await second;
        Assert.AreSequenceEqual(["a", "c"], list.Data!.ToList());
    }

    [TestMethod]
    public async Task Invalidated_queries_refetch_before_the_mutation_reports_success()
    {
        var (client, list, lists, _) = await TodoList();
        var add = client.Mutation<string, string>(
            (text, _) => Task.FromResult(text),
            new() { Invalidates = [["todos"]] });

        var call = add.MutateAsync("b");
        Assert.AreEqual(2, lists.Calls, "the list refetches after the change");
        Assert.IsTrue(add.IsPending, "'saved' must not show next to the list from before the save");

        lists.Respond(1, ["a", "b"]);
        await call;

        Assert.IsTrue(add.IsSuccess);
        Assert.AreSequenceEqual(["a", "b"], list.Data!.ToList());
    }

    [TestMethod]
    public async Task Callbacks_run_in_order_and_a_throwing_one_is_isolated()
    {
        var (client, _, errors) = Client();
        var order = new List<string>();
        var save = client.Mutation<int, int>(
            (n, _) => Task.FromResult(n * 2),
            new()
            {
                OnMutate = n => { order.Add("mutate"); return null; },
                OnSuccess = (result, n) => { order.Add($"success {result}"); throw new InvalidOperationException("bad callback"); },
                OnSettled = n => order.Add("settled"),
            });

        await save.MutateAsync(2);

        Assert.AreSequenceEqual(["mutate", "success 4", "settled"], order);
        Assert.IsTrue(save.IsSuccess, "a callback failing does not turn a change the server accepted into a failure");
        Assert.ContainsSingle(errors);
    }

    [TestMethod]
    public async Task A_cancelled_mutation_rolls_back_and_returns_to_idle()
    {
        var (client, list, _, _) = await TodoList();
        var server = new FakeServer<string>();
        var add = client.Mutation<string, string>(
            (text, ct) => server.Fetch()(ct),
            new() { OnMutate = text => client.Optimistic<IReadOnlyList<string>>(["todos"], items => [.. items ?? [], text]) });
        using var cts = new CancellationTokenSource();

        var call = add.MutateAsync("b", cts.Token);
        cts.Cancel();
        await call;

        Assert.IsTrue(add.IsIdle, "a cancelled change is not a failure");
        Assert.IsNull(add.Error);
        Assert.AreSequenceEqual(["a"], list.Data!.ToList());
    }

    [TestMethod]
    public async Task Only_the_latest_call_speaks_for_the_mutation()
    {
        var (client, _, _) = Client();
        var server = new FakeServer<int>();
        var save = client.Mutation<int, int>((n, ct) => server.Fetch()(ct));

        var first = save.MutateAsync(1);
        var second = save.MutateAsync(2);
        server.Respond(1, 20);
        await second;
        server.Respond(0, 10);
        await first;

        Assert.AreEqual(20, save.Data, "an earlier call finishing late does not overwrite the latest one's result");
        Assert.AreEqual(2, save.Arg);
    }

    [TestMethod]
    public async Task A_failed_call_is_retried_when_asked_to()
    {
        var (client, clock, _) = Client();
        var attempts = 0;
        var save = client.Mutation<int, int>(
            (n, _) => ++attempts < 3 ? Task.FromException<int>(new InvalidOperationException("flaky")) : Task.FromResult(n),
            new() { RetryCount = 2, RetryDelay = TimeSpan.FromMilliseconds(100) });

        var call = save.MutateAsync(7);
        await Until(() => clock.Timers == 1);
        clock.Advance(TimeSpan.FromMilliseconds(100));
        await Until(() => clock.Timers == 2);
        clock.Advance(TimeSpan.FromMilliseconds(200));
        await call;

        Assert.AreEqual(3, attempts);
        Assert.IsTrue(save.IsSuccess);
    }

    [TestMethod]
    public void Several_rollbacks_combine_and_undo_in_reverse()
    {
        var undone = new List<int>();
        var rollback = BlexRollback.Combine(BlexRollback.From(() => undone.Add(1)), null, BlexRollback.From(() => undone.Add(2)));

        rollback.Rollback();
        rollback.Rollback();   // at most once

        Assert.AreSequenceEqual([2, 1], undone);
    }

    [TestMethod]
    public async Task Dehydrated_queries_hydrate_another_client_without_a_request()
    {
        var (server, _, _) = Client();
        var fetches = new FakeServer<IReadOnlyList<string>>();
        var prerendered = server.Query(["todos"], fetches.Fetch());
        fetches.Respond(0, ["a", "b"]);
        await prerendered.WhenSettledAsync();
        _ = server.Query<int>(["failing"], _ => Task.FromException<int>(new InvalidOperationException()));

        var handoff = server.Dehydrate();
        Assert.AreEqual(1, handoff.Count, "only successfully fetched queries are handed over");

        var (interactive, _, _) = Client();
        interactive.Hydrate(handoff);
        var refetches = new FakeServer<IReadOnlyList<string>>();
        using var list = interactive.Query(["todos"], refetches.Fetch(), new BlexQueryOptions { StaleTime = TimeSpan.FromMinutes(1) });

        Assert.AreSequenceEqual(["a", "b"], list.Data!.ToList());
        Assert.AreEqual(0, refetches.Calls, "data the prerender fetched is not fetched again while fresh");
    }

    [TestMethod]
    public void A_handed_over_query_nobody_asks_for_is_dropped_after_the_gc_time()
    {
        var (prerender, _, _) = Client();
        prerender.SetQueryData<string>(["n"], "prerendered");
        var handoff = prerender.Dehydrate();

        var (interactive, clock, _) = Client(o => o.GcTime = TimeSpan.FromMinutes(5));
        interactive.Hydrate(handoff);
        clock.Advance(TimeSpan.FromMinutes(5));

        using var late = interactive.Query<string>(["n"], _ => new TaskCompletionSource<string>().Task);
        Assert.IsFalse(late.HasData, "the payload is not kept for the life of the app");
    }

    [TestMethod]
    public async Task Hydration_does_not_overwrite_newer_data()
    {
        var (prerender, _, _) = Client();
        prerender.SetQueryData<string>(["n"], "old");
        var handoff = prerender.Dehydrate();

        var (interactive, clock, _) = Client();
        clock.Advance(TimeSpan.FromMinutes(1));
        interactive.SetQueryData<string>(["n"], "new");
        interactive.Hydrate(handoff);

        Assert.AreEqual("new", interactive.GetQueryData<string>(["n"]));
        await Task.CompletedTask;
    }
}
