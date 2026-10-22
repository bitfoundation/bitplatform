using StackExchange.Redis;
using Microsoft.AspNetCore.SignalR;
using Boilerplate.Server.Api.Infrastructure.SignalR;

namespace Boilerplate.Tests.Features.SignalR;

[TestClass, TestCategory("IntegrationTest")]
public partial class SignalRRedisBackplaneTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task DisposingSignalRsBackplane_Should_KeepTheCacheConnectionOpen()
    {
        await using var server = new AppTestServer();

        await server.Build().Start(TestContext.CancellationToken);

        var services = server.ApiApp.Services;

        await services.GetRequiredService<IHubContext<AppHub>>().Clients.All.SendAsync("PING", TestContext.CancellationToken);

        ((IDisposable)services.GetRequiredService<HubLifetimeManager<AppHub>>()).Dispose();

        Assert.IsTrue(services.GetRequiredKeyedService<IConnectionMultiplexer>("redis-cache").IsConnected,
            "SignalR's Redis backplane closed the redis-cache connection that FusionCache still uses.");
    }
}
