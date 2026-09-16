namespace Bit.Brouter.Tests.E2E.Infrastructure;

/// <summary>
/// Builds the harness hosts before the first test, so a run always drives the current sources.
/// Serial on purpose: both hosts build Bit.Brouter and the harness library into the same obj folders.
/// </summary>
public static class HarnessBuild
{
    public static async Task EnsureBuiltAsync()
    {
        if (E2EEnvironment.SkipBuild || E2EEnvironment.PublishedHost is not null) return;

        await BuildAsync(RepoLayout.WebHostProject(), "-f", E2EEnvironment.Framework);

        if (OperatingSystem.IsWindows())
        {
            await BuildAsync(RepoLayout.HybridHostProject());
        }
    }

    private static Task BuildAsync(string project, params string[] extraArguments) =>
        ChildProcess.RunToCompletionAsync("dotnet",
            ["build", project, "-c", E2EEnvironment.Configuration, "-nologo", "-v:q", .. extraArguments],
            Path.GetDirectoryName(project)!,
            TimeSpan.FromMinutes(15));
}
