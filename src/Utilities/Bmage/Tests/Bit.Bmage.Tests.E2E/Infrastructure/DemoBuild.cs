namespace Bit.Bmage.Tests.E2E.Infrastructure;

/// <summary>
/// Builds the demo host before the first test, so a run always drives the current sources - the
/// library, the editor and the demo pages alike. Skipped when the run drives an external deployment
/// (<c>BMAGE_E2E_HOST_URL</c>) or the host was built beforehand (<c>BMAGE_E2E_SKIP_BUILD=1</c>).
/// </summary>
public static class DemoBuild
{
    public static Task EnsureBuiltAsync()
    {
        if (E2EEnvironment.SkipBuild || E2EEnvironment.HostUrl is not null) return Task.CompletedTask;

        var project = RepoLayout.ServerProject();
        return ChildProcess.RunToCompletionAsync("dotnet",
            ["build", project, "-c", E2EEnvironment.Configuration, "-nologo", "-v:q"],
            Path.GetDirectoryName(project)!,
            TimeSpan.FromMinutes(15));
    }
}
