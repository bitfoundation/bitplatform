using Bit.Bswup.Demo.Server.Services;

namespace Bit.Bswup.Tests.Mcp.Services;

/// <summary>
/// The embedded corpus the tools hand out: the README that doubles as the reference guide, and
/// the working source files. All of it is embedded rather than read from disk, so the failure this
/// guards against is a published deployment where an .csproj glob quietly stopped matching and a
/// tool answers "no source file at ..." for a path it advertises itself.
/// </summary>
[TestClass]
public class SourceCatalogTests
{
    // -- The reference guide ---------------------------------------------------

    [TestMethod]
    public void Readme_IsEmbeddedAndReadable()
    {
        Assert.IsTrue(BswupSourceCatalog.Readme.Length > 1000, "the README resource did not make it into the assembly");
        StringAssert.Contains(BswupSourceCatalog.Readme, "bit Bswup");
    }

    [TestMethod]
    public void GuideSections_AreTheHeadingsOfTheReadme()
    {
        var headings = BswupSourceCatalog.GuideSections.Select(section => section.Heading).ToArray();

        CollectionAssert.Contains(headings, "JavaScript API");
        CollectionAssert.Contains(headings, "Backing out of Bswup (the cleanup worker)");
        Assert.IsTrue(BswupSourceCatalog.GuideSections.All(section => section.Level is 2 or 3));
        Assert.IsTrue(BswupSourceCatalog.GuideSections.All(section => section.Lines > 0));
    }

    [TestMethod]
    public void GuideSections_AttachSubSectionsToTheirParent()
    {
        var polling = BswupSourceCatalog.GuideSections.Single(section => section.Heading == "Polling for updates");

        Assert.AreEqual(3, polling.Level);
        Assert.AreEqual("JavaScript API", polling.Parent);

        var api = BswupSourceCatalog.GuideSections.Single(section => section.Heading == "JavaScript API");

        Assert.AreEqual(2, api.Level);
        Assert.IsNull(api.Parent, "a top-level section has no parent");
    }

    [TestMethod]
    public void GetGuideSection_ReturnsTheSectionWithItsOwnHeading()
    {
        var section = BswupSourceCatalog.GetGuideSection("JavaScript API");

        Assert.IsNotNull(section);
        StringAssert.StartsWith(section, "## JavaScript API");
    }

    [TestMethod]
    public void GetGuideSection_IncludesItsSubSections()
    {
        var section = BswupSourceCatalog.GetGuideSection("JavaScript API");

        StringAssert.Contains(section, "### Polling for updates",
            "a reader asking for a section expects what is nested under it");
    }

    [TestMethod]
    public void GetGuideSection_StopsAtTheNextSectionOfTheSameOrHigherRank()
    {
        var section = BswupSourceCatalog.GetGuideSection("JavaScript API");

        Assert.IsFalse(section!.Contains("## Upgrading to v-10-6-0", StringComparison.Ordinal));
    }

    [TestMethod]
    [DataRow("javascript api")]
    [DataRow("JAVASCRIPT API")]
    [DataRow("JavaScript-API")]
    [DataRow("  JavaScript API  ")]
    public void GetGuideSection_MatchesIgnoringCaseAndPunctuation(string heading)
    {
        // Nobody types a heading back the way it is punctuated.
        var section = BswupSourceCatalog.GetGuideSection(heading);

        Assert.IsNotNull(section, heading);
        StringAssert.StartsWith(section, "## JavaScript API");
    }

    [TestMethod]
    public void GetGuideSection_MatchesAHeadingCarryingInlineCode()
    {
        var section = BswupSourceCatalog.GetGuideSection("The built-in progress UI (BswupProgress)");

        Assert.IsNotNull(section, "the heading is written with backticks around BswupProgress");
    }

    [TestMethod]
    [DataRow("no such section")]
    [DataRow("")]
    [DataRow("   ")]
    public void GetGuideSection_ReturnsNullForAHeadingTheGuideDoesNotHave(string heading)
    {
        Assert.IsNull(BswupSourceCatalog.GetGuideSection(heading));
    }

