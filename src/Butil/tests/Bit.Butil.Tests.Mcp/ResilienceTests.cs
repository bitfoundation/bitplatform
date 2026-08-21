using System.Diagnostics;
using NUnit.Framework;
using ModelContextProtocol.Client;
using Bit.Butil.Tests.Mcp.Infrastructure;

namespace Bit.Butil.Tests.Mcp;

/// <summary>
/// How the server behaves under the conditions a deployment actually produces: several clients at
/// once, the same question asked twice, a caller that gives up halfway, and a session that starts
/// cold.
/// <para>
/// Almost everything this server hands out is built lazily and cached - the reflected API surface,
/// the search index, one rendered Markdown copy of each page per origin - and every one of those is
/// a place where two concurrent callers, or a caller who cancels, can leave a shared cache holding
/// something wrong for the rest of the process's life. That class of bug never shows up in a
/// single-threaded test and always shows up in production.
/// </para>
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class ResilienceTests : McpTestBase
{
    [Test]
    public async Task Concurrent_clients_get_the_same_answers()
    {
        // Six independent sessions asking at once, over the same stateless transport.
        var work = Enumerable.Range(0, 6).Select(async index =>
        {
            var transport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = McpServerFixture.Url("mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            });

            await using var client = await McpClient.CreateAsync(transport, cancellationToken: Ct);

            var overview = await client.CallToolAsync("GetButilGuideSection", new Dictionary<string, object?> { ["heading"] = "Getting started" }, cancellationToken: Ct);
            var page = await client.CallToolAsync("GetButilDocsPage", new Dictionary<string, object?> { ["slug"] = "clipboard" }, cancellationToken: Ct);
            var search = await client.CallToolAsync("SearchButil", new Dictionary<string, object?> { ["query"] = $"storage {index}" }, cancellationToken: Ct);

            await transport.DisposeAsync();

            return (Overview: Text(overview), Page: Text(page), Search: Text(search));
        });

        var answers = await Task.WhenAll(work);

        Assert.Multiple(() =>
        {
            Assert.That(answers.Select(answer => answer.Overview).Distinct().Count(), Is.EqualTo(1),
                "Six concurrent sessions got different overviews.");

            Assert.That(answers.Select(answer => answer.Page).Distinct().Count(), Is.EqualTo(1),
                "Six concurrent sessions got different renderings of the same page - the render cache is not answering consistently.");

            foreach (var answer in answers)
            {
                Assert.That(answer.Page, Does.Not.Contain("could not be rendered on the server"));
                Assert.That(answer.Search, Is.Not.Empty);
            }
        });
    }

    [Test]
    public async Task Rendering_several_different_pages_at_once_does_not_cross_them_over()
    {
        // A page is rendered outside the app's router, and its NavigationManager can only be
        // pointed at one URL per scope. If two pages ever shared a scope, one of them would come
        // back carrying the other's canonical URL and anchors - and be cached under its own key.
        var slugs = new[] { "clipboard", "crypto", "geolocation", "storage", "fetch", "console" };

        var pages = await Task.WhenAll(slugs.Select(async slug =>
            (Slug: slug, Text: Text(await CallAsync("GetButilDocsPage", new { slug })))));

        Assert.Multiple(() =>
        {
            foreach (var (slug, text) in pages)
            {
                Assert.That(text, Does.StartWith($"Bit.Butil documentation page: /{slug}"),
                    $"The answer for /{slug} is not the /{slug} page.");

                Assert.That(text, Does.Not.Contain("could not be rendered"), $"/{slug} did not render under concurrency.");
            }
        });
    }

    [Test]
    public async Task The_same_question_twice_gives_the_same_answer()
    {
        // Every tool is annotated idempotent. A client is entitled to rely on that - it is what
        // lets it re-ask after a dropped connection without wondering whether it got a different
        // library back.
        var probes = new (string Tool, object? Arguments)[]
        {
            // The listing form of each retrieval tool: an empty call is a real call here, not a
            // missing argument, so it has to be as stable as any other.
            ("GetButilApiDetails", null),
            ("GetButilDocsPage", null),
            ("GetButilGuideSection", null),
            ("GetButilSourceFile", null),
            ("GetButilSetupGuide", new { hostingModel = "web-app" }),
            ("GetButilDocsPage", new { slug = "troubleshooting" }),
            ("GetButilApiDetails", new { typeName = "LocalStorage" }),
            ("PlanButilFeature", new { apis = "Clipboard, WakeLock" }),
        };

        foreach (var (tool, arguments) in probes)
        {
            var first = Text(await CallAsync(tool, arguments));
            var second = Text(await CallAsync(tool, arguments));

            Assert.That(second, Is.EqualTo(first), $"{tool} answered differently the second time it was asked.");
        }
    }

    [Test]
    public async Task A_cached_page_is_served_faster_than_it_was_rendered()
    {
        // Not a performance test - a cache-correctness one. Rendering a page and flattening it costs
        // far more than serving it, so if the second call is not dramatically cheaper the cache is
        // not being consulted, and every client is paying for a render it did not need.
        const string slug = "web-audio";

        var cold = Stopwatch.StartNew();
        var first = Text(await CallAsync("GetButilDocsPage", new { slug }));
        cold.Stop();

        var warm = Stopwatch.StartNew();
        var second = Text(await CallAsync("GetButilDocsPage", new { slug }));
        warm.Stop();

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(first));
            Assert.That(warm.Elapsed, Is.LessThanOrEqualTo(cold.Elapsed + TimeSpan.FromMilliseconds(50)),
                $"The second render of /{slug} took {warm.ElapsedMilliseconds}ms against {cold.ElapsedMilliseconds}ms cold, so nothing was cached.");
        });
    }

    [Test]
    public async Task A_cancelled_call_does_not_poison_what_comes_after_it()
    {
        // A client that gives up mid-render must not leave the server with a half-built cache entry
        // or a broken renderer. The page it abandoned has to be servable straight afterwards.
        using var cancellation = new CancellationTokenSource();

        var abandoned = Mcp.CallToolAsync("GetButilDocsPage", new Dictionary<string, object?> { ["slug"] = "web-authn" },
                                          cancellationToken: cancellation.Token);

        await cancellation.CancelAsync();

        try
        {
            await abandoned;
        }
        catch (OperationCanceledException)
        {
            // The expected outcome, and not the thing under test.
        }
        catch (Exception exception) when (exception.InnerException is OperationCanceledException)
        {
            // Same, wrapped by the transport.
        }

        var text = Text(await CallAsync("GetButilDocsPage", new { slug = "web-authn" }));

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.StartWith("Bit.Butil documentation page: /web-authn"));
            Assert.That(text, Does.Not.Contain("could not be rendered"));
        });
    }

    [Test]
    public async Task A_fresh_session_needs_no_warm_up()
    {
        // The search index is built in the background from startup and nothing waits for it, so the
        // first caller must still get a real answer rather than an empty one. That first caller
        // cannot be a test - by the time one runs, another fixture may have warmed the app - so the
        // fixture makes the call itself, before anything connects, and it is checked here.
        if (McpServerFixture.ColdSearch is { } cold)
        {
            Assert.That(cold, Does.Contain("Clipboard"),
                "The first search the server answered, made before any fixture connected, found nothing: the index needs warming up.");
        }

        // And a session opened later still needs no warm-up call of its own.
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = McpServerFixture.Url("mcp"),
            TransportMode = HttpTransportMode.StreamableHttp
        });

        await using var client = await McpClient.CreateAsync(transport, cancellationToken: Ct);

        var result = await client.CallToolAsync("SearchButil", new Dictionary<string, object?> { ["query"] = "clipboard" }, cancellationToken: Ct);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsError, Is.Not.True);
            Assert.That(Text(result), Does.Contain("Clipboard"));
        });

        await transport.DisposeAsync();
    }

    [Test]
    public async Task A_realistic_agent_session_completes_end_to_end()
    {
        // The workflow the prompts tell an agent to follow, run as one: search for a capability,
        // plan the feature, read the reference, learn what the page has to arrange, and fetch a
        // working example. If any step hands the next one something it cannot use, this breaks.
        var search = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "keep the screen awake while a video plays" });

        Assert.That(search.Hits, Is.Not.Empty, search.Message);

        var plan = await CallStructuredAsync<FeaturePlan>("PlanButilFeature", new { apis = "WakeLock" });

        Assert.Multiple(() =>
        {
            Assert.That(plan.Unknown, Is.Empty);
            Assert.That(plan.RequiresSecureContext, Is.True, "A wake lock needs a secure context, and the plan is where an agent finds that out.");
        });

        var inspection = (await CallStructuredAsync<FeaturePlan>("PlanButilFeature", new { apis = "WakeLock" })).Apis.Single();

        Assert.That(inspection.NextCalls, Is.Not.Null.And.Not.Empty);

        // The inspection's own follow-up calls have to be callable verbatim, exactly like a search
        // hit's - an agent is told to make them next.
        foreach (var nextCall in inspection.NextCalls!)
        {
            var call = ToolCallReference.Parse(nextCall);

            Assert.That(call, Is.Not.Null, $"'{nextCall}' is not a call an agent can make verbatim.");
            Assert.That(ButilMcp.Tools.ContainsKey(call!.Tool), Is.True, $"'{nextCall}' names a tool that does not exist.");

            var answer = await CallAsync(call.Tool, call.Arguments);

            Assert.That(Text(answer), Is.Not.Empty, $"'{nextCall}' answered with nothing.");
        }

        var example = Text(await CallAsync("GetButilSourceFile", new { path = "Demo/Client/Pages/WakeLockPage.razor" }));

        Assert.That(example, Does.Contain("WakeLock"), "The page that documents an API is also the working example of it.");
    }
}
