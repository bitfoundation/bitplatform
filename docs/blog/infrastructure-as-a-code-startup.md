## **Infrastructure as a Code** Startup Solution

`Infrastructure as code` is the process of defining Infrastructure as a series of files; Instead of managing Infra with interactive tools like Portals.

The advantage of this method is that if we have different stages such as Development, QA, Sandbox, Production, etc., first in some files, we write the required Infra structure and automatically build Development from it, and then if we get the answer, we make QA and other stages from the same, and from now on, any change in Infra is first tested in Development, and if it gets the answer, it goes to QA and then Production.

This method, due to its automation, reduces the possibility of errors and, depending on the implementation method, can be very similar to Migrations in EntityFramework, as migrations are created and applied to the Development database, and only if the tests are passed, the changes can be sent automatically to the QA, etc, and it is not possible to forget anything in between.

One of the best tools for `Infra as a code` is a tool called **[Pulumi](https://www.pulumi.com/)**, which is compatible with Kubernetes, Azure, AWS, and Google Cloud.

**Kubernetes**, for example, has its own methods for maintaining the Infra structure in its configuration files, but Pulumi offers both simplicity and ease in the Cloud, where you typically use your own Cloud Database Services and App Services and Logging Systems which are not Kubernetes subsets. You can control the whole Cloud and Kubernetes at the same time with one tool.

For example, most people in the Cloud, use `Database as a service` instead of creating a database in Kubernetes, which means achieving higher quality and reducing costs. Some other examples are requesting DDoS protection service and CDN and Media Services.

To work with Pulumi, you can get an account from its website, in which case snapshots of Infra changes in the code are stored inside the Pulumi website, or you can keep Snapshots similar to the Snapshots of the Entity Framework inside the source controller, therefore you will not have any dependency on Pulumi servers.

After installing Pulumi CLI, you can create a project with one of the programming languages Go, C#, JavaScript or Python and then create your own resources inside it and create Firewall settings and ...


Then with the `Pulumi up` command, your changes will be applied to Development or any other stage you have selected. Finally, if Infra needs to be changed, first you change the project file and then go through the other necessary procedures within the team, including Code Review, etc., and then run `Pulumi up` again.

The following is an example code, from the launch of App Service, SQL Server, Blob Storage, and Application Insights.

The App Service built to run our Backend will have both a Connection String connected to the database and a Connection String related to Blob Storage to store its files in it, and finally, Application Insights will be responsible for Monitoring.

   ```csharp
var sqlDatabasePassword = pulumiConfig.RequireSecret("sql-server-nikola-dev-password");
var sqlDatabaseUserId = pulumiConfig.RequireSecret("sql-server-nikola-dev-user-id");

var resourceGroup = new ResourceGroup("rg-dds-nikola-dev", new ResourceGroupArgs
{
    Name = "rg-dds-nikola-dev",
    Location = "WestUS"
});

var storageAccount = new Account("storagenikoladev", new AccountArgs
{
    Name = "storagenikoladev",
    ResourceGroupName = resourceGroup.Name,
    Location = resourceGroup.Location,
    AccountKind = "StorageV2",
    AccountReplicationType = "LRS",
    AccountTier = "Standard",
});

var container = new Container("container-nikola-dev", new ContainerArgs
{
    Name = "container-nikola-dev",
    ContainerAccessType = "blob",
    StorageAccountName = storageAccount.Name
});

var blobStorage = new Blob("blob-nikola-dev", new BlobArgs
{
    Name = "blob-nikola-dev",
    StorageAccountName = storageAccount.Name,
    StorageContainerName = container.Name,
    Type = "Block"
});

var appInsights = new Insights("app-insights-nikola-dev", new InsightsArgs
{
    Name = "app-insights-nikola-dev",
    ResourceGroupName = resourceGroup.Name,
    Location = resourceGroup.Location,
    ApplicationType = "web" // also general for mobile apps
});

var sqlServer = new SqlServer("sql-server-nikola-dev", new SqlServerArgs
{
    Name = "sql-server-nikola-dev",
    ResourceGroupName = resourceGroup.Name,
    Location = resourceGroup.Location,
    AdministratorLogin = sqlDatabaseUserId,
    AdministratorLoginPassword = sqlDatabasePassword,
    Version = "12.0"
});

var sqlDatabase = new Database("sql-database-nikola-dev", new DatabaseArgs
{
    Name = "sql-database-nikola-dev",
    ResourceGroupName = resourceGroup.Name,
    Location = resourceGroup.Location,
    ServerName = sqlServer.Name,
    RequestedServiceObjectiveName = "Basic"
});

var appServicePlan = new Plan("app-plan-nikola-dev", new PlanArgs
{
    Name = "app-plan-nikola-dev",
    ResourceGroupName = resourceGroup.Name,
    Location = resourceGroup.Location,
    Sku = new PlanSkuArgs
    {
        Tier = "Shared",
        Size = "D1"
    }
});

var appService = new AppService("app-service-nikola-dev", new AppServiceArgs
{
    Name = "app-service-nikola-dev",
    ResourceGroupName = resourceGroup.Name,
    Location = resourceGroup.Location,
    AppServicePlanId = appServicePlan.Id,
    SiteConfig = new AppServiceSiteConfigArgs
    {
        Use32BitWorkerProcess = true, // X64 not allowed in shared plan!
        AlwaysOn = false, // not allowed in shared plan!
        Http2Enabled = true
    },
    AppSettings =
    {
        { "ApplicationInsights:InstrumentationKey", appInsights.InstrumentationKey },
        { "APPINSIGHTS_INSTRUMENTATIONKEY", appInsights.InstrumentationKey }
    },
    ConnectionStrings = new InputList<AppServiceConnectionStringArgs>()
    {
        new AppServiceConnectionStringArgs
        {
            Name = "AppDbConnectionString",
            Type = "SQLAzure",
            Value = Output.Tuple(sqlServer.Name, sqlDatabase.Name, sqlDatabaseUserId, sqlDatabasePassword).Apply(t =>
            {
                (string _sqlServer, string _sqlDatabase, string _sqlDatabaseUserId, string _sqlDatabasePassword) = t;
                return $"Data Source=tcp:{_sqlServer}.database.windows.net;Initial Catalog={_sqlDatabase};User ID={_sqlDatabaseUserId};Password={_sqlDatabasePassword};Max Pool Size=1024;Persist Security Info=true;Application Name=Nikola";
            })
        },
        new AppServiceConnectionStringArgs
        {
            Name = "AzureBlobStorageConnectionString",
            Type = "Custom",
            Value = Output.Tuple(storageAccount.PrimaryAccessKey, storageAccount.Name).Apply(t =>
            {
                (string _primaryAccess, string _storageAccountName) = t;
                return $"DefaultEndpointsProtocol=https;AccountName={_storageAccountName};AccountKey={_primaryAccess};EndpointSuffix=core.windows.net";
            })
        }
    }
});

appService.OutboundIpAddresses.Apply(ips =>
{
    foreach (string ip in ips.Split(','))
    {
        new FirewallRule($"app-srv-{ip}", new FirewallRuleArgs
        {
            Name = $"app-srv-{ip}",
            EndIpAddress = ip,
            ResourceGroupName = resourceGroup.Name,
            ServerName = sqlServer.Name,
            StartIpAddress = ip
        });
    }

    return (string?)null;
});
```

Now assume we start using Redis in the new Sprint. This will change the file above and a Redis will be added to it. The above file is a single source of truth, and by looking at it we can actually understand what the structure of our Infra is (exactly like the model in the Entity Framework Core).

Then, when changes are to be made to QA, the CI/CD routine can automatically first change its own Infra (i.e. QA) to add Redis, then publish the project and execute Database migrations. If the whole process finished successfully, all of these procedures will be applied automatically when publishing to Production without any manual work, which in turn will create a fundamental improvement in the project's DevOps routine.
