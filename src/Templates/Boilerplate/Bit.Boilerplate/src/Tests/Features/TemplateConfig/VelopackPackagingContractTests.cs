//+:cnd:noEmit
using System.Text.RegularExpressions;

namespace Boilerplate.Tests.Features.TemplateConfig;

/// <summary>
/// Two properties of the Windows head's Velopack wiring that nothing else can notice: both hold across all 22
/// template configurations, neither is visible while the app runs, and neither fails a build.
/// <para>
/// Source scans on purpose. The packaging path they defend only executes inside <c>vpk pack</c> on a CI agent and
/// inside Velopack's own install/update hooks on an end user's machine - neither of which this suite can drive - and
/// what is being asserted is a property of the source text either way. <see cref="GetTemplateRoot"/> anchors on
/// <c>.template.config/template.json</c>, which is itself excluded from generated output, so a generated project
/// reports inconclusive rather than failing on files it does not have.
/// </para>
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class VelopackPackagingContractTests
{
    /// <summary>
    /// The <c>vpk</c> tool packs the app; the <c>Velopack</c> library runs inside it. Velopack's own
    /// <c>CompatUtil.VerifyVelopackVersion</c> compares the two during <c>vpk pack</c> and, when the library is the
    /// newer of the pair, logs an error saying the mismatch "can cause compatibility issues" and that "in a future
    /// version this may become a fatal error".
    /// <para>
    /// This drifts silently and will drift again: <c>.config/dotnet-tools.json</c> is not an MSBuild file, so the
    /// dependency-update commits that move <c>PackageVersion</c> never touch it. That is exactly what happened here -
    /// the tool was pinned to 1.0.1, and the library was moved to 1.2.0 eleven days later by a bulk dependency bump.
    /// Nothing failed; the CD log simply gained an error line nobody reads.
    /// </para>
    /// </summary>
    [TestMethod]
    public void VpkToolVersion_Should_MatchThePinnedVelopackLibraryVersion()
    {
        var toolsManifest = ReadTemplateFile("src/Client/Boilerplate.Client.Windows/.config/dotnet-tools.json");
        var packageVersions = ReadTemplateFile("src/Directory.Packages.props");

        var toolVersion = Regex.Match(toolsManifest, """"vpk"\s*:\s*\{[^}]*?"version"\s*:\s*"(?<version>[^"]+)"""",
                                      RegexOptions.Singleline).Groups["version"].Value;
        var libraryVersion = Regex.Match(packageVersions, """<PackageVersion\s+Include="Velopack"\s+Version="(?<version>[^"]+)""")
                                  .Groups["version"].Value;

        Assert.AreNotEqual(string.Empty, toolVersion, "No vpk entry in the Windows head's dotnet-tools.json - if the tool was renamed or removed, retarget this test rather than deleting it.");
        Assert.AreNotEqual(string.Empty, libraryVersion, "No Velopack PackageVersion in src/Directory.Packages.props - if the package was renamed or removed, retarget this test rather than deleting it.");

        Assert.AreEqual(libraryVersion, toolVersion,
            $"The vpk tool is pinned to {toolVersion} while the Velopack library is pinned to {libraryVersion}. " +
            "vpk pack compares the two and logs a compatibility error when the library is newer. Move the tool " +
            "manifest with the package version - a dependency bump cannot do it for you, because dotnet-tools.json " +
            "is not an MSBuild file.");
    }

    /// <summary>
    /// Velopack re-launches the app's own exe with <c>--veloapp-install</c> / <c>--veloapp-updated</c> /
    /// <c>--veloapp-obsolete</c> / <c>--veloapp-uninstall</c> and expects <c>VelopackApp.Build().Run()</c> to
    /// recognise the argument and terminate. Its own documentation says the call "should be used as early as possible
    /// in your application startup code. (eg. the beginning of Main() in Program.cs)".
    /// <para>
    /// Everything placed above it executes during every one of those hooks. It had drifted to roughly fifty lines
    /// down, behind the WebView2 runtime probe, the whole DI graph, options validation, a synchronous isolated-storage
    /// read and the construction of a <c>Form</c> - so a throw in any of them aborted the hook, and Velopack kills a
    /// hook process after 30 seconds and reports the install as successful anyway. None of that is reachable on a
    /// developer machine, where hooks never run.
    /// </para>
    /// </summary>
    [TestMethod]
    public void VelopackApp_Should_RunBeforeAnythingElseInMain()
    {
        var program = ReadTemplateFile("src/Client/Boilerplate.Client.Windows/Program.cs");

        var mainIndex = program.IndexOf("public static void Main(", StringComparison.Ordinal);
        Assert.AreNotEqual(-1, mainIndex, "No Main entry point in the Windows head's Program.cs - if it was renamed, retarget this test.");

        var runIndex = program.IndexOf("VelopackApp.Build().Run()", mainIndex, StringComparison.Ordinal);
        Assert.AreNotEqual(-1, runIndex, "VelopackApp.Build().Run() is gone from Main. Velopack's install/update/uninstall hooks re-launch this exe and rely on it to exit; without it they run the whole app instead.");

        // Every statement between the opening brace of Main and the Run() call, ignoring comments and blank lines.
        var bodyStart = program.IndexOf('{', mainIndex) + 1;
        var statementsBefore = program[bodyStart..runIndex]
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && line.StartsWith("//", StringComparison.Ordinal) is false)
            .ToArray();

        Assert.AreEqual(0, statementsBefore.Length,
            "VelopackApp.Build().Run() must be the first statement of Main, because everything above it also runs " +
            "during every Velopack install/update/uninstall hook - and a throw there aborts the hook silently. " +
            $"Found before it: {string.Join(" | ", statementsBefore)}");
    }

    private static string ReadTemplateFile(string relativePath)
    {
        var path = Path.Combine(GetTemplateRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

        Assert.IsTrue(File.Exists(path), $"{relativePath} does not exist under the template root. If the file moved, " +
                                         "point this test at its new home rather than deleting the assertion.");

        return File.ReadAllText(path);
    }

    /// <summary>
    /// Walks up from the test assembly to the directory that owns <c>.template.config/template.json</c>, which is the
    /// same anchor <c>HybridHostHardeningTests</c> and <c>TemplateConfigurationTests</c> use.
    /// </summary>
    private static string GetTemplateRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null &&
               File.Exists(Path.Combine(directory.FullName, ".template.config", "template.json")) is false)
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            Assert.Inconclusive("No .template.config/template.json above the test binaries - this is a generated project, not the template's own tree.");
            return default!;
        }

        return directory.FullName;
    }
}
