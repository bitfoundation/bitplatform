using Bunit;
using Bit.Bcrdt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Components.Authorization;

namespace Boilerplate.WorkbenchTests.Infrastructure;

/// <summary>
/// bUnit contexts with the Workbench's own services, as a circuit opened on this machine gets them, or with
/// <c>isLocal</c> false, one opened through a tunnel. The stores save nowhere, and the review document stays in memory
/// unless a <c>server</c> is given, whose review it then joins through the relay.
/// </summary>
public static class WorkbenchBunit
{
    public static BunitContext Create(bool isLocal = true, WorkbenchTestServer? server = null)
    {
        var ctx = new BunitContext();

        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        // Bmotion stops when the browser doesn't find its element; here every rendered element is there.
        var modules = ctx.JSInterop.SetupModule();
        modules.Mode = JSRuntimeMode.Loose;
        modules.Setup<bool>("registerElement", _ => true).SetResult(true);

        // Replaced below by the Workbench's own.
        ctx.Services.RemoveAll<HttpClient>();
        ctx.Services.RemoveAll<IAuthorizationService>();
        ctx.Services.RemoveAll<AuthenticationStateProvider>();
        ctx.Services.RemoveAll<IAuthorizationPolicyProvider>();

        var bunitServices = ctx.Services.Select(s => s.ServiceType).ToHashSet();

        foreach (var descriptor in WorkbenchTestServer.CreateBuilder().Services)
        {
            if (bunitServices.Contains(descriptor.ServiceType))
                continue;

            ctx.Services.Add(descriptor);
        }

        if (server is null)
        {
            ctx.Services.RemoveAll<IBcrdtTransportFactory>();
        }
        else
        {
            // The relay connects to the address the running server listens on.
            ctx.Services.RemoveAll<IServer>();
            ctx.Services.AddSingleton(server.Services.GetRequiredService<IServer>());
        }

        ctx.Services.RemoveAll<IHttpContextAccessor>();
        ctx.Services.AddSingleton<IHttpContextAccessor>(new FixedHttpContextAccessor(WorkbenchTestReviewer.CreateHttpContext(server?.Address ?? new("http://localhost/"), isLocal)));
        ctx.Services.RemoveAll<IBlexStorage>();
        ctx.Services.AddScoped<IBlexStorage, InMemoryBlexStorage>();

        ctx.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        return ctx;
    }

    private sealed class InMemoryBlexStorage : IBlexStorage
    {
        private readonly ConcurrentDictionary<string, string> values = [];

        public ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default) => ValueTask.FromResult(values.GetValueOrDefault(key));

        public ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            values[key] = value;
            return ValueTask.CompletedTask;
        }

        public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            values.TryRemove(key, out _);
            return ValueTask.CompletedTask;
        }
    }
}
