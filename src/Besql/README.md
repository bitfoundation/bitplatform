## bit entity framework core sqlite (bit Besql)

How to use `Bit.Besql`:

The usage of `Bit.Besql` is exactly the same as the regular usage of `Microsoft.EntityFrameworkCore.Sqlite` with [IDbContextFactory](https://learn.microsoft.com/en-us/aspnet/core/blazor/blazor-ef-core?view=aspnetcore-8.0#new-dbcontext-instances).

To get start, simply install `Bit.Besql` and use `services.AddBesqlDbContextFactory` instead of `services.AddDbContextFactory`.
Then add the following script:
```html
<script src="_content/Bit.Besql/browserCache.js"></script>
```

Note: Don't use `IDbContextFactory` in `OnInitialized` because it relies on `IJSRuntime`. Use `OnAfterRender` instead.

In order to download sqlite db file from browser cache storage in blazor WebAssembly run the followings in browser console:
```js
const cache = await caches.open('Bit-Besql');
const resp = await cache.match('/data/cache/Boilerplate-ClientDb.db');
const blob = await resp.blob();
URL.createObjectURL(blob);
```