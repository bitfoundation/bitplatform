using Bit.BbScanner.Demo.Server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Bit.BbScanner.Tests.Mcp;

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

        CollectionAssert.IsSubsetOf(new[] { "bbscanner://guide", "bbscanner://api", "bbscanner://formats" }, uris);
        CollectionAssert.Contains(uris, "bbscanner://guide/Getting%20started");
        CollectionAssert.Contains(uris, "bbscanner://guide/PDF417%20status");
    }

    [TestMethod]
    public async Task The_templates_are_published()
    {
        var templates = (await McpTestHost.Client.ListResourceTemplatesAsync()).Select(template => template.UriTemplate).ToArray();

        CollectionAssert.IsSubsetOf(new[] { "bbscanner://guide/{heading}", "bbscanner://api/{typeName}", "bbscanner://formats/{format}", "bbscanner://source/{path}" }, templates);
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
        Assert.AreEqual(BbScannerApiCatalog.RenderType("BbScanner"), await ReadAsync("bbscanner://api/BbScanner"));
        Assert.AreEqual(BbScannerFormatCatalog.Render("QrCode"), await ReadAsync("bbscanner://formats/QrCode"));
        Assert.AreEqual(BbScannerSourceCatalog.GetSourceFile("Demo/Client/Pages/Scan.razor"), await ReadAsync("bbscanner://source/Demo%2FClient%2FPages%2FScan.razor"));
        Assert.StartsWith("## Symbologies", await ReadAsync("bbscanner://guide/Symbologies"));
    }

    // ---- Prompts -----------------------------------------------------------

    private static readonly (string Name, Dictionary<string, object?>? Arguments)[] _prompts =
    [
        ("add-bbscanner-to-app", new() { ["renderMode"] = "wasm" }),
        ("implement-bbscanner-feature", new() { ["feature"] = "scan a barcode from an uploaded photo" }),
        ("debug-bbscanner", new() { ["symptom"] = "the scanner shows NotSupported instead of the camera" }),
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
    public async Task The_prompts_name_only_types_the_library_has()
    {
        // A prompt that tells an agent to use a type the library does not have sends it straight into
        // the mistake the prompt exists to prevent.
        var types = BbScannerApiCatalog.Types.Select(type => type.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var (name, arguments) in _prompts)
        {
            var result = await McpTestHost.Client.GetPromptAsync(name, arguments);
            var text = string.Join('\n', result.Messages.Select(message => message.Content).OfType<TextContentBlock>().Select(block => block.Text));

            foreach (var type in System.Text.RegularExpressions.Regex.Matches(text, @"`(I?Bb[A-Z][A-Za-z]*)`").Select(match => match.Groups[1].Value))
            {
                Assert.Contains(type, types, $"'{name}' names '{type}', which is not a public Bit.BbScanner type.");
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
        var types = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "bbscanner://api/{typeName}" }, "typeName", "BbScanner")).Completion.Values;
        var headings = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "bbscanner://guide/{heading}" }, "heading", "symbol")).Completion.Values;
        var formats = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "bbscanner://formats/{format}" }, "format", "ean")).Completion.Values;
        var paths = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "bbscanner://source/{path}" }, "path", "Scan")).Completion.Values;

        CollectionAssert.Contains(types.ToArray(), "BbScanner");
        Assert.AreEqual("BbScanner", types[0], "A value the typed text starts should rank first.");
        CollectionAssert.Contains(headings.ToArray(), "Symbologies");
        CollectionAssert.Contains(formats.ToArray(), "Ean13");
        CollectionAssert.Contains(paths.ToArray(), "Demo/Client/Pages/Scan.razor");
    }

    [TestMethod]
    public async Task Prompt_arguments_complete_with_the_phrasings_they_were_written_for()
    {
        var symptoms = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "debug-bbscanner" }, "symptom", "")).Completion.Values;
        var modes = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "add-bbscanner-to-app" }, "renderMode", "")).Completion.Values;

        Assert.IsNotEmpty(symptoms);
        CollectionAssert.IsSubsetOf(BbScannerSetupGuide.RenderModes, modes.ToArray());
    }

    private static async Task<string> ReadAsync(string uri)
    {
        var result = await McpTestHost.Client.ReadResourceAsync(uri);

        return string.Join('\n', result.Contents.OfType<TextResourceContents>().Select(contents => contents.Text));
    }
}
