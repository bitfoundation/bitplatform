using System.Text.Json;
using System.Text.Json.Serialization;
using Bit.Blow.Demo.Server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Bit.Blow.Tests.Mcp;

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

        CollectionAssert.IsSubsetOf(new[] { "blow://guide", "blow://api", "blow://theme", "blow://sample-flow" }, uris);
        CollectionAssert.Contains(uris, "blow://guide/Getting%20started");
        CollectionAssert.Contains(uris, "blow://guide/Saving%2C%20undo%20and%20clipboard");

        // Only the top-level sections are listed; the rest stay behind the template.
        CollectionAssert.DoesNotContain(uris, "blow://guide/Install");
    }

    [TestMethod]
    public async Task The_templates_are_published()
    {
        var templates = (await McpTestHost.Client.ListResourceTemplatesAsync()).Select(template => template.UriTemplate).ToArray();

        CollectionAssert.IsSubsetOf(new[] { "blow://guide/{heading}", "blow://api/{typeName}", "blow://source/{path}" }, templates);
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
        Assert.AreEqual(BlowApiCatalog.RenderType("BlowCanvas"), await ReadAsync("blow://api/BlowCanvas"));
        Assert.AreEqual(BlowSourceCatalog.GetSourceFile("Demo/Client/Pages/CustomNodes.razor"), await ReadAsync("blow://source/Demo%2FClient%2FPages%2FCustomNodes.razor"));
        Assert.StartsWith("### Custom nodes", await ReadAsync("blow://guide/Custom%20nodes"));
        Assert.AreEqual(BlowThemeCatalog.RenderIndex(), await ReadAsync("blow://theme"));
        Assert.AreEqual(BlowSourceCatalog.Readme, await ReadAsync("blow://guide"));
    }

    [TestMethod]
    public async Task The_sample_flow_resource_is_the_one_the_lab_hands_out()
    {
        var sample = await ReadAsync("blow://sample-flow");

        Assert.AreEqual(BlowFlowLab.Sample, sample);
        Assert.AreEqual(sample, (await McpFlowLabTests.InspectAsync(null)).Sample);

        var snapshot = JsonSerializer.Deserialize<BlowSnapshot>(sample, new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } });

        Assert.IsNotNull(snapshot);
        Assert.IsNotEmpty(snapshot.Nodes);
    }

    // ---- Prompts -----------------------------------------------------------

    private static readonly (string Name, Dictionary<string, object?>? Arguments)[] _prompts =
    [
        ("add-blow-to-app", new() { ["renderMode"] = "wasm" }),
        ("implement-blow-feature", new() { ["feature"] = "a workflow editor with custom nodes and typed handles" }),
        ("debug-blow-flow", new() { ["symptom"] = "the canvas renders nothing at all" }),
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
        var types = BlowApiCatalog.Types.Select(type => type.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var (name, arguments) in _prompts)
        {
            var text = await PromptTextAsync(name, arguments);

            foreach (var type in System.Text.RegularExpressions.Regex.Matches(text, @"`<?(I?Blow[A-Z][A-Za-z]*)").Select(match => match.Groups[1].Value))
            {
                Assert.Contains(type, types, $"'{name}' names '{type}', which is not a public Bit.Blow type.");
            }
        }
    }

    [TestMethod]
    public async Task The_prompts_name_only_canvas_members_the_canvas_has()
    {
        var members = BlowApiCatalog.GetTypeDetails(nameof(BlowCanvas))!.Members.Select(member => member.Name).ToHashSet(StringComparer.Ordinal);

        string[] named = ["NodesChanged", "EdgesChanged", "OnSelectionChange", "OnConnect", "Refresh", "UpdateNodeInternals", "ToObject", "RestoreAsync",
                          "Copy", "PasteAsync", "IsValidConnection", "HistoryEnabled", "ClipboardShortcutsEnabled", "SnapToGrid", "SnapToGuides", "SelectionOnDrag", "OnError", "Style"];

        var text = string.Join('\n', await Task.WhenAll(_prompts.Select(prompt => PromptTextAsync(prompt.Name, prompt.Arguments))));

        foreach (var member in named)
        {
            Assert.Contains(member, members, $"BlowCanvas has no member '{member}'.");
            Assert.Contains($"`{member}", text, $"The prompts no longer mention '{member}'; drop it from this list.");
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
        var types = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "blow://api/{typeName}" }, "typeName", "BlowCanvas")).Completion.Values;
        var headings = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "blow://guide/{heading}" }, "heading", "custom")).Completion.Values;
        var paths = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "blow://source/{path}" }, "path", "SaveRestore")).Completion.Values;

        CollectionAssert.Contains(types.ToArray(), "BlowCanvas");
        Assert.AreEqual("BlowCanvas", types[0], "A value the typed text starts should rank first.");
        CollectionAssert.Contains(headings.ToArray(), "Custom nodes");
        CollectionAssert.Contains(headings.ToArray(), "Custom edges");
        CollectionAssert.Contains(paths.ToArray(), "Demo/Client/Pages/Examples/SaveRestoreExample.razor");
    }

    [TestMethod]
    public async Task Prompt_arguments_complete_with_the_phrasings_they_were_written_for()
    {
        var symptoms = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "debug-blow-flow" }, "symptom", "")).Completion.Values;
        var features = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "implement-blow-feature" }, "feature", "undo")).Completion.Values;
        var modes = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "add-blow-to-app" }, "renderMode", "")).Completion.Values;

        Assert.IsNotEmpty(symptoms);
        Assert.IsNotEmpty(features);
        CollectionAssert.IsSubsetOf(BlowSetupGuide.RenderModes, modes.ToArray());
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
