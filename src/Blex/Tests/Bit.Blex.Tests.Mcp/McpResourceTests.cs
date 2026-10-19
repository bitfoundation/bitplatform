using Bit.Blex.Demo.Server.Services;
using ModelContextProtocol.Protocol;

namespace Bit.Blex.Tests.Mcp;

/// <summary>
/// The same body of knowledge exposed as resources. They are the half of this server nobody notices when
/// it breaks - an agent that only calls tools never touches them - and a tool and a resource taking the
/// same argument have to resolve it identically.
/// </summary>
[TestClass]
public class McpResourceTests
{
    [TestMethod]
    public async Task Every_documentation_page_is_listed_as_something_a_person_can_click()
    {
        var resources = await McpTestHost.Client.ListResourcesAsync();
        var uris = resources.Select(resource => resource.Uri).ToArray();

        foreach (var page in DocsPageRenderer.Pages)
        {
            CollectionAssert.Contains(uris, $"blex://docs/{page.Slug}", $"The '{page.Title}' page is not listed as a resource.");
        }

        foreach (var resource in resources)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(resource.Title), $"'{resource.Uri}' is listed without a title.");
            Assert.AreEqual("text/markdown", resource.MimeType, $"'{resource.Uri}' is served as something other than Markdown.");
        }
    }

    [TestMethod]
    public async Task The_whole_guide_the_api_and_the_diagnostics_are_listed_alongside_the_pages()
    {
        var uris = (await McpTestHost.Client.ListResourcesAsync()).Select(resource => resource.Uri).ToArray();

        foreach (var uri in new[] { "blex://guide", "blex://api", "blex://diagnostics" })
        {
            CollectionAssert.Contains(uris, uri);
        }
    }

    [TestMethod]
    public async Task The_templated_resources_cover_the_catalogs_too_big_to_enumerate()
    {
        var templates = await McpTestHost.Client.ListResourceTemplatesAsync();

        CollectionAssert.AreEquivalent(
            new[] { "blex://guide/{heading}", "blex://api/{typeName}", "blex://diagnostics/{code}", "blex://docs/{slug}", "blex://source/{path}" },
            templates.Select(template => template.UriTemplate).ToArray());

        foreach (var template in templates)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(template.Description), $"'{template.UriTemplate}' is published without a description.");
        }

        // Source, not prose: a client that renders Markdown would eat its indentation.
        Assert.AreEqual("text/plain", templates.Single(t => t.UriTemplate == "blex://source/{path}").MimeType);
    }

    [TestMethod]
    public async Task Every_listed_resource_can_actually_be_read()
    {
        foreach (var resource in await McpTestHost.Client.ListResourcesAsync())
        {
            var text = await ReadAsync(resource.Uri);

            Assert.IsTrue(text.Length > 100, $"'{resource.Uri}' answered with {text.Length} characters.");
            Assert.IsFalse(text.StartsWith("No documentation page", StringComparison.Ordinal), $"'{resource.Uri}' is listed but does not resolve.");
            Assert.IsFalse(text.Contains("could not be rendered", StringComparison.Ordinal), $"'{resource.Uri}' could not be rendered.");
        }
    }

    [TestMethod]
    public async Task Each_templated_resource_resolves_a_real_key()
    {
        StringAssert.StartsWith(await ReadAsync("blex://guide/Persistence"), "## Persistence");
        StringAssert.StartsWith(await ReadAsync("blex://api/BlexOptions"), "# BlexOptions");
        StringAssert.StartsWith(await ReadAsync("blex://diagnostics/BLEX001"), "# BLEX001");
        StringAssert.Contains(await ReadAsync("blex://source/Demo%2FClient%2FStores%2FCartStore.cs"), "[BlexStore");
        StringAssert.Contains(await ReadAsync("blex://docs/effects"), "# Effects");
    }

    [TestMethod]
    public async Task A_resource_and_the_tool_beside_it_resolve_the_same_key_the_same_way()
    {
        Assert.AreEqual(await McpCall.TextAsync("GetBlexGuideSection", new() { ["heading"] = "Undo / redo" }),
                        await ReadAsync("blex://guide/Undo%20%2F%20redo"));

        Assert.AreEqual(await McpCall.TextAsync("GetBlexSourceFile", new() { ["path"] = "Demo/Client/Stores/CartStore.cs" }),
                        await ReadAsync("blex://source/Demo%2FClient%2FStores%2FCartStore.cs"));

        StringAssert.Contains(await McpCall.TextAsync("GetBlexDocsPage", new() { ["slug"] = "history" }), await ReadAsync("blex://docs/history"));
    }

    [TestMethod]
    public async Task An_unknown_key_answers_with_a_document_saying_so()
    {
        StringAssert.Contains(await ReadAsync("blex://docs/nope"), "No documentation page has the slug 'nope'");
        StringAssert.Contains(await ReadAsync("blex://guide/nope"), "no section called 'nope'");
        StringAssert.Contains(await ReadAsync("blex://api/nope"), "no public type called 'nope'");
        StringAssert.Contains(await ReadAsync("blex://diagnostics/nope"), "has no diagnostic 'nope'");
        StringAssert.Contains(await ReadAsync("blex://source/nope"), "No source file at 'nope'");
    }

    private static async Task<string> ReadAsync(string uri)
    {
        var result = await McpTestHost.Client.ReadResourceAsync(uri);

        var text = string.Join('\n', result.Contents.OfType<TextResourceContents>().Select(contents => contents.Text));

        Assert.IsFalse(string.IsNullOrWhiteSpace(text), $"'{uri}' came back empty.");

        return text;
    }
}
