# Bit.BevTools.Loader

Opens [bit BevTools](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/BevTools) on demand, so a
Blazor WebAssembly app can lazy load `Bit.BevTools.wasm` and nobody downloads it until they ask for it. It is small,
trimmable and AOT compatible, and it is the only BevTools assembly the app's startup code touches.

```xml
<!-- the shared project: reference both -->
<PackageReference Include="Bit.BevTools" />
<PackageReference Include="Bit.BevTools.Loader" />

<!-- the Blazor WebAssembly project -->
<BlazorWebAssemblyLazyLoad Include="Bit.BevTools.wasm" />
```

```csharp
builder.Services.AddBitBevToolsLoaderServices();
```

```razor
@* MainLayout.razor *@
<BevToolsLoaderPanel />

@* anywhere, e.g. a diagnostics page *@
@inject BevToolsLoader BevToolsLoader
@if (BevToolsLoader.CanOpen)
{
    <button @onclick="BevToolsLoader.OpenAsync">Open dev tools</button>
}
```

When `Bit.BevTools` is among the assemblies the app starts with and should run from the first render (Development, or
`RegisterAtStartup = true`), the loader registers it exactly as `AddBitBevToolsServices` does. Otherwise it installs
hooks that cost next to nothing until BevTools is attached: a pass-through component activator, pass-through handlers in
`IHttpClientFactory` pipelines, the framework's ActivitySource and meters, and the service collection for the Services
panel. `OpenAsync` then loads BevTools, attaches it to the running app and opens it. Add `BevToolsHttpHookHandler` to a
hand-built `HttpClient` pipeline to have its requests recorded too.

An attached BevTools sees what happens after the attach. In a process that serves many users (Blazor Server,
prerendering) `CanOpen` is false outside Development unless `AllowInMultiUserHost` is set, because the panel would show
whoever opens it the server side objects of their circuit.
