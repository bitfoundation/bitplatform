namespace Bit.Bmotion.Tests.E2E.Infrastructure;

/// <summary>
/// Builds the harness host before the first test, so a run always drives the current sources - the library, its
/// TypeScript bridge (compiled by the library's own build) and the harness pages.
/// </summary>
public static class HarnessBuild
{
    public static Task EnsureBuiltAsync()
    {
        if (E2EEnvironment.SkipBuild || E2EEnvironment.PublishedHost is not null) return Task.CompletedTask;

        var project = RepoLayout.WebHarnessProject();
        return ChildProcess.RunToCompletionAsync("dotnet",
            ["build", project, "-c", E2EEnvironment.Configuration, "-f", E2EEnvironment.Framework, "-nologo", "-v:q"],
            Path.GetDirectoryName(project)!,
            TimeSpan.FromMinutes(15));
    }
}
