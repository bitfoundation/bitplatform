namespace Bit.Minifier.Tests;

[TestClass]
public class BitAssembliesTests
{
    [TestMethod]
    public void EveryBitPackageIsMinifiedEvenWhenADemoBuildsItFromSource()
    {
        var src = Src();
        string[] products = ["BlazorUI", "Brouter", "Bswup", "Butil", "Utilities"];
        var packages = products
            .SelectMany(product => Projects(src, product))
            .Where(project => IsApp(src, project) is false)
            .Where(project => File.ReadAllText(project).Contains("<IsPackable>false</IsPackable>", StringComparison.Ordinal) is false)
            .Where(project => File.ReadAllText(project).Contains("<PackAsTool>true</PackAsTool>", StringComparison.Ordinal) is false)
            .Select(project => Path.GetFileNameWithoutExtension(project))
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
        var apps = Projects(src, ".")
            .Where(project => IsApp(src, project))
            .Select(project => Path.GetFileNameWithoutExtension(project))
            .ToList();

        Assert.IsTrue(apps.Count > 100);
        var matched = apps.Where(name => MinifierOptions.Matches(MinifierOptions.BitAssemblies, name)).ToList();
        Assert.AreEqual(0, matched.Count, $"Apps the bit assemblies' patterns take for packages: {string.Join(", ", matched)}");
    }

    [TestMethod]
    [DataRow("Bit.Bnew", true)]
    [DataRow("bit.bnew.server", true)]
    [DataRow("Bit.Bnew.Testing", true)]
    [DataRow("Bit.Bnew.Demo", false)]
    [DataRow("Bit.Bnew.Demo.Client", false)]
    [DataRow("Bit.Bnew.Samples.Wasm.Client", false)]
    [DataRow("Bit.Bnew.Tests.E2E", false)]
    [DataRow("Bit.Websites.Bnew.Client", false)]
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
        Assert.IsTrue(MinifierOptions.Matches(["!*.Friend", "Bit.*"], "Bit.Minifier.Tests.Library"));
        Assert.IsTrue(MinifierOptions.Matches(["Bit.Minifier.Tests.Library"], "Bit.Minifier.Tests.Library"));
        Assert.IsFalse(MinifierOptions.Matches(["Bit.Minifier.Tests.Library"], "Bit.Minifier.Tests.Library2"));
        Assert.IsFalse(MinifierOptions.Matches([], "Bit.Minifier.Tests.Library"));
    }

    private static string Src()
    {
        var src = new DirectoryInfo(AppContext.BaseDirectory);
        while (src is not null && File.Exists(Path.Combine(src.FullName, "Bit.slnx")) is false) src = src.Parent;
        Assert.IsNotNull(src, "No src/Bit.slnx above the tests.");
        return src.FullName;
    }

    private static IEnumerable<string> Projects(string src, string folder) =>
        Directory.EnumerateFiles(Path.Combine(src, folder), "Bit.*.csproj", SearchOption.AllDirectories)
            .Where(project => Folders(src, project).Any(f => f is "bin" or "obj" or "node_modules") is false);

    private static bool IsApp(string src, string project) =>
        Folders(src, project).Any(folder => folder is "Demo" or "Tests" or "tests" or "Samples" or "Websites" || folder.Contains(".Demo", StringComparison.Ordinal));

    private static string[] Folders(string src, string project) =>
        Path.GetRelativePath(src, Path.GetDirectoryName(project)!).Split(Path.DirectorySeparatorChar);
}
