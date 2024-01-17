## bit Blazor Entity Framework Sqlite (bit Besql)

**Step by step walkthrough video:**

[![bit besql video](http://img.youtube.com/vi/ClpMKUboJmA/sd2.jpg)](http://www.youtube.com/watch?v=ClpMKUboJmA "bit besql video")

How to use `Bit.Besql`:

The usage of `Bit.Besql` is exactly the same as the regular usage of `Microsoft.EntityFrameworkCore.Sqlite` with [IDbContextFactory](https://learn.microsoft.com/en-us/aspnet/core/blazor/blazor-ef-core?view=aspnetcore-8.0#new-dbcontext-instances).

To get started, simply install `Bit.Besql` and use `services.AddBesqlDbContextFactory` instead of `services.AddDbContextFactory`.
Then add the following script:
```html
<script src="_content/Bit.Besql/bit-besql.js"></script>
```

Note: Do NOT use `IDbContextFactory` in `OnInitialized` method because it relies on `IJSRuntime`. Use `OnAfterRender` method instead.

In order to download sqlite db file from browser cache storage in blazor WebAssembly run the followings in browser console:
```js
const cache = await caches.open('bit-besql');
const resp = await cache.match('/data/cache/MyDb.db');
const blob = await resp.blob();
const urlToDownload = URL.createObjectURL(blob);
```
