//+:cnd:noEmit
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Check out appsettings.Development.json for credentials/passwords settings.

//#if (database == "SqlServer")
var sqlDatabase = builder.AddSqlServer("sqlserver", port: 1759)
        .WithDbGate(config => config.WithDataVolume())
        .WithDataVolume()
        .WithImage("mssql/server", "2025-latest")
        .AddDatabase("mssqldb"); // Sql server 2025 supports embedded vector search.

//#elif (database == "PostgreSql")
var postgresDatabase = builder.AddPostgres("postgresserver")
        .WithPgAdmin(config => config.WithVolume("/var/lib/pgadmin/Boilerplate/data"))
        .WithDataVolume()
        .WithImage("pgvector/pgvector", "pg18") // pgvector supports embedded vector search.
        .AddDatabase("postgresdb");

//#elif (database == "MySql")
var mySqlDatabase = builder.AddMySql("mysqlserver")
        .WithPhpMyAdmin(config => config.WithVolume("/var/lib/phpMyAdmin/Boilerplate/data"))
        .WithDataVolume()
        .AddDatabase("mysqldb");
//#elif (database == "Sqlite")
var sqlite = builder.AddSqlite("sqlite", databaseFileName: "BoilerplateDb.db")
    .WithSqliteWeb(config => config.WithVolume("/var/lib/sqliteweb/Boilerplate/data"));
//#endif
//#if (filesStorage == "AzureBlobStorage")
var azureBlobStorage = builder.AddAzureStorage("storage")
        .RunAsEmulator(azurite =>
        {
            azurite
                .WithDataVolume();
        })
        .AddBlobs("azureblobstorage");

//#elif (filesStorage == "S3")
var s3Storage = builder.AddMinioContainer("s3")
    .WithDataVolume();
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
//#elif (database == "PostgreSql")
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
serverWebProject.WithReference(azureBlobStorage);
//#elif (filesStorage == "S3")
serverWebProject.WithReference(s3Storage);
//#endif
//#endif

if (builder.ExecutionContext.IsRunMode) // The following project is only added for testing purposes.
{
    // Blazor WebAssembly Standalone project.
    builder.AddProject("clientwebwasm", "../../Client/Boilerplate.Client.Web/Boilerplate.Client.Web.csproj")
        .WithExplicitStart();

    var mailpit = builder.AddMailPit("smtp") // For testing purposes only, in production, you would use a real SMTP server.
        .WithDataVolume("mailpit");

    //#if (api == "Standalone")
    serverApiProject.WithReference(mailpit);
    //#else
    serverWebProject.WithReference(mailpit);
    //#endif

    // Blazor Hybrid Windows project.
    builder.AddProject("clientwindows", "../../Client/Boilerplate.Client.Windows/Boilerplate.Client.Windows.csproj")
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
