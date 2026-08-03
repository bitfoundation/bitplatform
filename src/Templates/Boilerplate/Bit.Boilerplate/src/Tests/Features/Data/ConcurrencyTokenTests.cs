using Boilerplate.Server.Api.Features.Tenants;

namespace Boilerplate.Tests.Features.Data;

[TestClass, TestCategory("IntegrationTest")]
public partial class ConcurrencyTokenTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// Pins the whole <c>long Version</c> optimistic-concurrency contract, which is provider dependent and used to be
    /// silently inert on the default database. SQL Server maps <c>Version</c> to a store-generated <c>rowversion</c>
    /// and PostgreSQL onto the <c>xmin</c> system column, so both advance the stored value on every UPDATE; SQLite has
    /// no such type, <c>AppDbContext.ConfigureRowVersion</c> is not even emitted there, and nothing else in the app
    /// ever writes <c>Version</c> - so the <c>WHERE Id = @id AND Version = @clientVersion</c> that
    /// <c>AppDbContext.OnSavingChanges</c> builds was comparing the client's value against a column that never moved,
    /// and every concurrent edit was accepted. <c>OnSavingChanges</c> now advances the token itself wherever it is not
    /// store-generated.
    /// <para>
    /// The two assertions are deliberately separate: the first fails on the "the token never moves" shape even before
    /// a second writer exists, and it is the one that goes red on SQLite if the increment is ever removed. Both hold on
    /// every provider, so this test says the same thing in CI (PostgreSQL / SQL Server) as it does on a developer's
    /// SQLite machine. <c>Tenant</c> is used because it is the only <c>long Version</c> entity present in the default
    /// configuration; <c>Product</c>, <c>Category</c> and <c>SystemPrompt</c> need a module or signalR.
    /// </para>
    /// The tenant is created and deleted by this test so nothing it writes outlives the run.
    /// </summary>
    [TestMethod]
    public async Task Version_Should_Advance_And_RejectAStaleWrite()
    {
        await using var server = new AppTestServer();

        await server.Build(s => s.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        var tenantId = Guid.NewGuid();
        var tenantName = $"concurrency-{Guid.NewGuid():N}";

        try
        {
            await using (var scope = server.WebApp.Services.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await dbContext.Tenants.AddAsync(new Tenant { Id = tenantId, Name = tenantName, Title = "original" }, TestContext.CancellationToken);
                await dbContext.SaveChangesAsync(TestContext.CancellationToken);
            }

            // Two independent DbContexts stand in for two independent requests: each holds the Version it read, which
            // is what a client sends back in its DTO.
            await using var scopeA = server.WebApp.Services.CreateAsyncScope();
            await using var scopeB = server.WebApp.Services.CreateAsyncScope();
            var dbContextA = scopeA.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbContextB = scopeB.ServiceProvider.GetRequiredService<AppDbContext>();

            var tenantForA = await dbContextA.Tenants.SingleAsync(t => t.Id == tenantId, TestContext.CancellationToken);
            var tenantForB = await dbContextB.Tenants.SingleAsync(t => t.Id == tenantId, TestContext.CancellationToken);

            var versionBothRead = tenantForA.Version;

            tenantForA.Title = "written-by-A";
            await dbContextA.SaveChangesAsync(TestContext.CancellationToken);

            long versionAfterA;
            await using (var scope = server.WebApp.Services.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                versionAfterA = (await dbContext.Tenants.AsNoTracking().SingleAsync(t => t.Id == tenantId, TestContext.CancellationToken)).Version;
            }

            Assert.AreNotEqual(versionBothRead, versionAfterA,
                "A concurrency token that does not change after an UPDATE cannot detect anything: every stale write would match it forever.");

            // B still holds the pre-A version - exactly what a stale PUT carries.
            tenantForB.Title = "written-by-B";

            await Assert.ThrowsExactlyAsync<ConflictException>(
                () => dbContextB.SaveChangesAsync(TestContext.CancellationToken),
                "B's write is based on a version A has already superseded, so it must be rejected rather than silently overwrite A's edit.");

            await using (var scope = server.WebApp.Services.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var finalTenant = await dbContext.Tenants.AsNoTracking().SingleAsync(t => t.Id == tenantId, TestContext.CancellationToken);
                Assert.AreEqual("written-by-A", finalTenant.Title, "A's edit must survive; losing it is the data loss this test exists to catch.");
            }
        }
        finally
        {
            await using var scope = server.WebApp.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Tenants.Where(t => t.Id == tenantId).ExecuteDeleteAsync(TestContext.CancellationToken);
        }
    }
}
