using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Protocol;
using Bit.BlazorUI.Tests.Mcp.Infrastructure;

namespace Bit.BlazorUI.Tests.Mcp;

/// <summary>
/// The half of the server a person reaches rather than a model: the prompts an editor lists as
/// slash commands, the resources it lets someone attach to a conversation, and the completions that
/// make either usable without knowing what to type.
/// </summary>
[TestClass]
public class PromptAndResourceTests : McpTestBase
{
    private static readonly string[] _prompts =
    [
        "add-bit-blazorui-to-app",
        "build-bit-blazorui-screen",
        "migrate-to-bit-blazorui",
        "theme-bit-blazorui-app",
        "debug-bit-blazorui-issue"
    ];

    [TestMethod]
    public async Task Publishes_the_five_prompts_with_titles_and_descriptions()
    {
        var prompts = await Mcp.ListPromptsAsync(cancellationToken: Ct);

        using var scope = Assert.Scope();

        CollectionAssert.AreEquivalent(_prompts, prompts.Select(p => p.Name).ToArray());

        foreach (var prompt in prompts)
        {
            Assert.IsNotEmpty(prompt.ProtocolPrompt.Title ?? string.Empty, $"{prompt.Name} has no title.");
            Assert.IsGreaterThan(40, (prompt.Description ?? string.Empty).Length, $"{prompt.Name} has no usable description.");
        }
    }

    [TestMethod]
    public async Task Every_prompt_names_the_tools_it_wants_called_and_the_order()
    {
        using var scope = Assert.Scope();

        foreach (var name in _prompts)
        {
            // Every argument gets a value: three of the five are prose a caller has to supply, and a
            // prompt whose subject is missing is not the thing this test is about.
            var arguments = (await Mcp.ListPromptsAsync(cancellationToken: Ct))
                .First(p => p.Name == name).ProtocolPrompt.Arguments?
                .ToDictionary(a => a.Name, a => (object?)"a searchable list of products")
                ?? [];

            var result = await Mcp.GetPromptAsync(name, arguments, cancellationToken: Ct);

            var text = string.Join("\n", result.Messages.SelectMany(m => new[] { m.Content }).OfType<TextContentBlock>().Select(b => b.Text));

            Assert.IsGreaterThan(300, text.Length, $"The '{name}' prompt is too short to be a workflow.");

            Assert.IsTrue(ToolNames.Any(tool => text.Contains(tool, StringComparison.Ordinal)),
                $"The '{name}' prompt names none of this server's tools, so it cannot be telling an agent what order to call them in.");

            // The standing rules live in the server's instructions, which the client has had in
            // context since initialize. A prompt that repeated them would be paying twice.
            Assert.DoesNotContain("Six things hold", text);
        }
    }

    [TestMethod]
    public async Task A_prompt_argument_is_interpolated_into_what_it_produces()
    {
        var result = await Mcp.GetPromptAsync("build-bit-blazorui-screen",
            new Dictionary<string, object?> { ["screen"] = "a product list with filters" }, cancellationToken: Ct);

        var text = string.Join("\n", result.Messages.Select(m => m.Content).OfType<TextContentBlock>().Select(b => b.Text));

        StringAssert.Contains(text, "a product list with filters");
    }

    [TestMethod]
    public async Task Resources_and_templates_read_the_same_catalogs_the_tools_do()
    {
        var resources = await Mcp.ListResourcesAsync(cancellationToken: Ct);
        var templates = await Mcp.ListResourceTemplatesAsync(cancellationToken: Ct);

        using var scope = Assert.Scope();

        CollectionAssert.AreEquivalent(new[] { "bitblazorui://components", "bitblazorui://theming" },
            resources.Select(r => r.Uri).ToArray());

        CollectionAssert.AreEquivalent(new[]
        {
            "bitblazorui://components/{name}",
            "bitblazorui://components/{name}/examples",
            "bitblazorui://types/{typeName}",
            "bitblazorui://setup/{hostingModel}",
            "bitblazorui://theming/{section}"
        }, templates.Select(t => t.UriTemplate).ToArray());

        foreach (var resource in resources)
        {
            Assert.IsNotEmpty(resource.Name, $"{resource.Uri} has no name for a client to store.");
            Assert.IsNotEmpty(resource.Title ?? string.Empty, $"{resource.Uri} has no title for a person to read.");
        }

        // The resources are a second door onto the same catalogs, so a component read through one
        // has to be the component read through the other.
        var read = await Mcp.ReadResourceAsync("bitblazorui://components/BitButton", cancellationToken: Ct);
        var text = string.Join("\n", read.Contents.OfType<TextResourceContents>().Select(c => c.Text));

        StringAssert.StartsWith(text, "# BitButton");
        StringAssert.Contains(text, "AllowDisabledFocus");
    }

    [DataTestMethod]
    [DataRow("bitblazorui://components/{name}", "name", "Dat", "BitDatePicker")]
    [DataRow("bitblazorui://types/{typeName}", "typeName", "BitCol", "BitColor")]
    [DataRow("bitblazorui://setup/{hostingModel}", "hostingModel", "", "web-app")]
    [DataRow("bitblazorui://theming/{section}", "section", "Pre", "Presets")]
    public async Task A_template_argument_completes_from_the_catalog_behind_it(string template, string argument, string typed, string expected)
    {
        var completion = await Mcp.CompleteAsync(new ResourceTemplateReference { Uri = template }, argument, typed, cancellationToken: Ct);

        CollectionAssert.Contains(completion.Completion.Values.ToArray(), expected,
            $"Completing '{argument}' of {template} with '{typed}' did not offer {expected}.");
    }

    [TestMethod]
    public async Task A_prompt_argument_with_a_closed_set_completes_too()
    {
        var completion = await Mcp.CompleteAsync(new PromptReference { Name = "add-bit-blazorui-to-app" }, "hostingModel", string.Empty, cancellationToken: Ct);

        var values = completion.Completion.Values.ToArray();

        using var scope = Assert.Scope();

        // Without this, a person picking the prompt out of a menu is asked to type a hosting model
        // with nothing to type it from.
        foreach (var model in new[] { "web-app", "wasm", "server", "hybrid", "unknown" })
        {
            CollectionAssert.Contains(values, model);
        }
    }
}
