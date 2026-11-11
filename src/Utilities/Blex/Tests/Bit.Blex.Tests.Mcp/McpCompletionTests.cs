using Bit.Blex.Demo.Server.Services;
using ModelContextProtocol.Protocol;

namespace Bit.Blex.Tests.Mcp;

/// <summary>
/// The values a client offers while someone fills in a prompt argument or a resource placeholder - each
/// set actually wired, and every value offered one its tool accepts.
/// </summary>
[TestClass]
public class McpCompletionTests
{
    [TestMethod]
    public async Task Every_resource_placeholder_offers_the_keys_that_exist()
    {
        Assert.Contains("effects", (await CompleteResourceAsync("blex://docs/{slug}", "slug", "")).Values.ToArray());
        Assert.Contains("Persistence", (await CompleteResourceAsync("blex://guide/{heading}", "heading", "")).Values.ToArray());
        Assert.Contains("BlexOptions", (await CompleteResourceAsync("blex://api/{typeName}", "typeName", "")).Values.ToArray());
        Assert.Contains("BLEX001", (await CompleteResourceAsync("blex://diagnostics/{code}", "code", "")).Values.ToArray());
        Assert.Contains("Demo/Client/Stores/CartStore.cs", (await CompleteResourceAsync("blex://source/{+path}", "path", "")).Values.ToArray());
    }

    [TestMethod]
    public async Task Completion_narrows_as_a_person_types_and_prefixes_rank_first()
    {
        var completion = await CompleteResourceAsync("blex://api/{typeName}", "typeName", "blexoptio");

        Assert.AreSequenceEqual(new[] { "BlexOptions" }, completion.Values.ToArray());

        var history = await CompleteResourceAsync("blex://api/{typeName}", "typeName", "history");
        Assert.Contains("History", history.Values[0]);
    }

    [TestMethod]
    public async Task A_free_text_prompt_argument_is_offered_the_phrasings_it_was_written_for()
    {
        Assert.IsGreaterThanOrEqualTo(5, (await CompletePromptAsync("debug-blex-state", "symptom", "")).Values.Count);
        Assert.IsTrue((await CompletePromptAsync("implement-blex-feature", "feature", "undo")).Values.All(v => v.Contains("undo", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task The_hosting_model_is_completed_from_its_declared_values_once()
    {
        var completion = await CompletePromptAsync("add-blex-to-app", "hostingModel", "");

        Assert.AreSequenceEqual(new[] { "unknown" }.Concat(BlexSetupGuide.HostingModels).ToArray(), completion.Values.ToArray(), global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
    }

    [TestMethod]
    public async Task Every_completed_key_is_one_its_tool_accepts()
    {
        foreach (var slug in (await CompleteResourceAsync("blex://docs/{slug}", "slug", "")).Values)
        {
            Assert.IsFalse((await McpCall.TextAsync("GetBlexDocsPage", new() { ["slug"] = slug })).StartsWith("No documentation page", StringComparison.Ordinal), slug);
        }

        foreach (var code in (await CompleteResourceAsync("blex://diagnostics/{code}", "code", "")).Values)
        {
            Assert.StartsWith($"# {code}", await McpCall.TextAsync("GetBlexDiagnostics", new() { ["code"] = code }));
        }

        foreach (var typeName in (await CompleteResourceAsync("blex://api/{typeName}", "typeName", "")).Values)
        {
            Assert.IsFalse((await McpCall.TextAsync("GetBlexApi", new() { ["typeName"] = typeName })).Contains("has no public type called", StringComparison.Ordinal), typeName);
        }
    }

    [TestMethod]
    public void A_set_bigger_than_the_protocol_allows_is_cut_and_says_so()
    {
        var completion = BlexCompletions.Complete("typeName", string.Empty);

        Assert.IsLessThanOrEqualTo(100, completion.Values.Count);
        Assert.AreEqual(completion.Total > 100, completion.HasMore is true);
    }

    private static async Task<Completion> CompleteResourceAsync(string uriTemplate, string argument, string typed)
        => (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = uriTemplate }, argument, typed)).Completion;

    private static async Task<Completion> CompletePromptAsync(string prompt, string argument, string typed)
        => (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = prompt }, argument, typed)).Completion;
}
