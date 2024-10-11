using Microsoft.EntityFrameworkCore;
using Boilerplate.Server.Api.Data;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Tests;

[TestClass]
public partial class TestsInitializer
{
    public static string access_token { get; private set; } = null!;

    [AssemblyInitialize]
    public static async Task Initialize(TestContext _)
    {
        await using var testServer = new AppTestServer();

        await testServer.Build().Start();

        await InitializeDatabase(testServer);

        await GetTestAccessToken(testServer);
    }

    private static async Task InitializeDatabase(AppTestServer testServer)
    {
        if (AppEnvironment.IsDev())
        {
            await using var scope = testServer.WebApp.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
        }
    }

    private static async Task GetTestAccessToken(AppTestServer testServer)
    {
        await using var scope = testServer.WebApp.Services.CreateAsyncScope();

        testServer.WebApp.Configuration["AppSettings:BearerTokenExpiration"] = "0.24:00:00";

        access_token = (await scope.ServiceProvider.GetRequiredService<IIdentityController>()
            .SignIn(new()
            {
                Email = "test@bitplatform.dev",
                Password = "123456"
            }, default)).AccessToken!;
    }
}
