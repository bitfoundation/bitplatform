using Bit.Blex.Query;
using static Bit.Blex.Tests.Query.QueryTest;

namespace Bit.Blex.Tests.Query;

/// <summary>
/// <see cref="BlexQueryClient.InfiniteQuery{TPage, TParam}"/>: pages that load one after another, refetch together,
/// and never lose what was loaded to a failed page.
/// </summary>
[TestClass]
public class BlexInfiniteQueryTests
{
    private static BlexInfiniteQuery<FeedPage, int?> Feed(BlexQueryClient client, PagedServer server, BlexQueryOptions? options = null)
        => client.InfiniteQuery<FeedPage, int?>(["feed"], server.Fetch, 1, (last, _) => last.Next, options);

    private static string[] Contents(BlexInfiniteQuery<FeedPage, int?> feed) => [.. feed.Pages.Select(p => p.Content)];

    [TestMethod]
    public async Task Pages_load_one_after_another_until_there_is_no_next()
    {
        var (client, _, _) = Client();
        var server = new PagedServer();
        using var feed = Feed(client, server);
        await feed.WhenSettledAsync();

        Assert.AreSequenceEqual(["v1-1"], Contents(feed));
        Assert.IsTrue(feed.HasNextPage);

        await feed.FetchNextPageAsync();
        await feed.FetchNextPageAsync();

        Assert.AreSequenceEqual(["v1-1", "v1-2", "v1-3"], Contents(feed));
        Assert.AreSequenceEqual<int?>([1, 2, 3], [.. feed.PageParams]);
        Assert.IsFalse(feed.HasNextPage);

        await feed.FetchNextPageAsync();
        Assert.AreSequenceEqual([1, 2, 3], server.Requested, "no request past the last page");
    }

    [TestMethod]
    public async Task An_invalidation_fetches_every_loaded_page_again_in_order()
    {
        var (client, _, _) = Client();
        var server = new PagedServer();
        using var feed = Feed(client, server);
        await feed.WhenSettledAsync();
        await feed.FetchNextPageAsync();
        await feed.FetchNextPageAsync();

        server.Version = "v2";
        await client.InvalidateAsync(["feed"]);

        Assert.AreSequenceEqual(["v2-1", "v2-2", "v2-3"], Contents(feed));
        Assert.AreSequenceEqual([1, 2, 3, 1, 2, 3], server.Requested);
    }

    [TestMethod]
    public async Task A_refetch_ends_where_the_server_has_no_next_page_any_more()
    {
        var (client, _, _) = Client();
        var server = new PagedServer();
        using var feed = Feed(client, server);
        await feed.WhenSettledAsync();
        await feed.FetchNextPageAsync();
        await feed.FetchNextPageAsync();

        server.Pages = 2;
        await feed.RefetchAsync();

        Assert.AreSequenceEqual(["v1-1", "v1-2"], Contents(feed));
        Assert.IsFalse(feed.HasNextPage);
    }

    [TestMethod]
    public async Task The_next_page_waits_for_a_running_refetch_and_follows_its_pages()
    {
        var (client, _, _) = Client();
        var server = new PagedServer();
        using var feed = Feed(client, server);
        await feed.WhenSettledAsync();
        await feed.FetchNextPageAsync();

        server.Hold = true;
        server.Version = "v2";
        var refetch = feed.RefetchAsync();          // asks for page 1 again
        var next = feed.FetchNextPageAsync();       // must not append to the pages the refetch replaces
        Assert.IsTrue(feed.IsFetchingNextPage);

        server.Respond(2);
        await Until(() => server.Requested.Count == 4);
        server.Respond(3);
        await refetch;
        await Until(() => server.Requested.Count == 5);
        Assert.AreEqual(3, server.Requested[4], "the page after the refetched ones");
        server.Respond(4);
        await next;

        Assert.AreSequenceEqual(["v2-1", "v2-2", "v2-3"], Contents(feed));
        Assert.IsFalse(feed.IsFetchingNextPage);
    }

    [TestMethod]
    public async Task Asking_for_the_next_page_again_while_it_loads_costs_no_request()
    {
        var (client, _, _) = Client();
        var server = new PagedServer();
        using var feed = Feed(client, server);
        await feed.WhenSettledAsync();

        server.Hold = true;
        var first = feed.FetchNextPageAsync();
        var second = feed.FetchNextPageAsync();   // a double click
        await Until(() => server.Requested.Count == 2);
        server.Respond(1);
        await Task.WhenAll(first, second);

        Assert.AreSequenceEqual([1, 2], server.Requested);
        Assert.AreSequenceEqual(["v1-1", "v1-2"], Contents(feed));
    }

    [TestMethod]
    public async Task A_failed_next_page_keeps_the_pages_loaded_before_and_can_be_retried()
    {
        var (client, _, _) = Client();
        var server = new PagedServer();
        using var feed = Feed(client, server);
        await feed.WhenSettledAsync();

        server.Hold = true;
        var next = feed.FetchNextPageAsync();
        await Until(() => server.Requested.Count == 2);
        server.Fail(1, new InvalidOperationException("offline"));
        await next;

        Assert.IsTrue(feed.IsError);
        Assert.AreSequenceEqual(["v1-1"], Contents(feed));
        Assert.IsTrue(feed.HasNextPage);

        server.Hold = false;
        await feed.FetchNextPageAsync();
        Assert.IsTrue(feed.IsSuccess);
        Assert.AreSequenceEqual(["v1-1", "v1-2"], Contents(feed));
    }

    [TestMethod]
    public async Task The_pages_are_handed_over_from_the_prerender()
    {
        var (prerender, _, _) = Client();
        var server = new PagedServer();
        using (var feed = Feed(prerender, server))
        {
            await feed.WhenSettledAsync();
            await feed.FetchNextPageAsync();
        }

        var handoff = prerender.Dehydrate();
        var (interactive, _, _) = Client();
        interactive.Hydrate(handoff);
        var refetches = new PagedServer();
        using var hydrated = Feed(interactive, refetches, new BlexQueryOptions { StaleTime = TimeSpan.FromMinutes(1) });

        Assert.AreSequenceEqual(["v1-1", "v1-2"], Contents(hydrated));
        Assert.IsEmpty(refetches.Requested, "fresh pages are not fetched again");
    }

    [TestMethod]
    public async Task A_changed_page_fetch_is_used_by_later_refetches()
    {
        var (client, _, _) = Client();
        var server = new PagedServer();
        var other = new PagedServer { Version = "other" };
        using var feed = Feed(client, server);
        await feed.WhenSettledAsync();

        feed.Update(other.Fetch, (last, _) => last.Next);
        await feed.RefetchAsync();

        Assert.AreSequenceEqual(["other-1"], Contents(feed));
    }
}
