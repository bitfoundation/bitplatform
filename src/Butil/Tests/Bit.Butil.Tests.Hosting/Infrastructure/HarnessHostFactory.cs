using System.Collections.Concurrent;
using ButilTests.Harness.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace ButilTests.Hosting.Infrastructure;

/// <summary>The harness web host in one render mode and script-loading mode, in memory, with every log entry captured.</summary>
public sealed class HarnessHostFactory(string mode, string scripts = HarnessScripts.Bundle) : WebApplicationFactory<ButilTests.Harness.Web.Components.App>
{
    private static readonly ConcurrentDictionary<(string Mode, string Scripts), Lazy<HarnessHostFactory>> _shared = new();

    public string Mode { get; } = mode;

    public string Scripts { get; } = scripts;

    public CapturingLoggerProvider Logs { get; } = new();

    /// <summary>
    /// A factory per mode shared by every test that only reads responses. Tests that assert on the logs create
    /// their own, so entries from concurrent tests cannot mix.
    /// </summary>
    public static HarnessHostFactory Shared(string mode, string scripts = HarnessScripts.Bundle) =>
        _shared.GetOrAdd((mode, scripts), key => new Lazy<HarnessHostFactory>(() => new HarnessHostFactory(key.Mode, key.Scripts))).Value;

    public static async Task DisposeSharedAsync()
    {
        foreach (var factory in _shared.Values.Where(f => f.IsValueCreated))
        {
            await factory.Value.DisposeAsync();
        }

        _shared.Clear();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(HarnessRenderModes.ConfigurationKey, Mode);
        builder.UseSetting(HarnessScripts.ConfigurationKey, Scripts);
        builder.ConfigureLogging(logging =>
        {
            logging.AddProvider(Logs);
            logging.SetMinimumLevel(LogLevel.Debug);
        });
    }
}
