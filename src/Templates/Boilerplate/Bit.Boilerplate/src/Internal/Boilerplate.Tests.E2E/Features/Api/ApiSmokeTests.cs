using Boilerplate.Client.Core.Infrastructure.Services.Contracts;

namespace Boilerplate.Tests.E2E.Features.Api;

/// <summary>
/// The browserless counterpart of the platform smoke tests: instead of opening an app, it calls the deployed APIs
/// through the client's own typed controllers and reads what they wrote through <see cref="AppDbContext"/>, which
/// doubles as the probe that <see cref="TestHost"/> is wired.
/// </summary>
[TestClass, TestCategory(TestCategories.Api), Retry(2)]
public partial class ApiSmokeTests
{
    /// <summary>The seeded non-admin member of the default store tenant; see UserConfiguration.</summary>
    private const string storeUserEmail = "store-user@bitplatform.dev";
    private const string storeUserPassword = "123456";

    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// The API's own write reaching its own database, per deployed API. Signing in makes the server insert a
    /// UserSession row, and the row is then ours to read and delete straight through the db context - which only lines
    /// up if the API and this host are looking at the same database, so every row also pins the deployments to the one
    /// <c>postgresdb</c> they share.
    /// <para>
    /// Deleting the row is also the cleanup: this runs against live deployments, so a session left behind is a session
    /// left signed in.
    /// </para>
    /// </summary>
    [TestMethod]
    [DataRow(App.Todo, DisplayName = nameof(App.Todo))]
    [DataRow(App.Sales, DisplayName = nameof(App.Sales))]
    [DataRow(App.AdminPanel, DisplayName = nameof(App.AdminPanel))]
    public async Task SignIn_Should_CreateAUserSession_DeletableThroughTheDbContext(App app)
    {
        var api = DeployedApps.ApiOf(app);

        await using var scope = TestHost.CreateScope(api);

        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var identityController = scope.ServiceProvider.GetRequiredService<IIdentityController>();

        Assert.AreEqual(new Uri(api), httpClient.BaseAddress,
            $"{nameof(TestHost.CreateScope)} is what aims the scope's http client, and every typed controller in it, at one API.");

        var signInResponse = await identityController.SignIn(new()
        {
            Email = storeUserEmail,
            Password = storeUserPassword
        }, TestContext.CancellationToken);

        Assert.IsFalse(signInResponse.RequiresTwoFactor, $"'{storeUserEmail}' is not expected to have two factor authentication enabled.");
        Assert.IsNotNull(signInResponse.AccessToken, "A completed sign in answers with an access token.");

        // The session the server just created is named by the access token it minted for it.
        var sessionId = IAuthTokenProvider.ParseAccessToken(signInResponse.AccessToken, validateExpiry: true).GetSessionId();

        var session = await dbContext.UserSessions
            .Include(userSession => userSession.User)
            .SingleOrDefaultAsync(userSession => userSession.Id == sessionId, TestContext.CancellationToken);

        Assert.IsNotNull(session, $"Session {sessionId} was created by {api} but is not in the database this host is connected to.");
        Assert.AreEqual(storeUserEmail, session.User!.Email, "The session belongs to whoever signed in.");

        var deleted = await dbContext.UserSessions
            .Where(userSession => userSession.Id == sessionId)
            .ExecuteDeleteAsync(TestContext.CancellationToken);

        Assert.AreEqual(1, deleted, "Exactly the one session signing in created.");

        Assert.IsFalse(await dbContext.UserSessions.AnyAsync(userSession => userSession.Id == sessionId, TestContext.CancellationToken),
            "The deleted session must be gone; anything else means the delete never reached the deployment's database.");
    }
}
