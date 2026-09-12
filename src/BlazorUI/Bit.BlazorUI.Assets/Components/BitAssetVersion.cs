using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace Bit.BlazorUI;

/// <summary>
/// Appends the content hash of an asset to its path.
/// The file is read through the hosting environment, so this only happens where the ASP.NET Core
/// hosting assemblies are part of the app (server rendering). On WebAssembly and Blazor Hybrid they
/// are not, so the value the server persisted while prerendering is reused, and the path is returned
/// unchanged when there was no prerendering, instead of failing to load the types.
/// </summary>
internal static class BitAssetVersion
{
    // Resolved by name: a type reference would make the client load Microsoft.AspNetCore.Hosting.Abstractions,
    // which is what this indirection exists to avoid.
    private static readonly Type? WebHostEnvironmentType =
        Type.GetType("Microsoft.AspNetCore.Hosting.IWebHostEnvironment, Microsoft.AspNetCore.Hosting.Abstractions", throwOnError: false);

    /// <summary>
    /// Returns the path to render, along with the subscription that carries it over to the render that
    /// follows prerendering. The subscription is <c>default</c> when there is nothing to carry over,
    /// and disposing it is safe in either case.
    /// </summary>
    internal static (string? Path, PersistingComponentStateSubscription Subscription) Resolve(
        IServiceProvider services, string component, string? path, bool appendVersion, bool carryOverToClient)
    {
        if (appendVersion is false || string.IsNullOrEmpty(path)) return (path, default);

        var state = services.GetService(typeof(PersistentComponentState)) as PersistentComponentState;
        var key = $"{nameof(BitAssetVersion)}.{component}.{path}";

        if (WebHostEnvironmentType is null || services.GetService(WebHostEnvironmentType) is null)
        {
            // No web root to hash the file against. Reusing what the prerendering pass persisted keeps the
            // markup of an interactive component identical on both sides, so the browser is not sent back
            // for an asset it already has under its versioned path.
            return (state is not null && TryTakePersisted(state, key, out var persisted) ? persisted : path, default);
        }

        var versionedPath = Append(services, path!, GetPathBase(services));

        if (state is null || carryOverToClient is false) return (versionedPath, default);

        return (versionedPath, state.RegisterOnPersisting(() =>
        {
            Persist(state, key, versionedPath);
            return Task.CompletedTask;
        }));
    }

    // The persisted value is a string, so nothing the serializer needs is reachable only through reflection.
    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "The persisted value is a string.")]
    private static bool TryTakePersisted(PersistentComponentState state, string key, [NotNullWhen(true)] out string? value)
    {
        return state.TryTakeFromJson(key, out value) && value is not null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "The persisted value is a string.")]
    private static void Persist(PersistentComponentState state, string key, string value)
    {
        state.PersistAsJson(key, value);
    }

    /// <summary>
    /// The path an app hosted under a sub-path is served from, read from the NavigationManager every Blazor
    /// host registers rather than from IHttpContextAccessor, which an app only has after calling
    /// AddHttpContextAccessor and whose absence would silently leave the versioning to miss every file.
    /// </summary>
    private static string GetPathBase(IServiceProvider services)
    {
        if (services.GetService(typeof(NavigationManager)) is not NavigationManager navigationManager) return string.Empty;

        if (Uri.TryCreate(navigationManager.BaseUri, UriKind.Absolute, out var baseUri) is false) return string.Empty;

        return Uri.UnescapeDataString(baseUri.AbsolutePath).TrimEnd('/');
    }

    // Kept apart from Resolve so the hosting types are only ever loaded after the probe above passed.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string Append(IServiceProvider services, string path, string pathBase)
    {
        var webHost = (IWebHostEnvironment)services.GetService(typeof(IWebHostEnvironment))!;

        return BitFileVersionProvider.AppendFileVersion(webHost.WebRootFileProvider, new PathString(pathBase), path);
    }
}
