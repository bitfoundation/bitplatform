using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol;
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
[TestClass]
public class ToolFailureTests : McpTestBase
{
    [TestMethod]
    public async Task An_unknown_type_is_answered_with_the_nearest_names()
    {
        var result = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName = "Clip" });

        using (Assert.Scope())
        {
            Assert.IsNull(result.Details);
            Assert.Contains("Did you mean", result.Message!);
            Assert.Contains("Clipboard", result.Message!);
        }
    }

    [TestMethod]
    public async Task An_unrecognisable_type_is_pointed_at_the_listing()
    {
        var result = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName = "Telepathy" });

        using (Assert.Scope())
        {
            Assert.IsNull(result.Details);
            Assert.Contains("GetButilApiDetails with no type name", result.Message!);
            Assert.Contains("SearchButil", result.Message!);
        }
    }

    [TestMethod]
    public async Task An_empty_argument_is_the_listing_rather_than_a_failed_lookup()
    {
        // Contains("") matches every type, so a naive "did you mean" here would spend a client's
        // context window listing the entire public surface back at it as a refusal. An empty string
        // is not a miss - it is the same request as omitting the argument, which is the listing.
        var blank = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails", new { typeName = "   " });
        var omitted = await CallStructuredAsync<ApiDetailsResult>("GetButilApiDetails");

        using (Assert.Scope())
        {
            Assert.IsNull(blank.Details);
            Assert.IsNotEmpty(blank.Types ?? []);

            // A listing may explain itself - it says which of the types it names carry a summary -
            // but an empty type name is not a miss, so nothing here may read as a refusal.
            Assert.DoesNotContain("Did you mean", blank.Message ?? string.Empty,
                "An empty type name is a request for the list, so there is nothing to refuse.");
            Assert.DoesNotContain("has no public type", blank.Message ?? string.Empty,
                "An empty type name is a request for the list, so there is nothing to refuse.");

            // The same listing, not merely one of the same size: a blank argument that took some
            // other path through the tool could answer with as many types and the wrong ones.
            Assert.AreSequenceEqual(omitted.Types!.Select(type => type.Name), blank.Types!.Select(type => type.Name),
                "A blank type name answered with a different list than omitting it.");
        }

        // The same for the three that answer with a document.
        foreach (var (tool, argument) in new[] { ("GetButilDocsPage", "slug"), ("GetButilGuideSection", "heading"), ("GetButilSourceFile", "path") })
        {
            var text = Text(await CallAsync(tool, new Dictionary<string, object?>(StringComparer.Ordinal) { [argument] = "  " }));
            var listing = Text(await CallAsync(tool));

            Assert.AreEqual(listing, text,
                $"{tool} read a blank {argument} as something other than a request for its listing.");
        }
    }

    [TestMethod]
    public async Task An_unknown_hosting_model_lists_the_ones_that_exist()
    {
        var text = Text(await CallAsync("GetButilSetupGuide", new { hostingModel = "react-native" }));

        using (Assert.Scope())
        {
            Assert.Contains("not a known hosting model", text);

            foreach (var model in ButilMcp.HostingModels)
            {
                Assert.Contains(model, text, $"The refusal does not offer '{model}' as an alternative.");
            }
        }
    }

    [TestMethod]
    public async Task An_unknown_docs_slug_lists_the_slugs_that_exist()
    {
        var text = Text(await CallAsync("GetButilDocsPage", new { slug = "clipbored" }));

        using (Assert.Scope())
        {
            Assert.Contains("No documentation page has the slug 'clipbored'", text);
            Assert.Contains("clipboard", text, "The available slugs are listed, and the one that was meant is among them.");
        }
    }

    [TestMethod]
    public async Task An_unknown_guide_heading_lists_the_headings_that_exist()
    {
        var text = Text(await CallAsync("GetButilGuideSection", new { heading = "How to install Node" }));

        using (Assert.Scope())
        {
            Assert.Contains("has no section called", text);
            Assert.Contains("Getting started", text);
        }
    }

    [TestMethod]
    public async Task A_partial_source_path_is_answered_with_the_paths_it_could_mean()
    {
        var text = Text(await CallAsync("GetButilSourceFile", new { path = "ClipboardPage" }));

        using (Assert.Scope())
        {
            Assert.Contains("Did you mean", text);
            Assert.Contains("Demo/Client/Pages/ClipboardPage.razor", text);
        }
    }

    [TestMethod]
    public async Task An_unknown_source_path_is_pointed_at_the_listing()
    {
        var text = Text(await CallAsync("GetButilSourceFile", new { path = "somewhere/else.txt" }));

        Assert.Contains("Call GetButilSourceFile with no path", text);
    }

    [TestMethod]
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

            Assert.StartsWith("No source file at", text, $"'{attempt}' was answered with something other than a miss.");
        }
    }

    [TestMethod]
    public async Task An_unknown_api_name_is_answered_with_candidates()
    {
        var inspection = await InspectAsync("Clipbo");

        using (Assert.Scope())
        {
            Assert.IsFalse(inspection.IsKnown);
            Assert.Contains("Clipboard", inspection.Message!);
        }
    }

    [TestMethod]
    public async Task An_empty_api_name_says_what_the_argument_wants()
    {
        var inspection = await InspectAsync("   ");

        using (Assert.Scope())
        {
            Assert.IsFalse(inspection.IsKnown);
            Assert.Contains("Clipboard", inspection.Message!, "The refusal shows the three shapes of name the argument accepts.");
            Assert.Contains("web-authn", inspection.Message!);
        }
    }

    [TestMethod]
    public async Task A_search_that_matches_nothing_says_which_kind_of_nothing()
    {
        // Two different empties, and an agent cannot tell them apart from an empty list: nothing
        // matched, or the query was phrased entirely in words this index drops before matching.
        var unmatched = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "quantum flux capacitor" });

        using (Assert.Scope())
        {
            Assert.IsEmpty(unmatched.Hits);
            Assert.Contains("Nothing in Bit.Butil matches", unmatched.Message!);
            Assert.Contains("GetButilDocsPage with no", unmatched.Message!);
        }

        var unsearchable = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "how do I get the browser" });

        using (Assert.Scope())
        {
            Assert.IsEmpty(unsearchable.Hits);
            Assert.Contains("no searchable term", unsearchable.Message!);
        }
    }

    [TestMethod]
    public async Task An_empty_search_is_answered_rather_than_refused()
    {
        var result = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "" });

        using (Assert.Scope())
        {
            Assert.IsEmpty(result.Hits);
            Assert.IsFalse(string.IsNullOrEmpty(result.Message));
        }
    }

    [TestMethod]
    public async Task A_search_limit_is_clamped_rather_than_honoured_blindly()
    {
        var none = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "storage", limit = 0 });
        var many = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "storage", limit = 10_000 });

        using (Assert.Scope())
        {
            Assert.HasCount(1, none.Hits, "A limit of zero is clamped to one rather than answering with nothing.");
            Assert.IsLessThanOrEqualTo(50, many.Hits.Length, "The upper clamp is what stops one query returning the whole index.");
            Assert.IsNotEmpty(many.Hits);
        }
    }

    [TestMethod]
    public async Task A_search_query_that_is_a_whole_file_is_bounded()
    {
        // Every term is counted against every entry, so the work is terms x corpus. A pasted file
        // as a query is the shape of input that turns that into minutes; the tokenizer caps it.
        var query = string.Join(" ", Enumerable.Range(0, 4000).Select(i => $"term{i} clipboard storage"));

        var started = DateTime.UtcNow;
        var result = await CallStructuredAsync<SearchResult>("SearchButil", new { query });
        var elapsed = DateTime.UtcNow - started;

        using (Assert.Scope())
        {
            Assert.IsNotEmpty(result.Hits, "The real terms in the query should still match.");
            Assert.IsLessThan(TimeSpan.FromSeconds(30), elapsed, $"A {query.Length}-character query took {elapsed}.");
        }
    }

    [TestMethod]
    public async Task An_empty_plan_says_what_the_argument_wants_and_still_answers_with_the_rules()
    {
        // This is the only tool here whose argument is genuinely required - an empty one is not a
        // request for a listing, it is a call with nothing to plan - so it is the only one that has
        // to say what a name looks like. An empty Apis array would be a checklist with no reason
        // attached, which reads as an answer and teaches nothing.
        var plan = await CallStructuredAsync<FeaturePlan>("PlanButilFeature", new { apis = "" });

        using (Assert.Scope())
        {
            Assert.HasCount(1, plan.Apis);
            Assert.IsFalse(plan.Apis[0].IsKnown);
            Assert.Contains("Clipboard", plan.Apis[0].Message!,
                "The refusal shows the three shapes of name the argument accepts.");
            Assert.Contains("web-authn", plan.Apis[0].Message!,
                "The refusal shows the three shapes of name the argument accepts.");

            // Registration and prerendering hold for every Butil call, whatever the feature is.
            Assert.IsNotEmpty(plan.Checklist);
            Assert.Contains("AddBitButilServices()", string.Join(" ", plan.Checklist));
        }
    }

    [TestMethod]
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

        using (Assert.Scope())
        {
            foreach (var answer in answers)
            {
                Assert.DoesNotContain("   at ", answer, $"An answer carries a stack frame: {answer}");
                Assert.DoesNotContain("Exception", answer, $"An answer names an exception type: {answer}");
                Assert.DoesNotMatchRegex(@"[A-Za-z]:\\", answer, $"An answer carries a filesystem path: {answer}");
                Assert.DoesNotContain("/home/", answer, $"An answer carries a filesystem path: {answer}");
            }
        }
    }

    [TestMethod]
    public async Task A_call_to_a_tool_that_does_not_exist_is_an_error_rather_than_a_hang()
    {
        // The one case that SHOULD be a protocol error: the tool itself is not there. It has to arrive as an
        // McpException naming the tool - a bare failure of some other type would tell a client nothing about
        // which call it was that did not land.
        var exception = await Assert.ThrowsAsync<McpException>(
            async () => await Mcp.CallToolAsync("GetButilTeaAndBiscuits", cancellationToken: Ct));

        Assert.Contains("GetButilTeaAndBiscuits", exception.Message,
            $"The error does not say which tool was missing: {exception.Message}");
    }
}
