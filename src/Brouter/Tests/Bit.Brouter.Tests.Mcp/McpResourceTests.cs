using Bit.Brouter.Demo.Client;
using ModelContextProtocol.Protocol;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// The same body of knowledge, exposed for a client that wants to attach documentation to a
/// conversation up front or let a person browse and pin it.
/// <para>
/// Resources are the half of this server nobody notices when it breaks: an agent that only calls
/// tools never touches them, so a resource whose URI stopped resolving would keep failing quietly
/// in every client that lists them. The other thing worth pinning down is that a tool and a
/// resource taking the same argument resolve it identically - they are documented as reading the
/// same catalogs, and a slug that works in one must not fail in the other.
/// </para>
/// </summary>
[TestClass]
public class McpResourceTests
{
    [TestMethod]
    public async Task Every_documentation_page_is_listed_as_something_a_person_can_click()
    {
        // The templates alone are unusable in a picker: nothing on the wire says which slugs exist.
        var resources = await McpTestHost.Client.ListResourcesAsync();
        var uris = resources.Select(resource => resource.Uri).ToArray();

        foreach (var page in DocsCatalog.AllPages)
        {
            var slug = page.Slug.Length == 0 ? "overview" : page.Slug;

            CollectionAssert.Contains(uris, $"brouter://docs/{slug}", $"The '{page.Title}' page is not listed as a resource.");
        }

        foreach (var resource in resources)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(resource.Name));
            Assert.IsFalse(string.IsNullOrWhiteSpace(resource.Title), $"'{resource.Uri}' is listed without a title.");
            Assert.AreEqual("text/markdown", resource.MimeType, $"'{resource.Uri}' is served as something other than Markdown.");
        }
    }

    [TestMethod]
    public async Task The_whole_guide_the_api_and_the_constraints_are_listed_alongside_the_pages()
    {
        var uris = (await McpTestHost.Client.ListResourcesAsync()).Select(resource => resource.Uri).ToArray();

        foreach (var uri in new[] { "brouter://guide", "brouter://api", "brouter://constraints" })
        {
            CollectionAssert.Contains(uris, uri);
        }
    }

    [TestMethod]
    public async Task The_templated_resources_cover_the_catalogs_too_big_to_enumerate()
    {
        var templates = await McpTestHost.Client.ListResourceTemplatesAsync();
        var uris = templates.Select(template => template.UriTemplate).ToArray();

        CollectionAssert.AreEquivalent(
            new[] { "brouter://guide/{heading}", "brouter://api/{typeName}", "brouter://docs/{slug}", "brouter://source/{path}" },
            uris);

        foreach (var template in templates)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(template.Description), $"'{template.UriTemplate}' is published without a description.");
        }
    }

    [TestMethod]
    public async Task A_source_file_is_served_as_text_rather_than_as_markdown()
    {
        // It is source, not prose: a client that renders Markdown would eat its indentation.
        var template = (await McpTestHost.Client.ListResourceTemplatesAsync())
                       .Single(t => t.UriTemplate == "brouter://source/{path}");

        Assert.AreEqual("text/plain", template.MimeType);
    }

    [TestMethod]
    public async Task Every_listed_resource_can_actually_be_read()
    {
        // A listed URI that does not resolve is the one failure a browsing person cannot work around.
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
        var guide = await ReadAsync("brouter://guide/Async%20guards");
        StringAssert.StartsWith(guide, "## Async guards");

        var api = await ReadAsync("brouter://api/BrouterOptions");
        StringAssert.StartsWith(api, "# BrouterOptions");
        StringAssert.Contains(api, "CaseSensitive");

        var source = await ReadAsync("brouter://source/Demo%2FClient%2FAppRouter.razor");
        StringAssert.Contains(source, "<Broute");

        var docs = await ReadAsync("brouter://docs/guards");
        StringAssert.Contains(docs, "# Guards");
    }

    [TestMethod]
    public async Task The_exact_uri_of_a_page_wins_over_the_template_that_would_also_match_it()
    {
        // Both are published for brouter://docs/guards; they have to be the same document either way.
        var listed = await ReadAsync("brouter://docs/guards");
        var page = await McpCall.TextAsync("GetBrouterDocsPage", new() { ["slug"] = "guards" });

        StringAssert.Contains(page, listed, "The docs resource and the docs tool are handing out different text for the same slug.");
    }

    [TestMethod]
    public async Task A_resource_and_the_tool_beside_it_resolve_the_same_key_the_same_way()
    {
        // They are documented as sharing their resolution, which is what makes it safe for a client to
        // mix them: a heading, type or path learned from a tool has to work as a URI and vice versa.
        Assert.AreEqual(await McpCall.TextAsync("GetBrouterGuideSection", new() { ["heading"] = "Data loader" }),
                        await ReadAsync("brouter://guide/Data%20loader"));

        Assert.AreEqual(await McpCall.TextAsync("GetBrouterSourceFile", new() { ["path"] = "Demo/Client/DocsCatalog.cs" }),
                        await ReadAsync("brouter://source/Demo%2FClient%2FDocsCatalog.cs"));
    }

    [TestMethod]
    public async Task An_unknown_key_answers_with_a_document_saying_so()
    {
        // A read that fails outright tells a browsing person nothing; a resource that explains itself
        // is the same courtesy the tools extend.
        StringAssert.Contains(await ReadAsync("brouter://docs/nope"), "No documentation page has the slug 'nope'");
        StringAssert.Contains(await ReadAsync("brouter://guide/nope"), "no section called 'nope'");
        StringAssert.Contains(await ReadAsync("brouter://api/nope"), "no public type called 'nope'");
        StringAssert.Contains(await ReadAsync("brouter://source/nope"), "No source file at 'nope'");
    }

    [TestMethod]
    public async Task The_api_resource_renders_every_member_of_a_type_as_markdown()
    {
        var api = await ReadAsync("brouter://api/Broute");

        StringAssert.Contains(api, "# Broute (Component)");
        StringAssert.Contains(api, "## Parameter", "The parameters are not grouped under their own heading.");
        StringAssert.Contains(api, "**Path**");
        StringAssert.Contains(api, "`bool`", "Member types are not being rendered.");
    }

    [TestMethod]
    public async Task The_constraints_resource_is_a_table_a_model_can_read_at_a_glance()
    {
        var constraints = await ReadAsync("brouter://constraints");

        StringAssert.Contains(constraints, "| Constraint | Category | Rule | Passes | Fails |");
        StringAssert.Contains(constraints, "`{value:int}`");
        StringAssert.Contains(constraints, "`{value:slug}`", "The app's own custom constraint is missing from the table.");
    }

    private static async Task<string> ReadAsync(string uri)
    {
        var result = await McpTestHost.Client.ReadResourceAsync(uri);

        var text = string.Join('\n', result.Contents.OfType<TextResourceContents>().Select(contents => contents.Text));

        Assert.IsFalse(string.IsNullOrWhiteSpace(text), $"'{uri}' came back empty.");

        return text;
    }
}
