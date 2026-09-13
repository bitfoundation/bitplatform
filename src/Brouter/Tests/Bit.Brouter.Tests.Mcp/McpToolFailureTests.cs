using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// What happens when a call goes wrong, and what the answers cost when they go right.
/// <para>
/// A documentation server's real failure mode is not throwing - it is answering. An unknown slug, a
/// misspelled type or a renamed heading must come back as a helpful, successful answer naming the
/// values that do exist (those cases are covered with the tools they belong to); a call the protocol
/// itself cannot satisfy must come back as an error rather than as prose an agent would read as
/// documentation. And no answer may be big enough to swallow the context window it lands in.
/// </para>
/// </summary>
[TestClass]
public class McpToolFailureTests
{
    /// <summary>Mirrors McpController.MaxDocumentLength.</summary>
    private const int MaxDocumentLength = 40_000;

    [TestMethod]
    public async Task A_tool_that_does_not_exist_is_a_protocol_error()
    {
        var exception = await Assert.ThrowsExactlyAsync<McpProtocolException>(
            async () => await McpCall.RawAsync("GetBrouterEverything"));

        StringAssert.Contains(exception.Message, "GetBrouterEverything");
    }

    [TestMethod]
    public async Task A_missing_required_argument_fails_the_call_rather_than_answering()
    {
        // Answering GetBrouterSetupGuide without a render mode would mean guessing at the one thing
        // the caller is asking about.
        foreach (var tool in new[] { "GetBrouterSetupGuide", "SearchBrouter", "InspectBrouterRouteTemplates" })
        {
            var result = await McpCall.RawAsync(tool);

            Assert.AreEqual(true, result.IsError, $"'{tool}' answered a call that left out its required argument.");
        }
    }

    [TestMethod]
    public void A_long_answer_is_cut_at_the_cap_and_says_that_it_was()
    {
        // Nothing this server hands out is over the cap any more - the documentation pages are served
        // rendered rather than as their own source, and what is left of the demo's source is code. So
        // the cut is exercised where it lives, which is also the only way to state the rule that no
        // real file happens to test: a cut never falls between the halves of a surrogate pair, since
        // half a pair is not text and a client re-encoding the answer would mangle or reject it.
        var emoji = "\U0001F680";

        var text = new string('x', MaxDocumentLength - 1) + emoji + new string('y', 100);

        var truncated = Bit.Brouter.Demo.Server.Controllers.McpController.Truncate(text);

        StringAssert.Contains(truncated, "[truncated");
        StringAssert.Contains(truncated, "the full text is longer than");

        var kept = truncated[..truncated.IndexOf("\n\n[truncated", StringComparison.Ordinal)];

        Assert.AreEqual(MaxDocumentLength - 1, kept.Length, "The cut fell in the middle of a surrogate pair.");
        Assert.IsFalse(char.IsHighSurrogate(kept[^1]), "The kept text ends on half a character.");

        // What fits is handed over whole, without a notice about a cut that did not happen.
        Assert.AreEqual(text[..100], Bit.Brouter.Demo.Server.Controllers.McpController.Truncate(text[..100]));
    }

    [TestMethod]
    public async Task No_documentation_answer_is_big_enough_to_dominate_a_context_window()
    {
        // Every prose tool an agent calls in the ordinary course of working, at its largest answer.
        (string Tool, Dictionary<string, object?>? Arguments)[] calls =
        [
            ("GetBrouterSetupGuide", new() { ["renderMode"] = "auto" }),
            ("GetBrouterGuideSection", new() { ["heading"] = "Data loader" }),
            ("GetBrouterGuideSection", null),
            ("GetBrouterDocsPage", new() { ["slug"] = "api" }),
            ("GetBrouterDocsPage", null),
            ("GetBrouterApi", null),
            ("GetBrouterApi", new() { ["typeName"] = "IBrouterRoute" }),
            ("GetBrouterRouteConstraints", null),
            ("GetBrouterSourceFile", null),
            ("GetBrouterSourceFile", new() { ["path"] = "Demo/Client/AppRouter.razor" }),
        ];

        foreach (var (tool, arguments) in calls)
        {
            var text = await McpCall.TextAsync(tool, arguments);

            Assert.IsTrue(text.Length <= MaxDocumentLength + 200,
                $"'{tool}' answered with {text.Length} characters, past the {MaxDocumentLength} cap.");
        }
    }

    [TestMethod]
    public async Task Every_tool_answers_something_a_client_can_read()
    {
        // A structured tool still has to put its answer in the content blocks: a client that does not
        // support structured output would otherwise see an empty result.
        foreach (var tool in McpToolSurfaceTests.ExpectedTools)
        {
            var arguments = MinimalArgumentsFor(tool);
            var result = await McpCall.RawAsync(tool, arguments);

            Assert.AreEqual(false, result.IsError is true, $"'{tool}' failed when called with its documented arguments.");

            var text = string.Join('\n', result.Content.OfType<TextContentBlock>().Select(block => block.Text));

            Assert.IsFalse(string.IsNullOrWhiteSpace(text), $"'{tool}' answered with no readable content.");
        }
    }

    /// <summary>The smallest sensible call for a tool - what a client would send first.</summary>
    private static Dictionary<string, object?>? MinimalArgumentsFor(string tool) => tool switch
    {
        "SearchBrouter" => new() { ["query"] = "guard" },
        "GetBrouterSetupGuide" => new() { ["renderMode"] = "server" },
        "GetBrouterGuideSection" => new() { ["heading"] = "Quick start" },
        "GetBrouterApi" => new() { ["typeName"] = "IBrouter" },
        "GetBrouterSourceFile" => new() { ["path"] = "Demo/Client/AppRouter.razor" },
        "InspectBrouterRouteTemplates" => new() { ["templates"] = "/users/{id:int}" },
        _ => null
    };
}
