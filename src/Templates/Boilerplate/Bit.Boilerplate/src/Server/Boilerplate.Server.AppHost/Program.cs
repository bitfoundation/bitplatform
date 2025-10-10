//+:cnd:noEmit
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Check out appsettings.Development.json for credentials/passwords settings.

//#if (database == "SqlServer")
var sqlDatabase = builder.AddSqlServer("sqlserver")
        .WithDbGate(config => config.WithLifetime(ContainerLifetime.Persistent).WithDataVolume())
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume()
        .WithImage("mssql/server", "2025-latest")
        .AddDatabase("sqldb"); // Sql server 2025 supports embedded vector search.

//#elif (database == "PostgreSql")
var postgresDatabase = builder.AddPostgres("postgresserver")
        .WithPgAdmin(config => config.WithLifetime(ContainerLifetime.Persistent).WithVolume("/var/lib/pgadmin/Boilerplate/data"))
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume()
        .WithImage("pgvector/pgvector", "pg18") // pgvector supports embedded vector search.
        .AddDatabase("postgresdb");

//#elif (database == "MySql")
var mySqlDatabase = builder.AddMySql("mysqlserver")
        .WithPhpMyAdmin(config => config.WithLifetime(ContainerLifetime.Persistent).WithVolume("/var/lib/phpMyAdmin/Boilerplate/data"))
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume()
        .AddDatabase("mysqldb");
//#elif (database == "Sqlite")
var sqlite = builder.AddSqlite("sqlite", databaseFileName: "BoilerplateDb.db")
    .WithSqliteWeb(config => config.WithLifetime(ContainerLifetime.Persistent).WithVolume("/var/lib/sqliteweb/Boilerplate/data"));
//#endif
//#if (filesStorage == "AzureBlobStorage")
var azureBlobStorage = builder.AddAzureStorage("storage")
        .RunAsEmulator(azurite =>
        {
            azurite
                .WithLifetime(ContainerLifetime.Persistent)
                .WithDataVolume();
        })
        .AddBlobs("blobs");

//#elif (filesStorage == "S3")
var s3Storage = builder.AddMinioContainer("minio")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();
//#endif

var keycloak = builder.AddKeycloak("keycloak")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var mailpit = builder.AddMailPit("mailpit") // For testing purposes only, in production, you would use a real SMTP server.
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("mailpit");

//#if (signalR == true || database == "PostgreSQL" || database == "SqlServer")
builder.AddGitHubModel("llm", "openai/gpt-4o-mini"); // You can either replace `Aspire.Hosting.GitHub.Models` nuget package with `Aspire.Hosting.Azure.AIFoundry`
// OR you can remove `Aspire.Hosting.GitHub.Models` nuget package and simply set any OpenAI compatible endpoint in your Server.Api's app settings.
//#endif

var serverWebProject = builder.AddProject<Projects.Boilerplate_Server_Web>("serverweb") // Replace . with _ if needed to ensure the project builds successfully.
    .WithExternalHttpEndpoints();

// Adding health checks endpoints to applications in non-development environments has security implications.
// See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
if (builder.Environment.IsDevelopment())
{
    serverWebProject.WithHttpHealthCheck("/alive");
}

//#if (api == "Standalone")
var serverApiProject = builder.AddProject<Projects.Boilerplate_Server_Api>("serverapi") // Replace . with _ if needed to ensure the project builds successfully.
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
//#elif (database == "PostgreSql")
serverApiProject.WithReference(postgresDatabase).WaitFor(postgresDatabase);
//#elif (database == "MySql")
serverApiProject.WithReference(mySqlDatabase).WaitFor(mySqlDatabase);
//#elif (database == "Sqlite")
serverApiProject.WithReference(sqlite).WaitFor(sqlite);
//#endif
//#if (filesStorage == "AzureBlobStorage")
serverApiProject.WithReference(azureBlobStorage, "azureblobstorage");
//#elif (filesStorage == "S3")
serverApiProject.WithReference(s3Storage, "s3");
//#endif
serverApiProject.WithReference(mailpit, "smtp");
serverApiProject.WithReference(keycloak);
//#else

//#if (database == "SqlServer")
serverWebProject.WithReference(sqlDatabase).WaitFor(sqlDatabase);
//#elif (database == "PostgreSql")
serverWebProject.WithReference(postgresDatabase).WaitFor(postgresDatabase);
//#elif (database == "MySql")
serverWebProject.WithReference(mySqlDatabase).WaitFor(mySqlDatabase);
//#elif (database == "Sqlite")
serverWebProject.WithReference(sqlite).WaitFor(sqlite);
//#endif
//#if (filesStorage == "AzureBlobStorage")
serverWebProject.WithReference(azureBlobStorage, "azureblobstorage");
//#elif (filesStorage == "S3")
serverWebProject.WithReference(s3Storage, "s3");
//#endif
serverWebProject.WithReference(mailpit, "smtp");
serverWebProject.WithReference(keycloak);
//#endif

// Blazor WebAssembly Standalone project.
builder.AddProject<Projects.Boilerplate_Client_Web>("clientwebwasm"); // Replace . with _ if needed to ensure the project builds successfully.

if (builder.ExecutionContext.IsRunMode) // The following project is only added for testing purposes.
{
    // Blazor Hybrid Windows project.
    builder.AddProject<Projects.Boilerplate_Client_Windows>("clientwindows") // Replace . with _ if needed to ensure the project builds successfully.
        .WithExplicitStart();

    //#if (api == "Standalone")
    builder.AddDevTunnel("api-dev-tunnel")
        .WithAnonymousAccess()
        .WithReference(serverApiProject.WithHttpEndpoint(name: "devTunnel").GetEndpoint("devTunnel"));
    //#endif

    var tunnel = builder.AddDevTunnel("web-dev-tunnel")
        .WithAnonymousAccess()
        .WithReference(serverWebProject.WithHttpEndpoint(name: "devTunnel").GetEndpoint("devTunnel"));
}

await builder
    .Build()
    .RunAsync();
