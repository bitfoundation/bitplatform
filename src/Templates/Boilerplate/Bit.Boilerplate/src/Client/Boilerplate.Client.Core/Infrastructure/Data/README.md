## bit entity framework core sqlite [bit Besql](https://bitplatform.dev/besql)

How to use `Bit.Besql`:

The usage of `Bit.Besql` is exactly the same as the regular usage of `Microsoft.EntityFrameworkCore.Sqlite` with [IDbContextFactory](https://learn.microsoft.com/en-us/aspnet/core/blazor/blazor-ef-core?view=aspnetcore-10.0#new-dbcontext-instances).

In order to download sqlite db file from browser cache storage in blazor WebAssembly run the followings in browser console:
```js
const cache = await caches.open('bit-Besql');
const resp = await cache.match('/data/cache/App_Data/AppOffline.db');
const blob = await resp.blob();
const urlToDownload = URL.createObjectURL(blob);
const a = document.createElement('a');
a.href = urlToDownload;
a.download = 'AppOffline.db';
a.click();
URL.revokeObjectURL(urlToDownload);

https://inloop.github.io/sqlite-viewer/
```

**Migration**

`AppOfflineDbContext` migrations are slightly different from Boilerplate.Server.Api's `AppDbContext` migrations.
To add migration for `AppOfflineDbContext` first set `Boilerplate.Server.Web` as the Startup Project in solution explorer and set `Boilerplate.Client.Core` it as the Default Project in Package Manager Console and run the following commands:
```powershell
Add-Migration YourMigrationName -OutputDir Infrastructure\Data\Migrations -Context AppOfflineDbContext -Verbose
```
Or open a terminal in your Boilerplate.Server.Web project directory and run followings:
```bash
dotnet tool restore && dotnet ef migrations add YourMigrationName --context AppOfflineDbContext --output-dir Infrastructure/Data/Migrations --project ../../Client/Boilerplate.Client.Core/Boilerplate.Client.Core.csproj --verbose
```

*Note*: If you encounter any problem in running these commands, first make sure that the solution builds successfully.

*Note*: You may not run `Update-Database` command, because client app should programmatically create database and tables on every device that runs the app using `DbContext.Database.MigrateAsync()` code.

*Compiled Models (required before publishing):*

Outside of the Development environment the app **refuses to open the offline database** until a compiled model has been generated: the first `CreateDbContextAsync` throws `InvalidOperationException: AppOfflineDbContext has not been optimized`. So this is not an optional performance tweak - it is a release step, and it has to be **re-run after every model or migration change**, because EF Core does not detect a stale compiled model and will silently use it against a schema it no longer matches. That is also why no compiled model is checked into the template.

The generated model is discovered automatically (EF Core emits an `[assembly: DbContextModel(...)]` attribute for it), so no `UseModel(...)` call is needed. See [EF Core compiled models](https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics?tabs=with-di%2Cexpression-api-with-constant#compiled-models) for the technique and [dotnet ef dbcontext optimize](https://learn.microsoft.com/en-us/ef/core/cli/dotnet#dotnet-ef-dbcontext-optimize) for the command.

To generate it, follow these steps in the Package Manager Console:

1. Make sure `Server.Web` is set as the default startup project, and `Boilerplate.Client.Core` is the default project in the Package Manager Console.

2. Run the following command:

```powershell
Optimize-DbContext -Context AppOfflineDbContext -OutputDir Infrastructure/Data/CompiledModel -Namespace Boilerplate.Client.Core.Infrastructure.Data -Verbose
```

**OR** Run the following command in Boilerplate.Server.Web directory:
```bash
dotnet tool restore && dotnet ef dbcontext optimize --context AppOfflineDbContext --output-dir Infrastructure/Data/CompiledModel --namespace Boilerplate.Client.Core.Infrastructure.Data --project ../../Client/Boilerplate.Client.Core/Boilerplate.Client.Core.csproj --verbose
```

By adhering to these steps, you leverage EF Core compiled models to boost the performance of your application, ensuring an optimized and efficient data access method.