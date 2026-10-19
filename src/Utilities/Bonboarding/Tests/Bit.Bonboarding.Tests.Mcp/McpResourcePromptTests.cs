using Bit.Bonboarding.Demo.Server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Bit.Bonboarding.Tests.Mcp;

/// <summary>
/// The other two halves of the protocol - resources and prompts - plus the completions that let a
/// client fill in their placeholders and arguments.
/// </summary>
[TestClass]
public class McpResourcePromptTests
{
    // ---- Resources ---------------------------------------------------------

    [TestMethod]
    public async Task The_fixed_resources_and_the_guide_sections_are_listed()
    {
        var uris = (await McpTestHost.Client.ListResourcesAsync()).Select(resource => resource.Uri).ToArray();

        CollectionAssert.IsSubsetOf(new[] { "bonboarding://guide", "bonboarding://api", "bonboarding://theme" }, uris);
        CollectionAssert.Contains(uris, "bonboarding://guide/Getting%20started");
        CollectionAssert.Contains(uris, "bonboarding://guide/Multi-page%20tours");
    }

    [TestMethod]
    public async Task The_templates_are_published()
    {
        var templates = (await McpTestHost.Client.ListResourceTemplatesAsync()).Select(template => template.UriTemplate).ToArray();

        CollectionAssert.IsSubsetOf(new[] { "bonboarding://guide/{heading}", "bonboarding://api/{typeName}", "bonboarding://source/{path}" }, templates);
    }

    [TestMethod]
    public async Task Every_listed_resource_reads_back_as_text()
    {
        foreach (var resource in await McpTestHost.Client.ListResourcesAsync())
        {
            var text = await ReadAsync(resource.Uri);

            Assert.IsFalse(string.IsNullOrWhiteSpace(text), resource.Uri);
            Assert.DoesNotContain("has no section called", text, resource.Uri);
        }
    }

    [TestMethod]
    public async Task The_templates_resolve_the_same_keys_the_tools_do()
    {
        Assert.AreEqual(BonboardingApiCatalog.RenderType("BonboardingTour"), await ReadAsync("bonboarding://api/BonboardingTour"));
        Assert.AreEqual(BonboardingSourceCatalog.GetSourceFile("Demo/Client/Pages/Flows.razor"), await ReadAsync("bonboarding://source/Demo%2FClient%2FPages%2FFlows.razor"));
        Assert.StartsWith("## Persistence", await ReadAsync("bonboarding://guide/Persistence"));
    }

    // ---- Prompts -----------------------------------------------------------

    private static readonly (string Name, Dictionary<string, object?>? Arguments)[] _prompts =
    [
        ("add-bonboarding-to-app", new() { ["renderMode"] = "auto" }),
        ("implement-bonboarding-tour", new() { ["feature"] = "a welcome tour that runs once per user" }),
        ("debug-bonboarding-tour", new() { ["symptom"] = "the tour starts but nothing is drawn" }),
    ];

    [TestMethod]
    public async Task Every_prompt_is_published()
    {
        var names = (await McpTestHost.Client.ListPromptsAsync()).Select(prompt => prompt.Name).ToArray();

        CollectionAssert.AreEquivalent(_prompts.Select(prompt => prompt.Name).ToArray(), names);
    }

    [TestMethod]
    public async Task Every_prompt_carries_its_argument_and_names_only_real_tools()
    {
        foreach (var (name, arguments) in _prompts)
        {
            var result = await McpTestHost.Client.GetPromptAsync(name, arguments);

            var text = string.Join('\n', result.Messages.Select(message => message.Content).OfType<TextContentBlock>().Select(block => block.Text));

            foreach (var value in arguments!.Values) Assert.Contains((string)value!, text, name);

            var mentioned = ToolNames.MentionedIn(text).ToArray();
            Assert.IsNotEmpty(mentioned, name);

            foreach (var tool in mentioned) CollectionAssert.Contains(McpSurfaceTests.ExpectedTools, tool, $"'{name}' mentions '{tool}', which is not a tool.");
        }
    }

    [TestMethod]
    public async Task An_unknown_prompt_is_an_error()
    {
        await Assert.ThrowsAsync<McpException>(async () => await McpTestHost.Client.GetPromptAsync("write-my-app"));
    }

    // ---- Completions -------------------------------------------------------

    [TestMethod]
    public async Task Resource_placeholders_complete_from_the_catalogs()
    {
        var types = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "bonboarding://api/{typeName}" }, "typeName", "BonboardingStep")).Completion.Values;
        var headings = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "bonboarding://guide/{heading}" }, "heading", "multi")).Completion.Values;
        var paths = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "bonboarding://source/{path}" }, "path", "Flows")).Completion.Values;

        CollectionAssert.Contains(types.ToArray(), "BonboardingStep");
        Assert.AreEqual("BonboardingStep", types[0], "A value the typed text starts should rank first.");
        CollectionAssert.Contains(headings.ToArray(), "Multi-page tours");
        CollectionAssert.Contains(paths.ToArray(), "Demo/Client/Pages/Flows.razor");
    }

    [TestMethod]
    public async Task Prompt_arguments_complete_with_the_phrasings_they_were_written_for()
    {
        var symptoms = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "debug-bonboarding-tour" }, "symptom", "")).Completion.Values;
        var modes = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "add-bonboarding-to-app" }, "renderMode", "")).Completion.Values;

        Assert.IsNotEmpty(symptoms);
        CollectionAssert.IsSubsetOf(BonboardingSetupGuide.RenderModes, modes.ToArray());
    }

    private static async Task<string> ReadAsync(string uri)
    {
        var result = await McpTestHost.Client.ReadResourceAsync(uri);

        return string.Join('\n', result.Contents.OfType<TextResourceContents>().Select(contents => contents.Text));
    }
}
