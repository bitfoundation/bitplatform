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

    /// <summary>
    /// The running Aspire host - with real backing containers such as Redis
    /// Started by <see cref="RunAspireHost"/> during assembly initialization.
    /// </summary>
    internal static DistributedApplication AspireApp => aspireApp ?? throw new InvalidOperationException();
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
        var aspireAppBuilder = await DistributedApplicationTestingBuilder
            .CreateAsync<Program>(testContext.CancellationToken);

        foreach (var res in aspireAppBuilder.Resources.Where(r => r is ProjectResource or IResourceWithParent<ProjectResource>).ToList())
            aspireAppBuilder.Resources.Remove(res);

        // The following resources are not that much useful in tests and just add to the startup time, so we remove them from the application.
        foreach (var res in aspireAppBuilder.Resources.Where(r => r is DevTunnelResource or DevTunnelPortResource
            //#if (database == 'SqlServer')
            or DbGateContainerResource
            //#elif (database == 'PostgreSql')
            or Aspire.Hosting.Postgres.PgAdminContainerResource
            //#elif (database == 'MySql')
            or Aspire.Hosting.MySql.PhpMyAdminContainerResource
            //#elif (database == 'Sqlite')
            or SqliteWebResource
            //#endif
            //#if (redis == true)
            or Aspire.Hosting.Redis.RedisInsightResource
            or Aspire.Hosting.Redis.RedisCommanderResource
            //#endif
            or Aspire.Hosting.Maui.MauiAndroidDeviceResource
            or Aspire.Hosting.Maui.MauiAndroidEmulatorResource
            || r.GetType().Name is "OtlpLoopbackResource").ToList())
        {
            aspireAppBuilder.Resources.Remove(res);
        }

        aspireApp = await aspireAppBuilder.BuildAsync(testContext.CancellationToken);

        await aspireApp.StartAsync(testContext.CancellationToken);

        foreach (var connectionString in aspireAppBuilder.Resources.OfType<IResourceWithConnectionString>())
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
