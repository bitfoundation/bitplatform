namespace Bit.Bmotion.Tests.E2E.Infrastructure;

/// <summary>One running Bit.Bmotion.Tests.Harness.Web process: a default render mode and a set of library options.</summary>
public sealed class WebHarnessHost : IAsyncDisposable
{
    private readonly ChildProcess _process;

    private WebHarnessHost(string baseUrl, ChildProcess process)
    {
        BaseUrl = baseUrl;
        _process = process;
    }

    /// <summary>The URL the host answers at, without a trailing slash.</summary>
    public string BaseUrl { get; }

    /// <summary>The host's recent console output - quoted when a test needs to know what the server logged.</summary>
    public string RecentOutput => _process.RecentOutput;

    public static async Task<WebHarnessHost> StartAsync(string mode, HarnessProfile profile)
    {
        var baseUrl = $"http://127.0.0.1:{ChildProcess.FreePort()}";
        string[] hostArguments = ["--urls", baseUrl, $"--BmotionHarness:Mode={mode}", .. profile.ToArguments()];

        ChildProcess process;
        if (E2EEnvironment.PublishedHost is { } published)
        {
            // A publish output runs as it would in production: static web assets come from its wwwroot.
            process = ChildProcess.Start("dotnet", [Path.Combine(published, $"{RepoLayout.WebHarnessName}.dll"), .. hostArguments], published,
                new Dictionary<string, string> { ["ASPNETCORE_ENVIRONMENT"] = "Production" });
        }
        else
        {
            // A build output only finds the static web assets of its references (bit-bmotion.js among them)
            // through the development manifest, which Development switches on.
            var assembly = RepoLayout.WebHarnessAssembly(E2EEnvironment.Framework, E2EEnvironment.Configuration);
            if (File.Exists(assembly) is false)
                throw new FileNotFoundException($"The web harness host has not been built for {E2EEnvironment.Framework}/{E2EEnvironment.Configuration}.", assembly);

            process = ChildProcess.Start("dotnet", [assembly, .. hostArguments], Path.GetDirectoryName(RepoLayout.WebHarnessProject())!,
                new Dictionary<string, string> { ["ASPNETCORE_ENVIRONMENT"] = "Development" });
        }

        try
        {
            await process.WaitForHttpAsync(baseUrl + "/", TimeSpan.FromMinutes(2));
        }
        catch
        {
            await process.DisposeAsync();
            throw;
        }

        return new WebHarnessHost(baseUrl, process);
    }

    public ValueTask DisposeAsync() => _process.DisposeAsync();
}
