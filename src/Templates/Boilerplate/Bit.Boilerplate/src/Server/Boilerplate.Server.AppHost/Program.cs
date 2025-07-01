//+:cnd:noEmit
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

//#if (database == "SqlServer")
var sqlServer = builder.AddSqlServer("sql-server")
                 .WithLifetime(ContainerLifetime.Persistent)
                 .WithVolume("/var/lib/sql-server/Boilerplate/data")
                 .WithImage("mssql/server", "2025-latest"); // Sql server 2025 supports embedded vector search.

var sqlDatabase = sqlServer.AddDatabase("BoilerplateSqlServerDb");
//#elif (database == "PostgreSql")
var postgresServer = builder.AddPostgres("postgres-server")
                   .WithLifetime(ContainerLifetime.Persistent)
                   .WithVolume("/var/lib/postgres-server/Boilerplate/data")
                   .WithImage("pgvector/pgvector", "pg17"); // pgvector supports embedded vector search.

var postgresDatabase = postgresServer.AddDatabase("BoilerplatePostgreSQLDb");
//#elif (database == "MySql")
var mysqlServer = builder.AddMySql("mysql-server")
                   .WithLifetime(ContainerLifetime.Persistent)
                   .WithVolume("/var/lib/mysql-server/Boilerplate/data");

var mysqlDatabase = mysqlServer.AddDatabase("BoilerplateMySqlDb");
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

var serverWebProject = builder.AddProject<Projects.Boilerplate_Server_Web>("boilerplate-server-web");

//#if (api == "Standalone")
var serverApiProject = builder.AddProject<Projects.Boilerplate_Server_Api>("boilerplate-server-api");
serverWebProject.WithReference(serverApiProject).WaitFor(serverApiProject);
//#if (database == "SqlServer")
serverApiProject.WithReference(sqlDatabase, "SqlServerConnectionString").WaitFor(sqlDatabase);
//#elif (database == "PostgreSql")
serverApiProject.WithReference(postgresDatabase, "PostgreSQLConnectionString").WaitFor(postgresDatabase);
//#elif (database == "MySql")
serverApiProject.WithReference(mysqlDatabase, "MySqlConnectionString").WaitFor(mysqlDatabase);
//#endif
//#if (filesStorage == "AzureBlobStorage")
serverApiProject.WithReference(azureBlobStorage).WaitFor(azureBlobStorage);
//#endif

//#else

//#if (database == "SqlServer")
serverWebProject.WithReference(sqlDatabase, "SqlServerConnectionString").WaitFor(sqlDatabase);
//#elif (database == "PostgreSql")
serverWebProject.WithReference(postgresDatabase, "PostgreSQLConnectionString").WaitFor(postgresDatabase);
//#elif (database == "MySql")
serverWebProject.WithReference(mysqlDatabase, "MySqlConnectionString").WaitFor(mysqlDatabase);
//#endif
//#if (filesStorage == "AzureBlobStorage")
serverWebProject.WithReference(azureBlobStorage, "AzureBlobStorageSasUrl").WaitFor(azureBlobStorage);
//#endif

//#endif

await builder
    .Build()
    .RunAsync();
