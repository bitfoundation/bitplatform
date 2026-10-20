using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Bdnd.Demo.Server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Bit.Bdnd.Tests.Mcp;

/// <summary>
/// The other two halves of the protocol - resources and prompts - plus the completions that let a
/// client fill in their placeholders and arguments.
/// </summary>
[TestClass]
public partial class McpResourcePromptTests
{
    // ---- Resources ---------------------------------------------------------

    [TestMethod]
    public async Task The_fixed_resources_and_the_guide_sections_are_listed()
    {
        var uris = (await McpTestHost.Client.ListResourcesAsync()).Select(resource => resource.Uri).ToArray();

        CollectionAssert.IsSubsetOf(new[] { "bdnd://guide", "bdnd://api", "bdnd://sample-scenario" }, uris);
        CollectionAssert.Contains(uris, "bdnd://guide/Getting%20started");
        CollectionAssert.Contains(uris, "bdnd://guide/Coming%20from%20React%20DnD");

        // Only the top-level sections are listed; the rest stay behind the template.
        CollectionAssert.DoesNotContain(uris, "bdnd://guide/Install");
    }

    [TestMethod]
    public async Task The_templates_are_published()
    {
        var templates = (await McpTestHost.Client.ListResourceTemplatesAsync()).Select(template => template.UriTemplate).ToArray();

        CollectionAssert.IsSubsetOf(new[] { "bdnd://guide/{heading}", "bdnd://api/{typeName}", "bdnd://source/{path}" }, templates);
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
        Assert.AreEqual(BdndApiCatalog.RenderType("BdndDropTarget"), await ReadAsync("bdnd://api/BdndDropTarget"));
        Assert.AreEqual(BdndApiCatalog.RenderType("BdndDraggable"), await ReadAsync("bdnd://api/BdndDraggable%3CTItem%3E"));
        Assert.AreEqual(BdndSourceCatalog.GetSourceFile("Demo/Client/Pages/Sortable/SortableCard.razor"), await ReadAsync("bdnd://source/Demo%2FClient%2FPages%2FSortable%2FSortableCard.razor"));
        Assert.StartsWith("### Drop results", await ReadAsync("bdnd://guide/Drop%20results"));
        Assert.AreEqual(BdndApiCatalog.RenderIndex(), await ReadAsync("bdnd://api"));
        Assert.AreEqual(BdndSourceCatalog.Readme, await ReadAsync("bdnd://guide"));
    }

    [TestMethod]
    public async Task The_sample_scenario_resource_is_the_one_the_lab_hands_out()
    {
        var sample = await ReadAsync("bdnd://sample-scenario");

        Assert.AreEqual(BdndDropLab.Sample, sample);
        Assert.AreEqual(sample, (await McpDropLabTests.InspectAsync(null)).Sample);

        using var document = JsonDocument.Parse(sample);

        Assert.AreEqual("card", document.RootElement.GetProperty("draggable").GetProperty("type").GetString());
        Assert.IsGreaterThan(0, document.RootElement.GetProperty("targets").GetArrayLength());
    }

    // ---- Prompts -----------------------------------------------------------

    private static readonly (string Name, Dictionary<string, object?>? Arguments)[] _prompts =
    [
        ("add-bdnd-to-app", new() { ["renderMode"] = "wasm" }),
        ("implement-bdnd-feature", new() { ["feature"] = "a sortable list reordered by dragging" }),
        ("debug-bdnd-drag", new() { ["symptom"] = "the item drags but the drop target never lights up" }),
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
        var types = BdndApiCatalog.Types.Select(type => SimpleName(type.Name)).ToHashSet(StringComparer.Ordinal);

        foreach (var (name, arguments) in _prompts)
        {
            var text = await PromptTextAsync(name, arguments);

            foreach (var type in BdndTypeRegex().Matches(text).Select(match => match.Groups["type"].Value))
            {
                Assert.Contains(type, types, $"'{name}' names '{type}', which is not a public Bit.Bdnd type.");
            }
        }
    }

    [TestMethod]
    public async Task The_prompts_name_only_members_the_components_have()
    {
        var members = new[] { "BdndDraggable", "BdndDropTarget", "BdndDropEventArgs", "BdndDragEndContext", "BdndDropState", "BdndDragState", "BdndInterop", "BitBdnd" }
            .SelectMany(type => BdndApiCatalog.GetTypeDetails(type)!.Members.Select(member => member.Name))
            .ToHashSet(StringComparer.Ordinal);

        string[] named = ["Type", "Accept", "AcceptTypes", "OnDrop", "OnHover", "OnNativeDrop", "CanDropPredicate", "OnEnd", "DidDrop", "DropResult",
                          "IsDragging", "IsOver", "IsOverShallow", "CanDrop", "UseCustomDragLayer", "UseHandle", "DropEffect", "CanDrag", "AddBitBdndServices"];

        var text = string.Join('\n', await Task.WhenAll(_prompts.Select(prompt => PromptTextAsync(prompt.Name, prompt.Arguments))));

        foreach (var member in named)
        {
            Assert.Contains(member, members, $"No component of the library has a member '{member}'.");
            Assert.Contains($"`{member}", text, $"The prompts no longer mention '{member}'; drop it from this list.");
        }
    }

    [TestMethod]
    public async Task The_setup_prompt_says_what_the_library_needs_and_what_it_does_not()
    {
        var text = await PromptTextAsync("add-bdnd-to-app", new() { ["renderMode"] = "auto" });

        Assert.Contains("builder.Services.AddBitBdndServices();", text);
        Assert.Contains("BOTH the server's and the client's", text);
        Assert.Contains("Do NOT link a stylesheet", text);
        Assert.Contains("<BdndProvider>", text);
        Assert.Contains("interactively rendered page", text);
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
        var types = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "bdnd://api/{typeName}" }, "typeName", "BdndDrop")).Completion.Values;
        var headings = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "bdnd://guide/{heading}" }, "heading", "drop")).Completion.Values;
        var paths = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "bdnd://source/{path}" }, "path", "Sortable")).Completion.Values;

        CollectionAssert.Contains(types.ToArray(), "BdndDropTarget<TItem>");
        Assert.StartsWith("BdndDrop", types[0], "A value the typed text starts should rank first.");
        CollectionAssert.Contains(headings.ToArray(), "Drop targets");
        CollectionAssert.Contains(headings.ToArray(), "Drop results");
        CollectionAssert.Contains(paths.ToArray(), "Demo/Client/Pages/Sortable/SortableCard.razor");
    }

    [TestMethod]
    public async Task Prompt_arguments_complete_with_the_phrasings_they_were_written_for()
    {
        var symptoms = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "debug-bdnd-drag" }, "symptom", "")).Completion.Values;
        var features = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "implement-bdnd-feature" }, "feature", "sortable")).Completion.Values;
        var modes = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "add-bdnd-to-app" }, "renderMode", "")).Completion.Values;

        Assert.IsNotEmpty(symptoms);
        Assert.IsNotEmpty(features);
        CollectionAssert.IsSubsetOf(BdndSetupGuide.RenderModes, modes.ToArray());
    }

    private static string SimpleName(string typeName)
    {
        var generic = typeName.IndexOf('<', StringComparison.Ordinal);

        return generic < 0 ? typeName : typeName[..generic];
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

    [GeneratedRegex(@"`<?(?<type>I?Bdnd[A-Z][A-Za-z]*)")]
    private static partial Regex BdndTypeRegex();
}
