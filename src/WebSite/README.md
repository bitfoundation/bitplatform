# Bit.Platform.WebSite
Blazor multi mode with best practices in mind!

In Directory.build.props, you can switch between blazor server / web assembly by using either

```xml
<BlazorMode>BlazorWebAssembly</BlazorMode>
```

```xml
<BlazorMode>BlazorServer</BlazorMode>
```

For blazor web assembly use followings in Bit.Platform.WebSite.Web:

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
```

for blazor server use:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
```

To run blazor web assembly only start Api project, but in blazor server you need multi startup for both api & web projects.

In shared project, you can also detect code is running in blazor server / web assembly mode by use any of followings:

```cs

if (Bit.Platform.WebSite.Shared.BlazorModeDetector.Current.IsBlazorServer())
{
}

#if BlazorWebAssembly

#endif

```

It's recommended to clear your browser's cache and close/open visual studio while switching between blazor modes!
