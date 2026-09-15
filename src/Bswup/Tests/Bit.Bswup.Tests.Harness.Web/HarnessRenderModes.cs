using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bswup.Tests.Harness.Web;

/// <summary>
/// The render modes the harness host can serve the Blazor Web App in, by the names the suites pass as
/// <c>--BswupHarness:Mode=&lt;name&gt;</c> (or <c>UseSetting</c> in-process).
/// </summary>
public static class HarnessRenderModes
{
    public const string ConfigurationKey = "BswupHarness:Mode";

    public const string WebAssembly = "wasm";
    public const string WebAssemblyNoPrerender = "wasm-noprerender";
    public const string Auto = "auto";
    public const string Server = "server";

    public static IReadOnlyList<string> All { get; } = [WebAssembly, WebAssemblyNoPrerender, Auto, Server];

    public static string FromConfiguration(IConfiguration configuration) => configuration[ConfigurationKey] ?? WebAssembly;

    /// <summary>
    /// A mode whose interactive runtime lives on the server cannot boot from a cached document: its component
    /// markers are tied to the request that rendered them. Such apps set forcePrerender, so the worker leaves
    /// navigations to the server and only serves the static assets.
    /// </summary>
    public static bool NeedsServerForNavigations(string mode) => mode is Auto or Server;

    /// <summary>
    /// The render mode of the harness root. <paramref name="prerender"/> is false for the document the service
    /// worker downloads as the app shell (its noPrerenderQuery), whatever the mode prerenders otherwise.
    /// </summary>
    public static IComponentRenderMode Resolve(string mode, bool prerender) => mode switch
    {
        WebAssembly => new InteractiveWebAssemblyRenderMode(prerender),
        WebAssemblyNoPrerender => new InteractiveWebAssemblyRenderMode(prerender: false),
        Auto => new InteractiveAutoRenderMode(prerender),
        Server => new InteractiveServerRenderMode(prerender),
        _ => throw new InvalidOperationException($"Unknown {ConfigurationKey} '{mode}'. Expected one of: {string.Join(", ", All)}.")
    };
}
