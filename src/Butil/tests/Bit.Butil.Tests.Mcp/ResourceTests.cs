using NUnit.Framework;
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
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class ResourceTests : McpTestBase
{
    [Test]
    public async Task Server_advertises_exactly_the_expected_resources()
    {
        var resources = await Mcp.ListResourcesAsync(cancellationToken: Ct);

        var advertised = resources.ToDictionary(resource => resource.Name, resource => resource.Uri, StringComparer.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(advertised.Keys, Is.EquivalentTo(ButilMcp.Resources.Keys));

            foreach (var (name, uri) in ButilMcp.Resources)
            {
                // TryGetValue rather than the indexer: a resource that is gone should fail the
                // assertion above and this one, not throw out of the whole Multiple block.
                Assert.That(advertised.TryGetValue(name, out var advertisedUri), Is.True, $"{name} is not advertised at all.");
                Assert.That(advertisedUri, Is.EqualTo(uri));
            }

            foreach (var resource in resources)
            {
                // The name is the identifier a client stores and a completion returns; the title is
                // the line a person reads in a picker. Both are required, and they are not the same
                // thing - see the class comment on McpResources.
                Assert.That(resource.Title, Is.Not.Null.And.Not.Empty, $"{resource.Name} has no title.");
                Assert.That(resource.Description, Is.Not.Null.And.Not.Empty, $"{resource.Name} has no description.");
                Assert.That(resource.MimeType, Is.Not.Null.And.Not.Empty, $"{resource.Name} declares no MIME type.");
            }
        });
    }

    [Test]
    public async Task Server_advertises_exactly_the_expected_resource_templates()
    {
        var templates = await Mcp.ListResourceTemplatesAsync(cancellationToken: Ct);

        var advertised = templates.ToDictionary(template => template.Name, template => template.UriTemplate, StringComparer.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(advertised.Keys, Is.EquivalentTo(ButilMcp.ResourceTemplates.Keys));

            foreach (var (name, expected) in ButilMcp.ResourceTemplates)
            {
                Assert.That(advertised.TryGetValue(name, out var advertisedTemplate), Is.True, $"{name} is not advertised at all.");
                Assert.That(advertisedTemplate, Is.EqualTo(expected.UriTemplate));
            }

            foreach (var template in templates)
            {
                Assert.That(template.Title, Is.Not.Null.And.Not.Empty, $"{template.Name} has no title.");
                Assert.That(template.Description, Is.Not.Null.And.Not.Empty, $"{template.Name} has no description.");
            }
        });
    }

    [Test]
    public async Task The_guide_resource_is_the_whole_readme()
    {
        var text = await ReadTextAsync("butil://guide");

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.Not.Empty, "An empty guide means the README was not embedded into the published app.");
            Assert.That(text, Does.Contain("Bit.Butil"));
            Assert.That(text, Does.Contain("## Getting started"));
            Assert.That(text, Does.Contain("AddBitButilServices"));
        });
    }

    [Test]
    public async Task The_api_resource_separates_what_you_inject_from_what_you_construct()
    {
        var text = await ReadTextAsync("butil://api");

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.StartWith("# Bit.Butil public API"));
            Assert.That(text, Does.Contain("## Injectable services"));
            Assert.That(text, Does.Contain("## Everything else"));
            Assert.That(text, Does.Contain("**Clipboard**"));

            // The injectable half is listed before the other one, which is the whole reason the
            // resource splits them: it is the first question a reader has about a type.
            Assert.That(text.IndexOf("## Injectable services", StringComparison.Ordinal),
                        Is.LessThan(text.IndexOf("## Everything else", StringComparison.Ordinal)));
        });
    }

    [Test]
    public async Task The_support_resource_is_a_readable_table()
    {
        var text = await ReadTextAsync("butil://support");

        Assert.Multiple(() =>
        {
            // The matrix and the page index are one table now: the same rows answer "which engines
            // run this" and "where is it written up", so the resource is the index.
            Assert.That(text, Does.StartWith("# Bit.Butil documentation pages"));
            Assert.That(text, Does.Contain("| Slug | Title | Summary | Services | Engines | Requires |"));

            var rows = DocsIndexRow.ParseAll(text);

            Assert.That(rows.Length, Is.GreaterThan(40), "The table should carry a row per documented API.");
            Assert.That(rows.Select(row => row.Slug), Does.Contain("clipboard"));

            // The table is a map: only the name of each precondition, with the sentence explaining
            // it one PlanButilFeature call away.
            Assert.That(text, Does.Contain("Secure context"));
            Assert.That(text, Does.Not.Contain("only available over HTTPS or on localhost"));
        });
    }

    [Test]
    public async Task A_guide_section_resource_answers_the_same_text_as_the_tool()
    {
        var viaResource = await ReadTextAsync($"butil://guide/{Uri.EscapeDataString("Getting started")}");
        var viaTool = Text(await CallAsync("GetButilGuideSection", new { heading = "Getting started" }));

        Assert.That(viaResource, Is.EqualTo(viaTool),
            "The resource and the tool read the same catalog; answering differently means one of them has drifted.");
    }

    [Test]
    public async Task A_type_resource_renders_the_same_reference_the_tool_returns()
    {
        var text = await ReadTextAsync("butil://api/Clipboard");

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.StartWith("# Clipboard (Service)"));
            Assert.That(text, Does.Contain("@inject Bit.Butil.Clipboard clipboard"));
            Assert.That(text, Does.Contain("## Method"));
            Assert.That(text, Does.Contain("**WriteText**"));
            Assert.That(text, Does.Contain("Documentation page: /clipboard"));
        });
    }

    [Test]
    public async Task A_docs_page_resource_renders_the_page()
    {
        var text = await ReadTextAsync("butil://docs/clipboard");

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.Not.Empty);
            Assert.That(text, Does.Not.Contain("could not be rendered on the server"));
            Assert.That(text.Length, Is.LessThanOrEqualTo(ButilMcp.MaxDocumentLength + 200));
        });
    }

    [Test]
    public async Task A_source_resource_hands_back_the_file()
    {
        // The template captures one segment, so a path with slashes in it arrives encoded - which
        // is exactly how the resource's own description tells a client to ask for it.
        var text = await ReadTextAsync($"butil://source/{Uri.EscapeDataString("Demo/Client/Pages/ClipboardPage.razor")}");

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Not.StartWith("No source file at"));
            Assert.That(text, Does.Contain("@page \"/clipboard\""));
        });
    }

    [Test]
    public async Task Every_advertised_resource_reads()
    {
        var resources = await Mcp.ListResourcesAsync(cancellationToken: Ct);

        foreach (var resource in resources)
        {
            var text = await ReadTextAsync(resource.Uri);

            Assert.That(text, Is.Not.Empty, $"{resource.Uri} read back as nothing.");
        }
    }

    [Test]
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

        Assert.Multiple(() =>
        {
            foreach (var answer in answers)
            {
                Assert.That(answer, Is.Not.Empty);
                Assert.That(answer, Does.Not.Contain("Exception"));
                Assert.That(answer.Length, Is.LessThan(500), $"A miss answered with {answer.Length} characters: {answer}");
            }
        });
    }

    private async Task<string> ReadTextAsync(string uri)
    {
        var result = await Mcp.ReadResourceAsync(uri, cancellationToken: Ct);

        Assert.That(result.Contents, Is.Not.Empty, $"{uri} returned no contents.");

        return string.Join("\n", result.Contents.OfType<TextResourceContents>().Select(content => content.Text));
    }
}