    [TestMethod]
    public void GuideSections_DoNotPickUpHashLinesInsideCodeFences()
    {
        // A '#' inside a fenced block is a shell comment or a preprocessor directive.
        foreach (var section in BswupSourceCatalog.GuideSections)
        {
            Assert.IsFalse(section.Heading.StartsWith("if ", StringComparison.Ordinal), section.Heading);
            Assert.IsFalse(section.Heading.Contains("dotnet ", StringComparison.Ordinal), section.Heading);
        }
    }

    [TestMethod]
    public void GuideSections_AreEachRetrievable()
    {
        foreach (var section in BswupSourceCatalog.GuideSections)
        {
            Assert.IsNotNull(BswupSourceCatalog.GetGuideSection(section.Heading),
                $"'{section.Heading}' is listed but cannot be fetched - the listing and the reader disagree");
        }
    }

    // -- The source files ------------------------------------------------------

    [TestMethod]
    [DataRow("Library/Scripts/bit-bswup.sw.ts")]
    [DataRow("Library/Scripts/bit-bswup.ts")]
    [DataRow("Library/BswupProgress.razor")]
    [DataRow("Demo/Client/wwwroot/service-worker.js")]
    [DataRow("Demo/Client/wwwroot/service-worker.published.js")]
    [DataRow("Demo/Server/Components/App.razor")]
    [DataRow("Sample/BasicSample/wwwroot/index.html")]
    [DataRow("Sample/BasicSample/wwwroot/service-worker.js")]
    [DataRow("Sample/FullSample/Client/wwwroot/service-worker.published.js")]
    public void SourceFiles_IncludeTheOnesTheToolsNameByPath(string path)
    {
        // Each of these is quoted in a tool description or a prompt as something a caller can ask
        // for by name, so a glob that stops matching it turns advice into a dead end.
        Assert.IsNotNull(BswupSourceCatalog.GetSourceFile(path), $"'{path}' is not embedded");
        Assert.IsTrue(BswupSourceCatalog.SourceFiles.Any(file => file.Path == path), $"'{path}' is not listed");
    }

    [TestMethod]
    public void SourceFiles_KeepTheServiceWorkerSourceOutOfASatelliteAssembly()
    {
        // "bit-bswup.sw.ts" reads to MSBuild as a Swahili-cultured resource unless WithCulture is
        // turned off, which moves the engine's own source where nothing on this side can find it.
        var worker = BswupSourceCatalog.GetSourceFile("Library/Scripts/bit-bswup.sw.ts");

        Assert.IsNotNull(worker);
        StringAssert.Contains(worker, "DEFAULT_ASSETS_INCLUDE");
    }

    [TestMethod]
    public void GetSourceFile_MatchesRegardlessOfCaseAndSlashDirection()
    {
        Assert.IsNotNull(BswupSourceCatalog.GetSourceFile("library/scripts/bit-bswup.sw.ts"));
        Assert.IsNotNull(BswupSourceCatalog.GetSourceFile(@"Library\Scripts\bit-bswup.sw.ts"));
        Assert.IsNotNull(BswupSourceCatalog.GetSourceFile("/Library/Scripts/bit-bswup.sw.ts"));
    }

    [TestMethod]
    [DataRow("nope.ts")]
    [DataRow("")]
    [DataRow("   ")]
    public void GetSourceFile_ReturnsNullForAPathThatIsNotThere(string path)
    {
        Assert.IsNull(BswupSourceCatalog.GetSourceFile(path));
    }

    [TestMethod]
    public void SourceFiles_AreEachReadableAndDescribed()
    {
        Assert.IsTrue(BswupSourceCatalog.SourceFiles.Length > 20, "the embedded source globs matched almost nothing");

        foreach (var file in BswupSourceCatalog.SourceFiles)
        {
            Assert.IsNotNull(BswupSourceCatalog.GetSourceFile(file.Path), file.Path);
            CollectionAssert.Contains(new[] { "Library", "Demo", "Sample" }, file.Kind, file.Path);
            Assert.IsTrue(file.Lines > 0, file.Path);
        }
    }

