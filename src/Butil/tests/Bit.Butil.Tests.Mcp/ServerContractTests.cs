using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bit.Butil.Tests.Mcp.Infrastructure;

namespace Bit.Butil.Tests.Mcp;

/// <summary>
/// The handshake: what the server says about itself before it is asked anything, and what a client
/// is therefore allowed to assume for the rest of the session.
/// <para>
/// Everything here is paid for once per session and read by the model every turn after that, so a
/// mistake in it is a mistake that is never re-read and never corrected. That makes it worth
/// asserting on directly rather than through the tools it describes.
/// </para>
/// </summary>
[TestClass]
public class ServerContractTests : McpTestBase
{
    [TestMethod]
    public void Server_identifies_itself()
    {
        var info = Mcp.ServerInfo;

        Assert.IsNotNull(info, "The server must answer initialize with a serverInfo.");

        using (Assert.Scope())
        {
            // The name is the identity a client stores in its configuration file - it cannot drift.
            Assert.AreEqual("bit-butil", info!.Name);
            Assert.AreEqual("Bit.Butil - the browser platform for Blazor", info.Title);
            Assert.StartsWith("https://github.com/bitfoundation/bitplatform", info.WebsiteUrl);

            // The version is the assembly's own informational version. Its exact value moves with
            // every release, but it has to be a real version rather than the "unknown" fallback -
            // the whole premise of these tools is that they answer for a known build.
            Assert.IsFalse(string.IsNullOrEmpty(info.Version));
            Assert.AreNotEqual("unknown", info.Version,
                "The server could not read a version off the Bit.Butil assembly, so it cannot say which build it answers for.");
            Assert.MatchesRegex(@"^\d+\.\d+\.\d+", info.Version,
                $"serverInfo.version should be the Bit.Butil assembly version, but was '{info.Version}'.");
        }
    }

    [TestMethod]
    public void Server_advertises_the_capabilities_it_implements()
    {
        var capabilities = Mcp.ServerCapabilities;

        Assert.IsNotNull(capabilities);

        using (Assert.Scope())
        {
            Assert.IsNotNull(capabilities!.Tools, "The tools are the whole point of this server.");
            Assert.IsNotNull(capabilities.Resources, "McpResources is registered with WithResourcesFromAssembly.");
            Assert.IsNotNull(capabilities.Prompts, "McpPrompts is registered with WithPromptsFromAssembly.");

            // Without this, an editor offering "add-butil-to-app" asks the user to type a hosting
            // model with nothing to type it from - see Services/ButilCompletions.cs.
            Assert.IsNotNull(capabilities.Completions,
                "A completion handler is registered with WithCompleteHandler, so completions must be advertised.");
        }
    }

    [TestMethod]
    public void Instructions_carry_what_a_tool_description_cannot()
    {
        var instructions = Mcp.ServerInstructions;

        Assert.IsFalse(string.IsNullOrEmpty(instructions),
            "The server sets ServerInstructions; a client that gets none has lost the only text the server writes into the model's context.");

        using (Assert.Scope())
        {
            // The entry point. Seven tools with no stated order is seven guesses.
            Assert.Contains("SearchButil", instructions);

            // The four facts that decide whether code that compiles also runs.
            Assert.Contains("OnAfterRenderAsync", instructions);
            Assert.Contains("AddBitButilServices()", instructions);
            Assert.Contains("bit-butil.js", instructions);
            Assert.Contains("ButilSubscription", instructions);

            // The counts are interpolated from the catalogs rather than written down. A zero here
            // means a catalog came up empty at startup and the instructions are lying about it.
            Assert.MatchesRegex(@"\b(?!0\b)\d+ injectable\s*\n?\s*services", instructions,
                "The instructions interpolate the number of injectable services; it must not be zero.");
            Assert.DoesNotContain("0 documented browser APIs", instructions);
        }
    }

    [TestMethod]
    public void Instructions_stay_within_a_sane_budget()
    {
        var instructions = Mcp.ServerInstructions!;

        // Paid for on every request of every session. There is no hard protocol limit, so this is a
        // budget rather than a rule: it exists so a future edit that pastes the README in here has
        // to be a deliberate decision rather than an accident.
        Assert.IsLessThan(6000, instructions.Length,
            $"The server instructions are {instructions.Length} characters and are re-sent every session.");
    }

    [TestMethod]
    public async Task Instructions_are_the_map_of_the_tools()
    {
        // This routing used to be a GetButilOverview tool, which was the one thing on the server a
        // client could never need: the instructions are already in the model's context by the time
        // it would decide to call it. Removing it moved the job here, so here is where a tool that
        // nothing points at now shows up.
        var instructions = Mcp.ServerInstructions!;
        var advertised = (await Mcp.ListToolsAsync(cancellationToken: Ct)).Select(tool => tool.Name).ToArray();

        using (Assert.Scope())
        {
            foreach (var tool in advertised)
            {
                Assert.Contains(tool, instructions,
                    $"The instructions never mention {tool}, so nothing tells an agent when to reach for it.");
            }

            // The other half of the fold: an agent that does not know a listing is one empty call
            // away goes looking for a listing tool, finds none, and guesses an argument instead.
            Assert.Contains("no argument", instructions);
        }
    }
}
