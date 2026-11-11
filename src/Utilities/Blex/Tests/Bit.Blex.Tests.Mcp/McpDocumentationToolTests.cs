using Bit.Blex.Demo.Server.Services;

namespace Bit.Blex.Tests.Mcp;

/// <summary>
/// The prose tools - the guide, the docs pages, the setup guides and the source files - answering with
/// the material, with the index when their key is left out, and with a way forward when the key is wrong.
/// </summary>
[TestClass]
public class McpDocumentationToolTests
{
    [TestMethod]
    public async Task Every_docs_page_renders_through_the_tool()
    {
        // The pages render outside the app's layout and <BlexProvider>. A page that reaches for JS interop
        // or for a service the server does not register while initializing throws there and only there -
        // so this is the test that catches it.
        Assert.IsGreaterThanOrEqualTo(19, DocsPageRenderer.Pages.Length, $"Only {DocsPageRenderer.Pages.Length} pages were found in the site map.");

        foreach (var page in DocsPageRenderer.Pages)
        {
            var markdown = await McpCall.TextAsync("GetBlexDocsPage", new() { ["slug"] = page.Slug });

            Assert.IsFalse(markdown.Contains("could not be rendered", StringComparison.Ordinal),
                $"The '{page.Title}' page did not render: {markdown[..Math.Min(400, markdown.Length)]}");
            Assert.IsGreaterThan(500, markdown.Length, $"The '{page.Title}' page came back as {markdown.Length} characters, which is not a page.");
            // Not the sidebar title: the introduction's heading is its tagline.
            Assert.Contains("\n# ", markdown, $"The '{page.Title}' page came back without its heading.");
        }
    }

    [TestMethod]
    public async Task A_docs_page_keeps_its_code_samples_as_fenced_blocks()
    {
        var markdown = await McpCall.TextAsync("GetBlexDocsPage", new() { ["slug"] = "effects" });

        Assert.Contains("```", markdown);
        Assert.Contains("[BlexEffect]", markdown);
        Assert.IsFalse(markdown.Contains("<span", StringComparison.Ordinal), "Syntax-highlighting markup leaked into the Markdown.");
    }

    [TestMethod]
    public async Task The_root_page_answers_to_the_words_a_caller_reaches_for()
    {
        var introduction = await McpCall.TextAsync("GetBlexDocsPage", new() { ["slug"] = "introduction" });

        foreach (var alias in new[] { "overview", "/", "Introduction" })
        {
            Assert.AreEqual(introduction, await McpCall.TextAsync("GetBlexDocsPage", new() { ["slug"] = alias }), $"'{alias}' does not reach the introduction.");
        }
    }

    [TestMethod]
    public async Task Every_guide_section_is_reachable_by_its_heading()
    {
        Assert.IsGreaterThan(20, BlexSourceCatalog.GuideSections.Length, "The README was not embedded, or lost its headings.");

        foreach (var section in BlexSourceCatalog.GuideSections)
        {
            var text = await McpCall.TextAsync("GetBlexGuideSection", new() { ["heading"] = section.Heading });

            Assert.StartsWith($"{new string('#', section.Level)} {section.Heading}", text);
        }
    }

    [TestMethod]
    public async Task A_guide_heading_ignores_case_and_punctuation()
    {
        var exact = await McpCall.TextAsync("GetBlexGuideSection", new() { ["heading"] = ".NET MAUI" });
        var loose = await McpCall.TextAsync("GetBlexGuideSection", new() { ["heading"] = "net maui" });

        Assert.AreEqual(exact, loose);
        Assert.Contains("### Blazor Hybrid", exact, "A section does not carry its sub-sections.");
    }

    [TestMethod]
    public async Task Every_setup_guide_carries_the_checklist_and_working_code()
    {
        foreach (var model in BlexSetupGuide.HostingModels)
        {
            var guide = await McpCall.TextAsync("GetBlexSetupGuide", new() { ["hostingModel"] = model });

            Assert.StartsWith("# Setting Blex up", guide);
            Assert.Contains("## Checklist", guide, $"The '{model}' guide has no checklist.");
            Assert.Contains("`dotnet add package Bit.Blex` in every project that declares stores", guide, $"The '{model}' guide never tells the reader to add the package that carries the generator.");
            Assert.Contains("```", guide, $"The '{model}' guide shows no code.");
        }
    }

    [TestMethod]
    public async Task The_web_app_guide_registers_in_both_containers_from_one_method()
    {
        var guide = await McpCall.TextAsync("GetBlexSetupGuide", new() { ["hostingModel"] = "wasm" });

        Assert.Contains("### `Demo/Client/Extensions/IServiceCollectionExtensions.cs`", guide);
        Assert.Contains("AddBitBlexServices", guide);
        Assert.Contains("<BlexProvider>", guide);
        Assert.Contains("BOTH", guide);
    }

    [TestMethod]
    public async Task The_native_maui_guide_uses_the_singleton_builder_extensions()
    {
        var guide = await McpCall.TextAsync("GetBlexSetupGuide", new() { ["hostingModel"] = "maui" });

        Assert.Contains("UseBitBlex", guide);
        Assert.Contains("AddBitBlexPreferencesPersistence", guide);
    }

    [TestMethod]
    public async Task Every_listed_source_file_is_handed_out_verbatim()
    {
        Assert.IsGreaterThan(15, BlexSourceCatalog.SourceFiles.Length, "The demo and sample sources were not embedded.");

        foreach (var file in BlexSourceCatalog.SourceFiles)
        {
            var text = await McpCall.TextAsync("GetBlexSourceFile", new() { ["path"] = file.Path });

            Assert.AreEqual(BlexSourceCatalog.GetSourceFile(file.Path)!.Length, text.Length, $"'{file.Path}' was not handed out whole.");
        }
    }

    [TestMethod]
    public void The_source_catalog_hands_out_examples_and_not_site_chrome()
    {
        var paths = BlexSourceCatalog.SourceFiles.Select(file => file.Path).ToArray();

        Assert.Contains("Demo/Client/Stores/CartStore.cs", paths);
        Assert.Contains("Demo/Client/Extensions/IServiceCollectionExtensions.cs", paths);
        Assert.Contains("Sample/Standalone/Program.cs", paths);
        Assert.Contains("Sample/MAUI/MauiProgram.cs", paths);

        // A documentation page's source is its prose, which the docs tool already serves rendered.
        Assert.DoesNotContain(path => path.EndsWith("/Effects.razor", StringComparison.OrdinalIgnoreCase), paths, "A docs page leaked into the source catalog.");
        Assert.DoesNotContain(path => path.Contains("CodeHighlighter", StringComparison.Ordinal), paths, "Site chrome leaked into the source catalog.");
    }

    [TestMethod]
    public async Task A_wrong_key_answers_with_the_way_forward()
    {
        Assert.Contains("Available slugs:", await McpCall.TextAsync("GetBlexDocsPage", new() { ["slug"] = "nope" }));
        Assert.Contains("Did you mean:", await McpCall.TextAsync("GetBlexGuideSection", new() { ["heading"] = "Persist" }));
        Assert.Contains("Did you mean:", await McpCall.TextAsync("GetBlexSourceFile", new() { ["path"] = "CartStore" }));
        Assert.Contains("is not a known hosting model", await McpCall.TextAsync("GetBlexSetupGuide", new() { ["hostingModel"] = "silverlight" }));
    }
}
