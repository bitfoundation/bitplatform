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
        .WithLifetime(ContainerLifetime.Persistent)
        .WithVolume("/var/lib/postgres-server/Boilerplate/data")
        .WithImage("pgvector/pgvector", "pg17") // pgvector supports embedded vector search.
        .AddDatabase("postgresdb");

//#elif (database == "MySql")

var mySqlDatabase = builder.AddMySql("mysqlserver")
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
                .WithDataVolume("BoilerplateStorage")
                .WithBlobPort(10000)
                .WithQueuePort(10001)
                .WithTablePort(10002);
        })
        .AddBlobs("blobs");

//#elif (filesStorage == "S3")
// minio docker instance for testing purposes.
var s3Storage = builder.AddContainer("minio", "minio/minio", "latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithArgs("server", "/data", "--console-address", ":9001") // Add MinIO server command
    .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
    .WithEndpoint(port: 9000, targetPort: 9000, "api")
    .WithEndpoint(port: 9001, targetPort: 9001, "console") // http://127.0.0.1:9001/browser
    .WithVolume("/var/lib/minio/Boilerplate/data");
//#endif

var serverWebProject = builder.AddProject<Boilerplate_Server_Web>("serverweb")
    .WithExternalHttpEndpoints(); // Replace . with _ if needed to ensure the project builds successfully.

//#if (api == "Standalone")
var serverApiProject = builder.AddProject<Boilerplate_Server_Api>("serverapi")
    .WithExternalHttpEndpoints(); // Replace . with _ if needed to ensure the project builds successfully.

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
serverApiProject
    .WithEnvironment("ConnectionStrings__MinIOS3ConnectionString", "minio.s3://keyId=minioadmin;key=minioadmin;serviceUrl=http://localhost:9000;bucket=attachments")
    .WaitFor(s3Storage);
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
serverWebProject
    .WithEnvironment("ConnectionStrings__MinIOS3ConnectionString", "minio.s3://keyId=minioadmin;key=minioadmin;serviceUrl=http://localhost:9000;bucket=attachments")
    .WaitFor(s3Storage);
//#endif

//#endif

await builder
    .Build()
    .RunAsync();
