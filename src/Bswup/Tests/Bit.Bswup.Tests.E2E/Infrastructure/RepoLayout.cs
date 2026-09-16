namespace Bit.Bswup.Tests.E2E.Infrastructure;

/// <summary>
/// Finds the harness project from the test binary's location by walking up, so a run does not depend on
/// the working directory the shell, the IDE or the test runner picked.
/// </summary>
public static class RepoLayout
{
    public const string WebHostName = "Bit.Bswup.Tests.Harness.Web";

    public static string WebHostProject() => FindUpward(Path.Combine("Tests", WebHostName, $"{WebHostName}.csproj"));

    public static string WebHostAssembly(string framework, string configuration) =>
        Path.Combine(Path.GetDirectoryName(WebHostProject())!, "bin", configuration, framework, $"{WebHostName}.dll");

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
