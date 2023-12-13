## bit entity framework core sqlite (bit Besql)

How to use `Bit.Besql`:

The usage of `Bit.Besql` is exactly the same as the regular usage of `Microsoft.EntityFrameworkCore.Sqlite` with [IDbContextFactory](https://learn.microsoft.com/en-us/aspnet/core/blazor/blazor-ef-core?view=aspnetcore-8.0#new-dbcontext-instances).

To get started, simply install `Bit.Besql` and use `services.AddSqliteDbContextFactory` instead of `services.AddDbContextFactory`.

Note: Don't use `IDbContextFactory` in `OnInitialized` because it relies on `IJSRuntime`. Use `OnAfterRender` instead.