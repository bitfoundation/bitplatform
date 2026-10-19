namespace Bit.Bmotion.Tests.E2E.Infrastructure;

/// <summary>
/// Everything the suite reads from the environment to decide what it runs against, in one place. Environment
/// variables rather than runsettings, which the Microsoft.Testing.Platform runner does not thread through
/// reliably. (How the browser is launched is <see cref="BrowserLaunch"/>'s.)
/// </summary>
public static class E2EEnvironment
{
    /// <summary>
    /// <c>BMOTION_E2E_HOST</c>: the render mode every page of the run is served in - one of
    /// <see cref="HarnessHostKinds.All"/>. Defaults to <see cref="HarnessHostKinds.WebAssembly"/>, the one runtime
    /// every Bit.Bmotion feature is available on.
    /// </summary>
    public static string Host { get; } = HarnessHostKinds.Validate(Read("BMOTION_E2E_HOST") ?? HarnessHostKinds.WebAssembly);

    /// <summary><c>BMOTION_E2E_FRAMEWORK</c>: the target framework the harness host runs on. Defaults to net10.0.</summary>
    public static string Framework { get; } = Read("BMOTION_E2E_FRAMEWORK") ?? "net10.0";

    /// <summary>
    /// <c>BMOTION_E2E_CONFIGURATION</c>: the build configuration of the harness host. Defaults to the configuration
    /// this test assembly was built in.
    /// </summary>
    public static string Configuration { get; } = Read("BMOTION_E2E_CONFIGURATION") ??
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    /// <summary>
    /// <c>BMOTION_E2E_PUBLISHED_HOST</c>: a folder holding a <c>dotnet publish</c> output of
    /// Bit.Bmotion.Tests.Harness.Web to run instead of the build output (a trimmed or AOT-compiled build, see
    /// publish-gate.sh). Nothing is built when it is set.
    /// </summary>
    public static string? PublishedHost { get; } = Read("BMOTION_E2E_PUBLISHED_HOST");

    /// <summary><c>BMOTION_E2E_SKIP_BUILD=1</c>: the harness host was built beforehand (as CI does); do not rebuild it.</summary>
    public static bool SkipBuild { get; } = Read("BMOTION_E2E_SKIP_BUILD") == "1";

    private static string? Read(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
