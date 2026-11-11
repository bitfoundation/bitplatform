using Bit.Brouter.Demo.Client;
using Bit.Brouter.Demo.Server.Services;
using ModelContextProtocol.Protocol;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// The values a client offers while someone fills in a prompt argument or a resource template's
/// placeholder.
/// <para>
/// Every key this server takes is a key into a closed set that nothing on the wire spells out, so
/// without completions a person picking <c>brouter://docs/{slug}</c> has to go and call a listing
/// tool first just to learn what a slug looks like. The tests check that each set is actually
/// wired - a placeholder answered with an empty list looks exactly like "no matches" in a client -
/// and that the values offered are ones the corresponding tool accepts.
/// </para>
/// </summary>
[TestClass]
public class McpCompletionTests
{
    [TestMethod]
    public async Task Every_resource_placeholder_offers_the_keys_that_exist()
    {
        var slugs = await CompleteResourceAsync("brouter://docs/{slug}", "slug", "");
        Assert.Contains("guards", slugs.Values.ToArray());
        // The overview's real slug is the empty string, which nobody can type; the alias stands in.
        Assert.Contains("overview", slugs.Values.ToArray());
        Assert.DoesNotContain(string.Empty, slugs.Values, "An unpickable empty value is being offered.");

        var headings = await CompleteResourceAsync("brouter://guide/{heading}", "heading", "");
        Assert.Contains("Async guards", headings.Values.ToArray());

        var types = await CompleteResourceAsync("brouter://api/{typeName}", "typeName", "");
        Assert.Contains("BrouterOptions", types.Values.ToArray());

        var paths = await CompleteResourceAsync("brouter://source/{path}", "path", "");
        Assert.Contains("Demo/Client/AppRouter.razor", paths.Values.ToArray());
    }

    [TestMethod]
    public async Task Completion_narrows_as_a_person_types()
    {
        var completion = await CompleteResourceAsync("brouter://api/{typeName}", "typeName", "brouterop");

        Assert.AreSequenceEqual(new[] { "BrouterOptions" }, completion.Values.ToArray());
        Assert.AreEqual(1, completion.Total);
        Assert.AreNotEqual(true, completion.HasMore);
    }

    [TestMethod]
    public async Task What_a_value_starts_with_outranks_what_it_merely_contains()
    {
        // "loader" appears inside three headings; the two that begin with it come first.
        var completion = await CompleteResourceAsync("brouter://guide/{heading}", "heading", "loader");

        Assert.IsGreaterThanOrEqualTo(2, completion.Values.Count);
        Assert.StartsWith("Loader", completion.Values[0]);
        Assert.AreEqual("Data loader", completion.Values[^1], "A substring match should rank below the prefix matches.");
    }

    [TestMethod]
    public async Task A_free_text_prompt_argument_is_offered_the_phrasings_it_was_written_for()
    {
        // A symptom cannot be completed, only started off - and starting in the workflow's own words
        // lands in the one it was designed around.
        var symptoms = await CompletePromptAsync("debug-brouter-routing", "symptom", "");
        Assert.IsGreaterThanOrEqualTo(5, symptoms.Values.Count, "The debugging workflow offers no symptoms to start from.");

        var narrowed = await CompletePromptAsync("debug-brouter-routing", "symptom", "guard");
        Assert.IsTrue(narrowed.Values.All(value => value.Contains("guard", StringComparison.OrdinalIgnoreCase)));

        var features = await CompletePromptAsync("implement-brouter-feature", "feature", "cache");
        Assert.IsNotEmpty(features.Values);
        Assert.Contains("cache", features.Values[0]);
    }

    [TestMethod]
    public async Task The_render_mode_is_completed_from_its_declared_values()
    {
        // Nothing in this server's completion table answers "renderMode": the values are declared with
        // [AllowedValues], and the SDK completes from them. Anything returned here would be merged
        // with those rather than replace them - the same value offered to someone twice.
        var completion = await CompletePromptAsync("add-brouter-to-app", "renderMode", "");

        Assert.AreSequenceEqual(new[] { "unknown", "server", "wasm", "auto", "standalone-wasm", "hybrid" }, completion.Values.ToArray(), Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
        Assert.AreSequenceEqual(completion.Values.Distinct().ToArray(), completion.Values.ToArray(), "A value is being offered twice.");
    }

    [TestMethod]
    public async Task An_argument_with_nothing_to_complete_answers_empty_rather_than_failing()
    {
        var completion = await CompletePromptAsync("add-brouter-to-app", "nosucharg", "");

        Assert.IsEmpty(completion.Values);
    }

    [TestMethod]
    public async Task Every_completed_slug_is_a_slug_the_documentation_tool_accepts()
    {
        // The point of a completion is that the value it hands over works. Every offered slug is put
        // straight back through the tool a client would call next.
        var completion = await CompleteResourceAsync("brouter://docs/{slug}", "slug", "");

        Assert.HasCount(DocsCatalog.AllPages.Count(), completion.Values);

        foreach (var slug in completion.Values)
        {
            var page = await McpCall.TextAsync("GetBrouterDocsPage", new() { ["slug"] = slug });

            Assert.IsFalse(page.Contains("No documentation page has the slug", StringComparison.Ordinal),
                $"'{slug}' is offered as a completion but the docs tool does not accept it.");
        }
    }

    [TestMethod]
    public async Task Every_completed_heading_type_and_path_is_one_its_tool_accepts()
    {
        foreach (var heading in (await CompleteResourceAsync("brouter://guide/{heading}", "heading", "")).Values)
        {
            var section = await McpCall.TextAsync("GetBrouterGuideSection", new() { ["heading"] = heading });

            Assert.IsFalse(section.Contains("has no section called", StringComparison.Ordinal), $"'{heading}' is offered but not accepted.");
        }

        foreach (var typeName in (await CompleteResourceAsync("brouter://api/{typeName}", "typeName", "")).Values)
        {
            var result = await McpCall.TextAsync("GetBrouterApi", new() { ["typeName"] = typeName });

            Assert.IsFalse(result.Contains("has no public type called", StringComparison.Ordinal), $"'{typeName}' is offered but not accepted.");
        }

        foreach (var path in (await CompleteResourceAsync("brouter://source/{path}", "path", "")).Values)
        {
            var file = await McpCall.TextAsync("GetBrouterSourceFile", new() { ["path"] = path });

            Assert.IsFalse(file.StartsWith("No source file at", StringComparison.Ordinal), $"'{path}' is offered but not accepted.");
        }
    }

    [TestMethod]
    public void A_set_bigger_than_the_protocol_allows_is_cut_and_says_so()
    {
        // The protocol caps one response at 100 values; the rest is reported through hasMore rather
        // than quietly dropped. Exercised against the completion table directly, because no set this
        // server has is that big yet - which is precisely why the cap is easy to break unnoticed.
        var completion = BrouterCompletions.Complete("path", string.Empty);

        Assert.IsLessThanOrEqualTo(100, completion.Values.Count);
        Assert.AreEqual(completion.Total > 100, completion.HasMore is true);
    }

    private static async Task<Completion> CompleteResourceAsync(string uriTemplate, string argument, string typed)
    {
        var result = await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = uriTemplate }, argument, typed);

        return result.Completion;
    }

    private static async Task<Completion> CompletePromptAsync(string prompt, string argument, string typed)
    {
        var result = await McpTestHost.Client.CompleteAsync(new PromptReference { Name = prompt }, argument, typed);

        return result.Completion;
    }
}
