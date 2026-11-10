using System.Xml.Linq;

namespace Bit.Minifier.Tests;

[TestClass]
public class BitAssembliesTests
{
    [TestMethod]
    public void EveryBitPackageIsMinifiedEvenWhenADemoBuildsItFromSource()
    {
        var src = Src();
        var packages = Projects(src)
            .Where(project => IsApp(src, project) is false)
            .Where(project => File.ReadAllText(project).Contains("<IsPackable>false</IsPackable>", StringComparison.Ordinal) is false)
            .Where(project => File.ReadAllText(project).Contains("<PackAsTool>true</PackAsTool>", StringComparison.Ordinal) is false)
            // an analyzer, a template or a build tool: a package with no assembly of its own for a publish to ship
            .Where(project => File.ReadAllText(project).Contains("<IncludeBuildOutput>false</IncludeBuildOutput>", StringComparison.Ordinal) is false)
            .Select(AssemblyName)
            .Where(name => name.EndsWith("Generators", StringComparison.Ordinal) is false)
            .ToList();

        Assert.IsTrue(packages.Count > 30);
        var missed = packages.Where(name => MinifierOptions.Matches(MinifierOptions.BitAssemblies, name) is false).ToList();
        Assert.AreEqual(0, missed.Count, $"Packages the bit assemblies' patterns miss: {string.Join(", ", missed)}");
    }

    [TestMethod]
    public void TheDemosSamplesTestsAndWebsitesStayTheAppsOwnCode()
    {
        // what the repository publishes as an app keeps its names like any app's own code
        var src = Src();
        var apps = Projects(src)
            .Where(project => IsApp(src, project))
            .Select(AssemblyName)
            .ToList();

        Assert.IsTrue(apps.Count > 100);
        var matched = apps.Where(name => MinifierOptions.Matches(MinifierOptions.BitAssemblies, name)).ToList();
        Assert.AreEqual(0, matched.Count, $"Apps the bit assemblies' patterns take for packages: {string.Join(", ", matched)}");
    }

    [TestMethod]
    [DataRow("Bit.Bnew", true)]
    [DataRow("bit.bnew.server", true)]
    [DataRow("Bit.Bnew.Testing", true)]
    [DataRow("Bit.Bnew.SampleCharts", true)]
    [DataRow("Bit.Bnew.Demo", false)]
    [DataRow("Bit.Bnew.Demo.Client", false)]
    [DataRow("Bit.Bnew.Samples.Wasm.Client", false)]
    [DataRow("Bit.Bnew.BasicSample", false)]
    [DataRow("Bit.Bnew.FullSample.Client", false)]
    [DataRow("Bit.Bnew.Tests.E2E", false)]
    [DataRow("Bit.Websites.Bnew.Client", false)]
    [DataRow("Bit.Acme.Client", false)]
    [DataRow("Microsoft.AspNetCore.Components", false)]
    [DataRow("MyApp.Client", false)]
    public void ANewPackageNeedsNothingAdded(string name, bool package)
    {
        Assert.AreEqual(package, MinifierOptions.Matches(MinifierOptions.BitAssemblies, name));
    }

    [TestMethod]
    public void AnExclusionWinsWhateverItsOrder()
    {
        Assert.IsFalse(MinifierOptions.Matches(["!*.Friend", "Bit.*"], "Bit.Minifier.Tests.Friend"));
        Assert.IsFalse(MinifierOptions.Matches(["Bit.*", "!*.FRIEND"], "Bit.Minifier.Tests.Friend"));
        Assert.IsTrue(MinifierOptions.Matches(["!*.Friend", "Bit.*"], "Bit.Minifier.Tests.Library"));
        Assert.IsTrue(MinifierOptions.Matches(["Bit.Minifier.Tests.Library"], "Bit.Minifier.Tests.Library"));
        Assert.IsFalse(MinifierOptions.Matches(["Bit.Minifier.Tests.Library"], "Bit.Minifier.Tests.Library2"));
        Assert.IsFalse(MinifierOptions.Matches([], "Bit.Minifier.Tests.Library"));
    }

    /// <summary>The repository's src folder, the one holding Bit.slnx.</summary>
    private static string Src()
    {
        var src = new DirectoryInfo(AppContext.BaseDirectory);
        while (src is not null && File.Exists(Path.Combine(src.FullName, "Bit.slnx")) is false) src = src.Parent;
        Assert.IsNotNull(src, "No src/Bit.slnx above the tests.");
        return src.FullName;
    }

    /// <summary>Every Bit.*.csproj under <paramref name="folder"/>, never descending into build outputs or node_modules.</summary>
    private static IEnumerable<string> Projects(string folder) =>
        Directory.EnumerateFiles(folder, "Bit.*.csproj")
            .Concat(Directory.EnumerateDirectories(folder)
                .Where(sub => Path.GetFileName(sub) is not ("bin" or "obj" or "node_modules"))
                .SelectMany(Projects));

    /// <summary>Whether the project is one the repository publishes as an app: a demo, sample, test or website.</summary>
    private static bool IsApp(string src, string project) =>
        Folders(src, project).Any(folder => folder is "Demo" or "Tests" or "tests" or "Samples" or "Websites" || folder.Contains(".Demo", StringComparison.Ordinal));

    /// <summary>The folders between <paramref name="src"/> and the project.</summary>
    private static string[] Folders(string src, string project) =>
        Path.GetRelativePath(src, Path.GetDirectoryName(project)!).Split(Path.DirectorySeparatorChar);

    /// <summary>The name the project's assembly gets: its own &lt;AssemblyName&gt;, or else the project's name.</summary>
    private static string AssemblyName(string project)
    {
        var projectName = Path.GetFileNameWithoutExtension(project);
        var assemblyName = XDocument.Load(project).Descendants().LastOrDefault(e => e.Name.LocalName == "AssemblyName")?.Value.Trim();
        return string.IsNullOrEmpty(assemblyName) ? projectName : assemblyName.Replace("$(MSBuildProjectName)", projectName, StringComparison.OrdinalIgnoreCase);
    }
}
