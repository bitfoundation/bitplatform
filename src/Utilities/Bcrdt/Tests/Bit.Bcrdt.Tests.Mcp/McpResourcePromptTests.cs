using Bit.Bcrdt.Demo.Server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Bit.Bcrdt.Tests.Mcp;

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

        CollectionAssert.IsSubsetOf(new[] { "bcrdt://guide", "bcrdt://api", "bcrdt://setup" }, uris);
        CollectionAssert.Contains(uris, "bcrdt://guide/Getting%20started");
        CollectionAssert.Contains(uris, "bcrdt://guide/Choosing%20a%20data%20type");

        // Every top-level section, and only those: the sub-sections stay templated.
        var listed = uris.Count(uri => uri.StartsWith("bcrdt://guide/", StringComparison.Ordinal));
        Assert.AreEqual(BcrdtSourceCatalog.GuideSections.Count(section => section.Level == 2), listed);
    }

    [TestMethod]
    public async Task The_templates_are_published()
    {
        var templates = (await McpTestHost.Client.ListResourceTemplatesAsync()).Select(template => template.UriTemplate).ToArray();

        CollectionAssert.IsSubsetOf(new[] { "bcrdt://guide/{heading}", "bcrdt://api/{typeName}", "bcrdt://source/{path}" }, templates);
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
        Assert.AreEqual(BcrdtApiCatalog.RenderType("BcrdtCollaborativeDocument"), await ReadAsync("bcrdt://api/BcrdtCollaborativeDocument"));
        Assert.AreEqual(BcrdtSourceCatalog.GetSourceFile("Demo/Client/Pages/Live.razor"), await ReadAsync("bcrdt://source/Demo%2FClient%2FPages%2FLive.razor"));
        Assert.StartsWith("## Choosing a data type", await ReadAsync("bcrdt://guide/Choosing%20a%20data%20type"));
        Assert.AreEqual(await McpCall.TextAsync("GetBcrdtSetupGuide"), await ReadAsync("bcrdt://setup"));
        Assert.AreEqual(await McpCall.TextAsync("GetBcrdtApi"), await ReadAsync("bcrdt://api"));
        Assert.AreEqual(BcrdtSourceCatalog.Readme, await ReadAsync("bcrdt://guide"));
    }

    // ---- Prompts -----------------------------------------------------------

    private static readonly (string Name, Dictionary<string, object?>? Arguments)[] _prompts =
    [
        ("add-bcrdt-to-app", new() { ["hostingModel"] = "web-app" }),
        ("implement-bcrdt-feature", new() { ["feature"] = "a form several people fill in together" }),
        ("debug-bcrdt-sync", new() { ["symptom"] = "two people typed into the same field and one of them lost everything" }),
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
        var types = BcrdtApiCatalog.Types.Select(type => type.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var (name, arguments) in _prompts)
        {
            var text = await PromptTextAsync(name, arguments);

            foreach (var type in System.Text.RegularExpressions.Regex.Matches(text, @"`(I?Bcrdt[A-Z][A-Za-z]*)`").Select(match => match.Groups[1].Value))
            {
                Assert.Contains(type, types, $"'{name}' names '{type}', which is not a public Bit.Bcrdt type.");
            }
        }
    }

    [TestMethod]
    public async Task The_prompts_aim_at_the_mistakes_that_fail_silently()
    {
        var setup = await PromptTextAsync("add-bcrdt-to-app", new() { ["hostingModel"] = "server" });
        var feature = await PromptTextAsync("implement-bcrdt-feature", new() { ["feature"] = "a shared description field" });
        var debug = await PromptTextAsync("debug-bcrdt-sync", new() { ["symptom"] = "edits never arrive" });

        // Setup: both containers, scoped not singleton, an absolute address, the sized hub, the data type.
        Assert.Contains("BOTH", setup);
        Assert.Contains("singleton", setup);
        Assert.Contains("absolute address", setup);
        Assert.Contains("AddBitBcrdtHub", setup);
        Assert.Contains("Choosing a data type", setup);

        // Features: the merge semantics first, checked against the real engine, and the input binding.
        Assert.Contains("one value", feature);
        Assert.Contains("InspectBcrdtMerge", feature);
        Assert.Contains("BcrdtTextSynchronizer", feature);
        Assert.Contains("created once", feature);

        // Debugging: the wiring first, then the merge lab for a lost edit and the decoder for a rejected one.
        Assert.Contains("GetBcrdtSetupGuide", debug);
        Assert.Contains("InspectBcrdtMerge", debug);
        Assert.Contains("InspectBcrdtUpdate", debug);
        Assert.Contains("UseWebSockets", debug);
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
        var types = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "bcrdt://api/{typeName}" }, "typeName", "BcrdtCollab")).Completion.Values;
        var headings = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "bcrdt://guide/{heading}" }, "heading", "choosing")).Completion.Values;
        var paths = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "bcrdt://source/{path}" }, "path", "Live")).Completion.Values;

        CollectionAssert.Contains(types.ToArray(), "BcrdtCollaborativeDocument");
        Assert.StartsWith("BcrdtCollab", types[0], "A value the typed text starts should rank first.");
        CollectionAssert.Contains(headings.ToArray(), "Choosing a data type");
        CollectionAssert.Contains(paths.ToArray(), "Demo/Client/Pages/Live.razor");
    }

    [TestMethod]
    public async Task Prompt_arguments_complete_with_the_phrasings_they_were_written_for()
    {
        var symptoms = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "debug-bcrdt-sync" }, "symptom", "")).Completion.Values;
        var features = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "implement-bcrdt-feature" }, "feature", "offline")).Completion.Values;
        var models = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "add-bcrdt-to-app" }, "hostingModel", "")).Completion.Values;

        Assert.IsNotEmpty(symptoms);
        Assert.IsNotEmpty(features);
        Assert.IsTrue(features.All(feature => feature.Contains("offline", StringComparison.OrdinalIgnoreCase)));
        CollectionAssert.IsSubsetOf(BcrdtSetupGuide.HostingModels, models.ToArray());
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
