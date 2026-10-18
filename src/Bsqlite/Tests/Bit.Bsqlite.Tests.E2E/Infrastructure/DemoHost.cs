namespace Bit.Bsqlite.Tests.E2E.Infrastructure;

/// <summary>
/// The demo server the demo tests drive: Bit.Bsqlite.Demo.Server started from its build output on a free
/// port, once for the whole run. Every database the demo opens lives in the browser, and every test opens
/// its own browser context, so sharing the process shares nothing but the server-side copy of the catalog.
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
        if (E2EEnvironment.DemoUrl is { } external)
            return new DemoHost(external, process: null);

        var assembly = RepoLayout.DemoServerAssembly(E2EEnvironment.Configuration);
        if (File.Exists(assembly) is false)
            throw new FileNotFoundException(
                $"The demo server has not been built in the {E2EEnvironment.Configuration} configuration. Build it first with " +
                $"'dotnet build src/Bsqlite/Bit.Bsqlite.Demo/Server/Bit.Bsqlite.Demo.Server.csproj -c {E2EEnvironment.Configuration}'", assembly);

        var baseUrl = $"http://127.0.0.1:{ChildProcess.FreePort()}";

        // A build output only finds the static web assets of its references (the WebAssembly client, the
        // worker, the engine and the VFSes among them) through the development manifest, which Development
        // switches on.
        var process = ChildProcess.Start("dotnet", [assembly, "--urls", baseUrl], Path.GetDirectoryName(RepoLayout.DemoServerProject())!,
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

        return new DemoHost(baseUrl, process);
    }

    public ValueTask DisposeAsync() => _process?.DisposeAsync() ?? ValueTask.CompletedTask;
}
