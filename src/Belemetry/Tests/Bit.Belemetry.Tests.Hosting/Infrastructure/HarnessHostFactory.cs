using System.Collections.Concurrent;
using Bit.Belemetry.Tests.Harness.Web;
using Bit.Belemetry.Tests.Harness.Web.Collector;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bit.Belemetry.Tests.Hosting.Infrastructure;

/// <summary>
/// The harness web host in one render mode, in memory. Its exporters post to <c>http://localhost/test-collector</c>,
/// which the in-process <see cref="TestServer"/> answers: Belemetry's export traffic is routed into it through a
/// <see cref="BelemetryHttpMessageHandlerProvider"/>, so the server pipeline's exports land in the host's own
/// capturing receiver exactly as they would over the network.
/// </summary>
public sealed class HarnessHostFactory(string mode, bool serverPipeline = false) : WebApplicationFactory<Bit.Belemetry.Tests.Harness.Web.Components.App>
{
    private static readonly ConcurrentDictionary<string, Lazy<HarnessHostFactory>> _shared = new();

    private readonly TaskCompletionSource<TestServer> _server = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public string Mode { get; } = mode;

    public bool ServerPipeline { get; } = serverPipeline;

    /// <summary>The capturing receiver of this host.</summary>
    public HarnessCollector Collector => Services.GetRequiredService<HarnessCollector>();

    /// <summary>A factory per mode (server pipeline off) shared by every test that only reads responses.</summary>
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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(HarnessRenderModes.ConfigurationKey, Mode);
        builder.UseSetting(HarnessConfiguration.ServerPipelineKey, ServerPipeline ? "true" : "false");
        builder.ConfigureTestServices(services =>
            services.AddSingleton(new BelemetryHttpMessageHandlerProvider(new TestServerHandler(_server.Task))));
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        // Not this.Server: that property builds the host when it has not been built yet, and the pipeline's first
        // export can race the factory's own initialization.
        _server.TrySetResult((TestServer)host.Services.GetRequiredService<IServer>());
        return host;
    }

    /// <summary>Sends through the in-memory server once it exists; the pipeline starts before the host is built.</summary>
    private sealed class TestServerHandler(Task<TestServer> server) : HttpMessageHandler
    {
        private HttpMessageInvoker? _invoker;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _invoker ??= new HttpMessageInvoker((await server.WaitAsync(cancellationToken)).CreateHandler(), disposeHandler: true);
            return await _invoker.SendAsync(request, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _invoker?.Dispose();
            base.Dispose(disposing);
        }
    }
}
