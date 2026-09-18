using System.Collections.Concurrent;
using Bit.Brouter.Tests.Harness.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace Bit.Brouter.Tests.Hosting.Infrastructure;

/// <summary>The harness web host in one render mode, in memory, with every log entry captured.</summary>
public sealed class HarnessHostFactory(string mode) : WebApplicationFactory<Bit.Brouter.Tests.Harness.Web.Components.App>
{
    private static readonly ConcurrentDictionary<string, Lazy<HarnessHostFactory>> _shared = new();

    public string Mode { get; } = mode;

    public CapturingLoggerProvider Logs { get; } = new();

    /// <summary>
    /// A factory per mode shared by every test that only reads responses. Tests that assert on the logs
    /// create their own, so entries from concurrent tests cannot mix.
    /// </summary>
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

    /// <summary>A client that reports redirects instead of following them.</summary>
    public HttpClient CreateNonRedirectingClient() => CreateClient(new() { AllowAutoRedirect = false });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(HarnessRenderModes.ConfigurationKey, Mode);
        builder.ConfigureLogging(logging =>
        {
            logging.AddProvider(Logs);
            logging.SetMinimumLevel(LogLevel.Debug);
        });
    }
}
