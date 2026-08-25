using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Protocol;
using Bit.Butil.Tests.Mcp.Infrastructure;

namespace Bit.Butil.Tests.Mcp;

/// <summary>
/// The resource half of the server: the same body of knowledge the tools serve, exposed for a
/// client that wants to attach documentation to a conversation up front or let a person browse and
/// pin it.
/// <para>
/// Both halves read the same catalogs, so the interesting assertions are the ones that hold them
/// against each other: a resource that answered something different from the tool covering the same
/// material would mean one of the two had gone stale, which is the failure the shared catalogs
/// exist to make impossible.
/// </para>
/// </summary>
[TestClass]
public class ResourceTests : McpTestBase
{
    [TestMethod]
    public async Task Server_advertises_exactly_the_expected_resources()
    {
        var resources = await Mcp.ListResourcesAsync(cancellationToken: Ct);

        var advertised = resources.ToDictionary(resource => resource.Name, resource => resource.Uri, StringComparer.Ordinal);

        using (Assert.Scope())
        {
            CollectionAssert.AreEquivalent(ButilMcp.Resources.Keys.ToArray(), advertised.Keys.ToArray());

            foreach (var (name, uri) in ButilMcp.Resources)
            {
                // TryGetValue rather than the indexer: a resource that is gone should fail the
                // assertion above and this one, not throw out of the whole Multiple block.
                Assert.IsTrue(advertised.TryGetValue(name, out var advertisedUri), $"{name} is not advertised at all.");
                Assert.AreEqual(uri, advertisedUri);
            }

            foreach (var resource in resources)
            {
                // The name is the identifier a client stores and a completion returns; the title is
                // the line a person reads in a picker. Both are required, and they are not the same
                // thing - see the class comment on McpResources.
                Assert.IsFalse(string.IsNullOrEmpty(resource.Title), $"{resource.Name} has no title.");
                Assert.IsFalse(string.IsNullOrEmpty(resource.Description), $"{resource.Name} has no description.");
                Assert.IsFalse(string.IsNullOrEmpty(resource.MimeType), $"{resource.Name} declares no MIME type.");
            }
        }
    }

    [TestMethod]
    public async Task Server_advertises_exactly_the_expected_resource_templates()
    {
        var templates = await Mcp.ListResourceTemplatesAsync(cancellationToken: Ct);

        var advertised = templates.ToDictionary(template => template.Name, template => template.UriTemplate, StringComparer.Ordinal);

        using (Assert.Scope())
        {
            CollectionAssert.AreEquivalent(ButilMcp.ResourceTemplates.Keys.ToArray(), advertised.Keys.ToArray());

            foreach (var (name, expected) in ButilMcp.ResourceTemplates)
            {
                Assert.IsTrue(advertised.TryGetValue(name, out var advertisedTemplate), $"{name} is not advertised at all.");
                Assert.AreEqual(expected.UriTemplate, advertisedTemplate);
            }

            foreach (var template in templates)
            {
                Assert.IsFalse(string.IsNullOrEmpty(template.Title), $"{template.Name} has no title.");
                Assert.IsFalse(string.IsNullOrEmpty(template.Description), $"{template.Name} has no description.");
            }
        }
    }

    [TestMethod]
    public async Task The_guide_resource_is_the_whole_readme()
    {
        var text = await ReadTextAsync("butil://guide");

        using (Assert.Scope())
        {
            Assert.IsNotEmpty(text, "An empty guide means the README was not embedded into the published app.");
            Assert.Contains("Bit.Butil", text);
            Assert.Contains("## Getting started", text);
            Assert.Contains("AddBitButilServices", text);
        }
    }

    [TestMethod]
    public async Task The_api_resource_separates_what_you_inject_from_what_you_construct()
    {
        var text = await ReadTextAsync("butil://api");

        using (Assert.Scope())
        {
            Assert.StartsWith("# Bit.Butil public API", text);
            Assert.Contains("## Injectable services", text);
            Assert.Contains("## Everything else", text);
            Assert.Contains("**Clipboard**", text);

            // The injectable half is listed before the other one, which is the whole reason the
            // resource splits them: it is the first question a reader has about a type.
            Assert.IsLessThan(text.IndexOf("## Everything else", StringComparison.Ordinal),
                              text.IndexOf("## Injectable services", StringComparison.Ordinal));
        }
    }

