using System.Net;
using System.Text.Json;
using System.Xml.Linq;
using System.IO.Compression;
using System.Net.Http.Headers;
using Bit.Butil.Tests.Mcp.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.Mcp;

/// <summary>
/// The corpus the site's own search box downloads and searches: <c>/api/docs/search-index</c>.
/// <para>
/// It is served by the same app as the MCP tools and it is built the same way - by parsing the
/// embedded source of the docs pages - so it fails the same way: silently. A parser that loses its
/// footing on one attribute does not throw, it returns a shorter index, and the only symptom is a
/// search box that cannot find what is plainly written on the page. Every assertion here is a
/// version of that symptom, checked against the deployment rather than against the parser.
/// </para>
/// </summary>
[TestClass]
public class DocsSearchIndexTests
{
    private const string IndexPath = "api/docs/search-index";

    private static HttpClient Http => McpServerFixture.Http;

    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private static Index? _index;

    /// <summary>The wire shape, restated here: this suite talks to the app over HTTP, not to its types.</summary>
    private sealed record Entry(string Title, string? Page, string Url, string Group, string Keywords, string Summary, string Body);

    private sealed record Index(Entry[] Entries);

    /// <summary>
    /// The index, fetched once for the fixture. The tests inside a fixture run in order, so the
    /// first of them pays for it and the rest read it.
    /// </summary>
    private static async Task<Entry[]> EntriesAsync()
    {
        if (_index is not null) return _index.Entries;

        var json = await Http.GetStringAsync(McpServerFixture.Url(IndexPath));
        _index = JsonSerializer.Deserialize<Index>(json, _json);

        Assert.IsNotNull(_index?.Entries, "The search index did not deserialize into anything.");

        return _index.Entries;
    }

    /// <summary>Every page of the site, taken from the sitemap it generates from its own nav.</summary>
    private static async Task<string[]> PagesAsync()
    {
        var sitemap = XDocument.Parse(await Http.GetStringAsync(McpServerFixture.Url("sitemap.xml")));

        return
        [
            .. sitemap.Descendants()
                .Where(element => element.Name.LocalName == "loc")
                .Select(element => new Uri(element.Value).AbsolutePath)
                .Where(path => path != "/")
        ];
    }

    [TestMethod]
    public async Task Every_page_the_site_publishes_is_in_the_index()
    {
        var entries = await EntriesAsync();
        var pages = await PagesAsync();

        var indexed = entries.Where(entry => entry.Page is null).Select(entry => entry.Url).ToHashSet(StringComparer.Ordinal);

        using (Assert.Scope())
        {
            foreach (var page in pages)
            {
                Assert.IsTrue(indexed.Contains(page), $"{page} is in the sitemap but has no entry of its own in the search index.");
            }
        }
    }

