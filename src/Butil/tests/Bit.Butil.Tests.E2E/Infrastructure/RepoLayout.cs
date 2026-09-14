namespace Bit.Butil.Tests.E2E.Infrastructure;

/// <summary>
/// Finds files of this repository from a test binary's location, by walking up from the output
/// directory rather than by a relative path - so a run does not depend on which working directory
/// the shell, the IDE or the test runner happened to pick, or on how deep the output folder is.
/// </summary>
/// <remarks>
/// Shared by the E2E suite and, as a linked source file, by the benchmark suite: one walk-up with
/// one stop condition, so a re-layout of the repository breaks in one place with one message.
/// </remarks>
public static class RepoLayout
{
    /// <summary>
    /// The full path of <paramref name="relativePath"/> under the nearest ancestor of the output
    /// directory that has it. <c>Bit.Butil/Bit.Butil.csproj</c> resolves to the library's project
    /// file, <c>Samples/Bit.Butil.Samples.Web/Bit.Butil.Samples.Web.csproj</c> to the sample's.
    /// </summary>
    public static string FindUpward(string relativePath)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate) || Directory.Exists(candidate)) return candidate;
        }

        throw new DirectoryNotFoundException($"Could not find {relativePath} walking up from {AppContext.BaseDirectory}.");
    }

    /// <summary>The <c>src/Butil</c> folder: the one holding <c>Bit.Butil/Bit.Butil.csproj</c>.</summary>
    public static string ButilRoot()
        => Path.GetDirectoryName(Path.GetDirectoryName(FindUpward(Path.Combine("Bit.Butil", "Bit.Butil.csproj"))))!;

    /// <summary>The <c>Bit.Butil</c> project folder, where <c>build.mjs</c> and the build outputs live.</summary>
    public static string ButilProject() => Path.Combine(ButilRoot(), "Bit.Butil");

    /// <summary>The project file of the standalone WebAssembly sample the browser suites drive.</summary>
    public static string SampleWebProject()
        => FindUpward(Path.Combine("Samples", "Bit.Butil.Samples.Web", "Bit.Butil.Samples.Web.csproj"));
}
