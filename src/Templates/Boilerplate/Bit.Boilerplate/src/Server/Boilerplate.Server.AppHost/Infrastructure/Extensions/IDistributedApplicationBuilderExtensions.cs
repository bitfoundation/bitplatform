//+:cnd:noEmit
//#if (database == "SqlServer" || database == "PostgreSQL" || redis == true || filesStorage == "AzureBlobStorage")
using Aspire.Hosting.Azure;
//#endif
//#if (redis == true)
using Azure.Provisioning.RedisEnterprise;
//#endif
using Boilerplate.Server.AppHost.Infrastructure.Services;

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
        public IResourceBuilder<IResourceWithConnectionString> AddRedisCache()
        {
            // RunAsContainer does nothing in publish mode, so the Azure resource would be published as Azure Managed Redis
            // (Bicep) and the Docker Compose project would have no Redis at all. Published, it is the same container instead.
            if (builder.ExecutionContext.IsPublishMode)
                return ConfigureContainer(builder.AddRedis("redis-cache")).PublishAsComposeRedis();

            return builder.AddAzureManagedRedis("redis-cache")
                .RunAsContainer(redis => ConfigureContainer(redis)) // Remove this RunAsContainer and related configuration to use actual Azure Redis instance
                .ConfigureInfrastructure(infra =>
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

            static IResourceBuilder<RedisResource> ConfigureContainer(IResourceBuilder<RedisResource> redis)
            {
                return redis.WithRedisInsight(redisInsight => redisInsight.WithExplicitStart())
                    .WithRedisCommander(redisCommander => redisCommander.WithExplicitStart())
                    .WithImage("redis", "8")
                    .WithArgs(
                     "--save", "",                        // Backend API has its own L1 in-memory cache, no need to have RDB snapshots for the L2 redis cache in case of failures.
                     "--appendonly", "no",                // Disables AOF persistence as well for the same reason.
                     "--maxmemory-policy", "allkeys-lru"  // Documents the Azure-side EvictionPolicy below. Inert in the container: no maxmemory is set, so nothing is ever evicted.
                 ).WithOtlpExporter();
            }
        }

        /// <summary>
        /// Adds a Redis instance configured for Hangfire background jobs and distributed locking.
        /// This instance uses AOF persistence for durability.
        /// </summary>
        public IResourceBuilder<IResourceWithConnectionString> AddRedisPersistent()
        {
            // Published, it is the same container (See AddRedisCache).
            if (builder.ExecutionContext.IsPublishMode)
                return ConfigureContainer(builder.AddRedis("redis-persistent")).PublishAsComposeRedis();

            return builder.AddAzureManagedRedis("redis-persistent")
                .RunAsContainer(redis => ConfigureContainer(redis)) // Remove this RunAsContainer and related configuration to use actual Azure Redis instance
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

            static IResourceBuilder<RedisResource> ConfigureContainer(IResourceBuilder<RedisResource> redis)
            {
                return redis.WithRedisInsight(redisInsight => redisInsight.WithExplicitStart())
                    .WithRedisCommander(redisCommander => redisCommander.WithExplicitStart())
                    .WithImage("redis", "8")
                    .WithDataVolume()
                    .WithArgs(
                        "--dir", "/data",
                        "--appendonly", "yes",             // Enable AOF (Append only file) for data durability
                        "--appendfsync", "always",         // Sync to disk on every write for maximum durability. Temporarily disable it programmatically using C# code during bulk operations if needed.
                        "--save", "",                      // Disables RDB snapshots
                        "--maxmemory-policy", "noeviction" // Raise error when memory limit is reached instead of evicting keys
                    ).WithOtlpExporter();
            }
        }
        //#endif

        //#if (database == "SqlServer")
        /// <summary>
        /// Adds a SQL Server instance with DbGate management UI and a database named <c>mssqldb</c>.
        /// Uses SQL Server 2025 which supports embedded vector search.
        /// </summary>
        public IResourceBuilder<IResourceWithConnectionString> AddSqlServer()
        {
            // Published, it is the same container (See AddRedisCache).
            if (builder.ExecutionContext.IsPublishMode)
                return ConfigureContainer(builder.AddSqlServer("sqlserver"))
                    .PublishAsHealthyComposeService("/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P \"$$MSSQL_SA_PASSWORD\" -Q \"SELECT 1\" -b -o /dev/null")
                    .AddDatabase("mssqldb");

            return builder.AddAzureSqlServer("sqlserver")
                .RunAsContainer(sqlServer => ConfigureContainer(sqlServer)) // Remove this RunAsContainer and related configuration to use actual Azure SQL Server instance
                .AddDatabase("mssqldb");

            static IResourceBuilder<SqlServerServerResource> ConfigureContainer(IResourceBuilder<SqlServerServerResource> sqlServer)
            {
                return sqlServer.WithDbGate(dbGate => dbGate.WithDataVolume().WithExplicitStart())
                    .WithDataVolume()
                    .WithImage("mssql/server", "2025-latest") // Sql server 2025 supports embedded vector search.
                    .WithOtlpExporter();
            }
        }
        //#endif

        //#if (database == "PostgreSQL")
        /// <summary>
        /// Adds a PostgreSQL Server instance with pgAdmin and a database named <c>postgresdb</c>.
        /// Uses pgvector (pg18) image which supports embedded vector search.
        /// </summary>
        public IResourceBuilder<IResourceWithConnectionString> AddPostgreSQL()
        {
            // Published, it is the same container (See AddRedisCache).
            if (builder.ExecutionContext.IsPublishMode)
                return ConfigureContainer(builder.AddPostgres("postgresserver"))
                    .PublishAsHealthyComposeService("pg_isready -U postgres -d postgres")
                    .AddDatabase("postgresdb");

            return builder.AddAzurePostgresFlexibleServer("postgresserver")
                .RunAsContainer(postgres => ConfigureContainer(postgres)) // Remove this RunAsContainer and related configuration to use actual Azure PostgreSQL instance
                .AddDatabase("postgresdb");

            static IResourceBuilder<PostgresServerResource> ConfigureContainer(IResourceBuilder<PostgresServerResource> postgres)
            {
                return postgres.WithPgAdmin(pgAdmin => pgAdmin.WithExplicitStart())
                    .WithV18DataVolume()
                    .WithOptimizedSetup()
                    .WithImage("pgvector/pgvector", "pg18") // pgvector supports embedded vector search.
                    .WithOtlpExporter();
            }
        }
        //#endif

        //#if (database == "MySql")
        /// <summary>
        /// Adds a MySQL server instance with phpMyAdmin and a database named <c>mysqldb</c>.
        /// </summary>
        public IResourceBuilder<MySqlDatabaseResource> AddMySql()
        {
            return builder.AddMySql("mysqlserver")
                .WithPhpMyAdmin(phpMyAdmin => phpMyAdmin.WithExplicitStart())
                .WithDataVolume()
                .WithOtlpExporter()
                .PublishAsHealthyComposeService("mysqladmin ping -h 127.0.0.1 -u root -p\"$$MYSQL_ROOT_PASSWORD\" --silent")
                .AddDatabase("mysqldb");
        }
        //#endif

        //#if (database == "Sqlite")
        /// <summary>
        /// Adds a SQLite database instance with a web-based management UI.
        /// </summary>
        public IResourceBuilder<IResourceWithConnectionString> AddSqlite()
        {
            // Published, the database file lives in the /container_volume named volume (See ConfigureDockerComposePublishing).
            if (builder.ExecutionContext.IsPublishMode)
                return builder.AddConnectionString("sqlite", ReferenceExpression.Create($"Data Source=/container_volume/App_Data/BoilerplateDb.db;"));

            return builder.AddSqlite("sqlite", databaseFileName: "BoilerplateDb.db")
                .WithSqliteWeb(sqliteWeb => sqliteWeb.WithExplicitStart());
        }
        //#endif

        //#if (filesStorage == "AzureBlobStorage")
        public IResourceBuilder<IResourceWithConnectionString> AddAzureStorage()
        {
            // Published: an Azure Storage account outside the Docker Compose project, given as the `azureblobstorage`
            // connection string.
            if (builder.ExecutionContext.IsPublishMode)
                return builder.AddConnectionString("azureblobstorage");

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

        //#if (filesStorage == "S3")
        /// <summary>
        /// Adds RustFS, a lightweight S3-compatible object storage server, and exposes it as the <c>s3</c> connection
        /// string that <c>Program.Services.cs</c> builds its FluentStorage store out of.
        /// <see cref="RustFsResource"/> is a plain container resource with no connection string of its own, so it is
        /// assembled here from the container's primary endpoint and its credentials.
        /// https://github.com/konnta0/Aspire.Extensions
        /// </summary>
        public IResourceBuilder<IResourceWithConnectionString> AddS3Storage()
        {
            // Published: an S3-compatible store outside the Docker Compose project (Cloudflare R2 for example), given as
            // the `s3` connection string.
            if (builder.ExecutionContext.IsPublishMode)
                return builder.AddConnectionString("s3");

            var rustFs = builder.AddRustFs("s3")
                .WithDataVolume();

            rustFs.AddBucket("files");

            return rustFs;
        }
        //#endif

        //#if (signalR == true || database == "PostgreSQL" || database == "SqlServer")
        public IResourceBuilder<ContainerResource> AddTextEmbeddingsInference()
        {
            return builder.AddContainer("embedding", "ghcr.io/huggingface/text-embeddings-inference", "cpu-1.8")
                .WithArgs("--model-id", "BAAI/bge-small-en-v1.5")
                .WithVolume("tei-data", "/data")
                .WithHttpEndpoint(targetPort: 80)
                .WithExplicitStart();
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
            if (builder.ExecutionContext.IsPublishMode)
            {
                // Published: one cloudflared container in the Docker Compose project, started with the connector token of
                // a tunnel created once in Cloudflare (the `cloudflare-tunnel-tunnel-token` parameter). There, the tunnel's
                // public hostnames route to http://serverweb:8080 (and http://serverapi:8080), so the server opens no
                // inbound port. The hostnames below are only recorded: a connector makes no Cloudflare API call.
                var tunnel = builder.AddCloudflareTunnelConnector("cloudflare-tunnel")
                    .PublishAsDockerComposeService((_, service) => service.Restart = "unless-stopped");
                serverWebProject.WithCloudflareTunnel(tunnel, hostname: "use-your-web-app-url-here.com", endpointName: "http", routeName: "serverweb-route");
                //#if (api == "Standalone")
                serverApiProject.WithCloudflareTunnel(tunnel, hostname: "use-your-api-server-url-here.com", endpointName: "http", routeName: "serverapi-route");
                //#endif
                return;
            }

            var serverWebDomain = builder.Configuration["Parameters:cloudflare-tunnel-web-domain"];
            if (string.IsNullOrWhiteSpace(serverWebDomain) is false)
            {
                var tunnel = builder.AddCloudflareTunnel("cloudflare-tunnel-web");
                serverWebProject.WithCloudflareTunnel(tunnel, hostname: serverWebDomain);
            }
            else
            {
                builder.AddCloudflareQuickTunnel("cloudflare-tunnel-web")
                    .WithReference(serverWebProject);
            }

            //#if (api == "Standalone")
            // Standalone's API is a separate server, so expose it on its own hostname when one is configured.
            var serverApiDomain = builder.Configuration["Parameters:cloudflare-tunnel-api-domain"];
            if (string.IsNullOrWhiteSpace(serverApiDomain) is false)
            {
                var tunnel = builder.AddCloudflareTunnel("cloudflare-tunnel-api");
                serverApiProject.WithCloudflareTunnel(tunnel, hostname: serverApiDomain);
            }
            else
            {
                builder.AddCloudflareQuickTunnel("cloudflare-tunnel-api")
                    .WithReference(serverApiProject);
            }
            //#endif
        }
        //#endif

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

        /// <summary>Without a container runtime, a named volume never gets ready and Aspire ends the whole run, so drops them.</summary>
        public IDistributedApplicationBuilder RunWithoutContainerRuntime()
        {
            if (builder.ExecutionContext.IsRunMode is false || ContainerRuntime.IsRunning(builder))
                return builder;

            foreach (var container in builder.Resources.OfType<ContainerResource>())
            {
                foreach (var volume in container.Annotations.OfType<ContainerMountAnnotation>().Where(mount => mount.Type is ContainerMountType.Volume).ToArray())
                {
                    container.Annotations.Remove(volume);
                }
            }

            return builder;
        }

        /// <summary>
        /// Keeps every container of the application model alive between runs, so that each run reuses them instead of
        /// booting them up from scratch, and stops them once the app host hasn't run for <paramref name="idleMinutes"/>.
        /// If nothing can be started to stop them, they keep Aspire's default session lifetime instead.
        /// </summary>
        /// <remarks>
        /// Call it right before <see cref="IDistributedApplicationBuilder.Build"/>, so all the resources are already added
        /// while the application model is still mutable.
        /// </remarks>
        public IDistributedApplicationBuilder UsePersistentContainers(int idleMinutes = 15)
        {
            if (ContainerRuntime.IsRunning(builder) is false || ContainersKeeper.Start(builder, TimeSpan.FromMinutes(idleMinutes)) is false)
                return builder;

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
