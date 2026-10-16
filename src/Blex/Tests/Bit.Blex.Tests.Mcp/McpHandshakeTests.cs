using Bit.Blex.Demo.Server.Services;

namespace Bit.Blex.Tests.Mcp;

/// <summary>
/// What a client learns before it has called anything: who the server says it is, what it says it can
/// do, and the instructions it puts in front of the model. All three are set in Program.cs, and only a
/// real handshake shows them.
/// </summary>
[TestClass]
public class McpHandshakeTests
{
    [TestMethod]
    public void Server_identifies_itself_as_the_library()
    {
        var info = McpTestHost.Client.ServerInfo;

        Assert.AreEqual("bit-blex", info.Name);
        Assert.AreEqual("Bit.Blex", info.Title);
        Assert.IsFalse(string.IsNullOrWhiteSpace(info.Description));
        Assert.IsNotNull(info.WebsiteUrl);
    }

    [TestMethod]
    public void Server_version_is_the_librarys_version_and_carries_no_build_metadata()
    {
        var version = McpTestHost.Client.ServerInfo.Version;

        Assert.AreEqual(BlexServerInstructions.BlexVersion, version);
        Assert.AreNotEqual("unknown", version, "The Bit.Blex assembly version could not be read.");
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
    public void Instructions_are_the_ones_the_server_declares_and_name_their_build()
    {
        var instructions = McpTestHost.Client.ServerInstructions;

        Assert.AreEqual(BlexServerInstructions.Text, instructions);
        StringAssert.Contains(instructions!, BlexServerInstructions.BlexVersion);
    }

    [TestMethod]
    public void Instructions_stay_short_enough_to_sit_in_every_context_window()
    {
        // They are prepended to the conversation whether or not a tool is ever called, so their cost is
        // paid by every session.
        var instructions = McpTestHost.Client.ServerInstructions!;

        Assert.IsTrue(instructions.Length is > 500 and < 4_500,
            $"The server instructions are {instructions.Length} characters; they are meant to stay a few hundred words.");
    }

    [TestMethod]
    public async Task Instructions_only_name_tools_that_exist()
    {
        var tools = (await McpTestHost.Client.ListToolsAsync()).Select(tool => tool.Name).ToHashSet(StringComparer.Ordinal);

        var mentioned = ToolNames.MentionedIn(McpTestHost.Client.ServerInstructions!).ToArray();

        Assert.IsTrue(mentioned.Length >= 5, "The instructions name almost no tools - the working rules are what they are for.");

        foreach (var name in mentioned)
        {
            Assert.IsTrue(tools.Contains(name), $"The server instructions tell the model to call '{name}', which this server does not expose.");
        }
    }
}
