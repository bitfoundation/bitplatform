//+:cnd:noEmit
using Boilerplate.Server.Api.Features.Identity.OAuth;
using Boilerplate.Server.Api.Features.Identity.OAuth.Models;

namespace Boilerplate.Tests.Features.Identity.OAuth;

/// <summary>The sweep is the only way a code leaves the table, and every consent mints one.</summary>
[TestClass, TestCategory("IntegrationTest")]
public class OAuthRetentionTests
{
    public TestContext TestContext { get; set; } = default!;

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
        var liveCode = NewCode(userId, expiresOn: now + 60);

        await dbContext.OAuthAuthorizationCodes.AddRangeAsync([expiredCode, liveCode], TestContext.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.CancellationToken);

        try
        {
            await scope.ServiceProvider.GetRequiredService<OAuthRetentionJobRunner>().EnforceRetention(TestContext.CancellationToken);

            Assert.IsFalse(await dbContext.OAuthAuthorizationCodes.AnyAsync(code => code.Id == expiredCode.Id, TestContext.CancellationToken),
                "A code a day past its expiry has nothing left to prove and goes.");
            Assert.IsTrue(await dbContext.OAuthAuthorizationCodes.AnyAsync(code => code.Id == liveCode.Id, TestContext.CancellationToken),
                "A code that can still be exchanged must not be swept out from under the client waiting to exchange it.");
        }
        finally
        {
            Guid[] codeIds = [expiredCode.Id, liveCode.Id];

            await dbContext.OAuthAuthorizationCodes.Where(code => codeIds.Contains(code.Id)).ExecuteDeleteAsync(CancellationToken.None);
        }
    }

    private static OAuthAuthorizationCode NewCode(Guid userId, long expiresOn) => new()
    {
        Id = Guid.CreateSequentialGuid(),
        CodeHash = Guid.NewGuid().ToString("N"),
        ClientId = "retention-test-client",
        RedirectUri = "http://127.0.0.1:1/callback",
        Resource = "http://127.0.0.1/dev-mcp",
        Scope = "dev-mcp",
        CodeChallenge = "retention-test-challenge",
        UserId = userId,
        ExpiresOn = expiresOn
    };
}
