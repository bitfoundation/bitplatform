using System.Buffers.Text;
using System.Security.Cryptography;
using Boilerplate.Shared.Features.PushNotification;

namespace Boilerplate.Tests.Features.PushNotification;

[TestClass, TestCategory("IntegrationTest")]
public partial class PushEndpointAllowListTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task Subscribe_Should_RejectAnEndpointOutsideTheBrowserPushServices()
    {
        await using var server = new AppTestServer();

        await server.Build().Start(TestContext.CancellationToken);

        await using var client = server.CreateAppClient();
        var pushNotificationController = client.GetController<IPushNotificationController>();

        string[] endpoints =
        [
            "http://fcm.googleapis.com/fcm/send/plain-http",
            "https://push.example/endpoint",
            "https://169.254.169.254/latest/meta-data",
            "https://localhost/internal",
            "https://fcm.googleapis.com.attacker.example/fcm/send/lookalike",
            "https://fcm.googleapis.com:8443/fcm/send/other-port",
            "fcm.googleapis.com/fcm/send/relative"
        ];

        var deviceIds = new List<string>();

        try
        {
            foreach (var endpoint in endpoints)
            {
                var subscription = NewBrowserSubscription(endpoint);
                deviceIds.Add(subscription.DeviceId!);

                await Assert.ThrowsExactlyAsync<BadRequestException>(() => pushNotificationController.Subscribe(subscription, TestContext.CancellationToken),
                    $"'{endpoint}' is not an https address of a browser push service, so the server must never be made to send to it.");
            }

            Assert.AreEqual(0, await CountSubscriptions(server, deviceIds), "A refused subscription must not have been stored.");
        }
        finally
        {
            await DeleteSubscriptions(server, deviceIds);
        }
    }

    [TestMethod]
    public async Task Subscribe_Should_AcceptTheEndpointsOfTheBrowserPushServices()
    {
        await using var server = new AppTestServer();

        await server.Build().Start(TestContext.CancellationToken);

        await using var client = server.CreateAppClient();
        var pushNotificationController = client.GetController<IPushNotificationController>();

        var marker = Guid.NewGuid().ToString("N");

        string[] endpoints =
        [
            $"https://fcm.googleapis.com/fcm/send/{marker}",
            $"https://updates.push.services.mozilla.com/wpush/v2/{marker}",
            $"https://wns2-par02p.notify.windows.com/w/?token={marker}",
            $"https://web.push.apple.com/{marker}"
        ];

        var deviceIds = new List<string>();

        try
        {
            foreach (var endpoint in endpoints)
            {
                var subscription = NewBrowserSubscription(endpoint);
                deviceIds.Add(subscription.DeviceId!);

                await pushNotificationController.Subscribe(subscription, TestContext.CancellationToken);
            }

            Assert.AreEqual(endpoints.Length, await CountSubscriptions(server, deviceIds), "Every browser's own push service must still be accepted.");
        }
        finally
        {
            await DeleteSubscriptions(server, deviceIds);
        }
    }

    [TestMethod]
    public async Task TheWebPushHttpClient_Should_FollowNoRedirect_AndResolveNoServiceName()
    {
        await using var server = new AppTestServer();

        await server.Build().Start(TestContext.CancellationToken);

        var handlerFactory = server.ApiApp.Services.GetRequiredService<IHttpMessageHandlerFactory>();

        Assert.Contains(IsServiceDiscovery, HandlerChain(handlerFactory.CreateHandler("APNS")),
            "Control: every other client resolves service names, otherwise the assertion below proves nothing.");

        var webPushChain = HandlerChain(handlerFactory.CreateHandler("Vapid"));

        Assert.DoesNotContain(IsServiceDiscovery, webPushChain,
            "The web push client sends to an address a browser handed in, so it must never resolve it as a service name.");

        var primaryHandler = webPushChain[^1] as SocketsHttpHandler;

        Assert.IsNotNull(primaryHandler, $"The web push client ends in {webPushChain[^1].GetType().Name} rather than the shared SocketsHttpHandler.");
        Assert.IsFalse(primaryHandler.AllowAutoRedirect, "A push service never redirects, and a followed redirect would arrive somewhere the endpoint check never saw.");
    }

    [TestMethod]
    public async Task ThePushEndpoints_Should_StopServingAfterTheBurstLimit()
    {
        await using var server = new AppTestServer();

        await server.Build().Start(TestContext.CancellationToken);

        await using var client = server.CreateAppClient();
        var pushNotificationController = client.GetController<IPushNotificationController>();

        var served = 0;

        for (var i = 0; i < 40; i++)
        {
            try
            {
                await pushNotificationController.Unsubscribe(new() { DeviceId = $"push-burst-{Guid.NewGuid():N}", Platform = "fcmV1" }, TestContext.CancellationToken);
                served++;
            }
            catch (TooManyRequestsException)
            {
                Assert.IsGreaterThan(0, served, "The endpoint must serve some requests before throttling starts.");
                return;
            }
        }

        Assert.Fail($"A burst of 40 anonymous push requests was never throttled ({served} served).");
    }

    private static PushNotificationSubscriptionDto NewBrowserSubscription(string endpoint)
    {
        using var keyPair = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var publicKey = keyPair.PublicKey.ExportParameters().Q;

        return new()
        {
            DeviceId = $"push-endpoint-{Guid.NewGuid():N}",
            Platform = "browser",
            Endpoint = endpoint,
            P256dh = Base64Url.EncodeToString([0x04, .. publicKey.X!, .. publicKey.Y!]),
            Auth = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(16))
        };
    }

    private static List<HttpMessageHandler> HandlerChain(HttpMessageHandler handler)
    {
        List<HttpMessageHandler> chain = [];

        for (HttpMessageHandler? current = handler; current is not null; current = (current as DelegatingHandler)?.InnerHandler)
        {
            chain.Add(current);
        }

        return chain;
    }

    private static bool IsServiceDiscovery(HttpMessageHandler handler)
        => handler.GetType().Namespace?.StartsWith("Microsoft.Extensions.ServiceDiscovery", StringComparison.Ordinal) is true;

    private async Task<int> CountSubscriptions(AppTestServer server, List<string> deviceIds)
    {
        await using var scope = server.ApiApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.PushNotificationSubscriptions.IgnoreQueryFilters().CountAsync(s => deviceIds.Contains(s.DeviceId!), TestContext.CancellationToken);
    }

    private async Task DeleteSubscriptions(AppTestServer server, List<string> deviceIds)
    {
        await using var scope = server.ApiApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.PushNotificationSubscriptions.IgnoreQueryFilters().Where(s => deviceIds.Contains(s.DeviceId!)).ExecuteDeleteAsync(CancellationToken.None);
    }
}
