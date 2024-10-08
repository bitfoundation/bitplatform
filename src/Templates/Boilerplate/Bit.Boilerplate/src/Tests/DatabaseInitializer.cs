using Boilerplate.Server.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Boilerplate.Tests;

[TestClass]
public partial class DatabaseInitializer
{
    [AssemblyInitialize]
    public static async Task InitializeDatabase(TestContext _)
    {
        var testServer = new AppTestServer();

        await testServer.Build(services =>
        {
            // Services registered in this test project will be used instead of the application's services, allowing you to fake certain behaviors during testing.
        }).Start();

        if (AppEnvironment.IsDev())
        {
            await using var scope = testServer.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            try
            {
                await dbContext.Database.MigrateAsync();
            }
            catch { }

            try
            {
                await dbContext.Database.EnsureCreatedAsync();
            }
            catch { }
        }

        await testServer.DisposeAsync();
    }
}
