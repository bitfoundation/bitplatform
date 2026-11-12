using Bit.Bmage.Demo.Client;

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

        Assert.AreSequenceEqual(onDisk, embedded);
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
                Assert.StartsWith($"{new string('#', level)} {heading}", section);
            }
        }

        var install = BmageSourceCatalog.GetDocumentSection("getting-started", "Install")!;
        Assert.IsFalse(install.Contains("## Register the services", StringComparison.Ordinal), "A section ran on into the next one.");
    }

    [TestMethod]
    public void Sources_EveryDemoPageInTheNavigation_IsEmbedded_AndDescribedByIt()
    {
        foreach (var page in DocsCatalog.AllPages)
        {
            var path = BmageSourceCatalog.SourcePathOf(page);
            var file = BmageSourceCatalog.SourceFiles.SingleOrDefault(entry => entry.Path == path);

            Assert.IsNotNull(file, $"{path} is not embedded.");
            Assert.AreEqual("Demo page", file.Kind);
            Assert.AreEqual(page.Description, file.Description);
            Assert.AreEqual(page.Url.TrimStart('/'), file.Slug);
            Assert.AreEqual(page.Title, file.Title);
        }

        // The path is derived from the page's type, so it has to name the file that really is the page.
        Assert.AreEqual("Demo/Client/Pages/Avatar.razor", BmageSourceCatalog.SourcePathOf(DocsCatalog.FindByPath("avatar")!));
        Assert.AreEqual("Demo/Client/Pages/McpServerPage.razor", BmageSourceCatalog.SourcePathOf(DocsCatalog.FindByPath("mcp-server")!));
    }

    [TestMethod]
    public void Sources_TheSitesOwnShell_IsChrome_AndNotOfferedAsAnExample()
    {
        var chrome = BmageSourceCatalog.SourceFiles.Where(file => file.Path.StartsWith("Demo/Client/Layout/", StringComparison.Ordinal)).ToArray();

        Assert.Contains(file => file.Path == "Demo/Client/Layout/MainLayout.razor", chrome, "The layout is not embedded.");
        Assert.IsTrue(chrome.All(file => file.Kind == BmageSourceCatalog.SiteChromeKind), string.Join(", ", chrome.Where(file => file.Kind != BmageSourceCatalog.SiteChromeKind).Select(file => file.Path)));
        Assert.DoesNotContain(file => file.Kind == BmageSourceCatalog.SiteChromeKind && file.Path.StartsWith("Demo/Client/Layout/", StringComparison.Ordinal) is false, BmageSourceCatalog.SourceFiles);

        var hits = BmageSearchIndex.Search("command palette header footer navigation layout", 50);

        Assert.IsNotEmpty(hits);
        Assert.DoesNotContain(hit => hit.Title.StartsWith("Demo/Client/Layout/", StringComparison.Ordinal), hits,
            string.Join(", ", hits.Select(hit => hit.Title)));
    }

    [TestMethod]
    public void Sources_TheHostWiringAndTheClientProject_AreEmbedded()
    {
        Assert.Contains("AddDemoServices", BmageSourceCatalog.GetSourceFile("Demo/Server/Program.cs")!);
        Assert.Contains("bit-bmage-editor.css", BmageSourceCatalog.GetSourceFile("Demo/Server/Components/App.razor")!);
        Assert.Contains("AddBitBmageServices", BmageSourceCatalog.GetSourceFile("Demo/Client/Extensions/IServiceCollectionExtensions.cs")!);
        Assert.Contains("RunAOTCompilation", BmageSourceCatalog.GetSourceFile("Demo/Client/Bit.Bmage.Demo.Client.csproj")!);

        Assert.AreEqual("Host", BmageSourceCatalog.SourceFiles.Single(file => file.Path == "Demo/Server/Program.cs").Kind);
    }

    [TestMethod]
    public void Sources_TheMauiSamplesWiring_IsEmbedded_AsSamples()
    {
        Assert.Contains("AddMauiBlazorWebView", BmageSourceCatalog.GetSourceFile("Samples/MAUI/MauiProgram.cs")!);
        Assert.Contains("BlazorWebView", BmageSourceCatalog.GetSourceFile("Samples/MAUI/MainPage.xaml")!);
        Assert.Contains("bit-bmage-editor.css", BmageSourceCatalog.GetSourceFile("Samples/MAUI/wwwroot/index.html")!);
        Assert.Contains("MaxDegreeOfParallelism", BmageSourceCatalog.GetSourceFile("Samples/MAUI/Extensions/IServiceCollectionExtensions.cs")!);
        Assert.Contains("<UseMaui>true</UseMaui>", BmageSourceCatalog.GetSourceFile("Samples/MAUI/Bit.Bmage.Samples.Maui.csproj")!);
        Assert.Contains("AddBitBmageServices", BmageSourceCatalog.GetSourceFile("Samples/Core/Extensions/IServiceCollectionExtensions.cs")!);

        var samples = BmageSourceCatalog.SourceFiles.Where(file => file.Path.StartsWith("Samples/", StringComparison.Ordinal)).ToArray();
        Assert.HasCount(6, samples);
        Assert.IsTrue(samples.All(file => file.Kind == "Sample"), string.Join(", ", samples.Select(file => $"{file.Path}: {file.Kind}")));
        Assert.Contains("MAUI", samples.Single(file => file.Path.EndsWith(".csproj", StringComparison.Ordinal)).Description!);
    }

    [TestMethod]
    public void Sources_PathsAreForgiving_AndLineCountsAreReal()
    {
        Assert.IsNotNull(BmageSourceCatalog.GetSourceFile("demo\\client\\pages\\avatar.razor"));
        Assert.IsNotNull(BmageSourceCatalog.GetSourceFile("/Demo/Client/Pages/Avatar.razor"));
        Assert.IsNull(BmageSourceCatalog.GetSourceFile("Demo/Client/Pages/Nowhere.razor"));

        var avatar = BmageSourceCatalog.SourceFiles.Single(file => file.Path == "Demo/Client/Pages/Avatar.razor");
        var lines = File.ReadAllLines(Path.Combine(BmageRoot, "Demo", "Client", "Pages", "Avatar.razor")).Length;

        Assert.AreEqual(lines, avatar.Lines);
    }

    [TestMethod]
    public void Sources_NothingFromBinOrObj_IsEmbedded()
    {
        Assert.DoesNotContain(file => file.Path.Contains("/obj/", StringComparison.OrdinalIgnoreCase) || file.Path.Contains("/bin/", StringComparison.OrdinalIgnoreCase), BmageSourceCatalog.SourceFiles);
    }
}