    [TestMethod]
    public async Task Every_page_is_indexed_deeper_than_its_own_title()
    {
        // The failure this catches: a page that parses down to one entry. The corpus is what a
        // reader searches, and a page reduced to its title is a page whose content is unfindable -
        // which is the whole reason the index exists rather than the nav taxonomy alone.
        var entries = await EntriesAsync();
        var pages = await PagesAsync();

        var counts = entries
            .GroupBy(entry => entry.Url.Split('#')[0], StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        using (Assert.Scope())
        {
            foreach (var page in pages)
            {
                Assert.IsTrue(counts.GetValueOrDefault(page) > 1,
                    $"{page} contributed {counts.GetValueOrDefault(page)} entries: its sections and its API table were not indexed.");
            }
        }
    }

    [TestMethod]
    public async Task A_section_is_a_hit_of_its_own_at_its_own_anchor()
    {
        var entries = await EntriesAsync();

        var section = entries.FirstOrDefault(entry => entry.Url == "/clipboard#copy-text");

        Assert.IsNotNull(section, "The Clipboard page's 'Copy text' section is not in the index.");

        using (Assert.Scope())
        {
            Assert.AreEqual("Copy text", section.Title);
            Assert.AreEqual("Clipboard", section.Page, "A section has to name the page it would open.");
            Assert.Contains("WriteText", section.Keywords, "The member the section documents is what someone searches for it by.");
            // The sample is the section's body, and a sample is what someone remembers of a page.
            Assert.Contains("WriteText", section.Body);
        }
    }

    [TestMethod]
    public async Task The_reference_table_is_indexed_row_by_row()
    {
        var entries = await EntriesAsync();

        var member = entries.FirstOrDefault(entry => entry.Url == "/clipboard#api-section" && entry.Title == "ReadText");

        Assert.IsNotNull(member, "Clipboard.ReadText has no row in the index, so searching its exact name finds only the page.");
        Assert.Contains("ValueTask<string> ReadText()", member.Keywords, "The signature is part of what a row is searched by.");
    }

    [TestMethod]
    public async Task The_prose_of_a_page_is_searchable_and_not_only_its_headings()
    {
        var entries = await EntriesAsync();

        // "unsanitized" is written once, in the middle of a section on one page, and appears nowhere
        // in any title, slug or nav summary. Before the pages themselves were indexed, this word -
        // and every word like it - found nothing at all.
        var matches = entries.Where(entry =>
            entry.Summary.Contains("unsanitized", StringComparison.OrdinalIgnoreCase) ||
            entry.Body.Contains("unsanitized", StringComparison.OrdinalIgnoreCase));

        Assert.IsNotEmpty(matches, "A word written in a page's prose is not in the index.");
        Assert.IsTrue(matches.All(entry => entry.Url.StartsWith("/clipboard", StringComparison.Ordinal)),
            "'unsanitized' is Clipboard's word; matching it elsewhere means text is leaking between entries.");
    }

    [TestMethod]
    public async Task The_index_carries_text_rather_than_markup()
    {
        var entries = await EntriesAsync();

        // Markup in a body is a corpus that matches "div" and "class" on every page, and a snippet
        // that quotes a CSS class back at the reader. What is looked for here is only what the
        // pages' own markup brings with it: the code samples ARE indexed, deliberately, and they
        // contain angle brackets and Razor directives of their own.
        string[] leaks = ["class=\"", "<span", "role=\"row\""];

        using (Assert.Scope())
        {
            foreach (var leak in leaks)
            {
                var entry = entries.FirstOrDefault(entry => entry.Body.Contains(leak, StringComparison.Ordinal)
                                                         || entry.Summary.Contains(leak, StringComparison.Ordinal));

                Assert.IsNull(entry, $"\"{leak}\" reached the index through {entry?.Url}: it is markup, not documentation.");
            }
        }
    }

    [TestMethod]
    public async Task Every_entry_can_be_opened()
    {
        var entries = await EntriesAsync();
        var pages = (await PagesAsync()).ToHashSet(StringComparer.Ordinal);

        using (Assert.Scope())
        {
            foreach (var entry in entries)
            {
                Assert.IsNotEmpty(entry.Title, $"{entry.Url} has an entry with no title.");
                Assert.IsTrue(pages.Contains(entry.Url.Split('#')[0]), $"{entry.Url} points at a page the site does not publish.");
                Assert.IsFalse(entry.Url.EndsWith('#'), $"{entry.Url} carries an empty anchor.");
            }
        }
    }

    [TestMethod]
    public async Task The_index_is_served_compressed()
    {
        // It is the largest thing the site downloads and it is prose, which compresses to a fraction
        // of itself. Serving it uncompressed to a browser that asked for gzip is the difference
        // between a search box that is ready by the time a query is typed and one that is not.
        using var request = new HttpRequestMessage(HttpMethod.Get, McpServerFixture.Url(IndexPath));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        using var response = await Http.SendAsync(request);
        var compressed = await response.Content.ReadAsByteArrayAsync();

        Assert.Contains("gzip", response.Content.Headers.ContentEncoding, "The index was served uncompressed to a client that asked for gzip.");

        using var stream = new GZipStream(new MemoryStream(compressed), CompressionMode.Decompress);
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        Assert.IsGreaterThan(compressed.Length * 2, json.Length, "The compressed index is barely smaller than the JSON, which means it is not the JSON.");
        Assert.Contains("\"entries\"", json);
    }

    [TestMethod]
    public async Task A_returning_visitor_revalidates_instead_of_downloading_it_again()
    {
        using var first = await Http.GetAsync(McpServerFixture.Url(IndexPath));
        var etag = first.Headers.ETag;

        Assert.IsNotNull(etag, "The index has no ETag, so every visit downloads it again.");

        using var again = new HttpRequestMessage(HttpMethod.Get, McpServerFixture.Url(IndexPath));
        again.Headers.IfNoneMatch.Add(etag);

        using var second = await Http.SendAsync(again);

        Assert.AreEqual(HttpStatusCode.NotModified, second.StatusCode);
        Assert.IsEmpty(await second.Content.ReadAsByteArrayAsync(), "A 304 answered with a body.");
    }
}
