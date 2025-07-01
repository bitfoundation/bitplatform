//+:cnd:noEmit
using Projects;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

//#if (database == "SqlServer")
var sqlDatabase = builder.AddSqlServer("sql-server")
        .WithLifetime(ContainerLifetime.Persistent)
        .WithVolume("/var/lib/sql-server/Boilerplate/data")
        .WithImage("mssql/server", "2025-latest")
        .AddDatabase("SqlServerDb"); // Sql server 2025 supports embedded vector search.

//#elif (database == "PostgreSql")
var postgresDatabase = builder.AddPostgres("postgres-server")
        .WithLifetime(ContainerLifetime.Persistent)
        .WithVolume("/var/lib/postgres-server/Boilerplate/data")
        .WithImage("pgvector/pgvector", "pg17") // pgvector supports embedded vector search.
        .AddDatabase("PostgreSQLDb");

//#elif (database == "MySql")

var mySqlDatabase = builder.AddMySql("mySql-server")
        .WithLifetime(ContainerLifetime.Persistent)
        .WithVolume("/var/lib/mySql-server/Boilerplate/data")
        .AddDatabase("MySqlDb");

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

//#endif

var serverWebProject = builder.AddProject<Boilerplate_Server_Web>("server-web"); // Replace . with _ if needed to ensure the project builds successfully.

//#if (api == "Standalone")
var serverApiProject = builder.AddProject<Boilerplate_Server_Api>("server-api"); // Replace . with _ if needed to ensure the project builds successfully.

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
//#endif

//#endif

await builder
    .Build()
    .RunAsync();
