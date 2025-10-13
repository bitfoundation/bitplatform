//+:cnd:noEmit
using Microsoft.EntityFrameworkCore;
//#if (aspire == true)
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Aspire.Hosting.DevTunnels;
using Aspire.Hosting.ApplicationModel;
//#endif
using Boilerplate.Server.Api.Data;
//#if (database  == 'Sqlite')
using Microsoft.Data.Sqlite;
//#endif
using Microsoft.Extensions.Hosting;

namespace Boilerplate.Tests;

[TestClass]
public partial class TestsInitializer
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
    /// be overridden in tests if needed, unlike <see cref="AppTestServer"/> used in <see cref="IdentityApiTests"/> 
    /// and <see cref="IdentityPagesTests"/>. The code below runs the Aspire app without the server web 
    /// project, retrieves necessary connection strings (e.g., database connection string), and passes 
    /// them to <see cref="AppTestServer"/>.
    /// </summary>
    private static async Task RunAspireHost(TestContext testContext)
    {
        var aspireBuilder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Boilerplate_Server_AppHost>(testContext.CancellationToken);

        foreach (var res in aspireBuilder.Resources.OfType<ProjectResource>().ToList())
            aspireBuilder.Resources.Remove(res);
        foreach (var res in aspireBuilder.Resources.OfType<DevTunnelResource>().ToList()) // remove unnecessary resources.
            aspireBuilder.Resources.Remove(res);

        aspireApp = await aspireBuilder.BuildAsync(testContext.CancellationToken);

        await aspireApp.StartAsync(testContext.CancellationToken);

        //#if (database == "SqlServer")
        Environment.SetEnvironmentVariable("ConnectionStrings__sqldb", await aspireApp.GetConnectionStringAsync("sqldb", testContext.CancellationToken));
        await aspireApp.ResourceNotifications.WaitForResourceAsync("sqldb", KnownResourceStates.Running, testContext.CancellationToken);
        //#elif (database == "PostgreSql")
        Environment.SetEnvironmentVariable("ConnectionStrings__postgresdb", await aspireApp.GetConnectionStringAsync("postgresdb", testContext.CancellationToken));
        await aspireApp.ResourceNotifications.WaitForResourceAsync("postgresdb", KnownResourceStates.Running, testContext.CancellationToken);
        //#elif (database == "MySql")
        Environment.SetEnvironmentVariable("ConnectionStrings__mysqldb", await aspireApp.GetConnectionStringAsync("mysqldb", testContext.CancellationToken));
        await aspireApp.ResourceNotifications.WaitForResourceAsync("mysqldb", KnownResourceStates.Running, testContext.CancellationToken);
        //#endif
        //#if (filesStorage == "AzureBlobStorage")
        Environment.SetEnvironmentVariable("ConnectionStrings__azureblobstorage", await aspireApp.GetConnectionStringAsync("azureblobstorage", testContext.CancellationToken));
        await aspireApp.ResourceNotifications.WaitForResourceAsync("azureblobstorage", KnownResourceStates.Running, testContext.CancellationToken);
        //#elif (filesStorage == "S3")
        Environment.SetEnvironmentVariable("ConnectionStrings__s3", await aspireApp.GetConnectionStringAsync("s3", testContext.CancellationToken));
        await aspireApp.ResourceNotifications.WaitForResourceAsync("s3", KnownResourceStates.Running, testContext.CancellationToken);
        //#endif
        Environment.SetEnvironmentVariable("ConnectionStrings__smtp", await aspireApp.GetConnectionStringAsync("smtp", testContext.CancellationToken));
        await aspireApp.ResourceNotifications.WaitForResourceAsync("smtp", KnownResourceStates.Running, testContext.CancellationToken);
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
            if ((await dbContext.Database.GetPendingMigrationsAsync()).Any())
            {
                await dbContext.Database.MigrateAsync();
            }
            else if ((await dbContext.Database.GetAppliedMigrationsAsync()).Any() is false)
            {
                throw new InvalidOperationException("No migrations have been added. Please ensure that migrations are added before running tests.");
            }
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
