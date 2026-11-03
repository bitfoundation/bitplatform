using System.Text;
using Microsoft.AspNetCore.Components.Authorization;

namespace Boilerplate.Tests.Features.Tenants;

[TestClass, TestCategory("IntegrationTest")]
public partial class CrossTabTenantSwitchTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task ATenantSwitchInAnotherTab_Should_SoftRestartThisTab_AndARefreshShouldNot()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var client = server.CreateAppClient();

        var storage = client.Services.GetRequiredService<IStorageService>();
        var pubSubService = client.Services.GetRequiredService<PubSubService>();

        var userId = Guid.NewGuid();
        var firstTenantId = Guid.NewGuid();

        await storage.SetItem("access_token", BuildToken(userId, firstTenantId));
        await client.AuthManager.GetAuthenticationStateAsync();

        var softRestarts = 0;
        var unsubscribe = pubSubService.Subscribe(ClientAppMessages.SOFT_RESTART, async _ => softRestarts++);

        try
        {
            await WriteFromAnotherTab(BuildToken(userId, firstTenantId));

            Assert.AreEqual(0, softRestarts, "A plain token refresh in another tab must not restart this one.");

            await WriteFromAnotherTab(BuildToken(userId, Guid.NewGuid()));

            Assert.AreEqual(1, softRestarts, "A tenant switch in another tab must restart this one, or its page keeps showing the previous tenant's data.");
        }
        finally
        {
            unsubscribe();
        }

        async Task WriteFromAnotherTab(string accessToken)
        {
            var notified = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            void OnChanged(Task<AuthenticationState> _) => notified.TrySetResult();

            client.AuthManager.AuthenticationStateChanged += OnChanged;

            try
            {
                await storage.SetItem("access_token", accessToken);

                pubSubService.Publish(ClientAppMessages.LOCAL_STORAGE_CHANGED, "access_token");

                await notified.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.CancellationToken);
            }
            finally
            {
                client.AuthManager.AuthenticationStateChanged -= OnChanged;
            }
        }
    }

    private static string BuildToken(Guid userId, Guid tenantId)
    {
        var payload = $$"""{"nameid":"{{userId}}","{{AppClaimTypes.TENANT_ID}}":"{{tenantId}}","exp":{{DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()}}}""";

        return $"header.{Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)).TrimEnd('=').Replace('+', '-').Replace('/', '_')}.signature";
    }
}
