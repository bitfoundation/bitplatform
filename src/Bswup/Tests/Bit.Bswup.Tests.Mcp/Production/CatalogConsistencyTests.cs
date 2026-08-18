using System.Text.RegularExpressions;
using Bit.Bswup.Demo.Client;
using Bit.Bswup.Demo.Server.Controllers;
using Bit.Bswup.Demo.Server.Services;
using Microsoft.AspNetCore.Components;
using ModelContextProtocol.Server;

namespace Bit.Bswup.Tests.Mcp.Production;

/// <summary>
/// The wiring between the lists this server keeps: the docs catalog, the tools, and every place a
/// tool name or a slug is written into prose.
/// <para>
/// None of these references is checked by a compiler. A tool renamed, a page added to the router
/// but not to the catalog, a slug quoted in a description that no longer resolves - each of them
/// leaves an agent following an instruction into a dead end, and each of them is invisible until
/// someone reads the answer. That is what this class is for.
/// </para>
/// </summary>
[TestClass]
public class CatalogConsistencyTests
{
    /// <summary>The tool names the MCP server publishes, read off the controller.</summary>
    private static readonly string[] _toolNames =
    [
        .. typeof(McpController).GetMethods()
            .SelectMany(method => method.GetCustomAttributes(typeof(McpServerToolAttribute), inherit: true)
                                        .OfType<McpServerToolAttribute>()
                                        // An attribute that names nothing is published under the method's own
                                        // name, so that is the name this list has to hold.
                                        .Select(attribute => attribute.Name ?? method.Name))
    ];

    /// <summary>Anything written like a Bswup tool call in a piece of prose.</summary>
    private static readonly Regex _toolMention = new(@"\b(?<name>(?:Get|Search|Inspect|Analyze)Bswup[A-Za-z]*)");

    private static void AssertOnlyRealTools(string text, string where)
    {
        foreach (var mentioned in _toolMention.Matches(text).Select(match => match.Groups["name"].Value).Distinct())
        {
            CollectionAssert.Contains(_toolNames, mentioned, $"{where} points at '{mentioned}', which is not a tool");
        }
    }

    // -- Tool names quoted in prose --------------------------------------------

    [TestMethod]
    public void EveryToolDescription_OnlyPointsAtToolsThatExist()
    {
        foreach (var method in typeof(McpController).GetMethods())
        {
            var tool = method.GetCustomAttributes(typeof(McpServerToolAttribute), inherit: true).OfType<McpServerToolAttribute>().FirstOrDefault();
            if (tool is null) continue;

            var description = method.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), inherit: true)
                                    .OfType<System.ComponentModel.DescriptionAttribute>()
                                    .FirstOrDefault()?.Description;

            Assert.IsFalse(string.IsNullOrWhiteSpace(description), $"{tool.Name} has no description");

