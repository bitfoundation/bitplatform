using Bit.Bmotion.Demo.Client.Shared;

namespace Bit.Bmotion.Tests.Mcp.Services;

/// <summary>
/// The guide and the demo sources the tools hand out.
/// <para>
/// Both are embedded resources, which is the point: a published, single-folder deployment has no
/// repository next to it. That also makes this the one part of the server that can be wrong in
/// production and right on a developer's machine - a changed glob in the .csproj takes the text
/// away without breaking a build or throwing anything, and the tools then answer every question
/// with an empty string. These tests fail when the payload is missing rather than when it is
/// merely different.
/// </para>
/// </summary>
[TestClass]
public class SourceCatalogTests
{
    [TestMethod]
    public void Readme_IsEmbeddedInTheAssembly()
    {
        Assert.IsTrue(BmotionSourceCatalog.Readme.Length > 5_000,
                      $"The guide is {BmotionSourceCatalog.Readme.Length} characters - the embedded resource is missing or truncated.");

        StringAssert.Contains(BmotionSourceCatalog.Readme, "Bmotion");
    }

    [TestMethod]
    public void GuideSections_AreListedInReadingOrder_WithTheirLevels()
    {
        var sections = BmotionSourceCatalog.GuideSections;

        Assert.IsTrue(sections.Length >= 10, $"Only {sections.Length} sections were found in the guide.");

        foreach (var section in sections)
        {
            CollectionAssert.Contains(new[] { 2, 3 }, section.Level, $"'{section.Heading}' is at level {section.Level}.");
            Assert.IsTrue(section.Lines > 0, $"'{section.Heading}' is empty.");
            Assert.AreNotEqual(string.Empty, section.Heading.Trim());

            // A sub-section names the section it belongs to; a top-level one has nothing above it.
            if (section.Level == 3) Assert.IsNotNull(section.Parent, $"'{section.Heading}' is orphaned.");
            else Assert.IsNull(section.Parent, $"'{section.Heading}' is level 2 but claims a parent.");
        }
    }

