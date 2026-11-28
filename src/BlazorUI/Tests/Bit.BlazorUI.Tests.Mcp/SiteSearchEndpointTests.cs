using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bit.BlazorUI.Tests.Mcp.Infrastructure;

namespace Bit.BlazorUI.Tests.Mcp;

/// <summary>
/// The list the site's command palette (Ctrl+K) searches inside the component pages: every topic of a page split into
/// topics and every example section, read off the same demo markup the MCP tools answer from - so it lives and breaks
/// with them, and is checked against the deployed server the same way.
/// </summary>
[TestClass]
public class SiteSearchEndpointTests : McpTestBase
{
    private sealed record Section(string Title, string Component, string? Topic, string Url);

    private Task<Section[]> SectionsAsync() => OncePerFixtureAsync(async () =>
    {
        using var response = await McpServerFixture.Http.GetAsync(McpServerFixture.Url("api/SiteSearch/GetSections"));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        return JsonSerializer.Deserialize<Section[]>(await response.Content.ReadAsStringAsync(), JsonSerializerOptions.Web)!;
    });

    [TestMethod]
    public async Task Every_section_carries_the_address_that_lands_on_it()
    {
        var sections = await SectionsAsync();

        using var scope = Assert.Scope();

        Assert.IsGreaterThan(1000, sections.Length, "The sections of the component pages were not read.");
        Assert.IsTrue(sections.All(s => s.Url.StartsWith("/components/", StringComparison.Ordinal)), "A section points outside the component pages.");

        // A plain page: the component's address and the section's anchor.
        Assert.IsTrue(sections.Any(s => s.Component == "Button" && s.Title == "Basic" && s.Url == "/components/button#example1"));

        // A multi-API page answers its first tab only: the anchors of the other tabs land nowhere until they are picked.
        Assert.AreEqual(1, sections.Count(s => s.Component == "Dropdown" && s.Title == "Basic"));
    }

    [TestMethod]
    public async Task A_page_split_into_topics_lists_its_topics_and_lands_in_the_right_one()
    {
        var palette = (await SectionsAsync()).Where(s => s.Component == "CommandPalette").ToArray();

        using var scope = Assert.Scope();

        // Every topic but the overview, which is the component's own page.
        Assert.IsTrue(palette.Any(s => s.Title == "Search & sources" && s.Url == "/components/commandpalette/search"));
        Assert.IsFalse(palette.Any(s => s.Title == "Overview"));

        var fuzzy = palette.Single(s => s.Title == "Fuzzy matching");
        Assert.AreEqual("Search & sources", fuzzy.Topic);
        StringAssert.StartsWith(fuzzy.Url, "/components/commandpalette/search#example");

        var basic = palette.Single(s => s.Title == "Basic");
        Assert.AreEqual("/components/commandpalette#example1", basic.Url);
    }

    [TestMethod]
    public async Task A_topic_address_is_a_page_of_its_own_and_its_components_page()
    {
        var overview = await McpServerFixture.Http.GetStringAsync(McpServerFixture.Url("components/commandpalette"));
        var search = await McpServerFixture.Http.GetStringAsync(McpServerFixture.Url("components/commandpalette/search"));
        var api = await McpServerFixture.Http.GetStringAsync(McpServerFixture.Url("components/commandpalette/api"));

        using var scope = Assert.Scope();

        // The topic prerenders its own sections, under its own title and canonical address.
        StringAssert.Contains(search, "<title>CommandPalette: Search &amp; sources - bit BlazorUI</title>");
        StringAssert.Contains(search, "rel=\"canonical\" href=\"https://blazorui.bitplatform.dev/components/commandpalette/search\"");
        StringAssert.Contains(search, "id=\"example24\"", "The topic's first section (Fuzzy matching) was not rendered.");
        Assert.DoesNotContain("id=\"example2\"", search, "The overview's sections were rendered on a topic.");
        Assert.DoesNotContain("id=\"api-tables\"", search, "The API tables were rendered on a topic.");

        // It is still the component's page: the catalog finds the component one segment up (the category above the
        // title), and the topic strip marks the topic on screen.
        StringAssert.Contains(search, "doc-eyebrow");
        StringAssert.Matches(search, new System.Text.RegularExpressions.Regex("aria-current=\"page\"[^>]*>Search &amp; sources</a>"));

        // The overview holds the notes and the first sections; the API reference is a topic of its own.
        StringAssert.Contains(overview, "id=\"example2\"");
        Assert.DoesNotContain("id=\"example24\"", overview);
        StringAssert.Contains(api, "id=\"api-tables\"");
    }
}
