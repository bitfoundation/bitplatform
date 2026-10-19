using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

[TestClass]
public class TimelineTests
{
    [TestMethod]
    public void Record_assigns_monotonic_ids_and_keeps_only_the_newest_events()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.MaxEvents = 50);
        using (scope)
        {
            var ids = new List<long>();
            for (var i = 0; i < 120; i++)
            {
                ids.Add(session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.Custom, Category = "test", Title = "e" + i }));
            }

            Assert.AreSequenceEqual(ids.OrderBy(x => x), ids);
            var snapshot = session.Timeline.Snapshot();
            Assert.AreEqual(50, snapshot.Length);
            Assert.AreEqual("e70", snapshot[0].Title);
            Assert.AreEqual("e119", snapshot[^1].Title);
            Assert.IsNull(session.Timeline.Find(ids[0]));
            Assert.IsNotNull(session.Timeline.Find(ids[^1]));
        }
    }

    [TestMethod]
    public void Complete_sets_duration_detail_and_severity()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var id = session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.Http, Category = "http", Title = "GET /x" });
            var beforeCompletion = session.Timeline.Find(id)!;
            session.Timeline.Complete(id, 12.5, "200 OK", BevToolsSeverity.Warning);
            var evt = session.Timeline.Find(id)!;
            Assert.IsNull(beforeCompletion.DurationMs);
            Assert.AreEqual(12.5, evt.DurationMs);
            Assert.AreEqual("200 OK", evt.Detail);
            Assert.AreEqual(BevToolsSeverity.Warning, evt.Severity);
        }
    }

    [TestMethod]
    public async Task BeginActivity_measures_until_disposed()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            long id;
            using (session.TimelineApi.BeginActivity("test", "work"))
            {
                await Task.Delay(20);
                id = session.Timeline.Snapshot()[^1].Id;
            }

            var evt = session.Timeline.Find(id)!;
            Assert.IsNotNull(evt.DurationMs);
            Assert.IsTrue(evt.DurationMs >= 15, "duration should cover the awaited delay");
        }
    }

    [TestMethod]
    public void Disabled_session_records_nothing()
    {
        var collection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        collection.AddBitBevToolsServices(o => o.Enabled = false);
        using var provider = Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(collection);
        var timeline = (IBevToolsTimeline)provider.GetService(typeof(IBevToolsTimeline))!;
        Assert.IsFalse(timeline.IsEnabled);
        Assert.AreEqual(-1, timeline.Record(new BevToolsEvent { Title = "ignored" }));
    }

    [TestMethod]
    public void Version_changes_on_record_and_clear()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var v0 = session.Version;
            session.Timeline.Record(new BevToolsEvent { Title = "a" });
            var v1 = session.Version;
            session.Timeline.Clear();
            var v2 = session.Version;
            Assert.IsTrue(v1 > v0);
            Assert.IsTrue(v2 > v1);
            Assert.AreEqual(0, session.Timeline.Count);
        }
    }
}
