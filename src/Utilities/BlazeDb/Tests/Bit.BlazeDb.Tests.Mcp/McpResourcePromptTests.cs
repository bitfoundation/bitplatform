using Bit.BlazeDb.Demo.Server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Bit.BlazeDb.Tests.Mcp;

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

        CollectionAssert.IsSubsetOf(new[] { "blazedb://guide", "blazedb://api", "blazedb://diagnostics" }, uris);
        Assert.Contains("blazedb://guide/Quick%20start", uris);
        Assert.Contains("blazedb://guide/Multiple%20tabs", uris);
    }

    [TestMethod]
    public async Task The_templates_are_published()
    {
        var templates = (await McpTestHost.Client.ListResourceTemplatesAsync(cancellationToken: TestContext.CancellationToken)).Select(template => template.UriTemplate).ToArray();

        CollectionAssert.IsSubsetOf(new[] { "blazedb://guide/{heading}", "blazedb://api/{typeName}", "blazedb://diagnostics/{code}", "blazedb://source/{path}" }, templates);
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
        Assert.AreEqual(BlazeDbApiCatalog.RenderType("BlazeDbDatabase"), await ReadAsync("blazedb://api/BlazeDbDatabase"));
        Assert.AreEqual(BlazeDbDiagnosticsReference.Render(BlazeDbDiagnosticsReference.Find("BLZ013")!), await ReadAsync("blazedb://diagnostics/BLZ013"));
        Assert.AreEqual(BlazeDbSourceCatalog.GetSourceFile("Demo/Client/Data/DemoModels.cs"), await ReadAsync("blazedb://source/Demo%2FClient%2FData%2FDemoModels.cs"));
        Assert.StartsWith("## Storage quota", await ReadAsync("blazedb://guide/Storage%20quota"));
    }

    // ---- Prompts -----------------------------------------------------------

    private static readonly (string Name, Dictionary<string, object?>? Arguments)[] _prompts =
    [
        ("add-blazedb-to-app", new() { ["hostingModel"] = "wasm" }),
        ("implement-blazedb-feature", new() { ["feature"] = "a todo list that survives a page reload" }),
        ("debug-blazedb-database", new() { ["symptom"] = "the data is gone after a page reload" }),
    ];

    [TestMethod]
    public async Task Every_prompt_is_published()
    {
        var names = (await McpTestHost.Client.ListPromptsAsync(cancellationToken: TestContext.CancellationToken)).Select(prompt => prompt.Name).ToArray();

        Assert.AreSequenceEqual(_prompts.Select(prompt => prompt.Name).ToArray(), names, Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
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
    public async Task The_prompts_name_only_types_and_members_the_library_has()
    {
        // A prompt that tells an agent to use a type the library does not have sends it straight into
        // the mistake the prompt exists to prevent.
        var types = BlazeDbApiCatalog.Types.Select(type => type.Name.Split('<')[0]).ToHashSet(StringComparer.Ordinal);

        foreach (var (name, arguments) in _prompts)
        {
            var text = await PromptTextAsync(name, arguments);

            foreach (var type in System.Text.RegularExpressions.Regex.Matches(text, @"`(I?BlazeDb[A-Z][A-Za-z]*)`").Select(match => match.Groups[1].Value))
            {
                Assert.Contains(type, types, $"'{name}' names '{type}', which is not a public Bit.BlazeDb type.");
            }
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
        var types = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "blazedb://api/{typeName}" }, "typeName", "BlazeDbDatabase", cancellationToken: TestContext.CancellationToken)).Completion.Values;
        var headings = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "blazedb://guide/{heading}" }, "heading", "encrypt", cancellationToken: TestContext.CancellationToken)).Completion.Values;
        var codes = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "blazedb://diagnostics/{code}" }, "code", "BLZ01", cancellationToken: TestContext.CancellationToken)).Completion.Values;
        var paths = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "blazedb://source/{path}" }, "path", "Todos", cancellationToken: TestContext.CancellationToken)).Completion.Values;

        Assert.Contains("BlazeDbDatabase", types.ToArray());
        Assert.AreEqual("BlazeDbDatabase", types[0], "A value the typed text starts should rank first.");
        Assert.Contains("Encryption at rest", headings.ToArray());
        Assert.Contains("BLZ013", codes.ToArray());
        Assert.Contains("Demo/Client/Pages/Todos.razor", paths.ToArray());
    }

    [TestMethod]
    public async Task Prompt_arguments_complete_with_the_phrasings_they_were_written_for()
    {
        var symptoms = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "debug-blazedb-database" }, "symptom", "", cancellationToken: TestContext.CancellationToken)).Completion.Values;
        var models = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "add-blazedb-to-app" }, "hostingModel", "", cancellationToken: TestContext.CancellationToken)).Completion.Values;

        Assert.IsNotEmpty(symptoms);
        CollectionAssert.IsSubsetOf(BlazeDbSetupGuide.HostingModels, models.ToArray());
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
