using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace Bit.BlazorUI;

/// <summary>
/// Appends the content hash of an asset to its path.
/// The file is read through the hosting environment, so this only happens where the ASP.NET Core
/// hosting assemblies are part of the app (server rendering). On WebAssembly and Blazor Hybrid they
/// are not, so the path is returned unchanged instead of failing to load the types.
/// </summary>
internal static class BitAssetVersion
{
    // Resolved by name: a type reference would make the client load Microsoft.AspNetCore.Hosting.Abstractions,
    // which is what this indirection exists to avoid.
    private static readonly Type? WebHostEnvironmentType =
        Type.GetType("Microsoft.AspNetCore.Hosting.IWebHostEnvironment, Microsoft.AspNetCore.Hosting.Abstractions", throwOnError: false);

    internal static string? TryAppend(IServiceProvider services, string? path)
    {
        if (string.IsNullOrEmpty(path)) return path;

        if (WebHostEnvironmentType is null) return path;

        if (services.GetService(WebHostEnvironmentType) is null) return path;

        return Append(services, path);
    }

    // Kept apart from TryAppend so the hosting types are only ever loaded after the probe above passed.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string Append(IServiceProvider services, string path)
    {
        var webHost = (IWebHostEnvironment)services.GetService(typeof(IWebHostEnvironment))!;
        var httpContextAccessor = services.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;

        return BitFileVersionProvider.AppendFileVersion(webHost.WebRootFileProvider,
                                                        httpContextAccessor?.HttpContext?.Request.PathBase ?? PathString.Empty,
                                                        path);
    }
}
