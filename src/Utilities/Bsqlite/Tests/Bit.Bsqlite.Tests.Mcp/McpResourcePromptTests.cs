using Bit.Bsqlite.Demo.Server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Bit.Bsqlite.Tests.Mcp;

/// <summary>
/// The other two halves of the protocol - resources and prompts - plus the completions that let a
/// client fill in their placeholders and arguments.
/// </summary>
[TestClass]
public class McpResourcePromptTests
{
    // ---- Resources ---------------------------------------------------------

    [TestMethod]
    public async Task The_fixed_resources_the_documents_and_the_setup_guides_are_listed()
    {
        var uris = (await McpTestHost.Client.ListResourcesAsync()).Select(resource => resource.Uri).ToArray();

        CollectionAssert.IsSubsetOf(new[] { "bsqlite://guide", "bsqlite://api", "bsqlite://storage" }, uris);
        CollectionAssert.Contains(uris, "bsqlite://docs/storage");
        CollectionAssert.Contains(uris, "bsqlite://docs/Bit.Bsqlite.Testing");
        CollectionAssert.Contains(uris, "bsqlite://setup/move-from-besql");

        foreach (var document in BsqliteSourceCatalog.Documents) CollectionAssert.Contains(uris, $"bsqlite://docs/{Uri.EscapeDataString(document.Name)}");
        foreach (var scenario in BsqliteSetupGuide.Scenarios) CollectionAssert.Contains(uris, $"bsqlite://setup/{scenario}");
    }

    [TestMethod]
    public async Task The_templates_are_published()
    {
        var templates = (await McpTestHost.Client.ListResourceTemplatesAsync()).Select(template => template.UriTemplate).ToArray();

        CollectionAssert.IsSubsetOf(new[]
        {
            "bsqlite://docs/{name}", "bsqlite://api/{typeName}", "bsqlite://storage/{provider}", "bsqlite://setup/{scenario}", "bsqlite://source/{path}"
        }, templates);
    }

    [TestMethod]
    public async Task Every_listed_resource_reads_back_as_text()
    {
        foreach (var resource in await McpTestHost.Client.ListResourcesAsync())
        {
            var text = await ReadAsync(resource.Uri);

            Assert.IsFalse(string.IsNullOrWhiteSpace(text), resource.Uri);
            Assert.DoesNotContain("There is no Bit.Bsqlite document", text, resource.Uri);
            Assert.DoesNotContain("is not a known scenario", text, resource.Uri);
        }
    }

    [TestMethod]
    public async Task The_templates_resolve_the_same_keys_the_tools_do()
    {
        Assert.AreEqual(BsqliteApiCatalog.RenderType("BsqliteDatabase"), await ReadAsync("bsqlite://api/BsqliteDatabase"));
        Assert.AreEqual(BsqliteStorageCatalog.Render("opfs"), await ReadAsync("bsqlite://storage/opfs"));
        Assert.AreEqual(BsqliteSetupGuide.Get("testing"), await ReadAsync("bsqlite://setup/testing"));
        Assert.AreEqual(BsqliteSourceCatalog.GetDocument("live-queries"), await ReadAsync("bsqlite://docs/live-queries"));
        Assert.AreEqual(BsqliteSourceCatalog.GetSourceFile("Demo/Client/Components/SessionLifecycle.razor"),
            await ReadAsync("bsqlite://source/Demo%2FClient%2FComponents%2FSessionLifecycle.razor"));
        Assert.AreEqual(await McpCall.TextAsync("GetBsqliteDocument", new() { ["name"] = "readme" }), await ReadAsync("bsqlite://guide"));
    }

    [TestMethod]
    public async Task An_unknown_key_reads_back_as_an_explanation()
    {
        Assert.Contains("no public type called 'Zebra'", await ReadAsync("bsqlite://api/Zebra"));
        Assert.Contains("No first-party storage backend is called 'floppy'", await ReadAsync("bsqlite://storage/floppy"));
    }

    // ---- Prompts -----------------------------------------------------------

