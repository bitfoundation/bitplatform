namespace Bit.Butil.Tests.E2E.Infrastructure;

/// <summary>
/// The hosts the suite can drive, by the names <c>BUTIL_E2E_HOST</c> takes. The web render modes are the
/// <c>ButilHarness:Mode</c> names of Bit.Butil.Tests.Harness.Web (HarnessRenderModes there), restated rather
/// than referenced because this project deliberately references nothing it drives.
/// </summary>
public static class HarnessHostKinds
{
    /// <summary>Bit.Butil.Samples.Web: a standalone Blazor WebAssembly app, no server side at all.</summary>
    public const string Standalone = "standalone";

    public const string Server = "server";
    public const string ServerNoPrerender = "server-noprerender";
    public const string WebAssembly = "wasm";
    public const string WebAssemblyNoPrerender = "wasm-noprerender";
    public const string Auto = "auto";
    public const string AutoNoPrerender = "auto-noprerender";

    /// <summary>Bit.Butil.Tests.Harness.Hybrid: the pages inside a WinForms BlazorWebView, driven over CDP. Windows only.</summary>
    public const string Hybrid = "hybrid";

    public static IReadOnlyList<string> All { get; } =
        [Standalone, Server, ServerNoPrerender, WebAssembly, WebAssemblyNoPrerender, Auto, AutoNoPrerender, Hybrid];

    /// <summary>The render modes served by the Blazor Web App harness host.</summary>
    public static bool IsWebApp(string host) => host is Server or ServerNoPrerender or WebAssembly or WebAssemblyNoPrerender or Auto or AutoNoPrerender;

    /// <summary>Whether the first response of the host already contains the rendered page.</summary>
    public static bool Prerenders(string host) => host is Server or WebAssembly or Auto;

    /// <summary>
    /// The values <c>data-platform</c> on the harness page may take in this host: "browser" inside the
    /// WebAssembly runtime, "dotnet" in a circuit or a BlazorWebView. Auto may pick either for any one visit.
    /// </summary>
    public static IReadOnlyList<string> ExpectedPlatforms(string host) => host switch
    {
        Standalone or WebAssembly or WebAssemblyNoPrerender => ["browser"],
        Server or ServerNoPrerender or Hybrid => ["dotnet"],
        _ => ["dotnet", "browser"],
    };

    /// <summary>The <c>RendererInfo.Name</c> values the harness page may report in this host (.NET 9 and later).</summary>
    public static IReadOnlyList<string> ExpectedRenderers(string host) => host switch
    {
        Standalone or WebAssembly or WebAssemblyNoPrerender => ["WebAssembly"],
        Server or ServerNoPrerender => ["Server"],
        Hybrid => ["WebView"],
        _ => ["Server", "WebAssembly"],
    };

    public static string Validate(string host) => All.Contains(host)
        ? host
        // A typo must not quietly fall back to the default host and let a whole run pass against the wrong one.
        : throw new InvalidOperationException($"Unknown BUTIL_E2E_HOST '{host}'. Expected one of: {string.Join(", ", All)}.");
}
