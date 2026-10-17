using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Bit.Blex.Tests.Mcp;

/// <summary>
/// The four ready-made workflows. A prompt is worth its place only if it puts the tools in an order that
/// catches the expensive mistakes - and above all if it names tools that exist.
/// </summary>
[TestClass]
public class McpPromptTests
{
    private static readonly string[] _expectedPrompts =
    [
        "add-blex-to-app",
        "implement-blex-feature",
        "migrate-to-blex",
        "debug-blex-state",
    ];

    [TestMethod]
    public async Task Every_expected_prompt_is_published_with_a_title_and_a_description()
    {
        var prompts = await McpTestHost.Client.ListPromptsAsync();

        CollectionAssert.AreEquivalent(_expectedPrompts, prompts.Select(prompt => prompt.Name).ToArray());

        foreach (var prompt in prompts)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(prompt.Title), $"'{prompt.Name}' has no title.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(prompt.Description), $"'{prompt.Name}' has no description.");
        }
    }

    [TestMethod]
    public async Task A_prompts_arguments_are_declared_with_what_they_are_for()
    {
        var prompts = await McpTestHost.Client.ListPromptsAsync();

        var feature = prompts.Single(prompt => prompt.Name == "implement-blex-feature")
                             .ProtocolPrompt.Arguments!.SingleOrDefault(argument => argument.Name == "feature");
        Assert.IsNotNull(feature);
        Assert.AreEqual(true, feature.Required, "A feature request with no feature in it has nothing to work from.");

        var hostingModel = prompts.Single(prompt => prompt.Name == "add-blex-to-app")
                                  .ProtocolPrompt.Arguments!.SingleOrDefault(argument => argument.Name == "hostingModel");
        Assert.IsNotNull(hostingModel);
        Assert.AreNotEqual(true, hostingModel.Required, "The hosting model has a documented stand-in, so it is optional.");

        var migrate = prompts.Single(prompt => prompt.Name == "migrate-to-blex").ProtocolPrompt.Arguments;
        Assert.IsTrue(migrate is null || migrate.Count == 0, "The migration workflow takes no arguments.");
    }

    [TestMethod]
    public async Task Every_prompt_produces_one_user_message_that_lays_out_an_order()
    {
        foreach (var name in _expectedPrompts)
        {
            var result = await McpTestHost.Client.GetPromptAsync(name, ArgumentsFor(name));

            Assert.AreEqual(1, result.Messages.Count, $"'{name}' produced {result.Messages.Count} messages.");
            Assert.AreEqual(Role.User, result.Messages[0].Role, $"'{name}' does not speak as the user.");
            StringAssert.Contains((result.Messages[0].Content as TextContentBlock)?.Text, "Work in this order", $"'{name}' does not lay out an order to work in.");
        }
    }

    [TestMethod]
    public async Task Every_prompt_only_names_tools_this_server_has()
    {
        foreach (var name in _expectedPrompts)
        {
            foreach (var mentioned in ToolNames.MentionedIn(await GetTextAsync(name, ArgumentsFor(name))))
            {
                CollectionAssert.Contains(McpToolSurfaceTests.ExpectedTools, mentioned,
                    $"The '{name}' workflow tells the agent to call '{mentioned}', which this server does not expose.");
            }
        }
    }

    [TestMethod]
    public async Task Every_workflow_that_writes_a_store_checks_it_with_the_generator()
    {
        foreach (var name in new[] { "add-blex-to-app", "implement-blex-feature", "migrate-to-blex", "debug-blex-state" })
        {
            StringAssert.Contains(await GetTextAsync(name, ArgumentsFor(name)), "InspectBlexStore",
                $"The '{name}' workflow never puts the store it writes to the generator.");
        }
    }

    [TestMethod]
    public async Task An_argument_reaches_the_prompt_it_was_given_to()
    {
        StringAssert.Contains(await GetTextAsync("implement-blex-feature", new() { ["feature"] = "a cart that survives a reload" }), "a cart that survives a reload");
        StringAssert.Contains(await GetTextAsync("debug-blex-state", new() { ["symptom"] = "Redux DevTools shows nothing" }), "Redux DevTools shows nothing");
    }

    [TestMethod]
    public async Task A_missing_hosting_model_becomes_the_step_that_works_it_out()
    {
        var text = await GetTextAsync("add-blex-to-app", null);

        StringAssert.Contains(text, "unknown");
        StringAssert.Contains(text, "MauiProgram.cs");
        StringAssert.Contains(text, "AddInteractiveServerComponents");
    }

    [TestMethod]
    public async Task Every_guide_section_and_page_a_prompt_sends_the_agent_to_exists()
    {
        Assert.IsFalse((await McpCall.TextAsync("GetBlexGuideSection", new() { ["heading"] = "Why Blex" })).Contains("no section called", StringComparison.Ordinal));

        foreach (var slug in new[] { "middleware", "persistence", "testing", "stores" })
        {
            Assert.IsFalse((await McpCall.TextAsync("GetBlexDocsPage", new() { ["slug"] = slug })).StartsWith("No documentation page", StringComparison.Ordinal), slug);
        }
    }

    [TestMethod]
    public async Task A_prompt_that_does_not_exist_is_a_protocol_error()
    {
        await Assert.ThrowsExactlyAsync<McpProtocolException>(async () => await McpTestHost.Client.GetPromptAsync("write-my-app"));
    }

    private static Dictionary<string, object?>? ArgumentsFor(string name) => name switch
    {
        "implement-blex-feature" => new() { ["feature"] = "search-as-you-type that cancels the previous request" },
        "debug-blex-state" => new() { ["symptom"] = "persisted state is lost after a reload" },
        "add-blex-to-app" => new() { ["hostingModel"] = "wasm" },
        _ => null
    };

    private static async Task<string> GetTextAsync(string name, Dictionary<string, object?>? arguments)
    {
        var result = await McpTestHost.Client.GetPromptAsync(name, arguments);

        return string.Join('\n', result.Messages.Select(message => (message.Content as TextContentBlock)?.Text));
    }
}