    private static readonly (string Name, Dictionary<string, object?> Arguments)[] _prompts =
    [
        ("add-bsqlite-to-app", new() { ["hosting"] = "wasm" }),
        ("move-from-besql", new() { ["dataSource"] = "Offline-Client.db" }),
        ("debug-bsqlite-storage", new() { ["symptom"] = "the database is empty after a reload" }),
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

            foreach (var value in arguments.Values) Assert.Contains((string)value!, text, name);

            var mentioned = ToolNames.MentionedIn(text).ToArray();
            Assert.IsNotEmpty(mentioned, name);

            foreach (var tool in mentioned) CollectionAssert.Contains(McpSurfaceTests.ExpectedTools, tool, $"'{name}' mentions '{tool}', which is not a tool.");
        }
    }

    [TestMethod]
    public async Task The_prompts_name_only_types_the_library_has()
    {
        // A prompt that tells an agent to use a type the library does not have sends it straight into the
        // mistake the prompt exists to prevent.
        var types = BsqliteApiCatalog.Types.Select(type => type.Name.Split('<')[0]).ToHashSet(StringComparer.Ordinal);

        foreach (var (name, arguments) in _prompts)
        {
            var text = await PromptTextAsync(name, arguments);

            foreach (var type in System.Text.RegularExpressions.Regex.Matches(text, @"`(I?Bsqlite[A-Z][A-Za-z]*)`").Select(match => match.Groups[1].Value))
            {
                Assert.Contains(type, types, $"'{name}' names '{type}', which is not a public Bit.Bsqlite type.");
            }
        }
    }

    [TestMethod]
    public async Task The_prompts_that_take_a_default_answer_without_one()
    {
        var text = await PromptTextAsync("add-bsqlite-to-app", null);

        Assert.Contains("Its hosting model is: unknown.", text);
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
        var types = await CompleteAsync("bsqlite://api/{typeName}", "typeName", "BsqliteData");
        var names = await CompleteAsync("bsqlite://docs/{name}", "name", "live");
        var providers = await CompleteAsync("bsqlite://storage/{provider}", "provider", "index");
        var scenarios = await CompleteAsync("bsqlite://setup/{scenario}", "scenario", "besql");
        var paths = await CompleteAsync("bsqlite://source/{path}", "path", "SessionLifecycle");

        CollectionAssert.Contains(types, "BsqliteDatabase");
        Assert.StartsWith("BsqliteData", types[0], "A value the typed text starts should rank first.");
        CollectionAssert.Contains(names, "live-queries");
        CollectionAssert.Contains(providers, "indexeddb");
        CollectionAssert.Contains(scenarios, "move-from-besql");
        CollectionAssert.Contains(paths, "Demo/Client/Components/SessionLifecycle.razor");
    }

    [TestMethod]
    public async Task An_empty_prefix_completes_with_the_whole_set()
    {
        var scenarios = await CompleteAsync("bsqlite://setup/{scenario}", "scenario", "");

        CollectionAssert.AreEquivalent(BsqliteSetupGuide.Scenarios, scenarios);
    }

    [TestMethod]
    public async Task Prompt_arguments_complete_with_the_phrasings_they_were_written_for()
    {
        var symptoms = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "debug-bsqlite-storage" }, "symptom", "")).Completion.Values;
        var hosting = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "add-bsqlite-to-app" }, "hosting", "")).Completion.Values;

        Assert.IsNotEmpty(symptoms);
        CollectionAssert.IsSubsetOf(new[] { "unknown", "standalone-wasm", "wasm", "auto", "server" }, hosting.ToArray());
        Assert.HasCount(hosting.Distinct().Count(), hosting, "A value declared by [AllowedValues] should not be offered twice.");
    }

    private static async Task<string> ReadAsync(string uri)
    {
        var result = await McpTestHost.Client.ReadResourceAsync(uri);

        return string.Join('\n', result.Contents.OfType<TextResourceContents>().Select(contents => contents.Text));
    }

    private static async Task<string> PromptTextAsync(string name, Dictionary<string, object?>? arguments)
    {
        var result = await McpTestHost.Client.GetPromptAsync(name, arguments);

        return string.Join('\n', result.Messages.Select(message => message.Content).OfType<TextContentBlock>().Select(block => block.Text));
    }

    private static async Task<string[]> CompleteAsync(string template, string argument, string typed)
        => [.. (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = template }, argument, typed)).Completion.Values];
}
