using Boilerplate.Client.Core.Infrastructure.Data;

namespace Boilerplate.Tests.Features.Todo;

[TestClass, TestCategory("IntegrationTest")]
public class OfflineDbScopeTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task TheOfflineDatabase_OnTheServer_Should_BelongToOneScope()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        var title = Guid.NewGuid().ToString();

        await using (var firstScope = server.WebApp.Services.CreateAsyncScope())
        {
            var firstFactory = firstScope.ServiceProvider.GetRequiredService<IDbContextFactory<AppOfflineDbContext>>();

            await using (var dbContext = await firstFactory.CreateDbContextAsync(TestContext.CancellationToken))
            {
                await dbContext.TodoItems.AddAsync(new() { Id = Guid.NewGuid().ToString(), Title = title }, TestContext.CancellationToken);
                await dbContext.SaveChangesAsync(TestContext.CancellationToken);
            }

            await using var sameScopeDbContext = await firstFactory.CreateDbContextAsync(TestContext.CancellationToken);
            Assert.IsTrue(await sameScopeDbContext.TodoItems.AnyAsync(t => t.Title == title, TestContext.CancellationToken),
                "Every DbContext of one circuit has to see the same offline database.");

            await using var secondScope = server.WebApp.Services.CreateAsyncScope();
            var secondFactory = secondScope.ServiceProvider.GetRequiredService<IDbContextFactory<AppOfflineDbContext>>();
            await using var secondScopeDbContext = await secondFactory.CreateDbContextAsync(TestContext.CancellationToken);

            Assert.IsFalse(await secondScopeDbContext.TodoItems.AnyAsync(t => t.Title == title, TestContext.CancellationToken),
                "Another circuit must not read what one user's offline page pulled into its database.");
        }
    }
}
