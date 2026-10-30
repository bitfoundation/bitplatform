namespace Bit.BlazorHeadlessUI.Tests.E2E.Infrastructure;

/// <summary>
/// Everything the suite reads from the environment, in one place. Environment variables rather than
/// runsettings, which the Microsoft.Testing.Platform runner does not thread through reliably.
/// </summary>
public static class E2EEnvironment
{
    /// <summary>
    /// <c>BLAZORHEADLESSUI_E2E_RENDER_MODE</c>: <c>WebAssembly</c> (default), as the site is published, or <c>Server</c>, which
    /// publishes the demo without its WebAssembly client (<c>-p:IncludeWasm=false</c>) so the same pages run on a Blazor
    /// Server circuit. A demo the suite does not publish itself (<see cref="DemoDirectory"/>, <see cref="BaseUrl"/>) has to
    /// be in that mode; <see cref="DemoHost"/> checks the markers of the page it serves.
    /// </summary>
    public static string RenderMode { get; } = Read("BLAZORHEADLESSUI_E2E_RENDER_MODE")?.ToLowerInvariant() switch
    {
        null or "webassembly" or "wasm" => "WebAssembly",
        "server" => "Server",
        var other => throw new InvalidOperationException($"BLAZORHEADLESSUI_E2E_RENDER_MODE is '{other}'; expected WebAssembly or Server."),
    };

    public static bool IsWebAssembly => RenderMode == "WebAssembly";

    /// <summary>
    /// <c>BLAZORHEADLESSUI_E2E_DEMO_DIR</c>: a folder holding a <c>dotnet publish</c> output of Bit.BlazorHeadlessUI.Demo.Server
    /// (CI publishes it in a step of its own). The suite starts that instead of publishing the demo itself.
    /// </summary>
    public static string? DemoDirectory { get; } = Read("BLAZORHEADLESSUI_E2E_DEMO_DIR") is { } directory ? Path.GetFullPath(directory) : null;

    /// <summary>
    /// <c>BLAZORHEADLESSUI_E2E_BASE_URL</c>: an already running demo (for example one started from the IDE). When set the
    /// suite drives that and neither publishes nor starts anything.
    /// </summary>
    public static string? BaseUrl { get; } = Read("BLAZORHEADLESSUI_E2E_BASE_URL")?.TrimEnd('/');

    /// <summary>
    /// <c>BLAZORHEADLESSUI_E2E_CHANNEL</c>: e.g. chrome / msedge, to use an installed browser. Without it the suite uses the
    /// Chromium Playwright installed and falls back to an installed Chrome, then Edge.
    /// </summary>
    public static string? Channel { get; } = Read("BLAZORHEADLESSUI_E2E_CHANNEL");

    /// <summary><c>BLAZORHEADLESSUI_E2E_EXECUTABLE</c>: full path to a Chromium-based browser's executable.</summary>
    public static string? Executable { get; } = Read("BLAZORHEADLESSUI_E2E_EXECUTABLE");

    /// <summary><c>BLAZORHEADLESSUI_E2E_HEADED=1</c>: show the browser.</summary>
    public static bool Headed { get; } = Read("BLAZORHEADLESSUI_E2E_HEADED") == "1";

    /// <summary>
    /// True on a build server (<c>CI=true</c>, which GitHub Actions sets). A missing browser or a demo that
    /// does not start makes the tests inconclusive on a developer machine, but on a build server that would
    /// turn "the browser tests never ran" into a green build, so the tests fail there instead.
    /// </summary>
    public static bool IsContinuousIntegration { get; } = string.Equals(Read("CI"), "true", StringComparison.OrdinalIgnoreCase);

    private static string? Read(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
