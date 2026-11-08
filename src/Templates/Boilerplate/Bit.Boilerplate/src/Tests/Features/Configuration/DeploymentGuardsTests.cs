using Boilerplate.Server.Api.Infrastructure.Data;

namespace Boilerplate.Tests.Features.Configuration;

[TestClass, TestCategory("IntegrationTest")]
public partial class DeploymentGuardsTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// The migrations seed accounts whose password is in this repository, so a deployment over a database that still has
    /// them stops before it changes anything.
    /// </summary>
    [TestMethod]
    public async Task ADeployment_Should_Refuse_ADatabaseWhoseAccountsStillHaveTheSeedPassword()
    {
        await using var server = new AppTestServer();

        await server.Build().Start(TestContext.CancellationToken);

        await using var scope = server.ApiApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var refused = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => DeploymentGuards.RefuseSeedPasswords(dbContext, TestContext.CancellationToken));

        Assert.Contains("store-admin", refused.Message);
    }
}
