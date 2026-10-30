namespace Bit.BlazorHeadlessUI.Tests.E2E.Infrastructure;

/// <summary>
/// Finds the demo server from the test binary's location by walking up, so a run does not depend on
/// the working directory the shell, the IDE or the test runner picked.
/// </summary>
public static class RepoLayout
{
    public const string DemoServerName = "Bit.BlazorHeadlessUI.Demo.Server";

    public static string DemoServerProject() => FindUpward(Path.Combine("Demo", DemoServerName, $"{DemoServerName}.csproj"));

    /// <summary>Where the suite publishes the demo for <paramref name="renderMode"/>: next to the test binaries, one folder per mode.</summary>
    public static string PublishDirectory(string renderMode) => Path.Combine(AppContext.BaseDirectory, "demo", renderMode);

    public static string DemoServerAssembly(string directory) => Path.Combine(directory, $"{DemoServerName}.dll");

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
