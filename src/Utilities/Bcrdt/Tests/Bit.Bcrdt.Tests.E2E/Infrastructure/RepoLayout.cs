using System.Net;
using System.Text.RegularExpressions;

namespace Bit.Bcrdt.Tests.E2E.Infrastructure;

/// <summary>
/// Finds the demo server from the test binary's location by walking up, so a run does not depend on
/// the working directory the shell, the IDE or the test runner picked.
/// </summary>
public static class RepoLayout
{
    public const string DemoServerName = "Bit.Bcrdt.Demo.Server";

    public static string DemoServerProject() => FindUpward(Path.Combine("Demo", "Server", $"{DemoServerName}.csproj"));

    public static string DemoServerAssembly(string configuration) =>
        Path.Combine(Path.GetDirectoryName(DemoServerProject())!, "bin", configuration, "net11.0", $"{DemoServerName}.dll");

    /// <summary>The demo's routable pages, which the page sweep reads its routes from.</summary>
    public static string DemoPagesDirectory() =>
        Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(DemoServerProject()))!, "Client", "Pages");

    /// <summary>
    /// Every route of every page under <see cref="DemoPagesDirectory"/>, read from its <c>@page</c> directives, with
    /// the text of the page's <c>h1</c> as its source spells it, so a page added to the demo is swept without anyone
    /// having to list it here.
    /// </summary>
    public static IReadOnlyList<(string Route, string Heading)> DemoPages() =>
        [.. Directory.EnumerateFiles(DemoPagesDirectory(), "*.razor", SearchOption.AllDirectories)
            .Select(File.ReadAllText)
            .SelectMany(source => Regex.Matches(source, "@page\\s+\"([^\"]+)\"")
                .Select(route => (route.Groups[1].Value, Heading(source))))
            .OrderBy(page => page.Item1, StringComparer.Ordinal)];

    /// <summary>
    /// The text of the page's first <c>h1</c>, without markup, or an empty string when it has none or when the
    /// source computes it (a Razor expression), which leaves only its presence to check.
    /// </summary>
    private static string Heading(string source)
    {
        var h1 = Regex.Match(source, "<h1[^>]*>(.*?)</h1>", RegexOptions.Singleline);
        if (h1.Success is false || h1.Groups[1].Value.Contains('@')) return "";
        return WebUtility.HtmlDecode(Regex.Replace(Regex.Replace(h1.Groups[1].Value, "<[^>]+>", ""), "\\s+", " ")).Trim();
    }

    private static string FindUpward(string relativePath)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate)) return candidate;
        }

        throw new FileNotFoundException($"Could not find {relativePath} walking up from {AppContext.BaseDirectory}.");
    }
}
