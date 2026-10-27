namespace Bit.BbScanner.Tests.E2E.Infrastructure;

/// <summary>
/// The Blazor Server sample (<c>Bit.BbScanner.Samples.Server</c>) started from its build output on a free port, once,
/// the first time a test asks for it: the demo runs every page on WebAssembly, and this is where the scanner runs on a
/// circuit instead - the camera in the browser, the frame loop and the decoder on the server.
/// </summary>
public static class ServerSampleHost
{
    private static readonly Lazy<Task<Host>> _host = new(StartAsync);

    /// <summary>The running sample, or the reason it could not be started.</summary>
    public static Task<Host> GetAsync() => _host.Value;

    public static async Task StopAsync()
    {
        if (_host.IsValueCreated is false) return;

        var host = await _host.Value;
        if (host.Process is not null) await host.Process.DisposeAsync();
    }

    private static async Task<Host> StartAsync()
    {
        if (E2EEnvironment.ServerSampleBaseUrl is { } external)
            return new Host(external, null, null);

        var assembly = RepoLayout.ServerSampleAssembly(E2EEnvironment.Configuration);
        if (File.Exists(assembly) is false)
            return new Host(null, null,
                $"the Blazor Server sample has not been built in the {E2EEnvironment.Configuration} configuration. Build it first with " +
                $"'dotnet build src/Utilities/BbScanner/Samples/Server/Bit.BbScanner.Samples.Server.csproj -c {E2EEnvironment.Configuration}'");

        var baseUrl = $"http://127.0.0.1:{ChildProcess.FreePort()}";

        // Development, for the same reason as the demo: a build output finds its references' static web assets -
        // bit-bbscanner.js among them - through the development manifest.
        var process = ChildProcess.Start("dotnet", [assembly, "--urls", baseUrl], Path.GetDirectoryName(RepoLayout.ServerSampleProject())!,
            new Dictionary<string, string> { ["ASPNETCORE_ENVIRONMENT"] = "Development" });

        try
        {
            await process.WaitForHttpAsync(baseUrl + "/", TimeSpan.FromMinutes(2));
        }
        catch (Exception ex) when (ex is InvalidOperationException or TimeoutException)
        {
            await process.DisposeAsync();
            return new Host(null, null, "the Blazor Server sample could not be started: " + ex.Message);
        }

        return new Host(baseUrl, process, null);
    }

    /// <summary>Where the sample answers, without a trailing slash, or why it does not.</summary>
    public sealed record Host(string? BaseUrl, ChildProcess? Process, string? Unavailable);
}
