using Bit.Bmage.Demo.Client.Shared;

namespace Bit.Bmage.Tests.Mcp.Services;

/// <summary>
/// The embedded text: the README, every guide under docs/, and the demo's sources. They are embedded
/// so a published server still has them - which also means a guide added to docs/ and forgotten in
/// the .csproj glob would simply be missing, so the embedded set is compared against the disk.
/// </summary>
[TestClass]
public class SourceCatalogTests
{
    private static readonly string BmageRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [TestMethod]
    public void Documents_EveryGuideOnDisk_IsEmbedded()
    {
        var onDisk = Directory.GetFiles(Path.Combine(BmageRoot, "docs"), "*.md").Select(Path.GetFileNameWithoutExtension).Order().ToArray();

        Assert.IsNotEmpty(onDisk, $"No guides found under {BmageRoot}.");

        var embedded = BmageSourceCatalog.Documents.Select(document => document.Name).Where(name => name != BmageSourceCatalog.ReadmeName).Order().ToArray();

        CollectionAssert.AreEqual(onDisk, embedded);
    }

    [TestMethod]
    public void Documents_AreTheFilesVerbatim()
    {
        var readme = File.ReadAllText(Path.Combine(BmageRoot, "README.md"));

        Assert.AreEqual(readme.Replace("\r\n", "\n", StringComparison.Ordinal), BmageSourceCatalog.Readme.Replace("\r\n", "\n", StringComparison.Ordinal));

        var pipelines = File.ReadAllText(Path.Combine(BmageRoot, "docs", "pipelines.md"));

        Assert.AreEqual(pipelines.Replace("\r\n", "\n", StringComparison.Ordinal), BmageSourceCatalog.GetDocument("pipelines")!.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Documents_EveryOneHasATitleASummaryAndSections()
    {
        foreach (var document in BmageSourceCatalog.Documents)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(document.Title), document.Name);
            Assert.IsFalse(string.IsNullOrWhiteSpace(document.Summary), $"{document.Name} has no summary.");
            Assert.IsNotEmpty(document.Sections, $"{document.Name} has no sections.");
            Assert.IsFalse(document.Sections.Contains("Table of Contents"), $"{document.Name} lists its table of contents as a section.");
        }
    }

    [TestMethod]
    public void Sections_EndAtTheNextHeadingOfTheSameRank_AndIgnoreHashesInCode()
    {
        // performance.md has '#' lines in its code fences (shell comments) that are not headings.
        foreach (var document in BmageSourceCatalog.Documents)
        {
            foreach (var (heading, level) in BmageSourceCatalog.GetHeadings(document.Name))
            {
                var section = BmageSourceCatalog.GetDocumentSection(document.Name, heading);

                Assert.IsNotNull(section, $"{document.Name}: '{heading}' is listed but cannot be read.");
                StringAssert.StartsWith(section, $"{new string('#', level)} {heading}");
            }
        }

        var install = BmageSourceCatalog.GetDocumentSection("getting-started", "Install")!;
        Assert.IsFalse(install.Contains("## Register the services", StringComparison.Ordinal), "A section ran on into the next one.");
    }

    [TestMethod]
    public void Sources_EveryDemoPageInTheNavigation_IsEmbedded_AndDescribedByIt()
    {
        foreach (var page in NavItem.All)
        {
            var file = BmageSourceCatalog.SourceFiles.SingleOrDefault(entry => entry.Path == page.SourcePath);

            Assert.IsNotNull(file, $"{page.SourcePath} is not embedded.");
            Assert.AreEqual("Demo page", file.Kind);
            Assert.AreEqual(page.Description, file.Description);
            Assert.AreEqual(page.Href, file.Slug);
        }
    }

    [TestMethod]
    public void Sources_TheHostWiringAndTheClientProject_AreEmbedded()
    {
        StringAssert.Contains(BmageSourceCatalog.GetSourceFile("Demo/Server/Program.cs"), "AddDemoServices");
        StringAssert.Contains(BmageSourceCatalog.GetSourceFile("Demo/Server/Components/App.razor"), "bit-bmage-editor.css");
        StringAssert.Contains(BmageSourceCatalog.GetSourceFile("Demo/Client/Extensions/IServiceCollectionExtensions.cs"), "AddBitBmageServices");
        StringAssert.Contains(BmageSourceCatalog.GetSourceFile("Demo/Client/Bit.Bmage.Demo.Client.csproj"), "RunAOTCompilation");

        Assert.AreEqual("Host", BmageSourceCatalog.SourceFiles.Single(file => file.Path == "Demo/Server/Program.cs").Kind);
    }

    [TestMethod]
    public void Sources_PathsAreForgiving_AndLineCountsAreReal()
    {
        Assert.IsNotNull(BmageSourceCatalog.GetSourceFile("demo\\client\\pages\\avatar.razor"));
        Assert.IsNotNull(BmageSourceCatalog.GetSourceFile("/Demo/Client/Pages/Avatar.razor"));
        Assert.IsNull(BmageSourceCatalog.GetSourceFile("Demo/Client/Pages/Nowhere.razor"));

        var avatar = BmageSourceCatalog.SourceFiles.Single(file => file.Path == "Demo/Client/Pages/Avatar.razor");
        var lines = File.ReadAllLines(Path.Combine(BmageRoot, "Bit.Bmage.Demo", "Client", "Pages", "Avatar.razor")).Length;

        Assert.AreEqual(lines, avatar.Lines);
    }

    [TestMethod]
    public void Sources_NothingFromBinOrObj_IsEmbedded()
    {
        Assert.IsFalse(BmageSourceCatalog.SourceFiles.Any(file => file.Path.Contains("/obj/", StringComparison.OrdinalIgnoreCase) || file.Path.Contains("/bin/", StringComparison.OrdinalIgnoreCase)));
    }
}
