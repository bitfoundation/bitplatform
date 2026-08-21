using NUnit.Framework;
using ModelContextProtocol.Protocol;
using Bit.Butil.Tests.Mcp.Infrastructure;

namespace Bit.Butil.Tests.Mcp;

/// <summary>
/// completion/complete: the values that are valid for one argument of a prompt or of a resource
/// template, filtered by what has been typed so far.
/// <para>
/// Every one of those arguments is drawn from a closed set the server already holds, and without
/// this handler a person picking "add Butil to an app" out of a menu in their editor is asked to
/// type a hosting model with nothing to type it from. The sets come from the same catalogs the
/// tools answer from, so the assertions worth making are that they are the same sets - a completion
/// list that has drifted from the tools offers values that then do not resolve.
/// </para>
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class CompletionTests : McpTestBase
{
    [Test]
    public async Task The_hosting_model_argument_completes_to_the_models_that_exist()
    {
        var completion = await CompleteAsync(new PromptReference { Name = "add-butil-to-app" }, "hostingModel", "");

        Assert.Multiple(() =>
        {
            Assert.That(completion.Values, Is.EquivalentTo(new[] { "wasm", "web-app", "server", "hybrid", "unknown" }));
            Assert.That(completion.Total, Is.EqualTo(5));
            Assert.That(completion.HasMore, Is.Not.True);
        });
    }

    [Test]
    public async Task Typing_narrows_the_hosting_models()
    {
        var completion = await CompleteAsync(new PromptReference { Name = "add-butil-to-app" }, "hostingModel", "w");

        Assert.Multiple(() =>
        {
            // "unknown" contains a w, so it is offered too - but after the two that start with one.
            Assert.That(completion.Values, Is.EqualTo(new[] { "wasm", "web-app", "unknown" }).AsCollection);
            Assert.That(completion.Values, Does.Not.Contain("server"));
            Assert.That(completion.Values, Does.Not.Contain("hybrid"));
        });
    }

    [Test]
    public async Task A_prose_argument_offers_nothing_rather_than_a_menu_of_prose()
    {
        var completion = await CompleteAsync(new PromptReference { Name = "implement-butil-feature" }, "feature", "");

        Assert.That(completion.Values, Is.Empty, "Offering a menu of possible feature descriptions would be worse than silence.");
    }

    [Test]
    public async Task Docs_slugs_complete_from_the_same_list_the_tool_serves()
    {
        var completion = await CompleteAsync(new ResourceTemplateReference { Uri = "butil://docs/{slug}" }, "slug", "");

        var slugs = (await DocsIndexAsync()).Select(page => page.Slug).ToArray();

        Assert.Multiple(() =>
        {
            // The protocol caps one response at 100 values, so Total - not the length of Values - is
            // what has to agree with the listing. Every value offered still has to come from it.
            Assert.That(completion.Total, Is.EqualTo(slugs.Length));
            Assert.That(completion.Values, Is.Not.Empty);
            Assert.That(completion.Values, Is.SubsetOf(slugs),
                "The completion list and the docs listing have drifted apart, so a completed slug may not resolve.");
        });
    }

    [Test]
    public async Task Type_names_complete_from_the_same_list_the_tool_serves()
    {
        var completion = await CompleteAsync(new ResourceTemplateReference { Uri = "butil://api/{typeName}" }, "typeName", "Clip");

        Assert.Multiple(() =>
        {
            Assert.That(completion.Values, Does.Contain("Clipboard"));
            Assert.That(completion.Values.All(value => value.Contains("Clip", StringComparison.OrdinalIgnoreCase)), Is.True);
        });
    }

    [Test]
    public async Task Prefix_matches_come_before_mere_containment()
    {
        // Someone typing "storage" wants StorageManager before "Local & Session Storage", and both
        // before neither.
        var completion = await CompleteAsync(new ResourceTemplateReference { Uri = "butil://api/{typeName}" }, "typeName", "Storage");

        Assert.That(completion.Values, Is.Not.Empty);

        var firstNonPrefix = completion.Values.ToList().FindIndex(value => value.StartsWith("Storage", StringComparison.OrdinalIgnoreCase) is false);
        var lastPrefix = completion.Values.ToList().FindLastIndex(value => value.StartsWith("Storage", StringComparison.OrdinalIgnoreCase));

        if (firstNonPrefix >= 0)
        {
            Assert.That(lastPrefix, Is.LessThan(firstNonPrefix),
                $"Prefix matches are not grouped first: {string.Join(", ", completion.Values)}");
        }
    }

    [Test]
    public async Task Guide_headings_complete_from_the_same_list_the_tool_serves()
    {
        var completion = await CompleteAsync(new ResourceTemplateReference { Uri = "butil://guide/{heading}" }, "heading", "");

        var headings = await ListAsync("GetButilGuideSection");

        Assert.Multiple(() =>
        {
            Assert.That(completion.Total, Is.EqualTo(headings.Length));
            Assert.That(completion.Values, Is.Not.Empty);
            Assert.That(completion.Values, Is.SubsetOf(headings),
                "The completion list and the guide's own section list have drifted apart.");
        });
    }

    [Test]
    public async Task Source_paths_complete_and_are_capped_honestly()
    {
        var completion = await CompleteAsync(new ResourceTemplateReference { Uri = "butil://source/{path}" }, "path", "");

        var files = await ListAsync("GetButilSourceFile");

        Assert.Multiple(() =>
        {
            // The protocol caps one response at 100 values. Total and HasMore are what tell a client
            // to keep typing rather than that the list simply ends here.
            Assert.That(completion.Values.Count, Is.LessThanOrEqualTo(100));
            Assert.That(completion.Total, Is.EqualTo(files.Length));
            Assert.That(completion.HasMore, Is.EqualTo(files.Length > 100));
        });
    }

    [Test]
    public async Task Completed_values_are_values_the_server_can_then_resolve()
    {
        // The point of the whole handler: what it offers has to work when it is used.
        var slugs = await CompleteAsync(new ResourceTemplateReference { Uri = "butil://docs/{slug}" }, "slug", "c");

        foreach (var slug in slugs.Values.Take(5))
        {
            var text = Text(await CallAsync("GetButilDocsPage", new { slug }));

            Assert.That(text, Does.Not.StartWith("No documentation page has the slug"),
                $"The completion offered '{slug}', which the docs tool then could not resolve.");
        }

        var types = await CompleteAsync(new ResourceTemplateReference { Uri = "butil://api/{typeName}" }, "typeName", "But");

        foreach (var typeName in types.Values.Take(5))
        {
            var details = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName });

            Assert.That(details.Details, Is.Not.Null, $"The completion offered '{typeName}', which the API tool then could not resolve.");
        }
    }

    [Test]
    public async Task An_argument_with_no_closed_set_completes_to_nothing_rather_than_failing()
    {
        var unknownTemplate = await CompleteAsync(new ResourceTemplateReference { Uri = "butil://nothing/{here}" }, "here", "");
        var unknownArgument = await CompleteAsync(new ResourceTemplateReference { Uri = "butil://docs/{slug}" }, "notAnArgument", "");
        var unknownPrompt = await CompleteAsync(new PromptReference { Name = "no-such-prompt" }, "whatever", "");

        Assert.Multiple(() =>
        {
            Assert.That(unknownTemplate.Values, Is.Empty);
            Assert.That(unknownArgument.Values, Is.Empty);
            Assert.That(unknownPrompt.Values, Is.Empty);
        });
    }

    private async Task<Completion> CompleteAsync(Reference reference, string argumentName, string argumentValue)
    {
        var result = await Mcp.CompleteAsync(reference, argumentName, argumentValue, cancellationToken: Ct);

        Assert.That(result.Completion, Is.Not.Null);

        return result.Completion;
    }
}
