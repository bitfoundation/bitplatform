using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bit.BlazorUI.Tests.Mcp.Infrastructure;

namespace Bit.BlazorUI.Tests.Mcp;

/// <summary>
/// What the server does when it is asked for something it cannot resolve, when several clients ask
/// at once, and when the first caller arrives before the catalogs are built.
/// <para>
/// A tool that throws is a dead end: the agent sees a protocol error and has nothing to try next.
/// Every tool here answers a miss with prose that names the nearest candidates and the call that
/// would list them, which is the difference between a near miss worth reading and a retry made
/// blind.
/// </para>
/// </summary>
[TestClass]
public class ResilienceTests : McpTestBase
{
    [TestMethod]
    public void The_very_first_search_after_startup_is_a_real_answer()
    {
        if (McpServerFixture.ColdSearch is null) return; // Pointed at a server this suite did not start.

        // The catalogs are built in the background from startup and nothing waits for them. A first
        // caller who arrives mid-build has to get the same answer as the hundredth, not an empty
        // index.
        StringAssert.Contains(McpServerFixture.ColdSearch, "BitDatePicker",
            "The first search this process answered did not find the date picker, so a cold client sees an index that is not there yet.");
    }

    [DataTestMethod]
    [DataRow("GetBitBlazorUIComponent", "name", "BitDataGrd", "BitDataGrid")]
    [DataRow("GetBitBlazorUIComponentExamples", "name", "BitTogle", "BitToggle")]
    [DataRow("GetBitBlazorUIType", "typeName", "BitColour", "BitColor")]
    public async Task A_near_miss_is_answered_with_the_name_that_was_meant(string tool, string argument, string typed, string expected)
    {
        var result = await CallRawAsync(tool, new Dictionary<string, object?> { [argument] = typed });

        var text = Text(result);

        using var scope = Assert.Scope();

        Assert.AreNotEqual(true, result.IsError, $"{tool} threw on an unresolvable argument instead of answering.");
        StringAssert.Contains(text, "Did you mean", $"{tool} answered '{typed}' with no suggestion at all.");
        StringAssert.Contains(text, expected, $"{tool} did not suggest {expected} for '{typed}'.");
    }

    [DataTestMethod]
    [DataRow("GetBitBlazorUISetupGuide", "hostingModel", "blazor")]
    [DataRow("GetBitBlazorUIThemingGuide", "section", "Nonsense")]
    [DataRow("FindBitBlazorUIIcons", "query", "zzzzzzzz")]
    public async Task An_argument_from_a_closed_set_is_answered_with_the_set(string tool, string argument, string typed)
    {
        var result = await CallRawAsync(tool, new Dictionary<string, object?> { [argument] = typed });

        var text = Text(result);

        using var scope = Assert.Scope();

        Assert.AreNotEqual(true, result.IsError, $"{tool} threw on an unresolvable argument instead of answering.");
        Assert.IsGreaterThan(60, text.Length, $"{tool} answered '{typed}' with too little to act on.");
    }

    [TestMethod]
    public async Task An_empty_argument_is_a_request_for_the_listing_rather_than_a_failed_lookup()
    {
        using var scope = Assert.Scope();

        foreach (var tool in new[] { "GetBitBlazorUIComponent", "GetBitBlazorUIType", "GetBitBlazorUIThemingGuide" })
        {
            var answer = await CallAsync(tool);

            Assert.IsGreaterThan(1_000, answer.Length, $"{tool} with no argument did not answer with a listing.");
            Assert.DoesNotContain("Did you mean", answer, $"{tool} treated no argument as a failed lookup.");
        }
    }

    [TestMethod]
    public async Task Concurrent_callers_get_the_same_answer()
    {
        // The catalogs are built lazily and shared. Two callers racing the build must not be able to
        // see two different libraries.
        var answers = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
            await CallAsync("GetBitBlazorUIComponent", new { name = "BitDropdown" })));

        Assert.AreEqual(1, answers.Distinct(StringComparer.Ordinal).Count(), "Concurrent callers got different answers.");
    }

    [TestMethod]
    public async Task Every_tool_is_reachable_as_a_plain_HTTP_GET()
    {
        using var scope = Assert.Scope();

        // The GET mirror is how a person checks what a tool answers without an MCP client, and it is
        // the same method the protocol calls - so a tool that is unreachable here has been renamed
        // out from under the documentation that points at it.
        foreach (var (tool, query) in new[]
        {
            ("SearchBitBlazorUI", "?query=button"),
            ("GetBitBlazorUIComponent", "?name=BitButton"),
            ("GetBitBlazorUIComponentExamples", "?name=BitButton&example=Basic"),
            ("GetBitBlazorUIType", "?typeName=BitColor"),
            ("GetBitBlazorUISetupGuide", "?hostingModel=wasm"),
            ("GetBitBlazorUIThemingGuide", "?section=Presets"),
            ("FindBitBlazorUIIcons", "?query=save")
        })
        {
            using var response = await McpServerFixture.Http.GetAsync(McpServerFixture.Url($"api/mcp/{tool}{query}"));

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"api/mcp/{tool} answered {(int)response.StatusCode}.");

            var body = await response.Content.ReadAsStringAsync();

            Assert.IsGreaterThan(50, body.Length, $"api/mcp/{tool} answered with almost nothing.");
        }
    }
}
