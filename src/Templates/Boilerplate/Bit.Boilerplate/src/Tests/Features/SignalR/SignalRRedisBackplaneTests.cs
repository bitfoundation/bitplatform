using StackExchange.Redis;
using Microsoft.AspNetCore.SignalR;
using Boilerplate.Server.Api.Infrastructure.SignalR;

namespace Boilerplate.Tests.Features.SignalR;

[TestClass, TestCategory("IntegrationTest")]
public partial class SignalRRedisBackplaneTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task TheServer_Should_ShutDown_AfterTheCacheConnectionCloses()
    {
        await using var server = new AppTestServer();

        await server.Build().Start(TestContext.CancellationToken);

        await server.ApiApp.Services.GetRequiredService<IHubContext<AppHub>>().Clients.All.SendAsync("PING", TestContext.CancellationToken);

        await server.ApiApp.Services.GetRequiredKeyedService<IConnectionMultiplexer>("redis-cache").DisposeAsync();
    }
}
