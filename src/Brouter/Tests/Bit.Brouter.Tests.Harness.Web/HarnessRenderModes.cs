using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Brouter.Tests.Harness.Web;

/// <summary>
/// The render modes the harness host can serve, by the names the suites pass as
/// <c>--BrouterHarness:Mode=&lt;name&gt;</c> (or <c>UseSetting</c> in-process).
/// </summary>
public static class HarnessRenderModes
{
    public const string ConfigurationKey = "BrouterHarness:Mode";

    public const string Ssr = "ssr";
    public const string Server = "server";
    public const string ServerNoPrerender = "server-noprerender";
    public const string WebAssembly = "wasm";
    public const string WebAssemblyNoPrerender = "wasm-noprerender";
    public const string Auto = "auto";
    public const string AutoNoPrerender = "auto-noprerender";

    public static IReadOnlyList<string> All { get; } =
        [Ssr, Server, ServerNoPrerender, WebAssembly, WebAssemblyNoPrerender, Auto, AutoNoPrerender];

    /// <summary>Null means static server-side rendering: no interactive runtime at all.</summary>
    public static IComponentRenderMode? Resolve(string? mode) => mode switch
    {
        Ssr => null,
        Server => RenderMode.InteractiveServer,
        ServerNoPrerender => new InteractiveServerRenderMode(prerender: false),
        WebAssembly => RenderMode.InteractiveWebAssembly,
        WebAssemblyNoPrerender => new InteractiveWebAssemblyRenderMode(prerender: false),
        Auto => RenderMode.InteractiveAuto,
        AutoNoPrerender => new InteractiveAutoRenderMode(prerender: false),
        // A typo must not quietly fall back to some mode and let a whole suite pass against the wrong one.
        _ => throw new InvalidOperationException($"Unknown {ConfigurationKey} '{mode}'. Expected one of: {string.Join(", ", All)}.")
    };
}
