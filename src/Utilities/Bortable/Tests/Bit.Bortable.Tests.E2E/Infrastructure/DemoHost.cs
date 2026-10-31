namespace Bit.Bortable.Tests.E2E.Infrastructure;

/// <summary>
/// An app the tests drive, started on a free port once for the whole run: the demo server every suite but one
/// drives, from its build output, or the lazy loading sample, from its published output. Neither keeps state on
/// the server (each page runs in the browser on WebAssembly, its lists included), and every test opens its own
/// browser context, so sharing the process shares nothing.
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
                $"'dotnet build src/Utilities/Bortable/Demo/Server/Bit.Bortable.Demo.Server.csproj -c {E2EEnvironment.Configuration}'", assembly);

        // A build output only finds the static web assets of its references (the WebAssembly client,
        // bit-bortable.js and bit-bortable.css among them) through the development manifest, which Development
        // switches on.
        return await BootAsync(assembly, Path.GetDirectoryName(RepoLayout.DemoServerProject())!, "Development");
    }

    /// <summary>
    /// The lazy loading sample, from what <c>dotnet publish</c> left: the trimmed client that downloads
    /// <c>Bit.Bortable.wasm</c> on demand, as an app ships it, run from its own folder in Production.
    /// </summary>
    public static async Task<DemoHost> StartLazySampleAsync()
    {
        if (E2EEnvironment.LazySampleBaseUrl is { } external)
            return new DemoHost(external, process: null);

        var directory = RepoLayout.LazySamplePublishDirectory(E2EEnvironment.Configuration);
        var assembly = Path.Combine(directory, $"{RepoLayout.LazySampleName}.dll");
        if (File.Exists(assembly) is false)
            throw new FileNotFoundException(
                $"The lazy loading sample has not been published in the {E2EEnvironment.Configuration} configuration. Publish it first with " +
                $"'dotnet publish src/Utilities/Bortable/Samples/Lazy/{RepoLayout.LazySampleName}/{RepoLayout.LazySampleName}.csproj -c {E2EEnvironment.Configuration}'", assembly);

        return await BootAsync(assembly, directory, "Production");
    }

    private static async Task<DemoHost> BootAsync(string assembly, string workingDirectory, string environment)
    {
        var baseUrl = $"http://127.0.0.1:{ChildProcess.FreePort()}";

        var process = ChildProcess.Start("dotnet", [assembly, "--urls", baseUrl], workingDirectory,
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
