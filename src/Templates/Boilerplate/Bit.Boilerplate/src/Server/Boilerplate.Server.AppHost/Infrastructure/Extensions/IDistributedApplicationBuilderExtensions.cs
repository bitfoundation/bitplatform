//+:cnd:noEmit
using Aspire.Hosting.Maui;
//#if (database == "SqlServer" || database == "PostgreSQL" || redis == true || filesStorage == "AzureBlobStorage")
using Aspire.Hosting.Azure;
//#endif
using Aspire.Hosting.DevTunnels;
//#if (redis == true)
using Azure.Provisioning.RedisEnterprise;
//#endif

namespace Aspire.Hosting;

public static class IDistributedApplicationBuilderExtensions
{
    //#if (redis == true)
    /// <summary>
    /// Adds a Redis instance configured for FusionCache hybrid caching (L2 cache) and SignalR backplane.
    /// No persistence is needed for this cache instance.
    /// </summary>
    public static IResourceBuilder<AzureManagedRedisResource> AddRedisCache(this IDistributedApplicationBuilder builder)
    {
        return builder.AddAzureManagedRedis("redis-cache")
            .RunAsContainer(redis => // Remove this RunAsContainer and related configuration to use actual Azure Redis instance
            {
                redis.WithRedisInsight()
                    .WithRedisCommander()
                    .WithImage("redis/redis-stack", "latest")
                    .WithArgs(
                     "--save", "",                        // Backend API has its own L1 in-memory cache, no need to have RDB snapshots for the L2 redis cache in case of failures.
                     "--appendonly", "no",                // Disables AOF persistence as well for the same reason.
                     "--maxmemory-policy", "allkeys-lru"  // Evict least recently used keys when memory limit is reached
                 );
            }).ConfigureInfrastructure(infra =>
            {
                var db = infra.GetProvisionableResources()
                    .OfType<RedisEnterpriseDatabase>()
                    .Single();

                db.Persistence = new()
                {
                    IsAofEnabled = false,
                    IsRdbEnabled = false
                };

                db.EvictionPolicy = RedisEnterpriseEvictionPolicy.AllKeysLru;
            });
    }

    /// <summary>
    /// Adds a Redis instance configured for Hangfire background jobs and distributed locking.
    /// This instance uses AOF persistence for durability.
    /// </summary>
    public static IResourceBuilder<AzureManagedRedisResource> AddRedisPersistent(this IDistributedApplicationBuilder builder)
    {
        return builder.AddAzureManagedRedis("redis-persistent")
            .RunAsContainer(redis => // Remove this RunAsContainer and related configuration to use actual Azure Redis instance
            {
                redis.WithRedisInsight()
                    .WithRedisCommander()
                    .WithImage("redis/redis-stack", "latest")
                    .WithArgs(
                        "--appendonly", "yes",             // Enable AOF (Append only file) for data durability
                        "--appendfsync", "always",         // Sync to disk on every write for maximum durability. Temporarily disable it programmatically using C# code during bulk operations if needed.
                        "--save", "",                      // Disables RDB snapshots
                        "--maxmemory-policy", "noeviction" // Raise error when memory limit is reached instead of evicting keys
                    );
            })
            .ConfigureInfrastructure(infra =>
            {
                var db = infra.GetProvisionableResources()
                    .OfType<RedisEnterpriseDatabase>()
                    .Single();

                // --appendonly yes + --appendfsync always  
                db.Persistence = new()
                {
                    IsAofEnabled = true,
                    AofFrequency = PersistenceSettingAofFrequency.Always,
                    IsRdbEnabled = false  // --save ""  
                };

                // --maxmemory-policy noeviction  
                db.EvictionPolicy = RedisEnterpriseEvictionPolicy.NoEviction;
            });
    }
    //#endif

    //#if (database == "SqlServer")
    /// <summary>
    /// Adds a SQL Server instance with DbGate management UI and a database named <c>mssqldb</c>.
    /// Uses SQL Server 2025 which supports embedded vector search.
    /// </summary>
    public static IResourceBuilder<AzureSqlDatabaseResource> AddSqlServer(this IDistributedApplicationBuilder builder)
    {
        return builder.AddAzureSqlServer("sqlserver")
            .RunAsContainer(sqlServer => // Remove this RunAsContainer and related configuration to use actual Azure SQL Server instance
            {
                sqlServer.WithDbGate(config => config.WithDataVolume())
                    .WithDataVolume()
                    .WithImage("mssql/server", "2025-latest"); // Sql server 2025 supports embedded vector search.
            })
            .AddDatabase("mssqldb");
    }
    //#endif

