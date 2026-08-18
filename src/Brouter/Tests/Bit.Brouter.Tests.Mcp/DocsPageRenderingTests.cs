using Bit.Brouter.Demo.Client;
using Bit.Brouter.Demo.Server.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// Rendering a documentation page outside of a request, which is what lets an MCP client read the
/// same page a person reads rather than a second copy that could go stale.
/// <para>
/// The page renders on its own here - outside the app's &lt;Brouter&gt; and its layout - so anything
/// it reaches for while initializing is missing. That has to come back as a page-shaped answer
/// explaining itself, never as a failed call telling the agent nothing.
/// </para>
/// </summary>
[TestClass]
public class DocsPageRenderingTests
{
    [TestMethod]
    public void A_slug_resolves_to_the_same_page_for_the_tool_and_for_the_resource()
    {
        // Both go through this one method, which is what makes a slug that works in one work in the other.
        Assert.AreEqual("guards", DocsPageRenderer.FindPage("guards")?.Slug);

        // The overview's own slug is the empty string, which is not a thing anyone types.
        foreach (var alias in new[] { "overview", "index", "docs" })
        {
            Assert.AreEqual(string.Empty, DocsPageRenderer.FindPage(alias)?.Slug, $"'{alias}' did not resolve to the overview.");
        }

        Assert.IsNull(DocsPageRenderer.FindPage("nope"));
    }

    [TestMethod]
    public void An_unknown_slug_is_answered_with_the_slugs_that_exist()
    {
        var answer = DocsPageRenderer.NoSuchPage("nope");

        StringAssert.Contains(answer, "'nope'");

        foreach (var page in DocsCatalog.AllPages)
        {
            StringAssert.Contains(answer, page.Slug.Length == 0 ? "overview" : page.Slug);
        }
    }

    [TestMethod]
    public async Task Every_documentation_page_renders_in_the_apps_own_container()
    {
        // The pages are components of the WebAssembly client, rendered here in the server container -
        // the same arrangement prerendering uses, and the reason the host registers the client's
        // services. A page that renders in the browser and not here reaches this server as an apology.
        foreach (var page in DocsCatalog.AllPages)
        {
            var (markdown, error) = await RenderAsync(page);

            Assert.IsNull(error, $"The '{page.Title}' page threw while rendering: {error}");
            Assert.IsNotNull(markdown);
            Assert.IsTrue(markdown.Length > 500, $"The '{page.Title}' page rendered {markdown.Length} characters of Markdown.");
            StringAssert.StartsWith(markdown, "# ", $"The '{page.Title}' page did not render a heading of its own.");
        }
    }

    [TestMethod]
    public async Task A_rendered_page_is_kept_so_only_the_first_caller_pays_for_it()
    {
        // Rendering a page and flattening it costs far more than serving it, and the pages are static.
        var page = DocsCatalog.FindBySlug("faq")!;

        var (first, _) = await RenderAsync(page);
        var (second, _) = await RenderAsync(page);

        Assert.IsNotNull(first);
        Assert.AreSame(first, second, "The page was rendered again instead of being served from the cache.");
    }

    [TestMethod]
    public void A_page_that_cannot_be_rendered_says_where_to_read_it_instead()
    {
        var page = DocsCatalog.FindBySlug("guards")!;

        var unavailable = DocsPageRenderer.Unavailable(page, "JSInterop is not available");

        StringAssert.Contains(unavailable, page.Title);
        StringAssert.Contains(unavailable, page.Url);
        StringAssert.Contains(unavailable, "JSInterop is not available");

        // And where the same material is available as text, so the agent has somewhere to go.
        StringAssert.Contains(unavailable, "SearchBrouter");
    }

    private static async Task<(string? Markdown, string? Error)> RenderAsync(DocsPageInfo page)
    {
        // A renderer belongs to the request that asked for the page, so the scope is the unit here too.
        await using var scope = McpTestHost.Services.CreateAsyncScope();

        var renderer = scope.ServiceProvider.GetRequiredService<HtmlRenderer>();

        return await DocsPageRenderer.TryRenderMarkdownAsync(renderer, page);
    }
}
