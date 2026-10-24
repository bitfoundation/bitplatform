namespace Bit.Bmage.Tests.E2E.Infrastructure;

/// <summary>
/// Finds the demo host from the test binary's location by walking up, so a run does not depend on the
/// working directory the shell, the IDE or the test runner picked.
/// </summary>
public static class RepoLayout
{
    public const string ServerName = "Bit.Bmage.Demo.Server";

    public static string ServerProject() => FindUpward(Path.Combine("Demo", "Server", $"{ServerName}.csproj"));

    public static string ServerDirectory() => Path.GetDirectoryName(ServerProject())!;

    public static string ServerAssembly(string configuration) =>
        Path.Combine(ServerDirectory(), "bin", configuration, "net10.0", $"{ServerName}.dll");

    /// <summary>The host project of a hosting sample: Server, Wasm or Auto.</summary>
    public static string SampleProject(string sample) => FindUpward(sample == SampleHosts.Server
        ? Path.Combine("Samples", "Server", "Bit.Bmage.Samples.Server.csproj")
        : Path.Combine("Samples", sample, $"Bit.Bmage.Samples.{sample}", $"Bit.Bmage.Samples.{sample}.csproj"));

    public static string SampleAssembly(string sample, string configuration) =>
        Path.Combine(Path.GetDirectoryName(SampleProject(sample))!, "bin", configuration, "net10.0", $"{Path.GetFileNameWithoutExtension(SampleProject(sample))}.dll");

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
