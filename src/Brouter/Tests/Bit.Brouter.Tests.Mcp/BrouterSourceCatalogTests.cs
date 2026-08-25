using Bit.Brouter.Demo.Server.Services;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// The two bodies of hand-written material the server serves: the library's README, which doubles
/// as its reference guide, and the source files of the demo and the hosting samples.
/// <para>
/// Both are embedded into the assembly by the .csproj so the tools keep working from a published,
/// single-folder deployment where the repository is nowhere to be found - which also means a
/// mistyped <c>LogicalName</c> or a glob that stops matching produces an empty catalog rather than a
/// build error. These tests are what stands between that and a server that answers every question
/// with "not found".
/// </para>
/// </summary>
[TestClass]
public class BrouterSourceCatalogTests
{
    [TestMethod]
    public void The_readme_is_embedded_whole()
    {
        var readme = BrouterSourceCatalog.Readme;

        Assert.IsTrue(readme.Length > 10_000, $"The README came back as {readme.Length} characters; it is not being embedded.");
        StringAssert.Contains(readme, "## Install");
        StringAssert.Contains(readme, "## Quick start");
    }

    [TestMethod]
    public void A_hash_inside_a_code_fence_is_not_read_as_a_heading()
    {
        // The README is full of shell comments and preprocessor directives inside code blocks. One of
        // them read as a heading would end the section it sits in halfway through.
        foreach (var section in BrouterSourceCatalog.GuideSections)
        {
            Assert.IsFalse(section.Heading.StartsWith("!/", StringComparison.Ordinal), $"'{section.Heading}' is a shebang, not a heading.");
            Assert.IsFalse(section.Heading.StartsWith("if ", StringComparison.Ordinal), $"'{section.Heading}' is a preprocessor directive, not a heading.");
        }

        // The install section opens with a fenced shell block; its content has to survive intact.
        var install = BrouterSourceCatalog.GetGuideSection("Install")!;

        StringAssert.Contains(install, "dotnet add package Bit.Brouter");
        StringAssert.Contains(install, "AddBitBrouterServices");
    }

    [TestMethod]
    public void A_section_ends_at_the_next_heading_of_its_own_rank()
    {
        var section = BrouterSourceCatalog.GetGuideSection("Async guards")!;

        StringAssert.StartsWith(section, "## Async guards");

        // The heading that follows it in the README is not part of it.
        Assert.IsFalse(section.Contains("## Leave guards", StringComparison.Ordinal), "The section ran past its own end.");
    }

    [TestMethod]
    public void A_section_keeps_the_sub_sections_that_belong_to_it()
    {
        var section = BrouterSourceCatalog.GetGuideSection("Data loader")!;

        StringAssert.Contains(section, "### Preloading");
        StringAssert.Contains(section, "### Deferred (streamed) data");
    }

    [TestMethod]
    public void A_heading_is_matched_by_its_words_rather_than_by_its_punctuation()
    {
        var canonical = BrouterSourceCatalog.GetGuideSection("Leave guards (unsaved changes)");

        Assert.IsNotNull(canonical);

        foreach (var spelling in new[] { "leave guards unsaved changes", "LEAVE GUARDS (UNSAVED CHANGES)", "Leave-guards, unsaved changes!" })
        {
            Assert.AreEqual(canonical, BrouterSourceCatalog.GetGuideSection(spelling), $"'{spelling}' did not find the section.");
        }
    }

    [TestMethod]
    public void A_heading_that_is_not_in_the_guide_resolves_to_nothing_rather_than_to_something_near_it()
    {
        Assert.IsNull(BrouterSourceCatalog.GetGuideSection("Guards"), "A partial heading matched a section it is not.");
        Assert.IsNull(BrouterSourceCatalog.GetGuideSection("   "));
        Assert.IsNull(BrouterSourceCatalog.GetGuideSection(""));
    }

