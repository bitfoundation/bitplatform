using Boilerplate.Client.Core.Infrastructure.Services.Contracts;

namespace Boilerplate.Tests.E2E.Features.Smoke;

/// <summary>
/// The browserless smoke test: calls the deployed APIs through the client's own typed controllers and reads what they
/// wrote, which doubles as the probe that <see cref="DeployedApiClientProvider"/> is wired.
/// </summary>
[TestClass, TestCategory(TestCategories.Api), Retry(2)]
public partial class ApiSmokeTests
{
    /// <summary>The seeded non-admin member of the default store tenant; see UserConfiguration.</summary>
    private const string storeUserEmail = "store-user@bitplatform.dev";
    private const string storeUserPassword = "123456";

    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// Signing in inserts a UserSession this host can then read and delete, which only lines up if the API and this
    /// host share a database - so it also pins every deployment to the one <c>postgresdb</c>. The delete is the
    /// cleanup: against a live deployment, a session left behind is a session left signed in.
    /// </summary>
    [TestMethod]
    [DataRow(App.Todo, DisplayName = nameof(App.Todo))]
    [DataRow(App.Sales, DisplayName = nameof(App.Sales))]
    [DataRow(App.AdminPanel, DisplayName = nameof(App.AdminPanel))]
    public async Task SignIn_Should_CreateAUserSession_DeletableThroughTheDbContext(App app)
    {
        var api = DeployedApps.ApiOf(app);

        // Its own client for the http half, and the global one for the database half, which only that one holds.
        await using var apiClient = DeployedApiClientProvider.CreateApiClientFor(api);
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken);

        var identityController = apiClient.Services.GetRequiredService<IIdentityController>();
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(TestContext.CancellationToken);

        Assert.AreEqual(new Uri(api), apiClient.HttpClient.BaseAddress,
            $"{nameof(DeployedApiClientProvider.CreateApiClientFor)} is what aims the client's http client, and every typed controller beside it, at one API.");

        var signInResponse = await identityController.SignIn(new()
        {
            Email = storeUserEmail,
            Password = storeUserPassword
        }, TestContext.CancellationToken);

        Assert.IsFalse(signInResponse.RequiresTwoFactor, $"'{storeUserEmail}' is not expected to have two factor authentication enabled.");
        Assert.IsNotNull(signInResponse.AccessToken, "A completed sign in answers with an access token.");

        // The session the server just created is named by the access token it minted for it.
        var sessionId = IAuthTokenProvider.ParseAccessToken(signInResponse.AccessToken, validateExpiry: true).GetSessionId();

        int deleted;

        try
        {
            var session = await dbContext.UserSessions
                .Include(userSession => userSession.User)
                .SingleOrDefaultAsync(userSession => userSession.Id == sessionId, TestContext.CancellationToken);

            Assert.IsNotNull(session, $"Session {sessionId} was created by {api} but is not in the database this host is connected to.");
            Assert.AreEqual(storeUserEmail, session.User!.Email, "The session belongs to whoever signed in.");
        }
        finally
        {
            // Whatever the assertions above decided, the session must not stay signed in on a live deployment - and a
            // canceled or timed out test cannot clean up with its own token.
            deleted = await dbContext.UserSessions
                .Where(userSession => userSession.Id == sessionId)
                .ExecuteDeleteAsync(CancellationToken.None);
        }

        Assert.AreEqual(1, deleted, "Exactly the one session signing in created.");

        Assert.IsFalse(await dbContext.UserSessions.AnyAsync(userSession => userSession.Id == sessionId, TestContext.CancellationToken),
            "The deleted session must be gone; anything else means the delete never reached the deployment's database.");
    }
}
