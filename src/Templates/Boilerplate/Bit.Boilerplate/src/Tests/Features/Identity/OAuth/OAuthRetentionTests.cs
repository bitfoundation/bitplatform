//+:cnd:noEmit
using Boilerplate.Server.Api.Features.Identity.OAuth;
using Boilerplate.Server.Api.Features.Identity.OAuth.Models;

namespace Boilerplate.Tests.Features.Identity.OAuth;

/// <summary>The sweep is the only way a code leaves the table, and every consent mints one.</summary>
[TestClass, TestCategory("IntegrationTest")]
public class OAuthRetentionTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>Belongs to this test alone, which is what makes deleting by it a safe cleanup.</summary>
    private const string RetentionTestClientId = "retention-test-client";

    [TestMethod]
    public async Task TheRetentionJob_Should_SweepExpiredCodes_AndKeepExchangeableOnes()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().ToUnixTimeSeconds();

        // A day past expiry, which is the grace a consumed code keeps so a replay still has a session to revoke.
        var expiredCode = NewCode(userId, expiresOn: now - 2 * 86400);
        // Inside that grace: without this case a runner that swept every expired code would pass the two below.
        var recentlyExpiredCode = NewCode(userId, expiresOn: now - 3600);
        var liveCode = NewCode(userId, expiresOn: now + 60);

        await dbContext.OAuthAuthorizationCodes.AddRangeAsync([expiredCode, recentlyExpiredCode, liveCode], TestContext.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.CancellationToken);

        try
        {
            await scope.ServiceProvider.GetRequiredService<OAuthRetentionJobRunner>().EnforceRetention(TestContext.CancellationToken);

            Assert.IsFalse(await dbContext.OAuthAuthorizationCodes.AnyAsync(code => code.Id == expiredCode.Id, TestContext.CancellationToken),
                "A code a day past its expiry has nothing left to prove and goes.");
            Assert.IsTrue(await dbContext.OAuthAuthorizationCodes.AnyAsync(code => code.Id == recentlyExpiredCode.Id, TestContext.CancellationToken),
                "An expired code is kept for a day so ConsumeCode can still tell a replay from an unknown code, and revoke the session it minted.");
            Assert.IsTrue(await dbContext.OAuthAuthorizationCodes.AnyAsync(code => code.Id == liveCode.Id, TestContext.CancellationToken),
                "A code that can still be exchanged must not be swept out from under the client waiting to exchange it.");
        }
        finally
        {
            // By client id, not the three ids: MySQL's provider can map neither a collection parameter nor an inlined
            // constant list. The id belongs to this test alone and the three rows are all it creates.
            await dbContext.OAuthAuthorizationCodes.Where(code => code.ClientId == RetentionTestClientId).ExecuteDeleteAsync(CancellationToken.None);
        }
    }

    private static OAuthAuthorizationCode NewCode(Guid userId, long expiresOn) => new()
    {
        Id = Guid.CreateSequentialGuid(),
        CodeHash = Guid.NewGuid().ToString("N"),
        ClientId = RetentionTestClientId,
        RedirectUri = "http://127.0.0.1:1/callback",
        Resource = "http://127.0.0.1/dev-mcp",
        Scope = "dev-mcp",
        CodeChallenge = "retention-test-challenge",
        UserId = userId,
        ExpiresOn = expiresOn
    };
}
