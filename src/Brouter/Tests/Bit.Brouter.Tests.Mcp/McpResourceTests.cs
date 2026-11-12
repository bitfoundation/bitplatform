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
        var resources = await McpTestHost.Client.ListResourcesAsync(cancellationToken: TestContext.CancellationToken);
        var uris = resources.Select(resource => resource.Uri).ToArray();

        foreach (var page in DocsCatalog.AllPages)
        {
            var slug = page.Slug.Length == 0 ? "overview" : page.Slug;

            Assert.Contains($"brouter://docs/{slug}", uris, $"The '{page.Title}' page is not listed as a resource.");
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
        var uris = (await McpTestHost.Client.ListResourcesAsync(cancellationToken: TestContext.CancellationToken)).Select(resource => resource.Uri).ToArray();

        foreach (var uri in new[] { "brouter://guide", "brouter://api", "brouter://constraints" })
        {
            Assert.Contains(uri, uris);
        }
    }

    [TestMethod]
    public async Task The_templated_resources_cover_the_catalogs_too_big_to_enumerate()
    {
        var templates = await McpTestHost.Client.ListResourceTemplatesAsync(cancellationToken: TestContext.CancellationToken);
        var uris = templates.Select(template => template.UriTemplate).ToArray();

        Assert.AreSequenceEqual(
            new[] { "brouter://guide/{heading}", "brouter://api/{typeName}", "brouter://docs/{slug}", "brouter://source/{path}" }, uris, Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);

        foreach (var template in templates)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(template.Description), $"'{template.UriTemplate}' is published without a description.");
        }
    }

    [TestMethod]
    public async Task A_source_file_is_served_as_text_rather_than_as_markdown()
    {
        // It is source, not prose: a client that renders Markdown would eat its indentation.
        var template = (await McpTestHost.Client.ListResourceTemplatesAsync(cancellationToken: TestContext.CancellationToken))
                       .Single(t => t.UriTemplate == "brouter://source/{path}");

        Assert.AreEqual("text/plain", template.MimeType);
    }

    [TestMethod]
    public async Task Every_listed_resource_can_actually_be_read()
    {
        // A listed URI that does not resolve is the one failure a browsing person cannot work around.
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
        var guide = await ReadAsync("brouter://guide/Async%20guards");
        Assert.StartsWith("## Async guards", guide);

        var api = await ReadAsync("brouter://api/BrouterOptions");
        Assert.StartsWith("# BrouterOptions", api);
        Assert.Contains("CaseSensitive", api);

        var source = await ReadAsync("brouter://source/Demo%2FClient%2FAppRouter.razor");
        Assert.Contains("<Broute", source);

        var docs = await ReadAsync("brouter://docs/guards");
        Assert.Contains("# Guards", docs);
    }

    [TestMethod]
    public async Task The_exact_uri_of_a_page_wins_over_the_template_that_would_also_match_it()
    {
        // Both are published for brouter://docs/guards; they have to be the same document either way.
        var listed = await ReadAsync("brouter://docs/guards");
        var page = await McpCall.TextAsync("GetBrouterDocsPage", new() { ["slug"] = "guards" });

        Assert.Contains(listed, page, "The docs resource and the docs tool are handing out different text for the same slug.");
    }

    [TestMethod]
    public async Task A_resource_and_the_tool_beside_it_resolve_the_same_key_the_same_way()
    {
        // They are documented as sharing their resolution, which is what makes it safe for a client to
        // mix them: a heading, type or path learned from a tool has to work as a URI and vice versa.
        Assert.AreEqual(await McpCall.TextAsync("GetBrouterGuideSection", new() { ["heading"] = "Data loader" }),
                        await ReadAsync("brouter://guide/Data%20loader"));

        Assert.AreEqual(await McpCall.TextAsync("GetBrouterSourceFile", new() { ["path"] = "Demo/Client/AppRouter.razor" }),
                        await ReadAsync("brouter://source/Demo%2FClient%2FAppRouter.razor"));
    }

    [TestMethod]
    public async Task An_unknown_key_answers_with_a_document_saying_so()
    {
        // A read that fails outright tells a browsing person nothing; a resource that explains itself
        // is the same courtesy the tools extend.
        Assert.Contains("No documentation page has the slug 'nope'", await ReadAsync("brouter://docs/nope"));
        Assert.Contains("no section called 'nope'", await ReadAsync("brouter://guide/nope"));
        Assert.Contains("no public type called 'nope'", await ReadAsync("brouter://api/nope"));
        Assert.Contains("No source file at 'nope'", await ReadAsync("brouter://source/nope"));
    }

    [TestMethod]
    public async Task The_api_resource_renders_every_member_of_a_type_as_markdown()
    {
        var api = await ReadAsync("brouter://api/Broute");

        Assert.Contains("# Broute (Component)", api);
        Assert.Contains("## Parameter", api, "The parameters are not grouped under their own heading.");
        Assert.Contains("**Path**", api);
        Assert.Contains("`bool`", api, "Member types are not being rendered.");
    }

    [TestMethod]
    public async Task The_constraints_resource_is_a_table_a_model_can_read_at_a_glance()
    {
        var constraints = await ReadAsync("brouter://constraints");

        Assert.Contains("| Constraint | Category | Rule | Passes | Fails |", constraints);
        Assert.Contains("`{value:int}`", constraints);
        Assert.Contains("`{value:slug}`", constraints, "The app's own custom constraint is missing from the table.");
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
