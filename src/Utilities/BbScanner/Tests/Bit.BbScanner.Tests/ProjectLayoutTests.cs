using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Bit.BbScanner.Tests;

/// <summary>
/// What the projects and their guides say of each other: the MAUI package is built for every .NET the scanner is, and
/// the end-to-end guide's commands point at the folder its suite is built into.
/// </summary>
[TestClass]
public partial class ProjectLayoutTests
{
    [TestMethod]
    public void TheMauiPackageIsBuiltForEveryDotNetTheScannerIs()
    {
        // Built for .NET 10 alone, a .NET 11 app took Bit.BbScanner twice - the .NET 10 build through Bit.BbScanner.Maui
        // and the .NET 11 build of its own - and packaging it for Windows failed on their two static web asset manifests.
        var scanner = TargetFrameworks("Bit.BbScanner/Bit.BbScanner.csproj").Single().Split(';');
        var maui = TargetFrameworks("Bit.BbScanner.Maui/Bit.BbScanner.Maui.csproj");

        foreach (var framework in scanner)
        {
            Assert.Contains($"{framework}-android", maui[0], "Every platform");
            Assert.Contains($"{framework}-ios", maui[0], "Every platform");
            Assert.Contains($"{framework}-maccatalyst", maui[0], "Every platform");
            Assert.IsTrue(maui.Any(line => line.Contains($"{framework}-windows", StringComparison.Ordinal)), $"Windows, {framework}");
            Assert.IsTrue(maui.All(line => line.Contains($"{framework}-", StringComparison.Ordinal)), $"Linux, {framework}");
        }
    }

    [TestMethod]
    public void TheEndToEndGuidePointsAtTheFolderItsSuiteIsBuiltInto()
    {
        var framework = XDocument.Load(PathOf("Tests/Bit.BbScanner.Tests.E2E/Bit.BbScanner.Tests.E2E.csproj"))
            .Descendants("TargetFramework").Single().Value;
        var readme = File.ReadAllText(PathOf("Tests/Bit.BbScanner.Tests.E2E/README.md"));

        var folders = OutputFolder().Matches(readme).Select(match => match.Groups["framework"].Value).ToList();
        Assert.IsNotEmpty(folders);
        foreach (var folder in folders)
        {
            Assert.AreEqual(framework, folder);
        }
    }

    private static string[] TargetFrameworks(string project)
        => [.. XDocument.Load(PathOf(project)).Descendants("TargetFrameworks").Select(element => element.Value)];

    // This file is Tests/Bit.BbScanner.Tests/ProjectLayoutTests.cs under the BbScanner folder.
    private static string PathOf(string relative, [CallerFilePath] string self = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(self)!, "..", "..", relative));

    [GeneratedRegex(@"bin[/\\](?:Debug|Release)[/\\](?<framework>net\d+\.\d+)[/\\]")]
    private static partial Regex OutputFolder();
}
