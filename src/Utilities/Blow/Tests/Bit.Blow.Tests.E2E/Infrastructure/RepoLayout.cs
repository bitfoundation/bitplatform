namespace Bit.Blow.Tests.E2E.Infrastructure;

/// <summary>
/// Finds the demo server from the test binary's location by walking up, so a run does not depend on
/// the working directory the shell, the IDE or the test runner picked.
/// </summary>
public static class RepoLayout
{
    public const string DemoServerName = "Bit.Blow.Demo.Server";

    public static string DemoServerProject() => FindUpward(Path.Combine("Demo", "Server", $"{DemoServerName}.csproj"));

    public static string DemoServerAssembly(string configuration) =>
        Path.Combine(Path.GetDirectoryName(DemoServerProject())!, "bin", configuration, "net10.0", $"{DemoServerName}.dll");

    /// <summary>The demo's routable pages, which the page sweep reads its routes from.</summary>
    public static string DemoPagesDirectory() =>
        Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(DemoServerProject()))!, "Client", "Pages");

    /// <summary>
    /// The route of every page under <see cref="DemoPagesDirectory"/>, read from its <c>@page</c> directive, so a
    /// page added to the demo is swept without anyone having to list it here.
    /// </summary>
    public static IReadOnlyList<string> DemoRoutes() =>
        [.. Directory.EnumerateFiles(DemoPagesDirectory(), "*.razor", SearchOption.AllDirectories)
            .Select(file => System.Text.RegularExpressions.Regex.Match(File.ReadAllText(file), "@page\\s+\"([^\"]+)\""))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)
            .Order(StringComparer.Ordinal)];

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
