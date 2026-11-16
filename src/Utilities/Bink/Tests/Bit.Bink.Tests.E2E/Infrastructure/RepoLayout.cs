namespace Bit.Bink.Tests.E2E.Infrastructure;

/// <summary>
/// Finds the demo server from the test binary's location by walking up, so a run does not depend on
/// the working directory the shell, the IDE or the test runner picked.
/// </summary>
public static class RepoLayout
{
    public const string DemoServerName = "Bit.Bink.Demo.Server";

    public static string DemoServerProject() => FindUpward(Path.Combine("Demo", "Server", $"{DemoServerName}.csproj"));

    /// <summary>Where the suite publishes the demo when it publishes it itself: next to the test binaries.</summary>
    public static string PublishDirectory() => Path.Combine(AppContext.BaseDirectory, "demo");

    public static string DemoServerAssembly(string directory) => Path.Combine(directory, $"{DemoServerName}.dll");

    /// <summary>
    /// Every file a publish of the demo is made from: the library and both demo projects (less what their builds
    /// write), Bink's README the server embeds, and the build settings and SDK pin above them.
    /// </summary>
    public static IEnumerable<string> DemoInputs()
    {
        var bink = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(DemoServerProject())!, "..", ".."));
        foreach (var folder in new[] { "Bit.Bink", "Demo" })
            foreach (var file in SourceFiles(new DirectoryInfo(Path.Combine(bink, folder)))) yield return file;

        yield return Path.Combine(bink, "README.md");

        string[] settings = ["Bit.Build.props", "global.json", "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "NuGet.config"];
        for (var directory = new DirectoryInfo(bink); directory is not null; directory = directory.Parent)
            foreach (var name in settings)
                if (File.Exists(Path.Combine(directory.FullName, name))) yield return Path.Combine(directory.FullName, name);
    }

    private static IEnumerable<string> SourceFiles(DirectoryInfo directory)
    {
        if (directory.Exists is false) yield break;

        foreach (var file in directory.EnumerateFiles()) yield return file.FullName;
        foreach (var child in directory.EnumerateDirectories())
        {
            if (child.Name is "bin" or "obj" or "node_modules" || child.Name.StartsWith('.')) continue;
            foreach (var file in SourceFiles(child)) yield return file;
        }
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
