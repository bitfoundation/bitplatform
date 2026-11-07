//+:cnd:noEmit
using System.Text.RegularExpressions;

namespace Boilerplate.Tests.Features.TemplateConfig;

/// <summary>
/// <c>"sourceName": "Boilerplate"</c> makes the template engine rewrite that word everywhere it appears - file names,
/// namespaces, and <b>the inside of string literals</b>, including absolute urls. That is exactly what is wanted for
/// the project's own identifiers and exactly wrong for a url that points at somebody else's host: the emitted project
/// gets <c>https://github.com/bitfoundation/bitplatform/tree/develop/src/Templates/Contoso</c>, which is a 404.
/// <para>
/// The failure is silent in every way that matters. It compiles, it renders, the link is a decorative badge nobody
/// clicks during development, and it only appears in a <b>generated</b> project - never in this repository, where the
/// url is correct. The author of the defect had even wrapped the region in <c>@*-:cnd:noEmit*@</c> believing it was
/// protected; that pragma toggles <i>conditional processing</i> only and has no bearing on the rename, so the guard
/// read as protection while doing nothing.
/// </para>
/// <para>
/// This is a source scan rather than a generation test on purpose: generating a project takes tens of seconds and
/// proves one configuration, while the property being defended - "no foreign url contains the sourceName" - is a
/// property of the source text and holds for all 22 of them.
/// </para>
/// <para>
/// This runs against the template's own working copy. A generated project has no <c>.template.config</c> directory,
/// so there the test reports inconclusive rather than failing.
/// </para>
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class SourceNameLeakTests
{
    /// <summary>
    /// The value of <c>sourceName</c> in <c>.template.config/template.json</c>.
    /// </summary>
    private const string SourceName = "Boilerplate";

    /// <summary>
    /// Hosts the generated app legitimately owns, where a rewritten <c>Boilerplate</c> is either correct or harmless:
    /// the placeholder web-app url the template tells the user to replace, and localhost.
    /// </summary>
    private static readonly string[] OwnHosts = ["use-your-web-app-url-here.com", "localhost", "0.0.0.1", "127.0.0.1"];

    private const string LowercaseSourceName = "boilerplate";

    private static readonly string[] ScannedExtensions = [".razor", ".cs", ".html", ".cshtml", ".ts", ".json", ".md", ".yml"];

    [TestMethod]
    public void NoForeignUrl_Should_ContainTheSourceName()
    {
        var root = GetTemplateRoot();

        var urlPattern = new Regex(@"https?://[^\s""'<>)\]}\\]+", RegexOptions.Compiled);

        var offenders = new List<string>();

        foreach (var file in EnumerateSourceFiles(root))
        {
            var text = File.ReadAllText(file);
            if (text.Contains(SourceName, StringComparison.Ordinal) is false) continue;

            var lineNumber = 0;
            foreach (var line in text.Split('\n'))
            {
                lineNumber++;

                foreach (Match match in urlPattern.Matches(line))
                {
                    var url = match.Value.TrimEnd('.', ',', ';', ':');

                    if (url.Contains(SourceName, StringComparison.Ordinal) is false) continue;
                    if (Uri.TryCreate(url, UriKind.Absolute, out var uri) is false) continue;
                    if (OwnHosts.Any(h => uri.Host.Equals(h, StringComparison.OrdinalIgnoreCase))) continue;

                    offenders.Add($"{Path.GetRelativePath(root, file)}:{lineNumber} -> {url}");
                }
            }
        }

        Assert.IsEmpty(offenders,
            $"An absolute url pointing at a host the generated app does not own contains the sourceName " +
            $"'{SourceName}', so `dotnet new -n Contoso` will rewrite it and ship a broken link. Either drop the " +
            $"renameable segment from the url or build it from a token that does not match the sourceName. " +
            $"Note that a `cnd:noEmit` pragma does NOT prevent this - it only toggles conditional processing." +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [TestMethod]
    public void BitPlatformRepositoryPath_Should_NotContainTheLowercaseSourceName()
    {
        var root = GetTemplateRoot();

        var pathPattern = new Regex($@"bitfoundation/bitplatform[^\s""'<>)\]}}\\]*{LowercaseSourceName}[^\s""'<>)\]}}\\]*", RegexOptions.Compiled);

        var offenders = new List<string>();

        foreach (var file in EnumerateSourceFiles(root))
        {
            var lineNumber = 0;
            foreach (var line in File.ReadAllText(file).Split('\n'))
            {
                lineNumber++;

                foreach (Match match in pathPattern.Matches(line))
                {
                    offenders.Add($"{Path.GetRelativePath(root, file)}:{lineNumber} -> {match.Value}");
                }
            }
        }

        Assert.IsEmpty(offenders,
            $"A path in the bitplatform repository contains '{LowercaseSourceName}', which the template engine rewrites " +
            $"along with the sourceName, so `dotnet new -n Contoso` names a folder that doesn't exist there. A symbol that " +
            $"replaces a token with itself doesn't help, the engine drops it: name that folder without the word." +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    private static IEnumerable<string> EnumerateSourceFiles(string root)
    {
        return Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                        .Where(f => ScannedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                        .Where(f => f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") is false)
                        .Where(f => f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") is false)
                        .Where(f => f.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}") is false);
    }

    /// <summary>
    /// Walks up from the test assembly to the directory that owns <c>.template.config/template.json</c>, which is the
    /// same anchor <c>TemplateConfigurationTests</c> uses.
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
