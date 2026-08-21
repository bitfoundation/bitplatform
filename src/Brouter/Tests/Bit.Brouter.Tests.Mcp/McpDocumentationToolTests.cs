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

        Assert.IsTrue(entries.Length > 20, $"Only {entries.Length} guide sections are in the index; the README has far more.");

        StringAssert.StartsWith(entries[0], "- Install", "The guide no longer opens with its Install section.");

        foreach (var entry in entries)
        {
            StringAssert.Matches(entry, LinesRegex(), $"'{entry}' does not say how long the section is.");
        }

        // Every heading in the index has to be one the tool answers to - that is the whole point of
        // handing it over. The sub-sections are indented under the section they sit inside.
        foreach (var heading in new[] { "Install", "Data loader", "Loader caching (stale-while-revalidate)" })
        {
            StringAssert.Contains(index, heading, $"'{heading}' is missing from the guide index.");
        }

        StringAssert.Contains(index, "GetBrouterGuideSection(heading:", "The index never says what to do with a heading.");
    }

    [TestMethod]
    public async Task A_guide_section_comes_back_with_its_heading_and_its_code()
    {
        var section = await McpCall.TextAsync("GetBrouterGuideSection", new() { ["heading"] = "Async guards" });

        StringAssert.StartsWith(section, "## Async guards");
        StringAssert.Contains(section, "```", "The section lost its code samples on the way out.");
    }

    [TestMethod]
    public async Task A_guide_section_is_found_regardless_of_case_and_punctuation()
    {
        // A model asking for "loader caching" must not be told the section does not exist because the
        // README spells it "Loader caching (stale-while-revalidate)".
        var section = await McpCall.TextAsync("GetBrouterGuideSection", new() { ["heading"] = "loader caching stale while revalidate" });

        StringAssert.Contains(section, "Loader caching");
    }

    [TestMethod]
    public async Task A_guide_section_includes_its_sub_sections()
    {
        var dataLoader = await McpCall.TextAsync("GetBrouterGuideSection", new() { ["heading"] = "Data loader" });

        StringAssert.Contains(dataLoader, "### Loader caching (stale-while-revalidate)",
            "The section was cut before its sub-sections, so half of what the reader asked for is missing.");
    }

    [TestMethod]
    public async Task An_unknown_guide_heading_answers_with_where_the_headings_are()
    {
        var answer = await McpCall.TextAsync("GetBrouterGuideSection", new() { ["heading"] = "Teleportation" });

        StringAssert.Contains(answer, "no section called 'Teleportation'");
        StringAssert.Contains(answer, "GetBrouterGuideSection with no heading", "The apology does not say how to find out what the guide does have.");
    }

    [TestMethod]
    public async Task A_partial_guide_heading_answers_with_the_headings_it_could_have_meant()
    {
        var answer = await McpCall.TextAsync("GetBrouterGuideSection", new() { ["heading"] = "guards" });

        StringAssert.Contains(answer, "Did you mean");
        StringAssert.Contains(answer, "Async guards", "The apology does not name the section the caller was reaching for.");
    }

    [TestMethod]
    public async Task The_documentation_index_carries_everything_needed_to_pick_a_page()
    {
        var index = await McpCall.TextAsync("GetBrouterDocsPage");

        foreach (var page in DocsCatalog.AllPages)
        {
            var slug = page.Slug.Length == 0 ? "overview" : page.Slug;

            StringAssert.Contains(index, $"- `{slug}` - **{page.Title}**: {page.Description}",
                $"'{page.Title}' is not in the documentation index, or is not described there.");
        }

        foreach (var section in DocsCatalog.Sections)
        {
            StringAssert.Contains(index, $"## {section.Title}", $"The index does not group its pages under '{section.Title}'.");
        }

        StringAssert.Contains(index, "GetBrouterDocsPage(slug:", "The index never says what to do with a slug.");
    }

    [TestMethod]
    public async Task A_documentation_page_comes_back_as_markdown_with_its_source_url()
    {
        var page = await McpCall.TextAsync("GetBrouterDocsPage", new() { ["slug"] = "guards" });

        StringAssert.StartsWith(page, "Bit.Brouter documentation page: /docs/guards");
        StringAssert.Contains(page, "# Guards", "The page's own heading is missing, so the render produced no content.");
        StringAssert.Contains(page, "LeaveGuard");
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

        StringAssert.StartsWith(byAlias, "Bit.Brouter documentation page: /docs");

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

        StringAssert.Contains(answer, "No documentation page has the slug 'guardz'");
        StringAssert.Contains(answer, "guards", "The apology does not list the slugs that do exist.");
    }

    [TestMethod]
    public async Task The_source_index_includes_the_route_table_and_a_sample_per_render_mode()
    {
        var index = await McpCall.TextAsync("GetBrouterSourceFile");

        StringAssert.Contains(index, "`Demo/Client/AppRouter.razor`");

        foreach (var prefix in new[] { "Sample/Server/", "Sample/Wasm/", "Sample/Auto/", "Sample/Core/Extensions/" })
        {
            StringAssert.Contains(index, $"`{prefix}",
                $"No source file under '{prefix}' was embedded, so the setup guide that quotes it has nothing to show.");
        }

        foreach (var file in BrouterSourceCatalog.SourceFiles)
        {
            Assert.IsTrue(file.Kind is "Demo" or "Sample", $"'{file.Path}' is listed as '{file.Kind}'.");
            Assert.IsTrue(file.Lines > 0, $"'{file.Path}' is listed as empty.");
            Assert.IsFalse(file.Path.StartsWith("BrouterSource", StringComparison.Ordinal),
                $"'{file.Path}' still carries the resource prefix a caller is not supposed to know about.");
            Assert.IsFalse(file.Path.Contains('\\'), $"'{file.Path}' is spelled with a backslash, which no caller would type.");

            StringAssert.Contains(index, $"`{file.Path}` ({file.Lines} lines)", $"'{file.Path}' is not in the source index.");
        }

        StringAssert.Contains(index, "GetBrouterSourceFile(path:", "The index never says what to do with a path.");
    }

    [TestMethod]
    public async Task A_source_file_comes_back_verbatim()
    {
        var file = await McpCall.TextAsync("GetBrouterSourceFile", new() { ["path"] = "Demo/Client/AppRouter.razor" });

        StringAssert.Contains(file, "<Brouter", "The demo's route table is not in the file that is supposed to hold it.");
        StringAssert.Contains(file, "<Broute");
        Assert.AreEqual(BrouterSourceCatalog.GetSourceFile("Demo/Client/AppRouter.razor"), file);
    }

    [TestMethod]
    public async Task A_source_path_is_matched_the_way_a_caller_would_type_it()
    {
        var file = await McpCall.TextAsync("GetBrouterSourceFile", new() { ["path"] = "demo/client/approuter.razor" });

        StringAssert.Contains(file, "<Brouter");
    }

    [TestMethod]
    public async Task A_partial_source_path_answers_with_the_paths_it_could_have_meant()
    {
        var answer = await McpCall.TextAsync("GetBrouterSourceFile", new() { ["path"] = "AppRouter" });

        StringAssert.Contains(answer, "No source file at 'AppRouter'");
        StringAssert.Contains(answer, "Demo/Client/AppRouter.razor");
    }

    [TestMethod]
    public async Task An_unrecognizable_source_path_points_at_the_index()
    {
        var answer = await McpCall.TextAsync("GetBrouterSourceFile", new() { ["path"] = "../../../etc/passwd" });

        StringAssert.Contains(answer, "GetBrouterSourceFile with no path");

        // Pointing at the listing tool is only the polite half; the answer must also be about
        // nothing, rather than the contents of whatever the path climbed out to.
        StringAssert.StartsWith(answer, "No source file at '../../../etc/passwd'");
        Assert.IsFalse(answer.Contains("root:", StringComparison.Ordinal), "The traversal path handed back the file it climbed out to.");
    }

    [TestMethod]
    public async Task Every_setup_guide_hands_over_the_wiring_that_render_mode_needs()
    {
        foreach (var renderMode in BrouterSetupGuide.RenderModes)
        {
            var guide = await McpCall.TextAsync("GetBrouterSetupGuide", new() { ["renderMode"] = renderMode });

            StringAssert.Contains(guide, "AddBitBrouterServices", $"The '{renderMode}' guide never says to register the services.");
            StringAssert.Contains(guide, "## Checklist", $"The '{renderMode}' guide has no checklist.");
            StringAssert.Contains(guide, "@using Bit.Brouter", $"The '{renderMode}' guide never mentions the _Imports entry.");
            Assert.IsTrue(guide.Length > 1_500, $"The '{renderMode}' guide is only {guide.Length} characters - the sample files are missing.");
        }
    }

    [TestMethod]
    public async Task The_hosted_render_modes_quote_real_project_files()
    {
        // Prose about which container registers what is exactly the thing people get wrong; the guide
        // is supposed to hand over a compiling project instead.
        foreach (var renderMode in new[] { "server", "wasm", "auto" })
        {
            var guide = await McpCall.TextAsync("GetBrouterSetupGuide", new() { ["renderMode"] = renderMode });

            StringAssert.Contains(guide, "Program.cs", $"The '{renderMode}' guide quotes no Program.cs.");
            StringAssert.Contains(guide, "```csharp", $"The '{renderMode}' guide contains no C# code block.");
            StringAssert.Contains(guide, "Sample/Core/Extensions/IServiceCollectionExtensions.cs",
                $"The '{renderMode}' guide leaves out the shared registration method both containers call.");
        }
    }

    [TestMethod]
    public async Task A_standalone_wasm_app_is_told_it_needs_no_host_page()
    {
        var guide = await McpCall.TextAsync("GetBrouterSetupGuide", new() { ["renderMode"] = "standalone-wasm" });

        StringAssert.Contains(guide, "no server-rendered host page");
        StringAssert.Contains(guide, "App.razor");
    }

    [TestMethod]
    public async Task An_unknown_render_mode_answers_with_the_four_that_exist()
    {
        var answer = await McpCall.TextAsync("GetBrouterSetupGuide", new() { ["renderMode"] = "blazor" });

        StringAssert.Contains(answer, "not a known render mode");

        foreach (var renderMode in BrouterSetupGuide.RenderModes) StringAssert.Contains(answer, renderMode);
    }

    // Every entry of the guide index ends in the section's size: "- Data loader (36 lines)".
    [System.Text.RegularExpressions.GeneratedRegex(@"\(\d+ lines\)$")]
    private static partial System.Text.RegularExpressions.Regex LinesRegex();
}
