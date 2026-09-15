namespace Bit.Bswup.Tests.E2E.Infrastructure;

/// <summary>Builds the harness host before the first test, so a run always drives the current sources.</summary>
public static class HarnessBuild
{
    public static async Task EnsureBuiltAsync()
    {
        if (E2EEnvironment.SkipBuild || E2EEnvironment.PublishedHost is not null) return;

        var project = RepoLayout.WebHostProject();

        await ChildProcess.RunToCompletionAsync("dotnet",
            ["build", project, "-c", E2EEnvironment.Configuration, "-f", E2EEnvironment.Framework, "-nologo", "-v:q"],
            Path.GetDirectoryName(project)!,
            TimeSpan.FromMinutes(15));
    }
}
