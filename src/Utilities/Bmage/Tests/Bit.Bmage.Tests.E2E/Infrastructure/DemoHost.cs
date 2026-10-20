namespace Bit.Bmage.Tests.E2E.Infrastructure;

/// <summary>
/// The demo site every test drives: either an already-running deployment named by
/// <c>BMAGE_E2E_HOST_URL</c>, or Bit.Bmage.Demo.Server started once for the whole run on a free port.
/// The host only holds per-request state and every test opens a browser context of its own (its own
/// WebAssembly instance), so sharing one process never shares state between tests.
/// </summary>
public static class DemoHost
{
    private static readonly Lazy<Task<Instance>> _instance = new(StartAsync);

    /// <summary>The URL the demo answers at, without a trailing slash.</summary>
    public static async Task<string> BaseUrlAsync() => (await _instance.Value).BaseUrl;

    public static async Task StopAsync()
    {
        if (_instance.IsValueCreated is false) return;

        try
        {
            if ((await _instance.Value).Process is { } process) await process.DisposeAsync();
        }
        catch (Exception)
        {
            // A host that failed to start already failed the tests that needed it.
        }
    }

    private static async Task<Instance> StartAsync()
    {
        if (E2EEnvironment.HostUrl is { } external)
        {
            await WaitForExternalAsync(external);
            return new Instance(external, null);
        }

        // Reserved up front and passed explicitly, rather than taking the launch profile's port: other
        // checkouts and other demos on the same machine routinely hold the common ones.
        var baseUrl = $"http://127.0.0.1:{ChildProcess.FreePort()}";

        var assembly = RepoLayout.ServerAssembly(E2EEnvironment.Configuration);
        if (File.Exists(assembly) is false)
            throw new FileNotFoundException($"The demo host has not been built for {E2EEnvironment.Configuration}.", assembly);

        // A build output only finds the static web assets of its references - the client's _framework,
        // bit-bmage.js, the editor's stylesheet - through the development manifest, which Development
        // switches on.
        var process = ChildProcess.Start("dotnet", [assembly, "--urls", baseUrl], RepoLayout.ServerDirectory(),
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

    private static async Task WaitForExternalAsync(string baseUrl)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var deadline = DateTime.UtcNow.AddMinutes(1);
        Exception? last = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var response = await client.GetAsync(baseUrl + "/");
                if (response.IsSuccessStatusCode) return;
                last = new HttpRequestException($"{baseUrl}/ answered {(int)response.StatusCode}.");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                last = ex;
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"BMAGE_E2E_HOST_URL={baseUrl} did not answer within a minute.", last);
    }

    private sealed record Instance(string BaseUrl, ChildProcess? Process);
}