    /// <summary>
    /// The table of contents is a list of links to the sections below it: as an answer it is
    /// strictly worse than the section it points at, and it outranks real content in search.
    /// </summary>
    [TestMethod]
    public void GuideSections_LeaveOutTheTableOfContents()
    {
        Assert.IsFalse(BmotionSourceCatalog.GuideSections.Any(section =>
            section.Heading.Contains("Table of Contents", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Every heading the listing tool advertises has to be one the fetching tool answers - the two
    /// are separate walks of the same text, and a listing that names a section nobody can fetch
    /// sends an agent round a loop it cannot leave.
    /// </summary>
    [TestMethod]
    public void GetGuideSection_AnswersForEveryHeadingTheListingAdvertises()
    {
        foreach (var section in BmotionSourceCatalog.GuideSections)
        {
            var text = BmotionSourceCatalog.GetGuideSection(section.Heading);

            Assert.IsNotNull(text, $"The listing offers '{section.Heading}' but nothing fetches it.");
            StringAssert.StartsWith(text, new string('#', section.Level),
                                    $"'{section.Heading}' did not come back starting at its own heading.");
            StringAssert.Contains(text, section.Heading);
        }
    }

    [TestMethod]
    [DataRow("Installation")]
    [DataRow("installation")]
    [DataRow("INSTALLATION")]
    [DataRow("  Installation  ")]
    public void GetGuideSection_MatchesHeadingsRegardlessOfCaseAndSpacing(string heading)
    {
        Assert.IsNotNull(BmotionSourceCatalog.GetGuideSection(heading));
    }

    /// <summary>An agent rarely reproduces the punctuation of a heading it was told about second-hand.</summary>
    [TestMethod]
    public void GetGuideSection_IgnoresPunctuationInTheHeading()
    {
        var canonical = BmotionSourceCatalog.GetGuideSection("Layout & shared elements");

        Assert.IsNotNull(canonical, "The guide no longer has a 'Layout & shared elements' section to test with.");
        Assert.AreEqual(canonical, BmotionSourceCatalog.GetGuideSection("layout shared elements"));
        Assert.AreEqual(canonical, BmotionSourceCatalog.GetGuideSection("Layout and shared elements".Replace("and ", "", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void GetGuideSection_ASectionEndsWhereTheNextOneOfItsRankBegins()
    {
        var components = BmotionSourceCatalog.GetGuideSection("Components")!;

        // A level-2 section carries its own sub-sections...
        StringAssert.Contains(components, "### Bmotion");
        // ...and stops before the next level-2 heading.
        Assert.IsFalse(components.Contains("\n## Transitions", StringComparison.Ordinal),
                       "The Components section ran on into the one after it.");

        // A level-3 section carries neither its siblings nor the section above it.
        var presence = BmotionSourceCatalog.GetGuideSection("BmotionAnimatePresence")!;

        StringAssert.StartsWith(presence, "### BmotionAnimatePresence");
        Assert.IsFalse(presence.Contains("### BmotionPresenceSwitch", StringComparison.Ordinal));
    }

    /// <summary>
    /// The guide is full of fenced C# and shell samples, and both use '#' at the start of a line.
    /// Reading one as a heading would cut a section in half at an arbitrary point.
    /// </summary>
    [TestMethod]
    public void GuideSections_AreNotInventedFromCodeFences()
    {
        foreach (var section in BmotionSourceCatalog.GuideSections)
        {
            Assert.IsFalse(section.Heading.StartsWith("if ", StringComparison.Ordinal) ||
                           section.Heading.StartsWith("region", StringComparison.Ordinal) ||
                           section.Heading.StartsWith("!", StringComparison.Ordinal),
                           $"'{section.Heading}' looks like a line of code read as a heading.");
        }

        // The Installation section contains a fenced shell block; it has to survive intact.
        StringAssert.Contains(BmotionSourceCatalog.GetGuideSection("Installation")!, "```");
    }

    [TestMethod]
    [DataRow("Nonexistent section")]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    public void GetGuideSection_AnUnknownHeading_IsNullRatherThanTheWholeGuide(string? heading)
    {
        Assert.IsNull(BmotionSourceCatalog.GetGuideSection(heading!));
    }

    [TestMethod]
    public void SourceFiles_AreEmbedded_AndEveryListedPathResolves()
    {
        var files = BmotionSourceCatalog.SourceFiles;

        Assert.IsTrue(files.Length >= 10, $"Only {files.Length} source files are embedded.");

        foreach (var file in files)
        {
            Assert.IsNotNull(BmotionSourceCatalog.GetSourceFile(file.Path),
                             $"'{file.Path}' is listed but cannot be fetched.");
            Assert.IsTrue(file.Lines > 0, $"'{file.Path}' is empty.");
            CollectionAssert.Contains(new[] { "Demo page", "Demo", "Host" }, file.Kind, $"'{file.Path}' is a '{file.Kind}'.");
        }
    }

    [TestMethod]
    public void SourceFiles_IncludeTheHostWiring_TheSetupGuideClaimsToShowAsARealFile()
    {
        Assert.IsNotNull(BmotionSourceCatalog.GetSourceFile("Demo/Server/Program.cs"));

        Assert.IsTrue(BmotionSourceCatalog.SourceFiles.Any(file => file.Kind == "Host"),
                      "Nothing in the catalog is classified as host wiring.");
        Assert.IsTrue(BmotionSourceCatalog.SourceFiles.Any(file => file.Kind == "Demo page"),
                      "Nothing in the catalog is classified as a demo page.");
    }

    /// <summary>
    /// GetBmotionDemoPages hands out a SourcePath per page and tells the agent to pass it to
    /// GetBmotionSourceFile. Every one of them has to be a path that resolves.
    /// </summary>
    [TestMethod]
    public void SourceFiles_CoverEveryPageTheDemoPageListingPointsAt()
    {
        var missing = NavItem.All
            .Where(page => BmotionSourceCatalog.GetSourceFile(page.SourcePath) is null)
            .Select(page => $"{page.Title} -> {page.SourcePath}")
            .ToArray();

        Assert.AreEqual(0, missing.Length, $"Advertised but not embedded: {string.Join("; ", missing)}.");
    }

    [TestMethod]
    public void GetSourceFile_AcceptsThePathHoweverItIsSpelled()
    {
        var canonical = BmotionSourceCatalog.GetSourceFile("Demo/Server/Program.cs");

        Assert.IsNotNull(canonical);
        Assert.AreEqual(canonical, BmotionSourceCatalog.GetSourceFile("demo/server/program.cs"));
        Assert.AreEqual(canonical, BmotionSourceCatalog.GetSourceFile(@"Demo\Server\Program.cs"));
        Assert.AreEqual(canonical, BmotionSourceCatalog.GetSourceFile("/Demo/Server/Program.cs"));
    }

    [TestMethod]
    [DataRow("Demo/Client/Pages/NotAFile.razor")]
    [DataRow("../../../etc/passwd")]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    public void GetSourceFile_AnUnknownPath_IsNull(string? path)
    {
        Assert.IsNull(BmotionSourceCatalog.GetSourceFile(path!));
    }

    /// <summary>
    /// A file's description is taken from whatever the file itself already says. It may be absent,
    /// but it must never be a wall of the file's own source - that is what a listing is not for.
    /// </summary>
    [TestMethod]
    public void SourceFiles_DescriptionsAreOneLine_OrAbsent()
    {
        foreach (var file in BmotionSourceCatalog.SourceFiles.Where(file => file.Description is not null))
        {
            Assert.IsTrue(file.Description!.Length <= 220, $"'{file.Path}' has a {file.Description.Length}-character description.");
            Assert.IsFalse(file.Description.Contains('\n'), $"'{file.Path}' has a multi-line description.");
        }
    }

    [TestMethod]
    public void SourceFiles_LineCounts_MatchTheFilesTheyDescribe()
    {
        foreach (var file in BmotionSourceCatalog.SourceFiles)
        {
            var content = BmotionSourceCatalog.GetSourceFile(file.Path)!;
            var newlines = content.Count(c => c == '\n');
            var expected = content.Length == 0 ? 0 : content[^1] == '\n' ? newlines : newlines + 1;

            Assert.AreEqual(expected, file.Lines, $"'{file.Path}' is reported as {file.Lines} lines.");
        }
    }
}
