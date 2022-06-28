# TodoTemplate
Blazor multi mode with best practices in mind!

In Directory.build.props, you can switch between blazor server / web assembly and hybrid by using either

```xml
<BlazorMode>BlazorWebAssembly</BlazorMode>
```

```xml
<BlazorMode>BlazorServer</BlazorMode>
```

```xml
<BlazorMode>BlazorHybrid</BlazorMode>
```

For blazor web assembly use followings in TodoTemplate.App:

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
```

for blazor server use:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
```

For blazor hybrid, use Bit.BlazorUI.App project instead.

To run blazor web assembly only start Api project and use 'TodoTemplate.Api-BlazorWebAssembly' debug profile, but for blazor hybrid you need multi startup for both api & app projects. In blazor server you need multi startup for both api & web projects.
In Blazor hybrid and server, use 'TodoTemplate.Api' debug profile for api project.
For enhanced development experience, unload App project while debugging in server/web assembly modes and unload web project while debugging hybrid mode.

In shared project, you can also detect code is running in blazor server / web assembly or hybrid mode by use any of followings:

```cs

if (TodoTemplate.Shared.BlazorModeDetector.Current.IsBlazorServer())
{
}

#if BlazorWebAssembly

#endif

```

It's recommended to clear your browser's cache and close/open visual studio while switching between blazor modes!
