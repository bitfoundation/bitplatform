//+:cnd:noEmit
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// `aspire publish` turns the application model into the Docker Compose project the CD deploys to the server, the Aspire
// dashboard included; run mode leaves this resource out of the model. See `ConfigureDockerComposePublishing` below.
var compose = builder.AddDockerComposeEnvironment("compose");

// Check out appsettings.Development.json for credentials/passwords settings.

//#if(redis == true)
// Redis cache for FusionCache hybrid caching (L2 cache) and SignalR backplane - no persistence needed
var redisCache = builder.AddRedisCache();

// Redis for Hangfire background jobs, and distributed locking - persistent with AOF for durability
var redisPersistent = builder.AddRedisPersistent();
//#endif

//#if (database == "SqlServer")
var sqlDatabase = builder.AddSqlServer();
//#elif (database == "PostgreSQL")
var postgresDatabase = builder.AddPostgreSQL();
//#elif (database == "MySql")
var mySqlDatabase = builder.AddMySql();
//#elif (database == "Sqlite")
var sqlite = builder.AddSqlite();
//#endif

//#if (filesStorage == "AzureBlobStorage")
var azureBlobStorage = builder.AddAzureStorage();
//#elif (filesStorage == "S3")
var s3Storage = builder.AddS3Storage();
//#endif

// Development only: its realm seeds accounts with well-known passwords, and a production Keycloak needs a hostname,
// TLS and a database of its own (as published, `start --import-realm` exits with "Key material not provided").
var keycloak = builder.ExecutionContext.IsRunMode ? builder.AddKeycloak() : null;

//#if (signalR == true || database == "PostgreSQL" || database == "SqlServer")
var embedding = builder.AddTextEmbeddingsInference();
//#endif

var serverWebProject = builder.AddProject("serverweb", "../Boilerplate.Server.Web/Boilerplate.Server.Web.csproj")
    .WithExternalHttpEndpoints();

// Adding health checks endpoints to applications in non-development environments has security implications.
// See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
if (builder.Environment.IsDevelopment())
{
    serverWebProject.WithHttpHealthCheck("/alive");
}

//#if (api == "Standalone")
var serverApiProject = builder.AddProject("serverapi", "../Boilerplate.Server.Api/Boilerplate.Server.Api.csproj")
    .WithExternalHttpEndpoints();

// Adding health checks endpoints to applications in non-development environments has security implications.
// See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
if (builder.Environment.IsDevelopment())
{
    serverApiProject.WithHttpHealthCheck("/alive");
}

serverWebProject.WithReference(serverApiProject);
//#if (database == "SqlServer")
serverApiProject.WithReference(sqlDatabase).WaitFor(sqlDatabase);
//#elif (database == "PostgreSQL")
serverApiProject.WithReference(postgresDatabase).WaitFor(postgresDatabase);
//#elif (database == "MySql")
serverApiProject.WithReference(mySqlDatabase).WaitFor(mySqlDatabase);
//#elif (database == "Sqlite")
serverApiProject.WithReference(sqlite).WaitFor(sqlite);
//#endif
//#if (filesStorage == "AzureBlobStorage")
serverApiProject.WithReference(azureBlobStorage);
//#elif (filesStorage == "S3")
serverApiProject.WithReference(s3Storage);
//#endif
if (keycloak is not null)
{
    serverApiProject.WithReference(keycloak);
}
//#if (signalR == true || database == "PostgreSQL" || database == "SqlServer")
serverApiProject.WithEnvironment("AI__HuggingFace__EmbeddingEndpoint", embedding.GetEndpoint("http"));
//#endif
//#if (redis == true)
serverApiProject.WithReference(redisCache).WaitFor(redisCache);
serverWebProject.WithReference(redisCache).WaitFor(redisCache);
serverApiProject.WithReference(redisPersistent).WaitFor(redisPersistent);
//#endif
//#else

//#if (database == "SqlServer")
serverWebProject.WithReference(sqlDatabase).WaitFor(sqlDatabase);
//#elif (database == "PostgreSQL")
serverWebProject.WithReference(postgresDatabase).WaitFor(postgresDatabase);
//#elif (database == "MySql")
serverWebProject.WithReference(mySqlDatabase).WaitFor(mySqlDatabase);
//#elif (database == "Sqlite")
serverWebProject.WithReference(sqlite).WaitFor(sqlite);
//#endif
//#if (filesStorage == "AzureBlobStorage")
serverWebProject.WithReference(azureBlobStorage);
//#elif (filesStorage == "S3")
serverWebProject.WithReference(s3Storage);
//#endif
if (keycloak is not null)
{
    serverWebProject.WithReference(keycloak);
}
//#if (signalR == true || database == "PostgreSQL" || database == "SqlServer")
serverWebProject.WithEnvironment("AI__HuggingFace__EmbeddingEndpoint", embedding.GetEndpoint("http"));
//#endif
//#if (redis == true)
serverWebProject.WithReference(redisCache).WaitFor(redisCache);
serverWebProject.WithReference(redisPersistent).WaitFor(redisPersistent);
//#endif
//#endif

//#if (cloudflare == true)
// cloudflared connects straight to the projects (no reverse proxy) - possible now that RemoveWildcardEndpoints drops http2.
builder.AddCloudflareTunnels(serverWebProject
    //#if (api == "Standalone")
    , serverApiProject
    //#endif
    );
//#endif

//#if (database != "Other" || redis == true)
// DbGate, the one web UI for the database and Redis. In run mode it starts from the Aspire dashboard.
builder.ConfigureDbGate();

//#endif
// Publish mode only: the registry the images go to, the migrations service, health checks, restart policies, the
// dashboards on the server's loopback and the settings the deployed server reads from the deploy job.
builder.ConfigureDockerComposePublishing(compose, serverWebProject
    //#if (api == "Standalone")
    , serverApiProject
    //#endif
    );

if (builder.ExecutionContext.IsRunMode) // The following project is only added for testing purposes.
{
    // Blazor WebAssembly Standalone project.
    builder.AddProject("clientwebwasm", "../../Client/Boilerplate.Client.Web/Boilerplate.Client.Web.csproj")
        .WithExplicitStart();

    var mailpit = builder.AddMailPit("smtp") // For testing purposes only, in production, you would use a real SMTP server.
        .WithOtlpExporter()
        .WithDataVolume("mailpit");

    //#if (api == "Standalone")
    serverApiProject.WithReference(mailpit);
    //#else
    serverWebProject.WithReference(mailpit);
    //#endif

    if (OperatingSystem.IsWindows())
    {
        // Blazor Hybrid Windows project.
        builder.AddProject("clientwindows", "../../Client/Boilerplate.Client.Windows/Boilerplate.Client.Windows.csproj")
            .WithExplicitStart();
    }

    // Without Docker or Podman the projects still run, only the containers are missing. See `.docs/20- .NET Aspire.md`.
    builder.RunWithoutContainerRuntime();

    // Keeps the containers alive between runs and stops them once the app host hasn't run for 15 minutes. See `.docs/20- .NET Aspire.md`.
    builder.UsePersistentContainers();

    builder.RemoveWildcardEndpoints();
}

await builder
    .Build()
    .RunAsync();
