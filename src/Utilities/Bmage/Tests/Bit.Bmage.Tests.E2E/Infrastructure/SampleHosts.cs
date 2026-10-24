namespace Bit.Bmage.Tests.E2E.Infrastructure;

/// <summary>
/// The hosting samples (src/Utilities/Bmage/Samples): one set of pages hosted by three Blazor Web Apps, under
/// Interactive Server, Interactive WebAssembly and Interactive Auto. Each is built (unless
/// <c>BMAGE_E2E_SKIP_BUILD=1</c>) and started on a free port the first time a test asks for it, then shared by the
/// rest of the run - so a run that never reaches <see cref="SamplesBrowserTests"/> never builds them.
/// </summary>
public static class SampleHosts
{
    public const string Server = "Server";
    public const string Wasm = "Wasm";
    public const string Auto = "Auto";

    private static readonly Dictionary<string, Lazy<Task<Instance>>> _instances = new()
    {
        [Server] = new(() => StartAsync(Server)),
        [Wasm] = new(() => StartAsync(Wasm)),
        [Auto] = new(() => StartAsync(Auto)),
    };

    /// <summary>The URL the sample answers at, without a trailing slash.</summary>
    public static async Task<string> BaseUrlAsync(string sample) => (await _instances[sample].Value).BaseUrl;

    public static async Task StopAsync()
    {
        foreach (var instance in _instances.Values.Where(i => i.IsValueCreated))
        {
            try
            {
                await (await instance.Value).Process.DisposeAsync();
            }
            catch (Exception)
            {
                // A host that failed to start already failed the tests that needed it.
            }
        }
    }

    private static async Task<Instance> StartAsync(string sample)
    {
        var project = RepoLayout.SampleProject(sample);
        if (E2EEnvironment.SkipBuild is false)
        {
            await ChildProcess.RunToCompletionAsync("dotnet",
                ["build", project, "-c", E2EEnvironment.Configuration, "-nologo", "-v:q"],
                Path.GetDirectoryName(project)!,
                TimeSpan.FromMinutes(15));
        }

        var assembly = RepoLayout.SampleAssembly(sample, E2EEnvironment.Configuration);
        if (File.Exists(assembly) is false)
            throw new FileNotFoundException($"The {sample} sample has not been built for {E2EEnvironment.Configuration}.", assembly);

        var baseUrl = $"http://127.0.0.1:{ChildProcess.FreePort()}";

        // Development, for the same reason as the demo host: a build output finds its references' static web
        // assets (bit-bmage.js, the editor's stylesheet, the client's _framework) through the development manifest.
        var process = ChildProcess.Start("dotnet", [assembly, "--urls", baseUrl], Path.GetDirectoryName(project)!,
            new Dictionary<string, string> { ["ASPNETCORE_ENVIRONMENT"] = "Development" });

        try
        {
            await process.WaitForHttpAsync(baseUrl + "/", TimeSpan.FromMinutes(2));
        }
        catch
        {
            await process.DisposeAsync();
            throw;
        }

        return new Instance(baseUrl, process);
    }

    private sealed record Instance(string BaseUrl, ChildProcess Process);
}
