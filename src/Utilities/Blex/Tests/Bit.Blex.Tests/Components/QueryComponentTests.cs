using Bit.Blex.Blazor;
using Bit.Blex.Query;
using Bit.Blex.Tests.Query;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Blex.Tests.Components;

/// <summary>A component that observes one todo by id through <c>UseQuery</c>.</summary>
public sealed class TodoTitle : BlexComponentBase
{
    [Parameter] public int Id { get; set; }

    [Parameter] public FakeServer<string> Server { get; set; } = default!;

    [Parameter] public bool KeepPrevious { get; set; }

    public BlexQuery<string> Query { get; private set; } = default!;

    protected override void OnParametersSet()
        => Query = UseQuery(["todos", Id], Server.Fetch(), new BlexQueryOptions { KeepPreviousData = KeepPrevious });

    protected override void BuildRenderTree(RenderTreeBuilder builder)
        => builder.AddContent(0, Query.IsLoading ? "loading" : $"{Query.Data}{(Query.IsPlaceholderData ? " (previous)" : "")}");
}

/// <summary>
/// One <c>UseQuery</c> call in a loop over items, and another made once in <c>OnInitialized</c> -- the shapes a
/// single-observer-per-call-site design turned into an endless refetch, and a dropped observer.
/// </summary>
public sealed class TodoList : BlexComponentBase
{
    [Parameter] public int[] Ids { get; set; } = [];

    [Parameter] public FakeServer<string> Server { get; set; } = default!;

    [Parameter] public FakeServer<string> Users { get; set; } = default!;

    public BlexQuery<string> Me { get; private set; } = default!;

    public List<BlexQuery<string>> Items { get; } = [];

    protected override void OnInitialized() => Me = UseQuery(["me"], Users.Fetch());

    protected override void OnParametersSet()
    {
        Items.Clear();
        foreach (var id in Ids)
            Items.Add(UseQuery(["todos", id], Server.Fetch()));
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
        => builder.AddContent(0, string.Join(",", Items.Select(i => i.Data ?? "?")));
}

/// <summary>A feed paged through <c>UseInfiniteQuery</c>, keyed by a filter parameter.</summary>
public sealed class FeedList : BlexComponentBase
{
    [Parameter] public PagedServer Server { get; set; } = default!;

    [Parameter] public string Filter { get; set; } = "all";

    public BlexInfiniteQuery<FeedPage, int?> Feed { get; private set; } = default!;

    protected override void OnParametersSet()
        => Feed = UseInfiniteQuery<FeedPage, int?>(["feed", Filter], Server.Fetch, 1, (last, _) => last.Next);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
        => builder.AddContent(0, string.Join(",", Feed.Pages.Select(p => p.Content)) + (Feed.HasNextPage ? " +" : ""));
}

/// <summary>A component that re-renders as a mutation it watches changes.</summary>
public sealed class SaveButton : BlexComponentBase
{
    [Inject] private BlexQueryClient Queries { get; set; } = default!;

    [Parameter] public FakeServer<int> Server { get; set; } = default!;

    public BlexMutation<int, int> Save { get; private set; } = default!;

    protected override void OnInitialized()
        => Save = Watch(Queries.Mutation<int, int>((n, ct) => Server.Fetch()(ct)));

    protected override void BuildRenderTree(RenderTreeBuilder builder)
        => builder.AddContent(0, Save.Status.ToString());
}

/// <summary><c>&lt;BlexQueryView&gt;</c>, <c>UseQuery</c> and <c>Watch</c>: queries and mutations in components.</summary>
[TestClass]
public class QueryComponentTests : BunitContext
{
    private BlexQueryClient Queries => Services.GetRequiredService<BlexQueryClient>();

    public QueryComponentTests()
    {
        Services.AddBitBlexServices();
        Services.AddBitBlexQueries(o => o.RetryCount = 0);
    }

