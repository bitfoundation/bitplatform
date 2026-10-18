using Bit.Balette.Demo.Server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Bit.Balette.Tests.Mcp;

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

        CollectionAssert.IsSubsetOf(new[] { "balette://guide", "balette://api", "balette://theme" }, uris);
        CollectionAssert.Contains(uris, "balette://guide/Getting%20started");
        CollectionAssert.Contains(uris, "balette://guide/Search%20and%20ranking");

        // Every top-level section, and only those: the sub-sections stay templated.
        var listed = uris.Count(uri => uri.StartsWith("balette://guide/", StringComparison.Ordinal));
        Assert.AreEqual(BaletteSourceCatalog.GuideSections.Count(section => section.Level == 2), listed);
    }

    [TestMethod]
    public async Task The_templates_are_published()
    {
        var templates = (await McpTestHost.Client.ListResourceTemplatesAsync()).Select(template => template.UriTemplate).ToArray();

        CollectionAssert.IsSubsetOf(new[] { "balette://guide/{heading}", "balette://api/{typeName}", "balette://source/{path}" }, templates);
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
        Assert.AreEqual(BaletteApiCatalog.RenderType("BaletteCommand"), await ReadAsync("balette://api/BaletteCommand"));
        Assert.AreEqual(BaletteSourceCatalog.GetSourceFile("Demo/Client/Services/DemoCommands.cs"), await ReadAsync("balette://source/Demo%2FClient%2FServices%2FDemoCommands.cs"));
        Assert.AreEqual(BaletteSourceCatalog.GetSourceFile(BaletteSourceCatalog.StylesheetPath), await ReadAsync("balette://source/Library%2FStyles%2Fbit-balette.css"));
        Assert.StartsWith("## Nested pages", await ReadAsync("balette://guide/Nested%20pages"));
        Assert.AreEqual(await McpCall.TextAsync("GetBaletteThemeVariables"), await ReadAsync("balette://theme"));
        Assert.AreEqual(BaletteSourceCatalog.Readme, await ReadAsync("balette://guide"));
    }

    // ---- Prompts -----------------------------------------------------------

    private static readonly (string Name, Dictionary<string, object?>? Arguments)[] _prompts =
    [
        ("add-balette-to-app", new() { ["renderMode"] = "wasm" }),
        ("implement-balette-feature", new() { ["feature"] = "a rename command that asks for the new name" }),
        ("debug-balette-palette", new() { ["symptom"] = "Ctrl+K does nothing" }),
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
            var text = await PromptTextAsync(name, arguments);

            foreach (var value in arguments!.Values) Assert.Contains((string)value!, text, name);

            Assert.Contains("Work in this order", text, name);

            var mentioned = ToolNames.MentionedIn(text).ToArray();
            Assert.IsNotEmpty(mentioned, name);

            foreach (var tool in mentioned) CollectionAssert.Contains(McpSurfaceTests.ExpectedTools, tool, $"'{name}' mentions '{tool}', which is not a tool.");
        }
    }

    [TestMethod]
    public async Task The_prompts_name_only_types_the_library_has()
    {
        // A prompt that tells an agent to use a type the library does not have sends it straight into
        // the mistake the prompt exists to prevent.
        var types = BaletteApiCatalog.Types.Select(type => type.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var (name, arguments) in _prompts)
        {
            var text = await PromptTextAsync(name, arguments);

            foreach (var type in System.Text.RegularExpressions.Regex.Matches(text, @"`(I?Balette[A-Z][A-Za-z]*)`").Select(match => match.Groups[1].Value))
            {
                Assert.Contains(type, types, $"'{name}' names '{type}', which is not a public Bit.Balette type.");
            }
        }
    }

    [TestMethod]
    public async Task The_prompts_aim_at_the_mistakes_that_fail_silently()
    {
        var setup = await PromptTextAsync("add-balette-to-app", new() { ["renderMode"] = "auto" });
        var feature = await PromptTextAsync("implement-balette-feature", new() { ["feature"] = "a settings toggle" });
        var debug = await PromptTextAsync("debug-balette-palette", new() { ["symptom"] = "the palette opens unstyled" });

        // Setup: the stylesheet is not injected, the services go in both containers, the layout must be interactive,
        // and the namespace swallows the component's name.
        Assert.Contains("_content/Bit.Balette/bit-balette.css", setup);
        Assert.Contains("BOTH", setup);
        Assert.Contains("statically rendered", setup);
        Assert.Contains("Bit.Balette.Balette", setup);

        // Features: immutable commands, explicit ids, disposing page registrations, ApplyMatcher, Destructive.
        Assert.Contains("immutable", feature);
        Assert.Contains("explicit `Id`", feature);
        Assert.Contains("dispose", feature);
        Assert.Contains("ApplyMatcher", feature);
        Assert.Contains("Destructive", feature);

        // Debugging: the setup first, then the search lab for a missing command.
        Assert.Contains("GetBaletteSetupGuide", debug);
        Assert.Contains("InspectBaletteSearch", debug);
        Assert.Contains("not injected", debug);
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
        var types = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "balette://api/{typeName}" }, "typeName", "BaletteCommand")).Completion.Values;
        var headings = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "balette://guide/{heading}" }, "heading", "nested")).Completion.Values;
        var paths = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "balette://source/{path}" }, "path", "DemoCommands")).Completion.Values;

        CollectionAssert.Contains(types.ToArray(), "BaletteCommand");
        Assert.StartsWith("BaletteCommand", types[0], "A value the typed text starts should rank first.");
        CollectionAssert.Contains(headings.ToArray(), "Nested pages");
        CollectionAssert.Contains(paths.ToArray(), "Demo/Client/Services/DemoCommands.cs");
    }

    [TestMethod]
    public async Task Prompt_arguments_complete_with_the_phrasings_they_were_written_for()
    {
        var symptoms = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "debug-balette-palette" }, "symptom", "")).Completion.Values;
        var features = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "implement-balette-feature" }, "feature", "toggle")).Completion.Values;
        var modes = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "add-balette-to-app" }, "renderMode", "")).Completion.Values;

        Assert.IsNotEmpty(symptoms);
        Assert.IsTrue(features.All(feature => feature.Contains("toggle", StringComparison.OrdinalIgnoreCase)));
        Assert.IsNotEmpty(features);
        CollectionAssert.IsSubsetOf(BaletteSetupGuide.RenderModes, modes.ToArray());
    }

    private static async Task<string> PromptTextAsync(string name, Dictionary<string, object?>? arguments)
    {
        var result = await McpTestHost.Client.GetPromptAsync(name, arguments);

        return string.Join('\n', result.Messages.Select(message => message.Content).OfType<TextContentBlock>().Select(block => block.Text));
    }

    private static async Task<string> ReadAsync(string uri)
    {
        var result = await McpTestHost.Client.ReadResourceAsync(uri);

        return string.Join('\n', result.Contents.OfType<TextResourceContents>().Select(contents => contents.Text));
    }
}
