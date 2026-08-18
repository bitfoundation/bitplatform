using NUnit.Framework;
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
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class ServerContractTests : McpTestBase
{
    [Test]
    public void Server_identifies_itself()
    {
        var info = Mcp.ServerInfo;

        Assert.That(info, Is.Not.Null, "The server must answer initialize with a serverInfo.");

        Assert.Multiple(() =>
        {
            // The name is the identity a client stores in its configuration file - it cannot drift.
            Assert.That(info!.Name, Is.EqualTo("bit-butil"));
            Assert.That(info.Title, Is.EqualTo("Bit.Butil - the browser platform for Blazor"));
            Assert.That(info.WebsiteUrl, Does.StartWith("https://github.com/bitfoundation/bitplatform"));

            // The version is the assembly's own informational version. Its exact value moves with
            // every release, but it has to be a real version rather than the "unknown" fallback -
            // the whole premise of these tools is that they answer for a known build.
            Assert.That(info.Version, Is.Not.Null.And.Not.Empty);
            Assert.That(info.Version, Is.Not.EqualTo("unknown"),
                "The server could not read a version off the Bit.Butil assembly, so it cannot say which build it answers for.");
            Assert.That(info.Version, Does.Match(@"^\d+\.\d+\.\d+"),
                $"serverInfo.version should be the Bit.Butil assembly version, but was '{info.Version}'.");
        });
    }

    [Test]
    public void Server_advertises_the_capabilities_it_implements()
    {
        var capabilities = Mcp.ServerCapabilities;

        Assert.That(capabilities, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(capabilities!.Tools, Is.Not.Null, "The tools are the whole point of this server.");
            Assert.That(capabilities.Resources, Is.Not.Null, "McpResources is registered with WithResourcesFromAssembly.");
            Assert.That(capabilities.Prompts, Is.Not.Null, "McpPrompts is registered with WithPromptsFromAssembly.");

            // Without this, an editor offering "add-butil-to-app" asks the user to type a hosting
            // model with nothing to type it from - see Services/ButilCompletions.cs.
            Assert.That(capabilities.Completions, Is.Not.Null,
                "A completion handler is registered with WithCompleteHandler, so completions must be advertised.");
        });
    }

    [Test]
    public void Instructions_carry_what_a_tool_description_cannot()
    {
        var instructions = Mcp.ServerInstructions;

        Assert.That(instructions, Is.Not.Null.And.Not.Empty,
            "The server sets ServerInstructions; a client that gets none has lost the only text the server writes into the model's context.");

        Assert.Multiple(() =>
        {
            // The entry point. Fourteen tools with no stated order is fourteen guesses.
            Assert.That(instructions, Does.Contain("SearchButil"));

            // The four facts that decide whether code that compiles also runs.
            Assert.That(instructions, Does.Contain("OnAfterRenderAsync"));
            Assert.That(instructions, Does.Contain("AddBitButilServices()"));
            Assert.That(instructions, Does.Contain("bit-butil.js"));
            Assert.That(instructions, Does.Contain("ButilSubscription"));

            // The counts are interpolated from the catalogs rather than written down. A zero here
            // means a catalog came up empty at startup and the instructions are lying about it.
            Assert.That(instructions, Does.Match(@"\b(?!0\b)\d+ injectable\s*\n?\s*services"),
                "The instructions interpolate the number of injectable services; it must not be zero.");
            Assert.That(instructions, Does.Not.Contain("0 documented browser APIs"));
        });
    }

    [Test]
    public void Instructions_stay_within_a_sane_budget()
    {
        var instructions = Mcp.ServerInstructions!;

        // Paid for on every request of every session. There is no hard protocol limit, so this is a
        // budget rather than a rule: it exists so a future edit that pastes the README in here has
        // to be a deliberate decision rather than an accident.
        Assert.That(instructions.Length, Is.LessThan(6000),
            $"The server instructions are {instructions.Length} characters and are re-sent every session.");
    }
}
