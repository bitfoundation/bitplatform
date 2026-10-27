## bit entity framework core sqlite [bit Bsqlite](https://bsqlite.bitplatform.dev)

`AppOfflineDbContext` is a regular `Microsoft.EntityFrameworkCore.Sqlite` context, used through [IDbContextFactory](https://learn.microsoft.com/en-us/aspnet/core/blazor/blazor-ef-core?view=aspnetcore-10.0#new-dbcontext-instances). Where its database lives depends on where the app runs (See `IClientCoreServiceCollectionExtensions`):

- **Blazor WebAssembly:** `Bit.Bsqlite` runs SQLite in a dedicated worker and keeps `AppOffline.db` on OPFS, falling back to IndexedDB, then to memory when the browser has neither.
- **Blazor Hybrid (Android, iOS, Windows and macOS):** `Bit.Bsqlite` keeps `AppOffline.db` as a SQLite file in the app's own folder under local application data.
- **Blazor Server and pre-rendering:** the database is a plain SQLite file on the server, `App_Data/AppOffline.db` (`/container_volume/App_Data/AppOffline.db` in Docker), created by `AppOfflineDbContextFactory`.

Use the async EF Core API only (`ToListAsync`, `SaveChangesAsync`, ...): in the browser the database is in a worker the UI thread cannot wait for, so `Bit.Bsqlite` throws for the synchronous members, and does so in Blazor Hybrid too so that the same code runs everywhere.

In order to download the database from the browser in Blazor WebAssembly, export it with `BsqliteDatabase.ExportAsync()` (inject `BsqliteDatabase`), which returns a consistent SQLite file you can save with `FileSaveService`, then open it with a viewer such as https://inloop.github.io/sqlite-viewer/

**Migration**

`AppOfflineDbContext` migrations are slightly different from Boilerplate.Server.Api's `AppDbContext` migrations.
To add migration for `AppOfflineDbContext` first set `Boilerplate.Server.Web` as the Startup Project in solution explorer and set `Boilerplate.Client.Core` it as the Default Project in Package Manager Console and run the following commands:
```powershell
Add-Migration YourMigrationName -OutputDir Infrastructure\Data\Migrations -Context AppOfflineDbContext -Verbose
```
Or open a terminal in your Boilerplate.Server.Web project directory and run followings:
```bash
dnx dotnet-ef@10.0.14 -- migrations add YourMigrationName --context AppOfflineDbContext --output-dir Infrastructure/Data/Migrations --project ../../Client/Boilerplate.Client.Core/Boilerplate.Client.Core.csproj --verbose
```

*Note*: If you encounter any problem in running these commands, first make sure that the solution builds successfully.

*Note*: You may not run `Update-Database` command, because client app should programmatically create database and tables on every device that runs the app using `DbContext.Database.MigrateAsync()` code (See `AppOfflineDbContext.InitializeAsync`).

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
dnx dotnet-ef@10.0.14 -- dbcontext optimize --context AppOfflineDbContext --output-dir Infrastructure/Data/CompiledModel --namespace Boilerplate.Client.Core.Infrastructure.Data --project ../../Client/Boilerplate.Client.Core/Boilerplate.Client.Core.csproj --verbose
```

By adhering to these steps, you leverage EF Core compiled models to boost the performance of your application, ensuring an optimized and efficient data access method.