//+:cnd:noEmit
using Projects;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

//#if (database == "SqlServer")
var sqlDatabase = builder.AddSqlServer("sqlserver")
        .WithLifetime(ContainerLifetime.Persistent)
        .WithVolume("/var/lib/sql-server/Boilerplate/data")
        .WithImage("mssql/server", "2025-latest")
        .AddDatabase("sqldb"); // Sql server 2025 supports embedded vector search.

//#elif (database == "PostgreSql")
var postgresDatabase = builder.AddPostgres("postgresserver")
        .WithPgAdmin(config => config.WithLifetime(ContainerLifetime.Persistent).WithVolume("/var/lib/pgadmin/Boilerplate/data"))
        .WithLifetime(ContainerLifetime.Persistent)
        .WithVolume("/var/lib/postgres-server/Boilerplate/data")
        .WithImage("pgvector/pgvector", "pg17") // pgvector supports embedded vector search.
        .AddDatabase("postgresdb");

//#elif (database == "MySql")

var mySqlDatabase = builder.AddMySql("mysqlserver")
        .WithPhpMyAdmin(config => config.WithLifetime(ContainerLifetime.Persistent).WithVolume("/var/lib/phpMyAdmin/Boilerplate/data"))
        .WithLifetime(ContainerLifetime.Persistent)
        .WithVolume("/var/lib/mySql-server/Boilerplate/data")
        .AddDatabase("mysqldb");

//#endif
//#if (filesStorage == "AzureBlobStorage")
var azureBlobStorage = builder.AddAzureStorage("storage")
        .RunAsEmulator(azurite =>
        {
            azurite
                .WithLifetime(ContainerLifetime.Persistent)
                .WithDataVolume("BoilerplateStorage");
        })
        .AddBlobs("blobs");

//#elif (filesStorage == "S3")
var username = builder.AddParameter("user", "minioadmin");
var password = builder.AddParameter("password", "minioadmin", secret: true);
var s3Storage = builder.AddMinioContainer("minio", rootUser: username, rootPassword: password);
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
serverApiProject.WithReference(s3Storage, "MinIOS3ConnectionString").WaitFor(s3Storage);
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
serverWebProject.WithReference(s3Storage, "MinIOS3ConnectionString").WaitFor(s3Storage);
//#endif

//#endif

builder.AddAspireDashboard();

await builder
    .Build()
    .RunAsync();