    private IRenderedComponent<BlexQueryView<string>> RenderView(FakeServer<string> server, int id, BlexQueryOptions? options = null)
        => Render<BlexQueryView<string>>(p => p
            .Add(x => x.Key, new BlexQueryKey("todos", id))
            .Add(x => x.Fetch, server.Fetch())
            .Add(x => x.Options, options)
            .Add(x => x.LoadingContent, "loading")
            .Add(x => x.ErrorContent, error => $"error: {error.Message}")
            .Add(x => x.ChildContent, title => $"<b>{title}</b>"));

    [TestMethod]
    public void The_view_shows_loading_then_the_data()
    {
        var server = new FakeServer<string>();
        var cut = RenderView(server, 1);

        cut.MarkupMatches("loading");

        server.Respond(0, "milk");
        cut.WaitForAssertion(() => cut.MarkupMatches("<b>milk</b>"));
    }

    [TestMethod]
    public void The_view_shows_the_error_when_there_is_no_data()
    {
        var server = new FakeServer<string>();
        var cut = RenderView(server, 1);

        server.Fail(0, new InvalidOperationException("offline"));

        cut.WaitForAssertion(() => cut.MarkupMatches("error: offline"));
    }

    [TestMethod]
    public void The_view_follows_a_new_key_and_can_keep_the_previous_data_meanwhile()
    {
        var server = new FakeServer<string>();
        var cut = RenderView(server, 1, new BlexQueryOptions { KeepPreviousData = true });
        server.Respond(0, "milk");
        cut.WaitForAssertion(() => cut.MarkupMatches("<b>milk</b>"));

        cut.Render(p => p.Add(x => x.Key, new BlexQueryKey("todos", 2)));

        Assert.AreEqual(2, server.Calls);
        cut.MarkupMatches("<b>milk</b>");
        Assert.IsTrue(cut.Instance.Query!.IsPlaceholderData);

        server.Respond(1, "eggs");
        cut.WaitForAssertion(() => cut.MarkupMatches("<b>eggs</b>"));
        Assert.AreEqual(1, Queries.GetQueries().Count(q => q.ObserverCount == 1), "the view let go of the previous key");
    }

    [TestMethod]
    public void UseInfiniteQuery_renders_the_pages_loads_more_and_follows_a_new_key()
    {
        var server = new PagedServer();
        var cut = Render<FeedList>(p => p.Add(x => x.Server, server));
        cut.WaitForAssertion(() => cut.MarkupMatches("v1-1 +"));
        var first = cut.Instance.Feed;

        cut.InvokeAsync(() => cut.Instance.Feed.FetchNextPageAsync());
        cut.WaitForAssertion(() => cut.MarkupMatches("v1-1,v1-2 +"));
        Assert.AreSame(first, cut.Instance.Feed, "the same observer while the key holds");

        cut.Render(p => p.Add(x => x.Filter, "mine"));
        cut.WaitForAssertion(() => cut.MarkupMatches("v1-1 +"));
        Assert.AreNotSame(first, cut.Instance.Feed);
        Assert.AreEqual(1, Queries.GetQueries().Count(q => q.ObserverCount == 1), "the previous filter's feed was let go");
    }

    [TestMethod]
    public void UseQuery_renders_the_query_and_reuses_the_observer_while_the_key_holds()
    {
        var server = new FakeServer<string>();
        var cut = Render<TodoTitle>(p => p.Add(x => x.Id, 1).Add(x => x.Server, server));
        var first = cut.Instance.Query;

        cut.MarkupMatches("loading");
        server.Respond(0, "milk");
        cut.WaitForAssertion(() => cut.MarkupMatches("milk"));

        cut.Render(p => p.Add(x => x.Id, 1));
        Assert.AreSame(first, cut.Instance.Query, "the same key keeps its observer");
        Assert.AreEqual(1, server.Calls);
    }

