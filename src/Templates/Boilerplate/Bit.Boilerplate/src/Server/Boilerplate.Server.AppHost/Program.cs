//+:cnd:noEmit
using Projects;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

// Check out appsettings.json for credential settings.

//#if (database == "SqlServer")
var sqlServerPassword = builder.AddParameter("SqlServerPassword", secret: true);
var sqlDatabase = builder.AddSqlServer("sqlserver", password: sqlServerPassword)
        .WithDbGate(config => config.WithLifetime(ContainerLifetime.Persistent).WithDataVolume())
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume()
        .WithImage("mssql/server", "2025-latest")
        .AddDatabase("sqldb"); // Sql server 2025 supports embedded vector search.

//#elif (database == "PostgreSql")
var postgresPassword = builder.AddParameter("PostgresPassword", secret: true);
var postgresDatabase = builder.AddPostgres("postgresserver", password: postgresPassword)
        .WithPgAdmin(config => config.WithLifetime(ContainerLifetime.Persistent).WithVolume("/var/lib/pgadmin/Boilerplate/data"))
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume()
        .WithImage("pgvector/pgvector", "pg17") // pgvector supports embedded vector search.
        .AddDatabase("postgresdb");

//#elif (database == "MySql")
var mySqlPassword = builder.AddParameter("MySqlPassword", secret: true);
var mySqlDatabase = builder.AddMySql("mysqlserver", password: mySqlPassword)
        .WithPhpMyAdmin(config => config.WithLifetime(ContainerLifetime.Persistent).WithVolume("/var/lib/phpMyAdmin/Boilerplate/data"))
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume()
        .AddDatabase("mysqldb");

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
var minioUsername = builder.AddParameter("MinIOUser");
var minioPassword = builder.AddParameter("MinIOPassword", secret: true);
var s3Storage = builder.AddMinioContainer("minio", rootUser: minioUsername, rootPassword: minioPassword);
//#endif

var serverWebProject = builder.AddProject<Boilerplate_Server_Web>("serverweb") // Replace . with _ if needed to ensure the project builds successfully.
    .WithExternalHttpEndpoints();

//#if (api == "Standalone")
var serverApiProject = builder.AddProject<Boilerplate_Server_Api>("serverapi") // Replace . with _ if needed to ensure the project builds successfully.
    .WithExternalHttpEndpoints();

serverWebProject.WithReference(serverApiProject).WaitFor(serverApiProject);
//#if (database == "SqlServer")
serverApiProject.WithReference(sqlDatabase, "SqlServerConnectionString").WaitFor(sqlDatabase);
//#elif (database == "PostgreSql")
serverApiProject.WithReference(postgresDatabase, "PostgreSQLConnectionString").WaitFor(postgresDatabase);
//#elif (database == "MySql")
serverApiProject.WithReference(mySqlDatabase, "MySqlConnectionString").WaitFor(mySqlDatabase);
//#endif
//#if (filesStorage == "AzureBlobStorage")
serverApiProject.WithReference(azureBlobStorage, "AzureBlobStorageConnectionString").WaitFor(azureBlobStorage);
//#elif (filesStorage == "S3")
serverApiProject.WithReference(s3Storage, "S3ConnectionString").WaitFor(s3Storage);
//#endif

//#else

//#if (database == "SqlServer")
serverWebProject.WithReference(sqlDatabase, "SqlServerConnectionString").WaitFor(sqlDatabase);
//#elif (database == "PostgreSql")
serverWebProject.WithReference(postgresDatabase, "PostgreSQLConnectionString").WaitFor(postgresDatabase);
//#elif (database == "MySql")
serverWebProject.WithReference(mySqlDatabase, "MySqlConnectionString").WaitFor(mySqlDatabase);
//#endif
//#if (filesStorage == "AzureBlobStorage")
serverWebProject.WithReference(azureBlobStorage, "AzureBlobStorageConnectionString").WaitFor(azureBlobStorage);
//#elif (filesStorage == "S3")
serverWebProject.WithReference(s3Storage, "S3ConnectionString").WaitFor(s3Storage);
//#endif

//#endif

// Blazor WebAssembly Standalone project.
builder.AddProject<Boilerplate_Client_Web>("clientwebwasm"); // Replace . with _ if needed to ensure the project builds successfully.

if (builder.ExecutionContext.IsRunMode) // The following project is only added for testing purposes.
{
    // Blazor Hybrid Windows project.
    builder.AddProject<Boilerplate_Client_Windows>("clientwindows") // Replace . with _ if needed to ensure the project builds successfully.
        .WithExplicitStart();
}

builder.AddAspireDashboard();

await builder
    .Build()
    .RunAsync();
