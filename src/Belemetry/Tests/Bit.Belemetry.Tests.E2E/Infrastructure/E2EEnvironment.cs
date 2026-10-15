namespace Bit.Belemetry.Tests.E2E.Infrastructure;

/// <summary>
/// Everything the suite reads from the environment, in one place. Environment variables rather than runsettings, which
/// the Microsoft.Testing.Platform runner does not thread through reliably.
/// </summary>
public static class E2EEnvironment
{
    /// <summary>
    /// <c>BELEMETRY_E2E_FRAMEWORK</c>: the target framework the web harness host runs on (net10.0, net9.0 or net8.0).
    /// Defaults to net10.0. The hybrid host is always net10.0-windows.
    /// </summary>
    public static string Framework { get; } = Read("BELEMETRY_E2E_FRAMEWORK") ?? "net10.0";

    /// <summary>
    /// <c>BELEMETRY_E2E_CONFIGURATION</c>: the build configuration of the harness hosts. Defaults to the configuration
    /// this test assembly was built in.
    /// </summary>
    public static string Configuration { get; } = Read("BELEMETRY_E2E_CONFIGURATION") ??
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    /// <summary>
    /// <c>BELEMETRY_E2E_HOSTS</c>: a comma-separated subset of <c>ssr</c>, <c>server</c>, <c>wasm</c>, <c>auto</c> and
    /// <c>hybrid</c> to run; the suites of the other hosts report Inconclusive. Defaults to all of them (hybrid on
    /// Windows only).
    /// </summary>
    public static IReadOnlySet<string> Hosts { get; } =
        (Read("BELEMETRY_E2E_HOSTS") ?? "ssr,server,wasm,auto,hybrid")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// <c>BELEMETRY_E2E_PUBLISHED_HOST</c>: a folder holding a <c>dotnet publish</c> output of
    /// Bit.Belemetry.Tests.Harness.Web. When set the suite runs that (trimmed, AOT-compiled, ...) build instead of the
    /// project's build output, and builds nothing.
    /// </summary>
    public static string? PublishedHost { get; } = Read("BELEMETRY_E2E_PUBLISHED_HOST");

    /// <summary><c>BELEMETRY_E2E_SKIP_BUILD=1</c>: the hosts were built beforehand (as CI does); do not rebuild them.</summary>
    public static bool SkipBuild { get; } = Read("BELEMETRY_E2E_SKIP_BUILD") == "1";

    /// <summary>
    /// <c>BELEMETRY_E2E_CHANNEL</c>: e.g. chrome / msedge, to use an installed browser. Defaults to chromium,
    /// Playwright's bundled Chromium in its new headless mode.
    /// </summary>
    public static string Channel { get; } = Read("BELEMETRY_E2E_CHANNEL") ?? "chromium";

    /// <summary><c>BELEMETRY_E2E_EXECUTABLE</c>: full path to a chromium-family executable.</summary>
    public static string? Executable { get; } = Read("BELEMETRY_E2E_EXECUTABLE");

    /// <summary><c>BELEMETRY_E2E_HEADED=1</c>: show the browser.</summary>
    public static bool Headed { get; } = Read("BELEMETRY_E2E_HEADED") == "1";

    /// <summary>Whether the suite of <paramref name="host"/> is selected by <see cref="Hosts"/>.</summary>
    public static bool Runs(string host) => Hosts.Contains(host);

    private static string? Read(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
