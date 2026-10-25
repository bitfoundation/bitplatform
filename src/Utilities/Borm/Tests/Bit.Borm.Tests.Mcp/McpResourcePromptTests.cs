using Bit.Borm.Demo.Server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Bit.Borm.Tests.Mcp;

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

        CollectionAssert.IsSubsetOf(new[] { "borm://guide", "borm://api", "borm://theme", "borm://sample-schema" }, uris);
        CollectionAssert.Contains(uris, "borm://guide/Getting%20started");
        CollectionAssert.Contains(uris, "borm://guide/Generating%20a%20schema%20from%20a%20C%23%20type");

        // Only the top-level sections are listed; the rest stay behind the template.
        CollectionAssert.DoesNotContain(uris, "borm://guide/Install");
    }

    [TestMethod]
    public async Task The_templates_are_published()
    {
        var templates = (await McpTestHost.Client.ListResourceTemplatesAsync()).Select(template => template.UriTemplate).ToArray();

        CollectionAssert.IsSubsetOf(new[] { "borm://guide/{heading}", "borm://api/{typeName}", "borm://source/{path}" }, templates);
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
        Assert.AreEqual(BormApiCatalog.RenderType("BormState"), await ReadAsync("borm://api/BormState"));
        Assert.AreEqual(BormSourceCatalog.GetSourceFile("Demo/Client/Pages/WizardDemo.razor"), await ReadAsync("borm://source/Demo%2FClient%2FPages%2FWizardDemo.razor"));
        Assert.StartsWith("## Async and cascading options", await ReadAsync("borm://guide/Async%20and%20cascading%20options"));
        Assert.AreEqual(BormThemeCatalog.RenderIndex(), await ReadAsync("borm://theme"));
        Assert.AreEqual(BormSourceCatalog.Readme, await ReadAsync("borm://guide"));
    }

    [TestMethod]
    public async Task The_sample_schema_resource_is_the_one_the_lab_hands_out()
    {
        var sample = await ReadAsync("borm://sample-schema");

        Assert.AreEqual(BormSchemaLab.Sample, sample);
        Assert.AreEqual(sample, (await McpSchemaLabTests.InspectAsync(null)).Sample);

        Assert.IsTrue(BormJsonSchemaImporter.TryImport(sample, out var definition, out var error), error);
        Assert.IsNotEmpty(definition.Fields);
    }

    // ---- Prompts -----------------------------------------------------------

    private static readonly (string Name, Dictionary<string, object?>? Arguments)[] _prompts =
    [
        ("add-borm-to-app", new() { ["hostingModel"] = "web-app" }),
        ("implement-borm-form", new() { ["feature"] = "a field that only shows when a checkbox is ticked" }),
        ("debug-borm-form", new() { ["symptom"] = "a dropdown's options do not reload when the other field changes" }),
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
        var types = BormApiCatalog.Types.Select(type => type.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var (name, arguments) in _prompts)
        {
            var text = await PromptTextAsync(name, arguments);

            foreach (var type in System.Text.RegularExpressions.Regex.Matches(text, @"`<?(I?Borm[A-Z][A-Za-z]*)").Select(match => match.Groups[1].Value))
            {
                Assert.Contains(type, types, $"'{name}' names '{type}', which is not a public Bit.Borm type.");
            }
        }
    }

    [TestMethod]
    public async Task The_prompts_name_only_members_the_library_has()
    {
        // Every member a prompt tells an agent to reach for, held to the type that has to have it.
        (string Type, string[] Members)[] named =
        [
            ("BormFieldBuilder", ["VisibleWhen", "RequiredWhen", "ReadOnlyWhen", "ClearOnHide", "OptionsFrom", "Computed", "OnChange", "RevalidateOn", "MatchesField", "Normalize", "Trim"]),
            ("BormBuilder", ["Step", "Array", "ArrayOf"]),
            ("BormView", ["IdPrefix", "Definition", "Model", "Data", "State", "OnValidSubmit", "FocusFirstError"]),
            ("BormState", ["StateChanged", "ValidationTrigger"]),
            ("BormDefinition", ["Validate", "Clone"]),
            ("BormFieldDefinition", ["UpdateOn"]),
            ("BormFieldRendererRegistryExtensions", ["RegisterCustom"]),
            ("BormServiceCollectionExtensions", ["AddBorm"]),
            ("BormJsonSchemaImporter", ["TryImport", "Import"]),
        ];

        var text = string.Join('\n', await Task.WhenAll(_prompts.Select(prompt => PromptTextAsync(prompt.Name, prompt.Arguments))));

        foreach (var (type, members) in named)
        {
            var declared = BormApiCatalog.GetTypeDetails(type)!.Members.Select(member => member.Name).ToHashSet(StringComparer.Ordinal);

            foreach (var member in members)
            {
                Assert.Contains(member, declared, $"{type} has no member '{member}'.");
                Assert.Contains(member, text, $"The prompts no longer mention '{member}'; drop it from this list.");
            }
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
        var types = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "borm://api/{typeName}" }, "typeName", "BormState")).Completion.Values;
        var headings = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "borm://guide/{heading}" }, "heading", "valid")).Completion.Values;
        var paths = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "borm://source/{path}" }, "path", "Wizard")).Completion.Values;

        CollectionAssert.Contains(types.ToArray(), "BormState");
        Assert.AreEqual("BormState", types[0], "A value the typed text starts should rank first.");
        CollectionAssert.Contains(headings.ToArray(), "Validation");
        CollectionAssert.Contains(headings.ToArray(), "When validation runs");
        CollectionAssert.Contains(paths.ToArray(), "Demo/Client/Pages/WizardDemo.razor");
    }

    [TestMethod]
    public async Task Prompt_arguments_complete_with_the_phrasings_they_were_written_for()
    {
        var symptoms = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "debug-borm-form" }, "symptom", "")).Completion.Values;
        var features = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "implement-borm-form" }, "feature", "wizard")).Completion.Values;
        var models = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "add-borm-to-app" }, "hostingModel", "")).Completion.Values;

        Assert.IsNotEmpty(symptoms);
        Assert.IsNotEmpty(features);
        CollectionAssert.IsSubsetOf(BormSetupGuide.HostingModels, models.ToArray());
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
