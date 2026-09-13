using Microsoft.VisualStudio.TestTools.UnitTesting;
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
[TestClass]
public class CompletionTests : McpTestBase
{
    [TestMethod]
    public async Task The_hosting_model_argument_completes_to_the_models_that_exist()
    {
        var completion = await CompleteAsync(new PromptReference { Name = "add-butil-to-app" }, "hostingModel", "");

        using (Assert.Scope())
        {
            CollectionAssert.AreEquivalent(new[] { "wasm", "web-app", "server", "hybrid", "unknown" }, completion.Values.ToArray());
            Assert.AreEqual(5, completion.Total);
            Assert.AreNotEqual(true, completion.HasMore);
        }
    }

    [TestMethod]
    public async Task Typing_narrows_the_hosting_models()
    {
        var completion = await CompleteAsync(new PromptReference { Name = "add-butil-to-app" }, "hostingModel", "w");

        using (Assert.Scope())
        {
            // "unknown" contains a w, so it is offered too - but after the two that start with one.
            Assert.AreSequenceEqual(new[] { "wasm", "web-app", "unknown" }, completion.Values);
            Assert.DoesNotContain("server", completion.Values);
            Assert.DoesNotContain("hybrid", completion.Values);
        }
    }

    [TestMethod]
    public async Task A_prose_argument_offers_nothing_rather_than_a_menu_of_prose()
    {
        var completion = await CompleteAsync(new PromptReference { Name = "implement-butil-feature" }, "feature", "");

        Assert.IsEmpty(completion.Values, "Offering a menu of possible feature descriptions would be worse than silence.");
    }

    [TestMethod]
    public async Task Docs_slugs_complete_from_the_same_list_the_tool_serves()
    {
        var completion = await CompleteAsync(new ResourceTemplateReference { Uri = "butil://docs/{slug}" }, "slug", "");

        var slugs = (await DocsIndexAsync()).Select(page => page.Slug).ToArray();

        using (Assert.Scope())
        {
            // The protocol caps one response at 100 values, so Total - not the length of Values - is
            // what has to agree with the listing. Every value offered still has to come from it.
            Assert.AreEqual(slugs.Length, completion.Total);
            Assert.IsNotEmpty(completion.Values);
            CollectionAssert.IsSubsetOf(completion.Values.ToArray(), slugs,
                "The completion list and the docs listing have drifted apart, so a completed slug may not resolve.");
        }
    }

    [TestMethod]
    public async Task Type_names_complete_from_the_same_list_the_tool_serves()
    {
        var completion = await CompleteAsync(new ResourceTemplateReference { Uri = "butil://api/{typeName}" }, "typeName", "Clip");

        using (Assert.Scope())
        {
            Assert.Contains("Clipboard", completion.Values);
            Assert.IsTrue(completion.Values.All(value => value.Contains("Clip", StringComparison.OrdinalIgnoreCase)));
        }
    }

    [TestMethod]
    public async Task Prefix_matches_come_before_mere_containment()
    {
        // Someone typing "storage" wants StorageManager before "Local & Session Storage", and both
        // before neither.
        var completion = await CompleteAsync(new ResourceTemplateReference { Uri = "butil://api/{typeName}" }, "typeName", "Storage");

        Assert.IsNotEmpty(completion.Values);

        var firstNonPrefix = completion.Values.ToList().FindIndex(value => value.StartsWith("Storage", StringComparison.OrdinalIgnoreCase) is false);
        var lastPrefix = completion.Values.ToList().FindLastIndex(value => value.StartsWith("Storage", StringComparison.OrdinalIgnoreCase));

        if (firstNonPrefix >= 0)
        {
            Assert.IsLessThan(firstNonPrefix, lastPrefix,
                $"Prefix matches are not grouped first: {string.Join(", ", completion.Values)}");
        }
    }

    [TestMethod]
    public async Task Guide_headings_complete_from_the_same_list_the_tool_serves()
    {
        var completion = await CompleteAsync(new ResourceTemplateReference { Uri = "butil://guide/{heading}" }, "heading", "");

        var headings = await ListAsync("GetButilGuideSection");

        using (Assert.Scope())
        {
            Assert.AreEqual(headings.Length, completion.Total);
            Assert.IsNotEmpty(completion.Values);
            CollectionAssert.IsSubsetOf(completion.Values.ToArray(), headings,
                "The completion list and the guide's own section list have drifted apart.");
        }
    }

    [TestMethod]
    public async Task Source_paths_complete_and_are_capped_honestly()
    {
        var completion = await CompleteAsync(new ResourceTemplateReference { Uri = "butil://source/{path}" }, "path", "");

        var files = await ListAsync("GetButilSourceFile");

        using (Assert.Scope())
        {
            // The protocol caps one response at 100 values. Total and HasMore are what tell a client
            // to keep typing rather than that the list simply ends here.
            Assert.IsLessThanOrEqualTo(100, completion.Values.Count);
            Assert.AreEqual(files.Length, completion.Total);
            Assert.AreEqual(files.Length > 100, completion.HasMore);
        }
    }

    [TestMethod]
    public async Task Completed_values_are_values_the_server_can_then_resolve()
    {
        // The point of the whole handler: what it offers has to work when it is used.
        var slugs = await CompleteAsync(new ResourceTemplateReference { Uri = "butil://docs/{slug}" }, "slug", "c");

        foreach (var slug in slugs.Values.Take(5))
        {
            var text = Text(await CallAsync("GetButilDocsPage", new { slug }));

            Assert.DoesNotStartWith("No documentation page has the slug", text,
                $"The completion offered '{slug}', which the docs tool then could not resolve.");
        }

        var types = await CompleteAsync(new ResourceTemplateReference { Uri = "butil://api/{typeName}" }, "typeName", "But");

        foreach (var typeName in types.Values.Take(5))
        {
            var details = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName });

            Assert.IsNotNull(details.Details, $"The completion offered '{typeName}', which the API tool then could not resolve.");
        }
    }

    [TestMethod]
    public async Task An_argument_with_no_closed_set_completes_to_nothing_rather_than_failing()
    {
        var unknownTemplate = await CompleteAsync(new ResourceTemplateReference { Uri = "butil://nothing/{here}" }, "here", "");
        var unknownArgument = await CompleteAsync(new ResourceTemplateReference { Uri = "butil://docs/{slug}" }, "notAnArgument", "");
        var unknownPrompt = await CompleteAsync(new PromptReference { Name = "no-such-prompt" }, "whatever", "");

        using (Assert.Scope())
        {
            Assert.IsEmpty(unknownTemplate.Values);
            Assert.IsEmpty(unknownArgument.Values);
            Assert.IsEmpty(unknownPrompt.Values);
        }
    }

    private async Task<Completion> CompleteAsync(Reference reference, string argumentName, string argumentValue)
    {
        var result = await Mcp.CompleteAsync(reference, argumentName, argumentValue, cancellationToken: Ct);

        Assert.IsNotNull(result.Completion);

        return result.Completion;
    }
}