    [TestMethod]
    public void Every_listed_section_can_be_read_back_by_the_heading_it_is_listed_under()
    {
        // The listing is what a caller picks a heading from; a heading it prints that the lookup then
        // rejects is a dead end with no way around it.
        foreach (var section in BrouterSourceCatalog.GuideSections)
        {
            var text = BrouterSourceCatalog.GetGuideSection(section.Heading);

            Assert.IsNotNull(text, $"'{section.Heading}' is listed but cannot be read.");
            StringAssert.Contains(text, section.Heading);
        }
    }

    [TestMethod]
    public void Every_source_file_of_the_demo_and_the_samples_is_embedded()
    {
        var paths = BrouterSourceCatalog.SourceFiles.Select(file => file.Path).ToArray();

        // The route table, the pages behind it, the registration that adds the custom constraint,
        // the hosting files, and one file per sample project - the set the tools and the setup
        // guides actually reach for.
        foreach (var expected in new[]
        {
            "Demo/Client/AppRouter.razor",
            "Demo/Client/Pages/DataPage.razor",
            "Demo/Client/Extensions/IServiceCollectionExtensions.cs",
            "Demo/Client/Program.cs",
            "Demo/Server/Program.cs",
            "Demo/Server/Components/Pages/Host.razor",
            "Sample/Core/Extensions/IServiceCollectionExtensions.cs",
        })
        {
            CollectionAssert.Contains(paths, expected, $"'{expected}' is not embedded.");
        }

        Assert.IsTrue(paths.Count(path => path.StartsWith("Demo/", StringComparison.Ordinal)) > 20,
            "Barely any of the demo's files are embedded; the glob is not matching what it used to.");
    }

    [TestMethod]
    public void What_the_site_is_rather_than_what_it_teaches_is_not_handed_out_as_source()
    {
        var paths = BrouterSourceCatalog.SourceFiles.Select(file => file.Path).ToArray();

        // A documentation page's Razor source is that page's prose with markup around it, and the
        // docs tool already serves the page itself, rendered. Handing the same text over a second
        // time teaches nothing about routing and costs thousands of characters to say so.
        foreach (var page in Bit.Brouter.Demo.Client.DocsCatalog.AllPages)
        {
            var component = $"{page.PageType.Name}.razor";

            Assert.IsFalse(paths.Any(path => path.EndsWith(component, StringComparison.OrdinalIgnoreCase)),
                $"'{component}' is a documentation page, and is being handed out as source as well as rendered.");
        }

        // Stylesheets, scripts and the site's own chrome answer no question about Brouter either.
        foreach (var noise in new[] { ".css", ".js", "/Shared/", "McpDemoClient.cs" })
        {
            Assert.IsFalse(paths.Any(path => path.Contains(noise, StringComparison.OrdinalIgnoreCase)),
                $"'{noise}' files are embedded, and there is no question about routing they answer.");
        }
    }

    [TestMethod]
    public void A_files_folder_is_part_of_its_path_rather_than_flattened_away()
    {
        // %(RecursiveDir) only captures what a '**' matched, so a glob with a fixed folder prefix
        // flattens the file to the root of its group. Two paths here are written out for that reason.
        CollectionAssert.Contains(BrouterSourceCatalog.SourceFiles.Select(file => file.Path).ToArray(),
            "Demo/Server/Components/Pages/Host.razor");

        Assert.IsNull(BrouterSourceCatalog.GetSourceFile("Demo/Server/Pages/Host.razor"),
            "The host page is embedded without the folder it lives in.");
    }