    //#if (database == "PostgreSQL")
    /// <summary>
    /// Adds a PostgreSQL Server instance with pgAdmin and a database named <c>postgresdb</c>.
    /// Uses pgvector (pg18) image which supports embedded vector search.
    /// </summary>
    public static IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource> AddPostgreSQL(this IDistributedApplicationBuilder builder)
    {
        return builder.AddAzurePostgresFlexibleServer("postgresserver")
            .RunAsContainer(postgresDatabase => // Remove this RunAsContainer and related configuration to use actual Azure PostgreSQL instance
            {
                postgresDatabase.WithPgAdmin()
                    .WithV18DataVolume()
                    .WithOptimizedSetup()
                    .WithImage("pgvector/pgvector", "pg18"); // pgvector supports embedded vector search.
            })
            .AddDatabase("postgresdb");
    }
    //#endif

    //#if (database == "MySql")
    /// <summary>
    /// Adds a MySQL server instance with phpMyAdmin and a database named <c>mysqldb</c>.
    /// </summary>
    public static IResourceBuilder<MySqlDatabaseResource> AddMySql(this IDistributedApplicationBuilder builder)
    {
        return builder.AddMySql("mysqlserver")
            .WithPhpMyAdmin()
            .WithDataVolume()
            .AddDatabase("mysqldb");
    }
    //#endif

    //#if (database == "Sqlite")
    /// <summary>
    /// Adds a SQLite database instance with a web-based management UI.
    /// </summary>
    public static IResourceBuilder<SqliteResource> AddSqlite(this IDistributedApplicationBuilder builder)
    {
        return builder.AddSqlite("sqlite", databaseFileName: "BoilerplateDb.db")
            .WithSqliteWeb();
    }
    //#endif

    //#if (filesStorage == "AzureBlobStorage")
    public static IResourceBuilder<AzureBlobStorageResource> AddAzureStorage(this IDistributedApplicationBuilder builder)
    {
        return builder.AddAzureStorage("storage")
            .RunAsEmulator(azurite => // Remove this RunAsEmulator and related configuration to use actual Azure Blob Storage instance
            {
                azurite
                    .WithDataVolume();
            })
            .AddBlobs("azureblobstorage");
    }
    //#endif

    /// <summary>
    /// Adds the .NET MAUI Blazor Hybrid project and configures it for all supported device targets
    /// (Windows, macOS Catalyst, iOS Device, iOS Simulator, Android Device, Android Emulator).
    /// Uses dev tunnels for OpenTelemetry data collection on mobile/remote targets.
    /// </summary>
    public static IResourceBuilder<MauiProjectResource> AddMaui(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> serverWebProject,
        IResourceBuilder<DevTunnelResource> tunnel)
    {
        var mauiapp = builder.AddMauiProject("mauiapp", @"../../Client/Boilerplate.Client.Maui/Boilerplate.Client.Maui.csproj");

        if (OperatingSystem.IsWindows())
        {
            mauiapp.AddWindowsDevice()
                .WithExplicitStart()
                .WithReference(serverWebProject);
        }

        if (OperatingSystem.IsMacOS())
        {
            mauiapp.AddMacCatalystDevice()
                .WithExplicitStart()
                .WithReference(serverWebProject);
        }

        if (OperatingSystem.IsMacOS())
        {
            // Windows supports iOS Simulator and Physical devices if there's a mac connected to network, but the following runners only work on macOS for now.

            mauiapp.AddiOSDevice()
                .WithExplicitStart()
                .WithOtlpDevTunnel() // Required for OpenTelemetry data collection
                .WithReference(serverWebProject, tunnel);

            mauiapp.AddiOSSimulator()
                .WithExplicitStart()
                .WithOtlpDevTunnel() // Required for OpenTelemetry data collection
                .WithReference(serverWebProject, tunnel);
        }

        mauiapp.AddAndroidDevice()
            .WithExplicitStart()
            .WithOtlpDevTunnel() // Required for OpenTelemetry data collection
            .WithReference(serverWebProject, tunnel);

        mauiapp.AddAndroidEmulator()
            .WithExplicitStart()
            .WithOtlpDevTunnel() // Required for OpenTelemetry data collection
            .WithReference(serverWebProject, tunnel);

        return mauiapp;
    }
}
