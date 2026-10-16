namespace Bit.Bmage.Tests.E2E.Infrastructure;

/// <summary>
/// Everything the suite reads from the environment, in one place. Environment variables rather than
/// runsettings, which the Microsoft.Testing.Platform runner does not thread through reliably.
/// </summary>
public static class E2EEnvironment
{
    /// <summary>
    /// <c>BMAGE_E2E_HOST_URL</c>: an already-running deployment of the demo (a <c>dotnet run</c> in
    /// another terminal, an AOT publish, the live site) to drive instead of booting one. When unset, the
    /// suite builds Bit.Bmage.Demo.Server and starts it on a free port on 127.0.0.1.
    /// </summary>
    public static string? HostUrl { get; } = Read("BMAGE_E2E_HOST_URL")?.TrimEnd('/');

    /// <summary>
    /// <c>BMAGE_E2E_CONFIGURATION</c>: the build configuration of the demo host the suite boots. Defaults
    /// to the configuration this test assembly was built in.
    /// </summary>
    public static string Configuration { get; } = Read("BMAGE_E2E_CONFIGURATION") ??
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    /// <summary><c>BMAGE_E2E_SKIP_BUILD=1</c>: the demo host was built beforehand (as CI does); do not rebuild it.</summary>
    public static bool SkipBuild { get; } = Read("BMAGE_E2E_SKIP_BUILD") == "1";

    /// <summary>
    /// <c>BMAGE_E2E_CHANNEL</c>: e.g. chrome / msedge, to use an installed browser. When unset the suite
    /// tries Playwright's bundled Chromium first and then an installed Chrome and Edge, and reports the
    /// run as inconclusive if none of them launches.
    /// </summary>
    public static string? Channel { get; } = Read("BMAGE_E2E_CHANNEL");

    /// <summary><c>BMAGE_E2E_EXECUTABLE</c>: full path to a chromium-family executable.</summary>
    public static string? Executable { get; } = Read("BMAGE_E2E_EXECUTABLE");

    /// <summary><c>BMAGE_E2E_HEADED=1</c>: show the browser.</summary>
    public static bool Headed { get; } = Read("BMAGE_E2E_HEADED") == "1";

    private static string? Read(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
