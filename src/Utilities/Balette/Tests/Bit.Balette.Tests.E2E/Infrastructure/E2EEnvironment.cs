namespace Bit.Balette.Tests.E2E.Infrastructure;

/// <summary>
/// Everything the suite reads from the environment, in one place. Environment variables rather than
/// runsettings, which the Microsoft.Testing.Platform runner does not thread through reliably.
/// </summary>
public static class E2EEnvironment
{
    /// <summary>
    /// <c>BALETTE_E2E_CONFIGURATION</c>: the build configuration of the demo server the suite boots.
    /// Defaults to the configuration this test assembly was built in, so a Release test run drives the
    /// Release demo that CI builds rather than looking for a Debug build that was never produced.
    /// </summary>
    public static string Configuration { get; } = Read("BALETTE_E2E_CONFIGURATION") ??
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    /// <summary>
    /// <c>BALETTE_E2E_BASE_URL</c>: an already running demo (for example one started from the IDE). When set
    /// the suite drives that instead of booting the demo server itself.
    /// </summary>
    public static string? BaseUrl { get; } = Read("BALETTE_E2E_BASE_URL")?.TrimEnd('/');

    /// <summary>
    /// <c>BALETTE_E2E_BROWSER</c>: the engine the suite drives - <c>chromium</c> (default), <c>firefox</c> or <c>webkit</c>,
    /// each the build Playwright installed. The palette's browser layer leans on what engines implement differently
    /// (the native dialog's top layer and focus, key events of other layouts, sticky headings, forced colours), so the
    /// workflow runs the suite in all three.
    /// </summary>
    public static string Browser { get; } = Read("BALETTE_E2E_BROWSER")?.ToLowerInvariant() switch
    {
        null or "chromium" or "chrome" => "chromium",
        "firefox" => "firefox",
        "webkit" or "safari" => "webkit",
        var other => throw new InvalidOperationException($"BALETTE_E2E_BROWSER is '{other}'; expected chromium, firefox or webkit."),
    };

    /// <summary>Whether the suite drives Chromium, the only engine with the DevTools protocol.</summary>
    public static bool IsChromium => Browser == "chromium";

    /// <summary>
    /// <c>BALETTE_E2E_CHANNEL</c>: e.g. chrome / msedge, to use an installed browser. Without it the suite
    /// uses the Chromium Playwright installed and falls back to an installed Chrome, then Edge. Chromium only.
    /// </summary>
    public static string? Channel { get; } = Read("BALETTE_E2E_CHANNEL");

    /// <summary><c>BALETTE_E2E_EXECUTABLE</c>: full path to an executable of the engine <see cref="Browser"/> names.</summary>
    public static string? Executable { get; } = Read("BALETTE_E2E_EXECUTABLE");

    /// <summary>
    /// <c>BALETTE_E2E_RENDER_MODE</c>: <c>WebAssembly</c> (default), as the site is published, or <c>Server</c>, which runs
    /// the same pages on a Blazor Server circuit - every key the palette handles then crosses the connection. A demo the
    /// suite starts itself is started in that mode; an external one (<see cref="BaseUrl"/>) has to have been.
    /// </summary>
    public static string RenderMode { get; } = string.Equals(Read("BALETTE_E2E_RENDER_MODE"), "Server", StringComparison.OrdinalIgnoreCase) ? "Server" : "WebAssembly";

    /// <summary><c>BALETTE_E2E_HEADED=1</c>: show the browser.</summary>
    public static bool Headed { get; } = Read("BALETTE_E2E_HEADED") == "1";

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
