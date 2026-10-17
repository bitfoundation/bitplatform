using Bit.Bink.Demo.Server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Bit.Bink.Tests.Mcp;

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

        CollectionAssert.IsSubsetOf(new[] { "bink://guide", "bink://api", "bink://brushes", "bink://sample-document" }, uris);
        CollectionAssert.Contains(uris, "bink://guide/Getting%20started");
        CollectionAssert.Contains(uris, "bink://guide/SVG%20and%20PNG%20export");
    }

    [TestMethod]
    public async Task The_templates_are_published()
    {
        var templates = (await McpTestHost.Client.ListResourceTemplatesAsync()).Select(template => template.UriTemplate).ToArray();

        CollectionAssert.IsSubsetOf(new[] { "bink://guide/{heading}", "bink://api/{typeName}", "bink://brushes/{kind}", "bink://source/{path}" }, templates);
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
        Assert.AreEqual(BinkApiCatalog.RenderType("BinkCanvas"), await ReadAsync("bink://api/BinkCanvas"));
        Assert.AreEqual(BinkBrushCatalog.Render("pen"), await ReadAsync("bink://brushes/pen"));
        Assert.AreEqual(BinkSourceCatalog.GetSourceFile("Demo/Client/Pages/Signature.razor"), await ReadAsync("bink://source/Demo%2FClient%2FPages%2FSignature.razor"));
        Assert.StartsWith("## Serialization", await ReadAsync("bink://guide/Serialization"));
    }

    [TestMethod]
    public async Task The_sample_document_resource_is_the_one_the_lab_hands_out()
    {
        var sample = await ReadAsync("bink://sample-document");

        Assert.AreEqual(BinkDocumentLab.Sample, sample);
        Assert.IsTrue(BinkSerializer.TryDeserialize(sample, out _, out var error), error);
    }

    // ---- Prompts -----------------------------------------------------------

    private static readonly (string Name, Dictionary<string, object?>? Arguments)[] _prompts =
    [
        ("add-bink-to-app", new() { ["renderMode"] = "wasm" }),
        ("implement-bink-feature", new() { ["feature"] = "a signature pad that exports a PNG" }),
        ("debug-bink-canvas", new() { ["symptom"] = "the exported PNG is missing the text" }),
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
        var types = BinkApiCatalog.Types.Select(type => type.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var (name, arguments) in _prompts)
        {
            var result = await McpTestHost.Client.GetPromptAsync(name, arguments);
            var text = string.Join('\n', result.Messages.Select(message => message.Content).OfType<TextContentBlock>().Select(block => block.Text));

            foreach (var type in System.Text.RegularExpressions.Regex.Matches(text, @"`(I?Bink[A-Z][A-Za-z]*)`").Select(match => match.Groups[1].Value))
            {
                Assert.Contains(type, types, $"'{name}' names '{type}', which is not a public Bit.Bink type.");
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
        var types = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "bink://api/{typeName}" }, "typeName", "BinkCanvas")).Completion.Values;
        var headings = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "bink://guide/{heading}" }, "heading", "serial")).Completion.Values;
        var kinds = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "bink://brushes/{kind}" }, "kind", "high")).Completion.Values;
        var paths = (await McpTestHost.Client.CompleteAsync(new ResourceTemplateReference { Uri = "bink://source/{path}" }, "path", "Signature")).Completion.Values;

        CollectionAssert.Contains(types.ToArray(), "BinkCanvas");
        Assert.AreEqual("BinkCanvas", types[0], "A value the typed text starts should rank first.");
        CollectionAssert.Contains(headings.ToArray(), "Serialization");
        CollectionAssert.Contains(kinds.ToArray(), "highlighter");
        CollectionAssert.Contains(paths.ToArray(), "Demo/Client/Pages/Signature.razor");
    }

    [TestMethod]
    public async Task Prompt_arguments_complete_with_the_phrasings_they_were_written_for()
    {
        var symptoms = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "debug-bink-canvas" }, "symptom", "")).Completion.Values;
        var modes = (await McpTestHost.Client.CompleteAsync(new PromptReference { Name = "add-bink-to-app" }, "renderMode", "")).Completion.Values;

        Assert.IsNotEmpty(symptoms);
        CollectionAssert.IsSubsetOf(BinkSetupGuide.RenderModes, modes.ToArray());
    }

    private static async Task<string> ReadAsync(string uri)
    {
        var result = await McpTestHost.Client.ReadResourceAsync(uri);

        return string.Join('\n', result.Contents.OfType<TextResourceContents>().Select(contents => contents.Text));
    }
}
