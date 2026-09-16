namespace Bit.Butil.Tests.E2E.Infrastructure;

/// <summary>
/// Builds the harness host the run drives before the first test, so a run always drives the current sources.
/// Only the one the run needs: the web host for a web render mode, the WinForms host for hybrid. The
/// standalone sample is started with <c>dotnet run</c>, which builds it on its own.
/// </summary>
public static class HarnessBuild
{
    public static async Task EnsureBuiltAsync()
    {
        if (E2EEnvironment.SkipBuild) return;

        if (HarnessHostKinds.IsWebApp(E2EEnvironment.Host) && E2EEnvironment.PublishedHost is null)
        {
            await BuildAsync(RepoLayout.WebHarnessProject(), "-f", E2EEnvironment.Framework);
        }
        else if (E2EEnvironment.Host is HarnessHostKinds.Hybrid && OperatingSystem.IsWindows())
        {
            await BuildAsync(RepoLayout.HybridHarnessProject());
        }
    }

    private static Task BuildAsync(string project, params string[] extraArguments) =>
        ChildProcess.RunToCompletionAsync("dotnet",
            ["build", project, "-c", E2EEnvironment.Configuration, "-nologo", "-v:q", .. extraArguments],
            Path.GetDirectoryName(project)!,
            TimeSpan.FromMinutes(15));
}
