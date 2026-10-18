namespace Bit.Bsqlite.Tests.E2E.Infrastructure;

/// <summary>
/// Finds the libraries and the demo server from the test binary's location by walking up, so a run does not
/// depend on the working directory the shell, the IDE or the test runner picked.
/// </summary>
public static class RepoLayout
{
    public const string DemoServerName = "Bit.Bsqlite.Demo.Server";

    /// <summary>The packages whose wwwroot is served under <c>_content/&lt;package&gt;/</c>, as a Blazor app maps them.</summary>
    public static readonly string[] Packages =
    [
        "Bit.Bsqlite",
        "Bit.Bsqlite.Storage.Opfs",
        "Bit.Bsqlite.Storage.IndexedDb",
        "Bit.Bsqlite.Storage.CacheStorage",
    ];

    /// <summary>The src/Bsqlite folder.</summary>
    public static string BsqliteRoot() => Path.GetDirectoryName(FindUpward("Bit.Bsqlite.slnx"))!;

    /// <summary>A package's wwwroot: its compiled modules plus whatever its restore vendored there.</summary>
    public static string WebRoot(string package) => Path.Combine(BsqliteRoot(), package, "wwwroot");

    /// <summary>The pages the asset host serves at its root, copied next to the test assembly.</summary>
    public static string TestPages() => Path.Combine(AppContext.BaseDirectory, "TestPages");

    public static string DemoServerProject() => Path.Combine(BsqliteRoot(), "Bit.Bsqlite.Demo", "Server", $"{DemoServerName}.csproj");

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
