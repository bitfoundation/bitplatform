using System.Collections.Concurrent;
using System.Diagnostics;
using Bit.Blex.Query;
using static Bit.Blex.Tests.Query.QueryTest;

namespace Bit.Blex.Tests.Query;

/// <summary>
/// The next page of an infinite query follows whatever fetch is running when it is asked for -- including one that
/// starts in the window between the running fetch settling and the "load more" continuation running.
/// </summary>
/// <remarks>Listens to the query activities, which are process-wide, so it does not run alongside other tests.</remarks>
[TestClass]
[DoNotParallelize]
public class InfiniteQueryOrderingTests
{
    [TestMethod]
    public async Task The_next_page_follows_a_refetch_that_started_as_the_previous_fetch_settled()
    {
        using var context = new SingleThreadContext();
        await context.RunAsync(async () =>
        {
            var (client, _, errors) = Client();
            var server = new PagedServer();
            using var feed = client.InfiniteQuery<FeedPage, int?>(["feed"], server.Fetch, 1, (last, _) => last.Next);
            await feed.WhenSettledAsync();
            Assert.AreSequenceEqual([1], server.Requested);

            // The moment the refetch settles -- its awaiters released but not yet running, which is where a telemetry
            // listener runs -- an invalidation starts another fetch, as one posted to the same dispatcher would.
            var invalidated = false;
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == BlexDiagnostics.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity =>
                {
                    if (activity.DisplayName == "query feed" && !invalidated)
                    {
                        invalidated = true;
                        _ = client.InvalidateAsync(["feed"]);
                    }
                },
            };
            ActivitySource.AddActivityListener(listener);

            server.Hold = true;
            var refetch = feed.RefetchAsync();
            var next = feed.FetchNextPageAsync();
            Assert.IsTrue(feed.IsFetchingNextPage);

            server.Respond(1);
            await Until(() => server.Requested.Count == 3, "the invalidation refetches page 1 as the first refetch settles");
            Assert.AreSequenceEqual([1, 1, 1], server.Requested);

            // The next page waits for that fetch too, then asks for page 2 -- rather than taking that fetch for its own.
            server.Respond(2);
            await Until(() => server.Requested.Count == 4, "the next page is requested once the second refetch settled");
            Assert.AreSequenceEqual([1, 1, 1, 2], server.Requested);

            server.Respond(3);
            await next;
            await refetch;
            Assert.AreSequenceEqual(["v1-1", "v1-2"], [.. feed.Pages.Select(p => p.Content)]);
            Assert.IsFalse(feed.IsFetchingNextPage);
            Assert.IsEmpty(errors);
        });
    }

    /// <summary>One thread, one queue: continuations run in the order they were posted, as on a renderer's dispatcher.</summary>
    private sealed class SingleThreadContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = [];
        private readonly Thread _thread;

        public SingleThreadContext()
        {
            _thread = new Thread(Run) { IsBackground = true };
            _thread.Start();
        }

        public override void Post(SendOrPostCallback d, object? state) => _queue.Add((d, state));

        public Task RunAsync(Func<Task> work)
        {
            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Post(async _ =>
            {
                try
                {
                    await work();
                    done.SetResult();
                }
                catch (Exception ex)
                {
                    done.SetException(ex);
                }
            }, null);

            return done.Task;
        }

        private void Run()
        {
            SetSynchronizationContext(this);
            foreach (var (callback, state) in _queue.GetConsumingEnumerable())
                callback(state);
        }

        public void Dispose() => _queue.CompleteAdding();
    }
}
