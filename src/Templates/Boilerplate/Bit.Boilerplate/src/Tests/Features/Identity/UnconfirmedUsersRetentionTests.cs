//+:cnd:noEmit
using Boilerplate.Server.Api;
using Boilerplate.Server.Api.Features.Identity;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// <c>UnconfirmedUsersRetentionJobRunner</c> is the only thing that removes an account auto-provisioned for someone who
/// never asked for one. Both halves are pinned: what must go, and what must not - a sweep that deletes live accounts is
/// far worse than one that deletes nothing.
/// <para>
/// <c>DoNotParallelize</c> because the sweep is global: a concurrent run would delete another test's fixture in the
/// window between creating the user and giving it the external login that is supposed to save it.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest"), DoNotParallelize]
public class UnconfirmedUsersRetentionTests
{
    [TestMethod]
    public async Task EnforceRetention_Should_DeleteAnExpiredUnconfirmedUser()
    {
        await using var server = await StartServer();

        var userId = await CreateUnconfirmedUser(server, createdOn: DateTimeOffset.UtcNow - Retention(server) - TimeSpan.FromMinutes(1));

        await EnforceRetention(server);

        Assert.IsFalse(await UserExists(server, userId), "An unconfirmed account past its retention period must be deleted.");
    }

    [TestMethod]
    public async Task EnforceRetention_Should_LeaveAnUnconfirmedUserThatIsStillWithinItsRetentionPeriod()
    {
        await using var server = await StartServer();

        var userId = await CreateUnconfirmedUser(server, createdOn: DateTimeOffset.UtcNow);

        await EnforceRetention(server);

        Assert.IsTrue(await UserExists(server, userId), "A user who has not run out of time to confirm must survive the sweep.");

        await DeleteUser(server, userId);
    }

    /// <summary>
    /// A confirmed account is old, has no session once <c>UserSessionsRetentionJobRunner</c> has removed it, and must
    /// still be untouchable - it is the one row this job could destroy that nobody could get back.
    /// </summary>
    [TestMethod]
    public async Task EnforceRetention_Should_LeaveAConfirmedUser_HoweverOldAndSessionless()
    {
        await using var server = await StartServer();

        var userId = await CreateUnconfirmedUser(server, createdOn: DateTimeOffset.UtcNow - Retention(server) - TimeSpan.FromDays(30), emailConfirmed: true);

        await EnforceRetention(server);

        Assert.IsTrue(await UserExists(server, userId), "Confirmation is what makes an account real; age and having no live session say nothing.");

        await DeleteUser(server, userId);
    }

    /// <summary>
    /// An external sign-in account can have neither e-mail nor phone confirmed and still be confirmed, because the
    /// provider vouched for it - the rule <c>AppUserConfirmation.IsConfirmedAsync</c> applies everywhere else.
    /// </summary>
    [TestMethod]
    public async Task EnforceRetention_Should_LeaveAnUnconfirmedUserThatHasAnExternalLogin()
    {
        await using var server = await StartServer();

        var userId = await CreateUnconfirmedUser(server, createdOn: DateTimeOffset.UtcNow - Retention(server) - TimeSpan.FromMinutes(1));

        await using (var scope = server.WebApp.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Set<UserLogin>().AddAsync(new UserLogin
            {
                UserId = userId,
                LoginProvider = "Google",
                ProviderKey = Guid.CreateVersion7().ToString()
            }, TestContext.CancellationToken);

            await dbContext.SaveChangesAsync(TestContext.CancellationToken);
        }

        await EnforceRetention(server);

        Assert.IsTrue(await UserExists(server, userId), "An external login is a confirmation, so this account is in use even with no confirmed e-mail or phone.");

        await DeleteUser(server, userId);
    }


    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        return server;
    }

    private static TimeSpan Retention(AppTestServer server)
        => server.WebApp.Services.GetRequiredService<ServerApiSettings>().Identity.UnconfirmedUsersRetention;

    /// <summary>
    /// Written straight to the database, because the age is the whole point and nothing auto-provisioned through the
    /// endpoints can be created in the past.
    /// </summary>
    private async Task<Guid> CreateUnconfirmedUser(AppTestServer server, DateTimeOffset createdOn, bool emailConfirmed = false)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userName = $"retention-{Guid.CreateVersion7():N}";
        var email = $"{userName}@example.com";

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = emailConfirmed,
            LockoutEnabled = true,
            CreatedOn = createdOn
        };

        await dbContext.Set<User>().AddAsync(user, TestContext.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.CancellationToken);

        return user.Id;
    }

    private async Task EnforceRetention(AppTestServer server)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<UnconfirmedUsersRetentionJobRunner>()
            .EnforceRetention(TestContext.CancellationToken);
    }

    private async Task<bool> UserExists(AppTestServer server, Guid userId)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Set<User>().AnyAsync(user => user.Id == userId, TestContext.CancellationToken);
    }

    /// <summary>Survivors are cleaned up by the test; the database outlives the run.</summary>
    private async Task DeleteUser(AppTestServer server, Guid userId)
    {
        try
        {
            await using var scope = server.WebApp.Services.CreateAsyncScope();

            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<User>()
                .Where(user => user.Id == userId)
                .ExecuteDeleteAsync(TestContext.CancellationToken);
        }
        catch (Exception) { }
    }

    public TestContext TestContext { get; set; } = default!;
}
