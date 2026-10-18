namespace Bit.BlazeDb.Tests.E2E.Infrastructure;

/// <summary>
/// The demo server every test drives: Bit.BlazeDb.Demo.Server started from its build output on a free
/// port, once for the whole run. The demo keeps no state on the server - every database lives in a
/// browser tab - and every test opens its own browser context, with an Origin Private File System, an
/// IndexedDB and Web Locks of its own, so sharing the process shares nothing.
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

        var assembly = RepoLayout.DemoServerAssembly(E2EEnvironment.Configuration);
        if (File.Exists(assembly) is false)
            throw new FileNotFoundException(
                $"The demo server has not been built in the {E2EEnvironment.Configuration} configuration. Build it first with " +
                $"'dotnet build src/BlazeDb/Demo/Server/Bit.BlazeDb.Demo.Server.csproj -c {E2EEnvironment.Configuration}'", assembly);

        // localhost rather than 127.0.0.1: OPFS, Web Locks and WebCrypto exist only in a secure context, and a
        // browser treats http://localhost as one but not every loopback address spelled as an IP.
        var baseUrl = $"http://localhost:{ChildProcess.FreePort()}";

        // A build output only finds the static web assets of its references (the WebAssembly client and
        // the bit-blazedb-*.js modules among them) through the development manifest, which Development
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