    [TestMethod]
    public async Task The_support_resource_is_a_readable_table()
    {
        var text = await ReadTextAsync("butil://support");

        using (Assert.Scope())
        {
            // The matrix and the page index are one table now: the same rows answer "which engines
            // run this" and "where is it written up", so the resource is the index.
            Assert.StartsWith("# Bit.Butil documentation pages", text);
            Assert.Contains("| Slug | Title | Summary | Services | Engines | Requires |", text);

            var rows = DocsIndexRow.ParseAll(text);

            Assert.IsGreaterThan(40, rows.Length, "The table should carry a row per documented API.");
            Assert.Contains("clipboard", rows.Select(row => row.Slug));

            // The table is a map: only the name of each precondition, with the sentence explaining
            // it one PlanButilFeature call away.
            Assert.Contains("Secure context", text);
            Assert.DoesNotContain("only available over HTTPS or on localhost", text);
        }
    }

    [TestMethod]
    public async Task A_guide_section_resource_answers_the_same_text_as_the_tool()
    {
        var viaResource = await ReadTextAsync($"butil://guide/{Uri.EscapeDataString("Getting started")}");
        var viaTool = Text(await CallAsync("GetButilGuideSection", new { heading = "Getting started" }));

        Assert.AreEqual(viaTool, viaResource,
            "The resource and the tool read the same catalog; answering differently means one of them has drifted.");
    }

    [TestMethod]
    public async Task A_type_resource_renders_the_same_reference_the_tool_returns()
    {
        var text = await ReadTextAsync("butil://api/Clipboard");

        using (Assert.Scope())
        {
            Assert.StartsWith("# Clipboard (Service)", text);
            Assert.Contains("@inject Bit.Butil.Clipboard clipboard", text);
            Assert.Contains("## Method", text);
            Assert.Contains("**WriteText**", text);
            Assert.Contains("Documentation page: /clipboard", text);
        }
    }

    [TestMethod]
    public async Task A_docs_page_resource_renders_the_page()
    {
        var text = await ReadTextAsync("butil://docs/clipboard");

        using (Assert.Scope())
        {
            Assert.IsNotEmpty(text);
            Assert.DoesNotContain("could not be rendered on the server", text);
            Assert.IsLessThanOrEqualTo(ButilMcp.MaxDocumentLength + 200, text.Length);
        }
    }

    [TestMethod]
    public async Task A_source_resource_hands_back_the_file()
    {
        // The template captures one segment, so a path with slashes in it arrives encoded - which
        // is exactly how the resource's own description tells a client to ask for it.
        var text = await ReadTextAsync($"butil://source/{Uri.EscapeDataString("Demo/Client/Pages/ClipboardPage.razor")}");

        using (Assert.Scope())
        {
            Assert.DoesNotStartWith("No source file at", text);
            Assert.Contains("@page \"/clipboard\"", text);
        }
    }

    [TestMethod]
    public async Task Every_advertised_resource_reads()
    {
        var resources = await Mcp.ListResourcesAsync(cancellationToken: Ct);

        foreach (var resource in resources)
        {
            var text = await ReadTextAsync(resource.Uri);

            Assert.IsNotEmpty(text, $"{resource.Uri} read back as nothing.");
        }
    }

    [TestMethod]
    public async Task An_unknown_resource_argument_is_answered_rather_than_thrown()
    {
        // Same rule as the tools: a near miss is a sentence, not a protocol error.
        var answers = new[]
        {
            await ReadTextAsync("butil://api/Telepathy"),
            await ReadTextAsync("butil://docs/nowhere"),
            await ReadTextAsync($"butil://guide/{Uri.EscapeDataString("Chapter Nine")}"),
            await ReadTextAsync($"butil://source/{Uri.EscapeDataString("nowhere/at/all.cs")}"),
        };

        using (Assert.Scope())
        {
            foreach (var answer in answers)
            {
                Assert.IsNotEmpty(answer);
                Assert.DoesNotContain("Exception", answer);
                Assert.IsLessThan(500, answer.Length, $"A miss answered with {answer.Length} characters: {answer}");
            }
        }
    }

    private async Task<string> ReadTextAsync(string uri)
    {
        var result = await Mcp.ReadResourceAsync(uri, cancellationToken: Ct);

        Assert.IsNotEmpty(result.Contents, $"{uri} returned no contents.");

        return string.Join("\n", result.Contents.OfType<TextResourceContents>().Select(content => content.Text));
    }
}