    [TestMethod]
    public void UseQuery_moves_to_a_new_key_and_releases_the_old_one()
    {
        var server = new FakeServer<string>();
        var cut = Render<TodoTitle>(p => p.Add(x => x.Id, 1).Add(x => x.Server, server).Add(x => x.KeepPrevious, true));
        server.Respond(0, "milk");
        cut.WaitForAssertion(() => cut.MarkupMatches("milk"));

        cut.Render(p => p.Add(x => x.Id, 2));
        cut.MarkupMatches("milk (previous)");

        server.Respond(1, "eggs");
        cut.WaitForAssertion(() => cut.MarkupMatches("eggs"));

        var queries = Queries.GetQueries();
        Assert.AreEqual(0, queries.Single(q => q.Key.Equals(new BlexQueryKey("todos", 1))).ObserverCount);
        Assert.AreEqual(1, queries.Single(q => q.Key.Equals(new BlexQueryKey("todos", 2))).ObserverCount);
    }

    [TestMethod]
    public void Disposing_the_component_releases_its_queries()
    {
        var server = new FakeServer<string>();
        var cut = Render<TodoTitle>(p => p.Add(x => x.Id, 1).Add(x => x.Server, server));

        cut.Instance.Dispose();

        Assert.AreEqual(0, Queries.GetQueries().Single().ObserverCount);
    }

    [TestMethod]
    public void Watch_re_renders_as_a_mutation_changes()
    {
        var server = new FakeServer<int>();
        var cut = Render<SaveButton>(p => p.Add(x => x.Server, server));
        cut.MarkupMatches("Idle");

        var call = cut.InvokeAsync(() => cut.Instance.Save.MutateAsync(1));
        cut.WaitForAssertion(() => cut.MarkupMatches("Pending"));

        server.Respond(0, 2);
        cut.WaitForAssertion(() => cut.MarkupMatches("Success"));
    }

    [TestMethod]
    public async Task UseQuery_in_a_loop_keeps_one_observer_per_item_and_settles()
    {
        var server = new FakeServer<string>();
        var users = new FakeServer<string>();
        var cut = Render<TodoList>(p => p.Add(x => x.Ids, [1, 2, 3]).Add(x => x.Server, server).Add(x => x.Users, users));

        Assert.AreEqual(3, server.Calls);
        server.Respond(0, "a");
        server.Respond(1, "b");
        server.Respond(2, "c");
        cut.WaitForAssertion(() => cut.MarkupMatches("a,b,c"));

        await Task.Delay(100);
        Assert.AreEqual(3, server.Calls, "rendering the answers must not start new fetches");

        // The list shrinks: the item it lost is released, the others keep their observers and their data.
        cut.Render(p => p.Add(x => x.Ids, [2, 3]));
        cut.MarkupMatches("b,c");
        Assert.AreEqual(3, server.Calls);
        var queries = Queries.GetQueries();
        Assert.AreEqual(0, queries.Single(q => q.Key.Equals(new BlexQueryKey("todos", 1))).ObserverCount);
        Assert.AreEqual(1, queries.Single(q => q.Key.Equals(new BlexQueryKey("todos", 2))).ObserverCount);
    }

    [TestMethod]
    public void A_query_used_once_in_OnInitialized_survives_later_parameter_passes()
    {
        var server = new FakeServer<string>();
        var users = new FakeServer<string>();
        var cut = Render<TodoList>(p => p.Add(x => x.Ids, [1]).Add(x => x.Server, server).Add(x => x.Users, users));
        var me = cut.Instance.Me;

        cut.Render(p => p.Add(x => x.Ids, [2]));
        cut.Render(p => p.Add(x => x.Ids, [3]));

        Assert.AreEqual(1, Queries.GetQueries().Single(q => q.Key.Equals(new BlexQueryKey("me"))).ObserverCount);
        users.Respond(0, "ada");
        cut.WaitForAssertion(() => Assert.AreEqual("ada", me.Data));
    }

    [TestMethod]
    public void UseQuery_without_a_registered_client_explains_what_is_missing()
    {
        using var bare = new BunitContext();
        bare.Services.AddBitBlexServices();

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => bare.Render<TodoTitle>(p => p.Add(x => x.Id, 1).Add(x => x.Server, new FakeServer<string>())));
        Assert.Contains("AddBitBlexQueries", error.Message);
    }
}
