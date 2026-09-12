using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI;

/// <summary>
/// Appends the content hash of an asset to its path.
/// The file is read through the hosting environment, so the hash is only computed where the ASP.NET Core
/// hosting assemblies are part of the app (server rendering). Whatever prerendering computed is carried over
/// to the render that follows it and wins there - on WebAssembly and in Blazor Hybrid, where the file cannot
/// be read, and on a server circuit, where it could be read again but from a request base the browser chose -
/// so both renders of a component emit the same markup. A path with nothing to reuse and no file to hash is
/// returned unchanged, instead of failing to load the types.
/// </summary>
internal static class BitAssetVersion
{
    // Resolved by name: a type reference would make the client load Microsoft.AspNetCore.Hosting.Abstractions,
    // which is what this indirection exists to avoid.
    private static readonly Type? WebHostEnvironmentType =
        Type.GetType("Microsoft.AspNetCore.Hosting.IWebHostEnvironment, Microsoft.AspNetCore.Hosting.Abstractions", throwOnError: false);

    // Every asset of a page is carried over in a single entry, keyed by the path alone, since the hash is
    // read off the file and so is the same for every tag pointing at it. A key per tag would instead repeat
    // the path in the blob for each of them, and two tags on the same asset would collide on it: the second
    // PersistAsJson throws, and the first TryTakeFromJson takes the entry away from the rest.
    private const string StateKey = nameof(BitAssetVersion);

    // Keyed on the state rather than held statically, so nothing outlives the request or the circuit it
    // belongs to, and two of them in flight never see each other's paths. Components initialize and the
    // persisting callbacks run on the renderer's dispatcher, so a state's entry is never touched concurrently.
    private static readonly ConditionalWeakTable<PersistentComponentState, PageAssets> Pages = new();

    private static PathBaseMemo? lastPathBase;

    /// <summary>
    /// Returns the path to render. <paramref name="interactive"/> says whether the component renders again
    /// after prerendering, which is when what this render computed is worth carrying over.
    /// </summary>
    internal static string? Resolve(IServiceProvider services, string? path, bool appendVersion, bool interactive)
    {
        if (appendVersion is false || string.IsNullOrEmpty(path)) return path;

        var state = services.GetService(typeof(PersistentComponentState)) as PersistentComponentState;

        if (state is not null && TryGetRestored(state, path, out var restored)) return restored;

        var webHost = WebHostEnvironmentType is null ? null : services.GetService(WebHostEnvironmentType);

        // No web root to hash the file against, and nothing carried over: the path is rendered as written.
        if (webHost is null) return path;

        var versionedPath = Append(webHost, path, GetPathBase(services));

        if (state is not null && interactive) CarryOver(state, path, versionedPath);

        return versionedPath;
    }

    /// <summary>
    /// Adds the path to what the page carries over. The first path of a state registers the one callback
    /// that persists all of them, with an explicit render mode: the framework infers the mode of a callback
    /// registered without one from the component it belongs to, and this callback belongs to the page, not
    /// to a component - it would throw. Auto is accepted by the server store and the WebAssembly store
    /// alike, so the entry reaches whichever runtime renders the page next, and with no once-only flag
    /// every store of a composite persist gets its own copy.
    /// </summary>
    private static void CarryOver(PersistentComponentState state, string path, string versionedPath)
    {
        var page = Pages.GetValue(state, static _ => new PageAssets());

        page.Paths[path] = versionedPath;

        if (page.Subscribed) return;

        page.Subscribed = true;

        // The subscription lives as long as the state does - the request's or the circuit's - so it is
        // never disposed on its own.
        _ = state.RegisterOnPersisting(() =>
        {
            Persist(state, page.Paths);

            return Task.CompletedTask;
        }, RenderMode.InteractiveAuto);
    }

    /// <summary>
    /// Answers from the page's paths, and takes the persisted entry on a miss. TryTakeFromJson removes what
    /// it reads, so the entry is merged into the page's paths for the components that initialize after the
    /// first one. It is taken again on every later miss rather than once per state: on WebAssembly the state
    /// is the app's for its whole lifetime, and .NET 10 refills it on enhanced navigation, so a page reached
    /// later has its entry appear after the first take.
    /// </summary>
    private static bool TryGetRestored(PersistentComponentState state, string path, [NotNullWhen(true)] out string? value)
    {
        var page = Pages.GetValue(state, static _ => new PageAssets());

        if (page.Paths.TryGetValue(path, out value)) return true;

        var taken = TakePersisted(state);

        if (taken is null) return false;

        foreach (var (persistedPath, versionedPath) in taken)
        {
            page.Paths[persistedPath] = versionedPath;
        }

        return page.Paths.TryGetValue(path, out value);
    }

    // The persisted value is a dictionary of strings, so nothing the serializer needs is reachable only through reflection.
    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "The persisted value is a dictionary of strings.")]
    private static Dictionary<string, string>? TakePersisted(PersistentComponentState state)
    {
        return state.TryTakeFromJson<Dictionary<string, string>>(StateKey, out var value) ? value : null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "The persisted value is a dictionary of strings.")]
    private static void Persist(PersistentComponentState state, Dictionary<string, string> paths)
    {
        state.PersistAsJson(StateKey, paths);
    }

    /// <summary>
    /// The path an app hosted under a sub-path is served from, read from the NavigationManager every Blazor
    /// host registers rather than from IHttpContextAccessor, which an app only has after calling
    /// AddHttpContextAccessor and whose absence would silently leave the versioning to miss every file.
    /// The answer is the same for every tag of a request, so it is memoized on the base URI.
    /// </summary>
    private static string GetPathBase(IServiceProvider services)
    {
        if (services.GetService(typeof(NavigationManager)) is not NavigationManager navigationManager) return string.Empty;

        string baseUri;

        try
        {
            baseUri = navigationManager.BaseUri;
        }
        catch (InvalidOperationException)
        {
            // A NavigationManager that was never initialized throws on BaseUri. Rendering these tags through
            // HtmlRenderer outside a Razor Components endpoint - an email template, a generated page - is a
            // legitimate use, and no base is the same answer there was when there was no request to read one from.
            return string.Empty;
        }

        var memo = lastPathBase;

        if (memo is not null && memo.BaseUri == baseUri) return memo.PathBase;

        var pathBase = Uri.TryCreate(baseUri, UriKind.Absolute, out var uri)
                       ? Uri.UnescapeDataString(uri.AbsolutePath).TrimEnd('/')
                       : string.Empty;

        // PathString accepts only a rooted value; a base URI without a rooted path (about:blank) means no base.
        if (pathBase.StartsWith('/') is false) pathBase = string.Empty;

        lastPathBase = new PathBaseMemo(baseUri, pathBase);

        return pathBase;
    }

    // Kept apart from Resolve so the hosting types are only ever loaded after the probe above passed.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string Append(object webHost, string path, string pathBase)
    {
        return BitFileVersionProvider.AppendFileVersion(((IWebHostEnvironment)webHost).WebRootFileProvider, new PathString(pathBase), path);
    }



    private sealed class PageAssets
    {
        /// <summary>What was restored into this state and what this render computed, both keyed by the path as written.</summary>
        public Dictionary<string, string> Paths { get; } = new(StringComparer.Ordinal);

        public bool Subscribed { get; set; }
    }

    private sealed record PathBaseMemo(string BaseUri, string PathBase);
}
