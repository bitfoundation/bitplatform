using System.Collections.Concurrent;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace Bit.BlazeDb.Browser;

/// <summary>
/// Loads BlazeDb's JS modules once each and resolves their URLs against the document base.
/// Relative URLs passed to <see cref="JSHost.ImportAsync"/> resolve against the runtime's
/// <c>_framework/</c> directory rather than the app base, which is why the base URI is applied here.
/// </summary>
[SupportedOSPlatform("browser")]
internal static partial class BlazeDbBrowserModules
{
    public const string Opfs = "bit-blazedb-opfs";
    public const string Crypto = "bit-blazedb-crypto";
    public const string IndexedDb = "bit-blazedb-idb";
    public const string Tabs = "bit-blazedb-tabs";

    private static readonly ConcurrentDictionary<string, Task> Imports = new();

    public static Task EnsureAsync(string moduleName, string? moduleUrl = null)
    {
        // A faulted attempt must not stay cached, otherwise every later attempt replays the failure.
        if (Imports.TryGetValue(moduleName, out var existing) && !existing.IsFaulted)
        {
            return existing;
        }
        var import = ImportAsync(moduleName, moduleUrl is null ? ResolveUrl(moduleName) : ResolveCustomUrl(moduleUrl));
        Imports[moduleName] = import;
        return import;
    }

    /// <summary>
    /// Imports a module, making sure first that it can be fetched.
    /// <para>
    /// The runtime keeps the promise of the first <see cref="JSHost.ImportAsync"/> for a module name for the life
    /// of the page - a rejected one included, whatever URL a later call passes - and the <c>[JSImport]</c>
    /// bindings find a module only by that name. So an import that fails because the app is offline and the file
    /// is not cached yet, or because a wrong URL was passed, could never be retried: every later open, even with
    /// the URL corrected, would get the same rejection until the page reloads. Fetching the file first turns
    /// those failures into one that leaves the name untouched; the module itself then normally comes from the
    /// HTTP cache. A module that downloads but fails to load - a syntax error, a content security policy - still
    /// fails the import for good, and the exception says so.
    /// </para>
    /// </summary>
    private static async Task ImportAsync(string moduleName, string moduleUrl)
    {
        JSObject? response = null;
        try
        {
            response = await Fetch(moduleUrl).ConfigureAwait(false);
        }
        catch (JSException ex)
        {
            // fetch rejects when the network is down - the case worth keeping retryable - and also when a content
            // security policy allows the script (script-src) but not a fetch of it (connect-src). Offline the import
            // would fail too, so it is not attempted; otherwise it is, as it always was.
            if (IsOffline())
            {
                throw new InvalidOperationException(
                    $"BlazeDb could not download its script '{moduleUrl}': the browser is offline and the file is not cached. " +
                    "The open can be retried once the connection is back.", ex);
            }
        }
        using (response)
        {
            if (response is not null && !response.GetPropertyAsBoolean("ok"))
            {
                throw new InvalidOperationException(
                    $"BlazeDb could not download its script '{moduleUrl}' (HTTP {response.GetPropertyAsInt32("status")}). " +
                    "Check that the app references the Bit.BlazeDb package's static web assets, or pass the module's URL. " +
                    "The open can be retried.");
            }
        }
        try
        {
            await JSHost.ImportAsync(moduleName, moduleUrl).ConfigureAwait(false);
        }
        catch (JSException ex)
        {
            throw new InvalidOperationException(
                $"BlazeDb downloaded its script '{moduleUrl}' but the browser could not load it as a module. The browser " +
                "keeps that failure for the rest of the page's life, so reload the page once the cause is fixed.", ex);
        }
    }

    // A network failure - offline, say - rejects with a TypeError, which surfaces here as a JSException and fails
    // this attempt alone.
    [JSImport("globalThis.fetch")]
    private static partial Task<JSObject> Fetch(string url);

    private static bool IsOffline()
    {
        using var navigator = JSHost.GlobalThis.GetPropertyAsJSObject("navigator");
        return navigator is not null && navigator.HasProperty("onLine") && !navigator.GetPropertyAsBoolean("onLine");
    }

    /// <summary>
    /// A <c>moduleUrl</c> the application passed, made absolute the way <see cref="JSHost.ImportAsync"/> has always
    /// resolved it - against the runtime's <c>_framework/</c> directory - so the probe fetches the file the import loads.
    /// </summary>
    private static string ResolveCustomUrl(string moduleUrl)
    {
        if (Uri.TryCreate(moduleUrl, UriKind.Absolute, out var absolute) && absolute.Scheme is "http" or "https")
        {
            return moduleUrl;
        }
        using var document = JSHost.GlobalThis.GetPropertyAsJSObject("document");
        var baseUri = document?.GetPropertyAsString("baseURI");
        return string.IsNullOrEmpty(baseUri) || !Uri.TryCreate(baseUri, UriKind.Absolute, out var baseUrl)
            ? moduleUrl
            : new Uri(new Uri(baseUrl, "_framework/"), moduleUrl).AbsoluteUri;
    }

    private static string ResolveUrl(string moduleName)
    {
        var path = $"_content/Bit.BlazeDb/{moduleName}.js";
        using var document = JSHost.GlobalThis.GetPropertyAsJSObject("document");
        var baseUri = document?.GetPropertyAsString("baseURI");
        // Resolved as a relative reference, the way a <script src> would be, rather than by string
        // concatenation: without a <base href="…/"> the document's baseURI is its own URL, and
        // "https://host/app/page" + path would name a file that does not exist.
        return string.IsNullOrEmpty(baseUri) || !Uri.TryCreate(baseUri, UriKind.Absolute, out var baseUrl)
            ? "/" + path
            : new Uri(baseUrl, path).AbsoluteUri;
    }
}
