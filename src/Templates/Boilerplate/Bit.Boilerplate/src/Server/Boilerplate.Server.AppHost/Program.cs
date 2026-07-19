//+:cnd:noEmit
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

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
var s3Storage = builder.AddMinioContainer("s3")
    .WithOtlpExporter()
    .WithDataVolume();
//#endif

// https://aspire.dev/integrations/security/keycloak/
var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithDataVolume()
    .WithOtlpExporter()
    .WithRealmImport("./Infrastructure/Realms");

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
serverApiProject.WithReference(keycloak);
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
serverWebProject.WithReference(keycloak);
//#if (redis == true)
serverWebProject.WithReference(redisCache).WaitFor(redisCache);
serverWebProject.WithReference(redisPersistent).WaitFor(redisPersistent);
//#endif
//#endif

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

    //#if (api == "Standalone")
    builder.AddDevTunnel("api-dev-tunnel")
        .WithAnonymousAccess()
        .WithReference(serverApiProject.WithHttpEndpoint(name: "devTunnel", port: 5031).GetEndpoint("devTunnel"));
    //#endif

    var tunnel = builder.AddDevTunnel("web-dev-tunnel")
        .WithAnonymousAccess()
        .WithReference(serverWebProject.WithHttpEndpoint(name: "devTunnel", port: 5000).GetEndpoint("devTunnel"));

    if (OperatingSystem.IsWindows())
    {
        // Blazor Hybrid Windows project.
        builder.AddProject("clientwindows", "../../Client/Boilerplate.Client.Windows/Boilerplate.Client.Windows.csproj")
            .WithExplicitStart();
    }

    builder.AddMaui(serverWebProject, tunnel);
}

await builder
    .Build()
    .RunAsync();
