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
    extension(IDistributedApplicationBuilder builder)
    {
        /// <summary>
        /// Adds a Keycloak identity server. In run mode the development realm of the <c>./Infrastructure/Realms</c>
        /// folder is imported into it; that realm seeds accounts with well-known passwords, so it must never reach a
        /// published application model.
        /// https://aspire.dev/integrations/security/keycloak/
        /// </summary>
        public IResourceBuilder<KeycloakResource> AddKeycloak()
        {
            // No explicit host port: every other container here lets Aspire allocate one, and a fixed port cannot be
            // held by two app hosts at once - which `aspire run` plus `dotnet test` on one machine already is.
            var keycloak = builder.AddKeycloak("keycloak")
                .WithDataVolume();

            if (builder.ExecutionContext.IsRunMode)
            {
                keycloak.WithRealmImport("./Infrastructure/Realms");
            }

            return keycloak;
        }

        //#if (redis == true)
        /// <summary>
        /// Adds a Redis instance configured for FusionCache hybrid caching (L2 cache) and SignalR backplane.
        /// No persistence is needed for this cache instance.
        /// </summary>
        public IResourceBuilder<AzureManagedRedisResource> AddRedisCache()
        {
            return builder.AddAzureManagedRedis("redis-cache")
                .RunAsContainer(redis => // Remove this RunAsContainer and related configuration to use actual Azure Redis instance
                {
                    redis.WithRedisInsight()
                        .WithRedisCommander()
                        .WithImage("redis/redis-stack", "7.4.0-v8")
                        .WithArgs(
                         "--save", "",                        // Backend API has its own L1 in-memory cache, no need to have RDB snapshots for the L2 redis cache in case of failures.
                         "--appendonly", "no",                // Disables AOF persistence as well for the same reason.
                         "--maxmemory-policy", "allkeys-lru"  // Documents the Azure-side EvictionPolicy below. Inert in the container: no maxmemory is set, so nothing is ever evicted.
                     ).WithOtlpExporter();
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
        public IResourceBuilder<AzureManagedRedisResource> AddRedisPersistent()
        {
            return builder.AddAzureManagedRedis("redis-persistent")
                .RunAsContainer(redis => // Remove this RunAsContainer and related configuration to use actual Azure Redis instance
                {
                    redis.WithRedisInsight()
                        .WithRedisCommander()
                        .WithImage("redis/redis-stack", "7.4.0-v8")
                        .WithDataVolume()
                        .WithArgs(
                            "--dir", "/data",
                            "--appendonly", "yes",             // Enable AOF (Append only file) for data durability
                            "--appendfsync", "always",         // Sync to disk on every write for maximum durability. Temporarily disable it programmatically using C# code during bulk operations if needed.
                            "--save", "",                      // Disables RDB snapshots
                            "--maxmemory-policy", "noeviction" // Raise error when memory limit is reached instead of evicting keys
                        ).WithOtlpExporter();
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
        public IResourceBuilder<AzureSqlDatabaseResource> AddSqlServer()
        {
            return builder.AddAzureSqlServer("sqlserver")
                .RunAsContainer(sqlServer => // Remove this RunAsContainer and related configuration to use actual Azure SQL Server instance
                {
                    sqlServer.WithDbGate(config => config.WithDataVolume())
                        .WithDataVolume()
                        .WithImage("mssql/server", "2025-latest") // Sql server 2025 supports embedded vector search.
                        .WithOtlpExporter();
                })
                .AddDatabase("mssqldb");
        }
        //#endif

        //#if (database == "PostgreSQL")
        /// <summary>
        /// Adds a PostgreSQL Server instance with pgAdmin and a database named <c>postgresdb</c>.
        /// Uses pgvector (pg18) image which supports embedded vector search.
        /// </summary>
        public IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource> AddPostgreSQL()
        {
            return builder.AddAzurePostgresFlexibleServer("postgresserver")
                .RunAsContainer(postgresDatabase => // Remove this RunAsContainer and related configuration to use actual Azure PostgreSQL instance
                {
                    postgresDatabase.WithPgAdmin()
                        .WithV18DataVolume()
                        .WithOptimizedSetup()
                        .WithImage("pgvector/pgvector", "pg18") // pgvector supports embedded vector search.
                        .WithOtlpExporter();
                })
                .AddDatabase("postgresdb");
        }
        //#endif

        //#if (database == "MySql")
        /// <summary>
        /// Adds a MySQL server instance with phpMyAdmin and a database named <c>mysqldb</c>.
        /// </summary>
        public IResourceBuilder<MySqlDatabaseResource> AddMySql()
        {
            return builder.AddMySql("mysqlserver")
                .WithPhpMyAdmin()
                .WithDataVolume()
                .WithOtlpExporter()
                .AddDatabase("mysqldb");
        }
        //#endif

        //#if (database == "Sqlite")
        /// <summary>
        /// Adds a SQLite database instance with a web-based management UI.
        /// </summary>
        public IResourceBuilder<SqliteResource> AddSqlite()
        {
            return builder.AddSqlite("sqlite", databaseFileName: "BoilerplateDb.db")
                .WithSqliteWeb();
        }
        //#endif

        //#if (filesStorage == "AzureBlobStorage")
        public IResourceBuilder<AzureBlobStorageResource> AddAzureStorage()
        {
            return builder.AddAzureStorage("storage")
                .RunAsEmulator(azurite => // Remove this RunAsEmulator and related configuration to use actual Azure Blob Storage instance
                {
                    azurite
                        .WithOtlpExporter()
                        .WithDataVolume();
                })
                .AddBlobs("azureblobstorage");
        }
        //#endif

        //#if (cloudflare == true)
        /// <summary>
        /// Exposes the server projects through a Cloudflare Tunnel (cloudflared dials out, so the origin needs no
        /// public ip). Opt-in: does nothing unless the <c>cloudflare-tunnel-*</c> parameters are set (see appsettings.Development.json).
        /// </summary>
        public void AddCloudflareTunnels(
            IResourceBuilder<ProjectResource> serverWebProject
            //#if (api == "Standalone")
            , IResourceBuilder<ProjectResource> serverApiProject
            //#endif
            )
        {
            var domain = builder.Configuration["Parameters:cloudflare-tunnel-web-domain"];
            if (string.IsNullOrWhiteSpace(domain))
                return;

            var tunnel = builder.AddCloudflareTunnel("cloudflare-tunnel");

            serverWebProject.WithCloudflareTunnel(tunnel, hostname: domain);

            //#if (api == "Standalone")
            // Standalone's API is a separate server, so expose it on its own hostname when one is configured.
            var apiDomain = builder.Configuration["Parameters:cloudflare-tunnel-api-domain"];
            if (string.IsNullOrWhiteSpace(apiDomain) is false)
                serverApiProject.WithCloudflareTunnel(tunnel, hostname: apiDomain);
            //#endif
        }
        //#endif

        /// <summary>
        /// Adds the .NET MAUI Blazor Hybrid project and configures it for all supported device targets
        /// (Windows, macOS Catalyst, iOS Device, iOS Simulator, Android Device, Android Emulator).
        /// Uses dev tunnels for OpenTelemetry data collection on mobile/remote targets.
        /// </summary>
        public IResourceBuilder<MauiProjectResource> AddMaui(
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

        /// <summary>
        /// Projects' launchSettings bind <c>http://*:port</c> so a direct <c>dotnet run</c> is reachable over the LAN
        /// (e.g. from Android/iOS devices), but Aspire can't give a container a reachable address for a wildcard host,
        /// so that endpoint only makes the ingress container fail to start. Drops every wildcard endpoint from the
        /// model - run mode only, and launchSettings is untouched, so a direct run still binds every interface.
        /// </summary>
        public IDistributedApplicationBuilder RemoveWildcardEndpoints()
        {
            if (builder.ExecutionContext.IsRunMode is false)
                return builder;

            foreach (var project in builder.Resources.OfType<ProjectResource>().ToArray())
            {
                foreach (var wildcard in project.Annotations.OfType<EndpointAnnotation>().Where(endpoint => endpoint.TargetHost is "*").ToArray())
                    project.Annotations.Remove(wildcard);
            }

            return builder;
        }

        /// <summary>
        /// Gives every container of the application model a persistent lifetime, so that they are created once and are
        /// then reused by every subsequent run, instead of being re-created and booted up from scratch each and every time.
        /// </summary>
        /// <remarks>
        /// Call it right before <see cref="IDistributedApplicationBuilder.Build"/>, so all the resources are already added
        /// while the application model is still mutable.
        /// </remarks>
        public IDistributedApplicationBuilder UsePersistentContainers()
        {
            foreach (var container in builder.Resources.OfType<ContainerResource>().ToArray())
            {
                builder.CreateResourceBuilder(container)
                    .WithEnvironment(context =>
                    {
                        // Aspire injects its own OTLP endpoint (https://aspire.dev.internal:<port>) into the containers,
                        // and that port is allocated again on every run. Since the environment variables are part of the
                        // container's lifecycle key, leaving them in place makes Aspire re-create every container on each
                        // run ("Found existing Container, but calculated lifecycle key doesn't match"), which defeats the
                        // whole purpose. Dropping them costs us the containers' telemetry in the Aspire dashboard only.
                        foreach (var otelVariable in context.EnvironmentVariables.Keys.Where(key => key.StartsWith("OTEL_")).ToArray())
                        {
                            context.EnvironmentVariables.Remove(otelVariable);
                        }
                    })
                    .WithLifetime(ContainerLifetime.Persistent);
            }

            return builder;
        }
    }
}
