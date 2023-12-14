## bit entity framework core sqlite (bit Besql)

How to use `Bit.Besql`:

The usage of `Bit.Besql` is exactly the same as the regular usage of `Microsoft.EntityFrameworkCore.Sqlite` with [IDbContextFactory](https://learn.microsoft.com/en-us/aspnet/core/blazor/blazor-ef-core?view=aspnetcore-8.0#new-dbcontext-instances).

To get start, simply install `Bit.Besql` and use `services.AddBesqlDbContextFactory` instead of `services.AddDbContextFactory`.

Note: Don't use `IDbContextFactory` in `OnInitialized` because it relies on `IJSRuntime`. Use `OnAfterRender` instead.

In order to download sqlite db file from browser cache storage in blazor WebAssembly run the followings in browser console:
```js
const cache = await caches.open('Bit-Besql');
const resp = await cache.match('/data/cache/Boilerplate-ClientDb.db');
const blob = await resp.blob();
URL.createObjectURL(blob);
```

**Migration**

Set `Server` as the Startup Project in solution explorer and set `Client.Core` it as the Default Project in Package Manager Console and run the following commands:
```powershell
Add-Migration InitialMigration -OutputDir Data\Migrations -Context OfflineDbContext
```
Or open a terminal in your Server project directory and run followings:
```bash
dotnet ef migrations add InitialMigration --context OfflineDbContext --output-dir Data/Migrations --project ../Client/Boilerplate.Client.Core/Boilerplate.Client.Core.csproj
```

*Note*: If you encounter any problem in running these commands, first make sure that the solution builds successfully.

*Note*: You may not run `Update-Database` command, because client app should programmatically create database and tables on every device that runs the app using `DbContext.Database.MigrateAsync()` code.