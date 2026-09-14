using System.Collections.Concurrent;
using System.Net.Http.Json;
using Bit.Bswup.Tests.Harness.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bit.Bswup.Tests.Hosting.Infrastructure;

/// <summary>The harness host in one render mode, in memory.</summary>
public sealed class HarnessHostFactory(string mode) : WebApplicationFactory<Bit.Bswup.Tests.Harness.Web.Components.App>
{
    private static readonly ConcurrentDictionary<string, Lazy<HarnessHostFactory>> _shared = new();

    public string Mode { get; } = mode;

    /// <summary>A factory per mode, shared by every test: the host keeps each test's state under its own session.</summary>
    public static HarnessHostFactory Shared(string mode) =>
        _shared.GetOrAdd(mode, m => new Lazy<HarnessHostFactory>(() => new HarnessHostFactory(m))).Value;

    public static async Task DisposeSharedAsync()
    {
        foreach (var factory in _shared.Values.Where(f => f.IsValueCreated))
        {
            await factory.Value.DisposeAsync();
        }

        _shared.Clear();
    }

    /// <summary>
    /// A client on a session of its own: the host keys a session by the request's host name, the way the browser
    /// suites give each test an origin of its own.
    /// </summary>
    public HttpClient CreateSessionClient()
    {
        var client = CreateClient(new() { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Host = "t" + Guid.NewGuid().ToString("N")[..12] + ".localhost";
        return client;
    }

    public static async Task SetOptionsAsync(HttpClient client, object options)
    {
        using var response = await client.PutAsJsonAsync("/_harness/options", options);
        response.EnsureSuccessStatusCode();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(HarnessRenderModes.ConfigurationKey, Mode);
    }
}
