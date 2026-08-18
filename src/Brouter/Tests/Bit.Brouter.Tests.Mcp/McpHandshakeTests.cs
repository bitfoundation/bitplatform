using Bit.Brouter.Demo.Server.Services;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// What a client learns before it has called anything: who the server says it is, what it says it
/// can do, and the instructions it puts in front of the model. All three are set in Program.cs, and
/// all three are invisible from inside the app - only a real handshake shows them.
/// </summary>
[TestClass]
public class McpHandshakeTests
{
    [TestMethod]
    public void Server_identifies_itself_as_the_library()
    {
        var info = McpTestHost.Client.ServerInfo;

        Assert.AreEqual("bit-brouter", info.Name);
        Assert.AreEqual("Bit.Brouter", info.Title);
        Assert.IsFalse(string.IsNullOrWhiteSpace(info.Description));
        Assert.IsNotNull(info.WebsiteUrl);
    }

    [TestMethod]
    public void Server_version_is_the_librarys_version_and_carries_no_build_metadata()
    {
        var version = McpTestHost.Client.ServerInfo.Version;

        Assert.AreEqual(BrouterServerInstructions.BrouterVersion, version);
        Assert.AreNotEqual("unknown", version, "The Bit.Brouter assembly version could not be read.");
        Assert.IsFalse(version!.Contains('+'), $"Build metadata leaked into the reported version: '{version}'.");
    }

    [TestMethod]
    public void Server_advertises_tools_resources_prompts_and_completions()
    {
        var capabilities = McpTestHost.Client.ServerCapabilities;

        Assert.IsNotNull(capabilities.Tools);
        Assert.IsNotNull(capabilities.Resources, "WithResourcesFromAssembly is not reaching the wire.");
        Assert.IsNotNull(capabilities.Prompts, "WithPromptsFromAssembly is not reaching the wire.");
        Assert.IsNotNull(capabilities.Completions, "WithCompleteHandler is not reaching the wire - argument completion is silently off.");
    }

    [TestMethod]
    public void Instructions_are_the_ones_the_server_declares()
    {
        var instructions = McpTestHost.Client.ServerInstructions;

        Assert.IsNotNull(instructions);
        Assert.AreEqual(BrouterServerInstructions.Text, instructions);
    }

    [TestMethod]
    public void Instructions_name_the_build_they_answer_from()
    {
        StringAssert.Contains(McpTestHost.Client.ServerInstructions!, BrouterServerInstructions.BrouterVersion);
    }

    [TestMethod]
    public void Instructions_stay_short_enough_to_sit_in_every_context_window()
    {
        // They are prepended to the conversation whether or not a tool is ever called, so their cost
        // is paid by every session. A few hundred words is the budget; a runaway edit is the risk.
        var instructions = McpTestHost.Client.ServerInstructions!;

        Assert.IsTrue(instructions.Length is > 500 and < 4_000,
            $"The server instructions are {instructions.Length} characters; they are meant to stay around 2,000.");
    }

    [TestMethod]
    public async Task Instructions_only_name_tools_that_exist()
    {
        // The instructions tell the model which tool to reach for. A tool renamed here without being
        // renamed there sends it looking for something the server does not have.
        var tools = (await McpTestHost.Client.ListToolsAsync()).Select(tool => tool.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var mentioned in ToolNames.MentionedIn(McpTestHost.Client.ServerInstructions!))
        {
            Assert.IsTrue(tools.Contains(mentioned),
                $"The server instructions tell the model to call '{mentioned}', which this server does not expose.");
        }
    }
}
