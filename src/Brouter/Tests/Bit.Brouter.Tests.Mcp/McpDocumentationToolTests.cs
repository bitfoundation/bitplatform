using Bit.Brouter.Demo.Client;
using Bit.Brouter.Demo.Server.Dtos;
using Bit.Brouter.Demo.Server.Services;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// The tools that hand out text an agent is meant to read and act on: the overview, the reference
/// guide, the documentation pages, the setup guides and the demo's source files.
/// <para>
/// Every one of them answers out of an embedded resource or a rendered component, so what these
/// tests really pin down is that the material is still there and still reachable - a renamed
/// heading, a mistyped <c>LogicalName</c> in the .csproj or a page that no longer renders all
/// produce a perfectly successful tool call carrying an apology instead of documentation.
/// </para>
/// </summary>
[TestClass]
public class McpDocumentationToolTests
{
    [TestMethod]
    public async Task Overview_opens_with_the_readme_and_names_the_build_it_answers_from()
    {
        var overview = await McpCall.TextAsync("GetBrouterOverview");

        StringAssert.Contains(overview, "Blazor");
        StringAssert.Contains(overview, $"Bit.Brouter {BrouterServerInstructions.BrouterVersion}");
    }

    [TestMethod]
    public async Task Overview_carries_the_three_guide_sections_it_inlines()
    {
        var overview = await McpCall.TextAsync("GetBrouterOverview");

        foreach (var heading in new[] { "Install", "Quick start", "Features" })
        {
            StringAssert.Contains(overview, $"## {heading}",
                $"The overview inlines the guide's '{heading}' section, and it is not in the answer.");

            Assert.IsFalse(overview.Contains($"The guide's \"{heading}\" section was not found", StringComparison.Ordinal),
                $"The guide's '{heading}' heading was renamed, so the overview now hands out an apology in its place.");
        }
    }

    [TestMethod]
    public async Task Overview_routes_the_reader_to_every_tool_this_server_has()
    {
        // The overview is the entry point: a tool it does not mention is a tool an agent that starts
        // here never learns about.
        var overview = await McpCall.TextAsync("GetBrouterOverview");
        var mentioned = ToolNames.MentionedIn(overview).ToHashSet(StringComparer.Ordinal);

        foreach (var tool in McpToolSurfaceTests.ExpectedTools)
        {
            // The overview is what the reader is already holding, so it does not point at itself.
            if (tool is "GetBrouterOverview") continue;

            Assert.IsTrue(mentioned.Contains(tool), $"The overview's 'Which tool to call' section never mentions '{tool}'.");
        }

        foreach (var tool in mentioned)
        {
            CollectionAssert.Contains(McpToolSurfaceTests.ExpectedTools, tool,
                $"The overview tells the reader to call '{tool}', which this server does not expose.");
        }
    }

    [TestMethod]
    public async Task Guide_sections_are_listed_with_their_level_and_size()
    {
        var sections = await McpCall.StructuredAsync<BrouterGuideSectionDto[]>("GetBrouterGuideSections");

        Assert.IsTrue(sections.Length > 20, $"Only {sections.Length} guide sections were listed; the README has far more.");

        foreach (var section in sections)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(section.Heading));
            Assert.IsTrue(section.Level is 2 or 3, $"'{section.Heading}' is listed at level {section.Level}.");
            Assert.IsTrue(section.Lines > 0, $"'{section.Heading}' is listed as empty.");

            // A sub-section without its parent cannot be placed by the reader.
            if (section.Level == 3) Assert.IsNotNull(section.Parent, $"Sub-section '{section.Heading}' names no parent section.");
            else Assert.IsNull(section.Parent, $"Top-level section '{section.Heading}' names a parent.");
        }
    }

    [TestMethod]
    public async Task Guide_sections_are_listed_in_reading_order_under_their_own_parents()
    {
        // The listing doubles as a table of contents: a sub-section has to follow the section it
        // belongs to, or a reader picking a heading off it lands somewhere else entirely.
        var sections = await McpCall.StructuredAsync<BrouterGuideSectionDto[]>("GetBrouterGuideSections");

        Assert.AreEqual("Install", sections[0].Heading, "The guide no longer opens with its Install section.");

        string? current = null;

        foreach (var section in sections)
        {
            if (section.Level == 2) { current = section.Heading; continue; }

            Assert.AreEqual(current, section.Parent,
                $"'{section.Heading}' is listed under '{section.Parent}' but sits inside '{current}' in the README.");
        }
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
    public async Task An_unknown_guide_heading_answers_with_the_headings_that_do_exist()
    {
        var answer = await McpCall.TextAsync("GetBrouterGuideSection", new() { ["heading"] = "Teleportation" });

        StringAssert.Contains(answer, "no section called 'Teleportation'");
        StringAssert.Contains(answer, "'Async guards'", "The apology does not list what the guide does have.");
    }

    [TestMethod]
    public async Task The_documentation_pages_are_listed_with_everything_needed_to_pick_one()
    {
        var pages = await McpCall.StructuredAsync<BrouterDocsPageDto[]>("GetBrouterDocsList");

        Assert.AreEqual(DocsCatalog.AllPages.Count(), pages.Length);

        foreach (var page in pages)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(page.Section));
            Assert.IsFalse(string.IsNullOrWhiteSpace(page.Title));
            Assert.IsFalse(string.IsNullOrWhiteSpace(page.Description));
            Assert.IsFalse(string.IsNullOrWhiteSpace(page.Keywords), $"'{page.Title}' carries no keywords, so it cannot be searched for.");
            StringAssert.StartsWith(page.Url, "/docs");
        }
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
        // Its real slug is the empty string, which nobody types.
        var byOmission = await McpCall.TextAsync("GetBrouterDocsPage");

        foreach (var alias in new[] { "overview", "index", "docs" })
        {
            var byAlias = await McpCall.TextAsync("GetBrouterDocsPage", new() { ["slug"] = alias });

            Assert.AreEqual(byOmission, byAlias, $"'{alias}' does not resolve to the documentation overview.");
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
    public async Task The_source_files_include_the_route_table_and_a_sample_per_render_mode()
    {
        var files = await McpCall.StructuredAsync<BrouterSourceFileDto[]>("GetBrouterSourceFiles");
        var paths = files.Select(file => file.Path).ToArray();

        CollectionAssert.Contains(paths, "Demo/Client/AppRouter.razor");

        foreach (var prefix in new[] { "Sample/Server/", "Sample/Wasm/", "Sample/Auto/", "Sample/Core/Extensions/" })
        {
            Assert.IsTrue(paths.Any(path => path.StartsWith(prefix, StringComparison.Ordinal)),
                $"No source file under '{prefix}' was embedded, so the setup guide that quotes it has nothing to show.");
        }

        foreach (var file in files)
        {
            Assert.IsTrue(file.Kind is "Demo" or "Sample", $"'{file.Path}' is listed as '{file.Kind}'.");
            Assert.IsTrue(file.Lines > 0, $"'{file.Path}' is listed as empty.");
            Assert.IsFalse(file.Path.StartsWith("BrouterSource", StringComparison.Ordinal),
                $"'{file.Path}' still carries the resource prefix a caller is not supposed to know about.");
            Assert.IsFalse(file.Path.Contains('\\'), $"'{file.Path}' is spelled with a backslash, which no caller would type.");
        }
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
    public async Task An_unrecognizable_source_path_points_at_the_listing_tool()
    {
        var answer = await McpCall.TextAsync("GetBrouterSourceFile", new() { ["path"] = "../../../etc/passwd" });

        StringAssert.Contains(answer, "GetBrouterSourceFiles");
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
}
