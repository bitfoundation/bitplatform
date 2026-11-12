using System.Text.Json;
using Bit.Bortable.Demo.Server.Dtos;
using Bit.Bortable.Demo.Server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Bit.Bortable.Tests.Mcp;

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
        var uris = (await McpTestHost.Client.ListResourcesAsync(cancellationToken: TestContext.CancellationToken)).Select(resource => resource.Uri).ToArray();

        CollectionAssert.IsSubsetOf(new[] { "bortable://guide", "bortable://api", "bortable://theme", "bortable://sample-drag" }, uris);
        Assert.Contains("bortable://guide/Getting%20started", uris);
        Assert.Contains("bortable://guide/Coming%20from%20SortableJS", uris);

        // Only the top-level sections are listed; the rest stay behind the template.
        Assert.DoesNotContain("bortable://guide/Install", uris);
    }

    [TestMethod]
    public async Task The_templates_are_published()
    {
        var templates = (await McpTestHost.Client.ListResourceTemplatesAsync(cancellationToken: TestContext.CancellationToken)).Select(template => template.UriTemplate).ToArray();

        CollectionAssert.IsSubsetOf(new[] { "bortable://guide/{heading}", "bortable://api/{typeName}", "bortable://source/{path}" }, templates);
    }

    [TestMethod]
    public async Task Every_listed_resource_reads_back_as_text()
    {
        foreach (var resource in await McpTestHost.Client.ListResourcesAsync(cancellationToken: TestContext.CancellationToken))
        {
            var text = await ReadAsync(resource.Uri);

            Assert.IsFalse(string.IsNullOrWhiteSpace(text), resource.Uri);
            Assert.DoesNotContain("has no section called", text, resource.Uri);
        }
    }

    [TestMethod]
    public async Task The_templates_resolve_the_same_keys_the_tools_do()
    {
        Assert.AreEqual(BortableApiCatalog.RenderType("Bortable"), await ReadAsync("bortable://api/Bortable"));
        Assert.AreEqual(BortableApiCatalog.RenderType("BortablePull"), await ReadAsync("bortable://api/BortablePull"));
        Assert.AreEqual(BortableSourceCatalog.GetSourceFile("Demo/Client/Pages/Shared.razor"), await ReadAsync("bortable://source/Demo%2FClient%2FPages%2FShared.razor"));
        Assert.StartsWith("## Shared lists", await ReadAsync("bortable://guide/Shared%20lists"));
        Assert.AreEqual(BortableThemeCatalog.RenderIndex(), await ReadAsync("bortable://theme"));
        Assert.AreEqual(BortableSourceCatalog.Readme, await ReadAsync("bortable://guide"));
    }

    [TestMethod]
    public async Task The_sample_drag_resource_is_the_one_the_lab_runs()
    {
        var sample = await ReadAsync("bortable://sample-drag");

        Assert.AreEqual(BortableDragLab.Sample, sample);
        Assert.AreEqual(sample, (await McpDragLabTests.InspectAsync(null)).Sample);

        var scenario = JsonSerializer.Deserialize<BortableDragScenarioDto>(sample, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.IsNotNull(scenario);
        Assert.HasCount(2, scenario.Lists!);
        Assert.AreEqual("Review the PR", scenario.Drag!.Item);

        // And it runs as written, through the tool, to the same report as running nothing.
        Assert.AreEqual((await McpDragLabTests.InspectAsync(null)).Outcome, (await McpDragLabTests.InspectAsync(sample)).Outcome);
    }

    // ---- Prompts -----------------------------------------------------------

    private static readonly (string Name, Dictionary<string, object?>? Arguments)[] _prompts =
    [
        ("add-bortable-to-app", new() { ["renderMode"] = "wasm" }),
        ("implement-bortable-feature", new() { ["feature"] = "a kanban board with columns cards move between" }),
        ("debug-bortable-drag", new() { ["symptom"] = "an item cannot be dropped into the other list" }),
    ];

    [TestMethod]
    public async Task Every_prompt_is_published()
    {
        var names = (await McpTestHost.Client.ListPromptsAsync(cancellationToken: TestContext.CancellationToken)).Select(prompt => prompt.Name).ToArray();

        Assert.AreSequenceEqual(_prompts.Select(prompt => prompt.Name).ToArray(), names, global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
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

            foreach (var tool in mentioned) Assert.Contains(tool, McpSurfaceTests.ExpectedTools, $"'{name}' mentions '{tool}', which is not a tool.");
        }
    }

    [TestMethod]
    public async Task The_prompts_name_only_types_the_library_has()
    {
        // A prompt that tells an agent to use a type the library does not have sends it straight into
        // the mistake the prompt exists to prevent. Generic types are compared without their arguments.
        var types = BortableApiCatalog.Types.Select(type => type.Name.Split('<')[0]).ToHashSet(StringComparer.Ordinal);

        foreach (var (name, arguments) in _prompts)
        {
            var text = await PromptTextAsync(name, arguments);

            foreach (var type in System.Text.RegularExpressions.Regex.Matches(text, @"`<?((?:Bit)?Bortable[A-Za-z]*)").Select(match => match.Groups[1].Value))
            {
                Assert.Contains(type, types, $"'{name}' names '{type}', which is not a public Bit.Bortable type.");
            }
        }
    }

    [TestMethod]
    public async Task The_prompts_name_only_parameters_the_component_has()
    {
        var members = BortableApiCatalog.GetTypeDetails("Bortable")!.Members.Select(member => member.Name).ToHashSet(StringComparer.Ordinal);

        string[] named = ["Group", "Pull", "Put", "Sort", "OnEnd", "OnUpdate", "OnAdd", "OnRemove", "ItemsChanged", "Id", "Clone", "Filter", "Handle",
                          "Swap", "MultiDrag", "GhostClass", "ChosenClass", "SwapClass", "SelectedClass", "SwapThreshold", "Animation"];

        var text = string.Join('\n', await Task.WhenAll(_prompts.Select(prompt => PromptTextAsync(prompt.Name, prompt.Arguments))));

        foreach (var member in named)
        {
            Assert.Contains(member, members, $"Bortable<TItem> has no member '{member}'.");
            Assert.Contains($"`{member}", text, $"The prompts no longer mention '{member}'; drop it from this list.");
        }
    }

    [TestMethod]
    public async Task An_unknown_prompt_is_an_error()
    {
        await Assert.ThrowsAsync<McpException>(async () => await McpTestHost.Client.GetPromptAsync("write-my-app", cancellationToken: TestContext.CancellationToken));
    }

    // ---- Completions -------------------------------------------------------

    [TestMethod]
    public async Task Resource_placeholders_complete_from_the_catalogs()
    {
        var types = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "bortable://api/{typeName}" }, "typeName", "Bortable", cancellationToken: TestContext.CancellationToken)).Completion.Values;
        var headings = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "bortable://guide/{heading}" }, "heading", "swap", cancellationToken: TestContext.CancellationToken)).Completion.Values;
        var paths = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "bortable://source/{path}" }, "path", "SwapDemo", cancellationToken: TestContext.CancellationToken)).Completion.Values;

        Assert.Contains("Bortable<TItem>", types.ToArray());
        Assert.AreEqual("Bortable<TItem>", types[0], "A value the typed text starts should rank first.");
        Assert.Contains("Swap mode", headings.ToArray());
        Assert.Contains("Swap thresholds", headings.ToArray());
        Assert.Contains("Demo/Client/Pages/SwapDemo.razor", paths.ToArray());
    }

    [TestMethod]
    public async Task Prompt_arguments_complete_with_the_phrasings_they_were_written_for()
    {
        var symptoms = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "debug-bortable-drag" }, "symptom", "", cancellationToken: TestContext.CancellationToken)).Completion.Values;
        var features = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "implement-bortable-feature" }, "feature", "kanban", cancellationToken: TestContext.CancellationToken)).Completion.Values;
        var modes = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "add-bortable-to-app" }, "renderMode", "", cancellationToken: TestContext.CancellationToken)).Completion.Values;

        Assert.IsNotEmpty(symptoms);
        Assert.IsNotEmpty(features);
        CollectionAssert.IsSubsetOf(BortableSetupGuide.RenderModes, modes.ToArray());
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

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
