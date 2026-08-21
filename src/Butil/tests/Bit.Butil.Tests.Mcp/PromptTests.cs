using NUnit.Framework;
using ModelContextProtocol.Protocol;
using Bit.Butil.Tests.Mcp.Infrastructure;

namespace Bit.Butil.Tests.Mcp;

/// <summary>
/// The prompts: ready-made workflows for the four things people actually ask a browser-API wrapper
/// for - add it to an app, build a feature with it, replace hand-written JS interop, and work out
/// why a call that compiles does nothing.
/// <para>
/// A prompt's value is entirely in the order it puts the tool calls in, so that is what is asserted
/// here: that each one still names the tools its workflow depends on, and that the argument a
/// client fills in actually reaches the text. A prompt whose placeholder is dropped renders as a
/// generic instruction that reads fine and answers a different question.
/// </para>
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class PromptTests : McpTestBase
{
    [Test]
    public async Task Server_advertises_exactly_the_expected_prompts()
    {
        var prompts = await Mcp.ListPromptsAsync(cancellationToken: Ct);

        Assert.Multiple(() =>
        {
            Assert.That(prompts.Select(prompt => prompt.Name), Is.EquivalentTo(ButilMcp.Prompts.Keys));

            foreach (var prompt in prompts)
            {
                Assert.That(prompt.Title, Is.Not.Null.And.Not.Empty, $"{prompt.Name} has no title for a person to pick from a menu.");
                Assert.That(prompt.Description, Is.Not.Null.And.Not.Empty, $"{prompt.Name} has no description.");

                string[] arguments = [.. (prompt.ProtocolPrompt.Arguments ?? []).Select(argument => argument.Name)];

                Assert.That(arguments, Is.EquivalentTo(ButilMcp.Prompts[prompt.Name]), $"{prompt.Name} declares unexpected arguments.");

                foreach (var argument in prompt.ProtocolPrompt.Arguments ?? [])
                {
                    // A prompt argument with no description is a box a person is asked to fill in
                    // with no indication of what belongs in it.
                    Assert.That(argument.Description, Is.Not.Null.And.Not.Empty,
                        $"{prompt.Name}.{argument.Name} has no description.");
                }
            }
        });
    }

    [Test]
    public async Task The_setup_prompt_puts_the_argument_into_the_workflow()
    {
        var text = await GetPromptTextAsync("add-butil-to-app", new() { ["hostingModel"] = "web-app" });

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("web-app"), "The hosting model the caller passed never reached the prompt.");
            Assert.That(text, Does.Contain("GetButilSetupGuide"));
            Assert.That(text, Does.Contain("bit-butil.js"));
            Assert.That(text, Does.Contain("AddBitButilServices()"));
        });
    }

    [Test]
    public async Task The_setup_prompt_has_a_default_that_still_works()
    {
        // The argument is optional and defaults to "unknown", which is a real branch of the
        // workflow rather than a placeholder: step one is then to determine the hosting model.
        var text = await GetPromptTextAsync("add-butil-to-app", []);

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("unknown"));
            Assert.That(text, Does.Contain("AddInteractiveWebAssemblyComponents").Or.Contain("WebAssemblyHostBuilder"));
        });
    }

    [Test]
    public async Task The_feature_prompt_carries_the_request_and_the_order_to_work_in()
    {
        const string feature = "let the user pick a photo and save a cropped copy back to disk";

        var text = await GetPromptTextAsync("implement-butil-feature", new() { ["feature"] = feature });

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain(feature));

            // Search first, then plan, then confirm the members - the sequence is the point.
            Assert.That(text.IndexOf("SearchButil", StringComparison.Ordinal), Is.GreaterThanOrEqualTo(0));
            Assert.That(text.IndexOf("SearchButil", StringComparison.Ordinal),
                        Is.LessThan(text.IndexOf("PlanButilFeature", StringComparison.Ordinal)));
            Assert.That(text.IndexOf("PlanButilFeature", StringComparison.Ordinal),
                        Is.LessThan(text.IndexOf("GetButilApiDetails", StringComparison.Ordinal)));
        });
    }

    [Test]
    public async Task The_interop_prompt_takes_no_arguments_and_still_names_its_tools()
    {
        var text = await GetPromptTextAsync("replace-jsinterop-with-butil", []);

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("IJSRuntime"));
            Assert.That(text, Does.Contain("SearchButil"));
            Assert.That(text, Does.Contain("GetButilApiDetails"));
            Assert.That(text, Does.Contain("PlanButilFeature"));
            Assert.That(text, Does.Contain("ButilSubscription"));
        });
    }

    [Test]
    public async Task The_debug_prompt_starts_where_the_answer_usually_is()
    {
        const string symptom = "Clipboard.WriteText returns but nothing is on the clipboard";

        var text = await GetPromptTextAsync("debug-butil-issue", new() { ["symptom"] = symptom });

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain(symptom));
            Assert.That(text, Does.Contain("troubleshooting"), "Step one is the troubleshooting page, where the cause is often verbatim.");
            Assert.That(text, Does.Contain("PlanButilFeature"));
            Assert.That(text, Does.Contain("prerender").IgnoreCase);
        });
    }

    [Test]
    public async Task Every_prompt_renders_and_only_names_tools_that_exist()
    {
        var prompts = await Mcp.ListPromptsAsync(cancellationToken: Ct);
        var advertised = (await Mcp.ListToolsAsync(cancellationToken: Ct)).Select(tool => tool.Name).ToHashSet(StringComparer.Ordinal);

        var failures = new List<string>();

        foreach (var prompt in prompts)
        {
            // Fill every declared argument, so nothing renders as a placeholder.
            var arguments = (prompt.ProtocolPrompt.Arguments ?? [])
                .ToDictionary(argument => argument.Name, object? (_) => "test", StringComparer.Ordinal);

            var text = await GetPromptTextAsync(prompt.Name, arguments);

            if (string.IsNullOrWhiteSpace(text)) failures.Add($"{prompt.Name} rendered to nothing.");

            // Anything spelled like one of this server's tools has to be one of them: a workflow
            // that sends an agent to GetButilApiDetail (singular) wastes a turn on a tool error.
            foreach (var mentioned in System.Text.RegularExpressions.Regex.Matches(text, @"\b(?:GetButil|SearchButil|InspectButil|PlanButil)\w*")
                                                                          .Select(match => match.Value)
                                                                          .Distinct(StringComparer.Ordinal))
            {
                if (advertised.Contains(mentioned) is false) failures.Add($"{prompt.Name} names '{mentioned}', which is not a tool this server advertises.");
            }
        }

        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    private async Task<string> GetPromptTextAsync(string name, Dictionary<string, object?> arguments)
    {
        var result = await Mcp.GetPromptAsync(name, arguments, cancellationToken: Ct);

        Assert.That(result.Messages, Is.Not.Empty, $"{name} rendered no messages.");
        Assert.That(result.Messages[0].Role, Is.EqualTo(Role.User), $"{name} should render as something to send, not as an assistant turn.");

        return string.Join("\n", result.Messages.Select(message => message.Content).OfType<TextContentBlock>().Select(block => block.Text));
    }
}
