using Bit.Blex.Query;
using static Bit.Blex.Tests.Query.QueryTest;

namespace Bit.Blex.Tests.Query;

/// <summary>
/// <see cref="BlexQuery.IsStale"/> is worked out on read, so an observer is told when the stale time runs out --
/// otherwise a component showing it keeps showing "fresh" until something unrelated re-renders it.
/// </summary>
[TestClass]
public class StaleNotificationTests
{
    [TestMethod]
    public async Task An_observer_is_notified_when_its_data_turns_stale()
    {
        var (client, clock, _) = Client();
        var server = new FakeServer<int>();
        using var query = client.Query(["n"], server.Fetch(), new BlexQueryOptions { StaleTime = TimeSpan.FromSeconds(15) });
        server.Respond(0, 1);
        await query.WhenSettledAsync();
        Assert.IsFalse(query.IsStale);

        var changes = 0;
        query.Changed += () => changes++;

        clock.Advance(TimeSpan.FromSeconds(14));
        Assert.AreEqual(0, changes);

        clock.Advance(TimeSpan.FromSeconds(1));
        await Until(() => changes == 1, "the stale time ran out");
        Assert.IsTrue(query.IsStale);
        Assert.AreEqual(1, server.Calls, "turning stale is a notification, not a fetch");
    }

    [TestMethod]
    public async Task New_data_reschedules_the_notice()
    {
        var (client, clock, _) = Client();
        var server = new FakeServer<int>();
        using var query = client.Query(["n"], server.Fetch(), new BlexQueryOptions { StaleTime = TimeSpan.FromSeconds(10) });
        server.Respond(0, 1);
        await query.WhenSettledAsync();

        clock.Advance(TimeSpan.FromSeconds(8));
        client.SetQueryData(["n"], 2);

        var changes = 0;
        query.Changed += () => changes++;
        clock.Advance(TimeSpan.FromSeconds(5));
        Assert.AreEqual(0, changes, "the data set at 8 s is fresh until 18 s");
        Assert.IsFalse(query.IsStale);

        clock.Advance(TimeSpan.FromSeconds(5));
        await Until(() => changes == 1);
        Assert.IsTrue(query.IsStale);
    }

    [TestMethod]
    public async Task A_disposed_observer_is_not_notified()
    {
        var (client, clock, _) = Client();
        var server = new FakeServer<int>();
        var query = client.Query(["n"], server.Fetch(), new BlexQueryOptions { StaleTime = TimeSpan.FromSeconds(10) });
        server.Respond(0, 1);
        await query.WhenSettledAsync();
        var changes = 0;
        query.Changed += () => changes++;

        query.Dispose();
        clock.Advance(TimeSpan.FromSeconds(20));
        await Task.Delay(50);

        Assert.AreEqual(0, changes, "the stale notice goes with the observer");
    }
}
