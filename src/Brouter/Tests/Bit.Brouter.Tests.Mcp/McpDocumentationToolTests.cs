using Bit.Brouter.Demo.Client;
using Bit.Brouter.Demo.Server.Dtos;
using Bit.Brouter.Demo.Server.Services;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// The tools that hand out text an agent is meant to read and act on: the reference guide, the
/// documentation pages, the setup guides and the demo's source files.
/// <para>
/// Every one of them answers out of an embedded resource or a rendered component, so what these
/// tests really pin down is that the material is still there and still reachable - a renamed
/// heading, a mistyped <c>LogicalName</c> in the .csproj or a page that no longer renders all
/// produce a perfectly successful tool call carrying an apology instead of documentation.
/// </para>
/// <para>
/// Each of them also takes one key and answers with one thing - and answers with the index of what
/// there is when the key is left out. That second half is what a listing tool used to be, and it is
/// tested here as the same behavior it replaced: the index has to name everything the key could be.
/// </para>
/// </summary>
[TestClass]
public partial class McpDocumentationToolTests
{
    [TestMethod]
    public async Task The_guides_index_is_a_table_of_contents_in_reading_order()
    {
        // The index doubles as a table of contents: a sub-section has to follow the section it
        // belongs to, or a reader picking a heading off it lands somewhere else entirely. It also
        // carries each section's size, which is what makes fetching a three-hundred-line heading a
        // decision rather than a surprise.
        var index = await McpCall.TextAsync("GetBrouterGuideSection");

        var entries = index.Split('\n')
                           .Select(line => line.TrimEnd())
                           .Where(line => line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("  - ", StringComparison.Ordinal))
                           .ToArray();

        Assert.IsGreaterThan(20, entries.Length, $"Only {entries.Length} guide sections are in the index; the README has far more.");

        Assert.StartsWith("- Install", entries[0], "The guide no longer opens with its Install section.");

        foreach (var entry in entries)
        {
            Assert.MatchesRegex(LinesRegex(), entry, $"'{entry}' does not say how long the section is.");
        }

        // Every heading in the index has to be one the tool answers to - that is the whole point of
        // handing it over. The sub-sections are indented under the section they sit inside.
        foreach (var heading in new[] { "Install", "Data loader", "Loader caching (stale-while-revalidate)" })
        {
            Assert.Contains(heading, index, $"'{heading}' is missing from the guide index.");
        }

        Assert.Contains("GetBrouterGuideSection(heading:", index, "The index never says what to do with a heading.");
    }

    [TestMethod]
    public async Task A_guide_section_comes_back_with_its_heading_and_its_code()
    {
        var section = await McpCall.TextAsync("GetBrouterGuideSection", new() { ["heading"] = "Async guards" });

        Assert.StartsWith("## Async guards", section);
        Assert.Contains("```", section, "The section lost its code samples on the way out.");
    }

    [TestMethod]
    public async Task A_guide_section_is_found_regardless_of_case_and_punctuation()
    {
        // A model asking for "loader caching" must not be told the section does not exist because the
        // README spells it "Loader caching (stale-while-revalidate)".
        var section = await McpCall.TextAsync("GetBrouterGuideSection", new() { ["heading"] = "loader caching stale while revalidate" });

        Assert.Contains("Loader caching", section);
    }

    [TestMethod]
    public async Task A_guide_section_includes_its_sub_sections()
    {
        var dataLoader = await McpCall.TextAsync("GetBrouterGuideSection", new() { ["heading"] = "Data loader" });

        Assert.Contains("### Loader caching (stale-while-revalidate)", dataLoader,
            "The section was cut before its sub-sections, so half of what the reader asked for is missing.");
    }

    [TestMethod]
    public async Task An_unknown_guide_heading_answers_with_where_the_headings_are()
    {
        var answer = await McpCall.TextAsync("GetBrouterGuideSection", new() { ["heading"] = "Teleportation" });

        Assert.Contains("no section called 'Teleportation'", answer);
        Assert.Contains("GetBrouterGuideSection with no heading", answer, "The apology does not say how to find out what the guide does have.");
    }

    [TestMethod]
    public async Task A_partial_guide_heading_answers_with_the_headings_it_could_have_meant()
    {
        var answer = await McpCall.TextAsync("GetBrouterGuideSection", new() { ["heading"] = "guards" });

        Assert.Contains("Did you mean", answer);
        Assert.Contains("Async guards", answer, "The apology does not name the section the caller was reaching for.");
    }

