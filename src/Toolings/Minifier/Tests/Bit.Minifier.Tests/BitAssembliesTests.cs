namespace Bit.Minifier.Tests;

[TestClass]
public class BitAssembliesTests
{
    [TestMethod]
    public void EveryBitPackageIsMinifiedEvenWhenADemoBuildsItFromSource()
    {
        var src = new DirectoryInfo(AppContext.BaseDirectory);
        while (src is not null && File.Exists(Path.Combine(src.FullName, "Bit.slnx")) is false) src = src.Parent;
        Assert.IsNotNull(src, "No src/Bit.slnx above the tests.");

        string[] products = ["BlazorUI", "Brouter", "Bswup", "Butil", "Utilities"];
        var packages = products
            .SelectMany(product => Directory.EnumerateFiles(Path.Combine(src.FullName, product), "Bit.*.csproj", SearchOption.AllDirectories))
            .Where(project => Path.GetRelativePath(src.FullName, project).Split(Path.DirectorySeparatorChar)
                .Any(folder => folder is "Demo" or "Tests" or "tests" or "Samples" or "bin" or "obj" or "node_modules" || folder.Contains(".Demo", StringComparison.Ordinal)) is false)
            .Where(project => File.ReadAllText(project).Contains("<IsPackable>false</IsPackable>", StringComparison.Ordinal) is false)
            .Select(project => Path.GetFileNameWithoutExtension(project))
            .Where(name => name.EndsWith("Generators", StringComparison.Ordinal) is false)
            .ToList();

        Assert.IsTrue(packages.Count > 30);
        CollectionAssert.IsSubsetOf(packages, MinifierOptions.BitAssemblies.ToList());
    }
}
