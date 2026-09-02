//+:cnd:noEmit
//#if (aspire == true)
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Aspire.Hosting.ApplicationModel;
//#endif
//#if (database  == 'Sqlite')
using Microsoft.Data.Sqlite;
//#endif

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
        // Matched by name because some of them (OtlpLoopbackResource, CloudflareTunnelInstallerResource) are internal types.
        string[] typeNamesToBeRemoved = [
            "OtlpLoopbackResource",
            //#if (cloudflare == true)
            nameof(Aspire.Hosting.ApplicationModel.CloudflareTunnelResource),
            nameof(Aspire.Hosting.ApplicationModel.CloudflareQuickTunnelResource),
            "CloudflareTunnelInstallerResource",
            //#endif
            //#if (database == 'SqlServer')
            nameof(DbGateContainerResource),
            //#elif (database == 'PostgreSql')
            nameof(Aspire.Hosting.Postgres.PgAdminContainerResource),
            //#elif (database == 'MySql')
            nameof(Aspire.Hosting.MySql.PhpMyAdminContainerResource),
            //#elif (database == 'Sqlite')
            nameof(SqliteWebResource),
            //#endif
            //#if (redis == true)
            nameof(Aspire.Hosting.Redis.RedisInsightResource),
            nameof(Aspire.Hosting.Redis.RedisCommanderResource),
            //#endif
        ];

        foreach (var res in aspireAppBuilder.Resources.Where(r => typeNamesToBeRemoved.Contains(r.GetType().Name)).ToList())
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
    // The app's SQLite database is file-based in every shipped configuration, so this keep-alive connection only
    // matters when ConnectionStrings__sqlite is overridden to an in-memory database (Mode=Memory), which lives
    // only as long as at least one connection to it stays open.
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