    [TestMethod]
    public async Task The_documentation_index_carries_everything_needed_to_pick_a_page()
    {
        var index = await McpCall.TextAsync("GetBrouterDocsPage");

        foreach (var page in DocsCatalog.AllPages)
        {
            var slug = page.Slug.Length == 0 ? "overview" : page.Slug;

            Assert.Contains($"- `{slug}` - **{page.Title}**: {page.Description}", index,
                $"'{page.Title}' is not in the documentation index, or is not described there.");
        }

        foreach (var section in DocsCatalog.Sections)
        {
            Assert.Contains($"## {section.Title}", index, $"The index does not group its pages under '{section.Title}'.");
        }

        Assert.Contains("GetBrouterDocsPage(slug:", index, "The index never says what to do with a slug.");
    }

    [TestMethod]
    public async Task A_documentation_page_comes_back_as_markdown_with_its_source_url()
    {
        var page = await McpCall.TextAsync("GetBrouterDocsPage", new() { ["slug"] = "guards" });

        Assert.StartsWith("Bit.Brouter documentation page: /docs/guards", page);
        Assert.Contains("# Guards", page, "The page's own heading is missing, so the render produced no content.");
        Assert.Contains("LeaveGuard", page);
        Assert.IsFalse(page.Contains("could not be rendered", StringComparison.Ordinal));

        // Flattened to Markdown rather than handed over as the HTML the browser gets.
        Assert.IsFalse(page.Contains("<div", StringComparison.OrdinalIgnoreCase), "The page came back as HTML.");
        Assert.IsFalse(page.Contains("class=\"", StringComparison.Ordinal), "The page's CSS classes reached the client.");
    }

    [TestMethod]
    public async Task The_documentation_overview_answers_to_the_words_a_caller_would_actually_use()
    {
        // Its real slug is the empty string, which nobody types - and which is also how the tool is
        // called when it is asked for the index, so the words that stand in for it have to work.
        var byAlias = await McpCall.TextAsync("GetBrouterDocsPage", new() { ["slug"] = "overview" });

        Assert.StartsWith("Bit.Brouter documentation page: /docs", byAlias);

        foreach (var alias in new[] { "index", "docs" })
        {
            Assert.AreEqual(byAlias, await McpCall.TextAsync("GetBrouterDocsPage", new() { ["slug"] = alias }),
                $"'{alias}' does not resolve to the documentation overview.");
        }
    }

    [TestMethod]
    public async Task An_unknown_slug_answers_with_the_slugs_that_do_exist()
    {
        var answer = await McpCall.TextAsync("GetBrouterDocsPage", new() { ["slug"] = "guardz" });

        Assert.Contains("No documentation page has the slug 'guardz'", answer);
        Assert.Contains("guards", answer, "The apology does not list the slugs that do exist.");
    }

    [TestMethod]
    public async Task The_source_index_includes_the_route_table_and_a_sample_per_render_mode()
    {
        var index = await McpCall.TextAsync("GetBrouterSourceFile");

        Assert.Contains("`Demo/Client/AppRouter.razor`", index);

        foreach (var prefix in new[] { "Sample/Server/", "Sample/Wasm/", "Sample/Auto/", "Sample/Maui/", "Sample/Core/Extensions/" })
        {
            Assert.Contains($"`{prefix}", index,
                $"No source file under '{prefix}' was embedded, so the setup guide that quotes it has nothing to show.");
        }

        foreach (var file in BrouterSourceCatalog.SourceFiles)
        {
            Assert.IsTrue(file.Kind is "Demo" or "Sample", $"'{file.Path}' is listed as '{file.Kind}'.");
            Assert.IsGreaterThan(0, file.Lines, $"'{file.Path}' is listed as empty.");
            Assert.IsFalse(file.Path.StartsWith("BrouterSource", StringComparison.Ordinal),
                $"'{file.Path}' still carries the resource prefix a caller is not supposed to know about.");
            Assert.IsFalse(file.Path.Contains('\\'), $"'{file.Path}' is spelled with a backslash, which no caller would type.");

            Assert.Contains($"`{file.Path}` ({file.Lines} lines)", index, $"'{file.Path}' is not in the source index.");
        }

        Assert.Contains("GetBrouterSourceFile(path:", index, "The index never says what to do with a path.");
    }

    [TestMethod]
    public async Task A_source_file_comes_back_verbatim()
    {
        var file = await McpCall.TextAsync("GetBrouterSourceFile", new() { ["path"] = "Demo/Client/AppRouter.razor" });

        Assert.Contains("<Brouter", file, "The demo's route table is not in the file that is supposed to hold it.");
        Assert.Contains("<Broute", file);
        Assert.AreEqual(BrouterSourceCatalog.GetSourceFile("Demo/Client/AppRouter.razor"), file);
    }

