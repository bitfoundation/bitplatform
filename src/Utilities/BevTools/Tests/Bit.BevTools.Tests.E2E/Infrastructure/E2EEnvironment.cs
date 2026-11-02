namespace Bit.BevTools.Tests.E2E.Infrastructure;

/// <summary>
/// Everything the suite reads from the environment, in one place. Environment variables rather than
/// runsettings, which the Microsoft.Testing.Platform runner does not thread through reliably. The lazy loading
/// sample's own variables (<c>BEVTOOLS_E2E_LAZY_BASE_URL</c>, <c>BEVTOOLS_E2E_LAZY_AOT</c>) are read by the shared
/// harness, <c>Shared/LazyLoadingSample.cs</c>.
/// </summary>
public static class E2EEnvironment
{
    /// <summary>
    /// <c>BEVTOOLS_E2E_CONFIGURATION</c>: the configuration the lazy loading sample was published in. Defaults to the
    /// configuration this test assembly was built in, so a Release test run drives the Release publish that CI makes
    /// rather than looking for a Debug one that was never produced.
    /// </summary>
    public static string Configuration { get; } = Read("BEVTOOLS_E2E_CONFIGURATION") ??
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    /// <summary>
    /// <c>BEVTOOLS_E2E_CHANNEL</c>: e.g. chrome / msedge, to use an installed browser, or chromium-headless-shell for
    /// Playwright's headless shell. Without it the suite uses the full Chromium build Playwright installed, then its
    /// headless shell, and falls back to an installed Chrome, then Edge.
    /// </summary>
    public static string? Channel { get; } = Read("BEVTOOLS_E2E_CHANNEL");

    /// <summary>
    /// <c>BEVTOOLS_E2E_BROWSER</c>: <c>chromium</c> (the default), <c>firefox</c> or <c>webkit</c> - the browser engine
    /// the suite drives, from the browsers Playwright installed (<c>playwright.ps1 install firefox webkit</c>).
    /// </summary>
    public static string BrowserEngine { get; } = Read("BEVTOOLS_E2E_BROWSER")?.ToLowerInvariant() ?? "chromium";

    /// <summary><c>BEVTOOLS_E2E_EXECUTABLE</c>: full path to a chromium-family executable.</summary>
    public static string? Executable { get; } = Read("BEVTOOLS_E2E_EXECUTABLE");

    /// <summary><c>BEVTOOLS_E2E_HEADED=1</c>: show the browser.</summary>
    public static bool Headed { get; } = Read("BEVTOOLS_E2E_HEADED") == "1";

    /// <summary>
    /// True on a build server (<c>CI=true</c>, which GitHub Actions sets). A missing browser or a sample that
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
