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
        if (E2EEnvironment.BaseUrl is { } external)
            return new DemoHost(external, process: null);

        var baseUrl = $"http://127.0.0.1:{ChildProcess.FreePort()}";

        ChildProcess process;
        if (E2EEnvironment.PublishedHost is { } published)
        {
            var assembly = Path.Combine(published, $"{RepoLayout.DemoServerName}.dll");
            if (File.Exists(assembly) is false)
                throw new FileNotFoundException(
                    $"{published} holds no published demo server. Publish it first with " +
                    $"'bash src/Utilities/Bdnd/Tests/Bit.Bdnd.Tests.E2E/publish-gate.sh {published}'", assembly);

            // A publish output runs as it does in production: its static web assets come from its own wwwroot.
            process = ChildProcess.Start("dotnet", [assembly, "--urls", baseUrl], published,
                new Dictionary<string, string> { ["ASPNETCORE_ENVIRONMENT"] = "Production" });
        }
        else
        {
            var assembly = RepoLayout.DemoServerAssembly(E2EEnvironment.Configuration);
            if (File.Exists(assembly) is false)
                throw new FileNotFoundException(
                    $"The demo server has not been built in the {E2EEnvironment.Configuration} configuration. Build it first with " +
                    $"'dotnet build src/Utilities/Bdnd/Demo/Server/Bit.Bdnd.Demo.Server.csproj -c {E2EEnvironment.Configuration}'", assembly);

            // A build output only finds the static web assets of its references (the WebAssembly client,
            // the demo's stylesheet and bit-bdnd.js among them) through the development manifest, which Development
            // switches on.
            process = ChildProcess.Start("dotnet", [assembly, "--urls", baseUrl], Path.GetDirectoryName(RepoLayout.DemoServerProject())!,
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

        return new DemoHost(baseUrl, process);
    }

    public ValueTask DisposeAsync() => _process?.DisposeAsync() ?? ValueTask.CompletedTask;
}
