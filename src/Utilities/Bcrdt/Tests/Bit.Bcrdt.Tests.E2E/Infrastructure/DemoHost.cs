namespace Bit.Bcrdt.Tests.E2E.Infrastructure;

/// <summary>
/// The demo server every test drives: Bit.Bcrdt.Demo.Server started from its build output on a free port,
/// once for the whole run. The side-by-side pages keep every replica in the browser, on WebAssembly, talking
/// over the library's in-memory transport, and every test opens its own browser context, so sharing the process
/// shares nothing for them. The live page does go through the server, whose document host keeps a replica of every
/// live document, but each live test uses a room of its own, so no test ever meets another's document.
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

    /// <summary>The last lines the demo server wrote, for failure messages; empty for an external demo.</summary>
    public string RecentOutput => _process?.RecentOutput ?? string.Empty;

    public static async Task<DemoHost> StartAsync()
    {
        if (E2EEnvironment.BaseUrl is { } external)
            return new DemoHost(external, process: null);

        var assembly = RepoLayout.DemoServerAssembly(E2EEnvironment.Configuration);
        if (File.Exists(assembly) is false)
            throw new FileNotFoundException(
                $"The demo server has not been built in the {E2EEnvironment.Configuration} configuration. Build it first with " +
                $"'dotnet build src/Utilities/Bcrdt/Demo/Server/Bit.Bcrdt.Demo.Server.csproj -c {E2EEnvironment.Configuration}'", assembly);

        var baseUrl = $"http://127.0.0.1:{ChildProcess.FreePort()}";

        // A build output only finds the static web assets of its references (the WebAssembly client and
        // bit-bcrdt.js among them) through the development manifest, which Development switches on.
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
