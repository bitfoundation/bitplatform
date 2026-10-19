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
internal static class BlazeDbBrowserModules
{
    public const string Opfs = "bit-blazedb-opfs";
    public const string Crypto = "bit-blazedb-crypto";
    public const string IndexedDb = "bit-blazedb-idb";
    public const string Tabs = "bit-blazedb-tabs";

    private static readonly ConcurrentDictionary<string, Task> Imports = new();

    public static Task EnsureAsync(string moduleName, string? moduleUrl = null)
    {
        // A faulted import must not stay cached, otherwise every later attempt replays the failure.
        if (Imports.TryGetValue(moduleName, out var existing) && !existing.IsFaulted)
        {
            return existing;
        }
        var import = JSHost.ImportAsync(moduleName, moduleUrl ?? ResolveUrl(moduleName));
        Imports[moduleName] = import;
        return import;
    }

    private static string ResolveUrl(string moduleName)
    {
        var path = $"_content/Bit.BlazeDb.Browser/{moduleName}.js";
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
