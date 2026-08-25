using Microsoft.VisualStudio.TestTools.UnitTesting;
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
[TestClass]
public class PromptTests : McpTestBase
{
    [TestMethod]
    public async Task Server_advertises_exactly_the_expected_prompts()
    {
        var prompts = await Mcp.ListPromptsAsync(cancellationToken: Ct);

        using (Assert.Scope())
        {
            CollectionAssert.AreEquivalent(ButilMcp.Prompts.Keys.ToArray(), prompts.Select(prompt => prompt.Name).ToArray());

            foreach (var prompt in prompts)
            {
                Assert.IsFalse(string.IsNullOrEmpty(prompt.Title), $"{prompt.Name} has no title for a person to pick from a menu.");
                Assert.IsFalse(string.IsNullOrEmpty(prompt.Description), $"{prompt.Name} has no description.");

                string[] arguments = [.. (prompt.ProtocolPrompt.Arguments ?? []).Select(argument => argument.Name)];

                CollectionAssert.AreEquivalent(ButilMcp.Prompts[prompt.Name], arguments, $"{prompt.Name} declares unexpected arguments.");

                foreach (var argument in prompt.ProtocolPrompt.Arguments ?? [])
                {
                    // A prompt argument with no description is a box a person is asked to fill in
                    // with no indication of what belongs in it.
                    Assert.IsFalse(string.IsNullOrEmpty(argument.Description),
                        $"{prompt.Name}.{argument.Name} has no description.");
                }
            }
        }
    }

    [TestMethod]
    public async Task The_setup_prompt_puts_the_argument_into_the_workflow()
    {
        var text = await GetPromptTextAsync("add-butil-to-app", new() { ["hostingModel"] = "web-app" });

        using (Assert.Scope())
        {
            Assert.Contains("web-app", text, "The hosting model the caller passed never reached the prompt.");
            Assert.Contains("GetButilSetupGuide", text);
            Assert.Contains("bit-butil.js", text);
            Assert.Contains("AddBitButilServices()", text);
        }
    }

    [TestMethod]
    public async Task The_setup_prompt_has_a_default_that_still_works()
    {
        // The argument is optional and defaults to "unknown", which is a real branch of the
        // workflow rather than a placeholder: step one is then to determine the hosting model.
        var text = await GetPromptTextAsync("add-butil-to-app", []);

        using (Assert.Scope())
        {
            Assert.Contains("unknown", text);
            Assert.IsTrue(text.Contains("AddInteractiveWebAssemblyComponents", StringComparison.Ordinal)
                          || text.Contains("WebAssemblyHostBuilder", StringComparison.Ordinal));
        }
    }

    [TestMethod]
    public async Task The_feature_prompt_carries_the_request_and_the_order_to_work_in()
    {
        const string feature = "let the user pick a photo and save a cropped copy back to disk";

        var text = await GetPromptTextAsync("implement-butil-feature", new() { ["feature"] = feature });

        using (Assert.Scope())
        {
            Assert.Contains(feature, text);

            // Search first, then plan, then confirm the members - the sequence is the point.
            Assert.IsGreaterThanOrEqualTo(0, text.IndexOf("SearchButil", StringComparison.Ordinal));
            Assert.IsLessThan(text.IndexOf("PlanButilFeature", StringComparison.Ordinal),
                              text.IndexOf("SearchButil", StringComparison.Ordinal));
            Assert.IsLessThan(text.IndexOf("GetButilApiDetails", StringComparison.Ordinal),
                              text.IndexOf("PlanButilFeature", StringComparison.Ordinal));
        }
    }

    [TestMethod]
    public async Task The_interop_prompt_takes_no_arguments_and_still_names_its_tools()
    {
        var text = await GetPromptTextAsync("replace-jsinterop-with-butil", []);

        using (Assert.Scope())
        {
            Assert.Contains("IJSRuntime", text);
            Assert.Contains("SearchButil", text);
            Assert.Contains("GetButilApiDetails", text);
            Assert.Contains("PlanButilFeature", text);
            Assert.Contains("ButilSubscription", text);
        }
    }

    [TestMethod]
    public async Task The_debug_prompt_starts_where_the_answer_usually_is()
    {
        const string symptom = "Clipboard.WriteText returns but nothing is on the clipboard";

        var text = await GetPromptTextAsync("debug-butil-issue", new() { ["symptom"] = symptom });

        using (Assert.Scope())
        {
            Assert.Contains(symptom, text);
            Assert.Contains("troubleshooting", text, "Step one is the troubleshooting page, where the cause is often verbatim.");
            Assert.Contains("PlanButilFeature", text);
            Assert.Contains("prerender", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [TestMethod]
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

        Assert.IsEmpty(failures, string.Join("\n", failures));
    }

    private async Task<string> GetPromptTextAsync(string name, Dictionary<string, object?> arguments)
    {
        var result = await Mcp.GetPromptAsync(name, arguments, cancellationToken: Ct);

        Assert.IsNotEmpty(result.Messages, $"{name} rendered no messages.");
        Assert.AreEqual(Role.User, result.Messages[0].Role, $"{name} should render as something to send, not as an assistant turn.");

        return string.Join("\n", result.Messages.Select(message => message.Content).OfType<TextContentBlock>().Select(block => block.Text));
    }
}
