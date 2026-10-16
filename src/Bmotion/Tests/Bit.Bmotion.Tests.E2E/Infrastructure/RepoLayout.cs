namespace Bit.Bmotion.Tests.E2E.Infrastructure;

/// <summary>
/// Finds files of this repository from the test binary's location, by walking up from the output directory rather
/// than by a relative path - so a run does not depend on which working directory the shell, the IDE or the test
/// runner happened to pick.
/// </summary>
public static class RepoLayout
{
    public const string WebHarnessName = "Bit.Bmotion.Tests.Harness.Web";

    /// <summary>The full path of <paramref name="relativePath"/> under the nearest ancestor of the output directory that has it.</summary>
    public static string FindUpward(string relativePath)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate) || Directory.Exists(candidate)) return candidate;
        }

        throw new DirectoryNotFoundException($"Could not find {relativePath} walking up from {AppContext.BaseDirectory}.");
    }

    /// <summary>The Blazor Web App that serves the harness pages in every render mode.</summary>
    public static string WebHarnessProject()
        => FindUpward(Path.Combine("Tests", WebHarnessName, $"{WebHarnessName}.csproj"));

    public static string WebHarnessAssembly(string framework, string configuration)
        => Path.Combine(Path.GetDirectoryName(WebHarnessProject())!, "bin", configuration, framework, $"{WebHarnessName}.dll");

    /// <summary>The TypeScript source of the browser bridge, whose exports the interop contract test checks.</summary>
    public static string BridgeSource()
        => FindUpward(Path.Combine("Bit.Bmotion", "Scripts", "bit-bmotion.ts"));

    /// <summary>The C# side of the bridge, whose invoked identifiers the interop contract test checks.</summary>
    public static string InteropSource()
        => FindUpward(Path.Combine("Bit.Bmotion", "Interop", "BmotionInterop.cs"));
}
