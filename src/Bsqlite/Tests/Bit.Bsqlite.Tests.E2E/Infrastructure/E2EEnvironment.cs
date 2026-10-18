namespace Bit.Bsqlite.Tests.E2E.Infrastructure;

/// <summary>
/// Everything the suite reads from the environment, in one place. Environment variables rather than
/// runsettings, which the Microsoft.Testing.Platform runner does not thread through reliably.
/// </summary>
public static class E2EEnvironment
{
    /// <summary>
    /// <c>BSQLITE_E2E_CONFIGURATION</c>: the build configuration of the demo server the demo tests boot.
    /// Defaults to the configuration this test assembly was built in, so a Release test run drives the
    /// Release demo that CI builds rather than looking for a Debug build that was never produced.
    /// </summary>
    public static string Configuration { get; } = Read("BSQLITE_E2E_CONFIGURATION") ??
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    /// <summary>
    /// <c>BSQLITE_E2E_BROWSER</c>: <c>chromium</c> (the default), <c>firefox</c> or <c>webkit</c>. The storage
    /// matrix matters most on the last two - neither has JSPI, so both run the asynchronous VFSes on the
    /// Asyncify build - but only Chromium is installed by CI, so they are opt-in and need
    /// <c>playwright.ps1 install firefox webkit</c> first.
    /// </summary>
    public static string Browser { get; } = (Read("BSQLITE_E2E_BROWSER") ?? "chromium").ToLowerInvariant();

    /// <summary>
    /// <c>BSQLITE_E2E_CHANNEL</c>: e.g. chrome / msedge, to use an installed browser. Without it the suite
    /// uses the Chromium Playwright installed and falls back to an installed Chrome, then Edge - which is also
    /// what keeps it runnable where Playwright's own browser download is blocked.
    /// </summary>
    public static string? Channel { get; } = Read("BSQLITE_E2E_CHANNEL");

    /// <summary><c>BSQLITE_E2E_EXECUTABLE</c>: full path to a browser executable of <see cref="Browser"/>'s family.</summary>
    public static string? Executable { get; } = Read("BSQLITE_E2E_EXECUTABLE");

    /// <summary><c>BSQLITE_E2E_HEADED=1</c>: show the browser.</summary>
    public static bool Headed { get; } = Read("BSQLITE_E2E_HEADED") == "1";

    /// <summary>
    /// <c>BSQLITE_E2E_DEMO_URL</c>: an already running demo (for example one started from the IDE). When set
    /// the demo tests drive that instead of booting the demo server themselves.
    /// </summary>
    public static string? DemoUrl { get; } = Read("BSQLITE_E2E_DEMO_URL")?.TrimEnd('/');

    /// <summary>
    /// <c>BSQLITE_SOAK=1</c>: run the soak tests (category <c>Soak</c>) - a worker killed mid-transaction a
    /// thousand times over per storage, and four then eight tabs committing at once. They take minutes rather
    /// than seconds, so an ordinary run reports them inconclusive.
    /// </summary>
    public static bool Soak { get; } = Read("BSQLITE_SOAK") is "1" or "true";

    /// <summary><c>BSQLITE_SOAK_TABS</c>: tabs in the concurrent-write soak (default 8).</summary>
    public static int SoakTabs { get; } = ReadSize("BSQLITE_SOAK_TABS", 8);

    /// <summary><c>BSQLITE_SOAK_ROWS</c>: rows each of those tabs commits (default 50).</summary>
    public static int SoakRows { get; } = ReadSize("BSQLITE_SOAK_ROWS", 50);

    /// <summary><c>BSQLITE_SOAK_KILLS</c>: mid-transaction kills per storage (default 1000).</summary>
    public static int SoakKills { get; } = ReadSize("BSQLITE_SOAK_KILLS", 1000);

    /// <summary>
    /// <c>BSQLITE_E2E_BENCHMARKS=1</c>: run the benchmark page (category <c>Benchmark</c>) and print its numbers.
    /// They are for a human comparing backends and encodings on one machine, not a gate, so an ordinary run
    /// reports them inconclusive.
    /// </summary>
    public static bool Benchmarks { get; } = Read("BSQLITE_E2E_BENCHMARKS") is "1" or "true";

    /// <summary>
    /// True on a build server (<c>CI=true</c>, which GitHub Actions sets). A missing browser makes the tests
    /// inconclusive on a developer machine, but on a build server that would turn "the browser tests never
    /// ran" into a green build, so the tests fail there instead.
    /// </summary>
    public static bool IsContinuousIntegration { get; } = string.Equals(Read("CI"), "true", StringComparison.OrdinalIgnoreCase);

    private static string? Read(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int ReadSize(string name, int fallback)
        => int.TryParse(Read(name), out var value) && value > 0 ? value : fallback;
}
