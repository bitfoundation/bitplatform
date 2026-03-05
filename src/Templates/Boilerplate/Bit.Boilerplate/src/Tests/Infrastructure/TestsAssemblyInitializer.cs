//+:cnd:noEmit
using Microsoft.EntityFrameworkCore;
//#if (aspire == true)
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Aspire.Hosting.DevTunnels;
using Aspire.Hosting.ApplicationModel;
//#endif
//#if (database  == 'Sqlite')
using Microsoft.Data.Sqlite;
//#endif
using Microsoft.Extensions.Hosting;
using Boilerplate.Tests.Features.Identity;
using Boilerplate.Server.Api.Infrastructure.Data;

namespace Boilerplate.Tests.Infrastructure;

[TestClass]
public partial class TestsAssemblyInitializer
{
    //#if (aspire == true)
    private static DistributedApplication? aspireApp;
    //#endif

    [AssemblyInitialize]
    public static async Task Initialize(TestContext testContext)
    {
        //#if (aspire == true)
        await RunAspireHost(testContext);
        //#endif
        await using var testServer = new AppTestServer();

        await testServer.Build().Start(testContext.CancellationToken);

        await InitializeDatabase(testServer);
    }

    //#if (aspire == true)
    /// <summary>
    /// Aspire.Hosting.Testing executes the complete application, including dependencies like databases, 
    /// closely mimicking a production environment. However, it has a limitation: backend services cannot 
    /// be overridden in tests if needed, unlike <see cref="AppTestServer"/> used in <see cref="UITests"/> 
    /// and <see cref="IntegrationTests"/>. The code below runs the Aspire app without the server web 
    /// project, retrieves necessary connection strings (e.g., database connection string), and passes 
    /// them to <see cref="AppTestServer"/>, so you can override services in the server project.
    /// </summary>
    private static async Task RunAspireHost(TestContext testContext)
    {
        var aspireBuilder = await DistributedApplicationTestingBuilder
            .CreateAsync<Program>(testContext.CancellationToken);

        foreach (var res in aspireBuilder.Resources.OfType<ProjectResource>().ToList())
            aspireBuilder.Resources.Remove(res);
        foreach (var res in aspireBuilder.Resources.OfType<DevTunnelResource>().ToList()) // remove unnecessary resources.
            aspireBuilder.Resources.Remove(res);

        aspireApp = await aspireBuilder.BuildAsync(testContext.CancellationToken);

        await aspireApp.StartAsync(testContext.CancellationToken);

        foreach (var connectionString in aspireBuilder.Resources.OfType<IResourceWithConnectionString>())
        {
            Environment.SetEnvironmentVariable($"ConnectionStrings__{connectionString.Name}", await aspireApp.GetConnectionStringAsync(connectionString.Name, testContext.CancellationToken));
            await aspireApp.ResourceNotifications.WaitForResourceAsync(connectionString.Name, [.. KnownResourceStates.TerminalStates, KnownResourceStates.Running], testContext.CancellationToken);
        }
    }
    //#endif

    //#if (database  == 'Sqlite')
    //SQLite database in in-memory mode only lives as long as at least one connection to it is open
    //This connection is required to keep the database alive during the test run.
    private static SqliteConnection connection = null!;
    //#endif
    private static async Task InitializeDatabase(AppTestServer testServer)
    {
        if (testServer.WebApp.Environment.IsDevelopment())
        {
            await using var scope = testServer.WebApp.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            //#if (database  == 'Sqlite')
            //#if (IsInsideProjectTemplate == true)
            if (dbContext.Database.ProviderName!.EndsWith("Sqlite", StringComparison.InvariantCulture))
            {
                //#endif
                connection = new SqliteConnection(dbContext.Database.GetConnectionString());
                await connection.OpenAsync();
                //#if (IsInsideProjectTemplate == true)
            }
            //#endif
            //#endif
            await dbContext.Database.EnsureCreatedAsync(); // It's recommended to start using ef-core migrations.
        }
    }

    //#if (aspire == true)
    [AssemblyCleanup]
    public static async Task Cleanup()
    {
        if (aspireApp is not null)
        {
            await aspireApp.StopAsync();
            await aspireApp.DisposeAsync();
        }
    }
    //#endif
}
