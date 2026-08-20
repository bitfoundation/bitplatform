using NUnit.Framework;
using Bit.Butil.Tests.Mcp.Infrastructure;

namespace Bit.Butil.Tests.Mcp;

/// <summary>
/// What the tools do with an argument they cannot resolve - which, for a server an agent drives, is
/// most of the interesting behaviour.
/// <para>
/// The rule this server sets itself is that a near miss is worth reading: an unresolvable argument
/// comes back as a normal answer naming the closest candidates and the call that would list them,
/// never as a protocol error and never as an empty result. An error teaches an agent nothing and
/// costs it a turn; a sentence saying "did you mean Clipboard" ends the search. These tests hold
/// the server to that, and to the other half of it - that nothing about this process leaks out
/// while it happens.
/// </para>
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class ToolFailureTests : McpTestBase
{
    [Test]
    public async Task An_unknown_type_is_answered_with_the_nearest_names()
    {
        var result = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName = "Clip" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Details, Is.Null);
            Assert.That(result.Message, Does.Contain("Did you mean"));
            Assert.That(result.Message, Does.Contain("Clipboard"));
        });
    }

    [Test]
    public async Task An_unrecognisable_type_is_pointed_at_the_listing()
    {
        var result = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName = "Telepathy" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Details, Is.Null);
            Assert.That(result.Message, Does.Contain("GetButilApiDetails with no type name"));
            Assert.That(result.Message, Does.Contain("SearchButil"));
        });
    }

    [Test]
    public async Task An_empty_argument_is_the_listing_rather_than_a_failed_lookup()
    {
        // Contains("") matches every type, so a naive "did you mean" here would spend a client's
        // context window listing the entire public surface back at it as a refusal. An empty string
        // is not a miss - it is the same request as omitting the argument, which is the listing.
        var blank = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName = "   " });
        var omitted = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails");

        Assert.Multiple(() =>
        {
            Assert.That(blank.Details, Is.Null);
            Assert.That(blank.Message, Is.Null, "An empty type name is a request for the list, so there is nothing to refuse.");
            Assert.That(blank.Types, Is.Not.Null.And.Not.Empty);

            // The same listing, not merely one of the same size: a blank argument that took some
            // other path through the tool could answer with as many types and the wrong ones.
            Assert.That(blank.Types!.Select(type => type.Name),
                Is.EqualTo(omitted.Types!.Select(type => type.Name)).AsCollection,
                "A blank type name answered with a different list than omitting it.");
        });

        // The same for the three that answer with a document.
        foreach (var (tool, argument) in new[] { ("GetButilDocsPage", "slug"), ("GetButilGuideSection", "heading"), ("GetButilSourceFile", "path") })
        {
            var text = Text(await CallAsync(tool, new Dictionary<string, object?>(StringComparer.Ordinal) { [argument] = "  " }));
            var listing = Text(await CallAsync(tool));

            Assert.That(text, Is.EqualTo(listing),
                $"{tool} read a blank {argument} as something other than a request for its listing.");
        }
    }

    [Test]
    public async Task An_unknown_hosting_model_lists_the_ones_that_exist()
    {
        var text = Text(await CallAsync("GetButilSetupGuide", new { hostingModel = "react-native" }));

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("not a known hosting model"));

            foreach (var model in ButilMcp.HostingModels)
            {
                Assert.That(text, Does.Contain(model), $"The refusal does not offer '{model}' as an alternative.");
            }
        });
    }

    [Test]
    public async Task An_unknown_docs_slug_lists_the_slugs_that_exist()
    {
        var text = Text(await CallAsync("GetButilDocsPage", new { slug = "clipbored" }));

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("No documentation page has the slug 'clipbored'"));
            Assert.That(text, Does.Contain("clipboard"), "The available slugs are listed, and the one that was meant is among them.");
        });
    }

    [Test]
    public async Task An_unknown_guide_heading_lists_the_headings_that_exist()
    {
        var text = Text(await CallAsync("GetButilGuideSection", new { heading = "How to install Node" }));

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("has no section called"));
            Assert.That(text, Does.Contain("Getting started"));
        });
    }

    [Test]
    public async Task A_partial_source_path_is_answered_with_the_paths_it_could_mean()
    {
        var text = Text(await CallAsync("GetButilSourceFile", new { path = "ClipboardPage" }));

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("Did you mean"));
            Assert.That(text, Does.Contain("Demo/Client/Pages/ClipboardPage.razor"));
        });
    }

    [Test]
    public async Task An_unknown_source_path_is_pointed_at_the_listing()
    {
        var text = Text(await CallAsync("GetButilSourceFile", new { path = "somewhere/else.txt" }));

        Assert.That(text, Does.Contain("Call GetButilSourceFile with no path"));
    }

    [Test]
    public async Task Source_paths_cannot_walk_out_of_the_embedded_set()
    {
        // The catalog is a dictionary of embedded resources, not a directory, so there is nothing
        // to traverse into - but this is a public endpoint taking a free-text path, and "nothing to
        // traverse into" is a property worth a test rather than an assumption.
        var attempts = new[]
        {
            "../../../../../../etc/passwd",
            @"..\..\..\..\Windows\win.ini",
            "Demo/../../../appsettings.json",
            "/etc/hosts",
            "C:/Windows/win.ini",
        };

        foreach (var attempt in attempts)
        {
            var text = Text(await CallAsync("GetButilSourceFile", new { path = attempt }));

            Assert.That(text, Does.StartWith("No source file at"), $"'{attempt}' was answered with something other than a miss.");
        }
    }

    [Test]
    public async Task An_unknown_api_name_is_answered_with_candidates()
    {
        var inspection = await InspectAsync("Clipbo");

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsKnown, Is.False);
            Assert.That(inspection.Message, Does.Contain("Clipboard"));
        });
    }

    [Test]
    public async Task An_empty_api_name_says_what_the_argument_wants()
    {
        var inspection = await InspectAsync("   ");

        Assert.Multiple(() =>
        {
            Assert.That(inspection.IsKnown, Is.False);
            Assert.That(inspection.Message, Does.Contain("Clipboard"), "The refusal shows the three shapes of name the argument accepts.");
            Assert.That(inspection.Message, Does.Contain("web-authn"));
        });
    }

    [Test]
    public async Task A_search_that_matches_nothing_says_which_kind_of_nothing()
    {
        // Two different empties, and an agent cannot tell them apart from an empty list: nothing
        // matched, or the query was phrased entirely in words this index drops before matching.
        var unmatched = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "quantum flux capacitor" });

        Assert.Multiple(() =>
        {
            Assert.That(unmatched.Hits, Is.Empty);
            Assert.That(unmatched.Message, Does.Contain("Nothing in Bit.Butil matches"));
            Assert.That(unmatched.Message, Does.Contain("GetButilDocsPage with no"));
        });

        var unsearchable = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "how do I get the browser" });

        Assert.Multiple(() =>
        {
            Assert.That(unsearchable.Hits, Is.Empty);
            Assert.That(unsearchable.Message, Does.Contain("no searchable term"));
        });
    }

    [Test]
    public async Task An_empty_search_is_answered_rather_than_refused()
    {
        var result = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Hits, Is.Empty);
            Assert.That(result.Message, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task A_search_limit_is_clamped_rather_than_honoured_blindly()
    {
        var none = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "storage", limit = 0 });
        var many = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "storage", limit = 10_000 });

        Assert.Multiple(() =>
        {
            Assert.That(none.Hits, Has.Length.EqualTo(1), "A limit of zero is clamped to one rather than answering with nothing.");
            Assert.That(many.Hits.Length, Is.LessThanOrEqualTo(50), "The upper clamp is what stops one query returning the whole index.");
            Assert.That(many.Hits, Is.Not.Empty);
        });
    }

    [Test]
    public async Task A_search_query_that_is_a_whole_file_is_bounded()
    {
        // Every term is counted against every entry, so the work is terms x corpus. A pasted file
        // as a query is the shape of input that turns that into minutes; the tokenizer caps it.
        var query = string.Join(" ", Enumerable.Range(0, 4000).Select(i => $"term{i} clipboard storage"));

        var started = DateTime.UtcNow;
        var result = await CallStructuredAsync<SearchResult>("SearchButil", new { query });
        var elapsed = DateTime.UtcNow - started;

        Assert.Multiple(() =>
        {
            Assert.That(result.Hits, Is.Not.Empty, "The real terms in the query should still match.");
            Assert.That(elapsed, Is.LessThan(TimeSpan.FromSeconds(30)), $"A {query.Length}-character query took {elapsed}.");
        });
    }

    [Test]
    public async Task An_empty_plan_says_what_the_argument_wants_and_still_answers_with_the_rules()
    {
        // This is the only tool here whose argument is genuinely required - an empty one is not a
        // request for a listing, it is a call with nothing to plan - so it is the only one that has
        // to say what a name looks like. An empty Apis array would be a checklist with no reason
        // attached, which reads as an answer and teaches nothing.
        var plan = await CallStructuredAsync<FeaturePlan>("PlanButilFeature", new { apis = "" });

        Assert.Multiple(() =>
        {
            Assert.That(plan.Apis, Has.Length.EqualTo(1));
            Assert.That(plan.Apis[0].IsKnown, Is.False);
            Assert.That(plan.Apis[0].Message, Does.Contain("Clipboard").And.Contain("web-authn"),
                "The refusal shows the three shapes of name the argument accepts.");

            // Registration and prerendering hold for every Butil call, whatever the feature is.
            Assert.That(plan.Checklist, Is.Not.Empty);
            Assert.That(string.Join(" ", plan.Checklist), Does.Contain("AddBitButilServices()"));
        });
    }

    [Test]
    public async Task No_refusal_leaks_anything_about_the_server()
    {
        // A caller here is an arbitrary MCP client. Whatever went wrong belongs in the server's log,
        // where whoever can fix it will look - not in an answer that ends up in a transcript.
        var answers = new List<string>
        {
            Text(await CallRawAsync("GetButilDocsPage", new { slug = "nope" })),
            Text(await CallRawAsync("GetButilSourceFile", new { path = "nope" })),
            Text(await CallRawAsync("GetButilSetupGuide", new { hostingModel = "nope" })),
            Text(await CallRawAsync("GetButilGuideSection", new { heading = "nope" })),
            Text(await CallRawAsync("GetButilApiDetails", new { typeName = "nope" })),
            Text(await CallRawAsync("PlanButilFeature", new { apis = "nope" })),
        };

        Assert.Multiple(() =>
        {
            foreach (var answer in answers)
            {
                Assert.That(answer, Does.Not.Contain("   at "), $"An answer carries a stack frame: {answer}");
                Assert.That(answer, Does.Not.Contain("Exception"), $"An answer names an exception type: {answer}");
                Assert.That(answer, Does.Not.Match(@"[A-Za-z]:\\"), $"An answer carries a filesystem path: {answer}");
                Assert.That(answer, Does.Not.Contain("/home/"), $"An answer carries a filesystem path: {answer}");
            }
        });
    }

    [Test]
    public async Task A_call_to_a_tool_that_does_not_exist_is_an_error_rather_than_a_hang()
    {
        // The one case that SHOULD be a protocol error: the tool itself is not there.
        Assert.That(async () => await Mcp.CallToolAsync("GetButilTeaAndBiscuits", cancellationToken: Ct),
            Throws.Exception);
    }
}
