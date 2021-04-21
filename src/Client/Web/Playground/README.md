# Bit.Client.Web.BlazorUI.Playground
Blazor dual mode with best practices in mind!

In Directory.build.props, you can switch between blazor server and client by using either

```xml
<BlazorMode>Client</BlazorMode>
```

```xml
<BlazorMode>Server</BlazorMode>
```

In Bit.Client.Web.BlazorUI.Playground.Web, use

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
```

for client side blazor and use

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
```

for server side blazor.

Note that for Client mode, set Bit.Client.Web.BlazorUI.Playground.Api.csproj as startup project, but for Server mode set both Bit.Client.Web.BlazorUI.Playground.Api.csproj and Bit.Client.Web.BlazorUI.Playground.Web.csproj as startup projects.

In shared project, you can also detect code is running in blazor server or client/wasm modes by use any of followings:

```cs

if (Bit.Client.Web.BlazorUI.Playground.Shared.BlazorModeDetector.Current.IsServer())
{
}

#if BlazorClient

#endif

```

It's recommended to clear your browser's cache while switching between server and client/wasm modes!
