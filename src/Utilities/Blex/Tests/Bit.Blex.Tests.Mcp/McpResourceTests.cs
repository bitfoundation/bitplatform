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
        var resources = await McpTestHost.Client.ListResourcesAsync(cancellationToken: TestContext.CancellationToken);
        var uris = resources.Select(resource => resource.Uri).ToArray();

        foreach (var page in DocsPageRenderer.Pages)
        {
            Assert.Contains($"blex://docs/{page.Slug}", uris, $"The '{page.Title}' page is not listed as a resource.");
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
        var uris = (await McpTestHost.Client.ListResourcesAsync(cancellationToken: TestContext.CancellationToken)).Select(resource => resource.Uri).ToArray();

        foreach (var uri in new[] { "blex://guide", "blex://api", "blex://diagnostics" })
        {
            Assert.Contains(uri, uris);
        }
    }

    [TestMethod]
    public async Task The_templated_resources_cover_the_catalogs_too_big_to_enumerate()
    {
        var templates = await McpTestHost.Client.ListResourceTemplatesAsync(cancellationToken: TestContext.CancellationToken);

        Assert.AreSequenceEqual(
            new[] { "blex://guide/{heading}", "blex://api/{typeName}", "blex://diagnostics/{code}", "blex://docs/{slug}", "blex://source/{+path}" }, templates.Select(template => template.UriTemplate).ToArray(), global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);

        foreach (var template in templates)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(template.Description), $"'{template.UriTemplate}' is published without a description.");
        }

        // Source, not prose: a client that renders Markdown would eat its indentation.
        Assert.AreEqual("text/plain", templates.Single(t => t.UriTemplate == "blex://source/{+path}").MimeType);
    }

    [TestMethod]
    public async Task Every_listed_resource_can_actually_be_read()
    {
        foreach (var resource in await McpTestHost.Client.ListResourcesAsync(cancellationToken: TestContext.CancellationToken))
        {
            var text = await ReadAsync(resource.Uri);

            Assert.IsGreaterThan(100, text.Length, $"'{resource.Uri}' answered with {text.Length} characters.");
            Assert.IsFalse(text.StartsWith("No documentation page", StringComparison.Ordinal), $"'{resource.Uri}' is listed but does not resolve.");
            Assert.IsFalse(text.Contains("could not be rendered", StringComparison.Ordinal), $"'{resource.Uri}' could not be rendered.");
        }
    }

    [TestMethod]
    public async Task Each_templated_resource_resolves_a_real_key()
    {
        Assert.StartsWith("## Persistence", await ReadAsync("blex://guide/Persistence"));
        Assert.StartsWith("# BlexOptions", await ReadAsync("blex://api/BlexOptions"));
        Assert.StartsWith("# BLEX001", await ReadAsync("blex://diagnostics/BLEX001"));
        Assert.Contains("[BlexStore", await ReadAsync("blex://source/Demo%2FClient%2FStores%2FCartStore.cs"));
        Assert.Contains("# Effects", await ReadAsync("blex://docs/effects"));
    }

    [TestMethod]
    public async Task A_source_path_reads_with_its_slashes_as_written()
    {
        // What completion offers is the path as it is written; a client that pastes it into the template
        // without encoding it must land on the same file as one that encodes it.
        Assert.AreEqual(await ReadAsync("blex://source/Demo%2FClient%2FStores%2FCartStore.cs"),
                        await ReadAsync("blex://source/Demo/Client/Stores/CartStore.cs"));
        Assert.Contains("[BlexStore", await ReadAsync("blex://source/Demo/Client/Stores/CartStore.cs"));
    }

    [TestMethod]
    public async Task A_resource_and_the_tool_beside_it_resolve_the_same_key_the_same_way()
    {
        Assert.AreEqual(await McpCall.TextAsync("GetBlexGuideSection", new() { ["heading"] = "Undo / redo" }),
                        await ReadAsync("blex://guide/Undo%20%2F%20redo"));

        Assert.AreEqual(await McpCall.TextAsync("GetBlexSourceFile", new() { ["path"] = "Demo/Client/Stores/CartStore.cs" }),
                        await ReadAsync("blex://source/Demo%2FClient%2FStores%2FCartStore.cs"));

        Assert.Contains(await ReadAsync("blex://docs/history"), await McpCall.TextAsync("GetBlexDocsPage", new() { ["slug"] = "history" }));
    }

    [TestMethod]
    public async Task An_unknown_key_answers_with_a_document_saying_so()
    {
        Assert.Contains("No documentation page has the slug 'nope'", await ReadAsync("blex://docs/nope"));
        Assert.Contains("no section called 'nope'", await ReadAsync("blex://guide/nope"));
        Assert.Contains("no public type called 'nope'", await ReadAsync("blex://api/nope"));
        Assert.Contains("has no diagnostic 'nope'", await ReadAsync("blex://diagnostics/nope"));
        Assert.Contains("No source file at 'nope'", await ReadAsync("blex://source/nope"));
    }

    private static async Task<string> ReadAsync(string uri)
    {
        var result = await McpTestHost.Client.ReadResourceAsync(uri);

        var text = string.Join('\n', result.Contents.OfType<TextResourceContents>().Select(contents => contents.Text));

        Assert.IsFalse(string.IsNullOrWhiteSpace(text), $"'{uri}' came back empty.");

        return text;
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
