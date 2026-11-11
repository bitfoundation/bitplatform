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
        _prompts = await _server.Mcp.ListPromptsAsync(cancellationToken: _.CancellationToken);
    }

    [ClassCleanup]
    public static async Task StopAsync() => await _server.DisposeAsync();

    private static async Task<string> TextOfAsync(string name, object? arguments = null)
    {
        var result = await _server.Mcp.GetPromptAsync(name, ToArguments(arguments));

        Assert.IsNotEmpty(result.Messages, name);

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
            var result = await _server.Mcp.GetPromptAsync(prompt.Name, DefaultArgumentsFor(prompt), cancellationToken: TestContext.CancellationToken);

            Assert.IsNotEmpty(result.Messages, prompt.Name);
            Assert.IsTrue(result.Messages.All(message => message.Role == Role.User), prompt.Name);
            var actualValue = result.Messages.All(message => (message.Content as TextContentBlock)?.Text.Length > 200);
            Assert.IsTrue(actualValue, prompt.Name);
        }
    }

    [TestMethod]
    public async Task EveryPrompt_NamesOnlyToolsThatExist()
    {
        var tools = (await _server.Mcp.ListToolsAsync(cancellationToken: TestContext.CancellationToken)).Select(tool => tool.Name).ToArray();

        foreach (var prompt in _prompts)
        {
            var text = await RenderAsync(prompt);

            foreach (var mentioned in System.Text.RegularExpressions.Regex
                         .Matches(text, @"`(?<name>(?:Get|Search|Inspect|Analyze)Bswup\w*)")
                         .Select(match => match.Groups["name"].Value)
                         .Distinct())
            {
                Assert.Contains(mentioned, tools, $"'{prompt.Name}' tells the agent to call '{mentioned}', which is not a tool");
            }
        }
    }

    [TestMethod]
    public async Task AddBswupToApp_OrdersTheWorkAndCoversBothHostingModels()
    {
        var text = await TextOfAsync("add-bswup-to-app", new { hostingModel = "unknown" });

        Assert.Contains("determine it from the project first", text);
        Assert.Contains("GetBswupSetupGuide", text);
        Assert.Contains("externalAssets", text, "the Blazor Web App trap has to be spelled out");
        Assert.Contains("autostart=\"false\"", text);
        Assert.Contains("InspectBswupServiceWorker", text);
        Assert.Contains("service-worker.published.js", text);
    }

    [TestMethod]
    public async Task AddBswupToApp_CarriesTheHostingModelItWasGiven()
    {
        var text = await TextOfAsync("add-bswup-to-app", new { hostingModel = "blazor-web-app" });

        Assert.Contains("blazor-web-app", text);
    }

    [TestMethod]
    public async Task AddBswupToApp_DefaultsToDeterminingTheHostingModel()
    {
        // The argument is optional; a client that offers the prompt with nothing filled in must
        // still get a usable instruction.
        var text = await TextOfAsync("add-bswup-to-app");

        Assert.Contains("unknown", text);
    }

    [TestMethod]
    public async Task ConfigureBswupCaching_StartsFromSearchAndEndsAtTheTwoChecks()
    {
        var text = await TextOfAsync("configure-bswup-caching", new { requirement = "cache the Google Fonts stylesheet too" });

        Assert.Contains("cache the Google Fonts stylesheet too", text, "the requirement is what the prompt is about");
        Assert.Contains("SearchBswup", text);
        Assert.Contains("GetBswupServiceWorkerSettings", text);
        Assert.Contains("InspectBswupServiceWorker", text);
        Assert.Contains("assetUrls", text, "the caching check rides on the same call as the review");
        Assert.Contains("ABOVE the `self.importScripts", text);
    }

    [TestMethod]
    public async Task DebugBswup_LeadsWithTheTwoFailuresThatProduceNoVisibleError()
    {
        var text = await TextOfAsync("debug-bswup", new { symptom = "the app never picks up new versions" });

        Assert.Contains("the app never picks up new versions", text);
        Assert.Contains("troubleshooting", text);
        Assert.Contains("after the `importScripts` line", text);
        Assert.Contains("only one of the", text);
        Assert.Contains("AutoReload=\"false\"", text, "it is behind most 'updates no longer apply themselves' reports - naming the opt-out value is the whole point");
        Assert.Contains("ShowOnUpdate=\"false\"", text, "it is behind most 'no progress is shown while an update downloads' reports");
        Assert.Contains("both default to `true`", text, "the prompt must not send the model looking for a default that changed");
    }

    [TestMethod]
    public async Task RemoveBswup_WarnsAgainstDeletingTheWorkerFiles()
    {
        var text = await TextOfAsync("remove-bswup");

        Assert.Contains("Replace the CONTENT", text);
        Assert.Contains("Deleting the files instead is the classic mistake", text);
        Assert.Contains("forceRefresh", text);
        Assert.Contains("cleanup", text);
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

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
