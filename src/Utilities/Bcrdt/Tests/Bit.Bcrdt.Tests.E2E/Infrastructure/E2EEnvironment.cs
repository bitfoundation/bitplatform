namespace Bit.Bcrdt.Tests.E2E.Infrastructure;

/// <summary>
/// Everything the suite reads from the environment, in one place. Environment variables rather than
/// runsettings, which the Microsoft.Testing.Platform runner does not thread through reliably.
/// </summary>
public static class E2EEnvironment
{
    /// <summary>
    /// <c>BCRDT_E2E_CONFIGURATION</c>: the configuration the lazy loading sample was published in, whose publish folder
    /// (<c>bin/&lt;configuration&gt;/net11.0/publish</c>) the suite boots. Defaults to the configuration this test
    /// assembly was built in, so a Release test run drives the Release sample that CI publishes. The demo itself is
    /// always published in Release, as cd.bcrdt.demo.yml publishes it (<see cref="DemoDirectory"/>).
    /// </summary>
    public static string Configuration { get; } = Read("BCRDT_E2E_CONFIGURATION") ??
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    /// <summary>
    /// <c>BCRDT_E2E_DEMO_DIR</c>: a folder holding a <c>dotnet publish</c> output of Bit.Bcrdt.Demo.Server (CI publishes it
    /// in a step of its own). The suite starts that instead of publishing the demo itself, next to its binaries.
    /// </summary>
    public static string? DemoDirectory { get; } = Read("BCRDT_E2E_DEMO_DIR") is { } directory ? Path.GetFullPath(directory) : null;

    /// <summary>
    /// <c>BCRDT_E2E_BASE_URL</c>: an already running demo (for example one started from the IDE). When set
    /// the suite drives that instead of booting the demo server itself.
    /// </summary>
    public static string? BaseUrl { get; } = Read("BCRDT_E2E_BASE_URL")?.TrimEnd('/');

    /// <summary>
    /// <c>BCRDT_E2E_BROWSER</c>: the engine the suite drives - <c>chromium</c> (the default), <c>firefox</c>
    /// or <c>webkit</c>. The demo runs on WebAssembly in all three, and each has its own text input, focus and
    /// IndexedDB behavior, so the suite is meant to be run once per engine.
    /// </summary>
    public static string Browser { get; } = Read("BCRDT_E2E_BROWSER")?.ToLowerInvariant() ?? "chromium";

    /// <summary>
    /// <c>BCRDT_E2E_CHANNEL</c>: e.g. chrome / msedge, to use an installed browser. Without it the suite
    /// uses the Chromium Playwright installed and falls back to an installed Chrome, then Edge. Chromium only.
    /// </summary>
    public static string? Channel { get; } = Read("BCRDT_E2E_CHANNEL");

    /// <summary><c>BCRDT_E2E_EXECUTABLE</c>: full path to an executable of the engine <see cref="Browser"/> names.</summary>
    public static string? Executable { get; } = Read("BCRDT_E2E_EXECUTABLE");

    /// <summary><c>BCRDT_E2E_HEADED=1</c>: show the browser.</summary>
    public static bool Headed { get; } = Read("BCRDT_E2E_HEADED") == "1";

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
