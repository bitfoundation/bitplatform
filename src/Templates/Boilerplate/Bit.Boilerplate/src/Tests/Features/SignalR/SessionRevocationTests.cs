using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
using Boilerplate.Shared.Infrastructure.Services;

namespace Boilerplate.Tests.Features.SignalR;

[TestClass, TestCategory("IntegrationTest")]
public class SessionRevocationTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task ARevokedSession_Should_NotConnectToTheHub()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);
        await using var client = server.CreateAppClient();

        await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);
        var accessToken = await GetAccessToken(client);

        await using (var liveConnection = CreateHubConnection(server, accessToken))
        {
            await liveConnection.StartAsync(TestContext.CancellationToken);
            await liveConnection.InvokeAsync(SharedAppMessages.ChangeAuthenticationState, accessToken, TestContext.CancellationToken);
        }

        await client.AuthManager.SignOut(TestContext.CancellationToken);

        await using var revokedConnection = CreateHubConnection(server, accessToken);

        var closedWith = await StartAndWaitForClose(revokedConnection);

        Assert.IsNotNull(closedWith, "A signed out session's access token is still a valid jwt for minutes, so the hub has to refuse it itself.");
        Assert.EndsWith(nameof(AppStrings.UnauthorizedException), closedWith.Message, $"Closed with: {closedWith}");
    }

    [TestMethod]
    public async Task SwitchingAHubConnectionToARevokedSession_Should_CloseIt()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);
        await using var client = server.CreateAppClient();

        await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);
        var accessToken = await GetAccessToken(client);

        await client.AuthManager.SignOut(TestContext.CancellationToken);

        await using var hubConnection = CreateHubConnection(server, accessToken: null);

        var closed = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        hubConnection.Closed += exception =>
        {
            closed.TrySetResult(exception);
            return Task.CompletedTask;
        };

        await hubConnection.StartAsync(TestContext.CancellationToken);

        await Assert.ThrowsAsync<Exception>(
            () => hubConnection.InvokeAsync(SharedAppMessages.ChangeAuthenticationState, accessToken, TestContext.CancellationToken));

        await closed.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.CancellationToken);

        Assert.AreEqual(HubConnectionState.Disconnected, hubConnection.State);
    }

    [TestMethod]
    public async Task AHubConnection_Should_BelongOnlyToTheLastSessionThatClaimedIt()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);
        await using var firstClient = server.CreateAppClient();
        await using var secondClient = server.CreateAppClient();

        await TestAccountUtils.CreateAndSignIn(firstClient, TestContext.CancellationToken);
        await TestAccountUtils.CreateAndSignIn(secondClient, TestContext.CancellationToken);

        var firstAccessToken = await GetAccessToken(firstClient);
        var secondAccessToken = await GetAccessToken(secondClient);

        var firstUserSessionId = IAuthTokenProvider.ParseAccessToken(firstAccessToken, validateExpiry: false).GetSessionId();
        var secondUserSessionId = IAuthTokenProvider.ParseAccessToken(secondAccessToken, validateExpiry: false).GetSessionId();

        await using var hubConnection = CreateHubConnection(server, firstAccessToken);

        await hubConnection.StartAsync(TestContext.CancellationToken);
        await hubConnection.InvokeAsync(SharedAppMessages.ChangeAuthenticationState, firstAccessToken, TestContext.CancellationToken);

        Assert.AreEqual(hubConnection.ConnectionId, await ReadSignalRConnectionId(server, firstUserSessionId));

        await hubConnection.InvokeAsync(SharedAppMessages.ChangeAuthenticationState, secondAccessToken, TestContext.CancellationToken);

        Assert.IsNull(await ReadSignalRConnectionId(server, firstUserSessionId),
            "The first user's OTP, PROFILE_UPDATED and SESSION_REVOKED messages would otherwise reach whoever uses the connection now.");
        Assert.AreEqual(hubConnection.ConnectionId, await ReadSignalRConnectionId(server, secondUserSessionId));
    }

    [TestMethod]
    public async Task SessionRevoked_Should_SignOutOnlyTheSessionItNames()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);
        await using var client = server.CreateAppClient();

        await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);

        var storageService = client.Services.GetRequiredService<IStorageService>();
        var pubSubService = client.Services.GetRequiredService<PubSubService>();
        var userSessionId = IAuthTokenProvider.ParseAccessToken(await GetAccessToken(client), validateExpiry: false).GetSessionId();

        pubSubService.Publish(SharedAppMessages.SESSION_REVOKED, JsonSerializer.SerializeToElement(Guid.NewGuid()));

        await Task.Delay(TimeSpan.FromSeconds(1), TestContext.CancellationToken);

        Assert.IsNotNull(await storageService.GetItem("refresh_token"), "Another session's revocation must not sign out this one.");

        pubSubService.Publish(SharedAppMessages.SESSION_REVOKED, JsonSerializer.SerializeToElement(userSessionId));

        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(15);

        while (await storageService.GetItem("refresh_token") is not null)
        {
            if (DateTimeOffset.UtcNow >= deadline)
                Assert.Fail("The revocation of this very session must sign it out.");

            await Task.Delay(TimeSpan.FromMilliseconds(50), TestContext.CancellationToken);
        }
    }

    private static async Task<string> GetAccessToken(AppClient client)
    {
        var accessToken = await client.Services.GetRequiredService<IAuthTokenProvider>().GetAccessToken();

        Assert.IsFalse(string.IsNullOrWhiteSpace(accessToken), "The client has to be signed in.");

        return accessToken!;
    }

    private static HubConnection CreateHubConnection(AppTestServer server, string? accessToken)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(server.ApiAppAddress, "app-hub"), options =>
            {
                options.Transports = HttpTransportType.WebSockets;
                options.AccessTokenProvider = () => Task.FromResult(accessToken);
            })
            .Build();
    }

    private async Task<Exception?> StartAndWaitForClose(HubConnection hubConnection)
    {
        var closed = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        hubConnection.Closed += exception =>
        {
            closed.TrySetResult(exception);
            return Task.CompletedTask;
        };

        try
        {
            await hubConnection.StartAsync(TestContext.CancellationToken);
        }
        catch (Exception exception)
        {
            closed.TrySetResult(exception);
        }

        var completed = await Task.WhenAny(closed.Task, Task.Delay(TimeSpan.FromSeconds(30), TestContext.CancellationToken));

        return completed == closed.Task ? await closed.Task : null;
    }

    private async Task<string?> ReadSignalRConnectionId(AppTestServer server, Guid userSessionId)
    {
        await using var scope = server.ApiApp.Services.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().UserSessions
            .Where(us => us.Id == userSessionId)
            .Select(us => us.SignalRConnectionId)
            .SingleAsync(TestContext.CancellationToken);
    }
}
