namespace Bit.Blow.Tests.E2E.Infrastructure;

/// <summary>
/// Everything the suite reads from the environment, in one place. Environment variables rather than
/// runsettings, which the Microsoft.Testing.Platform runner does not thread through reliably.
/// </summary>
public static class E2EEnvironment
{
    /// <summary>
    /// <c>BLOW_E2E_CONFIGURATION</c>: the build configuration of the demo server the suite boots.
    /// Defaults to the configuration this test assembly was built in, so a Release test run drives the
    /// Release demo that CI builds rather than looking for a Debug build that was never produced.
    /// </summary>
    public static string Configuration { get; } = Read("BLOW_E2E_CONFIGURATION") ??
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    /// <summary>
    /// <c>BLOW_E2E_BASE_URL</c>: an already running demo (for example one started from the IDE). When set
    /// the suite drives that instead of booting the demo server itself.
    /// </summary>
    public static string? BaseUrl { get; } = Read("BLOW_E2E_BASE_URL")?.TrimEnd('/');

    /// <summary>
    /// <c>BLOW_E2E_BROWSER</c>: the engine to drive - <c>chromium</c> (the default), <c>firefox</c> or
    /// <c>webkit</c>, each the build Playwright installs.
    /// </summary>
    public static string Browser { get; } = Read("BLOW_E2E_BROWSER")?.ToLowerInvariant() ?? "chromium";

    /// <summary>True when the suite drives Chromium, the only engine with the DevTools protocol.</summary>
    public static bool IsChromium => Browser == "chromium";

    /// <summary>
    /// Ends a test that presses Shift+F10 or the context-menu key as Inconclusive outside Chromium. The canvas
    /// relies on the browser turning that key into a <c>contextmenu</c> event, which real Firefox and Safari do,
    /// but Playwright's Firefox and WebKit deliver only the key.
    /// </summary>
    public static void RequireKeyboardContextMenu()
    {
        if (IsChromium is false)
            Assert.Inconclusive($"Playwright's {Browser} does not raise contextmenu for a key press.");
    }

    /// <summary>
    /// <c>BLOW_E2E_CHANNEL</c>: e.g. chrome / msedge, to use an installed browser. Without it the suite
    /// uses the Chromium Playwright installed and falls back to an installed Chrome, then Edge. Chromium only.
    /// </summary>
    public static string? Channel { get; } = Read("BLOW_E2E_CHANNEL");

    /// <summary><c>BLOW_E2E_EXECUTABLE</c>: full path to an executable of the engine <see cref="Browser"/> names.</summary>
    public static string? Executable { get; } = Read("BLOW_E2E_EXECUTABLE");

    /// <summary><c>BLOW_E2E_HEADED=1</c>: show the browser.</summary>
    public static bool Headed { get; } = Read("BLOW_E2E_HEADED") == "1";

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
