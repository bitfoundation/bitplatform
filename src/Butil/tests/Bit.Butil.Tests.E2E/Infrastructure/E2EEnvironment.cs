namespace Bit.Butil.Tests.E2E.Infrastructure;

/// <summary>
/// Everything the suite reads from the environment to decide what it runs against, in one place. Environment
/// variables rather than runsettings, which the Microsoft.Testing.Platform runner does not thread through
/// reliably. (How the browser is launched is <see cref="BrowserLaunch"/>'s, shared with the benchmarks.)
/// </summary>
public static class E2EEnvironment
{
    /// <summary>
    /// <c>BUTIL_E2E_HOST</c>: which host every test in the run drives - one of <see cref="HarnessHostKinds.All"/>.
    /// Defaults to <see cref="HarnessHostKinds.Standalone"/>, the standalone WebAssembly sample.
    /// </summary>
    public static string Host { get; } = HarnessHostKinds.Validate(Read("BUTIL_E2E_HOST") ?? HarnessHostKinds.Standalone);

    /// <summary>
    /// <c>BUTIL_E2E_FRAMEWORK</c>: the target framework the web harness host runs on (net11.0, net10.0, net9.0 or
    /// net8.0). Defaults to net11.0. The standalone sample and the hybrid host are net11.0 only.
    /// </summary>
    public static string Framework { get; } = Read("BUTIL_E2E_FRAMEWORK") ?? "net11.0";

    /// <summary>
    /// <c>BUTIL_E2E_CONFIGURATION</c>: the build configuration of the harness hosts. Defaults to the
    /// configuration this test assembly was built in.
    /// </summary>
    public static string Configuration { get; } = Read("BUTIL_E2E_CONFIGURATION") ??
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    /// <summary>
    /// <c>BUTIL_E2E_BASE_URL</c>: an already-running deployment of the standalone sample (a trimmed publish
    /// served by any static server, say) to drive instead of booting one. Standalone host only.
    /// </summary>
    public static string? ExternalBaseUrl { get; } = Read("BUTIL_E2E_BASE_URL");

    /// <summary>
    /// <c>BUTIL_E2E_PUBLISHED_HOST</c>: a folder holding a <c>dotnet publish</c> output of
    /// Bit.Butil.Tests.Harness.Web. When set, a web render mode runs that (trimmed, AOT-compiled, ...) build
    /// instead of the project's build output, and nothing is built.
    /// </summary>
    public static string? PublishedHost { get; } = Read("BUTIL_E2E_PUBLISHED_HOST");

    /// <summary><c>BUTIL_E2E_SKIP_BUILD=1</c>: the harness hosts were built beforehand (as CI does); do not rebuild them.</summary>
    public static bool SkipBuild { get; } = Read("BUTIL_E2E_SKIP_BUILD") == "1";

    /// <summary>
    /// <c>BUTIL_E2E_HYBRID_INSTANCES</c>: how many BlazorWebView app windows the hybrid host runs side by side.
    /// A test holds one window for its whole duration, so this is the hybrid run's degree of parallelism.
    /// Defaults to 2.
    /// </summary>
    public static int HybridInstances { get; } = int.TryParse(Read("BUTIL_E2E_HYBRID_INSTANCES"), out var instances) && instances > 0 ? instances : 2;

    private static string? Read(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
