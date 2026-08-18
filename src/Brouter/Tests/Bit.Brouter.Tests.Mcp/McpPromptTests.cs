using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// The four ready-made workflows: add the router to an app, build a feature with it, migrate off
/// the built-in one, debug a URL that will not match.
/// <para>
/// A prompt is worth its place only if it puts the tools in an order that catches the expensive
/// mistakes, so the tests hold each one to the calls it is supposed to sequence - and, above all, to
/// naming tools that exist. A workflow telling an agent to call something this server does not have
/// is worse than no workflow at all: it spends the agent's turn on a dead end.
/// </para>
/// </summary>
[TestClass]
public class McpPromptTests
{
    private static readonly string[] _expectedPrompts =
    [
        "add-brouter-to-app",
        "implement-brouter-feature",
        "migrate-to-brouter",
        "debug-brouter-routing",
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

        // By name rather than by position: an argument added in front of these would otherwise fail
        // the test for the wrong reason, or - worse - pass it about the wrong argument.
        var feature = prompts.Single(prompt => prompt.Name == "implement-brouter-feature")
                             .ProtocolPrompt.Arguments!.SingleOrDefault(argument => argument.Name == "feature");
        Assert.IsNotNull(feature, "'implement-brouter-feature' declares no 'feature' argument.");
        Assert.AreEqual(true, feature.Required, "A feature request with no feature in it has nothing to work from.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(feature.Description));

        // The render mode has a documented stand-in, so it is optional rather than required.
        var renderMode = prompts.Single(prompt => prompt.Name == "add-brouter-to-app")
                                .ProtocolPrompt.Arguments!.SingleOrDefault(argument => argument.Name == "renderMode");
        Assert.IsNotNull(renderMode, "'add-brouter-to-app' declares no 'renderMode' argument.");
        Assert.AreNotEqual(true, renderMode.Required);

        var migrate = prompts.Single(prompt => prompt.Name == "migrate-to-brouter").ProtocolPrompt.Arguments;
        Assert.IsTrue(migrate is null || migrate.Count == 0, "The migration workflow takes no arguments.");
    }

    [TestMethod]
    public async Task Every_prompt_produces_one_user_message_that_reads_as_an_instruction()
    {
        (string Name, Dictionary<string, object?>? Arguments)[] calls =
        [
            ("add-brouter-to-app", new() { ["renderMode"] = "wasm" }),
            ("implement-brouter-feature", new() { ["feature"] = "warn before leaving a half-filled form" }),
            ("migrate-to-brouter", null),
            ("debug-brouter-routing", new() { ["symptom"] = "a deep link 404s on refresh" }),
        ];

        foreach (var (name, arguments) in calls)
        {
            var result = await McpTestHost.Client.GetPromptAsync(name, arguments);

            Assert.AreEqual(1, result.Messages.Count, $"'{name}' produced {result.Messages.Count} messages.");
            Assert.AreEqual(Role.User, result.Messages[0].Role, $"'{name}' does not speak as the user.");

            var text = (result.Messages[0].Content as TextContentBlock)?.Text;

            Assert.IsFalse(string.IsNullOrWhiteSpace(text), $"'{name}' produced an empty message.");
            StringAssert.Contains(text, "Work in this order", $"'{name}' does not lay out an order to work in.");
        }
    }

    [TestMethod]
    public async Task Every_prompt_only_names_tools_this_server_has()
    {
        foreach (var name in _expectedPrompts)
        {
            var text = await GetTextAsync(name, ArgumentsFor(name));

            foreach (var mentioned in ToolNames.MentionedIn(text))
            {
                CollectionAssert.Contains(McpToolSurfaceTests.ExpectedTools, mentioned,
                    $"The '{name}' workflow tells the agent to call '{mentioned}', which this server does not expose.");
            }
        }
    }

    [TestMethod]
    public async Task An_argument_reaches_the_prompt_it_was_given_to()
    {
        var text = await GetTextAsync("implement-brouter-feature", new() { ["feature"] = "keep the search page alive across navigations" });

        StringAssert.Contains(text, "keep the search page alive across navigations");
    }

    [TestMethod]
    public async Task A_missing_render_mode_becomes_the_step_that_works_it_out()
    {
        // The workflow is usable before anyone knows the answer: the model is told to determine it.
        var text = await GetTextAsync("add-brouter-to-app", null);

        StringAssert.Contains(text, "unknown");
        StringAssert.Contains(text, "AddInteractiveServerComponents", "Nothing tells the model how to find the render mode out.");
    }

    [TestMethod]
    public async Task The_setup_workflow_leads_with_the_mistake_that_costs_the_most()
    {
        // Registering the services in one of a Web App's two containers fails during prerendering
        // rather than at compile time, which is why the workflow has to say it out loud.
        var text = await GetTextAsync("add-brouter-to-app", new() { ["renderMode"] = "auto" });

        StringAssert.Contains(text, "GetBrouterSetupGuide");
        StringAssert.Contains(text, "DI containers");
        StringAssert.Contains(text, "AnalyzeBrouterRouteTable", "The workflow never checks the route table it just changed.");
    }

    [TestMethod]
    public async Task The_feature_workflow_checks_the_api_before_writing_against_it()
    {
        var text = await GetTextAsync("implement-brouter-feature", new() { ["feature"] = "load data before the page renders" });

        StringAssert.Contains(text, "SearchBrouter");
        StringAssert.Contains(text, "GetBrouterApiDetails");
        StringAssert.Contains(text, "do not infer a parameter from another router library");
    }

    [TestMethod]
    public async Task The_migration_workflow_keeps_the_apps_existing_pages_where_they_are()
    {
        var text = await GetTextAsync("migrate-to-brouter", null);

        StringAssert.Contains(text, "Migrating from the built-in Router");
        StringAssert.Contains(text, "AppAssembly");
        StringAssert.Contains(text, "instead of rewriting templates");
    }

    [TestMethod]
    public async Task The_debugging_workflow_starts_where_the_answer_usually_is()
    {
        var text = await GetTextAsync("debug-brouter-routing", new() { ["symptom"] = "the route matches but its parameter arrives null" });

        StringAssert.Contains(text, "the route matches but its parameter arrives null");
        StringAssert.Contains(text, "faq");
        StringAssert.Contains(text, "InspectBrouterRouteTemplate");
    }

    [TestMethod]
    public async Task A_prompt_that_does_not_exist_is_a_protocol_error()
    {
        await Assert.ThrowsExactlyAsync<McpProtocolException>(
            async () => await McpTestHost.Client.GetPromptAsync("write-my-app"));
    }

    [TestMethod]
    public async Task Every_documented_guide_section_a_prompt_sends_the_agent_to_exists()
    {
        // The workflows hand out headings and slugs verbatim; one that has been renamed since sends
        // the agent to an apology instead of to the material the step depends on.
        var migration = await McpCall.TextAsync("GetBrouterGuideSection", new() { ["heading"] = "Migrating from the built-in Router" });
        Assert.IsFalse(migration.Contains("no section called", StringComparison.Ordinal));

        var faq = await McpCall.TextAsync("GetBrouterDocsPage", new() { ["slug"] = "faq" });
        Assert.IsFalse(faq.Contains("No documentation page", StringComparison.Ordinal));

        var pageMigration = await McpCall.TextAsync("GetBrouterDocsPage", new() { ["slug"] = "migration" });
        Assert.IsFalse(pageMigration.Contains("No documentation page", StringComparison.Ordinal));
    }

    private static Dictionary<string, object?>? ArgumentsFor(string name) => name switch
    {
        "implement-brouter-feature" => new() { ["feature"] = "redirect to the sign-in page and come back afterwards" },
        "debug-brouter-routing" => new() { ["symptom"] = "two routes match the same URL and the wrong one wins" },
        "add-brouter-to-app" => new() { ["renderMode"] = "server" },
        _ => null
    };

    private static async Task<string> GetTextAsync(string name, Dictionary<string, object?>? arguments)
    {
        var result = await McpTestHost.Client.GetPromptAsync(name, arguments);

        return string.Join('\n', result.Messages.Select(message => (message.Content as TextContentBlock)?.Text));
    }
}