    [TestMethod]
    public void The_demos_own_pages_are_not_embedded_twice_under_the_sample_prefix()
    {
        // Samples/Core's pages are deliberately left out: they are a smaller copy of the demo's own,
        // and embedding both would double the corpus a search ranks over. Its service registration
        // stays, because it is the shared method a Web App's two containers both call.
        var core = BrouterSourceCatalog.SourceFiles
            .Where(file => file.Path.StartsWith("Sample/Core/", StringComparison.Ordinal))
            .Select(file => file.Path)
            .ToArray();

        CollectionAssert.Contains(core, "Sample/Core/Extensions/IServiceCollectionExtensions.cs");

        foreach (var path in core)
        {
            Assert.IsFalse(path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase),
                $"'{path}' is a second copy of a component the demo already provides.");
        }
    }

    [TestMethod]
    public void A_source_file_is_found_however_the_caller_spells_the_path()
    {
        var canonical = BrouterSourceCatalog.GetSourceFile("Demo/Client/AppRouter.razor");

        Assert.IsNotNull(canonical);
        Assert.AreEqual(canonical, BrouterSourceCatalog.GetSourceFile("demo/client/approuter.razor"));
        Assert.AreEqual(canonical, BrouterSourceCatalog.GetSourceFile("Demo\\Client\\AppRouter.razor"));
        Assert.AreEqual(canonical, BrouterSourceCatalog.GetSourceFile("/Demo/Client/AppRouter.razor"));

        Assert.IsNull(BrouterSourceCatalog.GetSourceFile("  "));
        Assert.IsNull(BrouterSourceCatalog.GetSourceFile("Demo/Client/NoSuchFile.razor"));
    }

    [TestMethod]
    public void A_files_line_count_is_the_number_of_lines_it_has()
    {
        // A trailing newline ends the last line rather than starting another one.
        foreach (var file in BrouterSourceCatalog.SourceFiles)
        {
            var content = BrouterSourceCatalog.GetSourceFile(file.Path)!;
            var expected = content.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n').Split('\n').Length;

            Assert.AreEqual(expected, file.Lines, $"'{file.Path}' is listed as {file.Lines} lines but has {expected}.");
        }
    }

    [TestMethod]
    public void A_files_description_is_taken_from_whatever_the_file_itself_says()
    {
        var files = BrouterSourceCatalog.SourceFiles.ToDictionary(file => file.Path, StringComparer.OrdinalIgnoreCase);

        // A razor page's leading @* *@ comment, and a C# file's XML summary - stripped of the markup
        // that would read as noise in a listing.
        var routeTable = files["Demo/Client/AppRouter.razor"].Description;
        Assert.IsNotNull(routeTable);
        Assert.IsFalse(routeTable.Contains("@*", StringComparison.Ordinal));

        Assert.IsTrue(files.TryGetValue("Demo/Client/ConstraintCatalog.cs", out var constraintCatalog),
            "'Demo/Client/ConstraintCatalog.cs' is not in the catalog, so its embedded resource is missing.");

        var catalog = constraintCatalog.Description;
        Assert.IsNotNull(catalog);
        Assert.IsFalse(catalog.Contains("<", StringComparison.Ordinal), $"XML markup leaked into a description: {catalog}");
        Assert.IsFalse(catalog.Contains("///", StringComparison.Ordinal));

        foreach (var file in BrouterSourceCatalog.SourceFiles)
        {
            Assert.IsTrue(file.Description is null or { Length: <= 220 },
                $"'{file.Path}' is described in {file.Description!.Length} characters, which is a paragraph rather than a line.");
        }
    }

    [TestMethod]
    public void Most_of_the_demos_files_say_what_they_are()
    {
        // Not a hard requirement of any single file - but a listing where nothing is described is a
        // listing nobody can choose from, and that is what a broken description pass looks like.
        // The demo's files are the ones that carry a comment saying what they demonstrate; the
        // sample projects are boilerplate hosting, quoted whole by the setup guides.
        var demo = BrouterSourceCatalog.SourceFiles.Where(file => file.Kind == "Demo").ToArray();
        var described = demo.Count(file => file.Description is not null);

        Assert.IsTrue(described > demo.Length / 2,
            $"Only {described} of {demo.Length} of the demo's source files carry a description.");
    }
}
