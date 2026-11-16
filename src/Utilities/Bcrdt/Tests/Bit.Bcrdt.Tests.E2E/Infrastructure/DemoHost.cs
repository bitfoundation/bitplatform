namespace Bit.Bcrdt.Tests.E2E.Infrastructure;

/// <summary>
/// The demo server every test drives: Bit.Bcrdt.Demo.Server published the way cd.bcrdt.demo.yml publishes it, started
/// on a free port, once for the whole run. The side-by-side pages keep every replica in the browser, on WebAssembly,
/// talking over the library's in-memory transport, and every test opens its own browser context, so sharing the
/// process shares nothing for them. The live page does go through the server, whose document host keeps a replica of
/// every live document, but each live test uses a room of its own, so no test ever meets another's document.
/// </summary>
/// <remarks>
/// A publish, not a build output: only a publish trims the WebAssembly client, and an interop payload the trimmer
/// breaks works in every build and fails only there (src/CLAUDE.md).
/// </remarks>
public sealed class DemoHost : IAsyncDisposable
{
    private readonly ChildProcess? _process;

    private DemoHost(string baseUrl, ChildProcess? process)
    {
        BaseUrl = baseUrl;
        _process = process;
    }

    /// <summary>The URL the demo answers at, without a trailing slash.</summary>
    public string BaseUrl { get; }

    /// <summary>The last lines the demo server wrote, for failure messages; empty for an external demo.</summary>
    public string RecentOutput => _process?.RecentOutput ?? string.Empty;

    public static async Task<DemoHost> StartAsync()
    {
        if (E2EEnvironment.BaseUrl is { } external)
            return new DemoHost(external, process: null);

        var directory = E2EEnvironment.DemoDirectory ?? await PublishAsync();
        var assembly = RepoLayout.DemoServerAssembly(directory);
        if (File.Exists(assembly) is false)
            throw new FileNotFoundException($"{directory} holds no published demo server. Publish it with 'dotnet publish " +
                                            $"src/Utilities/Bcrdt/Demo/Server/{RepoLayout.DemoServerName}.csproj -c Release -o <folder>'.", assembly);

        var baseUrl = $"http://127.0.0.1:{ChildProcess.FreePort()}";

        // The publish folder is the content root (wwwroot, the static assets manifest), and Production is what the site
        // runs in - whatever the machine's own ASPNETCORE_ENVIRONMENT says.
        var process = ChildProcess.Start(DotNet, [assembly, "--urls", baseUrl], directory,
            new Dictionary<string, string> { ["ASPNETCORE_ENVIRONMENT"] = "Production" });

        try
        {
            await process.WaitForHttpAsync(baseUrl + "/", TimeSpan.FromMinutes(2));
        }
        catch
        {
            await process.DisposeAsync();
            throw;
        }

        return new DemoHost(baseUrl, process);
    }

    public ValueTask DisposeAsync() => _process?.DisposeAsync() ?? ValueTask.CompletedTask;

    /// <summary>The dotnet the run was started with where the SDK says so, else the one on the PATH.</summary>
    private static string DotNet => Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host ? host : "dotnet";

    /// <summary>
    /// cd.bcrdt.demo.yml's publish (less its version stamp), into a folder next to the test binaries. Run from the
    /// demo's folder so src/global.json picks the SDK, as it does for CI's step.
    /// </summary>
    private static async Task<string> PublishAsync()
    {
        var project = RepoLayout.DemoServerProject();
        var output = RepoLayout.PublishDirectory();

        await ChildProcess.RunToCompletionAsync(DotNet,
            [
                "publish", project, "-c", "Release", "-p:AspNetCoreHostingModel=OutOfProcess", "-o", output,
                "-p:InvariantGlobalization=true", "-nologo", "-v:m",
            ],
            Path.GetDirectoryName(project)!, TimeSpan.FromMinutes(20));

        return output;
    }
}
