namespace Bit.BbScanner.Tests.E2E.Infrastructure;

/// <summary>
/// Finds the demo server from the test binary's location by walking up, so a run does not depend on
/// the working directory the shell, the IDE or the test runner picked.
/// </summary>
public static class RepoLayout
{
    public const string DemoServerName = "Bit.BbScanner.Demo.Server";

    public static string DemoServerProject() => FindUpward(Path.Combine("Bit.BbScanner.Demo", "Server", $"{DemoServerName}.csproj"));

    public static string DemoServerAssembly(string configuration) =>
        Path.Combine(Path.GetDirectoryName(DemoServerProject())!, "bin", configuration, "net10.0", $"{DemoServerName}.dll");

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
