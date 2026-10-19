namespace Bit.Bmotion.Tests.E2E.Infrastructure;

/// <summary>
/// The render modes the suite can drive, by the names <c>BMOTION_E2E_HOST</c> takes - the
/// <c>BmotionHarness:Mode</c> names of Bit.Bmotion.Tests.Harness.Web (HarnessRenderModes there), restated rather
/// than referenced because this project deliberately references nothing it drives.
/// </summary>
public static class HarnessHostKinds
{
    /// <summary>Static server-side rendering: no interactive runtime. Requested per page with <c>?mode=ssr</c>.</summary>
    public const string Ssr = "ssr";

    public const string Server = "server";
    public const string ServerNoPrerender = "server-noprerender";
    public const string WebAssembly = "wasm";
    public const string WebAssemblyNoPrerender = "wasm-noprerender";
    public const string Auto = "auto";
    public const string AutoNoPrerender = "auto-noprerender";

    /// <summary>The interactive modes a whole run can be pointed at.</summary>
    public static IReadOnlyList<string> All { get; } =
        [Server, ServerNoPrerender, WebAssembly, WebAssemblyNoPrerender, Auto, AutoNoPrerender];

    /// <summary>Whether the first response of the host already contains the rendered page.</summary>
    public static bool Prerenders(string host) => host is Server or WebAssembly or Auto;

    /// <summary>The <c>data-renderer</c> values the harness page may report in this host.</summary>
    public static IReadOnlyList<string> ExpectedRenderers(string host) => host switch
    {
        WebAssembly or WebAssemblyNoPrerender => ["WebAssembly"],
        Server or ServerNoPrerender => ["Server"],
        _ => ["Server", "WebAssembly"],
    };

    public static string Validate(string host) => All.Contains(host)
        ? host
        // A typo must not quietly fall back to the default host and let a whole run pass against the wrong one.
        : throw new InvalidOperationException($"Unknown BMOTION_E2E_HOST '{host}'. Expected one of: {string.Join(", ", All)}.");
}