            AssertOnlyRealTools(description!, $"the description of {tool.Name}");
        }
    }

    [TestMethod]
    public void EveryOptionsDocsPointer_NamesAPageThatExists()
    {
        var options = BswupScriptCatalog.ScriptOptions
            .Concat(BswupScriptCatalog.WorkerSettings)
            .Concat(BswupProgressCatalog.Parameters)
            .Where(option => option.Docs is not null);

        foreach (var option in options)
        {
            AssertOnlyRealTools(option.Docs!, $"the Docs pointer of {option.Name}");

            // The match has to succeed before the slug is looked up. An empty slug is valid -
            // it is the introduction page's own - but it is also what a pointer that names no
            // slug at all yields, and that one would sail through the assertion below.
            var match = Regex.Match(option.Docs!, "slug: \"(?<slug>[^\"]*)\"");

            Assert.IsTrue(match.Success, $"the Docs pointer of {option.Name} names no slug: {option.Docs}");

            var slug = match.Groups["slug"].Value;

            Assert.IsNotNull(DocsCatalog.FindBySlug(slug), $"{option.Name} points at the '{slug}' page, which does not exist");
        }
    }

    [TestMethod]
    public void EverySearchHit_NamesATheCallThatReturnsItsFullText()
    {
        var hits = new[] { "cache", "update", "progress", "worker", "install", "handler", "asset", "mode", "source" }
            .SelectMany(term => BswupSearchIndex.Search(term, 50))
            .DistinctBy(hit => hit.Tool)
            .ToArray();

        Assert.IsTrue(hits.Length > 0, "the search returned nothing at all, so nothing below was checked");

        foreach (var hit in hits)
        {
            AssertOnlyRealTools(hit.Tool, $"the '{hit.Title}' hit");
        }
    }

    [TestMethod]
    public void EverySearchHitPointingAtADocsPage_NamesASlugThatResolves()
    {
        var hits = new[] { "cache", "update", "progress", "worker", "install", "handler", "playground", "mcp", "recipe" }
            .SelectMany(term => BswupSearchIndex.Search(term, 50))
            .Where(hit => hit.Tool.StartsWith("GetBswupDocsPage", StringComparison.Ordinal))
            .DistinctBy(hit => hit.Tool)
            .ToArray();

        Assert.IsTrue(hits.Length > 0, "no hit pointed at a docs page, so nothing below was checked");

        foreach (var hit in hits)
        {
            var match = Regex.Match(hit.Tool, "slug: \"(?<slug>[^\"]*)\"");

            Assert.IsTrue(match.Success, $"the '{hit.Title}' hit names no slug: {hit.Tool}");

            var slug = match.Groups["slug"].Value;

            Assert.IsNotNull(DocsCatalog.FindBySlug(slug), $"'{hit.Title}' points at the '{slug}' page, which does not exist");
        }
    }

    [TestMethod]
    public void EverySearchHitPointingAtAGuideSection_NamesAHeadingThatResolves()
    {
        var hits = new[] { "cache", "update", "progress", "worker", "install", "handler", "cleanup", "upgrade" }
            .SelectMany(term => BswupSearchIndex.Search(term, 50))
            .Where(hit => hit.Tool.StartsWith("GetBswupGuideSection", StringComparison.Ordinal))
            .DistinctBy(hit => hit.Tool)
            .ToArray();

        Assert.IsTrue(hits.Length > 0, "no hit pointed at a guide section, so nothing below was checked");

        foreach (var hit in hits)
        {
            var heading = Regex.Match(hit.Tool, "heading: \"(?<heading>[^\"]*)\"").Groups["heading"].Value;

            Assert.IsNotNull(BswupSourceCatalog.GetGuideSection(heading), $"the guide has no '{heading}' section");
        }
    }

    [TestMethod]
    public void EverySearchHitPointingAtASourceFile_NamesAPathThatResolves()
    {
        var hits = new[] { "worker", "progress", "sample", "index", "program", "razor" }
            .SelectMany(term => BswupSearchIndex.Search(term, 50))
            .Where(hit => hit.Tool.StartsWith("GetBswupSourceFile", StringComparison.Ordinal))
            .DistinctBy(hit => hit.Tool)
            .ToArray();

        Assert.IsTrue(hits.Length > 0, "no hit pointed at a source file, so nothing below was checked");

        foreach (var hit in hits)
        {
            var path = Regex.Match(hit.Tool, "path: \"(?<path>[^\"]*)\"").Groups["path"].Value;

            Assert.IsNotNull(BswupSourceCatalog.GetSourceFile(path), $"there is no source file at '{path}'");
        }
    }

    [TestMethod]
    public void ThePlaceholderForAPageThatWillNotRender_PointsSomewhereRealInstead()
    {
        foreach (var page in DocsCatalog.AllPages)
        {
            var message = DocsPageRenderer.Unavailable(page, "a reason");

            AssertOnlyRealTools(message, $"the fallback for '{page.Title}'");
            StringAssert.Contains(message, page.Url, page.Title);
        }
    }

    // -- The docs catalog against the router -----------------------------------

    [TestMethod]
    public void EveryCatalogPage_HasTheRouteTheCatalogClaims()
    {
        // The nav panel writes its links out by hand and the catalog is a second list; a page whose
        // route changed under it would be advertised - and sitemapped - at a URL that 404s.
        foreach (var page in DocsCatalog.AllPages)
        {
            var routes = page.PageType
                .GetCustomAttributes(typeof(RouteAttribute), inherit: true)
                .OfType<RouteAttribute>()
                .Select(route => route.Template)
                .ToArray();

            Assert.IsTrue(routes.Length > 0, $"{page.PageType.Name} is in the catalog but is not a routable page");
            CollectionAssert.Contains(routes, page.Url, $"{page.PageType.Name}: the catalog says '{page.Url}', the component says '{string.Join(", ", routes)}'");
        }
    }

    [TestMethod]
    public void EveryCatalogPage_IsDescribedAndKeyworded()
    {
        foreach (var page in DocsCatalog.AllPages)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(page.Title), page.Url);
            Assert.IsFalse(string.IsNullOrWhiteSpace(page.Description), $"{page.Url} has no description - it is what search matches on");
            Assert.IsFalse(string.IsNullOrWhiteSpace(page.Keywords), $"{page.Url} has no keywords");
        }
    }

    [TestMethod]
    public void CatalogSlugsAndPageTypesAreUnique()
    {
        var duplicateSlugs = DocsCatalog.AllPages.GroupBy(page => page.Slug, StringComparer.OrdinalIgnoreCase)
                                                 .Where(group => group.Count() > 1)
                                                 .Select(group => group.Key)
                                                 .ToArray();

        Assert.AreEqual(0, duplicateSlugs.Length, string.Join(", ", duplicateSlugs));

        var duplicateTypes = DocsCatalog.AllPages.GroupBy(page => page.PageType)
                                                 .Where(group => group.Count() > 1)
                                                 .Select(group => group.Key.Name)
                                                 .ToArray();

        Assert.AreEqual(0, duplicateTypes.Length, string.Join(", ", duplicateTypes));
    }

    [TestMethod]
    public void EveryCatalogSlug_ResolvesBackToItsOwnPage()
    {
        foreach (var page in DocsCatalog.AllPages)
        {
            Assert.AreEqual(page, DocsCatalog.FindBySlug(page.Slug), page.Slug);
        }
    }

    [TestMethod]
    public void EveryNoIndexUrl_BelongsToARoutablePage()
    {
        // These are real routes that are deliberately not documentation, so they are not in the
        // catalog - but a typo here would silently stop emitting the noindex meta PageOutlet reads
        // this list for, and nothing would ever say so.
        var routes = typeof(DocsCatalog).Assembly.GetTypes()
            .SelectMany(type => type.GetCustomAttributes(typeof(RouteAttribute), inherit: true).OfType<RouteAttribute>())
            .Select(route => route.Template)
            .ToArray();

        foreach (var url in SiteMetadata.NoIndexUrls)
        {
            CollectionAssert.Contains(routes, url, $"'{url}' is marked noindex but no component routes there");
        }
    }

    // -- The prompts -----------------------------------------------------------

    [TestMethod]
    public void EveryPromptBody_OnlyPointsAtToolsAndPagesThatExist()
    {
        var prompts = new[]
        {
            ("add-bswup-to-app", McpPrompts.AddBswupToApp("standalone-wasm")),
            ("configure-bswup-caching", McpPrompts.ConfigureBswupCaching("anything")),
            ("debug-bswup", McpPrompts.DebugBswup("anything")),
            ("remove-bswup", McpPrompts.RemoveBswup()),
        };

        foreach (var (name, body) in prompts)
        {
            AssertOnlyRealTools(body, $"the '{name}' prompt");

            foreach (Match match in Regex.Matches(body, "slug: \"(?<slug>[^\"]*)\""))
            {
                var slug = match.Groups["slug"].Value;

                Assert.IsNotNull(DocsCatalog.FindBySlug(slug), $"'{name}' points at the '{slug}' page, which does not exist");
            }

            foreach (Match match in Regex.Matches(body, "heading: \"(?<heading>[^\"]*)\""))
            {
                var heading = match.Groups["heading"].Value;

                Assert.IsNotNull(BswupSourceCatalog.GetGuideSection(heading), $"'{name}' points at the '{heading}' section, which does not exist");
            }

            foreach (Match match in Regex.Matches(body, "path: \"(?<path>[^\"]*)\""))
            {
                var path = match.Groups["path"].Value;

                Assert.IsNotNull(BswupSourceCatalog.GetSourceFile(path), $"'{name}' points at '{path}', which is not embedded");
            }
        }
    }
}
