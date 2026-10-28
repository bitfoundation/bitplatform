using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Core;

[TestClass]
public class BitAiToolTests
{
    private sealed record WeatherArgs(string City, int Days);

    [TestMethod]
    public void SchemaIsDerivedFromTheArgumentType()
    {
        var schema = BitAiTool.CreateSchema<WeatherArgs>();

        Assert.AreEqual("object", schema.GetProperty("type").GetString());
        var properties = schema.GetProperty("properties");
        Assert.IsTrue(properties.TryGetProperty("City", out _) || properties.TryGetProperty("city", out _));
    }

    [TestMethod]
    public async Task ATypedToolDeserializesItsArgumentsAndSerializesItsResult()
    {
        var tool = BitAiTool.Create<WeatherArgs, object>(
            "get_weather",
            "Look up the weather.",
            (args, _) => ValueTask.FromResult<object>(new { summary = $"{args.City} for {args.Days} days" }));

        var arguments = JsonDocument.Parse("""{"city":"Oslo","days":2}""").RootElement;
        var outcome = await tool.InvokeAsync(new BitAiToolInvocation("c1", "get_weather", arguments));

        Assert.IsTrue(outcome.IsSuccess);
        Assert.AreEqual("Oslo for 2 days", outcome.Value!.Value.GetProperty("summary").GetString());
    }

    [TestMethod]
    public void ToolNamesResolveCaseInsensitively()
    {
        var registry = new BitAiToolRegistry();
        registry.Register(BitAiTool.Create(new BitAiToolDefinition("Search"),
            (_, _) => ValueTask.FromResult(BitAiToolOutcome.FromText("ok"))));

        Assert.IsNotNull(registry.Find("search"));
        Assert.IsNotNull(registry.Find("SEARCH"));
        Assert.IsNull(registry.Find("other"));
    }

    [TestMethod]
    public async Task TheDefaultPolicyApprovesDestructiveToolsAndHonoursAnExplicitChoice()
    {
        var policy = new BitAiDefaultToolApprovalPolicy();
        var invocation = new BitAiToolInvocation("c1", "t", default);

        Assert.IsFalse(await policy.RequiresApprovalAsync(new BitAiToolDefinition("read"), invocation));
        Assert.IsTrue(await policy.RequiresApprovalAsync(
            new BitAiToolDefinition("wipe") { Risk = BitAiToolRisk.Destructive }, invocation));
        Assert.IsTrue(await policy.RequiresApprovalAsync(
            new BitAiToolDefinition("send") { RequiresApproval = true }, invocation));
        Assert.IsFalse(await policy.RequiresApprovalAsync(
            new BitAiToolDefinition("wipe") { Risk = BitAiToolRisk.Destructive, RequiresApproval = false }, invocation));
    }

    [TestMethod]
    public async Task AlwaysApprovingStopsTheQuestionBeingAskedAgain()
    {
        var policy = new BitAiDefaultToolApprovalPolicy();
        var definition = new BitAiToolDefinition("wipe") { Risk = BitAiToolRisk.Destructive };
        var invocation = new BitAiToolInvocation("c1", "wipe", default);

        Assert.IsTrue(await policy.RequiresApprovalAsync(definition, invocation));
        policy.RememberAlwaysApproved("wipe");
        Assert.IsFalse(await policy.RequiresApprovalAsync(definition, invocation));
    }

    [TestMethod]
    public void OutcomeDisplayTextFallsBackThroughTextThenJson()
    {
        Assert.AreEqual("shown", BitAiToolOutcome.FromText("shown").GetDisplayText());
        Assert.AreEqual("boom", BitAiToolOutcome.Failure("boom").GetDisplayText());
        Assert.AreEqual("""{"a":1}""", BitAiToolOutcome.Success(new { a = 1 }).GetDisplayText());
    }

    // The default encoder of SerializeToElement escapes an apostrophe, the characters HTML is sensitive to and
    // everything outside ASCII; the text shown for a value keeps only the escapes JSON itself needs.
    [TestMethod]
    public void OutcomeDisplayTextShowsTheJsonOfAValueAsItWasWritten()
    {
        var value = JsonDocument.Parse("""
            {"filter":"status = \u0027archived\u0027 \u003Ca\u0026b\u002Bc\u003E","city":"\u062A\u0647\u0631\u0627\u0646","path":"C:\\logs","count":3}
            """).RootElement;

        Assert.AreEqual("""{"filter":"status = 'archived' <a&b+c>","city":"تهران","path":"C:\\logs","count":3}""",
                        BitAiToolOutcome.Success(value).GetDisplayText());
    }
}
