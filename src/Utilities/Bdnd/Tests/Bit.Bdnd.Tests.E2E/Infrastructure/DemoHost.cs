namespace Bit.Bdnd.Tests.E2E.Infrastructure;

/// <summary>
/// The demo server every test drives: Bit.Bdnd.Demo.Server started from its build output - or from a publish
/// output, see <see cref="E2EEnvironment.PublishedHost"/> - on a free port, once for the whole run. The demo keeps
/// no state on the server (each page runs in the browser on WebAssembly), and every test opens its own browser
/// context, so sharing the process shares nothing.
/// </summary>
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

    public static async Task<DemoHost> StartAsync()
    {
        if (E2EEnvironment.PublishedHost is { } publishOutput)
        {
            // Each names a demo to drive. Taking one of them would leave the build the other names untested, and a
            // published one is set precisely to test what the trimmer did.
            if (E2EEnvironment.BaseUrl is { } running)
                throw new InvalidOperationException(
                    $"BDND_E2E_BASE_URL ({running}) and BDND_E2E_PUBLISHED_HOST ({publishOutput}) are both set. Unset one of them.");

            if (Path.IsPathFullyQualified(publishOutput) is false)
                throw new InvalidOperationException(
                    $"BDND_E2E_PUBLISHED_HOST ({publishOutput}) is a relative path. Set it to the full path of the publish output: " +
                    "dotnet test runs the suite in its output folder, not in the folder it was started from.");
        }

        if (E2EEnvironment.BaseUrl is { } external)
            return new DemoHost(external, process: null);

        // A publish output runs as it does in production: its static web assets come from its own wwwroot. A build output
        // only finds the static web assets of its references (the WebAssembly client, the demo's stylesheet and
        // bit-bdnd.js among them) through the development manifest, which Development switches on.
        var (assembly, directory, environment, missing) = E2EEnvironment.PublishedHost is { } published
            ? (Path.Combine(published, $"{RepoLayout.DemoServerName}.dll"), published, "Production",
                $"{published} holds no published demo server. Publish it first, from src/Utilities/Bdnd (inside src, so " +
                $"src/global.json picks the SDK), with 'bash Tests/Bit.Bdnd.Tests.E2E/publish-gate.sh {published}'")
            : (RepoLayout.DemoServerAssembly(E2EEnvironment.Configuration), Path.GetDirectoryName(RepoLayout.DemoServerProject())!, "Development",
                $"The demo server has not been built in the {E2EEnvironment.Configuration} configuration. Build it first with " +
                $"'dotnet build src/Utilities/Bdnd/Demo/Server/Bit.Bdnd.Demo.Server.csproj -c {E2EEnvironment.Configuration}'");

        if (File.Exists(assembly) is false)
            throw new FileNotFoundException(missing, assembly);

        var baseUrl = $"http://127.0.0.1:{ChildProcess.FreePort()}";
        var process = ChildProcess.Start("dotnet", [assembly, "--urls", baseUrl], directory,
            new Dictionary<string, string> { ["ASPNETCORE_ENVIRONMENT"] = environment });

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
}
