
namespace Bit.BevTools.Tests;

[TestClass]
public class ConcurrencyTests
{
    [TestMethod]
    public void Ring_buffer_survives_parallel_writers_and_readers()
    {
        var buffer = new RingBuffer<int>(1000);
        var errors = 0;
        Parallel.For(0, 200_000, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
        {
            try
            {
                buffer.Add(i);
                if (i % 997 == 0)
                {
                    _ = buffer.ToArray();
                    _ = buffer.Last;
                    _ = buffer.FindLast(x => x == i);
                }
            }
            catch
            {
                Interlocked.Increment(ref errors);
            }
        });

        Assert.AreEqual(0, errors);
        Assert.AreEqual(1000, buffer.Count);
        Assert.AreEqual(1000, buffer.ToArray().Distinct().Count());
    }

    [TestMethod]
    public void Timeline_and_error_center_accept_parallel_recording()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.MaxEvents = 2000);
        using (scope)
        {
            var exceptions = 0;
            Parallel.For(0, 20_000, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
            {
                try
                {
                    var id = session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.Custom, Category = "t", Title = "e" + i });
                    if (i % 3 == 0)
                    {
                        session.Timeline.Complete(id, i);
                    }

                    if (i % 500 == 0)
                    {
                        session.Errors.Record(new InvalidOperationException("err" + (i % 7)), "test");
                        session.Diagnostics.Evaluate(force: true);
                    }
                }
                catch
                {
                    Interlocked.Increment(ref exceptions);
                }
            });

            Assert.AreEqual(0, exceptions);
            Assert.AreEqual(2000, session.Timeline.Count);
            var ids = session.Timeline.Snapshot().Select(e => e.Id).ToList();
            Assert.AreSequenceEqual(ids.OrderBy(x => x), ids);
            Assert.IsTrue(session.Errors.TotalOccurrences == 40);
        }
    }

    [TestMethod]
    public void Sessions_can_be_created_and_disposed_concurrently()
    {
        var exceptions = 0;
        Parallel.For(0, 64, _ =>
        {
            try
            {
                var (session, scope) = TestHelpers.CreateSession();
                session.Timeline.Record(new BevToolsEvent { Title = "x" });
                scope.Dispose();
                Assert.IsTrue(session.IsDisposed);
            }
            catch
            {
                Interlocked.Increment(ref exceptions);
            }
        });

        Assert.AreEqual(0, exceptions);
    }
}