    [TestMethod]
    public void SourceFiles_AreListedGroupedByKind()
    {
        var kinds = BswupSourceCatalog.SourceFiles.Select(file => file.Kind).ToArray();

        CollectionAssert.AreEqual(kinds.OrderBy(kind => kind, StringComparer.Ordinal).ToArray(), kinds,
            "the listing is meant to read as three groups");
    }

    [TestMethod]
    public void SourceFiles_AreAllDescribed()
    {
        // The descriptions used to be guessed from whatever comment came first in the file, which
        // on this set produced commented-out markup, an aside about screen readers, and a version
        // stamp. A caller picks the file to spend its next call on from this line, so every entry
        // has to carry one and none of it may be a fragment of the file's own code.
        foreach (var file in BswupSourceCatalog.SourceFiles)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(file.Description), $"{file.Path} has no description");
            Assert.IsFalse(file.Description!.Contains('<') && file.Description.Contains("/>"), $"{file.Path}: markup, not a description");
        }
    }

    [TestMethod]
    public void SourceFiles_DescriptionsSayWhichProjectAFileBelongsTo()
    {
        // The Blazor Web App sample has a Program.cs and a .csproj on each side and only one of
        // them carries the Bswup wiring, so naming the sample alone would not say which is which.
        var client = BswupSourceCatalog.SourceFiles.Single(file => file.Path == "Sample/FullSample/Client/Program.cs");
        var host = BswupSourceCatalog.SourceFiles.Single(file => file.Path == "Sample/FullSample/Server/Program.cs");

        StringAssert.Contains(client.Description!, "client project");
        StringAssert.Contains(host.Description!, "host project");
    }

    [TestMethod]
    public void SourceFiles_DoNotIncludeThisSitesOwnChrome()
    {
        // A blanket glob over the demo client used to embed every page, layout, stylesheet and
        // script of this site: sixty-four files, of which the reference answers none. They cost a
        // caller a listing to read and, if followed, tens of thousands of characters to no end.
        foreach (var file in BswupSourceCatalog.SourceFiles)
        {
            Assert.IsFalse(file.Path.StartsWith("Demo/Client/Pages/", StringComparison.Ordinal), file.Path);
            Assert.IsFalse(file.Path.StartsWith("Demo/Client/Shared/", StringComparison.Ordinal), file.Path);
            Assert.IsFalse(file.Path.EndsWith(".css", StringComparison.OrdinalIgnoreCase), file.Path);
            Assert.IsFalse(file.Path.EndsWith("_Imports.razor", StringComparison.OrdinalIgnoreCase), file.Path);
        }

        Assert.IsTrue(BswupSourceCatalog.SourceFiles.Length < 30,
            $"the listing is back to a directory dump: {BswupSourceCatalog.SourceFiles.Length} files");
    }

    [TestMethod]
    public void SourceFiles_DescriptionsAreOneLineLong()
    {
        foreach (var file in BswupSourceCatalog.SourceFiles.Where(file => file.Description is not null))
        {
            Assert.IsTrue(file.Description!.Length <= 220, $"{file.Path}: a description is one line, not the whole comment");
            Assert.IsFalse(file.Description.Contains('\n'), file.Path);
        }
    }

    [TestMethod]
    public void SourceFiles_DoNotIncludeBuildOutput()
    {
        foreach (var file in BswupSourceCatalog.SourceFiles)
        {
            Assert.IsFalse(file.Path.Contains("/obj/", StringComparison.OrdinalIgnoreCase), file.Path);
            Assert.IsFalse(file.Path.Contains("/bin/", StringComparison.OrdinalIgnoreCase), file.Path);
        }
    }

    [TestMethod]
    public void SourceFiles_ReportALineCountThatMatchesTheContent()
    {
        var file = BswupSourceCatalog.SourceFiles.Single(f => f.Path == "Demo/Client/wwwroot/service-worker.js");
        var content = BswupSourceCatalog.GetSourceFile(file.Path)!;

        var expected = content.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n').Split('\n').Length;

        Assert.AreEqual(expected, file.Lines, "a trailing newline ends the last line rather than starting another");
    }
}
