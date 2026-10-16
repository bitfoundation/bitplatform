namespace Bit.Bmage.Tests.E2E.Infrastructure;

/// <summary>
/// Finds the demo host from the test binary's location by walking up, so a run does not depend on the
/// working directory the shell, the IDE or the test runner picked.
/// </summary>
public static class RepoLayout
{
    public const string ServerName = "Bit.Bmage.Demo.Server";

    public static string ServerProject() => FindUpward(Path.Combine("Bit.Bmage.Demo", "Server", $"{ServerName}.csproj"));

    public static string ServerDirectory() => Path.GetDirectoryName(ServerProject())!;

    public static string ServerAssembly(string configuration) =>
        Path.Combine(ServerDirectory(), "bin", configuration, "net10.0", $"{ServerName}.dll");

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
