using Bit.Bswup.Tests.Mcp.TestInfra;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Bit.Bswup.Tests.Mcp.Protocol;

/// <summary>
/// The four ready-made workflows. A prompt's value is not its prose but the ORDER it puts the
/// tools in - the failure mode of an agent with seventeen tools is not ignorance, it is calling
/// them in a sequence that skips the check which would have caught the bug - so what is asserted
/// here is that each one still names the tools it depends on and still carries the rule that makes
/// the sequence worth following.
/// </summary>
[TestClass]
public class PromptTests
{
    private static McpTestServer _server = null!;
    private static IList<McpClientPrompt> _prompts = null!;

    [ClassInitialize]
    public static async Task StartAsync(TestContext _)
    {
        _server = await McpTestServer.StartAsync();
        _prompts = await _server.Mcp.ListPromptsAsync();
    }

    [ClassCleanup]
    public static async Task StopAsync() => await _server.DisposeAsync();

    private static async Task<string> TextOfAsync(string name, object? arguments = null)
    {
        var result = await _server.Mcp.GetPromptAsync(name, ToArguments(arguments));

        Assert.IsTrue(result.Messages.Count > 0, name);

        return string.Join("\n", result.Messages.Select(message => (message.Content as TextContentBlock)?.Text));
    }

    private static Dictionary<string, object?> ToArguments(object? arguments)
    {
        if (arguments is null) return [];

        return arguments.GetType()
                        .GetProperties()
                        .ToDictionary(property => property.Name, property => property.GetValue(arguments));
    }

    [TestMethod]
    public void EveryPrompt_HasATitleAndADescription()
    {
        foreach (var prompt in _prompts)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(prompt.ProtocolPrompt.Title), prompt.Name);
            Assert.IsFalse(string.IsNullOrWhiteSpace(prompt.Description), prompt.Name);
        }
    }

    [TestMethod]
    public void PromptArguments_AreDescribedSoAClientCanAskForThem()
    {
        foreach (var prompt in _prompts)
        {
            foreach (var argument in prompt.ProtocolPrompt.Arguments ?? [])
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(argument.Description), $"{prompt.Name}.{argument.Name}");
            }
        }
    }

    [TestMethod]
    public async Task EveryPrompt_ProducesAUserMessage()
    {
        foreach (var prompt in _prompts)
        {
            var result = await _server.Mcp.GetPromptAsync(prompt.Name, DefaultArgumentsFor(prompt));

            Assert.IsTrue(result.Messages.Count > 0, prompt.Name);
            Assert.IsTrue(result.Messages.All(message => message.Role == Role.User), prompt.Name);
            Assert.IsTrue(result.Messages.All(message => (message.Content as TextContentBlock)?.Text.Length > 200), prompt.Name);
        }
    }

    [TestMethod]
    public async Task EveryPrompt_NamesOnlyToolsThatExist()
    {
        var tools = (await _server.Mcp.ListToolsAsync()).Select(tool => tool.Name).ToArray();

        foreach (var prompt in _prompts)
        {
            var text = await RenderAsync(prompt);

            foreach (var mentioned in System.Text.RegularExpressions.Regex
                         .Matches(text, @"`(?<name>(?:Get|Search|Inspect|Analyze)Bswup\w*)")
                         .Select(match => match.Groups["name"].Value)
                         .Distinct())
            {
                CollectionAssert.Contains(tools, mentioned, $"'{prompt.Name}' tells the agent to call '{mentioned}', which is not a tool");
            }
        }
    }

    [TestMethod]
    public async Task AddBswupToApp_OrdersTheWorkAndCoversBothHostingModels()
    {
        var text = await TextOfAsync("add-bswup-to-app", new { hostingModel = "unknown" });

        StringAssert.Contains(text, "determine it from the project first");
        StringAssert.Contains(text, "GetBswupSetupGuide");
        StringAssert.Contains(text, "externalAssets", "the Blazor Web App trap has to be spelled out");
        StringAssert.Contains(text, "autostart=\"false\"");
        StringAssert.Contains(text, "InspectBswupServiceWorker");
        StringAssert.Contains(text, "service-worker.published.js");
    }

    [TestMethod]
    public async Task AddBswupToApp_CarriesTheHostingModelItWasGiven()
    {
        var text = await TextOfAsync("add-bswup-to-app", new { hostingModel = "blazor-web-app" });

        StringAssert.Contains(text, "blazor-web-app");
    }

    [TestMethod]
    public async Task AddBswupToApp_DefaultsToDeterminingTheHostingModel()
    {
        // The argument is optional; a client that offers the prompt with nothing filled in must
        // still get a usable instruction.
        var text = await TextOfAsync("add-bswup-to-app");

        StringAssert.Contains(text, "unknown");
    }

    [TestMethod]
    public async Task ConfigureBswupCaching_StartsFromSearchAndEndsAtTheTwoChecks()
    {
        var text = await TextOfAsync("configure-bswup-caching", new { requirement = "cache the Google Fonts stylesheet too" });

        StringAssert.Contains(text, "cache the Google Fonts stylesheet too", "the requirement is what the prompt is about");
        StringAssert.Contains(text, "SearchBswup");
        StringAssert.Contains(text, "GetBswupServiceWorkerSettings");
        StringAssert.Contains(text, "InspectBswupServiceWorker");
        StringAssert.Contains(text, "assetUrls", "the caching check rides on the same call as the review");
        StringAssert.Contains(text, "ABOVE the `self.importScripts");
    }

    [TestMethod]
    public async Task DebugBswup_LeadsWithTheTwoFailuresThatProduceNoVisibleError()
    {
        var text = await TextOfAsync("debug-bswup", new { symptom = "the app never picks up new versions" });

        StringAssert.Contains(text, "the app never picks up new versions");
        StringAssert.Contains(text, "troubleshooting");
        StringAssert.Contains(text, "after the `importScripts` line");
        StringAssert.Contains(text, "only one of the");
        StringAssert.Contains(text, "AutoReload", "the changed default is behind most 'updates no longer apply themselves' reports");
    }

    [TestMethod]
    public async Task RemoveBswup_WarnsAgainstDeletingTheWorkerFiles()
    {
        var text = await TextOfAsync("remove-bswup");

        StringAssert.Contains(text, "Replace the CONTENT");
        StringAssert.Contains(text, "Deleting the files instead is the classic mistake");
        StringAssert.Contains(text, "forceRefresh");
        StringAssert.Contains(text, "cleanup");
    }

    private static Dictionary<string, object?> DefaultArgumentsFor(McpClientPrompt prompt)
    {
        return (prompt.ProtocolPrompt.Arguments ?? [])
            .ToDictionary(argument => argument.Name, object? (_) => "anything");
    }

    private static async Task<string> RenderAsync(McpClientPrompt prompt)
    {
        var result = await _server.Mcp.GetPromptAsync(prompt.Name, DefaultArgumentsFor(prompt));

        return string.Join("\n", result.Messages.Select(message => (message.Content as TextContentBlock)?.Text));
    }
}