    [TestMethod]
    public async Task A_source_path_is_matched_the_way_a_caller_would_type_it()
    {
        var file = await McpCall.TextAsync("GetBrouterSourceFile", new() { ["path"] = "demo/client/approuter.razor" });

        Assert.Contains("<Brouter", file);
    }

    [TestMethod]
    public async Task A_partial_source_path_answers_with_the_paths_it_could_have_meant()
    {
        var answer = await McpCall.TextAsync("GetBrouterSourceFile", new() { ["path"] = "AppRouter" });

        Assert.Contains("No source file at 'AppRouter'", answer);
        Assert.Contains("Demo/Client/AppRouter.razor", answer);
    }

    [TestMethod]
    public async Task An_unrecognizable_source_path_points_at_the_index()
    {
        var answer = await McpCall.TextAsync("GetBrouterSourceFile", new() { ["path"] = "../../../etc/passwd" });

        Assert.Contains("GetBrouterSourceFile with no path", answer);

        // Pointing at the listing tool is only the polite half; the answer must also be about
        // nothing, rather than the contents of whatever the path climbed out to.
        Assert.StartsWith("No source file at '../../../etc/passwd'", answer);
        Assert.IsFalse(answer.Contains("root:", StringComparison.Ordinal), "The traversal path handed back the file it climbed out to.");
    }

    [TestMethod]
    public async Task Every_setup_guide_hands_over_the_wiring_that_render_mode_needs()
    {
        foreach (var renderMode in BrouterSetupGuide.RenderModes)
        {
            var guide = await McpCall.TextAsync("GetBrouterSetupGuide", new() { ["renderMode"] = renderMode });

            Assert.Contains("AddBitBrouterServices", guide, $"The '{renderMode}' guide never says to register the services.");
            Assert.Contains("## Checklist", guide, $"The '{renderMode}' guide has no checklist.");
            Assert.Contains("@using Bit.Brouter", guide, $"The '{renderMode}' guide never mentions the _Imports entry.");
            Assert.IsGreaterThan(1_500, guide.Length, $"The '{renderMode}' guide is only {guide.Length} characters - the sample files are missing.");
        }
    }

    [TestMethod]
    public async Task The_hosted_render_modes_quote_real_project_files()
    {
        // Prose about which container registers what is exactly the thing people get wrong; the guide
        // is supposed to hand over a compiling project instead.
        foreach (var renderMode in new[] { "server", "wasm", "auto", "hybrid" })
        {
            var guide = await McpCall.TextAsync("GetBrouterSetupGuide", new() { ["renderMode"] = renderMode });

            // MauiProgram.cs included: it is the Hybrid app's Program.cs.
            Assert.Contains("Program.cs", guide, $"The '{renderMode}' guide quotes no Program.cs.");
            Assert.Contains("```csharp", guide, $"The '{renderMode}' guide contains no C# code block.");
            Assert.Contains("Sample/Core/Extensions/IServiceCollectionExtensions.cs", guide,
                $"The '{renderMode}' guide leaves out the shared registration method both containers call.");
        }
    }

    [TestMethod]
    public async Task A_standalone_wasm_app_is_told_it_needs_no_host_page()
    {
        var guide = await McpCall.TextAsync("GetBrouterSetupGuide", new() { ["renderMode"] = "standalone-wasm" });

        Assert.Contains("no server-rendered host page", guide);
        Assert.Contains("App.razor", guide);
    }

    [TestMethod]
    public async Task A_hybrid_app_is_told_it_needs_no_host_page_and_shown_its_root_component()
    {
        var guide = await McpCall.TextAsync("GetBrouterSetupGuide", new() { ["renderMode"] = "hybrid" });

        Assert.Contains("no catch-all host page", guide);

        // The WebView's root component and its host page are where a Hybrid setup differs from a
        // Web App's; a guide without them leaves the reader to guess at both.
        Assert.Contains("### `Sample/Maui/MainPage.xaml`", guide);
        Assert.Contains("### `Sample/Maui/wwwroot/index.html`", guide);
        Assert.Contains("BlazorWebView", guide);
    }

    [TestMethod]
    public async Task An_unknown_render_mode_answers_with_the_ones_that_exist()
    {
        var answer = await McpCall.TextAsync("GetBrouterSetupGuide", new() { ["renderMode"] = "blazor" });

        Assert.Contains("not a known render mode", answer);

        foreach (var renderMode in BrouterSetupGuide.RenderModes) Assert.Contains(renderMode, answer);
    }

    // Every entry of the guide index ends in the section's size: "- Data loader (36 lines)".
    [System.Text.RegularExpressions.GeneratedRegex(@"\(\d+ lines\)$")]
    private static partial System.Text.RegularExpressions.Regex LinesRegex();
}
