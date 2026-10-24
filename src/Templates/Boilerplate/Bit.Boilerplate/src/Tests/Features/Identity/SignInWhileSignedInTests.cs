namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("IntegrationTest"), TestCategory("Identity")]
public partial class SignInWhileSignedInTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task AFailedSignIn_Should_KeepTheCurrentSession()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var client = server.CreateAppClient();

        var (email, userId) = await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);

        var sessionId = (await client.AuthManager.GetAuthenticationStateAsync()).User.GetSessionId();

        await Assert.ThrowsAsync<KnownException>(() => client.AuthManager.SignIn(new()
        {
            Email = email,
            Password = "not-the-password"
        }, TestContext.CancellationToken));

        var user = (await client.AuthManager.GetAuthenticationStateAsync()).User;

        Assert.IsTrue(user.IsAuthenticated() && user.GetSessionId() == sessionId,
            "A sign-in the server refused must not end the session the user already had.");
        Assert.AreEqual(1, await CountUserSessions(server, userId));
    }

    private async Task<int> CountUserSessions(AppTestServer server, Guid userId)
    {
        await using var scope = server.ApiApp.Services.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().UserSessions
            .CountAsync(us => us.UserId == userId, TestContext.CancellationToken);
    }
}
