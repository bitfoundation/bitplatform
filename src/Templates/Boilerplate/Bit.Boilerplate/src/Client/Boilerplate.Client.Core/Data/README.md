## bit entity framework core sqlite (bit Besql)

How to use `Bit.Besql`:

The usage of `Bit.Besql` is exactly the same as the regular usage of `Microsoft.EntityFrameworkCore.Sqlite` with [IDbContextFactory](https://learn.microsoft.com/en-us/aspnet/core/blazor/blazor-ef-core?view=aspnetcore-8.0#new-dbcontext-instances).

In order to download sqlite db file from browser cache storage in blazor WebAssembly run the followings in browser console:
```js
const cache = await caches.open('Bit-Besql');
const resp = await cache.match('/data/cache/Boilerplate-ClientDb.db');
const blob = await resp.blob();
URL.createObjectURL(blob);
```

**Migration**

Set `Server.Web` as the Startup Project in solution explorer and set `Client.Core` it as the Default Project in Package Manager Console and run the following commands:
```powershell
Add-Migration InitialMigration -OutputDir Data\Migrations -Context OfflineDbContext -Verbose
```
Or open a terminal in your Server.Web project directory and run followings:
```bash
dotnet ef migrations add InitialMigration --context OfflineDbContext --output-dir Data/Migrations --project ../Client/Boilerplate.Client.Core/Boilerplate.Client.Core.csproj --verbose
```

*Note*: If you encounter any problem in running these commands, first make sure that the solution builds successfully.

*Note*: You may not run `Update-Database` command, because client app should programmatically create database and tables on every device that runs the app using `DbContext.Database.MigrateAsync()` code.

*Optimizing EF Core Performance with Compiled Models:*

To enhance the performance of your models, consider compiling them using EF Core compiled models. Detailed information on this advanced optimization technique can be found [here](https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics?tabs=with-di%2Cexpression-api-with-constant#compiled-models) and [here](https://learn.microsoft.com/en-us/ef/core/cli/dotnet#dotnet-ef-dbcontext-optimize).

To implement this optimization, follow these steps in the Package Manager Console:

1. Make sure `Server.Web` is set as the default startup project, and `Client.Core` is the default project in the Package Manager Console.

2. Run the following command:

    ```powershell
    Optimize-DbContext -Context OfflineDbContext -OutputDir Data/CompiledModel -Namespace Boilerplate.Client.Core.Data
    ```

By adhering to these steps, you leverage EF Core compiled models to boost the performance of your application, ensuring an optimized and efficient data access method.